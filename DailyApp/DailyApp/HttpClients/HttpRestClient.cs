using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DailyApp.HttpClients
{
    /// <summary>
    /// 调用api工具类
    /// </summary>
    public class HttpRestClient
    {
        //实例化一个客户端
        private readonly RestClient Client;

        private readonly string baseUrl = "http://localhost:34751/api/";

        public HttpRestClient()
        {
            Client = new RestClient();
        }

        /// <summary>
        /// 执行请求
        /// </summary>
        /// <param name="request">请求数据</param>
        /// <returns>接收到的数据</returns>
        public ApiResponse Execute(ApiRequest request)
        {
            // 创建请求对象
            RestRequest restRequest = new RestRequest(request.Method);
            //设置发送的数据类型
            restRequest.AddHeader("Content-Type", "application/json");
            //设置请求参数
            if (request.Parameters!= null)
            {
                //设置请求参数 SerializeObject json序列化 对象->json字符串
                restRequest.AddParameter("param",JsonConvert.SerializeObject(request.Parameters),ParameterType.RequestBody);
            }

            Client.BaseUrl = new Uri(baseUrl+request.Route);
            //执行请求
            var res = Client.Execute(restRequest);
            if(res.StatusCode==System.Net.HttpStatusCode.OK)
            {
                //返回结果 DeserializeObject json字符串->对象
                return JsonConvert.DeserializeObject<ApiResponse>(res.Content);
            }
            else
            {
                return new ApiResponse() { ResultCode = -99, ResultMessage = "请求失败" };
            }

        }
    }
}
