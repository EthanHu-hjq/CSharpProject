using Microsoft.EntityFrameworkCore;

namespace DailyApp.API.DataModel
{
    /// <summary>
    /// DailyApp数据库上下文
    /// </summary>
    public class DailyDbContext: DbContext
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="options"></param>
        public DailyDbContext(DbContextOptions<DailyDbContext> options): base(options)
        {
            
        }

        /// <summary>
        /// 账号信息表
        /// </summary>
        public DbSet <AccountInfo> AccountInfo { get; set; }
    }
}
