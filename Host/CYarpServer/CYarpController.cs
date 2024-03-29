﻿using CYarp.Server;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CYarpServer
{
    [Authorize(Roles = "Mobile")]
    public class CYarpController : ControllerBase
    {
        private static readonly string clientIdClaimType = "ClientId";

        /// <summary>
        /// 处理cyarp
        /// 核心操作是从请求上下文获取clientId
        /// 然后使用clientId从IClientViewer服务获取IClient来转发http
        /// </summary>
        /// <param name="clientViewer">IClient的查看器</param>
        /// <returns></returns>
        [Route("/{**cyarp}")]
        public async Task InvokeAsync([FromServices] IClientViewer clientViewer)
        {
            var clientId = this.User.FindFirstValue(clientIdClaimType);
            if (clientId != null && clientViewer.TryGetValue(clientId, out var client))
            {
                this.Request.Headers.Remove(HeaderNames.Authorization);
                await client.ForwardHttpAsync(this.HttpContext);
            }
            else
            {
                this.Response.StatusCode = StatusCodes.Status502BadGateway;
            }
        }
    }
}
