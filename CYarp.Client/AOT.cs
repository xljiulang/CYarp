using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CYarp.Client
{
    /// <summary>
    /// AOT编译
    /// </summary>
    unsafe static class AOT
    {
        /// <summary>
        /// 传输错误枚举
        /// </summary>
        public enum TransportError : int
        {
            /// <summary>
            /// client句柄无效
            /// </summary>
            InvalidHandle = -1,

            /// <summary>
            /// 传输完成，表示与服务器的主连接和传输通道都已关闭
            /// </summary>
            Completed = 0,

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
            public delegate* unmanaged[Cdecl]<nint, nint, void> TunnelErrorCallback;
        }

        /// <summary>
        /// 创建客户端
        /// </summary>
        /// <returns>客户端句柄</returns>
        [UnmanagedCallersOnly(EntryPoint = "CreateClient", CallConvs = [typeof(CallConvCdecl)])]
        public static nint CreateClient()
        {
            var client = new CYarpClient();
            var gcHandle = GCHandle.Alloc(client);
            return GCHandle.ToIntPtr(gcHandle);
        }

        /// <summary>
        /// 释放客户端
        /// </summary>
        /// <param name="clientPtr">客户端句柄</param>
        [UnmanagedCallersOnly(EntryPoint = "FreeClient", CallConvs = [typeof(CallConvCdecl)])]
        public static void FreeClient(nint clientPtr)
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
        /// 传输数据
        /// </summary>
        /// <param name="clientPtr">客户端句柄</param>
        /// <param name="clientOptions">选项句柄</param>
        /// <returns>传输错误枚举</returns>
        [UnmanagedCallersOnly(EntryPoint = "Transport", CallConvs = [typeof(CallConvCdecl)])]
        public static TransportError Transport(nint clientPtr, ClientOptions* clientOptions)
        {
            var gcHandle = GCHandle.FromIntPtr(clientPtr);
            if (gcHandle.IsAllocated == false || gcHandle.Target is not CYarpClient client)
            {
                return TransportError.InvalidHandle;
            }

            if (clientOptions == null ||
                clientOptions->ServerUri == default ||
                clientOptions->TargetUri == default ||
                clientOptions->Authorization == default ||
                Uri.TryCreate(Marshal.PtrToStringUni(clientOptions->ServerUri), UriKind.Absolute, out var serverUri) == false ||
                Uri.TryCreate(Marshal.PtrToStringUni(clientOptions->TargetUri), UriKind.Absolute, out var targetUri) == false)
            {
                return TransportError.InvalidOptions;
            }

            var options = new CYarpClientOptions
            {
                ServerUri = serverUri,
                TargetUri = targetUri,
                Authorization = Marshal.PtrToStringUni(clientOptions->Authorization)!,
            };

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
                client.TransportAsync(options, default).Wait();
                return TransportError.Completed;
            }
            catch (AggregateException ex) when (ex.InnerException is CYarpClientException clientException)
            {
                return (TransportError)(int)clientException.ErrorCode;
            }
            catch (CYarpClientException ex)
            {
                return (TransportError)(int)ex.ErrorCode;
            }
            catch (Exception)
            {
                return TransportError.Completed;
            }
        }
    }
}
