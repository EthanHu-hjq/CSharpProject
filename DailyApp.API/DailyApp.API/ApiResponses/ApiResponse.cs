namespace DailyApp.API.ApiResponses
{
    /// <summary>
    /// API响应
    /// </summary>
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
