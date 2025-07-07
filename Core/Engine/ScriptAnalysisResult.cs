using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TestCore.Configuration;
using TestCore.Data;
using TestCore;
using System.Text.RegularExpressions;

namespace ToucanCore.Engine
{
    public class ScriptAnalysisResult : TF_Base
    {
        const string XMLTag = "ScriptAnalyze";
        public DateTime Time { get; set; }

        public List<StepAnalysisResult> StepResults { get; }

        public string ScriptName { get; set; }
        public string ScriptVersion { get; set; }

        private TF_Spec _Spec;
        public TF_Spec Spec
        {
            get
            {
                if (_Spec is null)
                {
                    _Spec = new TF_Spec("TYM", ScriptVersion ?? "0.1");
                }
                return _Spec;
            }
        }

        private TF_Result _ResultTemplate;
        public TF_Result ResultTemplate
        {
            get
            {
                if (_ResultTemplate is null)
                {
                    GenerateTemplate(null);
                }
                return _ResultTemplate;
            }
        }

        private Dictionary<string, string> _DefectCodes = new Dictionary<string, string>();
        public IReadOnlyDictionary<string, string> DefectCodes
        {
            get
            {
                if (_DefectCodes is null)
                {

                }

                return _DefectCodes;
            }
        }

        public ScriptAnalysisResult()
        {
        }

        /// <summary>
        /// Assign Defect Code, or Load and Check from history spec
        /// </summary>
        /// <param name="history"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public TF_Result GenerateTemplate(TF_Spec history)
        {
            if (GlobalConfiguration.Default.General.RestrictLimit && history != null)
            {
                // Check and Merge Spec
                bool rs = false;
                if (GlobalConfiguration.Default.General.RestrictLimitOnlyForDefectCode)
                {
                    rs = history.Limit.ContainNest(Spec.Limit, (x, y) =>
                    {
                        if (x.Name == y.Name)
                        {
                            y.Defect = x.Defect;
                            return true;
                        }
                        return false;
                    }
                    );
                }
                else
                {
                    rs = history.Limit.ContainNest(Spec.Limit, (x, y) =>
                    {
                        if (x.Name == y.Name)
                        {
                            y.Defect = x.Defect;
                            y.USL = x.USL;
                            y.Comp = x.Comp;
                            y.AdditionInfo = x.AdditionInfo;
                            y.Format = x.Format;
                            y.LSL = x.LSL;
                            y.Skip = x.Skip;
                            y.Sfc = x.Sfc;
                            return true;
                        }
                        return false;
                    }
                    );
                }

                if (rs)
                {
                    _ResultTemplate = new TF_Result(Spec);
                }
                else
                {
                    throw new InvalidOperationException("Spec does not match to Required.");
                }
            }
            else  // No Restrict, Assign Defect Code
            {
                List<TF_Limit> limitlist = new List<TF_Limit>();
                _DefectCodes.Clear();
                int maxdefect = 100;

                Spec.Limit.Run(
                    (x) =>
                    {
                        limitlist.Add(x);
                        if (x?.Defect is string)
                        {
                            var match = Regex.Match(x?.Defect, @"(\d+)$");
                            if (match.Success)
                            {
                                if (int.TryParse(match.Groups[1].Value, out int defnum))
                                {
                                    if (defnum > maxdefect)
                                    {
                                        maxdefect = defnum;
                                    }
                                }
                            }
                        }
                    }
                    );

                var currentdefect = (maxdefect / 100 + 1) * 100;

                for (int i = 1; i < limitlist.Count; i++)
                {
                    if (limitlist[i].Defect is null)
                    {
                        if (limitlist[i].Comp == Comparison.NULL) continue;
                        limitlist[i].Defect = $"{GlobalConfiguration.Default.General.Prefix_DefectCode}{currentdefect}";

                        //limitlist[i].Defect = $"{GlobalConfiguration.Default.General.Prefix_DefectCode}{TestCore.TF_Utility.DecToZnum_2Char(currentdefect)}";  //支持36进制

                        currentdefect++;
                    }
                }

                _ResultTemplate = new TF_Result(Spec);

                var flatsteps = _ResultTemplate.StepDatas.ToFlatList();

                //StringBuilder names = new StringBuilder();
                //_ResultTemplate.StepDatas.GetData(names, Extension.ItemData_FetchType.Name);

                //var arr_name = flatsteps.Select(x => x.Element.Name).ToArray();

                //StringBuilder defects = new StringBuilder();
                //_ResultTemplate.StepDatas.GetData(defects, Extension.ItemData_FetchType.Defect);

                //var arr_defect = defects.ToString().Split(',');
                //var arr_defect = flatsteps.Select(x => ((TF_ItemData)x.Element)?.Limit?.Defect).ToArray();

                for (int i = 1; i < flatsteps.Count; i++)
                {
                    if (flatsteps[i].Element is TF_ItemData itemdata)
                    {
                        try
                        {
                            _DefectCodes.Add(itemdata.Name, itemdata.Limit.Defect);
                        }
                        catch (Exception ex)
                        {
                            Error($"{itemdata.Name},{itemdata.Limit.Defect} Error:{ex}");
                        }
                    }
                }

                //for (int i = 1; i < arr_name.Length; i++)  // the first item is rootnode
                //{
                //    try
                //    {
                //        _DefectCodes.Add(arr_name[i], arr_defect[i]);
                //    }
                //    catch (Exception ex)
                //    {
                //        Error($"{arr_name[i]},{arr_defect[i]} Error:{ex}");
                //    }
                //}
            }

            _ResultTemplate.IsSFC = GlobalConfiguration.Default.SFCs.EnableSfc;
            _ResultTemplate.Status = TF_TestStatus.NULL;

            _ResultTemplate.StationConfig = GlobalConfiguration.Default.Station;
            _ResultTemplate.SFCsConfig = GlobalConfiguration.Default.SFCs;
            return _ResultTemplate;
        }


