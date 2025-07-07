using ApEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robin.Core
{
    public class ExecutionInjection
    {
        public ApAction PreUUTLoop { get; }
        public ApAction PreUUT { get; }
        public ApAction PostUUT { get; }
        public ApAction PostUUTLoop { get; }
        public ApAction OnExit { get; }

        public void Inject(Execution exec)
        {
            if (PreUUTLoop != null)
            {
                exec.OnPreUUTLoop += Exec_OnPreUUTLoop;
            }

            if (PreUUT != null)
            {
                exec.OnPreUUTed += Exec_OnPreUUTed;
            }

            if (PostUUT != null)
            {
                exec.OnPostUUTed += Exec_OnPostUUTed;
            }

            if (PostUUTLoop != null)
            {
                exec.OnPostUUTLoop += Exec_OnPostUUTLoop;
            }
        }

        private void Exec_OnPostUUTLoop(object sender, TestCore.Data.TF_Result e)
        {
            PostUUTLoop?.Execute();
        }

        private void Exec_OnPostUUTed(object sender, TestCore.Data.TF_Result e)
        {
            PostUUT?.Execute();
        }

        private void Exec_OnPreUUTed(object sender, TestCore.Data.TF_Result e)
        {
            PreUUT?.Execute();
        }

        private void Exec_OnPreUUTLoop(object sender, TestCore.Data.TF_Result e)
        {
            PreUUTLoop?.Execute();
        }
    }
}
