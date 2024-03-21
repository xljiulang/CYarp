namespace CYarp.Client
{
    /// <summary>
    /// 错误码
    /// </summary>
    public enum CYarpErrorCode
    {
        /// <summary>
        /// Options值无效
        /// </summary>
        InvalidOptions = 1,

        /// <summary>
        /// 连接到服务身份认证不通过
        /// </summary>
        ConnectUnauthorized = 2,

        /// <summary>
        /// 连接到服务器已超时
        /// </summary>
        ConnectTimedout = 3,

        /// <summary>
        /// 连接到服务器失败
        /// </summary>
        ConnectFailure = 4,
    }
}
