using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestCore.Data;
using TestCore.Base;
using TestCore;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using TestCore.Configuration;
using static ToucanCore.HAL.StartTrigger_Fixture;
using System.Threading;
using ToucanCore.Driver;
using ToucanCore.HAL;
using System.Runtime.CompilerServices;
using System.Windows;
using ToucanCore.Configuration;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace ToucanCore.Engine
{
    public static class EngineUtilities
    {
        public static TestCore.Services.IToolboxService ToolboxService = TestCore.Services.ServiceStatic.ToolboxService();

        /// <summary>
        /// Generate New TF_Result with required spec
        /// </summary>
        /// <param name="spec"></param>
        /// <param name="required"></param>
        /// <param name="defectPrefex"></param>
        /// <param name="RestrictLimit"></param>
        /// <param name="restrictLimitOnlyForDefectCode"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static TF_Result GenerateTemplate(TF_Spec spec, string defectPrefex, out Dictionary<string, string> defectCodes, TF_Spec required = null, bool restrictLimitOnlyForDefectCode = false)
        {
            TF_Result result = null;

            defectCodes = new Dictionary<string, string>();
            if (required != null)
            {
                bool rs = false;
                if (restrictLimitOnlyForDefectCode)
                {
                    rs = required.Limit.ContainNest(spec.Limit, (x, y) =>
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
                    // Apply Required spec into target spec, if newer, no change
                    rs = required.Limit.ContainNest(spec.Limit, (x, y) =>
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
                    result = new TF_Result(spec);
                }
                else
                {
                    throw new InvalidOperationException("Spec does not match to Required.");
                }
            }
            else
            {
                List<TF_Limit> limitlist = new List<TF_Limit>();
                int maxdefect = 100;

                spec.Limit.Run(
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

                if (limitlist.Count > 800)
                {
                    // TODO: 支持36进制
                }

                for (int i = 1; i < limitlist.Count; i++)
                {
                    if (limitlist[i].Defect is null)
                    {
                        if (limitlist[i].Comp == TestCore.Comparison.NULL) continue;
                        limitlist[i].Defect = $"{defectPrefex}{currentdefect}";
                        currentdefect++;
                    }
                }

                result = new TF_Result(spec);

                var flatsteps = result.StepDatas.ToFlatList();

                for (int i = 1; i < flatsteps.Count; i++)
                {
                    if (flatsteps[i].Element is TF_ItemData itemdata)
                    {
                        try
                        {
                            defectCodes.Add(itemdata.Name, itemdata.Limit.Defect);
                        }
                        catch (Exception ex)
                        {
                            TF_Base.StaticLog($"{itemdata.Name},{itemdata.Limit.Defect} Error:{ex}");
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

            return result;
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


        //public static void ApplyHooks(this IExecution execution)
        //{
        //    var hooks = Enum.GetValues(typeof(ProcessHook));

        //    foreach (ProcessHook hook in hooks)
        //    {
        //        var p = Path.Combine(execution.Workbase, "Hooks", $"{hook}.bat");

        //        Process process = new Process();
        //        process.StartInfo.UseShellExecute = false;
        //        process.StartInfo.FileName = p;
        //        process.StartInfo.CreateNoWindow = true;
        //        process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
        //        process.StartInfo.WorkingDirectory = execution.Workbase;

        //        if (File.Exists(p))
        //        {
        //            switch (hook)
        //            {
        //                case ProcessHook.OnInitializing:
        //                    execution.ExecutionStarted += (object sender, EventArgs e) => { process.Start(); };
        //                    break;
        //                case ProcessHook.OnPreUUTLoop:
        //                    execution.OnPreUUTLoop += (object sender, TF_Result rs) => { process.StartInfo.Arguments = rs.SocketId; process.Start();};
        //                    break;
        //                case ProcessHook.OnPreUUTing:
        //                    execution.OnPreUUTing += (object sender, TF_Result rs) => { process.StartInfo.Arguments = rs.SocketId; process.Start(); };
        //                    break;
        //                case ProcessHook.OnPreUUTed:
        //                    execution.OnPreUUTed += (object sender, TF_Result rs) => { process.StartInfo.Arguments = rs.SocketId; process.Start(); };
        //                    break;
        //                case ProcessHook.OnUUTIdentified:
        //                    execution.OnUutIdentified += (object sender, TF_Result rs) => { process.StartInfo.Arguments = rs.SocketId; process.Start(); };
        //                    break;
        //                case ProcessHook.OnUUTPassed:
        //                    execution.OnUutPassed += (object sender, TF_Result rs) => { process.StartInfo.Arguments = rs.SocketId; process.Start(); };
        //                    break;
        //                case ProcessHook.OnUUTFailed:
        //                    execution.OnUutFailed += (object sender, TF_Result rs) => { process.StartInfo.Arguments = rs.SocketId; process.Start(); };
        //                    break;
        //                case ProcessHook.OnError:
        //                    execution.OnError += (object sender, TF_Result rs) => { process.StartInfo.Arguments = rs.SocketId; process.Start(); };
        //                    break;
        //                case ProcessHook.OnQuit:
        //                    execution.ExecutionStopped += (object sender, EventArgs e) => { process.Start(); };
        //                    break;
        //                case ProcessHook.OnPostUUTing:
        //                    execution.OnPostUUTing += (object sender, TF_Result rs) => { process.StartInfo.Arguments = rs.SocketId; process.Start(); };
        //                    break;
        //                case ProcessHook.OnPostUUTed:
        //                    execution.OnPostUUTed += (object sender, TF_Result rs) => { process.StartInfo.Arguments = rs.SocketId; process.Start(); };
        //                    break;
        //                case ProcessHook.OnPostUUTLoop:
        //                    execution.OnPostUUTLoop += (object sender, TF_Result rs) => { process.StartInfo.Arguments = rs.SocketId; process.Start(); };
        //                    break;
        //            }
        //        }
        //    }
        //}

        public static int AddTestCount(this StationConfig station, int slotidx = -1)
        {
            try
            {
                using (var si = TestCore.Services.ServiceStatic.RootKey.CreateSubKey("StationInfo", true))
                {
                    using (var item = si.CreateSubKey($"{station.CustomerName}_{station.ProjectName}_{station.ProductName}_{station.StationName}"))
                    {
                        var name = slotidx < 0 ? "Count" : $"Slot_{slotidx}_Count";
                        if (item.GetValue(name, 0) is int idx)
                        {
                            item.SetValue(name, idx + 1);
                        }
                        else
                        {
                            item.SetValue(name, 1);
                        }
                        return 1;
                    }
                }
            }
            catch (Exception ex)
            {
                TF_Base.StaticLog(ex.ToString());
                return -1;
            }
        }

        public static int ClearTestCount(this StationConfig station, int slotidx = -1)
        {
            try
            {
                using (var si = TestCore.Services.ServiceStatic.RootKey.CreateSubKey("StationInfo", true))
                {
                    using (var item = si.CreateSubKey($"{station.CustomerName}_{station.ProjectName}_{station.ProductName}_{station.StationName}"))
                    {
                        if (slotidx < 0)
                        {
                            item?.SetValue($"Count", 0);

                            var names = si?.GetValueNames();
                            foreach (var name in names)
                            {
                                if (name.StartsWith("Slot_"))
                                {
                                    item.DeleteValue(name);
                                }
                            }
                        }
                        else
                        {
                            item?.SetValue($"Slot_{slotidx}_Count", 0);
                        }
                    }
                }

                return 1;
            }
            catch (Exception ex)
            {
                TF_Base.StaticLog(ex.ToString());
                return -1;
            }
        }

        public static int GetTestCount(this StationConfig station, int slotidx = -1)
        {
            try
            {
                using (var si = TestCore.Services.ServiceStatic.RootKey.CreateSubKey("StationInfo", true))
                {
                    using (var item = si.CreateSubKey($"{station.CustomerName}_{station.ProjectName}_{station.ProductName}_{station.StationName}"))
                    {
                        if (item.GetValue(slotidx < 0 ? "Count" : $"Slot_{slotidx}_Count", 0) is int idx)
                        {
                            return idx;
                        }
                        return 0;
                    }
                }
            }
            catch (Exception ex)
            {
                TF_Base.StaticLog(ex.ToString());
                return -2;
            }
        }

        public static string GenerateLocalReportDir(this TF_Result rs, string basedir)
        {
            string path = null;

            if (rs.StationConfig.ProjectName == rs.StationConfig.ProductName || string.IsNullOrEmpty(rs.StationConfig.ProductName))
            {
                path = $"{basedir ?? AppContext.BaseDirectory}\\{rs.StationConfig.CustomerName}\\{rs.StationConfig.ProjectName}\\{rs.StationConfig.StationName}\\{rs.EndTime.ToString("yyyy-MM-dd")}{(rs.IsSFC && rs.SFCsConfig.EnableSfc ? "" : "-Offline")}\\{rs.StationConfig.StationID ?? "00"}{rs.SocketId ?? "1"}";
            }
            else
            {
                path = $"{basedir ?? AppContext.BaseDirectory}\\{rs.StationConfig.CustomerName}\\{rs.StationConfig.ProjectName}\\{rs.StationConfig.ProductName}\\{rs.StationConfig.StationName}\\{rs.EndTime.ToString("yyyy-MM-dd")}{(rs.IsSFC && rs.SFCsConfig.EnableSfc ? "" : "-Offline")}\\{rs.StationConfig.StationID ?? "00"}{rs.SocketId ?? "1"}";
            }

            return path;
        }

        /// <summary>
        /// check the external Limit
        /// </summary>
        /// <param name="stepDatas"></param>
        /// <param name="limits"></param>
        /// <param name="defectitem"></param>
        /// <returns></returns>
        public static bool VerifyExternalLimit(this Nest<TF_StepData> stepDatas, Nest<TF_Limit> limits, out Nest<TF_StepData> defectitem) 
        {
            defectitem = null;

            if (stepDatas.Element is TF_ItemData itemdata)
            {
                switch (limits.Element.Comp)
                {
                    case Comparison.NULL: break;
                    case Comparison.LOG: break;

                    default:
                        if (itemdata.Value is null)
                        {
                            defectitem = stepDatas;
                            return false;
                        }
                        else
                        {
                            var rs = false;
                            if (itemdata.Value is TF_Curve cv)
                            {
                                var usl = limits.Element.USL as TF_Curve;
                                var lsl = limits.Element.LSL as TF_Curve;
                                rs = itemdata.Value.CheckLimit(usl?.Resample(cv), lsl?.Resample(cv), limits.Element.Comp);
                            }
                            else
                            {
                                rs = itemdata.Value.CheckLimit(limits.Element.USL, limits.Element.LSL, limits.Element.Comp);
                            }

                            if (!rs)
                            {
                                defectitem = stepDatas;
                                return false;
                            }
                        }
                        break;
                }
            }
            else
            {
                return true;
            }

            foreach (var limit in limits)
            {
                var step = stepDatas.First(x=> x.Element.Name == limit.Element.Name);

                if (!VerifyExternalLimit(step, limit, out defectitem))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Check the failure item with external limits
        /// </summary>
        /// <param name="stepDatas"></param>
        /// <param name="limits"></param>
        /// <returns></returns>
        public static bool VerifySecondaryLimit(this Nest<TF_StepData> stepDatas, Nest<TF_Limit> limits)
        {
            if (stepDatas.Element.Result == TF_ItemStatus.Failed)
            {
                if (stepDatas.Element is TF_ItemData itemdata)
                {
                    switch (limits.Element.Comp)
                    {
                        case Comparison.NULL: break;
                        case Comparison.LOG: break;

                        default:
                            if (itemdata.Value is null)
                            {
                                return false;
                            }
                            else
                            {
                                var rs = itemdata.Value.CheckLimit(limits.Element.USL, limits.Element.LSL, limits.Element.Comp);

                                if (!rs)
                                {
                                    return false;
                                }
                            }
                            break;
                    }
                }
                else
                {
                    return true;
                }

                foreach(var stepdata in stepDatas)
                {
                    if(stepdata.Element.Result == TF_ItemStatus.Failed)
                    {
                        var steplimit = limits.FirstOrDefault(x => x.Element.Name == stepdata.Element.Name);

                        if (steplimit is null) return false;
                        if (!VerifySecondaryLimit(stepdata, steplimit))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
            else
            {
                return true;
            }
        }

        public static bool CheckContainLimit(this Nest<TF_StepData> datas, Nest<TF_Limit> limits)
        {
            foreach (var limit in limits)
            {
                var stepdata = datas.FirstOrDefault(x => x.Element.Name == limit.Element.Name);

                if (stepdata is null) return false;
                if (!CheckContainLimit(stepdata, limit))
                {
                    return false;
                }
            }
            return true;
        }

        //public static void AttachSecondaryLimit(TF_Spec spec)
        //{
        //    if(spec.Secondary != null)
        //    {
        //        AttachSecondaryLimit(spec.Secondary);   // Link the secondary chain

        //        AttachSecondaryLimit(spec.Limit, spec.Secondary.Limit);
        //    }
        //}

        //public static void AttachSecondaryLimit(Nest<TF_Limit> prior,  Nest<TF_Limit> secondary)
        //{
        //    foreach (var item in secondary)
        //    {
        //        if(prior.FirstOrDefault(x=>x.Element.Name == item.Element.Name) is Nest<TF_Limit> temp)
        //        {
        //            temp.Element.Secondary = item.Element;

        //            AttachSecondaryLimit(temp, item);
        //        }
        //    }
        //}
    }

    [Flags]
    public enum StepFormatError
    {
        NoError,

        // Error and cold formatable
        IllegalStepName,
        DuplicatedItemName,

        // Warning and formatable
        NoDefectCode,
        DefectCodeConfliction,

        /// <summary>
        /// Warning, 
        /// </summary>
        DynamicLimit,
        ConditionalItem,
        RecurisiveCall,
        ActionWithJudgement,
    }
}
