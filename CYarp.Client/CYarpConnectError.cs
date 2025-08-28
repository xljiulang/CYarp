namespace CYarp.Client
{
    /// <summary>
    /// Connection error code
    /// </summary>
    public enum CYarpConnectError
    {
        /// <summary>
        /// Connection failure
        /// </summary>
        Failure = 1,

        /// <summary>
        /// Connection timed out
        /// </summary>
        Timedout = 2,

        /// <summary>
        /// Connection authentication failed
        /// </summary>
        Unauthorized = 3,

        /// <summary>
        /// Connection rejected (authorization failed)
        /// </summary>
        Forbid = 4,
    }
}
