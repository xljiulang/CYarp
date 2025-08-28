using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CYarp.Client
{
    /// <summary>
    /// AOT compilation
    /// </summary>
    static class AOT
    {
        /// <summary>
        /// Error codes
        /// </summary>
        public enum ErrorCode : int
        {
            /// <summary>
            /// Invalid handle
            /// </summary>
            InvalidHandle = -1,

            /// <summary>
            /// No error
            /// </summary>
            NoError = 0,

            /// <summary>
            /// Failed to connect to server
            /// </summary>
            ConnectFailure = 1,

            /// <summary>
            /// Connection to server timed out
            /// </summary>
            ConnectTimeout = 2,

            /// <summary>
            /// Server authentication failed
            /// </summary>
            ConnectUnauthorized = 3,

            /// <summary>
          	/// Connection rejected (authorization failed)
            /// </summary>
            ConnectForbid = 4,
        }

        /// <summary>
        /// Client options
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct ClientOptions
        {
            public nint ServerUri;
            public nint TargetUri;
            public nint TargetUnixDomainSocket;
            public int ConnectTimeout;
            public unsafe delegate* unmanaged[Cdecl]<nint, nint, void> TunnelErrorCallback;
        }

        /// <summary>
        /// Create client
        /// </summary>
        /// <returns>Client handle</returns>
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
        /// Set connection headers
        /// </summary>
        /// <param name="clientPtr">Client handle</param>
        /// <param name="headerName">Header name</param>
        /// <param name="headerValue">Header value</param>
        /// <returns></returns>
        [UnmanagedCallersOnly(EntryPoint = "CYarpClientSetConnectHeader", CallConvs = [typeof(CallConvCdecl)])]
        public static ErrorCode CYarpClientSetConnectHeader(nint clientPtr, nint headerName, nint headerValue)
        {
            var gcHandle = GCHandle.FromIntPtr(clientPtr);
            if (gcHandle.IsAllocated == false || gcHandle.Target is not CYarpClient client)
            {
                return ErrorCode.InvalidHandle;
            }

            var name = Marshal.PtrToStringUni(headerName);
            var value = Marshal.PtrToStringUni(headerValue);
            client.SetConnectHeader(name, value);
            return ErrorCode.NoError;
        }

        /// <summary>
        /// Free client
        /// </summary>
        /// <param name="clientPtr">Client handle</param>
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
        /// Synchronous data transport
        /// </summary>
        /// <param name="clientPtr">Client handle</param> 
        /// <returns>Transport error code</returns>
        [UnmanagedCallersOnly(EntryPoint = "CYarpClientTransport", CallConvs = [typeof(CallConvCdecl)])]
        public static ErrorCode CYarpClientTransport(nint clientPtr)
        {
            return Transport(clientPtr);
        }

        /// <summary>
        /// Asynchronous data transport
        /// </summary>
        /// <param name="clientPtr">Client handle</param>
        /// <param name="completedCallback">Transport completion callback, null for synchronous call</param>
        /// <returns>Transport error code</returns>
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
        /// Synchronous data transport
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
        /// Transport data
        /// </summary>
        /// <param name="client">Client</param>
        /// <param name="completedCallback">Transport completion callback</param>        
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
