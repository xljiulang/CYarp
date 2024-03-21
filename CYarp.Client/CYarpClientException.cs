using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CYarp.Client
{
    /// <summary>
    /// CYarp异常
    /// </summary>
    [DebuggerDisplay("ErrorCode = {ErrorCode}")]
    public class CYarpClientException : Exception
    {
        private static readonly Dictionary<CYarpErrorCode, string> messages = new()
        {
            { CYarpErrorCode.InvalidOptions,"InvalidOptions" },
            { CYarpErrorCode.ConnectUnauthorized,"ConnectUnauthorized" },
            { CYarpErrorCode.ConnectTimedout,"ConnectTimedout" },
            { CYarpErrorCode.ConnectFailure,"ConnectFailure" },
        };

        /// <summary>
        /// 获取错误码
        /// </summary>
        public CYarpErrorCode ErrorCode { get; }

        /// <summary>
        /// CYarp异常
        /// </summary>
        /// <param name="errorCode"></param> 
        /// <param name="innerException"></param>
        public CYarpClientException(CYarpErrorCode errorCode, Exception innerException)
            : base(GetMessage(errorCode, innerException), innerException)
        {
            this.ErrorCode = errorCode;
        }

        private static string GetMessage(CYarpErrorCode errorCode, Exception exception)
        {
            return messages.TryGetValue(errorCode, out var messge) ? messge : exception.Message;
        }
    }
}
