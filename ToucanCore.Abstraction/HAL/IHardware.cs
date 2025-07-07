using System;
using System.Collections.Generic;
using System.Text;

namespace ToucanCore.Abstraction.HAL
{
    public interface IHardware
    {
        // if specified version, it mean this only for this model
        string Model { get; }

        // Hardware serialnumber
        string SN { get; }

        /// <summary>
        /// Resource is a unique string which can identify the Instr.
        /// And you can customize it to let it carry more information.
        /// e.g. "COM4,119200,EVEN"
        /// e.g. "192.168.100.2"
        string Resource { get; set; }

        event EventHandler Initializing;
        event EventHandler Initialized;
        event EventHandler Cleared;

        bool IsOpen { get; }
        bool IsInitialized { get; }
        /// <summary>
        /// the initialize will try to allocate the local resource and do the pre action like configuration.
        /// </summary>
        /// <returns></returns>
        int Initialize();

        /// <summary>
        /// the clear will try to do the post action and dispose the resource allocated
        /// </summary>
        /// <returns></returns>
        int Clear();

        /// <summary>
        /// open/create the task, which will make the instrument occupied.
        /// in this action. a reset is recommended.
        /// </summary>
        /// <returns></returns>
        int Open();

        /// <summary>
        /// close/finish the task, whilc will make the instrument released.
        /// in this action. a reset is recommended.
        /// </summary>
        /// <returns></returns>
        int Close();

        /// <summary>
        /// To get the instrument info. Normally in SCPI, it's the response of cmd "*IDN?"
        /// </summary>
        /// <param name="idn">Hardware info</param>
        /// <returns></returns>
        int GetIDN(out string idn);
    }
}
