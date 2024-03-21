## CYarp
基于Yarp的http内网穿透中间件。支持tcp、http/2.0或http/3.0作为http/1.1的传输层

![net](net.png)
![cyarp](cyarp.png)


### 1 如何使用
#### 1.1 Demo项目
1. 运行CYarp.Hosting
2. 在PostMan请求到`http://localhost`，此时收到401授权未通过
3. 添加PostMan的Auth，选择Bearer Token，放如下的请求Token

> 请求Token

```
eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJNb2JpbGUiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9zaWQiOiJNb2JpbGUwMDEiLCJDbGllbnRJZCI6IkNsaWVudDAwMSIsImlhdCI6MTcxMDgxNjQ1MiwibmJmIjoxNzEwODE2NDUyLCJleHAiOjI3MTA5MDI4NTJ9.aC-9pVDvyhXsUub-wzZVttfc34wMtFrARDlUj3BYNFhy3Axr0U93CV_QFUP-m6DYI6gK0HkxUr6xlkWwItIFzvS95TsoMXOARVXlVQEP18_wQRQ0G3WRMmNJ_uElJ4uIcrha_Dr4e0cp38olHdABQgOXZgUNHFAHCY3rqtn6-gyTaTu6qAgoj2imi4tsOYFF_OPrCNkRoJavubzDTTXRB95cGz5kxzTSDdWCuIaktNsWN7WDK864VKyVgwca6ueQJogidvES_x26TZuLF6VNhYEkM6UjUZtT8WiD3nBhi2_dVS7BODMLfSyiFa68k1NK50DDfnYgiFU6Clb24Ra-2A
```

#### 1.2 开发指南
> 服务端开发

`CYarp.Server`设计为asp.netcore的一个http中间件，其依赖于Authentication身份认证中间件，使用如下方法进行注册和中间件的配置。

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

最后在Controller、minapi的处理者或中间件中处理http转发
```c#
// 请求者的授权验证
[Authorize(Roles = "Mobile")]
public class CYarpController : ControllerBase
{ 
    private static readonly string clientIdClaimType = "ClientId";

    /// <summary>
    /// 处理cyarp
    /// 核心操作是从请求上下文获取clientId
    /// 然后使用clientId从clientManager获取client来转发http
    /// </summary>
    /// <param name="clientManager"></param>
    /// <returns></returns>
    [Route("/{**cyarp}")]
    public async Task InvokeAsync([FromServices] IClientManager clientManager)
    {
        var clientId = this.User.FindFirstValue(clientIdClaimType);
        if (clientId != null && clientManager.TryGetValue(clientId, out var client))
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

使用`CYarp.Client`包很方便完成.NET客户端开发
```c#
 using var client = new CYarpClient();
 while (true))
 {
     await client.TransportAsync(this.clientOptions.CurrentValue, stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
 }
```

C和C++客户端，可以将CYarp.Client项目的源代码AOT编译为C导出的动态共享库来使用，[CYarp.Client.Native](https://github.com/xljiulang/CYarp/blob/master/CYarp.Client.Native)项目是C和C++调用Demo。

以下是CYarp.Client项目[AOT编译](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/?tabs=net8plus%2Cwindows)编译为C导出的动态共享库命令：

| 系统和框架  | 命令                                                        |
| ----------- | ----------------------------------------------------------- |
| win-x64     | dotnet publish -c Release /p:PublishAot=true -r win-x64     |
| linux-x64   | dotnet publish -c Release /p:PublishAot=true -r linux-x64   |
| linux-arm64 | dotnet publish -c Release /p:PublishAot=true -r linux-arm64 |


### 2 连接协议
#### 2.1 Client握手协议
> http/1.1

客户端发起如下请求
```
Get / HTTP/1.1
Connection: Upgrade
Upgrade: CYarp
Authorization：{客户端身份}
CYarp-Destination: {URI}
```

服务端验证通过则响应101状态码
```
HTTP/1.1 101 Switching Protocols
Connection: Upgrade
```

接着服务端将在连接后续的Stream里向客户端发送{tunnelId}\r\n的tunnelId值，指示客户端向服务端创建tunnel。

> http/2.0

客户端发起如下请求
```
:method = CONNECT
:protocol = CYarp
:scheme = https
:path = /
Authorization = {客户端身份}
CYarp-Destination = {URI}
```

服务端验证通过则响应202状态码
```
:status = 200
```

接着服务端将在连接后续的Stream里向客户端发送{tunnelId}\r\n的tunnelId值，指示客户端向服务端创建tunnel。


#### 2.2 Tunnel的创建
> http/1.1

客户端发起如下请求
```
Get /{tunnelId} HTTP/1.1
Connection: Upgrade
Upgrade: CYarp
```

服务端验证通过则响应101状态码
```
HTTP/1.1 101 Switching Protocols
Connection: Upgrade
```

接着服务端将在连接后续的Stream里向客户端发送http/1.1的请求和接收客户端的http响应。

> http/2.0

客户端发起如下请求
```
:method = CONNECT
:protocol = CYarp
:scheme = https
:path = /{tunnelId}
```

服务端验证通过则响应200状态码
```
:status = 200
```

接着服务端将在连接后续的Stream里向客户端发送http/1.1的请求和接收客户端的http响应。

### 3 安全传输
当服务端为https时，以下部分为tls安全传输
1. Client握手协议和其连接
2. Tunnel的创建和其连接

如果目标服务(Destination)也为https，则整个管道表现为tls in tls。