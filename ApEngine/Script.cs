using ApEngine.Base;
using ApEngine.UIs;
using AudioPrecision.API;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Xml.Linq;
using System.Xml.Serialization;
using TestCore;
using TestCore.Configuration;
using TestCore.Data;
using TestCore.Services;
using ToucanCore.Abstraction;
using ToucanCore.Abstraction.Configuration;
using ToucanCore.Abstraction.Engine;

namespace ApEngine
{
    public class Script : TF_Base, IScript
    {
        public const string DefaultSpecName = ".apspec";

        public ToucanCore.Abstraction.Engine.ISequence this[string name] => _Sequences.FirstOrDefault(x => x.Name == name);

        public ToucanCore.Abstraction.Engine.ISequence this[int index] => _Sequences[index];

        public string Name { get; private set; }

        public string Version { get; private set; }

        public DateTime Time { get; private set; }

        public string Author { get; set; }

        public string Note { get; set; }

        public string BaseDirectory { get; private set; }

        private List<Sequence> _Sequences = new List<Sequence>();
        public IReadOnlyCollection<ToucanCore.Abstraction.Engine.ISequence> Sequences { get { return _Sequences; } }

        public GlobalConfiguration SystemConfig { get; set; }

        /// <summary>
        /// this would update outside, the default is from SystemConfig
        /// For prevent Wrong infomation, which could get from remote server
        /// </summary>
        public StationConfig StationConfig { get; set; }

        /// <summary>
        /// this would update outside, the default is from SystemConfig
        /// For prevent Wrong infomation, which could get from remote server
        /// </summary>
        public SFCsConfig SFCsConfig { get; set; }


        public string FilePath { get; set; }

        public ToucanCore.Abstraction.Engine.ISequence ActiveSequence { get; private set; }

        public TF_Spec Spec { get; set; }

        public List<string> ImportFiles { get; } = new List<string>();
        public List<string> ExportFiles { get; } = new List<string>();

        public bool IsModified 
        {
            get 
            {
                if (string.IsNullOrEmpty(FilePath)) return true;  // For new one
                return ApxEngine.ApRef.IsProjectModified;
            }
        }

        public bool LockStatus
        {
            get
            {
                return ApxEngine.ApRef.IsProjectLocked;
            }
            set
            {
                LockScript(value);
            }
        }

        public InjectedVariableTable InjectedVariableTable { get; set; }

        public event EventHandler CalibrationExpired;
        public event EventHandler CalibrationExpiring;
        public event EventHandler ReferenceExpired;
        public event EventHandler ReferenceExpiring;
        public event EventHandler VerificationExpired;
        public event EventHandler VerificationExpiring;

        public int Open(string path)
        {
            if (ApxEngine.ApRef.ProjectFileName != path)
            {
                ApxEngine.ApRef.OpenProject(path);
                ApxEngine.ApRef.SignalMonitorsEnabled = false;

                if (!ApxEngine.ApRef.IsProjectLocked)
                {
                    LockScript(true);
                }
            }

            foreach (AudioPrecision.API.ISequenceSettings seq in ApxEngine.ApRef.Sequence.Sequences)
            {
                var sequence = new Sequence(seq);
                _Sequences.Add(sequence);

                if (seq == ApxEngine.ApRef.Sequence.Sequences.ActiveSequence)
                {
                    ActiveSequence = sequence;
                }
            }

            if(ActiveSequence.Name.Equals(ApxEngine.ReferenceSequenceName, StringComparison.OrdinalIgnoreCase) || ActiveSequence.Name.Equals(ApxEngine.VerificationSequenceName, StringComparison.OrdinalIgnoreCase))
            {
                var dutseq = Sequences.FirstOrDefault(x => x.Name.Equals(ApxEngine.DefaultSequenceName, StringComparison.OrdinalIgnoreCase));
                if(dutseq is null)
                {
                    dutseq = Sequences.FirstOrDefault(x => !(x.Name.Equals(ApxEngine.ReferenceSequenceName, StringComparison.OrdinalIgnoreCase) || x.Name.Equals(ApxEngine.VerificationSequenceName, StringComparison.OrdinalIgnoreCase)));
                }

                ((Sequence)dutseq).ApSequence.Activate();
                ActiveSequence = dutseq;
            }

            FileInfo fi = new FileInfo(path);
            Time = fi.LastWriteTime;

            FilePath = path;
            Name = Path.GetFileNameWithoutExtension(path);
            BaseDirectory = Directory.GetParent(path).FullName;

            Version = TF_Utility.GenerateAutoVersion(Time, 0).ToString();

            Author = ApxEngine.ApRef.Variables.GetUserDefinedVariable("Author");
            if (string.IsNullOrEmpty(Author))
            {
                InitVariables();   //
            }

            ApxEngine.ApRef.Sequence.Report.Checked = false;

            return 1;
        }

        public void LockScript(bool lockstatus = true)
        {
            if (lockstatus)
            {
                if (!ApxEngine.ApRef.IsProjectLocked)
                {
                    ApxEngine.ApRef.LockProject(LOCK);
                }
            }
            else
            {
                if (ApxEngine.ApRef.IsProjectLocked)
                {
                    ApxEngine.ApRef.UnlockProject(LOCK);
                }
            }
        }

        //const string DefaultSpecName = ".spec";
        const string LOCK = "locker";
        public int Save(string path = null)
        {
            //if (!ApxEngine.ApRef.IsProjectModified && path is null) return 1;  // ApxEngine.ApRef.IsProjectModified is not true even add limits, so save anyway

            if (path is null) path = FilePath;

            if (path is null) return 0;

            var dir = System.IO.Directory.GetParent(path).FullName;
            var configpath = Path.Combine(dir, GlobalConfiguration.DefaultFileName);
            SystemConfig?.Save(configpath);

            if(ActiveSequence.Name == ApxEngine.ReferenceSequenceName || ActiveSequence.Name == ApxEngine.VerificationSequenceName)
            {
                throw new InvalidOperationException("Current Active Sequence Name is REF/VER which is invalid as Entrypoint");
            }

            if (ApxEngine.ApRef.IsProjectModified)
            {
                for (int i = 0; i < ApxEngine.ApRef.Sequence.Count; i++)
                {
                    

                }

                ApxEngine.ApRef.SaveProject(path);
            }

            ((Sequence)ActiveSequence).Analyze();  // the change will not be effective if analyze before saving // Toucan will restart execution after saving
            
            ApxEngine.ApRef.LockProject(LOCK);  // the IsProjectModified will be false when lock first

            if (SystemConfig?.General?.RestrictLimit == true)
            {
                var specfilepath = Path.Combine(BaseDirectory, TF_Spec.DefaultFileName);

                if(File.Exists(specfilepath))
                {
                    var previewspec = TF_Spec.LoadFromXml(specfilepath);

                    var spec = TF_Spec.Merge(previewspec, ((Sequence)ActiveSequence).Spec, SystemConfig.General.Prefix_DefectCode);

                    FileInfo specfi = new FileInfo(specfilepath);
                    specfi.IsReadOnly = false;
                    File.Delete(previewspec.FilePath);

                    spec.ExportAsXml(specfilepath);
                }
                else
                {
                    Spec.UpdateDefectCode(SystemConfig.General.Prefix_DefectCode);
                    Spec.ExportAsXml(specfilepath);
                }
            }

            FilePath = path;
            FileInfo fi = new FileInfo(path);
            Time = fi.LastWriteTime;

            var specpath = Path.Combine(dir, $"{ActiveSequence.Name}{DefaultSpecName}");
            if (File.Exists(specpath))
            {
                FileInfo specfi = new FileInfo(specpath);
                specfi.IsReadOnly = false;
                File.Delete(specpath);
            }
            Spec = ((Sequence)ActiveSequence).Spec;
            Spec.Time = Time;

            Spec.Limit.Run((x) => { if (x is AP_Limit apl) { x.Name = $"{x.Name}${apl.ChannelIndex}"; } });   // Add channelindex

            Spec?.XmlSerialize()?.Save(specpath);
            FileInfo newspecfi = new FileInfo(specpath);
            newspecfi.IsReadOnly = true;

            return 1;
        }

