using ApEngineManager;
using NationalInstruments.TestStand.Interop.API;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestCore;
using TestCore.Configuration;
using TestCore.Data;
using ToucanCore.Abstraction.Configuration;
using ToucanCore.Abstraction.Engine;
using TsEngine;

namespace TsApEngine
{
    public class TsApScript : TF_Base, IScript
    {
        internal static TsApHybird StaticEngine { get; set; }
        public TsEngine.Script TsScript { get; private set; }
        public IScript ApScript { get; private set; }

        private Dictionary<string, string> _DefaultVariable;
        public IReadOnlyDictionary<string, string> DefaultVariable => _DefaultVariable;

        public const string ApVarSyncUp = "ApVarSyncUp";
        public const string ApVarConclusion = "ApResult";

        public string Name => ((IScript)TsScript).Name;

        public string Version => ((IScript)TsScript).Version;

        public DateTime Time => ((IScript)TsScript).Time;

        public string Author { get => ((IScript)TsScript).Author; set => ((IScript)TsScript).Author = value; }
        public string Note { get => ((IScript)TsScript).Note; set => ((IScript)TsScript).Note = value; }

        //public string PartNo => ((IScript)TsScript).PartNo;

        //public string CheckValue => ((IScript)TsScript).CheckValue;

        public bool IsModified => ((IScript)TsScript).IsModified;

        //public IReadOnlyList<string> ReferencesLib => ((IScript)TsScript).ReferencesLib;

        public string BaseDirectory { get; set; }

        public string FilePath { get => ((IScript)TsScript).FilePath; set => ((IScript)TsScript).FilePath = value; }
        public GlobalConfiguration SystemConfig { get => ((IScript)TsScript).SystemConfig; set => ((IScript)TsScript).SystemConfig = value; }
        public StationConfig StationConfig { get => ((IScript)TsScript).StationConfig; set => ((IScript)TsScript).StationConfig = value; }
        public SFCsConfig SFCsConfig { get => ((IScript)TsScript).SFCsConfig; set => ((IScript)TsScript).SFCsConfig = value; }

        //public HardwareConfig HardwareConfig { get => ((IScript)TsScript).HardwareConfig; set => ((IScript)TsScript).HardwareConfig = value; }

        public TF_Spec Spec { get; set; }
        public TF_Spec GoldenSampleSpec { get; set; }

        public IReadOnlyCollection<ISequence> Sequences => ((IScript)TsScript).Sequences;

        public ISequence ActiveSequence => ((IScript)TsScript).ActiveSequence;

        public bool LockStatus { get => ((IScript)ApScript).LockStatus; set => ((IScript)ApScript).LockStatus = value; }

        public InjectedVariableTable InjectedVariableTable { get; set; }

        public ISequence this[int index] => ((IScript)TsScript)[index];

        public ISequence this[string name] => ((IScript)TsScript)[name];

        public TsApScript()
        {
        }

        public event EventHandler CalibrationExpired
        {
            add
            {
                ((IScript)TsScript).CalibrationExpired += value;
            }

            remove
            {
                ((IScript)TsScript).CalibrationExpired -= value;
            }
        }

        public event EventHandler CalibrationExpiring
        {
            add
            {
                ((IScript)TsScript).CalibrationExpiring += value;
            }

            remove
            {
                ((IScript)TsScript).CalibrationExpiring -= value;
            }
        }

        public event EventHandler ReferenceExpired
        {
            add
            {
                ((IScript)TsScript).ReferenceExpired += value;
            }

            remove
            {
                ((IScript)TsScript).ReferenceExpired -= value;
            }
        }

        public event EventHandler ReferenceExpiring
        {
            add
            {
                ((IScript)TsScript).ReferenceExpiring += value;
            }

            remove
            {
                ((IScript)TsScript).ReferenceExpiring -= value;
            }
        }

        public event EventHandler VerificationExpired
        {
            add
            {
                ((IScript)TsScript).VerificationExpired += value;
            }

            remove
            {
                ((IScript)TsScript).VerificationExpired -= value;
            }
        }