        /// <summary>
        /// Check Duplicated Items
        /// </summary>
        /// <param name="nest"></param>
        /// <param name="errors"></param>
        /// <param name="prefix"></param>
        
        //public object XmlDeserialize(XElement element)
        //{
        //    throw new NotImplementedException();
        //}

        //public XElement XmlSerialize()
        //{
        //    XElement element = new XElement(XMLTag);

        //    element.Add(new XAttribute("name", ScriptName));
        //    element.Add(new XAttribute("time", Time));

        //    element.Add(Spec.XmlSerialize());

        //    XElement defects = new XElement("Defects");

        //    foreach (var d in DefectCodes)
        //    {
        //        XElement defect = new XElement("Defect");

        //        defect.Add(new XAttribute("name", d.Key));
        //        defect.Add(new XAttribute("code", d.Value));

        //        defects.Add(defect);
        //    }

        //    element.Add(defects);
        //    return element;
        //}

        //public static ScriptAnalysisResult LoadFromXml(string path)
        //{
        //    var doc = XDocument.Load(path);

        //    var top = doc.Element(XMLTag);

        //    if (top is null)
        //    {
        //        return null;
        //    }
        //    else
        //    {
        //        var specnode = top.Element(TF_Spec.XMLTag);
        //        if (specnode is null) return null;

        //        TF_Spec temp = new TF_Spec();

        //        var spec = temp.XmlDeserialize(specnode) as TF_Spec;

        //        ScriptAnalysisResult sar = new ScriptAnalysisResult()
        //        {
        //            _Spec = spec,
        //            ScriptVersion = spec.Version,
        //            ScriptName = spec.Name,
        //        };

        //        sar.ScriptName = top.Attribute("name").Value;
        //        sar.Time = DateTime.Parse(top.Attribute("time").Value);

        //        return sar;
        //    }
        //}
    }
}
