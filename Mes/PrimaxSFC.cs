using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestCore;
using TestCore.Data;
using Primax.SFCS.DLL;
using TestCore.Configuration;
using TestCore.Abstraction.Process;
using TestCore.Abstraction.Configuration;
using TestCore.Abstraction.Data;

namespace Mes
{
    public sealed class PrimaxSFC : TF_Base, IMes
    {
        public static SMTDLL Server { get; private set; }
        public const string DEFAULT_GET_PART_API = "Get_model";
        public static string Version = "1.0";
        public static int ConnectionTimeOut_ms = 5000;

        const string SFCsHeader = "BARCODE_PART,BARCODE,Model_NO,MACHINE_ID,Line_Code,Station_Code,PROCESS_NO,Result,Defect_Code,Defect_Info";
        public bool IsForValidation { get; set; }

        private readonly object checkingSfcsLock = new object();
        private readonly object commitingSfcsLock = new object();

        public PrimaxSFC()
        {
            Info($"Init SFC, Location {GlobalConfiguration.Default.Station.Location}");
            Info($"SFCsHeader: {SFCsHeader}");
            if (Server is null)
            {
                Info("Try Connect to SFCs Server");

                var d = Task.Run(() => { Server = new SMTDLL(); });

                d.Wait(ConnectionTimeOut_ms);

                if (d.IsCompleted)
                {
                    Info("SFCs Connection Established");
                }
                else
                {
                    var err = $"{GetType().Name} Init SFCs Connect Failed. Please check the if the SFCs is valid";
                    Error(err);

                    throw (new InvalidProgramException(err));
                }
            }
        }

        Dictionary<IMesConfig, string> DataColumnSet = new Dictionary<IMesConfig, string>();
        public void Initialize(IMesConfig sfcconfig, string datacolumn)
        {
            if (!DataColumnSet.ContainsKey(sfcconfig))
            {
                DataColumnSet.Add(sfcconfig, datacolumn);
            }
        }

        public DateTime GetDate(IMesConfig sfcconfig)
        {
            return DateTime.Now;
        }

        private string sn_partno_lineno = null;
        private string partno = null;
        private string lineno = null;
        private void GetModel(IMesConfig sfcconfig, string sn)
        {
            Debug($"GetModel params:{sfcconfig.Product}|{sn}");
            try
            {
                string rtn = null;
                lock (checkingSfcsLock)
                {
                    rtn = Server.GetSpecialValue(sfcconfig.Product, sn);
                }

                var componenet = rtn.Split(',');

                if (rtn.Contains("error") || componenet.Length < 2)
                {
                    throw new MesException($"GetModel Failed(获取机型码失败). SFCs Return :{rtn}");
                }
                else if (componenet[0].Length < 1)
                {
                    throw new MesException($"GetModel Failed(获取机型码失败). PartNo Is Empty. SFCs Return :{rtn}");
                }
                else if (componenet[1].Length < 1)
                {
                    throw new MesException($"GetModel Failed(获取机型码失败). LineNo Is Empty. SFCs Return :{rtn}");
                }
                else
                {
                    Debug(string.Format("GetModel rtn: {0}", rtn));
                    partno = componenet[0].Trim();
                    lineno = componenet[1].Trim();
                    sn_partno_lineno = sn;
                }
            }
            catch (Exception ex)
            {
                Error(ex);
                throw ex;
            }
        }

        public string GetPartNo(IMesConfig sfcconfig, string sn)
        {
            return sfcconfig.PersitPartNo;
            if (sn == sn_partno_lineno)
            {
                Info($"Same SN {sn}, fetch PartNo {partno} from memory");
            }
            else
            {
                GetModel(sfcconfig, sn);
            }
            return partno;
        }

        public string GetLineNo(IMesConfig sfcconfig, string sn)
        {
            return sfcconfig.PersitLineNo;
            //if (sn == sn_partno_lineno)
            //{
            //    Info($"Same SN {sn}, fetch LineNo {lineno} from memory");
            //}
            //else
            //{
            //    GetModel(sfcconfig, sn);
            //}
            //return lineno;
        }

        public void CheckStation(IMesConfig sfcconfig, string sn, string lineno)
        {
            Debug($"CheckStation params:{sfcconfig.Product}|{sfcconfig.Station}|{sn}|{lineno}");
            try
            {
                string rtn = null;
                lock (checkingSfcsLock)
                {
                    rtn = Server.CheckStationPass(sfcconfig.Product, sfcconfig.Station, sn, lineno);
                }

                if (rtn == "Y")
                {
                    Debug($"CheckStation rtn: {rtn}");
                }
                else
                {
                    if (IsForValidation)
                    {
                        Info($"ValidationMode, Ignore Check Station result. rtn: {rtn}");
                    }
                    else
                    {
                        throw new MesException($"{GetType().Name} CheckStation Failed(过站失败). SFCs Return :{rtn}");
                    }
                }
            }
            catch (Exception ex)
            {
                Error(ex);
                throw ex;
            }
        }

        public string ExecMesApi(IMesConfig sfcconfig, string api, params string[] parameters)
        {
            var para = string.Join(",", parameters);
            Debug(string.Format("GetSpecialValue params:{0}, {1}, {2}", sfcconfig.Product, api, para));
            var rtn = Server.GetSpecialValue(api, para);
            Debug($"GetSpecialValue rtn: {rtn}");
            return rtn;
        }

