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
	options.Authorization = (char16_t*)u"Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJDbGllbnQiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9zaWQiOiJDbGllbnQwMDEiLCJpYXQiOjE3MTA4MTY0NTIsIm5iZiI6MTcxMDgxNjQ1MiwiZXhwIjoyNzEwOTAyODUyfQ.ZHlGzulNdduJ1wUOykOTJFme0Gz2Sd-Zfu2qsDG-2z-JD1jHv45MrbPMYspEBUi9YDfN9ibeD8sZFDd_yEvZvSs74Jigx0OQhoY64ZZ-6howl0fhr8YPiWXNrzQOBRDxLUfAXW9IxDfcX4MaKI5mHhI-qzG8y1MmWvJkv_74FTgPGLERyBMFiRxAX8h4zEdO7YoLn5k4ptnBo77Gbr3qKAPKg2UI3rlUJZLFJC1fCkObpUD69AO58BfQNAOSo9fKTXf1JjvM5YNmuN39WbTzIwSIvixjWXvursBzne2Vl4xW-G-6jPiCyPyWfAoltXmW6fUycqtOraoFl0ka4RAnCQ\0";
	options.TunnelErrorCallback = tunnelError;

	CYarpClient client = CYarpClientCreate(&options);
	if (client != NULL)
	{
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
