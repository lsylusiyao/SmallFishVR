#pragma once

#include "../ViveInput/ViveInputClass.h"
#pragma comment(lib, "../x64/Debug/ViveInput.lib")
using namespace System;
//C++提供最最基础的VR类封装，C++-C#提供main函数中的类似功能，使用callback手法；C#专注于图形界面以及顶层的指令控制，以及和鱼的字符串发送接受等

namespace BridgeDll {
	public ref class BridgeClass
	{
	public:
		BridgeClass() :p(new ViveInputClass()) {}
		~BridgeClass() { delete p; }
		String^ GetDevices() { return gcnew String(p->PrintDevices().data()); } //注意一下这里会不会出现指针数据被释放问题
		bool GetKeepVRWorking() { return keepVRworking; }
		void SetKeepVRWorking(bool b) { keepVRworking = b; }
		void Run()
		{
			keepVRworking = true;
			while (keepVRworking && p->RunProcedure(true, -1)) { System::Threading::Thread::Sleep(10); }
		}
		void SetIsStrGiven(bool b) { p->SetIsStrGiven(b); }
		bool GetIsStrGiven() { return p->GetIsStrGiven(); }
		String^ GetInfoStr() { return gcnew String(p->GetInfoStr().data()); }
		void ClearInfoStr() { p->SetInfoStr(""); }

		array<double>^ GetHMD() { for (int i = 0; i < 6; i++) { HMD[i] = p->GetHMD()[i]; return HMD; } }
		array<double>^ GetLeftHand() { for (int i = 0; i < 8; i++) { leftHand[i] = p->GetLeftHand()[i] / Math::PI * 180; return leftHand; } }
		array<double>^ GetRightHand() { for (int i = 0; i < 8; i++) { rightHand[i] = p->GetRightHand()[i] / Math::PI * 180; return rightHand; } }

	private:
		bool keepVRworking = false; //想停止直接置为false就行
		ViveInputClass * p;
		array<double>^ HMD = gcnew array<double>(6);
		array<double>^ leftHand = gcnew array<double>(8);
		array<double>^ rightHand = gcnew array<double>(8);


	};


}
