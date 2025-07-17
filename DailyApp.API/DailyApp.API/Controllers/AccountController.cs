using AutoMapper;
using DailyApp.API.ApiResponses;
using DailyApp.API.DataModel;
using DailyApp.API.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DailyApp.API.Controllers
{
    /// <summary>
    /// 账户接口
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        //数据库上下文字段
        private readonly DailyDbContext _db;

        //AutoMapper字段
        private readonly IMapper _mapper;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="db">数据库上下文</param>
        /// <param name="mapper">DTO映射器</param>
        public AccountController(DailyDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        #region 注册
        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="accountInfoDTO">账户信息</param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Register(AccountInfoDTO accountInfoDTO)
        {
            ApiResponse response = new ApiResponse();//定义一个响应对象
            //业务
            try
            {
                //1.检查账户是否存在
                var account = _db.AccountInfo.Where(a => a.Account == accountInfoDTO.Account).FirstOrDefault();
                if (account != null)
                {
                    response.ResultCode = -1;
                    response.ResultMessage = "账户已存在";
                    return Ok(response);
                }
                //2.创建账户
                AccountInfo newAccount = _mapper.Map<AccountInfo>(accountInfoDTO);
                _db.AccountInfo.Add(newAccount);
                int result = _db.SaveChanges();
                if (result > 0)
                {
                    response.ResultCode = 0;
                    response.ResultMessage = "注册成功";
                }
                else
                {
                    response.ResultCode = -99;
                    response.ResultMessage = "注册失败";
                }
            }
            catch (Exception ex)
            {
                response.ResultCode = -99;
                response.ResultMessage = ex.Message;
            }

            //TODO: 注册逻辑
            return Ok(response);
        }
        #endregion

        #region 登录
        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="account">账户信息</param>
        /// <param name="password">密码</param>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Login(string account, string password)
        {
            ApiResponse response = new ApiResponse();
            try
            {
                //1.检查账户是否存在
                var accountInfo = _db.AccountInfo.Where(a => a.Account == account).FirstOrDefault();
                if (accountInfo == null)
                {
                    response.ResultCode = -1;
                    response.ResultMessage = "账户不存在";
                    return Ok(response);
                }
                //2.检查密码是否正确
                if (accountInfo.Password != password)
                {
                    response.ResultCode = -2;
                    response.ResultMessage = "密码错误";
                    return Ok(response);
                }
                //3.登录成功
                response.ResultCode = 0;
                response.ResultMessage = "登录成功";
                response.ResultData = accountInfo;
            }
            catch (Exception ex)
            {
                response.ResultCode = -99;
                response.ResultMessage = ex.Message;
            }
            return Ok(response);
        }
        #endregion
    }
}
