using NationalInstruments.TestStand.Interop.API;
using NationalInstruments.TestStand.Interop.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using TestCore;
using TestCore.Configuration;
using TestCore.Data;
using TestCore.Services;
using ToucanCore.Abstraction.Configuration;
using ToucanCore.Abstraction.Engine;

namespace TsEngine
{
    public class Script : TF_Base, IScript
    {
        internal static TestStandEngine StaticEngine { get; set; }
        public NationalInstruments.TestStand.Interop.API.SequenceFile _SequenceFile { get; private set; }
        public ISequence this[string name] => _Sequences.First(x=>x.Name == name);

        public ISequence this[int index] => _Sequences[index];

        public int Id { get; set; }

        public string Name { get; private set; }

        public string Version { get; private set; }

        public DateTime Time { get; private set; }

        public string Author { get; set; }
        public string Note { get; set; }

        public string PartNo { get; private set; }

        public string CheckValue { get; private set; }

        //public IReadOnlyList<string> ReferencesLib { get; private set; }
        //public IList<Variable> Variables { get; } = new List<Variable>();
        public string BaseDirectory { get; set; }
        public string FilePath { get; set; }
        public GlobalConfiguration SystemConfig { get; set; }
        //public HardwareConfig HardwareConfig { get; set; }

        public StationConfig StationConfig { get; set; }
        public SFCsConfig SFCsConfig { get; set; }

        public TF_Spec Spec { get; set; }
        public TF_Spec GoldenSampleSpec { get; set; }

        private List<ISequence> _Sequences = new List<ISequence>();
        public IReadOnlyCollection<ISequence> Sequences { get => _Sequences; } 

        public ISequence ActiveSequence { get; set; }

        string LOCK = LOCK_1;

        const string LOCK_1 = "PteAdmin";
        const string LOCK_2 = "tympte";

        public bool IsOriginalModel { get; private set; } = false;
        public bool LockStatus 
        {
            get
            {
                if (_SequenceFile.AsPropertyObjectFile() is PropertyObjectFile pof)
                {
                    return pof.Locked;
                }
                else
                {
                    return false;
                }
            }

            set
            {
                if(_SequenceFile.AsPropertyObjectFile() is PropertyObjectFile pof)
                {
                    try
                    {
                        if (value)
                        {
                            pof.Lock(LOCK);
                        }
                        else
                        {
                            pof.Unlock(LOCK);
                        }
                    }
                    catch
                    {
                        try
                        {
                            if (value)
                            {
                                pof.Lock(LOCK_2);
                            }
                            else
                            {
                                pof.Unlock(LOCK_2);
                            }

                            LOCK = LOCK_2;
                        }
                        catch
                        { 
                        }
                    }
                }
            }
        }

        public bool IsModified { get { return _SequenceFile.ChangeCount > 0; } }

        //public IReadOnlyCollection<string> GoldenSamples
        //{
        //    get
        //    {
        //        if (_GoldenSamples is null)
        //        {
        //            _GoldenSamples = ScriptUtilities.ReadTextLineAsList(Path.Combine(GetReferenceBase(), "GoldenSamples.txt"));
        //        }
        //        return _GoldenSamples;
        //    }
        //}

        public InjectedVariableTable InjectedVariableTable { get; set; }

        private List<string> _GoldenSamples;

        //public void UpdateGoldenSamples(IEnumerable<string> samples)
        //{
        //    var file = Path.Combine(GetReferenceBase(), "GoldenSamples.txt");
        //    _GoldenSamples = samples?.ToList();
        //    ScriptUtilities.SaveEnumerableTextByLine(file, samples);
        //}

        //public int Analyze()
        //{
        //    //var d = _SequenceFile.NumSequences;
        //    //_SequenceFile.GetSequence(0).Type = NationalInstruments.TestStand.Interop.API.SequenceTypes.SeqType_Normal;

        //    var configfile = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(FilePath), GlobalConfiguration.DefaultFileName);
            
        //    if (SystemConfig!= null)
        //    {
        //        var seqspec = AnalyzeSpec();
        //        if (SystemConfig.General.RestrictLimit)
        //        {
        //            var spec = Path.Combine(Directory.GetParent(FilePath).FullName, TF_Spec.DefaultFileName);
        //            if (File.Exists(spec))
        //            {
        //                Spec = TF_Spec.LoadFromXml(spec);
        //            }
        //            else
        //            {
        //                throw new SpecFileNotFoundException($"Spec file not found. Please contact with Engineer to generate one") { Script = this };
        //            }
        //        }
        //        else
        //        {
        //            Spec = seqspec;
        //        }
        //    }
        //    else
        //    {
        //        SystemConfig = GlobalConfiguration.Default;
        //        IsOriginalModel = true;
        //    }

        //    //var rs = new TF_Result(Spec);
        //    //rs.TestGuiVersion = System.Windows.Application.ResourceAssembly?.GetName()?.Version?.ToString() ?? System.Windows.Forms.Application.ProductVersion;
        //    //rs.TestSoftwareVersion = Version;

        //    return 1;
        //}

        public TF_Spec AnalyzeSpec() => AnalyzeSpec("MainSequence");

