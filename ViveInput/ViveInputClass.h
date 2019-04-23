#pragma once

#include <iostream>
#include <string>

// OpenVR
#include <openvr.h>
#include "../samples/shared/Matrices.h"


class __declspec(dllexport) ViveInputClass {
private:

	// Basic stuff
	vr::IVRSystem *m_pHMD = NULL;
	vr::TrackedDevicePose_t m_rTrackedDevicePose[vr::k_unMaxTrackedDeviceCount];
	Matrix4 m_rmat4DevicePose[vr::k_unMaxTrackedDeviceCount];

	// Position and rotation of pose
	vr::HmdVector3_t GetPosition(vr::HmdMatrix34_t matrix);
	vr::HmdQuaternion_t GetRotation(vr::HmdMatrix34_t matrix);
	vr::HmdVector3_t QuaternionToEulerAngle(vr::HmdQuaternion_t matrix);

	// If false the program will parse tracking data continously and not wait for openvr events
	bool bWaitForEventsBeforeParsing = false;
	
public:
	~ViveInputClass();
	ViveInputClass();

	// Main loop that listens for openvr events and calls process and parse routines, if false the service has quit
	bool RunProcedure(bool bWaitForEvents, int filterIndex);

	// Process a VR event and print some general info of what happens
	bool ProcessVREvent(const vr::VREvent_t & event, int filterIndex);

	// Parse a tracking frame and print its position / rotation / events.
	// Supply a filterIndex different than -1 to only show data for one specific device.
	void ParseTrackingFrame(int filterIndex);

	// prints information of devices
	std::string PrintDevices();
	//访问private
	bool GetIsStrGiven() { return isStrGiven; }
	void SetIsStrGiven(bool b) { isStrGiven = b; }
	std::string GetInfoStr() { return infoStr; }
	void SetInfoStr(std::string s) { infoStr = s; }
	double* GetHMD() { return HMD; }
	double* GetLeftHand() { return leftHand; }
	double* GetRightHand() { return rightHand; }
	double* GetTrigger() { return trigger; }

private:
	//向CLR传的数据
	bool isStrGiven = false;
	std::string infoStr; //这里如果given是true，那就去读取，然后马上把given变成false，然后把info清空

	/*
	* Also:
	* Open VR Convention (same as OpenGL)
	* right-handed system
	* +y is up
	* +x is to the right
	* -z is going away from you
	*/
	//pos*3, eularAngle*3, state*2
	double HMD[6];
	double leftHand[8];
	double rightHand[8];
	double trigger[2];

};

