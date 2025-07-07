using NationalInstruments.TestStand.Interop.API;
using NationalInstruments.TestStand.Interop.UI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using TestCore;
using TestCore.Configuration;
using TestCore.Data;
using ToucanCore.Abstraction.Configuration;
using ToucanCore.Abstraction.Engine;
using TsEngine;

namespace TsA2BEngine
{
    public class TsA2BScript : TF_Base, IScript, IExecutionUISetting
    {
        internal static TsA2BEngine StaticEngine { get; set; }
        public NationalInstruments.TestStand.Interop.API.SequenceFile _SequenceFile { get; private set; }
        public ISequence this[string name] => throw new NotImplementedException();

        public ISequence this[int index] => throw new NotImplementedException();
        public TsEngine.Script TsScript { get; private set; }
        public int Id { get; set; }

        public string Name { get; private set; }

        public string Version => TsScript.Version;

        public DateTime Time { get; private set; }

        public string Author { get; set; }
        public string Note { get; set; }

        public string PartNo { get; private set; }

        public string CheckValue { get; private set; }

        public IReadOnlyList<string> ReferencesLib { get; private set; }
        public IList<Variable> Variables { get; } = new List<Variable>();
        public string BaseDirectory { get; set; }
        public string FilePath { get; set; }
        public GlobalConfiguration SystemConfig { get=> TsScript.SystemConfig; set { TsScript.SystemConfig = value; } }
        public StationConfig StationConfig { get => TsScript.StationConfig; set { TsScript.StationConfig = value; } }
        public SFCsConfig SFCsConfig { get => TsScript.SFCsConfig; set { TsScript.SFCsConfig = value; } }

        //public HardwareConfig HardwareConfig { get => TsScript.HardwareConfig; set { TsScript.HardwareConfig = value; } }

        public TF_Spec Spec { get; set; }

        private List<ISequence> _Sequences = new List<ISequence>();
        public IReadOnlyCollection<ISequence> Sequences { get => _Sequences; }

        public ISequence ActiveSequence { get; set; }

        const string LOCK = "PteAdmin";
        
        public const string VariantName = "A2B";
        public const string VarTestData = "TestData";
        public int NodeCount { get; internal set; }

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
                if (_SequenceFile.AsPropertyObjectFile() is PropertyObjectFile pof)
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

        public int SlotRow { get; set; }

        public int SlotColumn { get; set; }

        public bool ForceFocus { get; set; }

        public TF_Spec GoldenSampleSpec { get; set; }
        public InjectedVariableTable InjectedVariableTable { get; set; }

        //private List<string> _GoldenSamples;

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

        //    if (SystemConfig.General.RestrictLimit)
        //    {
        //        var spec = Path.Combine(Directory.GetParent(FilePath).FullName, TF_Spec.DefaultFileName);
        //        if (File.Exists(spec))
        //        {
        //            Spec = TF_Spec.LoadFromXml(spec);
        //        }
        //        else
        //        {
        //            throw new SpecFileNotFoundException($"Spec file not found. Please contact with Engineer") { Script = this };
        //        }
        //    }
        //    else
        //    {
        //        Spec = AnalyzeSpec();
        //    }

        //    IsOriginalModel = TsScript.IsOriginalModel;

        //    //var rs = new TF_Result(Spec);
        //    //rs.TestGuiVersion = System.Windows.Application.ResourceAssembly?.GetName()?.Version?.ToString() ?? System.Windows.Forms.Application.ProductVersion;
        //    //rs.TestSoftwareVersion = Version;

        //    return 1;
        //}

