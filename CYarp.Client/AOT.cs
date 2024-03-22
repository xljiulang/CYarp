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
            /// 连接到服务器失败
            /// </summary>
            ConnectFailure = 1,

            /// <summary>
            /// 连接到服务器已超时
            /// </summary>
            ConnectTimedout = 2,

            /// <summary>
            /// 连接到服务身份认证不通过
            /// </summary>
            ConnectUnauthorized = 3,
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
        [UnmanagedCallersOnly(EntryPoint = "CYarpClientCreate", CallConvs = [typeof(CallConvCdecl)])]
        public static nint CYarpClientCreate(ClientOptions* clientOptions)
        {
            if (clientOptions == null ||
                clientOptions->ServerUri == default ||
                clientOptions->TargetUri == default ||
                clientOptions->Authorization == default ||
                Uri.TryCreate(Marshal.PtrToStringUni(clientOptions->ServerUri), UriKind.Absolute, out var serverUri) == false ||
                Uri.TryCreate(Marshal.PtrToStringUni(clientOptions->TargetUri), UriKind.Absolute, out var targetUri) == false)
            {
                return nint.Zero;
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
        /// 传输数据
        /// </summary>
        /// <param name="clientPtr">客户端句柄</param> 
        /// <returns>传输错误枚举</returns>
        [UnmanagedCallersOnly(EntryPoint = "CYarpClientTransport", CallConvs = [typeof(CallConvCdecl)])]
        public static TransportError CYarpClientTransport(nint clientPtr)
        {
            var gcHandle = GCHandle.FromIntPtr(clientPtr);
            if (gcHandle.IsAllocated == false || gcHandle.Target is not CYarpClient client)
            {
                return TransportError.InvalidHandle;
            }

            try
            {
                client.TransportAsync(default).Wait();
                return TransportError.Completed;
            }
            catch (AggregateException ex) when (ex.InnerException is CYarpConnectException connectException)
            {
                return (TransportError)(int)connectException.ErrorCode;
            }
            catch (CYarpConnectException ex)
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
