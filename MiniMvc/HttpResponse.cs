using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace MiniMvc
{
    public class HttpResponse
    {
        private NetworkStream networkStream;
        public HttpResponse(NetworkStream networkStream)
        {
            this.networkStream = networkStream;
        }

        /// <summary>
        /// http版本
        /// </summary>
        public string HttpVersion { get; set; }

        /// <summary>
        /// 状态码
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// 状态文本
        /// </summary>
        public string StatusMessage { get; set; }

        /// <summary>
        /// 响应头
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }

        /// <summary>
        /// 响应体
        /// </summary>
        public byte[] Body { get; set; }

        /// <summary>
        /// 写入响应流
        /// </summary>
        public void Write()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append($"{HttpVersion} {StatusCode} {StatusMessage}\r\n");
            if (Headers != null)
            {
                foreach (var key in Headers.Keys)
                {
                    stringBuilder.Append($"{key}:{Headers[key]}\r\n");
                }
            }
            if (Body == null)
            {
                stringBuilder.Append($"Content-Length:0\r\n");
            }
            else
            {
                stringBuilder.Append($"Content-Length:{Body.Length}\r\n");
            }
            stringBuilder.Append("\r\n");
            networkStream.Write(Encoding.UTF8.GetBytes(stringBuilder.ToString()));
            networkStream.Write(Body);
        }
    }
}
