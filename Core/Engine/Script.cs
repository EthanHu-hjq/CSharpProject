using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestCore.Configuration;
using TestCore.Data;
using ToucanCore.Abstraction.Configuration;
using ToucanCore.Abstraction.Engine;

namespace ToucanCore.Engine
{
    public class Script : IScriptPro, IExecutionUISetting
    {
        public IScript OriginalScript { get; }

        public string Name => OriginalScript.Name;

        public string Version => OriginalScript.Version;

        public DateTime Time => OriginalScript.Time;

        public string Author { get => OriginalScript.Author; set => OriginalScript.Author = value; }
        public string Note { get => OriginalScript.Note; set => OriginalScript.Note = value; }

        public bool IsModified => OriginalScript.IsModified;

        public string BaseDirectory => OriginalScript.BaseDirectory;

        public string FilePath { get => OriginalScript.FilePath; set => OriginalScript.FilePath = value; }

        public GlobalConfiguration SystemConfig { get => OriginalScript.SystemConfig; set => OriginalScript.SystemConfig = value; }

        public StationConfig StationConfig { get => OriginalScript.StationConfig; set => OriginalScript.StationConfig = value; }
        public SFCsConfig SFCsConfig { get => OriginalScript.SFCsConfig; set => OriginalScript.SFCsConfig = value; }

        public TF_Spec Spec { get => OriginalScript.Spec; set => OriginalScript.Spec = value; }

        public IReadOnlyCollection<ISequence> Sequences => OriginalScript.Sequences;

        public ISequence ActiveSequence => OriginalScript.ActiveSequence;

        public bool LockStatus { get => OriginalScript.LockStatus; set => OriginalScript.LockStatus = value; }

        public ISequence this[int index] => OriginalScript[index];

        public ISequence this[string name] => OriginalScript[name];

        public int Id => throw new NotImplementedException();

        public string PartNo => throw new NotImplementedException();

        public string CheckValue => throw new NotImplementedException();

        public HardwareConfig HardwareConfig { get; set; }

        public IList<Variable> Variables { get; set; } = new List<Variable>();

        public IReadOnlyList<string> ReferencesLib { get; private set; }

        public TF_Spec GoldenSampleSpec { get; set; }
        public InjectedVariableTable InjectedVariableTable { get => OriginalScript.InjectedVariableTable; set => OriginalScript.InjectedVariableTable = value; }

        public IReadOnlyCollection<string> GoldenSamples
        {
            get
            {
                if (_GoldenSamples is null)
                {
                    _GoldenSamples = ScriptUtilities.ReadTextLineAsList(System.IO.Path.Combine(GetReferenceBase(), "GoldenSamples.txt"));
                }
                return _GoldenSamples;
            }
        }
        private List<string> _GoldenSamples;

        public int SlotRow { get; set; }

        public int SlotColumn { get; set; }

        public bool ForceFocus { get; set; }

        public Script(IScript originalScript)
        {
            OriginalScript = originalScript;
            ScriptUtilities.InitScript(this);
        }

        public event EventHandler CalibrationExpired
        {
            add
            {
                OriginalScript.CalibrationExpired += value;
            }

            remove
            {
                OriginalScript.CalibrationExpired -= value;
            }
        }

        public event EventHandler CalibrationExpiring
        {
            add
            {
                OriginalScript.CalibrationExpiring += value;
            }

            remove
            {
                OriginalScript.CalibrationExpiring -= value;
            }
        }

        public event EventHandler ReferenceExpired
        {
            add
            {
                OriginalScript.ReferenceExpired += value;
            }

            remove
            {
                OriginalScript.ReferenceExpired -= value;
            }
        }

        public event EventHandler ReferenceExpiring
        {
            add
            {
                OriginalScript.ReferenceExpiring += value;
            }

            remove
            {
                OriginalScript.ReferenceExpiring -= value;
            }
        }

        public event EventHandler VerificationExpired
        {
            add
            {
                OriginalScript.VerificationExpired += value;
            }

            remove
            {
                OriginalScript.VerificationExpired -= value;
            }
        }

        public event EventHandler VerificationExpiring
        {
            add
            {
                OriginalScript.VerificationExpiring += value;
            }

            remove
            {
                OriginalScript.VerificationExpiring -= value;
            }
        }

        public int Open(string path)
        {
            return OriginalScript.Open(path);
        }

        public TF_Spec AnalyzeSpec()
        {
            return OriginalScript.AnalyzeSpec();
        }

        public int Save(string path = null)
        {
            return OriginalScript.Save(path);
        }

        public int StartCalibration()
        {
            return OriginalScript.StartCalibration();
        }

        public int ApplyCalibration()
        {
            return OriginalScript.ApplyCalibration();
        }

        public int ApplyReference()
        {
            return OriginalScript.ApplyReference();
        }

        public int ApplyVerification()
        {
            return OriginalScript.ApplyVerification();
        }

        public string GetReferenceBase()
        {
            return OriginalScript.GetReferenceBase();
        }

        public string GetCalibrationBase()
        {
            return OriginalScript.GetCalibrationBase();
        }

        public string GetVerificationBase()
        {
            return OriginalScript.GetVerificationBase();
        }

        public object Clone()
        {
            return OriginalScript.Clone();
        }

        public void UpdateGoldenSamples(IEnumerable<string> samples)
        {
            var file = System.IO.Path.Combine(GetReferenceBase(), "GoldenSamples.txt");
            _GoldenSamples = samples?.ToList();
            ScriptUtilities.SaveEnumerableTextByLine(file, samples);
        }
    }
}
