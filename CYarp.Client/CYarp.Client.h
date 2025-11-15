#include <stdint.h>

// CYarp error codes
enum CYarpErrorCode
{
	// client handle invalid
	InvalidHandle = -1,

	// no error
	NoError = 0,

	// failed to connect to server
	ConnectFailure = 1,

	// connection to server timed out
	ConnectTimeout = 2,

	// connection to server unauthorized
	ConnectUnauthorized = 3,

	// connection forbidden (authorization failed)
	ConnectForbid = 4,
};

// CYarp client
typedef void* CYarpClient;

// tunnel error callback
typedef void (*CYarpTunnelErrorCallback)(
	// error type
	char16_t* type,
	// error message
	char16_t* message);

// completion callback
typedef void (*CYarpCompletedCallback)(
	// error code
	CYarpErrorCode errorCode);


// client options
struct CYarpClientOptions
{
	// CYarp server Uri
	// supports http, https, ws and wss
	char16_t* ServerUri;
	// target server Uri
	// supports http and https
	char16_t* TargetUri;
	// target server Unix Domain Socket path [optional]
	char16_t* TargetUnixDomainSocket;
	// connection timeout to server or target in seconds, default is 5s
	int32_t ConnectTimeout;
	// tunnel transport error callback [optional]
	CYarpTunnelErrorCallback TunnelErrorCallback;
};

// create client
// returns NULL if parameters are invalid
extern "C" CYarpClient CYarpClientCreate(
	// options
	CYarpClientOptions* options);

// set request header for connection
extern "C" enum CYarpErrorCode CYarpClientSetConnectHeader(
	// client
	CYarpClient client,
	// header name
	char16_t* headerName,
	// header value
	char16_t* headerValue);

// free client
extern "C" void CYarpClientFree(
	// client
	CYarpClient client);

// synchronous transport data
extern "C" enum CYarpErrorCode CYarpClientTransport(
	// client
	CYarpClient client);

// asynchronous transport data
extern "C" enum CYarpErrorCode CYarpClientTransportAsync(
	// client
	CYarpClient client,
	// completed callback; if null then becomes synchronous call
	CYarpCompletedCallback completedCallback);