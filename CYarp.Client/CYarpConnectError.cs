namespace CYarp.Client
{
    /// <summary>
    /// 连接错误码
    /// </summary>
    public enum CYarpConnectError
    {
        /// <summary>
        /// 连接失败
        /// </summary>
        Failure = 1,

        /// <summary>
        /// 连接已超时
        /// </summary>
        Timedout = 2,

        /// <summary>
        /// 连接的身份认证不通过
        /// </summary>
        Unauthorized = 3,

        /// <summary>
        /// 连接的被拒绝(授权不通过)
        /// </summary>
        Forbid = 4,
    }
}
