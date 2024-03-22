#include <stdint.h>

typedef void* CYarpClient;

typedef void (*CYarpTunnelErrorCallback)(
	// 错误类型
	char16_t* type,
	// 错误消息
	char16_t* message);

// 传输错误枚举
enum CYarpTransportError
{
	// client句柄无效
	InvalidHandle = -1,

	// 传输完成
	Completed = 0,

	// 连接到服务器失败
	ConnectFailure = 1,

	// 连接到服务器已超时
	ConnectTimedout = 2,

	// 连接到服务身份认证不通过
	ConnectUnauthorized = 3
};

// 客户端选项
struct CYarpClientOptions
{
	// CYarp服务器Uri
	// 支持http和https
	char16_t* ServerUri;
	// 目标服务器Uri
	// 支持http和https
	char16_t* TargetUri;
	// 目标服务器的UnixDomainSocket路径[可选]
	char16_t* TargetUnixDomainSocket;
	// 连接到CYarp服务器的Authorization请求头的值
	char16_t* Authorization;
	// 与server或target的连接超时时长秒数，0默认为5s
	int32_t ConnectTimeout;
	// 隧道传输错误回调
	CYarpTunnelErrorCallback TunnelErrorCallback;
};

// 创建客户端
// 参数不正确时返回NULL
extern "C" CYarpClient CYarpClientCreate(CYarpClientOptions * options);

// 释放客户端
extern "C" void CYarpClientFree(CYarpClient client);

// 传输数据
extern "C" enum CYarpTransportError CYarpClientTransport(CYarpClient client);