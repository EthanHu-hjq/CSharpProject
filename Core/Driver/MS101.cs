using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using ToucanCore.Abstraction.HAL;

namespace ToucanCore.Driver
{
    public class MS101_V2R0 
    {
        static Dictionary<FixtureEvent, string> ChamberCommands = new Dictionary<FixtureEvent, string>();
        static Dictionary<FixtureState, string> ChamberSensors = new Dictionary<FixtureState, string>();

        public MS101_V2R0() 
        {
            ChamberCommands.Add(FixtureEvent.FrontDoorClosed, "");
        }
    }
}
