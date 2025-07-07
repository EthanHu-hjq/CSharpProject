using NModbus;
using NModbus.Device;
using NModbus.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;
using TestCore;
using TestCore.Services;
using ToucanCore.HAL;
using ToucanCore.Abstraction.HAL;
using System.IO.Ports;
using static ToucanCore.Driver.PLC_Mitsubishi_FX;

namespace ToucanCore.Driver
{
    public class LS200 : TF_Base, IFixture, ILockableFixture
    {
        const string IN_DoorClosed = "Modbus_DoorClosed";
        const string IN_DutReady = "Modbus_DutReady";
        const string OUT_TestDone = "Modbus_TestDone";

        readonly static string FilePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "LS200.xml");
        public FixtureManipulationConfig ManipulationConfig { get; } = new FixtureManipulationConfig(
            new string[] { "Door Closed", PositionKey, PressureKey, IN_DoorClosed, IN_DutReady }, new string[] { "X6", "0", "0", "601", "561" }, 
            new string[] { "Door Reg1", "Door Reg2", OUT_TestDone }, new string[] { "M13", "M15", "571"});

        public const string PositionRegister = "D128";
        public const string PressureRegister = "D132";

        public const string PositionKey = "Position(mm)";
        public const string PressureKey = "Pressure(N)";

        /// <summary>
        /// D128. Range 0-150mm
        /// </summary>
        public int PositionValue { get; private set; }

        /// <summary>
        /// D132. Range 0-400N
        /// </summary>
        public int PressureValue { get; private set; }

        public int SocketCount => 1;

        public bool AutoDutIn { get; set; }
        public bool AutoDutOut { get; set; }

        public FixtureState State => throw new NotImplementedException();

        public string Support => "LS200";
        public string Model => "LS200";

        public string SN => "LS200 SN";

        public string Resource { get; set; }

        public bool IsOpen => PLC?.Port?.IsOpen ?? false;

        public bool IsInitialized => PLC?.IsInitialized ?? false;

        public event EventHandler<DutMessage> DutIning;
        public event EventHandler<DutMessage> DutInDone;
        public event EventHandler<DutMessage> DutOuting;
        public event EventHandler<DutMessage> DutOuted;
        public event EventHandler<DutMessage> OnDutPresent;
        public event EventHandler<DutMessage> OnDutAbsent;
        public event EventHandler<DutMessage> FrontDoorOpening;
        public event EventHandler<DutMessage> FrontDoorOpened;
        public event EventHandler<DutMessage> FrontDoorClosing;
        public event EventHandler<DutMessage> FrontDoorClosed;
        public event EventHandler<DutMessage> RearDoorOpening;
        public event EventHandler<DutMessage> RearDoorOpened;
        public event EventHandler<DutMessage> RearDoorClosing;
        public event EventHandler<DutMessage> RearDoorClosed;
        public event EventHandler<DutMessage> EmergencyTrigged;
        public event EventHandler<DutMessage> FixtureError;
        public event EventHandler Initializing;
        public event EventHandler Initialized;
        public event EventHandler Cleared;

        PLC_Mitsubishi_FX PLC { get; set; }   // *idn? rtn: FB,LS200,V1.0
        //IModbusMaster Modbus { get; set; }

        public LS200()
        {
        }

        private string Query(string cmd)
        {

            return string.Empty;
        }

        private byte Modbus_SlaveAddr = 1;
        private bool IsFbV1 = false;
        public int CheckDutReady(out bool state, int slot = 0)
        {
            state = true;

            if(IsFbV1)
            {
                var reg1 = ManipulationConfig.InCommands[IN_DutReady];

                //if (PLC_Mitsubishi_FX.ParseChannel(reg1, out PLC_Mitsubishi_FX.RegisterType type, out short address))
                //{
                //    var rtn = PLC.ReadRegister(type, address, 1, out byte[] data, out bool state);

                //    Info($"CheckDutReady. Modbus Read {reg1}. rtn {string.Join(" ", rtn.Select(x => x.ToString("x02")).ToArray())}");
                //    state = (rtn[0] & 0x01) == 0x01;
                //}
                
            }
            //DutInDone?.Invoke(this, new DutMessage() { SocketIndex = slot });
            return 1;
        }
        [MethodImpl(MethodImplOptions.Synchronized)]
        public int Clear()
        {
            PLC?.Clear();
            //Simulator?.Close();
            Cleared?.Invoke(this, null);
            return 1;
        }
        [MethodImpl(MethodImplOptions.Synchronized)]
        public int Close()
        {
            if(IsOpen) PLC?.Close();
            return 1;
        }

