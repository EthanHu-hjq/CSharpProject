using AudioPrecision.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TestCore;
using TestCore.Base;
using TestCore.Data;
using ToucanCore.Abstraction.Engine;

namespace ApEngine
{
    public class Sequence : ToucanCore.Abstraction.Engine.ISequence
    {
        public AudioPrecision.API.ISequenceSettings ApSequence { get; private set; }
        public TF_Spec Spec { get; set; }

        /// <summary>
        /// For Robin, which need inherit test data when swith seequence
        /// </summary>
        public TF_Result[] InheritResults { get; set; }
        public string Name { get; private set; }

        /// <summary>
        /// Should
        /// </summary>
        public string Version { get; internal set; } = "0.0.0.1";

        public string Description { get; } = null;

        public List<SignalPath> SignalPaths { get; set; } = new List<SignalPath>();

        public Sequence() { }
        public Sequence(AudioPrecision.API.ISequenceSettings seq)
        {
            ApSequence = seq;
            Name = seq.Name;
        }

        internal void Activate()
        {
            if (ApxEngine.ApRef.Sequence.Sequences.ActiveSequence != ApSequence)
            {
                try
                {
                    ApSequence.Activate();
                }
                catch (System.Runtime.Remoting.RemotingException)
                {
                    foreach (AudioPrecision.API.ISequenceSettings s in ApxEngine.ApRef.Sequence.Sequences)
                    {
                        if (s.Name == Name)
                        {
                            s.Activate();
                            ApSequence = s;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// the analyze will active the Sequence in APx
        /// </summary>
        public int Analyze()
        {
            Activate();

            Spec = new TF_Spec(Name, Version, new Nest<TF_Limit>(new TF_Limit("Root", false)));

            InheritResults = null;

            SignalPaths?.Clear();
            for (int i = 0; i < ApxEngine.ApRef.Sequence.Count; i++)
            {
                var signalpath = ApxEngine.ApRef.Sequence[i];

                if (!signalpath.Checked) continue;

                SignalPath sp = new SignalPath(signalpath);
                sp.Analyze();

                if (sp.Limits.Count > 0)
                {
                    Spec.Limit.Add(sp.Limits);
                }
                SignalPaths.Add(sp);
            }
            return 1;
        }
    }
}
