using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestCore.Data;

namespace ToucanCore.Engine
{
    public class StepAnalysisResult
    {
        public string StepName { get; set; }

        public TF_Limit Limit { get; set; }

        public Dictionary<StepFormatError, string> StepErrors { get; set; }

        public object Step { get; set; }

        public object Tag { get; set; }

        public StepAnalysisResult()
        {
            StepErrors = new Dictionary<StepFormatError, string>();
        }
    }
}
