#include <iostream>
#include "CYarp.Client.h"
#include <codecvt>

#pragma comment(lib, "CYarp.Client.lib") 


void tunnelError(char16_t* type, char16_t* message)
{
	std::wstring wideType(type, type + std::char_traits<char16_t>::length(type));
	std::wstring wideMessage(message, message + std::char_traits<char16_t>::length(message));
}

int main()
{
	CYarpClientOptions options{};
	options.ServerUri = (char16_t*)u"https://localhost\0";
	options.TargetUri = (char16_t*)u"https://www.cnblogs.com/\0";
	options.TunnelErrorCallback = tunnelError;

	CYarpClient client = CYarpClientCreate(&options);
	if (client != NULL)
	{
		// Host/CYarpServer/README.md下的Client002的token
		char16_t* authorization = (char16_t*)u"Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJDbGllbnQiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9zaWQiOiJDbGllbnQwMDIiLCJpYXQiOjE3MTEyNzczNzMsIm5iZiI6MTcxMTI3NzM3MywiZXhwIjoyNzExMzYzNzczfQ.D9fXLNgoCo51HGbUU0nktiYDs-qjYnx5M0dJ6sTv8CSeSdYh0iP8kfjawvZkMhlptVmxptZDwDyjIFMoFIf_lMoT0FtZJtGo2RUbw_skSBpmmaZIvfw8l7nJMaONt4gViatySEnzKtVFYHL7OjOVQpAM_gNgKIAdlRrdeGYS7CWl2GVJO39-V5KSUb2_kFHKvtmiJicJW2iLK_7jMz-038dULHMJ1Q2fCAKnFjdgezZ7QwflbYBE-YMC0XPyi9aXmYw9wauVdxhnlDCDjl0uYTKnQRxJgi1z7BWFjPU4IDoB6ZEF2s0mt7AwIE2VpJB0Is56zzZeGfr-d2L0_7ZSdQ\0";
		CYarpClientSetConnectHeader(client, (char16_t*)u"Authorization", authorization);

		std::cout << "transporting ...\n";
		CYarpErrorCode error = CYarpClientTransport(client);

		std::cout << error;
		CYarpClientFree(client);
	}
	else
	{
		std::cout << "create client failed";
	}
}
