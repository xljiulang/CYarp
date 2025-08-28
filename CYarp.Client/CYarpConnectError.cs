namespace CYarp.Client
{
    /// <summary>
    /// ConnectionError码
    /// </summary>
    public enum CYarpConnectError
    {
        /// <summary>
        /// ConnectionFailure
        /// </summary>
        Failure = 1,

        /// <summary>
        /// ConnectionAlreadyTimeout
        /// </summary>
        Timedout = 2,

        /// <summary>
        /// ConnectionAuthentication不通过
        /// </summary>
        Unauthorized = 3,

        /// <summary>
        /// ConnectionBy拒绝(Authorization不通过)
        /// </summary>
        Forbid = 4,
    }
}
