// HTC Lighthouse Tracking
// 
// By Peter Thor 2016
// Reversed by lsylusiyao 2019
//
#define _USE_MATH_DEFINES

#include "LighthouseTracking.h"
#include <math.h>

LighthouseTracking::~LighthouseTracking() {
	if (m_pHMD != NULL)
	{
		vr::VR_Shutdown();
		m_pHMD = NULL;
	}
}

LighthouseTracking::LighthouseTracking() {
	vr::EVRInitError eError = vr::VRInitError_None;
	m_pHMD = vr::VR_Init(&eError, vr::VRApplication_Background);

	if (eError != vr::VRInitError_None)
	{
		m_pHMD = NULL;
		char buf[1024];
		sprintf_s(buf, sizeof(buf), "Unable to init VR runtime: %s", vr::VR_GetVRInitErrorAsEnglishDescription(eError));
		printf_s(buf);
		exit(EXIT_FAILURE);
	}
}

/*
* Loop-listen for events then parses them (e.g. prints the to user)
* Returns true if success or false if openvr has quit
*/
bool LighthouseTracking::RunProcedure(void) {

	// Process VREvent
	vr::VREvent_t event;
	//while (m_pHMD->PollNextEvent(&event, sizeof(event)))
	while (1)
	{
		// Process event
		if (!ProcessVREvent(event)) {
			char buf[1024];
			sprintf_s(buf, sizeof(buf), "(OpenVR) service quit\n");
			printf_s(buf);
			return false;
		}

		// Parse while something is happening
		ParseTrackingFrame();
		MySleep(200);
	}

	return true;
}

//-----------------------------------------------------------------------------
// Purpose: Processes a single VR event
//-----------------------------------------------------------------------------

bool LighthouseTracking::ProcessVREvent(const vr::VREvent_t & event)
{
	switch (event.eventType)
	{
		case vr::VREvent_TrackedDeviceActivated:
		{
			//SetupRenderModelForTrackedDevice(event.trackedDeviceIndex);
			char buf[1024];
			sprintf_s(buf, sizeof(buf), "(OpenVR) Device : %d attached\n", event.trackedDeviceIndex);
			printf_s(buf);
		}
		break;

		case vr::VREvent_TrackedDeviceDeactivated:
		{
			char buf[1024];
			sprintf_s(buf, sizeof(buf), "(OpenVR) Device : %d detached\n", event.trackedDeviceIndex);
			printf_s(buf);
		}
		break;

		case vr::VREvent_TrackedDeviceUpdated:
		{
			char buf[1024];
			sprintf_s(buf, sizeof(buf), "(OpenVR) Device : %d updated\n", event.trackedDeviceIndex);
			printf_s(buf);
		}
		break;

		case (vr::VREvent_DashboardActivated) :
		{
			char buf[1024];
			sprintf_s(buf, sizeof(buf), "(OpenVR) Dashboard activated\n");
			printf_s(buf);
		}
		break;

		case (vr::VREvent_DashboardDeactivated) :
		{
			char buf[1024];
			sprintf_s(buf, sizeof(buf), "(OpenVR) Dashboard deactivated\n");
			printf_s(buf);

		}
		break;

		case (vr::VREvent_ChaperoneDataHasChanged) :
		{
			char buf[1024];
			sprintf_s(buf, sizeof(buf), "(OpenVR) Chaperone data has changed\n");
			printf_s(buf);

		}
		break;

		case (vr::VREvent_ChaperoneSettingsHaveChanged) :
		{
			char buf[1024];
			sprintf_s(buf, sizeof(buf), "(OpenVR) Chaperone settings have changed\n");
			printf_s(buf);
		}
		break;

		case (vr::VREvent_ChaperoneUniverseHasChanged) :
		{
			char buf[1024];
			sprintf_s(buf, sizeof(buf), "(OpenVR) Chaperone universe has changed\n");
			printf_s(buf);

		}
		break;

		case (vr::VREvent_ApplicationTransitionStarted) :
		{
			char buf[1024];
			sprintf_s(buf, sizeof(buf), "(OpenVR) Application Transition: Transition has started\n");
			printf_s(buf);

		}
		break;

		case (vr::VREvent_ApplicationTransitionNewAppStarted) :
		{
			char buf[1024];
			sprintf_s(buf, sizeof(buf), "(OpenVR) Application transition: New app has started\n");
			printf_s(buf);

		}
		break;
		case (vr::VREvent_ButtonPress):
		{
			char buf[1024];
			sprintf_s(buf, sizeof(buf), "(OpenVR) button_press\n");
			printf_s(buf);
		}
	   break;

		case (vr::VREvent_Quit) :
		{
			char buf[1024];
			sprintf_s(buf, sizeof(buf), "(OpenVR) Received SteamVR Quit\n");
			printf_s(buf);

			return false;
		}
		break;

		
	}

	return true;
}



