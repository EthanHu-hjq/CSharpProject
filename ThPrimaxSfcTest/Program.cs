using Mes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI.WebControls;
using System.Xml.Linq;
using TestCore.Configuration;
using TestCore.Data;

namespace ThPrimaxSfcTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            /////http://10.80.1.27:9527/SMT/swagger/ui/index#!/

            //var api = "SFCExchange/GetPlant";
            //var apicontent = $"prodcut={HttpUtility.UrlEncode("Tyler S")}";

            //api = "SFCS/CheckStation";
            //apicontent = $"station={HttpUtility.UrlEncode("RF_Test")}&barcode={HttpUtility.UrlEncode("abcdefg")}&line={HttpUtility.UrlEncode("DP51")}&version={HttpUtility.UrlEncode("1.0")}";

            //api = "SFCS/CheckStation";

            ////var getspecialvalue =$"prodcut={HttpUtility.UrlEncode("")}&type={HttpUtility.UrlEncode("")}&parameters={HttpUtility.UrlEncode("")}";

            //HttpWebRequest request = HttpWebRequest.Create($"http://10.80.1.27:9527/SMT/api/1.1/{api}?{apicontent}") as HttpWebRequest;

            //request.Method = "Get";
            //request.Accept = "application/json";
            //request.Headers.Add("Authorization", $"baseAuth GMGkWIRzlwRqpd5d9CXYLj3r4k1S/9o9N+c8DG0PtGs=");


            //byte[] content = Encoding.UTF8.GetBytes(apicontent);

            ////request.ContentType = "application/json";
            ////request.ContentLength = content.Length;

            ////using (Stream requestStream = request.GetRequestStream())
            ////{
            ////    requestStream.Write(content, 0, content.Length);
            ////}

            //string result;
            //try
            //{
            //    HttpWebResponse httpWebResponse = (HttpWebResponse)request.GetResponse();
            //    string text = string.Empty;
            //    if (httpWebResponse.StatusCode == HttpStatusCode.OK)
            //    {
            //        using (Stream responseStream = httpWebResponse.GetResponseStream())
            //        {
            //            if (responseStream == null)
            //            {
            //                //return string.Empty;
            //                return;
            //            }
            //            using (StreamReader streamReader = new StreamReader(responseStream))
            //            {
            //                text = streamReader.ReadToEnd();
            //            }
            //        }
            //    }
            //    result = text;
            //}
            //catch (WebException ex)
            //{
            //    string text2 = "";
            //    using (Stream responseStream2 = ex.Response.GetResponseStream())
            //    {
            //        if (responseStream2 == null)
            //        {
            //            //return string.Empty;
            //            return;
            //        }
            //        using (StreamReader streamReader2 = new StreamReader(responseStream2))
            //        {
            //            text2 = streamReader2.ReadToEnd();
            //        }
            //    }
            //    if (text2.Contains("<!DOCTYPE html>"))
            //    {
            //        result = string.Format("{0}\"rlt\":false,\"msg\":\"{2}\"{1}", "{", "}", ex.Message);
            //    }
            //    else
            //    {
            //        result = text2;
            //    }
            //}
            //catch (Exception ex2)
            //{
            //    result = string.Format("{0}\"rlt\":false,\"msg\":\"{2}\"{1}", "{", "}", ex2.Message);
            //}
            ////return result;
            //Console.WriteLine(result);

            PrimaxWebApi primax = new PrimaxWebApi();

            string sfcsconfig = "<Sfcs><SfcsTable>MES_Transfer_BABY_YODA_Power_Test</SfcsTable><EnableSfc>true</EnableSfc><SfcsUploadData>false</SfcsUploadData><GetPartNoApi>Get_Model</GetPartNoApi><Version>1.0</Version><Product>Tyler S</Product><Station>AFT</Station></Sfcs>";

            var xml = XDocument.Parse(sfcsconfig);
            var sfc = new SFCsConfig(xml.Root);

            primax.Initialize(sfc, "");

            var sn = "abcdefg";
            var lineno = "DP51";
            try
            {
                primax.CheckStation(sfc, sn, lineno);
            }
            catch(Exception ex)
            {

            }

            TF_Result rs = new TF_Result(new TF_Spec("1.0", new TestCore.Nest<TF_Limit>(new TF_Limit("Root")))) { SFCsConfig = sfc, IsSFC = true };

            rs.SerialNumber = sn;
            rs.LineNo = lineno;
            rs.Status = TestCore.TF_TestStatus.PASSED;

            try
            {
                primax.CommitMesResult(rs, string.Empty, string.Empty, string.Empty);
            }
            catch (Exception ex1)
            {

            }
        }
    }
}
