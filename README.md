## CYarp
基于YArp的http内网穿透中间件。支持tcp、http/2.0或http/3.0作为http/1.1的传输层

![cyarp](cyarp.png)


### 1 示例
1. 运行CYarp.Hosting
2. 在PostMan请求到`http://localhost`，此时收到401授权未通过
3. 添加PostMan的Auth，选择Bearer Token，放如下的httpClient的Token

> httpClient的Token

```
eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJNb2JpbGUiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9zaWQiOiJNb2JpbGUwMDEiLCJDbGllbnRJZCI6IkNsaWVudDAwMSIsImlhdCI6MTcxMDgxNjQ1MiwibmJmIjoxNzEwODE2NDUyLCJleHAiOjI3MTA5MDI4NTJ9.aC-9pVDvyhXsUub-wzZVttfc34wMtFrARDlUj3BYNFhy3Axr0U93CV_QFUP-m6DYI6gK0HkxUr6xlkWwItIFzvS95TsoMXOARVXlVQEP18_wQRQ0G3WRMmNJ_uElJ4uIcrha_Dr4e0cp38olHdABQgOXZgUNHFAHCY3rqtn6-gyTaTu6qAgoj2imi4tsOYFF_OPrCNkRoJavubzDTTXRB95cGz5kxzTSDdWCuIaktNsWN7WDK864VKyVgwca6ueQJogidvES_x26TZuLF6VNhYEkM6UjUZtT8WiD3nBhi2_dVS7BODMLfSyiFa68k1NK50DDfnYgiFU6Clb24Ra-2A
```

### 2 协议
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