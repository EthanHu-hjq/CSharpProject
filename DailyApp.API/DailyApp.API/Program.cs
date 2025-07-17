
using DailyApp.API.AutoMappers;
using DailyApp.API.DataModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;

namespace DailyApp.API
{
    /// <summary>
    /// Program class
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main method
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(m =>
            {
                var path = AppContext.BaseDirectory + "DailyApp.API.xml";
                m.IncludeXmlComments(path, true);
            });
            //注册数据库上下文
            builder.Services.AddDbContext<DailyDbContext>(
                options => options.UseSqlServer(builder.Configuration.GetConnectionString("DbConnStr")));
            //注册AutoMapper,映射配置AutoMapperSettings
            builder.Services.AddAutoMapper(typeof(AutoMapperSettings));
            

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
