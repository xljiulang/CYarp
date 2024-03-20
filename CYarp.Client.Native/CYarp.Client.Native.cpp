// CYarp.Client.Native.cpp : 此文件包含 "main" 函数。程序执行将在此处开始并结束。
//

#include <iostream>
#include "CYarp.Client.h"

#pragma comment(lib, "CYarp.Client.lib")

int main()
{
	PClient client = CreateClient();
	ClientOptions options{};
	options.ServerUri = (char*)"https://localhost\0";
	options.TargetUri = (char*)"https://www.cnblogs.com/\0";
	options.Authorization = (char*)"Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJDbGllbnQiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9zaWQiOiJDbGllbnQwMDEiLCJpYXQiOjE3MTA4MTY0NTIsIm5iZiI6MTcxMDgxNjQ1MiwiZXhwIjoyNzEwOTAyODUyfQ.ZHlGzulNdduJ1wUOykOTJFme0Gz2Sd-Zfu2qsDG-2z-JD1jHv45MrbPMYspEBUi9YDfN9ibeD8sZFDd_yEvZvSs74Jigx0OQhoY64ZZ-6howl0fhr8YPiWXNrzQOBRDxLUfAXW9IxDfcX4MaKI5mHhI-qzG8y1MmWvJkv_74FTgPGLERyBMFiRxAX8h4zEdO7YoLn5k4ptnBo77Gbr3qKAPKg2UI3rlUJZLFJC1fCkObpUD69AO58BfQNAOSo9fKTXf1JjvM5YNmuN39WbTzIwSIvixjWXvursBzne2Vl4xW-G-6jPiCyPyWfAoltXmW6fUycqtOraoFl0ka4RAnCQ\0";

	TransportError error = TransportError::Completed;
	while (error == TransportError::Completed)
	{
		std::cout << "transporting ...\n";
		error = Transport(client, &options);
	}

	FreeClient(client);

	std::cout << error;
}

// 运行程序: Ctrl + F5 或调试 >“开始执行(不调试)”菜单
// 调试程序: F5 或调试 >“开始调试”菜单

// 入门使用技巧: 
//   1. 使用解决方案资源管理器窗口添加/管理文件
//   2. 使用团队资源管理器窗口连接到源代码管理
//   3. 使用输出窗口查看生成输出和其他消息
//   4. 使用错误列表窗口查看错误
//   5. 转到“项目”>“添加新项”以创建新的代码文件，或转到“项目”>“添加现有项”以将现有代码文件添加到项目
//   6. 将来，若要再次打开此项目，请转到“文件”>“打开”>“项目”并选择 .sln 文件
