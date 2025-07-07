using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TestCore;
using TestCore.Abstraction.Configuration;
using TestCore.Abstraction.Data;
using TestCore.Abstraction.Process;

namespace MesDemoForVendor
{
    /// <summary>
    /// the Class Name MUST start with the VendorName assigned from TYM
    /// </summary>
    public class VendorMes : IMes
    {
        /// <summary>
        /// Just For Validation. if true. this library could identify if it is true to determinate if really execute the apis
        /// </summary>
        public bool IsForValidation { get; set; }

        /// <summary>
        /// Your connection session object
        /// try to establish the connection. if timeout, throw Exception
        /// </summary>
        //private object Server;

        public VendorMes()
        {
            //Info($"Init MES, Location {GlobalConfiguration.Default.Station.Location}");
            //if (Server is null)
            //{
            //    //Info("Try Connect to SFCs Server");

            //    //var d = Task.Run(() => { Server = new SPD.SQLServer(); });

            //    //d.Wait(ConnectionTimeOut_ms);

            //    //if (d.IsCompleted)
            //    //{
            //    //    Info("MES Connection Established");
            //    //}
            //    //else
            //    //{
            //    //    var err = "Init MES Connect Failed. Please check the if the MES is valid";
            //    //    Error(err);

            //    //    throw (new InvalidProgramException(err));
            //    //}
            //}
        }

        /// <summary>
        /// Check if the SN in line test in the station
        /// </summary>
        /// <param name="mesconfig"></param>
        /// <param name="sn"></param>
        /// <param name="lineno"></param>
        /// <exception cref="MesException"></exception>
        public void CheckStation(IMesConfig mesconfig, string sn, string lineno)
        {
            //Info("CheckStation");
            if (MessageBox.Show($"Check Station for {mesconfig.Product} {sn} in {lineno}. Click Yes for Pass, click No for failed", "CheckStation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
            }
            else
            {
                throw new MesException($"CheckStation Failed. rtn {"Your return"}");
            }
        }

        /// <summary>
        /// Commit test result into MES
        /// </summary>
        /// <param name="rs"></param>
        /// <param name="specialdata">MES声明的特殊字段数据,不用声称字段名称.没有则忽略</param>
        /// <param name="extcol">扩充字段抬头,测试数据中有需要显式声称上传到MES服务器的数据字段抬头</param>
        /// <param name="extval">扩充字段数据,测试数据中有需要显式声称上传到MES服务器的数据字段数据</param>
        public void CommitMesResult(ITestResult rs, string specialdata, string extcol, string extval)
        {
            //Info("CommitMesResult");
        }

        /// <summary>
        /// 用以提供MES自定义扩充API, 比如从MES获取注册码，MAC地址等
        /// </summary>
        /// <param name="mesconfig"></param>
        /// <param name="api">API名称</param>
        /// <param name="parameters">API参数表</param>
        /// <returns>API返回内容</returns>
        public string ExecMesApi(IMesConfig mesconfig, string api, params string[] parameters)
        {
            //Info("ExecMesApi");
            return "ExecMesApi";
        }

        /// <summary>
        /// 获取当前MES配置下的系统时间
        /// </summary>
        /// <param name="mesconfig"></param>
        /// <returns></returns>
        public DateTime GetDate(IMesConfig mesconfig)
        {
            return DateTime.Now;
        }

        /// <summary>
        /// 获取当前SN所属线别
        /// </summary>
        /// <param name="mesconfig"></param>
        /// <param name="sn"></param>
        /// <returns></returns>
        public string GetLineNo(IMesConfig mesconfig, string sn)
        {
            //Info("GetLineNo");
            return "GetLineNo";
        }

        /// <summary>
        /// 获取当前SN的料号
        /// </summary>
        /// <param name="mesconfig"></param>
        /// <param name="sn"></param>
        /// <returns></returns>
        public string GetPartNo(IMesConfig mesconfig, string sn)
        {
            //Info("GetPartNo");
            return "GetPartNo";
        }

        /// <summary>
        /// 初始化, 写入对应Mes配置重需要上传的数据抬头
        /// </summary>
        /// <param name="mesconfig"></param>
        /// <param name="datacolumn">需上传MES的测试数据抬头，如无则为空</param>
        public void Initialize(IMesConfig mesconfig, string datacolumn)
        {
            
        }
    }
}
