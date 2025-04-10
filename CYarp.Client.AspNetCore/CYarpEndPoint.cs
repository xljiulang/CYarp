using System.Net;

namespace CYarp.Client.AspNetCore
{
    /// <summary>
    /// Cyarp终结点
    /// </summary>
    public class CYarpEndPoint : EndPoint
    {
        /// <summary>
        /// 获取选项
        /// </summary>
        public CYarpClientOptions Options { get; }

        /// <summary>
        /// Cyarp终结点
        /// </summary>
        /// <param name="options"></param>
        public CYarpEndPoint(CYarpClientOptions options)
        {
            Options = options;
        }

        /// <summary>
        /// 转换为文本
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Options.ServerUri.ToString();
        }
    }
}
