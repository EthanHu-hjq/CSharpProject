using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestCore.Services;

// this is for verification
// it will move the owl project

namespace ToucanCore.Owl
{
    /// <summary>
    /// For Toucan or AAT, kinds of logic station
    /// </summary>
    public interface ILogicStationService : IService
    {
        StationType StationType { get; }
        StationWorkStatus WorkStatus { get; }

        /// <summary>
        /// Should forbid move manual to auto remotely, for safety 
        /// </summary>
        StationWorkMode WorkMode { get; }

        bool OnProcessing { get; }

        /// <summary>
        /// Current Setting of Customer
        /// </summary>
        string Customer { get; }
        string Product { get; }
        string Station { get; }
        /// <summary>
        /// Current Setting of Software UniquName
        /// </summary>
        string Software { get; }


        int RequestTimeout { get; }

        /// <summary>
        /// Send request for inputing product. wait for response
        /// </summary>
        /// <param name="sn"></param>
        /// <param name="slot"></param>
        /// <returns> greate equal 1: request accepted. Nagetive: reject code</returns>
        int InputProductRequest(string sn, int slot = -1);

        /// <summary>
        /// Send request for outputing product. wait for response
        /// </summary>
        /// <param name="productgrade">greate equal 1: OK Unit. Nagative: Abnormal Unit. Could be for classficition</param>
        /// <param name="slot"></param>
        /// <returns></returns>
        int OutputProductRequest(out int productgrade, int slot = -1);

        /// <summary>
        /// Start a new process for product unit, Only For Debug Mode
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        int StartProcess(int slot = -1);

        int StopProcess(int slot = -1);

        int Abort(int slot = -1);

        int GetSlotStatus(int slot = -1);

        int UpdateMesResponse(int slot = -1);

        /// <summary>
        /// Quit Remote Control mode, and unlock the local actions
        /// </summary>
        /// <returns></returns>
        int LocateControl();

        int LoadConfiguration();

        /// <summary>
        /// Product Input Complete. Which means the Station will reject the input before it output the Product
        /// </summary>
        event EventHandler ProductInputComplete;

        /// <summary>
        /// Product Output Complete. Which means the Station will could a new input product request
        /// </summary>
        event EventHandler ProductOutputComplete;
    }

    public enum StationWorkMode
    {
        Manual,
        Debug,
        Auto
    }

    public enum StationWorkStatus
    {
        Initializing,
        Normal,
        Warning,
        Error,
    }

    public enum StationType
    {
        None,
        Test,
        Process,


        Unknown = 0xFF
    }
}
