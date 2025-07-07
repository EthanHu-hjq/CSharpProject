using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using TestCore;
using TestCore.Configuration;
using TestCore.Data;
using TestCore.Abstraction.Process;
using TestCore.Abstraction.Configuration;
using TestCore.Abstraction.Data;

namespace Mes
{
    public class PrimaxWebApi : TF_Base, IMes
    {
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        public string Root_URL = "http://10.80.1.27:9527/SMT/api/";
        public string ApiVersion = "1.1";
        public string Token = "GMGkWIRzlwRqpd5d9CXYLj3r4k1S/9o9N+c8DG0PtGs=";

        public const string API_GetPlant = "SFCExchange/GetPlant";

        public const string API_Checkstation = "SFCS/CheckStation";
        public const string API_GetModel = "SFCS/GetModel";
        public const string API_GetLine = "SFCS/GetLine";
        public const string API_GetStation = "SFCS/GetStation";
        public const string API_GetSpecialValue = "SFCS/GetSpecialValue";
        public const string API_InsertIntoTable = "SFCS/InsertIntoTable";

        const string SFCsHeader = "BARCODE_PART,BARCODE,Model_NO,MACHINE_ID,Line_Code,Station_Code,PROCESS_NO,Result,Defect_Code,Defect_Info";

        private readonly object checkingSfcsLock = new object();
        private readonly object commitingSfcsLock = new object();

        public bool IsForValidation { get; set; }

        public void CheckStation(IMesConfig sfcconfig, string sn, string lineno)
        {
            var apicontent = $"station={HttpUtility.UrlEncode(sfcconfig.Station)}&barcode={HttpUtility.UrlEncode(sn)}&line={HttpUtility.UrlEncode(lineno ?? sfcconfig.PersitLineNo)}&version={HttpUtility.UrlEncode("1.0")}";

            HttpWebRequest request = HttpWebRequest.Create($"{Root_URL}{ApiVersion}/{API_Checkstation}?{apicontent}") as HttpWebRequest;

            request.Method = "Get";
            request.Accept = "application/json";
            request.Headers.Add("Authorization", $"baseAuth {Token}");

            var rtn = RequestResponse(request);

            if (rtn == "\"Y\"")
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

        private string RequestResponse(HttpWebRequest request)
        {
            string result;
            try
            {
                HttpWebResponse httpWebResponse = (HttpWebResponse)request.GetResponse();
                string text = string.Empty;
                if (httpWebResponse.StatusCode == HttpStatusCode.OK)
                {
                    using (Stream responseStream = httpWebResponse.GetResponseStream())
                    {
                        if (responseStream == null)
                        {
                            return string.Empty;
                        }
                        using (StreamReader streamReader = new StreamReader(responseStream))
                        {
                            text = streamReader.ReadToEnd();
                        }
                    }
                }
                else
                {
                    Warn($"WebApi Response Status {httpWebResponse.StatusCode}. {httpWebResponse.StatusDescription}");
                }

                result = text;
            }
            catch (WebException ex)
            {
                string text2 = "";
                using (Stream responseStream2 = ex.Response.GetResponseStream())
                {
                    if (responseStream2 == null)
                    {
                        return string.Empty;
                    }
                    using (StreamReader streamReader2 = new StreamReader(responseStream2))
                    {
                        text2 = streamReader2.ReadToEnd();
                    }
                }
                if (text2.Contains("<!DOCTYPE html>"))
                {
                    result = string.Format("{0}\"rlt\":false,\"msg\":\"{2}\"{1}", "{", "}", ex.Message);
                }
                else
                {
                    result = text2;
                }
            }
            catch (Exception ex2)
            {
                result = string.Format("{0}\"rlt\":false,\"msg\":\"{2}\"{1}", "{", "}", ex2.Message);
            }

            return result;
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
                        //var rtn = InsertIntoTable(rs.SFCsConfig.Product, rs.SFCsConfig.SfcsTable, sfccol, sfcdata);
                        //var apicontent = $"product={HttpUtility.UrlEncode(tymrs.SFCsConfig.Product)}";

                        //HttpWebRequest request = HttpWebRequest.Create($"{Root_URL}{ApiVersion}/{API_InsertIntoTable}?{apicontent}") as HttpWebRequest;

                        HttpWebRequest request = HttpWebRequest.Create($"{Root_URL}{ApiVersion}/{API_InsertIntoTable}") as HttpWebRequest;

                        request.Method = "Post";
                        request.Accept = "application/json";
                        request.Headers.Add("Authorization", $"baseAuth {Token}");

                        string content = $"{{\"table\": \"{tymrs.SFCsConfig.SfcsTable}\",\"colNames\": \"{sfccol}\",\"values\": \"{sfcdata}\"}}";

                        byte[] bytes = Encoding.GetBytes(content);
                        request.ContentLength = bytes.Length;
                        request.ContentType = "application/json";

                        using (Stream requestStream = request.GetRequestStream())
                        {
                            requestStream.Write(bytes, 0, bytes.Length);
                        }

                        var rtn = RequestResponse(request);

                        if (rtn != "\"Y\"")
                        {
                            throw new MesException($"{GetType().Name} InsertIntoTable Failed(过站失败). Product: {tymrs.SFCsConfig.Product}, Table: {tymrs.SFCsConfig.SfcsTable}, Col: {sfccol}, Val: {sfcdata}. SFCs Return :{rtn}");
                        }
                    }
                }
            }
        }

        public string ExecMesApi(IMesConfig sfcconfig, string api, params string[] parameters)
        {
            var para = string.Join(",", parameters);
            Debug(string.Format("GetSpecialValue params:{0}, {1}, {2}", sfcconfig.Product, api, para));

            var apicontent = $"type={HttpUtility.UrlEncode(api)}&parameters={HttpUtility.UrlEncode(string.Join(",", para))}&product={HttpUtility.UrlEncode(sfcconfig.Product ?? sfcconfig.PersitLineNo)}";

            HttpWebRequest request = HttpWebRequest.Create($"{Root_URL}{ApiVersion}/{API_GetSpecialValue}?{apicontent}") as HttpWebRequest;

            request.Method = "Get";
            request.Accept = "application/json";
            request.Headers.Add("Authorization", $"baseAuth {Token}");

            var rtn = RequestResponse(request);

            if (rtn == "\"Y\"")
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

            Debug($"GetSpecialValue rtn: {rtn}");

            return rtn;
        }

        public DateTime GetDate(IMesConfig sfcconfig)
        {
            throw new NotImplementedException();
        }

        public string GetLineNo(IMesConfig sfcconfig, string sn)
        {
            throw new NotImplementedException();
        }

        public string GetPartNo(IMesConfig sfcconfig, string sn)
        {
            throw new NotImplementedException();
        }

        Dictionary<IMesConfig, string> DataColumnSet = new Dictionary<IMesConfig, string>();
        public void Initialize(IMesConfig sfcconfig, string datacolumn)
        {
            if (!DataColumnSet.ContainsKey(sfcconfig))
            {
                DataColumnSet.Add(sfcconfig, datacolumn);
            }
        }
    }
}