vr::HmdQuaternion_t LighthouseTracking::GetRotation(vr::HmdMatrix34_t matrix) {
	vr::HmdQuaternion_t q;

	q.w = sqrt(fmax(0, 1 + matrix.m[0][0] + matrix.m[1][1] + matrix.m[2][2])) / 2;
	q.x = sqrt(fmax(0, 1 + matrix.m[0][0] - matrix.m[1][1] - matrix.m[2][2])) / 2;
	q.y = sqrt(fmax(0, 1 - matrix.m[0][0] + matrix.m[1][1] - matrix.m[2][2])) / 2;
	q.z = sqrt(fmax(0, 1 - matrix.m[0][0] - matrix.m[1][1] + matrix.m[2][2])) / 2;
	q.x = copysign(q.x, matrix.m[2][1] - matrix.m[1][2]);
	q.y = copysign(q.y, matrix.m[0][2] - matrix.m[2][0]);
	q.z = copysign(q.z, matrix.m[1][0] - matrix.m[0][1]);
	return q;
}



vr::HmdVector3_t LighthouseTracking::GetPosition(vr::HmdMatrix34_t matrix) {
	vr::HmdVector3_t vector;

	vector.v[0] = matrix.m[0][3];
	vector.v[1] = matrix.m[1][3];
	vector.v[2] = matrix.m[2][3];

	return vector;
}

//****************//
vr::HmdVector3_t LighthouseTracking::QuaternionToEulerAngle(vr::HmdQuaternion_t matrix){
	vr::HmdVector3_t vector;
	vector.v[0] = atan2(2 * (matrix.w*matrix.x + matrix.y*matrix.z), 1 - 2 * (matrix.x*matrix.x+matrix.y*matrix.y));
	vector.v[1] = asin(2 * (matrix.w*matrix.y - matrix.z*matrix.x));
	vector.v[2] = atan2(2 * (matrix.w*matrix.z + matrix.x*matrix.y), 1 - 2 * (matrix.y*matrix.y + matrix.z*matrix.z));
	
	return vector;
}


vr::HmdVector3_t LighthouseTracking::QuaternionToEulerAngleRevised(vr::HmdQuaternion_t q1){
	//formula derivation reference  http://www.euclideanspace.com/maths/geometry/rotations/conversions/quaternionToEuler/index.htm
	vr::HmdVector3_t vector;
	vr::HmdVector3_t zero = { 0 };
	double sqw = q1.w*q1.w;
	double sqx = q1.x*q1.x;
	double sqy = q1.y*q1.y;
	double sqz = q1.z*q1.z;
	double unit = sqx + sqy + sqz + sqw; // if normalised is one, otherwise is correction factor
	double test = q1.x*q1.y + q1.z*q1.w;
	if (test > 0.499*unit) { // singularity at north pole
		vector.v[0] = 2 * atan2(q1.x, q1.w);
		vector.v[1] = M_PI / 2;
		vector.v[2] = 0;
		return zero;
	}
	if (test < -0.499*unit) { // singularity at south pole
		vector.v[0] = -2 * atan2(q1.x, q1.w);
		vector.v[1] = -M_PI / 2;
		vector.v[2] = 0;
		return zero;
	}
     vector.v[0]= atan2(2 * q1.y*q1.w - 2 * q1.x*q1.z, sqx - sqy - sqz + sqw);
	 vector.v[1]= asin(2 * test / unit);
	 vector.v[2]= atan2(2 * q1.x*q1.w - 2 * q1.y*q1.z, -sqx + sqy - sqz + sqw);



	return vector;
}

