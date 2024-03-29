[README](README.md) | [中文文档](README_zh.md)

## CYarp
A reverse proxy toolkit to help you expose multiple local http servers behind a NAT or firewall to the internet. It currently supports four connection methods: `HTTP/1.1 Upgrade`, `HTTP/2 Extended CONNECT`, `WebSocket` and `WebSocket over Http/2`.

### Features
1. Use high-performance [kestrel](https://learn.microsoft.com/zh-cn/aspnet/core/fundamentals/servers/kestrel?view=aspnetcore-8.0) as server
2. Use high-performance [YARP](https://github.com/microsoft/reverse-proxy) for http forwarding
3. Designed as middleware for asp.netcore
4. Open server-client interaction protocol
5. Provides .NET, C/C++ client libraries

### Network structure

![net](images/net.png)


### Apache Bench

Nginx, CYarp and frp_0.56.0 are deployed simultaneously on an Intel(R) Xeon(R) CPU E5-2650 v2 @ 2.60GHz CentOS Linux 7 (Core) system machine. The ab tool for testing is on another machine on the LAN. The test sequence is the parameter order from top to bottom of the table.

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

The [CYarp.Server](https://www.nuget.org/packages/CYarp.Server/) package is designed as an http middleware for asp.net core. By default, it relies on the `Authentication` middleware to verify the IClient connection. Use the following methods to register and configure the middleware.

```c#
builder.Services.AddAuthentication(<DefaultScheme>).AddYourScheme();
builder.Services.AddCYarp().Configure(cyarp=>{ ... });

var app = builder.Build();
app.UseAuthentication();
app.UseCYarp();
...
// app.UseAuthorization();
// app.MapControllers();
app.Run();
```
Authentication and authorization when connecting to IClient can be skipped using the following method
```c#
builder.Services.AddCYarp().Configure(cyarp=>{ ... });

var app = builder.Build();
app.UseCYarp().AllowAnonymous();
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
    /// <param name="clientViewer"></param>
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
> by HTTP/1.1

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

> by HTTP/2

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

At this time, the long connection based on `HTTP/2` has been completed, and then the following Stream in the long connection must implement the following functions

| Sender | Content          | Intention                                               | Receiver's actions                   |
| ------ | ---------------- | ------------------------------------------------------- | ------------------------------------ |
| Client | `PING\r\n`       | Detect server survival                                  | Reply `PONG\r\n`                     |
| Server | `PING\r\n`       | Detect client survival                                  | Reply `PONG\r\n`                     |
| Server | `{tunnelId}\r\n` | Let the Client to create a new HttpTunnel to the Server | Create HttpTunnel using `{tunnelId}` |

> by WebSocket

WebSocket connection requires the following request header, requesting the `/` path. After the connection is successful, multiple binary frames are used to carry CYarp's Stream.
| HeaderName             | HeaderValue                       |
| ---------------------- | --------------------------------- |
| Authorization          | {Client identity information}     |
| CYarp-TargetUri        | {Access Uri of target httpServer} |
| Sec-WebSocket-Protocol | `CYarp`                           |


#### Creation of HttpTunnel
> by HTTP/1.1

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

At this time, the creation of the HttpTunnel over `tcp` has been completed, and then the server will send an `HTTP/1.1` request to the client and receive the client's `HTTP/1.1` response in the subsequent Stream.

> by HTTP/2

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

At this time, the creation of the HttpTunnel over `HTTP/2` has been completed, and then the server will send an `HTTP/1.1` request to the client and receive the client's `HTTP/1.1` response in the subsequent Stream.

> by WebSocket

WebSocket connection requires the following request header, requesting the `/{tunnelId}` path. After the connection is successful, multiple binary frames are used to carry CYarp's Stream.

| HeaderName             | HeaderValue                       |
| ---------------------- | --------------------------------- |
| Sec-WebSocket-Protocol | `CYarp`                           |


### Security
When the server side uses https, the following parts are tls secure transmission
1. The long connection establishment process and the subsequent Stream of the long connection
2. The creation process of HttpTunnel and its subsequent Stream

If the TargetUri of the http server is also https, the traffic in HttpTunnel will appear as tls in tls.
