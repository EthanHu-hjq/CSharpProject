using System;

namespace TestBat
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                //for(int i = 0; i < args.Length; i++)
                //{
                //    Console.WriteLine($"第{i}个参数:{args[i]}!");
                //    switch(args[i].ToLower())
                //    {
                //        case "--port":
                //            i++;
                //            Console.WriteLine($"参数{i}: {args[i]}");
                //            break;
                //        case "--product":
                //            i++;
                //            Console.WriteLine($"参数{i}: {args[i]}");
                //            break;
                //        default:
                //            Console.WriteLine($"参数{i + 1}: {args[i]}");
                //            break;
                //    }
                //}
                // 业务逻辑：成功/失败判断
                bool isSuccess = true;
                string result = isSuccess ? "successful" : "fail";
                int exitCode = isSuccess ? 6 : 2;

                // 输出字符串结果（批处理捕获）
                if (args[0].ToLower() == "uart")
                    Console.WriteLine(result);
                else Console.WriteLine("99999999888888");
            }
            catch
            {
                Console.WriteLine("fail");
            }
        }
    }
}
