using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace MiniMvc
{
    /// <summary>
    /// http请求上下文
    /// </summary>
    public class HttpContext
    {
        /// <summary>
        /// 原始http请求数据
        /// </summary>
        public byte[] OriginalRequestDatas { get; set; }

        /// <summary>
        /// tcp连接
        /// </summary>
        public TcpClient TcpClient { get; set; }

        /// <summary>
        /// http请求
        /// </summary>
        public HttpRequest Request { get; set; }

        /// <summary>
        /// http响应
        /// </summary>
        public HttpResponse Response { get; set; }
    }
}