        public void InitVariables()
        {
            ApxEngine.ApRef.Variables.SetUserDefinedVariable("Author", Author);
            ApxEngine.ApRef.Variables.SetUserDefinedVariable("Customer", StationConfig?.CustomerName);
            ApxEngine.ApRef.Variables.SetUserDefinedVariable("Product", StationConfig?.ProductName);
            ApxEngine.ApRef.Variables.SetUserDefinedVariable("Station", StationConfig?.StationName);
            ApxEngine.ApRef.Variables.SetUserDefinedVariable("IsSfc", (SFCsConfig?.EnableSfc ?? false)?"1":"0");
            ApxEngine.ApRef.Variables.SetUserDefinedVariable("SlotIndex", "0");
            ApxEngine.ApRef.Variables.SetUserDefinedVariable("SN", "");
            //ApxEngine.ApRef.Variables.SetUserDefinedVariable("SFCs_ExtColumn", "");  // APx can not update the variable it self
            //ApxEngine.ApRef.Variables.SetUserDefinedVariable("SFCs_ExtValue", "");  // for data the engine should gather from execution, which might used to commit into NAS.
            ApxEngine.ApRef.Variables.SetUserDefinedVariable("RefBase", GetReferenceBase());
            ApxEngine.ApRef.Variables.SetUserDefinedVariable("VerBase", GetVerificationBase());
        }

        public object Clone()
        {
            throw new NotImplementedException();
        }

        public async Task<int> AnalyzeAsync()
        {
            var t = Task.Run(() => { return Analyze(); });
            await t;
            return t.Result;
        }

        public int Analyze()
        {
            Spec = AnalyzeSpec();

            return 1;
        }

        public TF_Spec AnalyzeSpec()
        {
            var defaultspecfile = Path.Combine(BaseDirectory, $"{ActiveSequence.Name}{DefaultSpecName}");

            TF_Spec seqspec = null;
            if (File.Exists(defaultspecfile))
            {
                try
                {
                    var filespec = TF_Spec.LoadFromXml(defaultspecfile);

                    //var defaultspecfilefi = new FileInfo(defaultspecfile);  // prevent engineer save the file after running sequence
                    //defaultspecfilefi.IsReadOnly = false;
                    //defaultspecfilefi.Delete();

                    if (Math.Abs(filespec.Time.Subtract(Time).TotalSeconds) <= 1)
                    {
                        Regex re = new Regex(@"(.+)\$(-?\d+)$");
                        var temp = filespec.Limit.Run((x) =>
                        {
                            var mtc = re.Match(x.Name);
                            if (mtc.Success)
                            {
                                var chidx = int.Parse(mtc.Groups[2].Value);
                                return new AP_Limit(mtc.Groups[1].Value, x.USL, x.LSL, x.Comp, x.Defect, x.Unit, x.Format, x.Skip, x.Sfc) { ChannelIndex = chidx };
                            }
                            else
                            {
                                return x;
                            }
                        });

                        filespec.Limit.Clear();
                        foreach (var item in temp) filespec.Limit.Add(item);

                        seqspec = filespec;
                        ((Sequence)ActiveSequence).Spec = seqspec;
                    }
                    else
                    {
                        seqspec = AnalyzeSpec((Sequence)ActiveSequence);
                    }
                }
                catch
                {
                    seqspec = AnalyzeSpec((Sequence)ActiveSequence);
                }
            }
            else
            {
                seqspec = AnalyzeSpec((Sequence)ActiveSequence);
            }

            return seqspec;
        }

        public TF_Spec AnalyzeSpec(Sequence seq)
        {
            if (seq.Spec is null)
            {
                seq.Analyze();
            }

            seq.Spec.Author = "Joey";//AuthService.UserName;
            seq.Spec.Time = DateTime.Now;//TimeService.CurrentTime;
            seq.Spec.Note = "Auto Generate By Toucan";

            return seq.Spec;
        }

        public int Activate(string name)
        {
            if (ApxEngine.ApRef.Sequence.Sequences.ActiveSequence.Name == name)
            {
                if (ActiveSequence.Name == name) { }
                else
                {
                    ActiveSequence = Sequences.FirstOrDefault(x => x.Name == name);
                }
                return 1;
            }

            if (Sequences.FirstOrDefault(x => x.Name == name) is Sequence seq)
            {
                return Activate(seq);
            }

            return 0;
        }

        public int Activate(Sequence seq)
        {
            //var str = seq.ApSequence.Name;

            if (ApxEngine.Mre_Operation.WaitOne())
            {
                ApxEngine.Mre_Operation.Reset();
                try
                {
                    seq.Activate();
                    Spec = seq.Spec;
                    ActiveSequence = seq;
                    return 1;
                }
                catch(Exception ex)
                {
                    var msg = $"Active Sequence {seq.Name} Failed. Current is {ApxEngine.ApRef.Sequence.Sequences.ActiveSequence.Name}. Err {ex.Message}";
                    Warn(msg);
                    MessageBox.Show(msg, "Need Reopen Script");
                }
                finally
                {
                    ApxEngine.Mre_Operation.Set();
                }
            }

            return 0;
        }

