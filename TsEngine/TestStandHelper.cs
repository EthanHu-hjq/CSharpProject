using NationalInstruments.TestStand.Interop.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TestCore.Data;
using TestCore;
using TsEngine.UIs;

namespace TsEngine
{
    public static class TestStandHelper
    {
        internal static DateTime TS_ZeroTime = new DateTime(1970, 1, 1, 0, 0, 0);

        internal static ExecutionUI _ExecutionUi { get; private set; }
        public static ExecutionUI ExecutionUi
        {
            get
            {
                if (_ExecutionUi is null)
                {
                    _ExecutionUi = new ExecutionUI();
                }
                return _ExecutionUi;
            }
        }

        public static NationalInstruments.TestStand.Interop.UI.Ax.AxApplicationMgr TS_AppMgr => ExecutionUi.TS_AppMgr;
        public static NationalInstruments.TestStand.Interop.API.Engine Engine { get => TS_AppMgr?.GetEngine(); }
        public static NationalInstruments.TestStand.Interop.UI.Ax.AxSequenceFileViewMgr TS_SeqFileViewMgr => ExecutionUi.AxSequenceFileMgr;

        public static Regex DefectFormat = new Regex(@"^@?(([\w-_]+)(\d+))");

        public static Nest<TF_Limit> StepToData(SequenceFile sequencefile, Step step, bool ismes)
        {
            var type = step.StepType.Name;
            string defectcode = null;

            string comment = step.AsPropertyObject().Comment;
            string skipstr = step.GetRunModeEx();
            bool skip = skipstr == "Skip";

            TF_Limit limit = null;
            Nest<TF_Limit> n_Limit = null;

            Match match = null;

            switch (type)
            {
                case "PassFailTest":
                    match = DefectFormat.Match(comment);
                    if (match.Success)
                    {
                        defectcode = match.Groups[1].Value;
                    }

                    limit = new TF_Limit(step.Name, 1, 1, Comparison.EQ, defectcode, null, null, skip, ismes);
                    n_Limit = new Nest<TF_Limit>() { Element = limit };

                    if (step.IsSequenceCall)
                    {
                        var seq = sequencefile.GetSequenceByName(step.Module.AsPropertyObject().GetValString("SeqName", 0));

                        SeqToData(sequencefile, seq, n_Limit, ismes);
                    }

                    break;

                case "NumericLimitTest":
                    match = DefectFormat.Match(comment);
                    if (match.Success)
                    {
                        defectcode = match.Groups[1].Value;
                    }

                    var hl = step.AsPropertyObject().GetValNumber("Limits.High", 0);
                    var ll = step.AsPropertyObject().GetValNumber("Limits.Low", 0);
                    var comp = step.AsPropertyObject().GetValString("Comp", 0);
                    var unit = step.AsPropertyObject().GetValString("Result.Units", 0);

                    if (Enum.TryParse(comp, out Comparison comparison))
                    {
                        switch (comparison)
                        {
                            case Comparison.LE:
                            case Comparison.LT:
                                limit = new TF_Limit(step.Name, ll, null, comparison, defectcode, unit, null, skip, ismes);
                                break;

                            default:
                                limit = new TF_Limit(step.Name, hl, ll, comparison, defectcode, unit, null, skip, ismes);
                                break;
                        }
                    }
                    else
                    {
                        limit = new TF_Limit(step.Name, hl, ll, Comparison.LOG, defectcode, unit, null, skip, ismes);
                    }

                    n_Limit = new Nest<TF_Limit>() { Element = limit };

                    if (step.IsSequenceCall)
                    {
                        var seq = sequencefile.GetSequenceByName(step.Module.AsPropertyObject().GetValString("SeqName", 0));

                        SeqToData(sequencefile, seq, n_Limit, ismes);
                    }
                    break;

                case "StringValueTest":
                    match = DefectFormat.Match(comment);
                    if (match.Success)
                    {
                        defectcode = match.Groups[1].Value;
                    }

                    var is_expr = step.AsPropertyObject().GetValBoolean("Limits.UseStringExpr", 0);

                    if (is_expr)
                    {
                        var ll_str = step.AsPropertyObject().GetValString("Limits.StringExpr", 0);
                        limit = new TF_Limit(step.Name, ll_str, Comparison.LOG, defectcode, null, null, skip, ismes);
                    }
                    else
                    {
                        var ll_str = step.AsPropertyObject().GetValString("Limits.String", 0);
                        limit = new TF_Limit(step.Name, ll_str, Comparison.MATCH, defectcode, null, null, skip, ismes);
                    }

                    n_Limit = new Nest<TF_Limit>() { Element = limit };

                    if (step.IsSequenceCall)
                    {
                        var seq = sequencefile.GetSequenceByName(step.Module.AsPropertyObject().GetValString("SeqName", 0));

                        SeqToData(sequencefile, seq, n_Limit, ismes);
                    }
                    break;

                case "NI_MultipleNumericLimitTest":
                    //match = DefectFormat.Match(comment);
                    //string prefix = null;
                    //int serial = 0;
                    string subdefect = null;
                    //if (match.Success)
                    //{
                    //    defectcode = match.Groups[1].Value;
                    //    var submatch = Regex.Match(defectcode, @"^(\D+)(\d+)$");
                    //    prefix = submatch.Groups[1].Value;
                    //    serial = int.Parse(submatch.Groups[2].Value);
                    //}

                    var meas_array = step.AsPropertyObject().GetValVariant("Result.Measurement", 0) as Array;

                    limit = new TF_Limit(step.Name);
                    n_Limit = new Nest<TF_Limit>() { Element = limit };

                    foreach (PropertyObject meas in meas_array)
                    {
                        var hl_multi = meas.GetValNumber("Limits.High", 0);
                        var ll_multi = meas.GetValNumber("Limits.Low", 0);
                        var comp_multi = meas.GetValString("Comp", 0);
                        var unit_multi = meas.GetValString("Units", 0);

                        //if (prefix != null)
                        //{
                        //    subdefect = $"{prefix}{serial}";
                        //    serial++;
                        //}

                        if (Enum.TryParse(comp_multi, out Comparison comparison_0))
                        {
                            switch (comparison_0)
                            {
                                case Comparison.LE:
                                case Comparison.LT:
                                    var limit_multi = new TF_Limit(meas.Name, ll_multi, null, comparison_0, subdefect, unit_multi, null, skip, ismes);
                                    n_Limit.Add(limit_multi);
                                    break;

                                default:
                                    var limit_multi0 = new TF_Limit(meas.Name, hl_multi, ll_multi, comparison_0, subdefect, unit_multi, null, skip, ismes);
                                    n_Limit.Add(limit_multi0);
                                    break;
                            }
                        }
                        else
                        {
                            var limit_multi = new TF_Limit(meas.Name, hl_multi, ll_multi, Comparison.LOG, subdefect, unit_multi, null, skip, ismes);
                            n_Limit.Add(limit_multi);
                        }

                        // add duplication detected if necessary
                    }

                    if (step.IsSequenceCall)
                    {
                        var seq = sequencefile.GetSequenceByName(step.Module.AsPropertyObject().GetValString("SeqName", 0));

                        SeqToData(sequencefile, seq, n_Limit, ismes);
                    }
                    break;

                case "SequenceCall":
                    limit = new TF_Limit(step.Name);

                    n_Limit = new Nest<TF_Limit>() { Element = limit };
                    if (step.IsSequenceCall)
                    {
                        var seq = sequencefile.GetSequenceByName(step.Module.AsPropertyObject().GetValString("SeqName", 0));

                        SeqToData(sequencefile, seq, n_Limit, ismes);
                    }
                    break;
            }

            return n_Limit;
        }

