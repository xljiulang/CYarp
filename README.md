## CYarp
基于Yarp的一对多http内网穿透组件，支持tcp或http/2.0作为http/1.1的传输层。

**功能特性**
1. 使用高性能的[kestrel](https://learn.microsoft.com/zh-cn/aspnet/core/fundamentals/servers/kestrel?view=aspnetcore-8.0)做服务器
2. 使用高性能的[YARP](https://github.com/microsoft/reverse-proxy)做http转发
3. 设计为asp.netcore的中间件，集成简单
4. 开放的服务端与客户端交互协议
5. 提供了.NET、C/C++客户端库

**网络架构**

![net](net.png)


**ab压测**

CYarp和frp在一台Intel(R) Xeon(R) CPU E5-2650 v2 @ 2.60GHz的CentOS Linux 7 (Core)系统机器上同时部署，压测时ab在局域网另一台机器上，压测顺序为表格上到下的参数顺序。

| 产品       | ab参数           | Requests per second | Percentage of the requests |
| ---------- | ---------------- | ------------------- | -------------------------- |
| CYarp      | -c 1 -n 10000    | 446.48              | P95=3 P99=3                |
| frp_0.56.0 | -c 1 -n 10000    | 444.18              | P95=3 P99=3                |
| CYarp      | -c 10 -n 50000   | 6001.57             | P95=2 P99=3                |
| frp_0.56.0 | -c 10 -n 50000   | 5473.53             | P95=3 P99=4                |
| CYarp      | -c 20 -n 100000   | 8640.89             | P95=3 P99=4                |
| frp_0.56.0 | -c 20 -n 100000   | 5897.58             | P95=5 P99=7                |
| CYarp      | -c 50 -n 200000  | 11864.86            | P95=6 P99=8                |
| frp_0.56.0 | -c 50 -n 200000  | 5222.04             | P95=17 P99=29              |
| CYarp      | -c 100 -n 500000 | 12500.28            | P95=11 P99=15              |
| frp_0.56.0 | -c 100 -n 500000 | 5134.38             | P95=35 P99=52              |



### 1 如何使用
#### 1.1 Demo项目
1. 运行Host/CYarpServer和Host/CYarpClient
2. 在PostMan请求到`http://localhost`，此时收到401授权未通过
3. 添加PostMan的Auth，选择Bearer Token，使用如下的测试Token来请求

> 测试Token

```
eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJNb2JpbGUiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9zaWQiOiJNb2JpbGUwMDEiLCJDbGllbnRJZCI6IkNsaWVudDAwMSIsImlhdCI6MTcxMDgxNjQ1MiwibmJmIjoxNzEwODE2NDUyLCJleHAiOjI3MTA5MDI4NTJ9.aC-9pVDvyhXsUub-wzZVttfc34wMtFrARDlUj3BYNFhy3Axr0U93CV_QFUP-m6DYI6gK0HkxUr6xlkWwItIFzvS95TsoMXOARVXlVQEP18_wQRQ0G3WRMmNJ_uElJ4uIcrha_Dr4e0cp38olHdABQgOXZgUNHFAHCY3rqtn6-gyTaTu6qAgoj2imi4tsOYFF_OPrCNkRoJavubzDTTXRB95cGz5kxzTSDdWCuIaktNsWN7WDK864VKyVgwca6ueQJogidvES_x26TZuLF6VNhYEkM6UjUZtT8WiD3nBhi2_dVS7BODMLfSyiFa68k1NK50DDfnYgiFU6Clb24Ra-2A
```

#### 1.2 开发指南
> 服务端开发

`CYarp.Server`包设计为asp.net core的一个http中间件，其依赖于Authentication身份认证中间件，使用如下方法进行注册和中间件的配置。

```c#
builder.Services.AddCYarp(cyarp=>
{
    ...
});
```

中间件配置顺序如下：
```c#
...
app.UseAuthentication();
...
app.UseCYarp();
...
```

最后在Controller、endpoint处理者或者最后一个中间件中处理http转发
```c#
// 请求者的授权验证
[Authorize(Roles = "Mobile")]
public class CYarpController : ControllerBase
{ 
    private static readonly string clientIdClaimType = "ClientId";

    /// <summary>
    /// 处理cyarp
    /// 核心操作是从请求上下文获取clientId
    /// 然后使用clientId从IClientViewer服务获取IClient来转发http
    /// </summary>
    /// <param name="clientManager"></param>
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
```

> 客户端开发

使用`CYarp.Client`包，很方便完成.NET客户端开发
```c#
var options = this.clientOptions.CurrentValue;
using var client = new CYarpClient(options);
await client.TransportAsync(stoppingToken);
```

C和C++客户端，可以将CYarp.Client项目的源代码AOT编译为C导出的动态共享库来使用，[Host/CYarpClient.Native](https://github.com/xljiulang/CYarp/blob/master/Host/CYarpClient.Native)项目是C和C++客户端Demo，需要先运行Host/CYarpServer做为调试的服务端。

以下是CYarp.Client项目[AOT编译](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/?tabs=net8plus%2Cwindows)为C导出的动态共享库命令：

| 系统和框架  | 命令                                                        |
| ----------- | ----------------------------------------------------------- |
| win-x64     | dotnet publish -c Release /p:PublishAot=true -r win-x64     |
| linux-x64   | dotnet publish -c Release /p:PublishAot=true -r linux-x64   |
| linux-arm64 | dotnet publish -c Release /p:PublishAot=true -r linux-arm64 |


### 2 CYarp协议

![cyarp](cyarp.png)
#### 2.1 建立长连接
> http/1.1

Client发起如下请求
```
Get / HTTP/1.1
Connection: Upgrade
Upgrade: CYarp
Authorization：{客户端身份信息}
CYarp-TargetUri: {目标httpServer的访问Uri}
```

Server验证通过则响应101状态码
```
HTTP/1.1 101 Switching Protocols
Connection: Upgrade
```

此时基于tcp的长连接已完成，接着在长连接后续的Stream要实现如下功能

| 发起方 | 内容                 | 含义                               | 接收方操作                     |
| ------ | -------------------- | ---------------------------------- | ------------------------------ |
| Client | 发送`PING\r\n`       | 侦测Server存活                     | 回复`PONG\r\n`                 |
| Server | 发送`PING\r\n`       | 侦测Client存活                     | 回复`PONG\r\n`                 |
| Server | 发送`{tunnelId}\r\n` | 让Client向Server创建新的HttpTunnel | 使用`{tunnelId}`创建HttpTunnel |
  

> http/2.0

Client发起如下请求，参考[rfc8441](https://www.rfc-editor.org/rfc/rfc8441#section-4)
```
:method = CONNECT
:protocol = CYarp
:scheme = https
:path = /
Authorization = {客户端身份信息}
CYarp-TargetUri = {目标httpServer的访问Uri}
```

Server验证通过则响应200状态码
```
:status = 200
```

此时基于http/2.0的长连接已完成，接着在长连接后续的Stream要实现如下功能

| 发起方 | 内容                 | 含义                               | 接收方操作                     |
| ------ | -------------------- | ---------------------------------- | ------------------------------ |
| Client | 发送`PING\r\n`       | 侦测Server存活                     | 回复`PONG\r\n`                 |
| Server | 发送`PING\r\n`       | 侦测Client存活                     | 回复`PONG\r\n`                 |
| Server | 发送`{tunnelId}\r\n` | 让Client向Server创建新的HttpTunnel | 使用`{tunnelId}`创建HttpTunnel |
  

#### 2.2 HttpTunnel的创建
> http/1.1

Client发起如下请求
```
Get /{tunnelId} HTTP/1.1
Connection: Upgrade
Upgrade: CYarp
```

Server验证通过则响应101状态码
```
HTTP/1.1 101 Switching Protocols
Connection: Upgrade
```

此时基于tcp的HttpTunnel创建已完成，接着服务端将在后续的Stream里向客户端发送http/1.1的请求和接收客户端的http1.1响应。

> http/2.0

Client发起如下请求，参考[rfc8441](https://www.rfc-editor.org/rfc/rfc8441#section-4)
```
:method = CONNECT
:protocol = CYarp
:scheme = https
:path = /{tunnelId}
```

Server验证通过则响应200状态码
```
:status = 200
```

此时基于http/2.0的HttpTunnel创建已完成，接着服务端将在后续的Stream里向客户端发送http/1.1的请求和接收客户端的http1.1响应。

### 3 安全传输
当Server方使用https时，以下部分为tls安全传输
1. 长连接建立过程和长连接的后续Stream
2. HttpTunnel的创建过程和其后续Stream

如果目标服务httpServer的TargetUri也是https，则HttpTunnel里面的流量表现为tls in tls。

### 4 业务安全
CYarp不涉及到任何业务协议，Client的身份认证依赖于asp.net core平台的身份认证中间件，而http转发部分(例如`Host\CYarpServer.CYarpController`)是由开发者自行开发来决定是否要转发，涉及的授权验证逻辑由开发者自行验证。

### 5 负载均衡
负载均衡的主要作用是将海量的Client端由多个CYarp.Server服务器实例来直接或间接分担承载。

**SLB层**

SLB层需要开启基于IP地址的TCP会话保持的，即来自同一IP地址的访问请求会转发到同一台后端CYarp.Server服务器上。如果没有SLB层，也可以让Client端实现客户端负载均衡。

**CYarp.Server层**

CYarp.Server服务器需要开发IClientManager服务将IClient的状态持久化到redis的功能，即以IClient的Id做Key，CYarp.Server节点Uri做Value。

**http网关层**

需要基于YARP自主开发网关，从http请求上下文获取Client的Id，然后从redis获取此Id值对应的CYarp.Server节点Uri，最后把http请求上下文转发到这个Uri即可。