        public TF_Spec AnalyzeSpec(string seqname)
        {
            try
            {
                TF_Spec spec = new TF_Spec("TYM", _SequenceFile.AsPropertyObjectFile().Version)
                {
                    Author = "Joey",//AuthService.UserName;
                    Time = DateTime.Now,
                    Note = "Auto Generate By Toucan",
                };

                var seq = _SequenceFile.GetSequenceByName(seqname);

                TestStandHelper.SeqToData(_SequenceFile, seq, spec.Limit, true);

                List<KeyValuePair<string, TestCore.Data.StepFormatError>> errors = new List<KeyValuePair<string, TestCore.Data.StepFormatError>>();
                spec.Limit.VerifyLimit(errors);

                if (!IsOriginalModel)
                {
                    if (errors.Count > 0)
                    {
                        StringBuilder sb = new StringBuilder();

                        foreach (var error in errors)
                        {
                            sb.AppendLine(string.Format("{0}\t{1, 24}", error.Key, error.Value));
                        }

                        var msg = sb.ToString();

                        MessageBox.Show(msg, "Illegal Script", MessageBoxButton.OK, MessageBoxImage.Error);
                        throw new InvalidDataException("Illegal Script");
                    }
                }

                return spec;
            }
            catch (Exception ex)
            {
                //PersistentSpec = null;
                Warn(ex);
                throw ex;  // throw it for notify the asyn caller
            }
        }

        public object Clone()
        {
            throw new NotImplementedException();
        }

        public int Open(string path)
        {
            _SequenceFile = TestStandHelper.Engine.GetSequenceFileEx(path);
            FilePath = path;

            Version = _SequenceFile.AsPropertyObjectFile().Version;

            try
            {
                _SequenceFile = TestStandHelper.Engine.GetSequenceFileEx(path);
                FilePath = path;
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                Regex re = new Regex(@"Change\s*the\s*Station\s*Model\s*to\s*'(.+)'");

                var match = re.Match(ex.Message);

                if (match.Success)
                {
                    Warn("Model Setting Exception. Change as Seq Required");

                    StaticEngine.SetModulePath(match.Groups[1].Value);
                    _SequenceFile = TestStandHelper.Engine.GetSequenceFileEx(path);
                }
                else
                {
                    Error(ex);
                    throw ex;
                }
            }
            TestStandHelper.TS_SeqFileViewMgr.SequenceFile = _SequenceFile;

            //var props = _SequenceFile.FileGlobalsDefaultValues.GetSubProperties("", 0);
            //foreach(var prop in props)
            //{
            //    switch (prop.Type.ValueType)
            //    {
            //        case PropertyValueTypes.PropValType_String:
            //            Variables.Add(new Variable() { Name = prop.Name, Type = "String", Value = prop.GetValString("", 0) });
            //            break;
            //        case PropertyValueTypes.PropValType_Number:
            //            Variables.Add(new Variable() { Name = prop.Name, Type = "Double", Value = prop.GetValNumber("", 0) });
            //            break;
            //        case PropertyValueTypes.PropValType_Boolean:
            //            Variables.Add(new Variable() { Name = prop.Name, Type = "Boolean", Value = prop.GetValBoolean("", 0) });
            //            break;
            //    }
            //}

            Name = Path.GetFileNameWithoutExtension(path);
            BaseDirectory = Directory.GetParent(path).FullName;
            //var configpath = Path.Combine(BaseDirectory, GlobalConfiguration.DefaultFileName);
            //var hwconfigpath = Path.Combine(BaseDirectory, HardwareConfig.DefaultFileName);
            //if (File.Exists(hwconfigpath))
            //{
            //    HardwareConfig = HardwareConfig.Load(hwconfigpath);
            //}

            //var seqspec = AnalyzeSpec();
            //if (File.Exists(configpath))
            //{
            //    SystemConfig = GlobalConfiguration.Load(configpath);
            //    ScriptUtilities.ApplyScriptConfig(this, seqspec, null, out TF_Spec spec, out TF_Spec gsspec);
            //    Spec = spec;
            //    GoldenSampleSpec = gsspec;
            //}
            //else
            //{
            //    Spec = seqspec;
            //}

            return 1;
        }

        public int Save(string path = null)
        {
            return 1;  // it should be not edit/save in Toucan
            var d = _SequenceFile.AsPropertyObjectFile();
            if (!d.Locked)
            {
                d.Lock(LOCK);
            }

            _SequenceFile.Save(path);
            return 1;
        }

        //private const int DefectStart = 100;
        //private const int DefectEnd = 999;
        //private Regex DefectFormat = new Regex(@"^@?(([\w-_]+)(\d+))");

        public event EventHandler CalibrationExpired;
        public event EventHandler CalibrationExpiring;
        public event EventHandler ReferenceExpired;
        public event EventHandler ReferenceExpiring;
        public event EventHandler VerificationExpired;
        public event EventHandler VerificationExpiring;

        public string GetCalibrationBase()
        {
            return Path.Combine(TestStandEngine.CalibrationBase, $"{StationConfig.CustomerName ?? "CU"}_{StationConfig?.ProductName ?? "PRD"}_{StationConfig?.StationName ?? "STS"}");
        }

        public string GetReferenceBase()
        {
            return Path.Combine(TestStandEngine.ReferenceBase, $"{StationConfig.CustomerName ?? "CU"}_{StationConfig?.ProductName ?? "PRD"}_{StationConfig?.StationName ?? "STS"}");
        }

        public int StartCalibration()
        {
            return 1;
        }

        public int ApplyCalibration()
        {
            return 1;
        }

        public int ApplyReference()
        {
            return 1;
        }

        public int ApplyVerification()
        {
            return 1;
        }

        public string GetVerificationBase()
        {
            return Path.Combine(TestStandEngine.VerificationBase, $"{StationConfig?.CustomerName ?? "CU"}_{StationConfig?.ProductName ?? "PRD"}_{StationConfig?.StationName ?? "STS"}");
        }
    }
}