        public TF_Spec AnalyzeSpec() => AnalyzeSpec(JsonConfigPath);
        public TF_Spec AnalyzeSpec(string configpath)
        {
            try
            {
                ScriptDesc desc = new ScriptDesc() { Version = TsScript.Version};
                if (configpath is null)
                {
                    var mainseq = _SequenceFile.GetSequenceByName("MainSequence");

                    var prop = _SequenceFile.FileGlobalsDefaultValues.GetPropertyObject(VariantName, 0);
                    desc.NodeCount = (int)prop.GetValNumber(nameof(NodeCount), 0);

                    var itemprops = prop.GetPropertyObjectElements(VarTestData, 0);

                    var initdefectnum = 100;

                    foreach (var itemprop in itemprops)
                    {
                        ItemDesc itemdesc = new ItemDesc();

                        itemdesc.LoopCount = (int)itemprop.GetValNumber("LoopCount", 0);
                        var ve = itemprop.GetPropertyObject("ItemType", 0);
                        var dt = ve.Type.ValueType;
                        var dstrve = ve.GetFormattedValue("", 0);

                        var type = 0;
                        if (dstrve.Equals("string", StringComparison.OrdinalIgnoreCase))
                        {
                            type = 1;
                        }

                        //var type  = itemprop.GetValNumber("ItemType", 0);   // Enum cannot get by this
                        itemdesc.Name = itemprop.GetValString("ItemName", 0);
                        itemdesc.Unit = itemprop.GetValString("Unit", 0);

                        itemdesc.DefectCode = $"{SystemConfig.General.Prefix_DefectCode}{initdefectnum}";
                        itemdesc.ItemType = type;

                        if (type == 1)  //String Item
                        {
                        }
                        else // Default is Numeric
                        {
                            itemdesc.USL = itemprop.GetValNumber("HighLimit", 0);
                            itemdesc.LSL = itemprop.GetValNumber("LowLimit", 0);
                        }

                        initdefectnum++;

                        desc.Items.Add(itemdesc);
                    }
                }
                else
                {
                    desc.UpdateFromJson(configpath);
                }
                NodeCount = desc.NodeCount;

                var spec = desc.AnalyzeSpec();

                //TestStandHelper.SeqToData(_SequenceFile, mainseq, spec.Limit, true);

                //List<KeyValuePair<string, StepFormatError>> errors = new List<KeyValuePair<string, StepFormatError>>();
                //ScriptUtilities.VerifyLimit(spec.Limit, errors);

                //if (!IsOriginalModel)
                //{
                //    if (errors.Count > 0)
                //    {
                //        StringBuilder sb = new StringBuilder();

                //        foreach (var error in errors)
                //        {
                //            sb.AppendLine(string.Format("{0}\t{1, 24}", error.Key, error.Value));
                //        }

                //        var msg = sb.ToString();

                //        MessageBox.Show(msg, "Illegal Script", MessageBoxButton.OK, MessageBoxImage.Error);
                //        throw new InvalidDataException("Illegal Script");
                //    }
                //}

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

        public string JsonConfigPath { get; private set; }

        public int Open(string path)
        {
            var dir = System.IO.Path.GetDirectoryName(path);
            BaseDirectory = dir;
            string tspath = null;
            using (StreamReader sr = new StreamReader(path))
            {
                while (!sr.EndOfStream)
                {
                    var p = sr.ReadLine();
                    var ext = System.IO.Path.GetExtension(p);

                    if (TsA2BEngine.TestStand.FileFilter.Contains(ext))
                    {
                        tspath = System.IO.Path.IsPathRooted(p) ? p : System.IO.Path.Combine(dir, p);
                    }
                    else if(string.Equals(ext, ".json", StringComparison.OrdinalIgnoreCase)) // Config file
                    {
                        JsonConfigPath = System.IO.Path.IsPathRooted(p) ? p : System.IO.Path.Combine(dir, p);
                    }
                    
                }
            }

            TsScript = new TsEngine.Script();
            TsScript.Open(tspath);

            FilePath = path;
            _SequenceFile = TsScript._SequenceFile;

            //if (JsonConfigPath != null)
            //{
            //    Spec = AnalyzeSpec();
            //}

            return 1;
        }

        public int Save(string path = null)
        {
            var d = _SequenceFile.AsPropertyObjectFile();
            if (!d.Locked)
            {
                d.Lock(LOCK);
            }

            _SequenceFile.Save(path);
            return 1;
        }

        public event EventHandler CalibrationExpired;
        public event EventHandler CalibrationExpiring;
        public event EventHandler ReferenceExpired;
        public event EventHandler ReferenceExpiring;
        public event EventHandler VerificationExpired;
        public event EventHandler VerificationExpiring;

        public string GetCalibrationBase()
        {
            return Path.Combine(TsA2BEngine.CalibrationBase, $"{StationConfig.CustomerName ?? "CU"}_{StationConfig.ProductName ?? "PRD"}_{StationConfig.StationName ?? "STS"}");
        }

        public string GetReferenceBase()
        {
            return Path.Combine(TsA2BEngine.ReferenceBase, $"{StationConfig.CustomerName ?? "CU"}_{StationConfig.ProductName ?? "PRD"}_{StationConfig.StationName ?? "STS"}");
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
            return Path.Combine(TsA2BEngine.VerificationBase, $"{StationConfig.CustomerName ?? "CU"}_{StationConfig.ProductName ?? "PRD"}_{StationConfig.StationName ?? "STS"}");
        }
    }

    public class ScriptDesc
    {
        public string Version { get; set; }
        public int NodeCount { get; set; }
        public List<ItemDesc> Items { get; set; } = new List<ItemDesc>();

        public TF_Spec AnalyzeSpec()
        {
            TF_Spec spec = new TF_Spec("TYM", Version)
            {
                Author = "Joey",//AuthService.UserName;
                Time = DateTime.Now,
                Note = "Auto Generate By Toucan",
            };

            var initdefectnum = 100;
            foreach (var itemdesc in Items)
            {
                if (itemdesc.ItemType == 1)  //String Item
                {
                    var item = new TF_Limit(itemdesc.Name, string.Empty, Comparison.LOG, itemdesc.DefectCode, itemdesc.Unit);
                    spec.Limit.Add(item);
                }
                else // Default is Numeric
                {
                    var comp = Comparison.GELE;
                    if (double.IsNaN(itemdesc.USL))
                    {
                        if (double.IsNaN(itemdesc.LSL))
                        {
                            comp = Comparison.LOG;
                        }
                        else
                        {
                            comp = Comparison.GE;
                        }
                    }
                    else if (double.IsNaN(itemdesc.LSL))
                    {
                        comp = Comparison.LE;
                        itemdesc.LSL = itemdesc.USL;
                        itemdesc.USL = double.NaN;
                    }

                    if (itemdesc.LoopCount > 1)
                    {
                        var item = new Nest<TF_Limit>(new TF_Limit(itemdesc.Name));
                        for (var i = 0; i < itemdesc.LoopCount; i++)
                        {
                            item.Add(new TF_Limit($"{i}", itemdesc.USL, itemdesc.LSL, comp, itemdesc.DefectCode, itemdesc.Unit));
                        }

                        spec.Limit.Add(item);
                    }
                    else
                    {
                        var item = new TF_Limit(itemdesc.Name, itemdesc.USL, itemdesc.LSL, comp, itemdesc.DefectCode, itemdesc.Unit);
                        spec.Limit.Add(item);
                    }
                }
                initdefectnum++;
            }

            return spec;
        }

        public void UpdateFromJson(string jsonpath)
        {
            var js = TSUtility.VariableTool.ReadData(jsonpath).Property("A2B").Value as JObject;

            Items.Clear();
            NodeCount = js.Value<int>(nameof(NodeCount));
            var itemobjs = js.Property("TestData").Value as JArray;

            foreach(JObject item in itemobjs)
            {
                ItemDesc itemdesc = new ItemDesc();

                itemdesc.Name = item.Value<string>("ItemName");
                itemdesc.Unit = item.Value<string>("Unit");
                itemdesc.LSL = item.Value<double>("LowLimit");
                itemdesc.USL = item.Value<double>("HighLimit");
                itemdesc.LoopCount = item.Value<int>("LoopCount");
                var typestr = item.Value<string>("ItemType");

                var match = Regex.Match(typestr, "(\\d+)");
                if (match.Success)
                {
                    itemdesc.ItemType = int.Parse(match.Groups[1].Value);
                }

                Items.Add(itemdesc);
            }

        }
    }

    public class ItemDesc
    {
        public string Name { get; set; }
        public int LoopCount { get; set; }
        public string DefectCode { get; set; }
        public string Unit { get; set; }

        public int ItemType { get; set; }

        public double USL { get; set; }
        public double LSL { get; set; }
    }
}
