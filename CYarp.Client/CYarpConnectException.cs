using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CYarp.Client
{
    /// <summary>
    /// Connection exception
    /// </summary>
    [DebuggerDisplay("ErrorCode = {ErrorCode}")]
    public class CYarpConnectException : Exception
    {
        private static readonly Dictionary<CYarpConnectError, string> messages = new()
        {
            { CYarpConnectError.Failure,"Connect failure" },
            { CYarpConnectError.Timedout,"Connect timed out" },
            { CYarpConnectError.Unauthorized,"Connect unauthorized" },
            { CYarpConnectError.Forbid ,"Connect forbidden" },
        };

        /// <summary>
        /// Gets the connection error code
        /// </summary>
        public CYarpConnectError ErrorCode { get; }

        /// <summary>
        /// CYarp connection exception
        /// </summary>
        /// <param name="errorCode"></param> 
        /// <param name="innerException"></param>
        public CYarpConnectException(CYarpConnectError errorCode, Exception innerException)
            : base(GetMessage(errorCode, innerException), innerException)
        {
            this.ErrorCode = errorCode;
        }

        private static string GetMessage(CYarpConnectError errorCode, Exception exception)
        {
            return messages.TryGetValue(errorCode, out var messge) ? messge : exception.Message;
        }
    }
}
