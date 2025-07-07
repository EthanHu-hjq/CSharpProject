
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TestCore.Services;

namespace ToucanCore.Owl
{
    public class ToucanOwlService : ILogicStationService
    {
        public StationType StationType => StationType.Test;

        public StationWorkStatus WorkStatus { get; set; }

        public StationWorkMode WorkMode => throw new NotImplementedException();

        public bool OnProcessing => throw new NotImplementedException();

        public string Customer => throw new NotImplementedException();

        public string Product => throw new NotImplementedException();

        public string Station => throw new NotImplementedException();

        public string Software => throw new NotImplementedException();

        public int RequestTimeout => throw new NotImplementedException();

        public string Name => throw new NotImplementedException();

        public bool IsInitialized => throw new NotImplementedException();

        public bool IsStarted => throw new NotImplementedException();

        public bool IsBusy => throw new NotImplementedException();

        public event EventHandler ProductInputComplete;
        public event EventHandler ProductOutputComplete;
        public event ServiceEventHandler ServiceInitialized;
        public event ServiceEventHandler ServiceStarted;
        public event ServiceEventHandler ServiceStopped;
        public event ServiceEventHandler ServiceDisposed;
        public event ServiceEventHandler ServiceWarning;

        public int Abort(int slot = -1)
        {
            throw new NotImplementedException();
        }

        public int Clear()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public int GetSlotStatus(int slot = -1)
        {
            throw new NotImplementedException();
        }

        public int Initialize()
        {
            throw new NotImplementedException();
        }

        public int InputProductRequest(string sn, int slot = -1)
        {
            throw new NotImplementedException();
        }

        public int LoadConfiguration()
        {
            throw new NotImplementedException();
        }

        public int LocateControl()
        {
            throw new NotImplementedException();
        }

        public int OutputProductRequest(out int productgrade, int slot = -1)
        {
            throw new NotImplementedException();
        }

        public Task StartAsync()
        {
            throw new NotImplementedException();
        }

        public int StartProcess(int slot = -1)
        {
            throw new NotImplementedException();
        }

        public Task StopAsync()
        {
            throw new NotImplementedException();
        }

        public int StopProcess(int slot = -1)
        {
            throw new NotImplementedException();
        }

        public int UpdateMesResponse(int slot = -1)
        {
            throw new NotImplementedException();
        }
    }
}