/*
* Parse a Frame with data from the tracking system
*
* Handy reference:
* https://github.com/TomorrowTodayLabs/NewtonVR/blob/master/Assets/SteamVR/Scripts/SteamVR_Utils.cs
*
* Also:
* Open VR Convention (same as OpenGL)
* right-handed system
* +y is up
* +x is to the right
* -z is going away from you
* http://www.3dgep.com/understanding-the-view-matrix/
*
*/
void LighthouseTracking::ParseTrackingFrame() {

	// Process SteamVR device states
	for (vr::TrackedDeviceIndex_t unDevice = 0; unDevice < vr::k_unMaxTrackedDeviceCount; unDevice++)
	{
		if (!m_pHMD->IsTrackedDeviceConnected(unDevice))
			continue;

		vr::VRControllerState_t state;
		if (m_pHMD->GetControllerState(unDevice, &state, sizeof(state)))
		{
			vr::TrackedDevicePose_t trackedDevicePose;
			vr::TrackedDevicePose_t *devicePose = &trackedDevicePose;

			vr::TrackedDevicePose_t trackedControllerPose;
			vr::TrackedDevicePose_t *controllerPose = &trackedControllerPose;

			vr::VRControllerState_t controllerState;
			vr::VRControllerState_t *ontrollerState_ptr = &controllerState;

			vr::HmdVector3_t vector;
			vr::HmdQuaternion_t quaternion;
			vr::HmdVector3_t eulerangle;
			vr::ETrackedDeviceClass trackedDeviceClass = vr::VRSystem()->GetTrackedDeviceClass(unDevice);
			switch (trackedDeviceClass) {
			case vr::ETrackedDeviceClass::TrackedDeviceClass_HMD:
				vr::VRSystem()->GetDeviceToAbsoluteTrackingPose(vr::TrackingUniverseSeated, 0, &trackedDevicePose, 1);

				vector = GetPosition(devicePose->mDeviceToAbsoluteTracking);
				quaternion = GetRotation(devicePose->mDeviceToAbsoluteTracking);

				// print stuff for the HMD here, see controller example below
				char buf[1024];
				sprintf_s(buf, sizeof(buf), "\n Helmet position \nx: %.3f y: %.3f z: %.3f\n", vector.v[0], vector.v[1], vector.v[2]);
				posVector[0] = vector;
				// printf_s(buf);
				break;

			case vr::ETrackedDeviceClass::TrackedDeviceClass_Controller:
				vr::VRSystem()->GetControllerStateWithPose(vr::TrackingUniverseStanding, unDevice, &controllerState, sizeof(controllerState),&trackedControllerPose);

				vector = GetPosition(controllerPose->mDeviceToAbsoluteTracking);
				quaternion = GetRotation(controllerPose->mDeviceToAbsoluteTracking);
				eulerangle = QuaternionToEulerAngle(quaternion);
				//读取按键是否按下，及触摸的相关测试
				//这里是？？？？？
				uint64_t My_Button_Pressed = controllerState.ulButtonPressed;
				float My_Trigger_position0 = controllerState.rAxis[0].y;
				float My_Trigger_position1 = controllerState.rAxis[1].x;
				float My_Trigger_position2 = controllerState.rAxis[2].x;
				float My_Trigger_position3 = controllerState.rAxis[3].x;
				float My_Trigger_position4 = controllerState.rAxis[4].x;
				
				/*char buf_a[1024];
				sprintf_s(buf_a, sizeof(buf_a), "\nLeft Controller position\nx: %.3f y: %.3f z: %.3f\n", vector.v[0], vector.v[1], vector.v[2]);
				printf_s(buf_a);*/


				/*if (My_Button_Pressed != 0)
				{
					char buf[1024];
					//char buf_button;
					sprintf_s(buf, sizeof(buf), "\nMy_Button_Pressed value: %f\n", My_Button_Pressed);//, vector.v[0], vector.v[1], vector.v[2]
					printf_s(buf);
				}*/
				    
				/*if (My_Trigger_position1!=0)//
				{
					char buf[1024];					
					sprintf_s(buf, sizeof(buf), "\nMy_Trigger_position value: %0.3f\n", My_Trigger_position1);
					printf_s(buf);
				}*/
				

				/** Trigger a single haptic pulse on a controller. After this call the application may not trigger another haptic pulse on this controller
				* and axis combination for 5ms. */
				//virtual void TriggerHapticPulse(vr::TrackedDeviceIndex_t unControllerDeviceIndex, uint32_t unAxisId, unsigned short usDurationMicroSec) = 0;
				
				//vr::EVRButtonId MyButtonID=vr::VRSystem()->
				
				/*if (My_Trigger_position1 > -0.1)
				{					
					//pVRSystem->Get(unDevice, Prop_Axis0Type_Int32 + n);
					//vr::EVRButtonId::k_EButton_Axis1
						//vr::VRSystem()->TriggerHapticPulse(unDevice, vr::EVRButtonId::k_EButton_SteamVR_Trigger, 1000);
					vr::VRSystem()->TriggerHapticPulse(unDevice, vr::ButtonMaskFromId(vr::k_EButton_Axis0), 3000);

				}*/

				/*
				vr::EVRButtonId My_Button_Id;
				vr::VRSystem()->GetButtonIdNameFromEnum(My_Button_Id);
				if (My_Button_Id!= 0)
				{
				char buf[1024];
				sprintf_s(buf, sizeof(buf), "\nMy_Button_Id value: %0.3f\n", My_Button_Id);
				printf_s(buf);
				}*/
				//if (My_Button_Id = vr::EVRButtonId(k_EButton_DPad_Up))

				//char button_name[1024];
				//button_name = vr::VRSystem()->GetButtonIdNameFromEnum(vr::k_EButton_Grip);//
				vr::ButtonMaskFromId(vr::k_EButton_Grip);
				switch (vr::VRSystem()->GetControllerRoleForTrackedDeviceIndex(unDevice)) {
				case vr::TrackedControllerRole_Invalid:
					// invalide hand... 
					break;

				case vr::TrackedControllerRole_LeftHand:
					char buf[1024];
					sprintf_s(buf, sizeof(buf), "Left Controller Euler Angle   rx: %.2f ry: %.2f rz: %.2f \n", eulerangle.v[0] * 180 / M_PI, eulerangle.v[1] * 180 / M_PI, eulerangle.v[2] * 180 / M_PI);
					//sprintf_s(buf, sizeof(buf), "qw: %.2f qx: %.2f qy: %.2f qz: %.2f\n", quaternion.w, quaternion.x, quaternion.y, quaternion.z);
					//printf_s(buf);
					
					if (My_Trigger_position1 != 0)//
					{
						char buf[1024];
						sprintf_s(buf, sizeof(buf), "\nMy_Trigger_position value: %0.3f\n", My_Trigger_position1);
						printf_s(buf);
					}
					if (My_Trigger_position1 >0.8)
					{	//pVRSystem->Get(unDevice, Prop_Axis0Type_Int32 + n);
						//vr::EVRButtonId::k_EButton_Axis1
						//vr::VRSystem()->TriggerHapticPulse(unDevice, vr::EVRButtonId::k_EButton_SteamVR_Trigger, 1000);
						vr::VRSystem()->TriggerHapticPulse(unDevice, vr::ButtonMaskFromId(vr::k_EButton_Axis0), 1000);

					}
					break;
				case vr::TrackedControllerRole_RightHand:
					char buf_R[1024];
					sprintf_s(buf_R, sizeof(buf_R), "\nRight Controller\nx: %.3f y: %.3f z: %.3f\n", vector.v[0], vector.v[1], vector.v[2]);
					//printf_s(buf_R);
					break;

				}

				break;
			}

		}
	}
}