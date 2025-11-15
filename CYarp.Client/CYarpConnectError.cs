namespace CYarp.Client
{
    /// <summary>
    /// Connection error codes
    /// </summary>
    public enum CYarpConnectError
    {
        /// <summary>
        /// Connection failed
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
        /// Connection was rejected (authorization failed)
        /// </summary>
        Forbid = 4,
    }
}
