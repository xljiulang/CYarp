#include <stdint.h>

typedef void* PClient;

// 传输错误枚举
enum TransportError
{
	// 传输完成，表示与服务器的主连接和传输通道都已关闭
	Completed = 0,
	// client句柄无效
	InvalidHandle = 1,
	// Options的一些参数错误
	OptionsArgumentError = 2,
	// 建立主连接到服务器异常
	ServerConnectError = 3,
};

// 客户端选项
struct ClientOptions
{
	// CYarp服务器Uri
	// 支持http和https
	char16_t* ServerUri;
	// 目标服务器Uri
	// 支持http、https
	char16_t* TargetUri;
	// 目标服务器的UnixDomainSocket路径[可选]
	char16_t* TargetUnixDomainSocket;
	// 连接到CYarp服务器的Authorization请求头的值
	char16_t* Authorization;
	// 与server或target的连接超时时长秒数，0默认为5s
	int32_t ConnectTimeout;
};

// 创建客户端
extern "C" PClient CreateClient();

// 释放客户端
extern "C" void FreeClient(
	// 客户端句柄
	PClient client);

// 传输数据
extern "C" enum TransportError Transport(
	// 客户端句柄
	PClient client,
	// 选项
	ClientOptions * options);