        public static void SeqToData(SequenceFile sequencefile, NationalInstruments.TestStand.Interop.API.Sequence seq, Nest<TF_Limit> ndata, bool ismes = true)
        {
            var cnt = seq.GetNumSteps(StepGroups.StepGroup_Setup);

            Regex defectformat = new Regex("");

            for (var i = 0; i < cnt; i++)
            {
                var step = seq.GetStep(i, StepGroups.StepGroup_Setup);
                var datas = StepToData(sequencefile, step, ismes);

                if (datas is null) continue;

                if (datas.Element.Comp != Comparison.NULL || datas.Count > 0)
                {
                    ndata.Add(datas);
                }
            }

            cnt = seq.GetNumSteps(StepGroups.StepGroup_Main);

            for (var i = 0; i < cnt; i++)
            {
                var step = seq.GetStep(i, StepGroups.StepGroup_Main);
                var datas = StepToData(sequencefile, step, ismes);

                if (datas is null) continue;

                if (datas.Element.Comp != Comparison.NULL || datas.Count > 0)
                {
                    ndata.Add(datas);
                }
            }

            cnt = seq.GetNumSteps(StepGroups.StepGroup_Cleanup);

            for (var i = 0; i < cnt; i++)
            {
                var step = seq.GetStep(i, StepGroups.StepGroup_Cleanup);
                var datas = StepToData(sequencefile, step, ismes);

                if (datas is null) continue;

                if (datas.Element.Comp != Comparison.NULL || datas.Count > 0)
                {
                    ndata.Add(datas);
                }
            }
        }

