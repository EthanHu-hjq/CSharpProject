using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ToucanCore.Abstraction.HAL;

namespace ToucanCore.HAL
{
    //public enum TriggerStartTestType
    //{
    //    None,
    //    Fixture,
    //    Keyboard,
    //    ApxAuxIn,
    //    External,
    //}

    //public interface IStartTrigger
    //{
    //    string Name { get; }
    //    TriggerStartTestType TriggerType { get; }
    //    string[] TriggerSources { get; }

    //    string Source { get; set; }

    //    void Initialize();
    //    void Clear();

    //    /// <summary>
    //    /// the callback when trigger signal happened
    //    /// </summary>
    //    EventHandler<DutMessage> StartTrigged { get; set; } // event will enable multiple trigger
    //    //int StartWaitingOnceTrigger();
    //    //void StartTriggedRecieve(object sender, TestCore.HAL.DutMessage e);
    //}

    /// <summary>
    /// No Trigger, Start Test Immediately
    /// </summary>
    public class StartTrigger_None : IStartTrigger
    {
        public readonly static StartTrigger_None Instance = new StartTrigger_None();

        public EventHandler<DutMessage> StartTrigged { get; set; }

        public string Name { get; } = "None";

        public string[] TriggerSources { get; } = new string[] { };
        public string Source { get; set; }

        public TriggerStartTestType TriggerType => TriggerStartTestType.None;

        private StartTrigger_None() { }

        public void Initialize()
        {
            //StartTrigged?.Invoke(this, null);
        }

        public void Clear()
        {
        }
    }

    /// <summary>
    /// Trigger by Key Down Event
    /// </summary>
    public class StartTrigger_Keyboard : IStartTrigger
    {
        public readonly static StartTrigger_Keyboard Instance = new StartTrigger_Keyboard();

        public EventHandler<DutMessage> StartTrigged { get; set; }

        public string Name { get; } = "Keyboard";
        public TriggerStartTestType TriggerType => TriggerStartTestType.Keyboard;
        public string[] TriggerSources { get; } = new string[] { "Z", "0", "[Space]" };
        private System.Windows.Input.Key[] _Sources { get; } = new System.Windows.Input.Key[] { System.Windows.Input.Key.Z, System.Windows.Input.Key.D0, System.Windows.Input.Key.Space };
        public string Source { get; set; }
        private System.Windows.Input.Key _Source { get; set; }
        private StartTrigger_Keyboard() 
        {
            
        }

