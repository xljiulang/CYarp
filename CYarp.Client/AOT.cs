using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CYarp.Client
{
    /// <summary>
    /// AOT编译
    /// </summary>
    static class AOT
    {
        /// <summary>
        /// 错误码
        /// </summary>
        public enum ErrorCode : int
        {
            /// <summary>
            /// 句柄无效
            /// </summary>
            InvalidHandle = -1,

            /// <summary>
            /// 无错误
            /// </summary>
            NoError = 0,

            /// <summary>
            /// 连接到服务器失败
            /// </summary>
            ConnectFailure = 1,

            /// <summary>
            /// 连接到服务器已超时
            /// </summary>
            ConnectTimeout = 2,

            /// <summary>
            /// 连接到服务身份认证不通过
            /// </summary>
            ConnectUnauthorized = 3,

            /// <summary>
          	/// 连接被拒绝(授权不通过)
            /// </summary>
            ConnectForbid = 4,
        }

        /// <summary>
        /// 客户端选项
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct ClientOptions
        {
            public nint ServerUri;
            public nint TargetUri;
            public nint TargetUnixDomainSocket;
            public nint Authorization;
            public int ConnectTimeout;
            public unsafe delegate* unmanaged[Cdecl]<nint, nint, void> TunnelErrorCallback;
        }

        /// <summary>
        /// 创建客户端
        /// </summary>
        /// <returns>客户端句柄</returns>
        [UnmanagedCallersOnly(EntryPoint = "CYarpClientCreate", CallConvs = [typeof(CallConvCdecl)])]
        public unsafe static nint CYarpClientCreate(ClientOptions* clientOptions)
        {
            if (clientOptions == null ||
                clientOptions->ServerUri == default ||
                clientOptions->TargetUri == default ||
                Uri.TryCreate(Marshal.PtrToStringUni(clientOptions->ServerUri), UriKind.Absolute, out var serverUri) == false ||
                Uri.TryCreate(Marshal.PtrToStringUni(clientOptions->TargetUri), UriKind.Absolute, out var targetUri) == false)
            {
                return nint.Zero;
            }

            var options = new CYarpClientOptions
            {
                ServerUri = serverUri,
                TargetUri = targetUri,
            };

            var authorization = Marshal.PtrToStringUni(clientOptions->Authorization);
            if (!string.IsNullOrEmpty(authorization))
            {
                options.ConnectHeaders[nameof(ClientOptions.Authorization)] = authorization;
            }

            if (clientOptions->TargetUnixDomainSocket != default)
            {
                options.TargetUnixDomainSocket = Marshal.PtrToStringUni(clientOptions->TargetUnixDomainSocket);
            }

            if (clientOptions->ConnectTimeout > 0)
            {
                options.ConnectTimeout = TimeSpan.FromSeconds(clientOptions->ConnectTimeout);
            }

            if (clientOptions->TunnelErrorCallback != default)
            {
                options.TunnelErrorCallback = (exception) =>
                {
                    var type = Marshal.StringToHGlobalUni(exception.GetType().Name);
                    var message = Marshal.StringToHGlobalUni(exception.Message);
                    clientOptions->TunnelErrorCallback(type, message);
                    Marshal.FreeHGlobal(type);
                    Marshal.FreeHGlobal(message);
                };
            }

            try
            {
                var client = new CYarpClient(options);
                var gcHandle = GCHandle.Alloc(client);
                return GCHandle.ToIntPtr(gcHandle);
            }
            catch (Exception)
            {
                return nint.Zero;
            }
        }

        /// <summary>
        /// 释放客户端
        /// </summary>
        /// <param name="clientPtr">客户端句柄</param>
        [UnmanagedCallersOnly(EntryPoint = "CYarpClientFree", CallConvs = [typeof(CallConvCdecl)])]
        public static void CYarpClientFree(nint clientPtr)
        {
            var gcHandle = GCHandle.FromIntPtr(clientPtr);
            if (gcHandle.IsAllocated)
            {
                if (gcHandle.Target is CYarpClient client)
                {
                    client.Dispose();
                }
                gcHandle.Free();
            }
        }

        /// <summary>
        /// 同步传输数据
        /// </summary>
        /// <param name="clientPtr">客户端句柄</param> 
        /// <returns>传输错误枚举</returns>
        [UnmanagedCallersOnly(EntryPoint = "CYarpClientTransport", CallConvs = [typeof(CallConvCdecl)])]
        public static ErrorCode CYarpClientTransport(nint clientPtr)
        {
            return Transport(clientPtr);
        }

        /// <summary>
        /// 异步传输数据
        /// </summary>
        /// <param name="clientPtr">客户端句柄</param>
        /// <param name="completedCallback">传输完成回调，null则转同步调用</param>
        /// <returns>传输错误枚举</returns>
        [UnmanagedCallersOnly(EntryPoint = "CYarpClientTransportAsync", CallConvs = [typeof(CallConvCdecl)])]
        public unsafe static ErrorCode CYarpClientTransportAsync(nint clientPtr, delegate* unmanaged[Cdecl]<ErrorCode, void> completedCallback)
        {
            if (completedCallback == null)
            {
                return Transport(clientPtr);
            }

            var gcHandle = GCHandle.FromIntPtr(clientPtr);
            if (gcHandle.IsAllocated == false || gcHandle.Target is not CYarpClient client)
            {
                return ErrorCode.InvalidHandle;
            }

            TransportAsync(client, errorCode => completedCallback(errorCode));
            return ErrorCode.NoError;
        }

        /// <summary>
        /// 同步数据传输
        /// </summary>
        /// <param name="clientPtr"></param>
        /// <returns></returns>
        private static ErrorCode Transport(nint clientPtr)
        {
            var gcHandle = GCHandle.FromIntPtr(clientPtr);
            if (gcHandle.IsAllocated == false || gcHandle.Target is not CYarpClient client)
            {
                return ErrorCode.InvalidHandle;
            }

            var completionSource = new TaskCompletionSource<ErrorCode>();
            TransportAsync(client, errorCode => completionSource.TrySetResult(errorCode));
            return completionSource.Task.Result;
        }

        /// <summary>
        /// 传输数据
        /// </summary>
        /// <param name="client">客户端</param>
        /// <param name="completedCallback">传输完成回调</param>        
        private static async void TransportAsync(CYarpClient client, Action<ErrorCode> completedCallback)
        {
            try
            {
                await client.TransportAsync(default);
                completedCallback(ErrorCode.NoError);
            }
            catch (CYarpConnectException ex)
            {
                var errorCode = (ErrorCode)(int)ex.ErrorCode;
                completedCallback(errorCode);
            }
            catch (Exception)
            {
                completedCallback(ErrorCode.NoError);
            }
        }
    }
}
