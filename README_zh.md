[README](README.md) | [中文文档](README_zh.md)

## CYarp
CYarp是一组高性能的反向代理组件，用于将 NAT 或防火墙后面的多个本地 http 服务器公开到互联网。 目前它支持`http/1.1 over tcp`和`http/1.1 over http2`隧道。

### 功能特性
1. 使用高性能的[kestrel](https://learn.microsoft.com/zh-cn/aspnet/core/fundamentals/servers/kestrel?view=aspnetcore-8.0)做服务器
2. 使用高性能的[YARP](https://github.com/microsoft/reverse-proxy)做http转发
3. 设计为asp.netcore的中间件，集成简单
4. 开放的服务端与客户端交互协议
5. 提供了.NET、C/C++客户端库

###  网络架构

![net](images/net.png)


### ab压测

Nginx、CYarp和frp_0.56.0在一台Intel(R) Xeon(R) CPU E5-2650 v2 @ 2.60GHz的CentOS Linux 7 (Core)系统机器上同时部署，压测时ab在局域网另一台机器上，压测顺序为表格上到下的参数顺序。


#### ab -c 1 -n 10000
| Product          | Requests per second | Rps Ratio | P95 | P99 |
| ---------------- | ------------------- | --------- | --- | --- |
| ab->nginx        | 1539.22             | 1.00      | 1   | 1   |
| ab->cyarp->nginx | 700.31              | 0.45      | 2   | 2   |
| ab->frp->nginx   | 593.76              | 0.39      | 2   | 2   |

#### ab -c 10 -n 50000
| Product          | Requests per second | Rps Ratio | P95 | P99 |
| ---------------- | ------------------- | --------- | --- | --- |
| ab->nginx        | 9915.55             | 1.00      | 3   | 4   |
| ab->cyarp->nginx | 9563.64             | 0.96      | 1   | 2   |
| ab->frp->nginx   | 5980.79             | 0.60      | 3   | 4   |

#### ab -c 20 -n 100000
| Product          | Requests per second | Rps Ratio | P95 | P99 |
| ---------------- | ------------------- | --------- | --- | --- |
| ab->nginx        | 11948.84            | 1.00      | 4   | 7   |
| ab->cyarp->nginx | 12542.54            | 1.05      | 3   | 3   |
| ab->frp->nginx   | 6238.09             | 0.52      | 5   | 7   |

#### ab -c 50 -n 200000
| Product          | Requests per second | Rps Ratio | P95 | P99 |
| ---------------- | ------------------- | --------- | --- | --- |
| ab->nginx        | 12801.34            | 1.00      | 6   | 12  |
| ab->cyarp->nginx | 13472.69            | 1.05      | 6   | 7   |
| ab->frp->nginx   | 5675.19             | 0.44      | 20  | 49  |

#### ab -c 100 -n 500000
| Product          | Requests per second | Rps Ratio | P95 | P99 |
| ---------------- | ------------------- | --------- | --- | --- |
| ab->nginx        | 14088.43            | 1.00      | 10  | 17  |
| ab->cyarp->nginx | 14216.45            | 1.01      | 10  | 12  |
| ab->frp->nginx   | 6504.36             | 0.46      | 20  | 49  |



### Demo示例
1. 运行Host/CYarpServer
2. 运行Host/CYarpClient
3. 在PostMan请求到`http://localhost`，此时收到401授权未通过
4. 添加PostMan的Auth，选择Bearer Token，使用如下的测试Token来请求

> 测试Token

```
eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJNb2JpbGUiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9zaWQiOiJNb2JpbGUwMDEiLCJDbGllbnRJZCI6IkNsaWVudDAwMSIsImlhdCI6MTcxMDgxNjQ1MiwibmJmIjoxNzEwODE2NDUyLCJleHAiOjI3MTA5MDI4NTJ9.aC-9pVDvyhXsUub-wzZVttfc34wMtFrARDlUj3BYNFhy3Axr0U93CV_QFUP-m6DYI6gK0HkxUr6xlkWwItIFzvS95TsoMXOARVXlVQEP18_wQRQ0G3WRMmNJ_uElJ4uIcrha_Dr4e0cp38olHdABQgOXZgUNHFAHCY3rqtn6-gyTaTu6qAgoj2imi4tsOYFF_OPrCNkRoJavubzDTTXRB95cGz5kxzTSDdWCuIaktNsWN7WDK864VKyVgwca6ueQJogidvES_x26TZuLF6VNhYEkM6UjUZtT8WiD3nBhi2_dVS7BODMLfSyiFa68k1NK50DDfnYgiFU6Clb24Ra-2A
```

### 开发指南
#### 服务端开发

[CYarp.Server](https://www.nuget.org/packages/CYarp.Server/) 包设计为asp.net core的一个http中间件，默认情况下其依赖于身份验证服务和验证方案来验证IClient的连接，使用如下方法进行注册和中间件的配置。

```c#
builder.Services.AddAuthentication(<DefaultScheme>).AddYourScheme();
builder.Services.AddCYarp().Configure(cyarp=>{ ... });

var app = builder.Build();
app.UseCYarp();
...
// app.UseAuthentication();
// app.UseAuthorization();
// app.MapControllers();
app.Run();
```

使用以下方式，可以跳过IClient连接时的身份验证和授权
```c#
builder.Services.AddCYarp().Configure(cyarp=>{ ... });

var app = builder.Build();
app.UseCYarp().AllowAnonymous();
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

#### 客户端开发

使用[CYarp.Client](https://www.nuget.org/packages/CYarp.Client/)包，很方便完成.NET客户端开发
```c#
var options = this.clientOptions.CurrentValue;
using var client = new CYarpClient(options);
await client.TransportAsync(stoppingToken);
```

C和C++客户端，可以将CYarp.Client项目的源代码[AOT编译](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/?tabs=net8plus%2Cwindows) 为C导出的动态共享库来使用，[Host/CYarpClient.Native](https://github.com/xljiulang/CYarp/blob/master/Host/CYarpClient.Native)项目是C和C++客户端Demo，需要先运行Host/CYarpServer做为调试的服务端。

以下是CYarp.Client项目AOT编译为C导出的动态共享库命令：

| 系统和框架  | 命令                                                        |
| ----------- | ----------------------------------------------------------- |
| win-x64     | dotnet publish -c Release /p:PublishAot=true -r win-x64     |
| linux-x64   | dotnet publish -c Release /p:PublishAot=true -r linux-x64   |
| linux-arm64 | dotnet publish -c Release /p:PublishAot=true -r linux-arm64 |


###  CYarp协议

#### 交互流程
![cyarp](images/cyarp.png)


####  建立长连接
> http/1.1

Client发起如下请求
```
Get / HTTP/1.1
Connection: Upgrade
Upgrade: CYarp
Authorization：{客户端身份信息}
CYarp-TargetUri: {目标httpServer的访问Uri}
```

Server验证通过则响应`101`状态码，还可能携带Set-Cookie的响应头
```
HTTP/1.1 101 Switching Protocols
Connection: Upgrade
Set-Cookie: <load balancer cookie>
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

Server验证通过则响应`200`状态码，还可能携带Set-Cookie的响应头
```
:status = 200
Set-Cookie = <load balancer cookie>
```

此时基于http/2.0的长连接已完成，接着在长连接后续的Stream要实现如下功能

| 发起方 | 内容                 | 含义                               | 接收方操作                     |
| ------ | -------------------- | ---------------------------------- | ------------------------------ |
| Client | 发送`PING\r\n`       | 侦测Server存活                     | 回复`PONG\r\n`                 |
| Server | 发送`PING\r\n`       | 侦测Client存活                     | 回复`PONG\r\n`                 |
| Server | 发送`{tunnelId}\r\n` | 让Client向Server创建新的HttpTunnel | 使用`{tunnelId}`创建HttpTunnel |
  

#### HttpTunnel的创建
> http/1.1

Client发起如下请求
```
Get /{tunnelId} HTTP/1.1
Connection: Upgrade
Upgrade: CYarp
Cookie：<if have Set-Cookie>
```

Server验证通过则响应`101`状态码，还可能携带Set-Cookie的响应头
```
HTTP/1.1 101 Switching Protocols
Connection: Upgrade
Set-Cookie: <load balancer cookie>
```

此时基于tcp的HttpTunnel创建已完成，接着服务端将在后续的Stream里向客户端发送http/1.1的请求和接收客户端的http1.1响应。

> http/2.0

Client发起如下请求，参考[rfc8441](https://www.rfc-editor.org/rfc/rfc8441#section-4)
```
:method = CONNECT
:protocol = CYarp
:scheme = https
:path = /{tunnelId}
Cookie = <if have Set-Cookie>
```

Server验证通过则响应`200`状态码，还可能携带Set-Cookie的响应头
```
:status = 200
Set-Cookie = <load balancer cookie>
```

此时基于http/2.0的HttpTunnel创建已完成，接着服务端将在后续的Stream里向客户端发送http/1.1的请求和接收客户端的http1.1响应。

### 安全
当Server方使用https时，以下部分为tls安全传输
1. 长连接建立过程和长连接的后续Stream
2. HttpTunnel的创建过程和其后续Stream

如果目标服务httpServer的TargetUri也是https，则HttpTunnel里面的流量表现为tls in tls。

CYarp不涉及到任何业务协议，Client的身份认证依赖于asp.net core平台的身份认证中间件，而http转发部分(例如`Host\CYarpServer.CYarpController`)是由开发者自行开发来决定是否要转发，涉及的授权验证逻辑由开发者自行验证。

###  负载均衡
负载均衡的主要作用是将海量的Client端由多个CYarp.Server服务器实例来直接或间接分担承载。

![balance](images/balance.png)

**SLB层**

SLB层需要开启基于IP地址的TCP会话保持，即来自同一IP地址的访问请求会转发到同一台后端CYarp.Server服务器上。如果没有SLB层，也可以让Client端实现客户端负载均衡，即客户端自行决定直接连接到后端的哪台CYarp.Server服务器上。

**CYarp.Server层**

CYarp.Server服务器需要基于redis等实现`IClientStateStorage`服务，即以IClient的Id值做为RedisKey、Node值做RedisValue持久化。当存在SLB时，这些CYarp.Server服务不再要求有从公网访问的要求。

**http网关层**

需要基于YARP自主开发CYarp.Server服务的http网关，从http请求上下文获取ClientId，然后从redis以ClientId值做为RedisKey反查得到Node值，最后把http请求上下文转发到这个Node对应的CYarp.Server服务。http网关一般不需要自身再做负载均衡，虽然只有一个http网关时它承载了所有用户端的http请求流量，但它的功能单一性能强悍。