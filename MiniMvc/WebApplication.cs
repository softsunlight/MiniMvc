using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
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
                Console.WriteLine(tcpClient.Client.RemoteEndPoint.ToString());
                //接收数据
                Task.Run(() =>
                {
                    //获取用户接收和发送数据的网络流
                    NetworkStream networkStream = tcpClient.GetStream();
                    //web请求数据
                    List<byte> requestDatas = new List<byte>();
                    //空行结束位置
                    int spaceLineEndIndex = -1;
                    //上次请求处理的位置，因为请求有可能分多次发送
                    int lastProcessDataCount = 0;
                    //当前请求请求体内容长度
                    int contentLength = 0;
                    while (true)
                    {
                        try
                        {
                            //将读取的数据写入内存流
                            MemoryStream memoryStream = new MemoryStream();
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
                            if (requestDatas.Count <= 0)
                            {
                                break;
                            }
                            if (spaceLineEndIndex > 0)
                            {
                                string requestHeader = Encoding.UTF8.GetString(requestDatas.ToArray(), 0, spaceLineEndIndex + 1);
                                Match contentLengthMatch = Regex.Match(requestHeader, @"(?is)Content-Length\s*:\s*(?<contentLength>\d+)");
                                if (contentLengthMatch.Success)
                                {
                                    contentLength = Convert.ToInt32(contentLengthMatch.Groups["contentLength"].Value);
                                }
                            }
                            else
                            {
                                for (int i = lastProcessDataCount; i < requestDatas.Count; i++)
                                {
                                    //判断空行位置，\r的assic码为13，\n的assic码为10，空行的特征：\r\n\r\n
                                    if (requestDatas[i] == 13 && requestDatas[i + 1] == 10 && requestDatas[i + 2] == 13 && requestDatas[i + 3] == 10)
                                    {
                                        spaceLineEndIndex = i + 3;
                                        break;
                                    }
                                }
                                lastProcessDataCount += requestDatas.Count - lastProcessDataCount;
                            }
                            //获取完整请求
                            if ((contentLength <= 0 && spaceLineEndIndex > 0) || (spaceLineEndIndex + 1 + contentLength >= requestDatas.Count))
                            {
                                //响应
                                byte[] wholeRequestDatas = new byte[spaceLineEndIndex + 1 + contentLength];
                                requestDatas.CopyTo(0, wholeRequestDatas, 0, wholeRequestDatas.Length);
                                HttpContext httpContext = new HttpContext();
                                httpContext.OriginalRequestDatas = wholeRequestDatas;
                                httpContext.TcpClient = tcpClient;
                                Process(httpContext);
                                //请求数据拷贝后，重置数据，处理下一次请求
                                requestDatas.RemoveRange(0, spaceLineEndIndex + 1 + contentLength);
                                spaceLineEndIndex = -1;
                                lastProcessDataCount = 0;
                                contentLength = 0;
                            }
                        }
                        catch (Exception ex)
                        {
                            break;
                        }
                    }
                });
            }
        }

        /// <summary>
        /// 处理请求
        /// </summary>
        /// <param name="httpContext"></param>
        private void Process(HttpContext httpContext)
        {
            Task.Run(() =>
            {
                HttpRequest httpRequest = GetHttpRequest(httpContext.OriginalRequestDatas);
                Console.WriteLine(httpRequest.RequestPath);
                HttpResponse httpResponse = new HttpResponse(httpContext.TcpClient.GetStream())
                {
                    HttpVersion = httpRequest.HttpVersion,
                    StatusCode = 404,
                    StatusMessage = "Not Found"
                };
                httpContext.Response = httpResponse;
                try
                {
                    if (httpRequest == null)
                    {
                        httpResponse.StatusCode = 400;
                        httpResponse.StatusMessage = "Bad Request";
                    }
                    //判断是否是CORS预检请求
                    if (httpRequest.RequestMethod == "OPTIONS" && (httpRequest.Headers.ContainsKey("Access-Control-Request-Headers") || httpRequest.Headers.ContainsKey("Access-Control-Request-Method")))
                    {
                        if (httpResponse.Headers == null)
                        {
                            httpResponse.Headers = new Dictionary<string, string>();
                        }
                        httpResponse.Headers["Access-Control-Allow-Origin"] = "*";
                        httpResponse.Headers["Access-Control-Allow-Methods"] = "GET,POST";
                        httpResponse.Headers["Access-Control-Allow-Headers"] = httpRequest.Headers["Access-Control-Request-Headers"];
                        httpResponse.StatusCode = 204;
                        httpResponse.StatusMessage = "No Content";
                        return;
                    }
                    if (httpRequest.RequestPath == "/cors")
                    {
                        if (httpResponse.Headers == null)
                        {
                            httpResponse.Headers = new Dictionary<string, string>();
                        }
                        httpResponse.Headers["Content-Type"] = "application/json";
                        httpResponse.Headers["Access-Control-Allow-Origin"] = "*";
                        httpResponse.Body = Encoding.UTF8.GetBytes("{}");
                        httpResponse.StatusCode = 200;
                        httpResponse.StatusMessage = "OK";
                        return;
                    }
                    string staticDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "static");
                    string filePath = "";
                    httpResponse.Headers = new Dictionary<string, string>();
                    if (httpRequest.RequestPath == "/")
                    {
                        filePath = Path.Combine(staticDir, @"html\index.html");
                        httpResponse.Headers["Content-Type"] = "text/html";
                    }
                    else
                    {
                        filePath = Path.Combine(staticDir, httpRequest.RequestPath.Substring(1).Replace("/", @"\"));
                    }
                    if (httpRequest.RequestPath.LastIndexOf("js") >= 0)
                    {
                        httpResponse.Headers["Content-Type"] = "text/js";
                    }
                    if (httpRequest.RequestPath.LastIndexOf("css") >= 0)
                    {
                        httpResponse.Headers["Content-Type"] = "text/css";
                    }
                    if (File.Exists(filePath))
                    {
                        httpResponse.Body = File.ReadAllBytes(filePath);
                        httpResponse.StatusCode = 200;
                        httpResponse.StatusMessage = "OK";
                    }
                }
                catch (Exception ex)
                {
                    httpResponse.StatusCode = 500;
                    httpResponse.StatusMessage = "Internal Server Error";
                }
                finally
                {
                    httpResponse.Write();
                }
            });
        }

        /// <summary>
        /// 构造请求类
        /// </summary>
        /// <param name="requestDatas"></param>
        /// <returns></returns>
        private HttpRequest GetHttpRequest(byte[] requestDatas)
        {
            HttpRequest request = new HttpRequest();
            try
            {
                int spaceLineEndIndex = 0;
                for (int i = 0; i < requestDatas.Length; i++)
                {
                    //判断空行位置，\r的assic码为13，\n的assic码为10，空行的特征：\r\n\r\n
                    if (requestDatas[i] == 13 && requestDatas[i + 1] == 10 && requestDatas[i + 2] == 13 && requestDatas[i + 3] == 10)
                    {
                        spaceLineEndIndex = i + 3;
                        break;
                    }
                }
                if (spaceLineEndIndex > 0)
                {
                    string requestHeader = Encoding.UTF8.GetString(requestDatas, 0, spaceLineEndIndex + 1);
                    string[] contents = requestHeader.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
                    string startLine = contents[0];
                    if (string.IsNullOrWhiteSpace(startLine))
                    {
                        return null;
                    }
                    string[] startLineArr = startLine.Split(' ');
                    if (startLineArr.Length < 3)
                    {
                        return null;
                    }
                    request.RequestMethod = startLineArr[0];
                    request.RequestPath = startLineArr[1].Split('?')[0];
                    request.HttpVersion = startLineArr[2];
                    MatchCollection requestHeaderMatches = Regex.Matches(requestHeader, @"(?is)(?<name>\S*)\s*:\s*(?<value>\S+)");
                    Dictionary<string, string> headers = new Dictionary<string, string>();
                    foreach (Match m in requestHeaderMatches)
                    {
                        if (m.Success)
                        {
                            headers[m.Groups["name"].Value] = m.Groups["value"].Value;
                        }
                    }
                    request.Headers = headers;
                }
                if (spaceLineEndIndex + 1 < requestDatas.Length)
                {
                    //处理请求体
                    byte[] bodys = new byte[requestDatas.Length - spaceLineEndIndex - 1];
                    Array.Copy(requestDatas, spaceLineEndIndex + 1, bodys, 0, bodys.Length);
                    request.Body = bodys;
                }
                return request;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

    }
}
