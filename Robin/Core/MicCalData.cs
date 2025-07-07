using ApEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestCore;

namespace Robin.Core
{
    public class MicCalData : TF_Base
    {
        public DateTime CalTime { get; set; }
        public List<MicChannelData> MicCals { get; } = new List<MicChannelData>();

        public void Apply()
        {
            foreach(var data in MicCals)
            {
                ApxEngine.ApRef.SignalPathSetup.References.AcousticInputReferences.SetSensitivity(0, data.Sensitivity * 10e-3);
                Info($"Set Sens {data.Name} {data.Sensitivity}");
            }
        }

        public int Save(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            using(StreamWriter sw = new StreamWriter(path))
            {
                sw.WriteLine(CalTime);
                sw.WriteLine("Channel,Mic ID,Mic Sens (mV/Pa)");
                
                foreach(var micval in MicCals)
                {
                    sw.WriteLine($"{micval.Name},{micval.ID},{micval.Sensitivity}");
                }
            }

            return 1;
        }

        public static MicCalData Load(string path)
        {
            using (StreamReader sr = new StreamReader(path))
            {
                var time = sr.ReadLine();

                MicCalData micCal = new MicCalData();

                if(DateTime.TryParse(time, out DateTime t))
                {
                    micCal.CalTime = t;
                }

                sr.ReadLine();

                while(!sr.EndOfStream)
                {
                    var temp = sr.ReadLine().Split(',');

                    if(temp.Length == 3)
                    {
                        micCal.MicCals.Add(new MicChannelData() { Name = temp[0], Index = int.Parse(temp[0].Substring(2)) - 1, ID = temp[1], Sensitivity = double.Parse(temp[2]) * 10e-4 }); // need to be 10e-4. not 10e-3
                    }
                    else
                    { 
                        break;
                    }
                }
                return micCal;
            }
        }
    }

    public class MicChannelData
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public string ID { get; set; }
        public double Sensitivity { get; set; }
    }
}
