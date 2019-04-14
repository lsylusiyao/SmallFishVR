#pragma once
#include "../ViveInput/ViveInputClass.h"
using namespace System;
//using namespace msclr::interop;
//C++提供最最基础的VR类封装，C++-C#提供main函数中的类似功能，使用callback手法；C#专注于图形界面以及顶层的指令控制，以及和鱼的字符串发送接受等

namespace BridgeDLL {
	public ref class BridgeClass
	{
	public:
		BridgeClass() :p(new ViveInputClass()) {}
		~BridgeClass() { delete p; }
		String^ GetDevices() { return gcnew String(p->PrintDevices().data()); } //注意一下这里会不会出现指针数据被释放问题
		bool keepVRworking = false; //想停止直接置为false就行
		void Run()
		{
			keepVRworking = true;
			while (keepVRworking && p->RunProcedure(true, -1)) { System::Threading::Thread::Sleep(10); }
		}
		void SetIsStrGiven(bool b) { p->isStrGiven = b; }
		bool GetIsStrGiven() { return p->isStrGiven; }
		String^ GetInfoStr() { return gcnew String(p->infoStr.data()); }

		array<double>^ GetHMDPos() {for (int i = 0; i < 3; i++) { HMDPos[i] = p->HMDPos[i]; return HMDPos; }}
		array<double>^ GetHMDEularAngle() {for (int i = 0; i < 3; i++) { HMDEularAngle[i] = p->HMDEularAngle[i]; return HMDEularAngle; }}
		array<double>^ GetleftHandPos() {for (int i = 0; i < 3; i++) { leftHandPos[i] = p->leftHandPos[i]; return leftHandPos; }}
		array<double>^ GetleftHandEularAngle() {for (int i = 0; i < 3; i++) { leftHandEularAngle[i] = p->leftHandEularAngle[i]; return leftHandEularAngle; }}
		array<double>^ GetleftHandState() {for (int i = 0; i < 2; i++) { leftHandState[i] = p->leftHandState[i]; return leftHandState; }}
		array<double>^ GetrightHandPos() {for (int i = 0; i < 3; i++) { rightHandPos[i] = p->rightHandPos[i]; return rightHandPos; }}
		array<double>^ GetrightHandEularAngle() {for (int i = 0; i < 3; i++) { rightHandEularAngle[i] = p->rightHandEularAngle[i]; return rightHandEularAngle; }}
		array<double>^ GetrightHandState() {for (int i = 0; i < 2; i++) { rightHandState[i] = p->rightHandState[i]; return rightHandState; }}
		
	private:
		ViveInputClass * p;
		array<double>^ HMDPos = gcnew array<double>(3);
		array<double>^ HMDEularAngle = gcnew array<double>(3);
		array<double>^ leftHandPos = gcnew array<double>(3);
		array<double>^ leftHandEularAngle = gcnew array<double>(3);
		array<double>^ leftHandState = gcnew array<double>(2);
		array<double>^ rightHandPos = gcnew array<double>(3);
		array<double>^ rightHandEularAngle = gcnew array<double>(3);
		array<double>^ rightHandState = gcnew array<double>(2);
		
		
	};
	
	
}