        private void MainWindow_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == _Source)
            {
                StartTrigged?.Invoke(this, new DutMessage() { SocketIndex = -1, Message="Start All"});
            }
        }

        public void Initialize()
        {
            if (Application.Current?.MainWindow != null)
            {
                if (Source == "Z")
                {
                    _Source = System.Windows.Input.Key.Z;
                }
                else if (Source == "0")
                {
                    _Source = System.Windows.Input.Key.NumPad0;
                }
                else if (Source == "[Space]")
                {
                    _Source = System.Windows.Input.Key.Space;
                }
                Application.Current.MainWindow.PreviewKeyUp += MainWindow_KeyUp;   // Need to be PreviewKeyDown, otherwise the KeyDown will filter the function key such as Space, return
            }
        }

        public void Clear()
        {
            if (Application.Current?.MainWindow != null)
            {
                Application.Current.MainWindow.PreviewKeyUp -= MainWindow_KeyUp;   // Need to be PreviewKeyDown, otherwise the KeyDown will filter the function key such as Space, return
            }
        }
    }

    /// <summary>
    /// Trigger by Fixture event
    /// </summary>
    public class StartTrigger_Fixture : IStartTrigger
    {
        public readonly static StartTrigger_Fixture Instance = new StartTrigger_Fixture();
        public string Name { get; } = "Fixture";
        public TriggerStartTestType TriggerType => TriggerStartTestType.Fixture;
        public string[] TriggerSources { get; } = new string[] { FixtureStartTriggerType.Door_Closing.ToString(), FixtureStartTriggerType.Door_Closed.ToString(), FixtureStartTriggerType.Dut_Ready.ToString() };
        public string Source { get; set; }

        public IFixture Fixture { get; set; }

        private StartTrigger_Fixture() { }

        public EventHandler<DutMessage> StartTrigged { get; set; }

        public void Initialize()
        {
            if (Fixture is null) return;

            if (Enum.TryParse(Source, out FixtureStartTriggerType sttype))
            {
                switch (sttype)
                {
                    case FixtureStartTriggerType.Door_Closing:
                        Fixture.FrontDoorClosing += StartTrigged;
                        break;
                    case FixtureStartTriggerType.Door_Closed:
                        Fixture.FrontDoorClosed += StartTrigged;
                        break;
                    case FixtureStartTriggerType.Dut_Ready:
                        Fixture.DutInDone += StartTrigged;
                        break;

                }
            }
        }

        public void Clear()
        {
            if (Fixture is null) return;

            if (Enum.TryParse(Source, out FixtureStartTriggerType sttype))
            {
                switch (sttype)
                {
                    case FixtureStartTriggerType.Door_Closing:
                        Fixture.FrontDoorClosing -= StartTrigged;
                        break;
                    case FixtureStartTriggerType.Door_Closed:
                        Fixture.FrontDoorClosed -= StartTrigged;
                        break;
                    case FixtureStartTriggerType.Dut_Ready:
                        Fixture.DutInDone -= StartTrigged;
                        break;
                }
            }
        }

        public enum FixtureStartTriggerType
        {
            Door_Closing,
            Door_Closed,
            Dut_Ready,
        }
    }

    public class StartTrigger_Ap : IStartTrigger
    {
        public readonly static StartTrigger_Ap Instance = new StartTrigger_Ap();

        public EventHandler<DutMessage> StartTrigged { get; set; }

        public string Name { get; } = "Ap AuxIn";
        public TriggerStartTestType TriggerType => TriggerStartTestType.ApxAuxIn;
        public string[] TriggerSources { get; } = new string[] { ApxAuxIn.Aux_In_1.ToString(), ApxAuxIn.Aux_In_2.ToString(), ApxAuxIn.Aux_In_3.ToString(), ApxAuxIn.Aux_In_4.ToString(), ApxAuxIn.Aux_In_5.ToString(), ApxAuxIn.Aux_In_6.ToString(), ApxAuxIn.Aux_In_7.ToString(), ApxAuxIn.Aux_In_8.ToString() };
        public string Source { get; set; }
        private StartTrigger_Ap() { }

        public void Initialize()
        {
            if (Enum.TryParse(Source, out ApxAuxIn sttype))
            {

            }
        }

        public void Clear()
        {
            
        }

        public enum ApxAuxIn
        {
            Aux_In_1,
            Aux_In_2,
            Aux_In_3,
            Aux_In_4,
            Aux_In_5,
            Aux_In_6,
            Aux_In_7,
            Aux_In_8,
        }
    }

    /// <summary>
    /// TODO: If the digital signal trigget by state, or raise/fall edge
    /// </summary>
    public class StartTrigger_Exteranl : IStartTrigger
    {
        public readonly static StartTrigger_Exteranl Instance = new StartTrigger_Exteranl();

        public event EventHandler Trigged;
        public EventHandler<DutMessage> StartTrigged { get; set; }

        public string Name { get; } = "Exteranl";
        public TriggerStartTestType TriggerType => TriggerStartTestType.External;
        public string[] TriggerSources { get; } = new string[] { "PLC X1", "PLC X2", "PLC X3", "PLC X4", "PLC X5", "PLC X6", "PLC X7", "PLC X8" };
        public string Source { get; set; }
        private StartTrigger_Exteranl() { }

        public int StartWaitingOnceTrigger()
        {
            throw new NotImplementedException();
        }

        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }
    }
}