        /// <summary>
        /// Analysis Test Report.
        /// </summary>
        /// <param name="step"></param>
        /// <param name="stepdatas"></param>
        public static void StepRecordToResult(PropertyObject step, Nest<TF_StepData> stepdatas)
        {
            var steptype = step.GetValString("TS.StepType", 0);
            var stepname = step.GetValString("TS.StepName", 0);
            var stepstatus = step.GetValString("Status", 0);

            Nest<TF_StepData> stepdata = null;
            switch (steptype)
            {
                case "PassFailTest":
                    stepdata = stepdatas.FirstOrDefault(x => x.Element.Name == stepname);
                    if (stepstatus == "Skipped")
                    {
                        stepdata.Element.Result = TF_ItemStatus.NotTested;
                    }
                    else
                    {
                        stepdata.Element.StartTime = TS_ZeroTime.AddSeconds(step.GetValNumber("TS.StartTime", 0));
                        stepdata.Element.EndTime = stepdata.Element.StartTime.AddSeconds(step.GetValNumber("TS.TotalTime", 0));
                        if (stepdata.Element is TF_ItemData itemdata)
                        {
                            var rs_boolean = step.GetValBoolean("PassFail", 0);
                            itemdata.Value = rs_boolean ? 1 : 0;

                            if (Enum.TryParse(stepstatus, out TF_ItemStatus itemstatus))
                            {
                                stepdata.Element.Result = itemstatus;
                            }
                        }

                        if (step.Exists("TS.SequenceCall.ResultList", 0))
                        {
                            var substeps = step.GetValVariant("TS.SequenceCall.ResultList", 0) as IEnumerable<object>;

                            foreach (var substepobj in substeps)
                            {
                                if (substepobj is PropertyObject substep)
                                {
                                    StepRecordToResult(substep, stepdata);
                                }
                            }
                        }
                    }

                    break;

                case "NumericLimitTest":
                    stepdata = stepdatas.FirstOrDefault(x => x.Element.Name == stepname);
                    if (stepstatus == "Skipped")
                    {
                        stepdata.Element.Result = TF_ItemStatus.NotTested;
                    }
                    else
                    {
                        stepdata.Element.StartTime = TS_ZeroTime.AddSeconds(step.GetValNumber("TS.StartTime", 0));
                        stepdata.Element.EndTime = stepdata.Element.StartTime.AddSeconds(step.GetValNumber("TS.TotalTime", 0));
                        if (stepdata.Element is TF_ItemData itemdata)
                        {
                            itemdata.Value = step.GetValNumber("Numeric", 0);

                            if (Enum.TryParse(stepstatus, out TF_ItemStatus itemstatus))
                            {
                                stepdata.Element.Result = itemstatus;
                            }
                        }

                        if (step.Exists("TS.SequenceCall.ResultList", 0))
                        {
                            var substeps = step.GetValVariant("TS.SequenceCall.ResultList", 0) as object[];

                            foreach (var substepobj in substeps)
                            {
                                if (substepobj is PropertyObject substep)
                                {
                                    StepRecordToResult(substep, stepdata);
                                }
                            }
                        }
                    }

                    break;
                case "StringValueTest":
                    stepdata = stepdatas.FirstOrDefault(x => x.Element.Name == stepname);
                    if (stepstatus == "Skipped")
                    {
                        stepdata.Element.Result = TF_ItemStatus.NotTested;
                    }
                    else
                    {
                        stepdata.Element.StartTime = TS_ZeroTime.AddSeconds(step.GetValNumber("TS.StartTime", 0));
                        stepdata.Element.EndTime = stepdata.Element.StartTime.AddSeconds(step.GetValNumber("TS.TotalTime", 0));
                        if (stepdata.Element is TF_ItemData itemdata)
                        {
                            itemdata.Value = step.GetValString("String", 0);

                            if (Enum.TryParse(stepstatus, out TF_ItemStatus itemstatus))
                            {
                                stepdata.Element.Result = itemstatus;
                            }
                        }

                        if (step.Exists("TS.SequenceCall.ResultList", 0))
                        {
                            var substeps = step.GetValVariant("TS.SequenceCall.ResultList", 0) as object[];

                            foreach (var substepobj in substeps)
                            {
                                if (substepobj is PropertyObject substep)
                                {
                                    StepRecordToResult(substep, stepdata);
                                }
                            }
                        }
                    }
                    break;
                case "NI_MultipleNumericLimitTest":
                    stepdata = stepdatas.FirstOrDefault(x => x.Element.Name == stepname);
                    if (stepstatus == "Skipped")
                    {
                        stepdata.Element.Result = TF_ItemStatus.NotTested;
                    }
                    else
                    {
                        stepdata.Element.StartTime = TS_ZeroTime.AddSeconds(step.GetValNumber("TS.StartTime", 0));
                        stepdata.Element.EndTime = stepdata.Element.StartTime.AddSeconds(step.GetValNumber("TS.TotalTime", 0));

                        var measures = step.GetValVariant("Measurement", 0) as object[];
                        foreach (var measobj in measures)
                        {
                            if (measobj is PropertyObject meas)
                            {
                                if (stepdata.FirstOrDefault(x => x.Element.Name == meas.Name)?.Element is TF_ItemData subitem)
                                {
                                    subitem.StartTime = stepdata.Element.StartTime;
                                    subitem.EndTime = stepdata.Element.EndTime;
                                    subitem.Value = meas.GetValNumber("Data", 0);

                                    var substatus = meas.GetValString("Status", 0);

                                    if (Enum.TryParse(substatus, out TF_ItemStatus subitemstatus))
                                    {
                                        subitem.Result = subitemstatus;
                                    }
                                }
                            }
                        }

                        if (step.Exists("TS.SequenceCall.ResultList", 0))
                        {
                            var substeps = step.GetValVariant("TS.SequenceCall.ResultList", 0) as object[];

                            foreach (var substepobj in substeps)
                            {
                                if (substepobj is PropertyObject substep)
                                {
                                    StepRecordToResult(substep, stepdata);
                                }
                            }
                        }

                        if (stepdata.Element is TF_ItemData itemdata)
                        {
                            if (Enum.TryParse(stepstatus, out TF_ItemStatus itemstatus))
                            {
                                stepdata.Element.Result = itemstatus;
                            }
                        }
                    }

                    break;
                case "SequenceCall":
                case "NI_Wait":
                    stepdata = stepdatas.FirstOrDefault(x => x.Element.Name == stepname);
                    if (stepdata != null)
                    {
                        if (stepstatus == "Skipped")
                        {
                            stepdata.Element.Result = TF_ItemStatus.NotTested;
                        }
                        else
                        {
                            stepdata.Element.StartTime = TS_ZeroTime.AddSeconds(step.GetValNumber("TS.StartTime", 0));
                            stepdata.Element.EndTime = stepdata.Element.StartTime.AddSeconds(step.GetValNumber("TS.TotalTime", 0));

                            if (stepdata.Element is TF_ItemData itemdata)
                            {
                                if (Enum.TryParse(stepstatus, out TF_ItemStatus itemstatus))
                                {
                                    stepdata.Element.Result = itemstatus;
                                }
                            }

                            try
                            {
                                var substeps = step.GetValVariant("TS.SequenceCall.ResultList", 0) as object[];

                                foreach (var substepobj in substeps)
                                {
                                    if (substepobj is PropertyObject substep)
                                    {
                                        StepRecordToResult(substep, stepdata);
                                    }
                                }
                            }
                            catch (System.Runtime.InteropServices.COMException)
                            {
                                // Not a data collection
                            }
                        }
                    }
                    break;
            }

        }

        public static NationalInstruments.TestStand.Interop.API.Sequence FetchOrCreateSequence(SequenceFile file, string name)
        {
            NationalInstruments.TestStand.Interop.API.Sequence seq = null;
            if (file.SequenceNameExists(name))
            {
                seq = file.GetSequenceByName(name);
            }
            else
            {
                seq = TestStandHelper.Engine.NewSequence();

                seq.Name = name;
                file.InsertSequenceEx(file.NumSequences, seq);
            }
            return seq;
        }
    }
}