        public int CloseFrontDoor(int slot = 0)
        {
            Open();
            var rtn = 0;

            if (PLC is null)
            { }
            else
            {
                FrontDoorClosing?.Invoke(this, new DutMessage());
                var reg1 = ManipulationConfig.OutCommands["Door Reg1"];
                var reg2 = ManipulationConfig.OutCommands["Door Reg2"];

                if (PLC_Mitsubishi_FX.ParseChannel(reg1, out PLC_Mitsubishi_FX.RegisterType type, out short address) &
                    PLC_Mitsubishi_FX.ParseChannel(reg2, out PLC_Mitsubishi_FX.RegisterType type2, out short address2))
                {
                    rtn = PLC.ForceStatus(type, address, true);
                    Thread.Sleep(1000);
                    rtn = PLC.ForceStatus(type2, address2, false);

                    GetStateFrontDoorClose(out bool state, slot);
                }
            }
            //FrontDoorClosed?.Invoke(this, new DutMessage());  // trig on get state
            Close();
            return rtn;
        }

        public int CloseRearDoor(int slot = 0)
        {
            return 1;
        }

        public int DutIn(int slot = 0)
        {
            //if (string.IsNullOrEmpty(ManipulationConfig.OutCommands["Dut Pallet"]))
            //{
            //}
            return 1;
        }

        public int DutOut(int slot = 0)
        {
            //if (string.IsNullOrEmpty(ManipulationConfig.OutCommands["Dut Pallet"]))
            //{
            //}
            return 1;
        }

        public int EmergencyStop()
        {
            return 1;
        }

        /// <summary>
        /// FB,LS200,V1.1
        /// </summary>
        /// <param name="idn"></param>
        /// <returns></returns>
        public int GetIDN(out string idn)
        {
            idn = SN;

            PLC.ReadRegister(RegisterType.D, 250, 0x10, out byte[] data, out bool state);
            var str = Encoding.ASCII.GetString(data);

            var newversionstr = string.Join("", Encoding.ASCII.GetBytes("FB,LS200").Select(x=>x.ToString("X02")));

            IsFbV1 = str.StartsWith(newversionstr);

            return 1;
        }

        public int GetSlotActiveState(out bool state, int slot = 0)
        {
            state = true;
            return 1;
        }

        public int GetStateDutIn(out bool state, int slot = 0)
        {
            //if (!string.IsNullOrEmpty(ManipulationConfig.InCommands["Dut In"]))
            //{
            //    var rtn = Query(ManipulationConfig.InCommands["Dut In"]);

            //    state = rtn == "";
            //    return 1;
            //}
            state = true;
            return 1;
        }

        public int GetStateDutOut(out bool state, int slot = 0)
        {
            //if (!string.IsNullOrEmpty(ManipulationConfig.InCommands["Dut Out"]))
            //{
            //    var rtn = Query(ManipulationConfig.InCommands["Dut Out"]);

            //    state = rtn == "";
            //    return 1;
            //}
            state = true;
            return 1;
        }

        public int GetStateDutPresent(out bool state, int slot = 0)
        {
            //if (!string.IsNullOrEmpty(ManipulationConfig.InCommands["Dut Present"]))
            //{
            //    var rtn = Query(ManipulationConfig.InCommands["Dut Present"]);

            //    state = rtn == "";
            //    return 1;
            //}
            state = true;
            return 1;
        }

        public int SetLockState(int slotindex, bool islocked)
        {
            if (IsFbV1)
            {
            }
            else
            {
            }

            return 1;
        }

        //private int SetLockState_Modbus(int slotindex, bool islocked)
        //{
        //    throw new NotImplementedException();
        //}

        //private int SetLockState_PLC(int slotindex, bool islocked)
        //{
        //    Open();
        //    var reg1 = ManipulationConfig.OutCommands["Door Reg1"];
        //    var reg2 = ManipulationConfig.OutCommands["Door Reg2"];
        //    var rtn = 0;
        //    if (PLC_Mitsubishi_FX.ParseChannel(reg1, out PLC_Mitsubishi_FX.RegisterType type, out short address) &
        //        PLC_Mitsubishi_FX.ParseChannel(reg2, out PLC_Mitsubishi_FX.RegisterType type2, out short address2))
        //    {
        //        if (islocked)  // with order
        //        {
        //            //rtn = PLC.ForceStatus(type2, address2, false);  // Reg1 False, Reg2 On, Open Door, In Auto Mode 
        //            rtn = PLC.ForceStatus(type, address, true);
        //        }
        //        else
        //        {