        public event EventHandler VerificationExpiring
        {
            add
            {
                ((IScript)TsScript).VerificationExpiring += value;
            }

            remove
            {
                ((IScript)TsScript).VerificationExpiring -= value;
            }
        }

        public int Open(string path)
        {
            TsApHybird.GetTsApFilePath(path, out string tspath, out string appath, out string dir);
            BaseDirectory = dir;

            TsScript = TsApHybird.TestStand.LoadScriptFile(tspath) as TsEngine.Script;
            ApScript = TsApHybird.Apx.LoadScriptFile(appath);

            //Spec = new TF_Spec(TsScript.Spec.Name, TsScript.Spec.Version);

            //foreach (var item in TsScript.Spec.Limit)
            //{
            //    Spec.Limit.Add(item);
            //}
            //foreach (var item in ApScript.Spec.Limit)
            //{
            //    Spec.Limit.Add(item);
            //}

            //HardwareConfig = TsScript.HardwareConfig;
            //SystemConfig = TsScript.SystemConfig;  // TS SFCs should be skipped

            if (!TsScript._SequenceFile.FileGlobalsDefaultValues.Exists(TsApScript.ApVarSyncUp, 0))
            {
                TsScript._SequenceFile.FileGlobalsDefaultValues.NewSubProperty(TsApScript.ApVarSyncUp, PropertyValueTypes.PropValType_Container, false, string.Empty, 0);
            }

            if (!TsScript._SequenceFile.FileGlobalsDefaultValues.Exists(TsApScript.ApVarConclusion, 0))
            {
                TsScript._SequenceFile.FileGlobalsDefaultValues.NewSubProperty(TsApScript.ApVarConclusion, PropertyValueTypes.PropValType_String, false, string.Empty, 0);
            }

            if (TsScript._SequenceFile.FileGlobalsDefaultValues.GetPropertyObject(TsApScript.ApVarSyncUp, 0) is PropertyObject po)
            {
                var props = po.GetSubProperties(string.Empty, 0);

                _DefaultVariable = new Dictionary<string, string>();

                for (int i = 0; i < props.Length; i++)
                {
                    if (props[i].Type.ValueType == PropertyValueTypes.PropValType_String)
                    {
                        var d = props[i].Name;
                        _DefaultVariable.Add(props[i].Name, props[i].GetValString(string.Empty, 0));
                    }
                }
            }

            return 1;
        }

        //public int Analyze()
        //{
        //    TsScript.Analyze();
        //    ApScript.Analyze();
        //    return 1;
        //}

        public TF_Spec AnalyzeSpec()
        {
            var tsspec = TsScript.AnalyzeSpec();
            var apspec = ApScript.AnalyzeSpec();

            TsScript.Spec = tsspec;
            ApScript.Spec = apspec;

            var spec = new TF_Spec(tsspec.Name, tsspec.Version);

            foreach (var item in tsspec.Limit)
            {
                spec.Limit.Add(item);
            }

            foreach (var item in apspec.Limit)
            {
                spec.Limit.Add(item);
            }

            return spec;
        }

        public int Save(string path = null)
        {
            return ((IScript)TsScript).Save(path);
        }

        public int StartCalibration()
        {
            return ((IScript)TsScript).StartCalibration();
        }

        public int ApplyCalibration()
        {
            return ((IScript)TsScript).ApplyCalibration();
        }

        public int ApplyReference()
        {
            return ((IScript)TsScript).ApplyReference();
        }

        public int ApplyVerification()
        {
            return ((IScript)TsScript).ApplyVerification();
        }

        public string GetReferenceBase()
        {
            return ((IScript)TsScript).GetReferenceBase();
        }

        public string GetCalibrationBase()
        {
            return ((IScript)TsScript).GetCalibrationBase();
        }

        public string GetVerificationBase()
        {
            return ((IScript)TsScript).GetVerificationBase();
        }

        public object Clone()
        {
            return ((ICloneable)TsScript).Clone();
        }
    }
}
