[README](README.md) | [中文文档](README_zh.md)

## CYarp
A reverse proxy toolkit to help you expose multiple local http servers behind a NAT or firewall to the internet. It currently supports `http/1.1 over tcp` and `http/1.1 over http2` tunnel.

### Features
1. Use high-performance [kestrel](https://learn.microsoft.com/zh-cn/aspnet/core/fundamentals/servers/kestrel?view=aspnetcore-8.0) as server
2. Use high-performance [YARP](https://github.com/microsoft/reverse-proxy) for http forwarding
3. Designed as middleware for asp.netcore
4. Open server-client interaction protocol
5. Provides .NET, C/C++ client libraries

### Network structure

![net](images/net.png)


### Apache Bench

CYarp and frp are deployed simultaneously on an Intel(R) Xeon(R) CPU E5-2650 v2 @ 2.60GHz CentOS Linux 7 (Core) system machine. The ab tool for testing is on another machine on the LAN. The test sequence is the parameter order from top to bottom of the table.

| Product    | Parameters of ab | Requests per second | Percentage of the requests |
| ---------- | ---------------- | ------------------- | -------------------------- |
| CYarp      | -c 1 -n 10000    | 522.59              | P95=2 P99=2                |
| frp_0.56.0 | -c 1 -n 10000    | 432.88              | P95=3 P99=3                |
| CYarp      | -c 10 -n 50000   | 6484.65             | P95=2 P99=3                |
| frp_0.56.0 | -c 10 -n 50000   | 5532.75             | P95=3 P99=4                |
| CYarp      | -c 20 -n 100000  | 9999.64             | P95=3 P99=4                |
| frp_0.56.0 | -c 20 -n 100000  | 5966.92             | P95=5 P99=6                |
| CYarp      | -c 50 -n 200000  | 12228.78            | P95=6 P99=8                |
| frp_0.56.0 | -c 50 -n 200000  | 5552.32             | P95=15 P99=20              |
| CYarp      | -c 100 -n 500000 | 12631.29            | P95=11 P99=15              |
| frp_0.56.0 | -c 100 -n 500000 | 5231.71             | P95=34 P99=49              |

###  Demo and experience

1. Run the `Host/CYarpServer` project
2. Run the `Host/CYarpClient` project
3. When PostMan requests `http://localhost`, it receives `401` authorization failed
4. Add PostMan's Auth, select Bearer Token, and use the following test token to request

> test token

```
eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJNb2JpbGUiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9zaWQiOiJNb2JpbGUwMDEiLCJDbGllbnRJZCI6IkNsaWVudDAwMSIsImlhdCI6MTcxMDgxNjQ1MiwibmJmIjoxNzEwODE2NDUyLCJleHAiOjI3MTA5MDI4NTJ9.aC-9pVDvyhXsUub-wzZVttfc34wMtFrARDlUj3BYNFhy3Axr0U93CV_QFUP-m6DYI6gK0HkxUr6xlkWwItIFzvS95TsoMXOARVXlVQEP18_wQRQ0G3WRMmNJ_uElJ4uIcrha_Dr4e0cp38olHdABQgOXZgUNHFAHCY3rqtn6-gyTaTu6qAgoj2imi4tsOYFF_OPrCNkRoJavubzDTTXRB95cGz5kxzTSDdWCuIaktNsWN7WDK864VKyVgwca6ueQJogidvES_x26TZuLF6VNhYEkM6UjUZtT8WiD3nBhi2_dVS7BODMLfSyiFa68k1NK50DDfnYgiFU6Clb24Ra-2A
```

### Development Guide
#### Server side

The [CYarp.Server](https://www.nuget.org/packages/CYarp.Server/) package is designed as an http middleware for asp.net core. It relies on the `Authentication` middleware. Use the following methods to register and configure the middleware.

```c#
builder.Services.AddCYarp().Configure(cyarp=>
{
    ...
});
```

The middlewares configuration sequence is as follows
```c#
...
app.UseAuthentication();
...
app.UseCYarp();
...
```

Finally, handle the http forwarding in the Controller, endpoint handler or the last custom middleware.
```c#
// Authorization verification of the requester, here the role is verified
[Authorize(Roles = "Mobile")]
public class CYarpController : ControllerBase
{ 
    private static readonly string clientIdClaimType = "ClientId";

    /// <summary>
    /// Handle cyarp
    /// The core operation is to get the clientId from the request context
    /// Then use clientId to get IClient from IClientViewer service to forward HttpContext
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

#### Client side

Using the [CYarp.Client](https://www.nuget.org/packages/CYarp.Client/) package, you can develop the client by `.NET` as follows
```c#
var options = this.clientOptions.CurrentValue;
using var client = new CYarpClient(options);
await client.TransportAsync(stoppingToken);
```

For C and C++ clients, you can [AOT compile](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/?tabs=net8plus%2Cwindows) the source code of the `CYarp.Client` project into a dynamic shared library exported from C for use. The [Host/CYarpClient.Native](https://github.com/xljiulang/CYarp/blob/master/Host/CYarpClient.Native) project is a C and C++ client demo, and you need to run `Host/CYarpServer` first for the server.

The following are the dynamic shared library commands exported by AOT compilation into C for the CYarp.Client project

| Platform and architecture | command                                                     |
| ------------------------- | ----------------------------------------------------------- |
| win-x64                   | dotnet publish -c Release /p:PublishAot=true -r win-x64     |
| linux-x64                 | dotnet publish -c Release /p:PublishAot=true -r linux-x64   |
| linux-arm64               | dotnet publish -c Release /p:PublishAot=true -r linux-arm64 |


### CYarp protocol
#### Interaction process
![cyarp](images/cyarp.png)


#### Establish a long connection
> by http/1.1

Client initiates the following request
```
Get / HTTP/1.1
Connection: Upgrade
Upgrade: CYarp
Authorization：{Client identity information}
CYarp-TargetUri: {Access Uri of target httpServer}
```

If the server is verified successfully, it will respond with a `101` status code and Set-Cookie is optional.
```
HTTP/1.1 101 Switching Protocols
Connection: Upgrade
Set-Cookie: <load balancer cookie>
```

At this time, the long connection based on `tcp` has been completed, and then the following Stream in the long connection must implement the following functions

| Sender | Content          | Intention                                               | Receiver's actions                   |
| ------ | ---------------- | ------------------------------------------------------- | ------------------------------------ |
| Client | `PING\r\n`       | Detect server survival                                  | Reply `PONG\r\n`                     |
| Server | `PING\r\n`       | Detect client survival                                  | Reply `PONG\r\n`                     |
| Server | `{tunnelId}\r\n` | Let the Client to create a new HttpTunnel to the Server | Create HttpTunnel using `{tunnelId}` |  

> by http/2.0

Client initiates the following request
```
:method = CONNECT
:protocol = CYarp
:scheme = https
:path = /
Authorization = {Client identity information}
CYarp-TargetUri = {Access Uri of target httpServer}
```

If the server is verified successfully, it will respond with a `200` status code and Set-Cookie is optional. 
```
:status = 200
Set-Cookie = <load balancer cookie>
```

At this time, the long connection based on `http/2.0` has been completed, and then the following Stream in the long connection must implement the following functions

| Sender | Content          | Intention                                               | Receiver's actions                   |
| ------ | ---------------- | ------------------------------------------------------- | ------------------------------------ |
| Client | `PING\r\n`       | Detect server survival                                  | Reply `PONG\r\n`                     |
| Server | `PING\r\n`       | Detect client survival                                  | Reply `PONG\r\n`                     |
| Server | `{tunnelId}\r\n` | Let the Client to create a new HttpTunnel to the Server | Create HttpTunnel using `{tunnelId}` |  
  

#### Creation of HttpTunnel
> by http/1.1

Client send the following request
```
Get /{tunnelId} HTTP/1.1
Connection: Upgrade
Upgrade: CYarp
Cookie：<if have Set-Cookie>
```

If the server is verified successfully, it will respond with a `101` status code and Set-Cookie is optional.
```
HTTP/1.1 101 Switching Protocols
Connection: Upgrade
Set-Cookie: <load balancer cookie>
```

At this time, the creation of the HttpTunnel over `tcp` has been completed, and then the server will send an http/1.1 request to the client and receive the client's http1.1 response in the subsequent Stream.

> by http/2.0

Client send the following request
```
:method = CONNECT
:protocol = CYarp
:scheme = https
:path = /{tunnelId}
Cookie = <if have Set-Cookie>
```

If the server is verified successfully, it will respond with a `200` status code and Set-Cookie is optional.
```
:status = 200
Set-Cookie = <load balancer cookie>
```

At this time, the creation of the HttpTunnel over `http/2.0` has been completed, and then the server will send an http/1.1 request to the client and receive the client's http1.1 response in the subsequent Stream.

### Security
When the server side uses https, the following parts are tls secure transmission
1. The long connection establishment process and the subsequent Stream of the long connection
2. The creation process of HttpTunnel and its subsequent Stream

If the TargetUri of the http server is also https, the traffic in HttpTunnel will appear as tls in tls.