        public int StartCalibration()
        {
            ScriptCalibration sc = new ScriptCalibration(this);

            if(sc.ShowDialog() == true)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        Timer CalibrationExpiredTimer;
        public int ApplyCalibration()
        {
            if (SystemConfig?.General.CalibrationPeriod > 0)
            {
                var path = $"{GetCalibrationBase()}{ScriptCalibData.FileExt}";

                if (File.Exists(path))
                {
                    var calibdata = ScriptCalibData.Load(path);
                    calibdata.RelevantDir = GetCalibrationBase();
                    calibdata.ValidTime = TimeSpan.FromDays(SystemConfig.General.CalibrationPeriod);
                    calibdata.WarnTime = TimeSpan.FromDays(SystemConfig.General.CalibrationWarning);

                    var timespan = calibdata.UpdateTime.Add(calibdata.ValidTime).Subtract(DateTime.Now);
                    var wt = timespan.Subtract(calibdata.WarnTime);

                    if (timespan.TotalMilliseconds <= 0)
                    {
                        throw new CalibrationDataExpiredException() { CalibrationDataPath = path };
                    }
                    else
                    {
                        if (CalibrationExpiredTimer != null)
                        {
                            CalibrationExpiredTimer.Stop();
                            CalibrationExpiredTimer.Close();
                            CalibrationExpiredTimer.Dispose();
                        }

                        CalibrationExpiredTimer = new Timer();

                        if (wt.TotalMilliseconds < 0)
                        {
                            CalibrationExpiredTimer.Interval = timespan.TotalMilliseconds;
                            CalibrationExpiredTimer.Elapsed += (sender, e) =>
                            {
                                CalibrationExpiredTimer.Stop();
                                CalibrationExpired?.Invoke(this, e);
                            };
                            throw new CalibrationDataExpiringWarning() { CalibrationDataPath = path, RemainedHours = timespan.Hours };
                        }
                        else
                        {
                            CalibrationExpiredTimer.Interval = wt.TotalMilliseconds;
                            CalibrationExpiredTimer.Elapsed += (sender, e) =>
                            {
                                CalibrationExpiredTimer.Stop();
                                CalibrationExpiring?.Invoke(this, e);   // TODO, update the timer to trig expired
                            };
                        }
                    }

                    var rtn = ApplyCalibData(calibdata);
                    return rtn;
                }
                else
                {
                    throw new CalibrationDataExpiredException() { CalibrationDataPath = path };
                }
            }
            else
            {
                return 1;
            }
        }

        public string GetCalibrationBase()
        {
            return Path.Combine(ApxEngine.CalibrationBase, $"{StationConfig?.CustomerName ?? "CU"}_{StationConfig?.ProductName ?? "PRD"}_{StationConfig?.StationName ?? "STS"}");
        }

        public string GetReferenceBase()
        {
            return Path.Combine(ApxEngine.ReferenceBase, $"{StationConfig?.CustomerName ?? "CU"}_{StationConfig?.ProductName ?? "PRD"}_{StationConfig?.StationName ?? "STS"}");
        }

        public string GetVerificationBase()
        {
            return Path.Combine(ApxEngine.VerificationBase, $"{StationConfig?.CustomerName ?? "CU"}_{StationConfig?.ProductName ?? "PRD"}_{StationConfig?.StationName ?? "STS"}");
        }

        public int ApplyCalibData(ScriptCalibData calib)
        {
            var calibrationbase = GetCalibrationBase();

            var spcnt = ApxEngine.ApRef.Sequence.Count;
            for (int i = 0; i < spcnt; i++)
            {
                var sp = ApxEngine.ApRef.Sequence.GetSignalPath(i);
                var name = sp.Name;

                if (!sp.Checked) continue;

                if (calib.SignalPathCalibDatas.ContainsKey(name))
                {
                    var spcal = calib.SignalPathCalibDatas[name];

                    var meascnt = sp.Count;

                    for (int j = 0; j < meascnt; j++)
                    {
                        var meas = sp[j];

                        if (!meas.Checked) continue;

                        switch (meas.MeasurementType)
                        {
                            case MeasurementType.SignalPathSetup:
                                meas.Show();
                                var setup = ApxEngine.ApRef.SignalPathSetup;
#if AP8
                                if (setup.Measure == MeasurandType.Acoustic)
                                {
                                    if (spcal.AcousticInput is null)
                                    {
                                        throw new InvalidProgramException($"SignalPath {name} require Analog, Current Setting Acoustic");
                                    }

                                    if (setup.InputConnector.Type != spcal.AcousticInput.ConnectorType)
                                    {
                                        throw new InvalidProgramException($"SignalPath {name} InputConnect Require {spcal.AcousticInput.ConnectorType}, Current {setup.InputConnector.Type}");
                                    }

                                    //var verify = setup.References.AcousticInputReferences.
                                    //setup.References.AcousticInputReferences.CalibratorFrequency = spcal.AcousticInput.Frequency;
                                    //setup.References.AcousticInputReferences.CalibratorLevel.Text = spcal.AcousticInput.Level;
                                    //setup.References.AcousticInputReferences.CalibratorFrequencyTolerance = spcal.AcousticInput.Tolerance;
                                    setup.Channels.CalibratorFrequency = spcal.AcousticInput.Frequency;
                                    setup.Channels.CalibratorLevel.Text = spcal.AcousticInput.Level;
                                    setup.Channels.CalibratorFrequencyTolerance = spcal.AcousticInput.Tolerance;

                                    //int chcnt = Math.Min(spcal.AcousticInput.Channels.Count(), setup.References.AcousticInputReferences.Count);
                                    int chcnt = Math.Min(spcal.AcousticInput.Channels.Count(), setup.Channels.Count);
                                    for (int idx = 0; idx < chcnt; idx++)
                                    {
                                        var chcal = spcal.AcousticInput.Channels.ElementAt(idx);
                                        //setup.References.AcousticInputReferences.SetSerialNum(idx, chcal.SerialNo);
                                        //setup.References.AcousticInputReferences.SetSensitivity(idx, chcal.Sensitivity);
                                        //setup.References.AcousticInputReferences.SetExpectedSensitivity(idx, chcal.Sensitivity_Expected);
                                        //setup.References.AcousticInputReferences.SetSensitivityTolerance(idx, chcal.Sensitivity_Tolerance);
                                        setup.Channels[i].SerialNumber = chcal.SerialNo;
                                        setup.Channels[i].Sensitivity.Value = chcal.Sensitivity;
                                        setup.Channels[i].ExpectedSensitivity.Value = chcal.Sensitivity_Expected;
                                        setup.Channels[i].SensitivityTolerance.Value = chcal.Sensitivity_Tolerance;
                                    }
                                }
#else

                                if (setup.AcousticInput)
                                {
                                    if (spcal.AcousticInput is null)
                                    {
                                        throw new InvalidProgramException($"SignalPath {name} require Analog, Current Setting Acoustic");
                                    }

                                    if (setup.InputConnector.Type != spcal.AcousticInput.ConnectorType)
                                    {
                                        throw new InvalidProgramException($"SignalPath {name} InputConnect Require {spcal.AcousticInput.ConnectorType}, Current {setup.InputConnector.Type}");
                                    }

                                    //var verify = setup.References.AcousticInputReferences.
                                    setup.References.AcousticInputReferences.CalibratorFrequency = spcal.AcousticInput.Frequency;
                                    setup.References.AcousticInputReferences.CalibratorLevel.Text = spcal.AcousticInput.Level;
                                    setup.References.AcousticInputReferences.CalibratorFrequencyTolerance = spcal.AcousticInput.Tolerance;

                                    int chcnt = Math.Min(spcal.AcousticInput.Channels.Count(), setup.References.AcousticInputReferences.Count);
                                    for (int idx = 0; idx < chcnt; idx++)
                                    {
                                        var chcal = spcal.AcousticInput.Channels.ElementAt(idx);
                                        setup.References.AcousticInputReferences.SetSerialNum(idx, chcal.SerialNo);
                                        setup.References.AcousticInputReferences.SetSensitivity(idx, chcal.Sensitivity);
                                        setup.References.AcousticInputReferences.SetExpectedSensitivity(idx, chcal.Sensitivity_Expected);
                                        setup.References.AcousticInputReferences.SetSensitivityTolerance(idx, chcal.Sensitivity_Tolerance);
                                    }
                                }
#endif
                                else
                                {
                                    if (spcal.AnalogInput is null)
                                    {
                                        throw new InvalidProgramException($"SignalPath {name} require Acoustic, Current Setting Analog");
                                    }

                                    if (setup.InputConnector.Type != spcal.AnalogInput.ConnectorType)
                                    {
                                        throw new InvalidProgramException($"SignalPath {name} InputConnect Require {spcal.AnalogInput.ConnectorType}, Current {setup.InputConnector.Type}");
                                    }

                                    setup.References.AnalogInputReferences.dBrA.Text = spcal.AnalogInput.dBrA;
                                    setup.References.AnalogInputReferences.dBrAOffset.Text = spcal.AnalogInput.dBrAOffset;
                                    setup.References.AnalogInputReferences.dBrB.Text = spcal.AnalogInput.dBrB;
                                    setup.References.AnalogInputReferences.dBrBOffset.Text = spcal.AnalogInput.dBrBOffset;
                                    setup.References.AnalogInputReferences.dBSpl1.Text = spcal.AnalogInput.dBSpl1;
                                    setup.References.AnalogInputReferences.dBSpl1CalibratorLevel.Text = spcal.AnalogInput.dBSpl1CalibratorLevel;
                                    setup.References.AnalogInputReferences.dBSpl2.Text = spcal.AnalogInput.dBSpl2;
                                    setup.References.AnalogInputReferences.dBSpl2CalibratorLevel.Text = spcal.AnalogInput.dBSpl2CalibratorLevel;
                                    setup.References.AnalogInputReferences.dBm.Text = spcal.AnalogInput.dBm;
                                    setup.References.AnalogInputReferences.Watts.Text = spcal.AnalogInput.Watts;
                                }

                                if (setup.AcousticOutput)
                                {
                                    if (spcal.AcousticOutput is null)
                                    {
                                        throw new InvalidProgramException($"SignalPath {name} require Analog, Current Setting Acoustic");
                                    }

                                    if (setup.OutputConnector.Type != spcal.AcousticOutput.ConnectorType)
                                    {
                                        throw new InvalidProgramException($"SignalPath {name} OutputConnect Require {spcal.AcousticOutput.ConnectorType}, Current {setup.OutputConnector.Type}");
                                    }

                                    setup.References.AcousticOutputReferences.VoltageRatio = spcal.AcousticOutput.VoltageRatio;
                                    setup.References.AcousticOutputReferences.ReferenceFrequency = spcal.AcousticOutput.RefFreq;
                                }
                                else
                                {
                                    if (spcal.AnalogOutput is null)
                                    {
                                        throw new InvalidProgramException($"SignalPath {name} require Acoustic, Current Setting Analog");
                                    }

                                    if (setup.OutputConnector.Type != spcal.AnalogOutput.ConnectorType)
                                    {
                                        throw new InvalidProgramException($"SignalPath {name} OutputConnect Require {spcal.AnalogOutput.ConnectorType}, Current {setup.OutputConnector.Type}");
                                    }

                                    setup.References.AnalogOutputReferences.dBrG.Text = spcal.AnalogOutput.dBrG;
                                    setup.References.AnalogOutputReferences.dBm.Text = spcal.AnalogOutput.dBm;
                                    setup.References.AnalogOutputReferences.Watts.Text = spcal.AnalogOutput.Watts;
                                }

                                break;
                            case MeasurementType.FrequencyResponse:
                                if (spcal.EqTableFiles.FirstOrDefault(x => x.StepName == meas.Name) is EqTableFile eqtable_fr)
                                {
                                    if (string.IsNullOrWhiteSpace(eqtable_fr?.EqFile)) continue;
                                    meas.Show();

                                    var format = System.IO.Path.GetExtension(eqtable_fr.EqFile);
                                    ApxEngine.ApRef.FrequencyResponse.Generator.EQSettings.EQTableType = eqtable_fr.EqType;
                                    if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
                                    {
                                        ApxEngine.LoadCsvEqTable(Path.Combine(calibrationbase, eqtable_fr.EqFile), out double[] freqs, out double[] eqvals, out string unit);

                                        ApxEngine.ApRef.FrequencyResponse.Generator.EQSettings.LevelUnit = unit;
                                        ApxEngine.ApRef.FrequencyResponse.Generator.EQSettings.SetEQTable(freqs, eqvals);
                                    }
                                    else if (format.Equals("xlsx", StringComparison.OrdinalIgnoreCase))
                                    {
                                        throw new NotSupportedException();
                                    }
                                }
                                else
                                {
                                    Warn($"{meas.Name} has no EQ applied");
                                }
                                break;
                            case MeasurementType.ContinuousSweep:
                                if (spcal.EqTableFiles.FirstOrDefault(x => x.StepName == meas.Name) is EqTableFile eqtable_cs)
                                {
                                    if (string.IsNullOrWhiteSpace(eqtable_cs?.EqFile)) continue;
                                    meas.Show();

                                    var format = System.IO.Path.GetExtension(eqtable_cs.EqFile);
                                    ApxEngine.ApRef.ContinuousSweep.GeneratorWithPilot.EQSettings.EQTableType = eqtable_cs.EqType;
                                    if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
                                    {
                                        ApxEngine.LoadCsvEqTable(Path.Combine(calibrationbase, eqtable_cs.EqFile), out double[] freqs, out double[] eqvals, out string unit);

                                        ApxEngine.ApRef.ContinuousSweep.GeneratorWithPilot.EQSettings.LevelUnit = unit;
                                        ApxEngine.ApRef.ContinuousSweep.GeneratorWithPilot.EQSettings.SetEQTable(freqs, eqvals);
                                    }
                                    else
                                    {
                                        throw new NotSupportedException();
                                    }
                                }
                                else
                                {
                                    Warn($"{meas.Name} has no EQ applied");
                                }
                                break;
                            case MeasurementType.SteppedFrequencySweep:
                                if (spcal.EqTableFiles.FirstOrDefault(x => x.StepName == meas.Name) is EqTableFile eqtable_sfs)
                                {
                                    if (string.IsNullOrWhiteSpace(eqtable_sfs?.EqFile)) continue;
                                    meas.Show();

                                    var format = System.IO.Path.GetExtension(eqtable_sfs.EqFile);
                                    ApxEngine.ApRef.SteppedFrequencySweep.Generator.EQSettings.EQTableType = eqtable_sfs.EqType;
                                    if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
                                    {
                                        ApxEngine.LoadCsvEqTable(Path.Combine(calibrationbase, eqtable_sfs.EqFile), out double[] freqs, out double[] eqvals, out string unit);

                                        ApxEngine.ApRef.SteppedFrequencySweep.Generator.EQSettings.LevelUnit = unit;
                                        ApxEngine.ApRef.SteppedFrequencySweep.Generator.EQSettings.SetEQTable(freqs, eqvals);
                                    }
                                    else
                                    {
                                        throw new NotSupportedException();
                                    }
                                }
                                else
                                {
                                    Warn($"{meas.Name} has no EQ applied");
                                }
                                break;
                            case MeasurementType.AcousticResponse:
                                if (spcal.EqTableFiles.FirstOrDefault(x => x.StepName == meas.Name) is EqTableFile eqtable_ar)
                                {
                                    if (string.IsNullOrWhiteSpace(eqtable_ar?.EqFile)) continue;
                                    meas.Show();

                                    var format = System.IO.Path.GetExtension(eqtable_ar.EqFile);

                                    ApxEngine.ApRef.AcousticResponse.GeneratorWithPilot.EQSettings.EQTableType = eqtable_ar.EqType;
                                    if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
                                    {
                                        ApxEngine.LoadCsvEqTable(Path.Combine(calibrationbase, eqtable_ar.EqFile), out double[] freqs, out double[] eqvals, out string unit);

                                        ApxEngine.ApRef.AcousticResponse.GeneratorWithPilot.EQSettings.LevelUnit = unit;
                                        ApxEngine.ApRef.AcousticResponse.GeneratorWithPilot.EQSettings.SetEQTable(freqs, eqvals);
                                    }
                                    else// if (format.Equals("xlsx", StringComparison.OrdinalIgnoreCase))
                                    {
                                        throw new NotSupportedException();
                                        //ApxEngine.ApRef.AcousticResponse.GeneratorWithPilot.EQSettings.ImportData(eqtable.EqFile, "", InputChannelIndex.Ch1);
                                    }
                                }
                                else
                                {
                                    Warn($"{meas.Name} has no EQ applied");
                                }
                                break;
                            case MeasurementType.MultitoneAnalyzer:
                                if (spcal.MultitoneAnalyzer.ContainsKey(meas.Name))
                                {
                                    meas.Show();

                                    var wavfile = Path.Combine(calibrationbase, spcal.MultitoneAnalyzer[meas.Name]);

                                    do
                                    {
                                        ApxEngine.ApRef.MultitoneAnalyzer.Generator.LoadWaveformFile(wavfile, true);
                                    }
                                    while (ApxEngine.ApRef.MultitoneAnalyzer.Generator.MultitoneSignalDefinition.Name != wavfile);
                                }
                                else
                                {
                                    Warn($"{meas.Name} has no EQ applied");
                                }
                                break;

                            default:
                                break;
                        }
                    }
                }
            }

            return 1;
        }

        public ScriptCalibData AnalyzeCalibration()
        {
            ScriptCalibData scd = new ScriptCalibData();
            var spcnt = ApxEngine.ApRef.Sequence.Count;
            for (int i = 0; i < spcnt; i++)
            {
                var sp = ApxEngine.ApRef.Sequence.GetSignalPath(i);
                var name = sp.Name;

                var meascnt = sp.Count;
                if (!sp.Checked) continue;

                SignalPathCalibData calib = new SignalPathCalibData();

                List<EqTableFile> tempeqtables = new List<EqTableFile>();
                List<Calib_ImpedanceThieleSmall> tempts = new List<Calib_ImpedanceThieleSmall>();
                List<Calib_LoudspeakerProductionTest> templpt = new List<Calib_LoudspeakerProductionTest>();
                for (int j = 0; j < meascnt; j++)
                {
                    var meas = sp[j];
                    if (!meas.Checked) continue;
                    switch (meas.MeasurementType)
                    {
                        case MeasurementType.SignalPathSetup:
                            meas.Show();
                            var setup = ApxEngine.ApRef.SignalPathSetup;
                            if (setup.AcousticOutput)
                            {
                                calib.AcousticOutput = new Calib_AcousticOutput()
                                {
                                    RefFreq = setup.References.AcousticOutputReferences.ReferenceFrequency,
                                    VoltageRatio = setup.References.AcousticOutputReferences.VoltageRatio,
                                    ConnectorType = setup.OutputConnector.Type,
                                };
                            }
                            else
                            {
                                calib.AnalogOutput = new Calib_AnalogOutput()
                                {
                                    dBm = setup.References.AnalogOutputReferences.dBm.Text,
                                    dBrG = setup.References.AnalogOutputReferences.dBrG.Text,
                                    Watts = setup.References.AnalogOutputReferences.Watts.Text,
                                    ConnectorType = setup.OutputConnector.Type,
                                };
                            }
#if AP8
                            if (setup.Measure == MeasurandType.Acoustic)
                            {
                                calib.AcousticInput = new Calib_AcousticInput()
                                {
                                    ConnectorType = setup.InputConnector.Type,
                                    //Level = setup.References.AcousticInputReferences.CalibratorLevel.Text,
                                    //Frequency = setup.References.AcousticInputReferences.CalibratorFrequency,
                                    //Tolerance = setup.References.AcousticInputReferences.CalibratorFrequencyTolerance,
                                    Level = setup.Channels.CalibratorLevel.Text,
                                    Frequency = setup.Channels.CalibratorFrequency,
                                    Tolerance = setup.Channels.CalibratorFrequencyTolerance,
                                };

                                //var chs = new Calib_AcousticInputChannel[setup.References.AcousticInputReferences.Count];
                                var chs = new Calib_AcousticInputChannel[setup.Channels.Count];
                                for (int idx = 0; idx < chs.Length; idx++)
                                {
                                    chs[idx] = new Calib_AcousticInputChannel()
                                    {
                                        Index = idx,
                                        //Sensitivity = setup.References.AcousticInputReferences.GetSensitivity(idx),
                                        //Sensitivity_Expected = setup.References.AcousticInputReferences.GetSensitivityTolerance(idx),
                                        //Sensitivity_Tolerance = setup.References.AcousticInputReferences.GetSensitivityTolerance(idx),
                                        //SerialNo = setup.References.AcousticInputReferences.GetSerialNum(idx),
                                        Sensitivity = setup.Channels[idx].Sensitivity.Value,
                                        Sensitivity_Expected = setup.Channels[idx].ExpectedSensitivity.Value,
                                        Sensitivity_Tolerance = setup.Channels[idx].SensitivityTolerance.Value,
                                        SerialNo = setup.Channels[idx].SerialNumber,
                                    };
                                }

                                calib.AcousticInput.Channels = chs.ToList();
                            }
#else
                            if (setup.AcousticInput)
                            {
                                calib.AcousticInput = new Calib_AcousticInput()
                                {
                                    ConnectorType = setup.InputConnector.Type,
                                    Level = setup.References.AcousticInputReferences.CalibratorLevel.Text,
                                    Frequency = setup.References.AcousticInputReferences.CalibratorFrequency,
                                    Tolerance = setup.References.AcousticInputReferences.CalibratorFrequencyTolerance,                                    
                                };

                                var chs = new Calib_AcousticInputChannel[setup.References.AcousticInputReferences.Count];
                                for (int idx = 0; idx < chs.Length; idx++)
                                {
                                    chs[idx] = new Calib_AcousticInputChannel()
                                    {
                                        Index = idx,
                                        Sensitivity = setup.References.AcousticInputReferences.GetSensitivity(idx),
                                        Sensitivity_Expected = setup.References.AcousticInputReferences.GetSensitivityTolerance(idx),
                                        Sensitivity_Tolerance = setup.References.AcousticInputReferences.GetSensitivityTolerance(idx),
                                        SerialNo = setup.References.AcousticInputReferences.GetSerialNum(idx),
                                    };
                                }

                                calib.AcousticInput.Channels = chs.ToList();
                            }
#endif
                            else
                            {
                                calib.AnalogInput = new Calib_AnalogInput()
                                {
                                    ConnectorType = setup.InputConnector.Type,
                                    dBrA = setup.References.AnalogInputReferences.dBrA.Text,
                                    dBrAOffset = setup.References.AnalogInputReferences.dBrAOffset.Text,
                                    dBrB = setup.References.AnalogInputReferences.dBrB.Text,
                                    dBrBOffset = setup.References.AnalogInputReferences.dBrBOffset.Text,
                                    dBSpl1 = setup.References.AnalogInputReferences.dBSpl1.Text,
                                    dBSpl1CalibratorLevel = setup.References.AnalogInputReferences.dBSpl1CalibratorLevel.Text,
                                    dBSpl2 = setup.References.AnalogInputReferences.dBSpl2.Text,
                                    dBSpl2CalibratorLevel = setup.References.AnalogInputReferences.dBSpl2CalibratorLevel.Text,
                                    dBm = setup.References.AnalogInputReferences.dBm.Text,
                                    Watts = setup.References.AnalogInputReferences.Watts.Text,
                                };
                            }
                            break;
                        case MeasurementType.LoudspeakerProductionTest:
                            meas.Show();
                            //var lspt = meas as AudioPrecision.API.ILoudspeakerProductionTestMeasurement;
                            var lspt = ApxEngine.ApRef.LoudspeakerProductionTest;
#if AP8
                            Calib_LoudspeakerProductionTest calib_lspt = new Calib_LoudspeakerProductionTest()
                            {
                                SenseR = lspt.ExternalSenseResistance,
                                ModelFit = lspt.ModelFit,
                                AmplifierGain = lspt.AmplifierGain.Text,
                                StepName = meas.Name
                            };

                            if(lspt.Measure == LoudspeakerTestMeasurementType.VdrvrAndVsense)
                            {
                                calib_lspt.TestConfiguration = LoudspeakerTestConfiguration.External2Ch;
                            }
                            else
                            {
                                calib_lspt.TestConfiguration = LoudspeakerTestConfiguration.External1Ch;
                            }
                            

                            if (calib_lspt.TestConfiguration == LoudspeakerTestConfiguration.External1Ch)
                            {
                                calib_lspt.Channel = lspt.VsenseChannel;
                            }
                            else
                            {
                                calib_lspt.Channel = lspt.VdrvrChannel;
                            }
#else
                            Calib_LoudspeakerProductionTest calib_lspt = new Calib_LoudspeakerProductionTest()
                            {
                                TestConfiguration = lspt.TestConfiguration,
                                SenseR = lspt.ExternalSenseResistance,
                                ModelFit = lspt.ModelFit,
                                AmplifierGain = lspt.AmplifierGain.Text,
                                StepName = meas.Name
                            };

                            if (lspt.TestConfiguration == LoudspeakerTestConfiguration.External1Ch)
                            {
                                calib_lspt.Channel = lspt.ExternalSenseResistorChannel;
                            }
                            else
                            {
                                calib_lspt.Channel = lspt.PrimaryChannel;
                            }
#endif

                            templpt.Add(calib_lspt);
                            break;
                        case MeasurementType.ImpedanceThieleSmall:
                            meas.Show();
                            //var ts = meas as AudioPrecision.API.IImpedanceThieleSmallMeasurement;
                            var ts = ApxEngine.ApRef.ImpedanceThieleSmall;
#if AP8
                            var calib_ts = new Calib_ImpedanceThieleSmall()
                            {
                                SenseR = ts.ExternalSenseResistance,
                                ModelFit = ts.ModelFit,
                                AmplifierGain = ts.AmplifierGain.Text,
                                //Channel = ts.PrimaryChannel,
                                //ExternalSenseResistorChannel = ts.ExternalSenseResistorChannel,
                                StepName = meas.Name,
                            };

                            if(ts.Measure == ImpedanceMeasurementType.VdrvrAndVsense)
                            {
                                calib_ts.TestConfiguration = ImpedanceConfiguration.External2Ch;
                            }
                            else
                            {
                                calib_ts.TestConfiguration = ImpedanceConfiguration.External1Ch;
                            }

                            if (calib_ts.TestConfiguration == ImpedanceConfiguration.External1Ch)
                            {
                                calib_ts.Channel = ts.VsenseChannel;
                            }
                            else
                            {
                                calib_ts.Channel = ts.VdrvrChannel;
                            }
#else
                            var calib_ts = new Calib_ImpedanceThieleSmall()
                            {
                                TestConfiguration = ts.TestConfiguration,
                                SenseR = ts.ExternalSenseResistance,
                                ModelFit = ts.ModelFit,
                                AmplifierGain = ts.AmplifierGain.Text,
                                //Channel = ts.PrimaryChannel,
                                //ExternalSenseResistorChannel = ts.ExternalSenseResistorChannel,
                                StepName = meas.Name,
                            };

                            if (ts.TestConfiguration == ImpedanceConfiguration.External1Ch)
                            {
                                calib_ts.Channel = ts.ExternalSenseResistorChannel;
                            }
                            else
                            {
                                calib_ts.Channel = ts.PrimaryChannel;
                            }
#endif

                            tempts.Add(calib_ts);
                            break;
                        case MeasurementType.FrequencyResponse:
                        case MeasurementType.ContinuousSweep:
                        case MeasurementType.SteppedFrequencySweep:
                        case MeasurementType.AcousticResponse:
                            tempeqtables.Add(new EqTableFile() { StepName = meas.Name });
                            break;
                        case MeasurementType.MultitoneAnalyzer:
                            calib.MultitoneAnalyzer.Add(meas.Name, null);
                            break;

                        default:
                            break;
                    }
                }

                if (tempeqtables.Count > 0) calib.EqTableFiles = tempeqtables.ToArray();
                if (tempts.Count > 0) calib.ImpedanceThieleSmalls = tempts.ToArray();
                if (templpt.Count > 0) calib.LoudspeakerProductionTests = templpt.ToArray();

                scd.SignalPathCalibDatas.Add(name, calib);
            }

            return scd;
        }

        public int ApplyCalibration(ScriptCalibData calib)
        {
            var spcnt = ApxEngine.ApRef.Sequence.Count;
            for (int i = 0; i < spcnt; i++)
            {
                var sp = ApxEngine.ApRef.Sequence.GetSignalPath(i);
                var name = sp.Name;

                if (!sp.Checked) continue;

                if (calib.SignalPathCalibDatas.ContainsKey(name))
                {
                    var spcal = calib.SignalPathCalibDatas[name];

                    var meascnt = sp.Count;

                    for (int j = 0; j < meascnt; j++)
                    {
                        var meas = sp[j];

                        if (!meas.Checked) continue;

                        switch (meas.MeasurementType)
                        {
                            case MeasurementType.SignalPathSetup:
                                meas.Show();
                                var setup = ApxEngine.ApRef.SignalPathSetup;
#if AP8
                                if (setup.Measure == MeasurandType.Acoustic)
                                {
                                    if (spcal.AcousticInput is null)
                                    {
                                        throw new InvalidProgramException($"SignalPath {name} require Analog, Current Setting Acoustic");
                                    }

                                    if (setup.InputConnector.Type != spcal.AcousticInput.ConnectorType)
                                    {
                                        throw new InvalidProgramException($"SignalPath {name} InputConnect Require {spcal.AcousticInput.ConnectorType}, Current {setup.InputConnector.Type}");
                                    }

                                    setup.Channels.CalibratorFrequency = spcal.AcousticInput.Frequency;
                                    setup.Channels.CalibratorLevel.Text = spcal.AcousticInput.Level;
                                    setup.Channels.CalibratorFrequencyTolerance = spcal.AcousticInput.Tolerance;

                                    int chcnt = Math.Min(spcal.AcousticInput.Channels.Count(), setup.Channels.Count);
                                    for (int idx = 0; idx < chcnt; idx++)
                                    {
                                        var chcal = spcal.AcousticInput.Channels.ElementAt(i);
                                        setup.Channels[idx].SerialNumber = chcal.SerialNo;
                                        setup.Channels[idx].Sensitivity.Value = chcal.Sensitivity;
                                        setup.Channels[idx].ExpectedSensitivity.Value = chcal.Sensitivity_Expected;
                                        setup.Channels[idx].SensitivityTolerance.Value = chcal.Sensitivity_Tolerance;
                                    }
                                }
#else
                                if (setup.AcousticInput)
                                {
                                    if (spcal.AcousticInput is null)
                                    {
                                        throw new InvalidProgramException($"SignalPath {name} require Analog, Current Setting Acoustic");
                                    }

                                    if (setup.InputConnector.Type != spcal.AcousticInput.ConnectorType)
                                    {
                                        throw new InvalidProgramException($"SignalPath {name} InputConnect Require {spcal.AcousticInput.ConnectorType}, Current {setup.InputConnector.Type}");
                                    }

                                    //var verify = setup.References.AcousticInputReferences.
                                    setup.References.AcousticInputReferences.CalibratorFrequency = spcal.AcousticInput.Frequency;
                                    setup.References.AcousticInputReferences.CalibratorLevel.Text = spcal.AcousticInput.Level;
                                    setup.References.AcousticInputReferences.CalibratorFrequencyTolerance = spcal.AcousticInput.Tolerance;

                                    int chcnt = Math.Min(spcal.AcousticInput.Channels.Count(), setup.References.AcousticInputReferences.Count);
                                    for (int idx = 0; idx < chcnt; idx++)
                                    {
                                        var chcal = spcal.AcousticInput.Channels.ElementAt(i);
                                        setup.References.AcousticInputReferences.SetSerialNum(idx, chcal.SerialNo);
                                        setup.References.AcousticInputReferences.SetSensitivity(idx, chcal.Sensitivity);
                                        setup.References.AcousticInputReferences.SetExpectedSensitivity(idx, chcal.Sensitivity_Expected);
                                        setup.References.AcousticInputReferences.SetSensitivityTolerance(idx, chcal.Sensitivity_Tolerance);
                                    }
                                }
#endif
                                else
                                {
                                    if (spcal.AnalogInput is null)
                                    {
                                        throw new InvalidProgramException($"SignalPath {name} require Acoustic, Current Setting Analog");
                                    }

                                    if (setup.InputConnector.Type != spcal.AnalogInput.ConnectorType)
                                    {
                                        throw new InvalidProgramException($"SignalPath {name} InputConnect Require {spcal.AnalogInput.ConnectorType}, Current {setup.InputConnector.Type}");
                                    }

                                    setup.References.AnalogInputReferences.dBrA.Text = spcal.AnalogInput.dBrA;
                                    setup.References.AnalogInputReferences.dBrAOffset.Text = spcal.AnalogInput.dBrAOffset;
                                    setup.References.AnalogInputReferences.dBrB.Text = spcal.AnalogInput.dBrB;
                                    setup.References.AnalogInputReferences.dBrBOffset.Text = spcal.AnalogInput.dBrBOffset;
                                    setup.References.AnalogInputReferences.dBSpl1.Text = spcal.AnalogInput.dBSpl1;
                                    setup.References.AnalogInputReferences.dBSpl1CalibratorLevel.Text = spcal.AnalogInput.dBSpl1CalibratorLevel;
                                    setup.References.AnalogInputReferences.dBSpl2.Text = spcal.AnalogInput.dBSpl2;
                                    setup.References.AnalogInputReferences.dBSpl2CalibratorLevel.Text = spcal.AnalogInput.dBSpl2CalibratorLevel;
                                    setup.References.AnalogInputReferences.dBm.Text = spcal.AnalogInput.dBm;
                                    setup.References.AnalogInputReferences.Watts.Text = spcal.AnalogInput.Watts;
                                }

                                if (setup.AcousticOutput)
                                {
                                    if (spcal.AcousticOutput is null)
                                    {
                                        throw new InvalidProgramException($"SignalPath {name} require Analog, Current Setting Acoustic");
                                    }

                                    if (setup.OutputConnector.Type != spcal.AcousticOutput.ConnectorType)
                                    {
                                        throw new InvalidProgramException($"SignalPath {name} OutputConnect Require {spcal.AcousticOutput.ConnectorType}, Current {setup.OutputConnector.Type}");
                                    }

                                    setup.References.AcousticOutputReferences.VoltageRatio = spcal.AcousticOutput.VoltageRatio;
                                    setup.References.AcousticOutputReferences.ReferenceFrequency = spcal.AcousticOutput.RefFreq;
                                }
                                else
                                {
                                    if (spcal.AnalogOutput is null)
                                    {
                                        throw new InvalidProgramException($"SignalPath {name} require Acoustic, Current Setting Analog");
                                    }

                                    if (setup.OutputConnector.Type != spcal.AnalogOutput.ConnectorType)
                                    {
                                        throw new InvalidProgramException($"SignalPath {name} OutputConnect Require {spcal.AnalogOutput.ConnectorType}, Current {setup.OutputConnector.Type}");
                                    }

                                    setup.References.AnalogOutputReferences.dBrG.Text = spcal.AnalogOutput.dBrG;
                                    setup.References.AnalogOutputReferences.dBm.Text = spcal.AnalogOutput.dBm;
                                    setup.References.AnalogOutputReferences.Watts.Text = spcal.AnalogOutput.Watts;
                                }

                                break;
                            case MeasurementType.FrequencyResponse:
                                if (spcal.EqTableFiles.FirstOrDefault(x => x.StepName == meas.Name) is EqTableFile eqtable_fr)
                                {
                                    if (string.IsNullOrWhiteSpace(eqtable_fr?.EqFile)) continue;
                                    meas.Show();

                                    var format = System.IO.Path.GetExtension(eqtable_fr.EqFile);
                                    ApxEngine.ApRef.FrequencyResponse.Generator.EQSettings.EQTableType = eqtable_fr.EqType;
                                    if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
                                    { }
                                    else if (format.Equals("xlsx", StringComparison.OrdinalIgnoreCase))
                                    {
                                        throw new NotSupportedException();
                                    }
                                }
                                else
                                {
                                    Warn($"{meas.Name} has no EQ applied");
                                }
                                break;
                            case MeasurementType.ContinuousSweep:
                                if (spcal.EqTableFiles.FirstOrDefault(x => x.StepName == meas.Name) is EqTableFile eqtable_cs)
                                {
                                    if (string.IsNullOrWhiteSpace(eqtable_cs?.EqFile)) continue;
                                    meas.Show();

                                    var format = System.IO.Path.GetExtension(eqtable_cs.EqFile);
                                    ApxEngine.ApRef.ContinuousSweep.GeneratorWithPilot.EQSettings.EQTableType = eqtable_cs.EqType;
                                    if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
                                    { }
                                    else
                                    {
                                        throw new NotSupportedException();
                                    }
                                }
                                else
                                {
                                    Warn($"{meas.Name} has no EQ applied");
                                }
                                break;
                            case MeasurementType.SteppedFrequencySweep:
                                if (spcal.EqTableFiles.FirstOrDefault(x => x.StepName == meas.Name) is EqTableFile eqtable_sfs)
                                {
                                    if (string.IsNullOrWhiteSpace(eqtable_sfs?.EqFile)) continue;
                                    meas.Show();

                                    var format = System.IO.Path.GetExtension(eqtable_sfs.EqFile);
                                    ApxEngine.ApRef.SteppedFrequencySweep.Generator.EQSettings.EQTableType = eqtable_sfs.EqType;
                                    if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
                                    { }
                                    else
                                    {
                                        throw new NotSupportedException();
                                    }
                                }
                                else
                                {
                                    Warn($"{meas.Name} has no EQ applied");
                                }
                                break;
                            case MeasurementType.AcousticResponse:
                                if (spcal.EqTableFiles.FirstOrDefault(x => x.StepName == meas.Name) is EqTableFile eqtable_ar)
                                {
                                    if (string.IsNullOrWhiteSpace(eqtable_ar?.EqFile)) continue;
                                    meas.Show();

                                    var format = System.IO.Path.GetExtension(eqtable_ar.EqFile);

                                    ApxEngine.ApRef.AcousticResponse.GeneratorWithPilot.EQSettings.EQTableType = eqtable_ar.EqType;
                                    if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
                                    {
                                        ApxEngine.LoadCsvEqTable(eqtable_ar.EqFile, out double[] freqs, out double[] eqvals, out string unit);

                                        ApxEngine.ApRef.AcousticResponse.GeneratorWithPilot.EQSettings.LevelUnit = unit;
                                        ApxEngine.ApRef.AcousticResponse.GeneratorWithPilot.EQSettings.SetEQTable(freqs, eqvals);
                                    }
                                    else// if (format.Equals("xlsx", StringComparison.OrdinalIgnoreCase))
                                    {
                                        throw new NotSupportedException();
                                        //ApRef.AcousticResponse.GeneratorWithPilot.EQSettings.ImportData(eqtable.EqFile, "", InputChannelIndex.Ch1);
                                    }
                                }
                                else
                                {
                                    Warn($"{meas.Name} has no EQ applied");
                                }
                                break;
                            case MeasurementType.MultitoneAnalyzer:
                                if (spcal.MultitoneAnalyzer.ContainsKey(meas.Name))
                                {
                                    meas.Show();

                                    var wavfile = spcal.MultitoneAnalyzer[meas.Name];

                                    do
                                    {
                                        ApxEngine.ApRef.MultitoneAnalyzer.Generator.LoadWaveformFile(wavfile, true);
                                    }
                                    while (ApxEngine.ApRef.MultitoneAnalyzer.Generator.MultitoneSignalDefinition.Name != wavfile);
                                }
                                else
                                {
                                    Warn($"{meas.Name} has no EQ applied");
                                }
                                break;

                            default:
                                break;
                        }
                    }
                }
            }

            return 1;
        }

        Timer ReferenceExpiredTimer;
        public int ApplyReference()
        {
            if (SystemConfig?.General.ReferencePeriod > 0)
            {
                var refbase = GetReferenceBase();

                if (!Directory.Exists(refbase)) throw new ReferenceDataExpiredException($"No Reference Data found, Please Run Reference");

                double dayelapsed = 0;
                for (int i = 0; i < SystemConfig.General.SocketCount; i++)
                {
                    var refpath = System.IO.Path.Combine(refbase, i.ToString());

                    if (!Directory.Exists(refpath)) throw new ReferenceDataExpiredException($"No Reference Data found, Please Run Reference");
                    
                    var files = Directory.GetFiles(refpath);

                    if (files.Length <= 0) throw new ReferenceDataExpiredException($"No Reference Data in Slot {i} found, Please Run Reference");
                    
                    FileInfo fi = new FileInfo(files[0]);
                    dayelapsed = Math.Max(DateTime.Now.Subtract(fi.LastWriteTime).TotalDays, dayelapsed);
                }

                var timespan = TimeSpan.FromDays(SystemConfig.General.ReferencePeriod).Subtract(TimeSpan.FromDays(dayelapsed));  // For trig expired event
                var wt = timespan.Subtract(TimeSpan.FromDays(SystemConfig.General.ReferenceWarning));   // for trig expiring event

                if (timespan.TotalMilliseconds <= 0)
                {
                    throw new ReferenceDataExpiredException($"Reference Data expired, Please Run Reference") { ReferenceDataPath = refbase };
                }
                else
                {
                    if (ReferenceExpiredTimer != null)
                    {
                        ReferenceExpiredTimer.Stop();
                        ReferenceExpiredTimer.Close();
                        ReferenceExpiredTimer.Dispose();
                    }

                    ReferenceExpiredTimer = new Timer();

                    if (wt.TotalMilliseconds < 0)
                    {
                        ReferenceExpiredTimer.Interval = timespan.TotalMilliseconds;
                        ReferenceExpiredTimer.Elapsed += (sender, e) =>
                        {
                            ReferenceExpiredTimer.Stop();
                            ReferenceExpired?.Invoke(this, e);
                        };
                        throw new ReferenceDataExpiringWarning() { ReferenceDataPath = refbase, RemainedHours = timespan.Hours };
                    }
                    else
                    {
                        ReferenceExpiredTimer.Interval = wt.TotalMilliseconds;
                        ReferenceExpiredTimer.Elapsed += (sender, e) =>
                        {
                            ReferenceExpiredTimer.Stop();
                            ReferenceExpiring?.Invoke(this, e);   // TODO, update the timer to trig expired
                        };
                    }
                }
            }

            return 1;
        }

        Timer VerificationExpiredTimer;
        public int ApplyVerification()
        {
            if (SystemConfig?.General.VerificationPeriod > 0)
            {
                var verbase = GetVerificationBase();

                if (!Directory.Exists(verbase)) throw new VerificationDataExpiredException($"No Verification record found, Please Run Verification");

                double dayelapsed = 0;
                for (int i = 0; i < SystemConfig.General.SocketCount; i++)
                {
                    var verpath = System.IO.Path.Combine(verbase, i.ToString());

                    if (!Directory.Exists(verpath)) throw new VerificationDataExpiredException($"No Verification record found, Please Run Verification");

                    var files = Directory.GetFiles(verpath);

                    if (files.Length <= 0) throw new VerificationDataExpiredException($"No Verification record in Slot {i} found, Please Run Verification");

                    FileInfo fi = new FileInfo(files[0]);
                    dayelapsed = Math.Max(DateTime.Now.Subtract(fi.LastWriteTime).TotalDays, dayelapsed);
                }

                var timespan = TimeSpan.FromDays(SystemConfig.General.VerificationPeriod).Subtract(TimeSpan.FromDays(dayelapsed));  // For trig expired event
                var wt = timespan.Subtract(TimeSpan.FromDays(SystemConfig.General.VerificationWarning));   // for trig expiring event

                if (timespan.TotalMilliseconds <= 0)
                {
                    throw new VerificationDataExpiredException("Verification Data has been expired, Please Run Verification") { VerificationDataPath = verbase };
                }
                else
                {
                    if (VerificationExpiredTimer != null)
                    {
                        VerificationExpiredTimer.Stop();
                        VerificationExpiredTimer.Close();
                        VerificationExpiredTimer.Dispose();
                    }

                    VerificationExpiredTimer = new Timer();

                    if (wt.TotalMilliseconds < 0)
                    {
                        VerificationExpiredTimer.Interval = timespan.TotalMilliseconds;
                        VerificationExpiredTimer.Elapsed += (sender, e) =>
                        {
                            VerificationExpiredTimer.Stop();
                            VerificationExpired?.Invoke(this, e);
                        };
                        throw new VerificationDataExpiringWarning() { VerificationDataPath = verbase, RemainedHours = timespan.Hours };
                    }
                    else
                    {
                        VerificationExpiredTimer.Interval = wt.TotalMilliseconds;
                        VerificationExpiredTimer.Elapsed += (sender, e) =>
                        {
                            VerificationExpiredTimer.Stop();
                            VerificationExpiring?.Invoke(this, e);   // TODO, update the timer to trig expired
                        };
                    }
                }
            }

            return 1;
        }


        public static Script NewScript(string author, GlobalConfiguration systemconfig)
        {
            Script script = new Script();
            ApxEngine.ApRef.CreateNewProject();
            ApxEngine.ApRef.Sequence[0].Name = "TYM";
            var sequence = new Sequence(ApxEngine.ApRef.Sequence.Sequences.ActiveSequence);
            script._Sequences.Add(sequence);

            ApxEngine.ApRef.Sequence.Sequences.Add(ApxEngine.ReferenceSequenceName);
            var refsequence = new Sequence(ApxEngine.ApRef.Sequence.Sequences.ActiveSequence);
            script._Sequences.Add(refsequence);
            ApxEngine.ApRef.Sequence.Sequences.Add(ApxEngine.VerificationSequenceName);
            var versequence = new Sequence(ApxEngine.ApRef.Sequence.Sequences.ActiveSequence);
            script._Sequences.Add(versequence);

            ApxEngine.ApRef.Sequence.Sequences[0].Activate();
            script.ActiveSequence = sequence;

            script.Author = author;
            script.Time = DateTime.Now;
            script.Version = "0.0.0.0";

            script.SystemConfig = systemconfig;
            script.StationConfig = systemconfig.Station;
            script.SFCsConfig = systemconfig.SFCs;

            script.InitVariables();

            ApxEngine.ApRef.Sequence.Report.Checked = false;

            return script;
        }
    }
}
