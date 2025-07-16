using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DailyApp.API.DataModel
{
    /// <summary>
    /// 账户信息
    /// </summary>
    [Table("AccountInfo")]//自定义表名
    public class AccountInfo
    {
        /// <summary>
        /// 账户ID
        /// </summary>
        [Key]//主键 自增长
        public int AccountId { get; set; }

        /// <summary>
        /// 账号
        /// </summary>
        public string? Account { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// 手机号
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// 注册时间
        /// </summary>
        public DateTime? RegisterTime { get; set; }
    }

}
