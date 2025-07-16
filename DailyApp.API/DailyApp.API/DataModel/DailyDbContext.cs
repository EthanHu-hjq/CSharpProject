using Microsoft.EntityFrameworkCore;

namespace DailyApp.API.DataModel
{
    public class DailyDbContext: DbContext
    {
        public DailyDbContext(DbContextOptions<DailyDbContext> options): base(options)
        {
            
        }

        /// <summary>
        /// 账号信息表
        /// </summary>
        public DbSet <AccountInfo> AccountInfo { get; set; }
    }
}