        //            rtn = PLC.ForceStatus(type2, address2, false);   // Both ON, Enable Manually Open/Close Door
        //            rtn = PLC.ForceStatus(type, address, false);  // Reg1 False, Reg2 On, Open Door, In Auto Mode 
        //        }

        //    }
        //    Close();

        //    return rtn;
        //}

        public int GetLockState(int slotindex, out bool islocked)
        {
            islocked = true;
            if(IsFbV1)
            {
            }

            //Open();
            //var ch = ManipulationConfig.InCommands["Door Reg1"];

            //PLC_Mitsubishi_FX.ParseChannel(ch, out PLC_Mitsubishi_FX.RegisterType type, out short address);
            //var rtn = PLC.ReadRegister(type, address, 1, out _, out islocked);
            //Close();
            return 1;
        }

        public int GetStateFrontDoorClose(out bool state, int slot = 0)
        {
            Open();
            state = false;
            if (IsFbV1)
            {
            }
            else
            {
                var ch = ManipulationConfig.InCommands["Door Closed"];

                PLC_Mitsubishi_FX.ParseChannel(ch, out PLC_Mitsubishi_FX.RegisterType type, out short address);

                var data = PLC.ReadRegister(type, address, 1, out _, out state);
            }

            if (state)
            {
                FrontDoorClosed?.Invoke(this, new DutMessage() { SocketIndex = slot, Message = "Front Door Closed" });
            }

            Close();

            return 1;
        }

        public int GetStateFrontDoorOpen(out bool state, int slot = 0)
        {
            //if (!string.IsNullOrEmpty(ManipulationConfig.InCommands["FrontDoor Opened"]))
            //{
            //    var rtn = Query(ManipulationConfig.InCommands["FrontDoor Opened"]);

            //    state = rtn == "";
            //    return 1;
            //}
            state = true;
            return 1;
        }

        public int GetStateRearDoorClose(out bool state, int slot = 0)
        {
            //if (!string.IsNullOrEmpty(ManipulationConfig.InCommands["RearDoor Closed"]))
            //{
            //    var rtn = Query(ManipulationConfig.InCommands["RearDoor Closed"]);

            //    state = rtn == "";
            //    return 1;
            //}
            state = true;
            return 1;
        }

        public int GetStateRearDoorOpen(out bool state, int slot = 0)
        {
            //if (!string.IsNullOrEmpty(ManipulationConfig.InCommands["RearDoor Opened"]))
            //{
            //    var rtn = Query(ManipulationConfig.InCommands["RearDoor Opened"]);



            //    state = rtn == "";
            //    return 1;
            //}
            state = true;
            return 1;
        }

        public int GetStateSafety(out bool state, int slot = 0)
        {
            //if (!string.IsNullOrEmpty(ManipulationConfig.InCommands["Safety"]))
            //{
            //    var rtn = Query(ManipulationConfig.InCommands["Safety"]);

            //    state = rtn == "";
            //    return 1;
            //}
            state = true;
            return 1;
        }

