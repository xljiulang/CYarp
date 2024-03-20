#include <stdint.h>

typedef void* PClient;

// 传输错误枚举
enum TransportError
{
	NoError = 0,
	InvalidHandle = 1,
	ParameterError = 2,
	ConnectError = 3
};

// 客户端选项
struct ClientOptions
{
	// CYarp服务器Uri
	char* ServerUri;
	// 目标服务器Uri
	char* TargetUri;
	// 连接到CYarp服务器的Authorization请求头的值
	char* Authorization;
	// 与server或target的连接超时时长，默认为30s
	int32_t ConnectTimeout;
};

// 创建客户端
extern "C" PClient CreateClient();

// 释放客户端
extern "C" void FreeClient(PClient client);

// 传输数据
extern "C" enum TransportError Transport(PClient client, ClientOptions options);