        public void CommitMesResult(ITestResult rs, string specialdata, string extcol, string extval)
        {
            string datahader = null;
            var tymrs = rs as TF_Result;

            if (DataColumnSet.ContainsKey(tymrs.SFCsConfig))
            {
                datahader = DataColumnSet[tymrs.SFCsConfig];
            }
            else
            {
                if (tymrs.SFCsConfig.SfcsDataMode.Equals("JDM", StringComparison.OrdinalIgnoreCase))
                {
                    tymrs.GenerateSfcHeader_JDM(out string header);
                    datahader = header;
                }
                else
                {
                    tymrs.GenerateSFCHeader(out string header);
                    datahader = header;
                }

                DataColumnSet.Add(tymrs.SFCsConfig, datahader);
            }

            if (tymrs.SFCsConfig.EnableSfc && tymrs.IsSFC && tymrs.SerialNumber.Length > 2)
            {
                var SFCs_Value = string.Empty;
                if (tymrs.SFCsConfig.SfcsUploadData)
                {
                    string data = null;
                    if (tymrs.SFCsConfig.SfcsDataMode?.ToLower() == "JDM")
                    {
                        tymrs.GenerateSFCValue_JDM(out data);
                    }
                    else
                    {
                        tymrs.GenerateSFCValue(out data);
                    }

                    SFCs_Value = data;
                }
                else
                {
                    datahader = string.Empty;
                }

                var sfcsstaticdata = $"{specialdata},{tymrs.SerialNumber},{tymrs.PartNo},{GlobalConfiguration.Default.SFCs.MachineId ?? Environment.MachineName},{tymrs.LineNo},{tymrs.SFCsConfig.Station},{tymrs.SFCsConfig.Station},{(tymrs.Status == TF_TestStatus.PASSED ? "PASS" : "FAIL")},{tymrs.GetDefectCodes(";", 1)},{tymrs.GetDefectDescs(";", 1)}";

                var sfccol = $"{SFCsHeader}{datahader}{extcol}";
                var sfcdata = $"{sfcsstaticdata}{SFCs_Value}{extval}";

                Info($"SFC Static Data: {sfcsstaticdata}");
                Info($"SFCs_Value: {SFCs_Value}");
                Info($"SFCs_ExtColumn: {extcol}");
                Info($"SFCs_ExtValue: {extval}");

                if (!IsForValidation)
                {
                    lock (commitingSfcsLock)
                    {
                        var rtn = InsertIntoTable(tymrs.SFCsConfig.Product, tymrs.SFCsConfig.SfcsTable, sfccol, sfcdata);
                        if (rtn != "Y")
                        {
                            throw new MesException($"{GetType().Name} InsertIntoTable Failed(过站失败). Product: {tymrs.SFCsConfig.Product}, Table: {tymrs.SFCsConfig.SfcsTable}, Col: {sfccol}, Val: {sfcdata}. SFCs Return :{rtn}");
                        }
                    }
                }
            }
        }

        //public string CheckStationPass(string Product, string Station, string Barcode, string LineNo)
        //{
        //    Product = string.Format("{0}_{1}", GlobalConfiguration.Default.Station.Location, Product);
        //    Debug(string.Format("CheckStation params:{0}, {1}, {2}, {3}, {4}", Product, Station, Barcode, LineNo, Version));
        //    try
        //    {
        //        var rtn = Server.CheckStationPass(Product, Station, Barcode, LineNo, Version);
        //        Debug(string.Format("CheckStation rtn: {0}", rtn));
        //        return rtn;
        //    }
        //    catch (Exception ex)
        //    {
        //        Error(ex);
        //        return ex.Message;
        //    }
        //}

        //public string GetSpecialValue(string Product, string Type, string Parameters)
        //{
        //    Product = $"{GlobalConfiguration.Default.Station.Location}_{Product}";
        //    Debug(string.Format("GetSpecialValue params:{0}, {1}, {2}", Product, Type, Parameters));
        //    var rtn = Server.GetSpecialValue(Product, Type, Parameters);
        //    Debug(string.Format("GetSpecialValue rtn: {0}", rtn));
        //    return rtn;
        //}

        private string InsertIntoTable(string Product, string TableName, string ColumnList, string ValueList)
        {
            Debug(string.Format("InsertIntoTable params:{0}|{1}|{2}|{3}", Product, TableName, ColumnList, ValueList));
            try
            {
                var rtn = Server.InsertIntoTable(Product, TableName, ColumnList, ValueList);
                Debug(string.Format("InsertIntoTable rtn: {0}", rtn));
                return rtn;
            }
            catch (Exception ex)
            {
                Error(ex);
                return ex.Message;
            }
        }

        //public string UpdateValues(string Product, string TableName, string UpdateValues, string WhereValues)
        //{
        //    Product = $"{GlobalConfiguration.Default.Station.Location}_{Product}";
        //    Debug(string.Format("UpdateValues params:{0}|{1}|{2}|{3}", Product, TableName, UpdateValues, WhereValues));
        //    try
        //    {
        //        var rtn = Server.UpdateValues(Product, TableName, UpdateValues, WhereValues);
        //        Debug(string.Format("UpdateValues rtn: {0}", rtn));
        //        return rtn;
        //    }
        //    catch (Exception ex)
        //    {
        //        Error(ex);
        //        return ex.Message;
        //    }
        //}

        //public string GetPartNo(string product, string sn)
        //{
        //    product = $"{GlobalConfiguration.Default.Station.Location}_{product}";
        //    Debug(string.Format("GetPartNo params:{0}|{1}", product, sn));
        //    try
        //    {
        //        var rtn = Server.GetSpecialValue(product, GET_PART_API, sn);
        //        Debug(string.Format("GetPartNo rtn: {0}", rtn));
        //        return rtn;
        //    }
        //    catch (Exception ex)
        //    {
        //        Error(ex);
        //        return ex.Message;
        //    }
        //}
    }
}
