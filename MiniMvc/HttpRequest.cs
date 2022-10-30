using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMvc
{
    public class HttpRequest
    {
        /// <summary>
        /// 请求方法
        /// </summary>
        public string RequestMethod { get; set; }

        /// <summary>
        /// 请求路径
        /// </summary>
        public string RequestPath { get; set; }

        /// <summary>
        /// http版本
        /// </summary>
        public string HttpVersion { get; set; }

        /// <summary>
        /// 请求头
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }

        /// <summary>
        /// 请求体
        /// </summary>
        public byte[] Body { get; set; }
    }
}
