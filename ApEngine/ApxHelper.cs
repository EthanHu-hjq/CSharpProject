using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestCore.MetaData;
using TestCore.Misc;
using TestCore.Services;
using TestCore;
using System.Windows;
using ApEngine.UIs;

namespace ApEngine
{
    public static class ApxHelper
    {
        static TestCore.Services.IToolboxService Toolbox = ServiceStatic.ToolboxService();
        //public static bool Push(string pspFilePath)
        //{
        //    if (Toolbox.ExecuteSoftware is null) return false;

        //    try
        //    {
        //        var psp = new PspFile(pspFilePath);
        //        if (Toolbox.PushPspDialog(Toolbox.ExecuteSoftware.Station, psp) == true)
        //        {
        //            return true;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        TF_Base.StaticLog($"Push Error. Err: {ex}");
        //    }
        //    return false;
        //}

        public static void CalibrationEquipment()
        {
            if (Toolbox.StationInstance?.Equipments?.FirstOrDefault() is StationInstanceBomItem sibi)
            {
                var cal = new ApxCalibration(sibi.Instance);
                if (cal.ShowDialog() == true)
                {
                    MessageBox.Show("Warning", "Calibration Changed. Please Reload Project");
                }
            }
        }
    }
}
