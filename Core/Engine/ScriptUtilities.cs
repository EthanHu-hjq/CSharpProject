using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using TestCore;
using TestCore.Configuration;
using TestCore.Data;
using ToucanCore.Abstraction.Engine;
using ToucanCore.Abstraction.Configuration;
using System.Drawing;

namespace ToucanCore.Engine
{
    public static class ScriptUtilities
    {
        /// <summary>
        /// read Golden Sample
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static List<string> ReadTextLineAsList(string file)
        {
            var rtn = new List<string>();
            if (File.Exists(file)) 
            {
                using (StreamReader sr = new StreamReader(file))
                {
                    while(!sr.EndOfStream)
                    {
                        var txt = sr.ReadLine();
                        if(!string.IsNullOrWhiteSpace(txt))
                        {
                            rtn.Add(txt);
                        }
                    }
                }
            }

            return rtn;
        }

        public static int SaveEnumerableTextByLine(string file, IEnumerable<string> collection)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }

                if (collection is null) return 1;

                using (StreamWriter sr = new StreamWriter(file))
                {
                    foreach (var i in collection)
                    {
                        sr.WriteLine(i);
                    }
                }

                return 1;
            }
            catch
            {
                return 0;
            }
        }

        public static void VerifyLimit(Nest<TF_Limit> nest, List<KeyValuePair<string, StepFormatError>> errors, string prefix = null)
        {
            if (prefix is null)
            {
                prefix = nest.Element.Name;
            }
            else
            {
                prefix = $"{prefix}|{nest.Element.Name}";
            }

            for (int i = 0; i < nest.Count - 1; i++)
            {
                for (int j = i + 1; j < nest.Count; j++)
                {
                    if (nest[i].Element.Name == nest[j].Element.Name)
                    {
                        errors.Add(new KeyValuePair<string, StepFormatError>(nest[i].Element.Name, StepFormatError.DuplicatedItemName));
                        break;
                    }
                }
            }

            foreach (var sub in nest)
            {
                VerifyLimit(sub, errors, prefix);
            }
        }

        //public static void UpdateDefectCode(this TF_Spec spec, string prefix)
        //{
        //    List<TF_Limit> limitlist = new List<TF_Limit>();
        //    int maxdefect = 100;

        //    spec.Limit.Run(
        //        (x) =>
        //        {
        //            limitlist.Add(x);
        //            if (x?.Defect is string)
        //            {
        //                var match = Regex.Match(x?.Defect, @"(\d+)$");
        //                if (match.Success)
        //                {
        //                    if (int.TryParse(match.Groups[1].Value, out int defnum))
        //                    {
        //                        if (defnum > maxdefect)
        //                        {
        //                            maxdefect = defnum;
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        );

        //    var currentdefect = (maxdefect / 100 + 1) * 100;

        //    for (int i = 1; i < limitlist.Count; i++)
        //    {
        //        if (limitlist[i].Defect is null)
        //        {
        //            if (limitlist[i].Comp == Comparison.NULL) continue;
        //            limitlist[i].Defect = $"{prefix}{currentdefect}";

        //            //limitlist[i].Defect = $"{GlobalConfiguration.Default.General.Prefix_DefectCode}{TestCore.TF_Utility.DecToZnum_2Char(currentdefect)}";  //支持36进制

        //            currentdefect++;
        //        }
        //    }
        //}

        public static void AttachSecondaryLimit(TF_Spec spec)
        {
            if (spec.Secondary != null)
            {
                AttachSecondaryLimit(spec.Secondary);   // Link the secondary chain

                AttachSecondaryLimit(spec.Limit, spec.Secondary.Limit);
            }
        }

        private static void AttachSecondaryLimit(Nest<TF_Limit> prior, Nest<TF_Limit> secondary)
        {
            foreach (var item in secondary)
            {
                if (prior.FirstOrDefault(x => x.Element.Name == item.Element.Name) is Nest<TF_Limit> temp)
                {
                    temp.Element.Secondary = item.Element;

                    AttachSecondaryLimit(temp, item);
                }
            }
        }

        //public static void AttachSecondarySpec(this TF_Spec spec, IEnumerable<TF_Spec> speclist) 
        //{
        //    var temp = spec;
        //    if (speclist is null) return;
        //    foreach(var sub in speclist) 
        //    {
        //        spec.Secondary = sub;
        //        MergeInto(temp.Limit, sub.Limit);
        //        temp = sub;
        //    }
        //}

        //private static void MergeInto(Nest<TF_Limit> limit, Nest<TF_Limit> newlimit)
        //{
        //    foreach(var sub in newlimit)
        //    {
        //        var item = limit.First(x => x.Element.Name == sub.Element.Name);
        //        item.Element.Secondary = sub.Element;

        //        MergeInto(item, sub);
        //    }
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="specfilepath"></param>
        /// <param name="seqspecfunc">Func for getting spec from sequence</param>
        /// <param name="updateseqfunc">Action to update spec from sequence</param>
        /// <returns></returns>
        public static TF_Spec AnalyzeSpec(IScript script, Func<TF_Spec> seqspecfunc, Action<TF_Spec> updateseqfunc)
        {
            TF_Spec tf = null;
            if (script.SystemConfig?.General?.RestrictLimit == true)
            {
                var specpath = Path.Combine(script.BaseDirectory, TF_Spec.DefaultFileName);
                if (File.Exists(specpath))
                {
                    var fspec = TF_Spec.LoadFromXml(specpath);
                    var seqspec = seqspecfunc.Invoke();

                    if (fspec.Limit.Assert(seqspec.Limit, (x, y) => { return x.Name == y.Name; }))
                    {
                        if (script.SystemConfig.General.RestrictLimitOnlyForDefectCode == true)
                        {
                            tf = seqspec;
                            tf.Limit.SyncRun(fspec.Limit, (x, y) => { x.Defect = y.Defect; });  //TODO, what if the structure is not same
                        }
                        else
                        {
                            tf = fspec;

                            if (updateseqfunc is null)
                            {

                            }
                            else
                            {
                                updateseqfunc?.Invoke(tf);
                            }

                            if (true)
                            {
                                throw new SpecNotMatchException();
                            }

                        }
                    }
                    else
                    {
                        tf = seqspec;
                        throw new SpecNotMatchException("Spec file does not match to the test script");
                    }
                }
                else
                {
                    throw new SpecFileNotFoundException($"Spec file not found. Please contact with Engineer") { Script = script};
                }
            }
            else
            {
                tf = seqspecfunc.Invoke();
            }

            var tempspec = tf;
            if (script.SystemConfig?.General?.SecondarySpecs != null)
            {
                foreach (var sub in script.SystemConfig.General.SecondarySpecs)
                {
                    if (Path.IsPathRooted(sub.Key))
                    {
                        tempspec.Secondary = TF_Spec.LoadFromXml(sub.Key);
                        tempspec = tempspec.Secondary;
                    }
                    else if (string.IsNullOrEmpty(sub.Key))   // Main Spec
                    {
                    }
                    else
                    {
                        tempspec.Secondary = TF_Spec.LoadFromXml(Path.Combine(script.BaseDirectory, sub.Key));
                        tempspec = tempspec.Secondary;
                    }
                    tempspec.Grade = sub.Value;
                }

                AttachSecondaryLimit(tf);
            }

            return tf;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="script"></param>
        /// <param name="seqspec">sequence spec</param>
        /// <param name="updatesequencefunc"></param>
        /// <param name="scriptspec"></param>
        /// <param name="goldensamplespec"></param>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="SpecNotMatchException"></exception>
        /// <exception cref="SpecFileNotFoundException"></exception>
        public static void ApplyScriptConfig(this IScript script, TF_Spec seqspec, Action<TF_Spec> updatesequencefunc, out TF_Spec scriptspec, out TF_Spec goldensamplespec)
        {
            goldensamplespec = null;
            scriptspec = seqspec;

            if (!string.IsNullOrEmpty(script.SystemConfig.General.GolderSampleSpec))
            {
                var gsspecpath = script.SystemConfig.General.GolderSampleSpec;
                if (!Path.IsPathRooted(script.SystemConfig.General.GolderSampleSpec))
                {
                    gsspecpath = Path.Combine(script.BaseDirectory, script.SystemConfig.General.GolderSampleSpec);
                }

                if (File.Exists(gsspecpath))
                {
                    goldensamplespec = TF_Spec.LoadFromXml(gsspecpath);
                }
                else
                {
                    throw new FileNotFoundException($"{gsspecpath} does not exist. Please check the Golden Sample Spec setting");
                }
            }

            if (script.SystemConfig.General.RestrictLimit)
            {
                var specpath = Path.Combine(script.BaseDirectory, TF_Spec.DefaultFileName);
                if (File.Exists(specpath))
                {
                    var spec = TF_Spec.LoadFromXml(specpath);

                    //if(spec.Limit.Assert(seqspec.Limit, (x, y) => { return x.Name == y.Name; }))
                    if(CompareSpecStructure(spec.Limit, seqspec.Limit))
                    {
                        if (script.SystemConfig.General.RestrictLimitOnlyForDefectCode || updatesequencefunc == null)
                        {
                            SyncSpec(spec.Limit, seqspec.Limit);
                            //seqspec.Limit.SyncRun(spec.Limit, (x, y) => { x.Defect = y.Defect; });
                            scriptspec = seqspec;
                        }
                        else
                        {
                            updatesequencefunc(spec);
                            scriptspec = spec;
                        }
                    }
                    else
                    {
                        seqspec.UpdateDefectCode(script.SystemConfig.General.Prefix_DefectCode ?? "D-");
                        throw new SpecNotMatchException() { Script = script, Original = spec, Target = seqspec };
                    }
                }
                else
                {
                    scriptspec.UpdateDefectCode(script.SystemConfig.General.Prefix_DefectCode ?? "D-");
                    throw new SpecFileNotFoundException("No Spec File Found in RestrictLimit mode") { Script = script};
                }
            }
            scriptspec.UpdateDefectCode(script.SystemConfig.General.Prefix_DefectCode ?? "D-");
        }

        private static bool CompareSpecStructure(Nest<TF_Limit> source, Nest<TF_Limit> target)
        {
            if (source.Count != target.Count) return false;
            foreach(var item in source)
            {
                var sub = target.FirstOrDefault(x => x.Element.Name == item.Element.Name);

                if (sub is null)
                {
                    return false;
                }
                else
                {
                    if (!CompareSpecStructure(item, sub)) return false;
                }
            }

            return true;
        }

        private static bool SyncSpec(Nest<TF_Limit> source, Nest<TF_Limit> target)
        {
            if (source.Count != target.Count) return false;
            foreach (var item in source)
            {
                var sub = target.FirstOrDefault(x => x.Element.Name == item.Element.Name);

                if (sub is null)
                {
                    return false;
                }
                else
                {
                    sub.Element.Defect = item.Element.Defect;
                    sub.Element.Sfc = item.Element.Sfc;
                    if (!SyncSpec(item, sub)) return false;
                }
            }

            return true;
        }

        public static void InitScript(ToucanCore.Engine.Script script)
        {
            script.InjectedVariableTable = InjectedVariableTable.GetFromWorkbase(script.BaseDirectory);

            var configpath = Path.Combine(script.BaseDirectory, GlobalConfiguration.DefaultFileName);
            var seqspec = script.AnalyzeSpec();
            if (File.Exists(configpath))
            {
                script.SystemConfig = GlobalConfiguration.Load(configpath);

                if(script.SystemConfig.Station is null || script.SystemConfig.Station?.CustomerName == null)
                {
                    script.StationConfig = new StationConfig(null,
                    EngineUtilities.ToolboxService?.Customer?.Name,
                    EngineUtilities.ToolboxService?.Project?.Name,
                    EngineUtilities.ToolboxService?.Product?.Name,
                    EngineUtilities.ToolboxService?.Station?.Name,
                     EngineUtilities.ToolboxService?.StationInstance?.StationId
                     );

                    script.StationConfig.Location = GlobalConfiguration.Default.Station?.Location ?? Location.TYHZ;
                    script.StationConfig.Vendor = GlobalConfiguration.Default.Station.Vendor;
                }
                else
                {
                    script.StationConfig = script.SystemConfig.Station;
                }

                script.SFCsConfig = script.SystemConfig.SFCs;

                ApplyScriptConfig(script.OriginalScript, seqspec, null, out TF_Spec spec, out TF_Spec gsspec);
                script.Spec = spec;
                script.GoldenSampleSpec = gsspec;

                if (script is IExecutionUISetting uis)
                {
                    if (int.TryParse(script.SystemConfig.Query("/System/UI/SlotRow", "-1"), out int row))
                    {
                        uis.SlotRow = row;
                    }
                    if (int.TryParse(script.SystemConfig.Query("/System/UI/SlotColumn", "-1"), out int col))
                    {
                        uis.SlotColumn = col;
                    }
                }
            }
            else
            {
                script.Spec = seqspec;
            }

            var hwconfigpath = Path.Combine(script.BaseDirectory, HardwareConfig.DefaultFileName);
            if (File.Exists(hwconfigpath))
            {
                script.HardwareConfig = HardwareConfig.Load(hwconfigpath);
            }
        }

        //public static void UpdateGoldenSamples(IScript script, IEnumerable<string> samples)
        //{
        //    var file = Path.Combine(script.GetReferenceBase(), "GoldenSamples.txt");
        //    script.GoldenSampleSpec = samples?.ToList();
        //    ScriptUtilities.SaveEnumerableTextByLine(file, samples);
        //}
    }

    public class SpecFileNotFoundException : FileNotFoundException
    { 
        public IScript Script { get; set; }
        public SpecFileNotFoundException(): base() { }
        public SpecFileNotFoundException(string msg) : base(msg) { }
    }

    public class SpecNotMatchException : InvalidOperationException
    {
        public IScript Script { get; set; }
        public TF_Spec Original { get; set; }
        public TF_Spec Target { get; set; }
        public SpecNotMatchException() : base() { }
        public SpecNotMatchException(string msg) : base(msg) { }
    }
}
