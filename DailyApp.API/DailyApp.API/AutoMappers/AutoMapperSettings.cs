using AutoMapper;
using DailyApp.API.DataModel;
using DailyApp.API.DTOs;

namespace DailyApp.API.AutoMappers
{
    /// <summary>
    /// 自动匹配转换DTO
    /// </summary>
    public class AutoMapperSettings : Profile
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public AutoMapperSettings()
        {
            CreateMap<AccountInfoDTO, AccountInfo>().ReverseMap();// 匹配AccountInfoDTO到AccountInfo
        }
    }
}
