using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DailyApp.HttpClients
{
    public class ApiResponse
    {
        /// <summary>
        /// 结果编码
        /// </summary>
        public int ResultCode { get; set; }

        /// <summary>
        /// 结果消息
        /// </summary>
        public string? ResultMessage { get; set; }

        /// <summary>
        /// 结果数据
        /// </summary>
        public object? ResultData { get; set; }
    }
}
