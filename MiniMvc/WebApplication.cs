using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MiniMvc
{
    /// <summary>
    /// web应用程序类
    /// </summary>
    public class WebApplication
    {
        /// <summary>
        /// web应用启动
        /// </summary>
        /// <param name="port">web端口</param>
        public void Start(int port)
        {
            //创建TcpListener对象并绑定本地回环测试地址和端口号
            TcpListener tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
            //启动服务
            tcpListener.Start();
            //等待客户端的连接
            while (true)
            {
                //接收客户端的连接
                TcpClient tcpClient = tcpListener.AcceptTcpClient();
                //接收数据
                Task.Run(() =>
                {
                    //获取用户接收和发送数据的网络流
                    NetworkStream networkStream = tcpClient.GetStream();
                    //将读取的数据写入内存流
                    MemoryStream memoryStream = new MemoryStream();
                    //web请求数据
                    List<byte> requestDatas = new List<byte>();
                    //空行结束位置
                    int spaceLineEndIndex = -1;
                    //上次请求处理的位置，因为请求有可能分多次发送
                    int lastProcessDataCount = 0;
                    while (true)
                    {
                        try
                        {
                            do
                            {
                                //定义数据接收缓冲区
                                byte[] buffer = new byte[512];
                                //读取数据
                                int readed = networkStream.Read(buffer, 0, buffer.Length);
                                //写入内存流
                                memoryStream.Write(buffer, 0, readed);
                            } while (networkStream.DataAvailable);//如果还有数据则继续读取
                            //将当前读取的数据存下
                            requestDatas.AddRange(memoryStream.ToArray());
                            for (int i = lastProcessDataCount; i < requestDatas.Count; i++)
                            {
                                //判断空行位置，\r的assic码为13，\n的assic码为10，空行的特征：\r\n\r\n
                                if (requestDatas[i] == 13 && requestDatas[i + 1] == 10 && requestDatas[i + 2] == 13 && requestDatas[i + 3] == 10)
                                {
                                    spaceLineEndIndex = i + 3;
                                    break;
                                }
                            }
                            //获取完整请求后开始处理请求
                            if (spaceLineEndIndex > 0)
                            {
                                int length = spaceLineEndIndex + 1;
                                Console.WriteLine(Encoding.UTF8.GetString(requestDatas.ToArray(), 0, length));
                                requestDatas.RemoveRange(0, length);
                                //响应，
                                networkStream.Write(Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\nContent-Type:text/plain\r\n\r\n"));
                            }
                            else
                            {
                                lastProcessDataCount += requestDatas.Count - lastProcessDataCount;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            break;
                        }
                    }
                });
            }
        }
    }
}
