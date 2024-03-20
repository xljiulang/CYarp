using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CYarp.Client
{
    /// <summary>
    /// AOT编译
    /// </summary>
    static class AOT
    {
        /// <summary>
        /// 传输错误枚举
        /// </summary>
        public enum TransportError : int
        {
            NoError = 0,
            InvalidHandle = 1,
            ParameterError = 2,
            ConnectError = 3,
        }

        /// <summary>
        /// 客户端选项
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct ClientOptions
        {
            public nint ServerUri;

            public nint TargetUri;

            public nint Authorization;

            public int ConnectTimeout;
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
        public static TransportError Transport(nint clientPtr, ClientOptions clientOptions)
        {
            var gcHandle = GCHandle.FromIntPtr(clientPtr);
            if (gcHandle.IsAllocated == false || gcHandle.Target is not CYarpClient client)
            {
                return TransportError.InvalidHandle;
            }

            if (clientOptions.ServerUri == default ||
                clientOptions.TargetUri == default ||
                clientOptions.Authorization == default ||
                Uri.TryCreate(Marshal.PtrToStringAnsi(clientOptions.ServerUri), UriKind.Absolute, out var serverUri) == false ||
                Uri.TryCreate(Marshal.PtrToStringAnsi(clientOptions.TargetUri), UriKind.Absolute, out var targetUri) == false)
            {
                return TransportError.ParameterError;
            }

            var options = new CYarpClientOptions
            {
                ServerUri = serverUri,
                TargetUri = targetUri,
                Authorization = Marshal.PtrToStringAnsi(clientOptions.Authorization)!,
            };

            if (clientOptions.ConnectTimeout > 0)
            {
                options.ConnectTimeout = TimeSpan.FromSeconds(clientOptions.ConnectTimeout);
            }

            try
            {
                client.TransportAsync(options, default).Wait();
                return TransportError.NoError;
            }
            catch (ArgumentException)
            {
                return TransportError.ParameterError;
            }
            catch (Exception)
            {
                return TransportError.ConnectError;
            }
        }
    }
}