        public int Initialize()
        {
            if(!IsInitialized)
            {
                //if(ToucanCore.HAL.HalHelper.RE_IpAddr.Match(Resource).Success)
                //{
                //    PLC = null;

                //    ModbusFactory factory = new ModbusFactory();
                //    Modbus = factory.CreateMaster(new TcpClient(Resource, 502));
                //    Modbus = NModbus.Serial.ModbusFactoryExtensions.CreateRtuMaster(factory, new SerialPort());
                //}
                //else
                //{
                    //Modbus = null;
                    PLC = new PLC_Mitsubishi_FX();
                    PLC.Resource = Resource;
                    PLC.Initialize();
                //}

                PLC.GetIDN(out string idn);
                IsFbV1 = idn?.StartsWith("Fb, LS200") ?? false;
                
                if (System.IO.File.Exists(FilePath))
                {
                    ManipulationConfig.Update(FixtureManipulationConfig.Load(FilePath));

                    if (int.TryParse(ManipulationConfig.InCommands[PressureKey], out int pressure))
                    {
                        PressureValue = pressure;
                    }
                    if (int.TryParse(ManipulationConfig.InCommands[PositionKey], out int position))
                    {
                        PositionValue = position;
                    }
                }

                //Open();
                //if (PressureValue > 0)
                //{
                //    PLC_Mitsubishi_FX.ParseChannel(PressureRegister, out PLC_Mitsubishi_FX.RegisterType type, out short address);

                //    byte[] data = Encoding.ASCII.GetBytes($"{(PressureValue % 0x100).ToString("x02")}{(PressureValue >> 8).ToString("x02")}");
                //    PLC.WriteRegister(PLC_Mitsubishi_FX.RegisterType.D, address, data);
                //}

                //if (PositionValue > 0)
                //{
                //    PLC_Mitsubishi_FX.ParseChannel(PositionRegister, out PLC_Mitsubishi_FX.RegisterType type, out short address);

                //    byte[] data = Encoding.ASCII.GetBytes($"{(PositionValue % 0x100).ToString("x02")}{(PositionValue >> 8).ToString("x02")}");
                //    PLC.WriteRegister(PLC_Mitsubishi_FX.RegisterType.D, address, data);
                //}

                ////var reg1 = ManipulationConfig.OutCommands["Door Reg1"];
                ////var reg2 = ManipulationConfig.OutCommands["Door Reg2"];
                ////var rtn = 0;
                ////if (PLC_Mitsubishi_FX.ParseChannel(reg1, out PLC_Mitsubishi_FX.RegisterType type1, out short address1) &
                ////    PLC_Mitsubishi_FX.ParseChannel(reg2, out PLC_Mitsubishi_FX.RegisterType type2, out short address2))
                ////{
                ////    rtn = PLC.ForceStatus(type1, address1, true);   // Both ON, Enable Manually Open/Close Door
                ////}

                //Close();
            }
            return 1;            
        }

        /// <summary>
        /// It will reset the position register and pressure register.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public int Open()
        {
            if (!IsOpen)
            {
                PLC?.Open();
                //SetLockState(0, false);
            }
            return IsOpen ? 1 : 0;
        }

        public int OpenFrontDoor(int slot = 0)
        {
            Open();
            FrontDoorOpening?.Invoke(this, new DutMessage());
            var rtn = 0;
            if (PLC is null)
            { 
            }
            else
            {
                var reg1 = ManipulationConfig.OutCommands["Door Reg1"];
                var reg2 = ManipulationConfig.OutCommands["Door Reg2"];
                
                if (PLC_Mitsubishi_FX.ParseChannel(reg1, out PLC_Mitsubishi_FX.RegisterType type, out short address) &
                    PLC_Mitsubishi_FX.ParseChannel(reg2, out PLC_Mitsubishi_FX.RegisterType type2, out short address2))
                {
                    //PLC.ForceStatus(type, address, false);
                    PLC.ForceStatus(type2, address2, true);
                    Thread.Sleep(2000);
                    //PLC.ForceStatus(type, address, true);
                    //PLC.ForceStatus(type, address, false);
                    PLC.ForceStatus(type2, address2, false);
                }

                //var reg2 = ManipulationConfig.OutCommands["Door Reg2"];
                //var rtn = 0;
                //if (PLC_Mitsubishi_FX.ParseChannel(reg2, out PLC_Mitsubishi_FX.RegisterType type2, out short address2))
                //{

                //    rtn = PLC.ForceStatus(type2, address2, true);  // Reg1 False, Reg2 On, Open Door, In Auto Mode             }
                //    Thread.Sleep(1000);
                //    rtn = PLC.ForceStatus(type2, address2, false);
                //}
            }
            FrontDoorOpened?.Invoke(this, new DutMessage());
            Info($"Open Front Door");
            Close();
            return rtn;
        }

        public int OpenRearDoor(int slot = 0)
        {
            //if (!string.IsNullOrEmpty(ManipulationConfig.OutCommands["RearDoor"]))
            //{
            //    var rtn = Query(ManipulationConfig.InCommands["RearDoor"]);
            //    return 1;
            //}
            return 1;
        }

        public int SettingUI()
        {
            try
            {
                FixtureManipulationSetting setting = new FixtureManipulationSetting(FilePath, ManipulationConfig);

                return setting.ShowDialog() == true ? 1 : 0;
            }
            catch
            {
                return -1;
            }
        }
    }
}
