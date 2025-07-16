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

        public AccountController(DailyDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="account">账户信息</param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Register(AccountInfoDTO accountInfoDTO)
        {
            ApiResponse response = new ApiResponse();//定义一个响应对象
            //业务
            try
            {
                //1.检查账户是否存在
                var account = _db.AccountInfo.Where(a=>a.Account==accountInfoDTO.Account).FirstOrDefault();
                if (account!= null)
                {
                    response.ResultCode = -1;
                    response.ResultMessage = "账户已存在";
                    return Ok(response);
                }
                //2.创建账户
                AccountInfo newAccount = new AccountInfo()
                {
                    Account = accountInfoDTO.Account,
                    Name = accountInfoDTO.Name,
                    Password = accountInfoDTO.Password,
                    Email = accountInfoDTO.Email,
                    Phone = accountInfoDTO.Phone,
                    RegisterTime = DateTime.Now
                };
                _db.AccountInfo.Add(newAccount);
                int result = _db.SaveChanges();
                if(result > 0)
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
    }
}
