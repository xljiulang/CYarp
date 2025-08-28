using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CYarp.Client
{
    /// <summary>
    /// ConnectionException
    /// </summary>
    [DebuggerDisplay("ErrorCode = {ErrorCode}")]
    public class CYarpConnectException : Exception
    {
        private static readonly Dictionary<CYarpConnectError, string> messages = new()
        {
            { CYarpConnectError.Failure,"Connect failure" },
            { CYarpConnectError.Timedout,"Connect timed out" },
            { CYarpConnectError.Unauthorized,"Connect unauthorized" },
            { CYarpConnectError.Forbid ,"Connect forbid" },
        };

        /// <summary>
        /// GetConnectionError码
        /// </summary>
        public CYarpConnectError ErrorCode { get; }

        /// <summary>
        /// CYarpException
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
