using NModbus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ModbusTcpClient.Helpers
{
    /// <summary>
    /// 短连接模式的Modbus-TCP工具类
    /// 每次通讯自动创建连接→执行操作→释放资源
    /// </summary>
    public class TcpHelper : IDisposable
    {
        // Modbus配置参数（仅保存配置，不持久化连接）
        private readonly string _serverIp;
        private readonly int _serverPort;
        private readonly int _connectTimeoutMs = 5000;
        private readonly int _readWriteTimeoutMs = 1000;

        /// <summary>
        /// 初始化Modbus配置
        /// </summary>
        /// <param name="serverIp">服务器IP</param>
        /// <param name="serverPort">端口（默认502）</param>
        public TcpHelper(string serverIp, int serverPort = 502)
        {
            _serverIp = serverIp;
            _serverPort = serverPort;
        }


        /// <summary>
        /// 短连接读取保持寄存器
        /// </summary>
        /// <param name="slaveAddr">从站地址</param>
        /// <param name="register">寄存器地址</param>
        /// <param name="numOfPoint">寄存器数量</param>
        /// <returns>寄存器值数组，null表示失败</returns>
        public ushort[] ReadHoldingRegisters(byte slaveAddr, ushort register, ushort numOfPoint)
        {
            return ExecuteShortConnectOperation(
                master => master.ReadHoldingRegisters(slaveAddr, register, numOfPoint),
                "读取保持寄存器"
            );
        }


        /// <summary>
        /// 短连接写入单个保持寄存器
        /// </summary>
        /// <param name="slaveAddr">从站地址</param>
        /// <param name="register">寄存器地址</param>
        /// <param name="value">写入值</param>
        /// <returns>是否成功</returns>
        public bool WriteHoldingRegister(byte slaveAddr, ushort register, ushort value)
        {
            var result = ExecuteShortConnectOperation(
                master =>
                {
                    master.WriteSingleRegister(slaveAddr, register, value);
                    return true;
                },
                "写入保持寄存器"
            );
            return result;
        }


        /// <summary>
        /// 核心：短连接执行Modbus操作（自动创建连接→执行→释放）
        /// </summary>
        /// <typeparam name="T">操作返回类型</typeparam>
        /// <param name="operation">Modbus操作逻辑</param>
        /// <param name="operationName">操作名称（日志用）</param>
        /// <returns>操作结果，null表示失败</returns>
        private T ExecuteShortConnectOperation<T>(Func<IModbusMaster, T> operation, string operationName)
        {
            TcpClient? tcpClient = null;
            IModbusMaster? modbusMaster = null;
            try
            {
                // 1. 创建TCP连接
                tcpClient = new TcpClient();
                if (!tcpClient.ConnectAsync(_serverIp, _serverPort).Wait(_connectTimeoutMs))
                {
                    throw new TimeoutException("TCP连接超时");
                }

                // 2. 创建ModbusMaster
                modbusMaster = new ModbusFactory().CreateMaster(tcpClient);
                modbusMaster.Transport.ReadTimeout = _readWriteTimeoutMs;
                modbusMaster.Transport.WriteTimeout = _readWriteTimeoutMs;

                // 3. 执行Modbus操作
                var result = operation.Invoke(modbusMaster);
                //Info($"{operationName}成功（重试次数：{retry}）");
                return result;
            }
            catch
            {
            }
            finally
            {
                // 4. 强制释放资源（无论成败）
                DisposeModbusResources(modbusMaster, tcpClient);
            }
            return default!;
        }


        /// <summary>
        /// 释放Modbus及TCP资源
        /// </summary>
        private void DisposeModbusResources(IModbusMaster? master, TcpClient? tcpClient)
        {
            // 释放ModbusMaster
            if (master != null)
            {
                try { master.Dispose(); } catch { }
            }

            // 释放TcpClient及底层Socket
            if (tcpClient != null)
            {
                try
                {
                    if (tcpClient.Client != null)
                    {
                        if (tcpClient.Connected)
                        {
                            tcpClient.Client.Shutdown(SocketShutdown.Both);
                        }
                        tcpClient.Client.Close(1000);
                    }
                }
                catch { }
                finally
                {
                    tcpClient.Dispose();
                }
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
