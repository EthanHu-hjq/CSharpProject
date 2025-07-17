using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DailyApp.Models
{
    public class PasswordEncryptor
    {
        public string EncryptPassword(string password)
        {
            // 创建SHA-256对象
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // 计算字符串的SHA-256哈希值
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                // 将字节数组转换为十六进制字符串
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
