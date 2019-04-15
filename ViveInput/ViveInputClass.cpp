//
// HTC Vive Lighthouse Tracking Example
// By Peter Thor 2016, 2017, 2018, 2019
//
// Shows how to extract basic tracking data
//

#include "ViveInputClass.h"
#include <Windows.h>

void MySleep(int miliseconds)
{
	Sleep(miliseconds);
}

using std::string;
// Destructor
ViveInputClass::~ViveInputClass() {
	if (m_pHMD != NULL)
	{
		vr::VR_Shutdown();
		m_pHMD = NULL;
	}
}

// Constructor
ViveInputClass::ViveInputClass() {
	vr::EVRInitError eError = vr::VRInitError_None;
	m_pHMD = vr::VR_Init(&eError, vr::VRApplication_Background);

	if (eError != vr::VRInitError_None)
	{
		m_pHMD = NULL;
		infoStr = "Unable to init VR runtime: %s", vr::VR_GetVRInitErrorAsEnglishDescription(eError);
		isStrGiven = true;
		exit(EXIT_FAILURE);
	}
}

/*
* Loop-listen for events then parses them (e.g. prints the to user)
* Supply a filterIndex other than -1 to show only info for that device in question. e.g. 0 is always the hmd.
* Returns true if success or false if openvr has quit
*/
bool ViveInputClass::RunProcedure(bool bWaitForEvents, int filterIndex = -1) {

	// Either A) wait for events, such as hand controller button press, before parsing...
	if (bWaitForEvents) {
		// Process VREvent
		vr::VREvent_t event;
		while (m_pHMD->PollNextEvent(&event, sizeof(event)))
		{
			// Process event
			if (!ProcessVREvent(event, filterIndex)) {
				
				infoStr = "(OpenVR) service quit\n";
				
				isStrGiven = true;
				//return false;
			}

			// parse a frame
			ParseTrackingFrame(filterIndex);
		}
	}
	else {
		// ... or B) continous parsint of tracking data irrespective of events
		std::cout << std::endl << "Parsing next frame...";

		ParseTrackingFrame(filterIndex);
	}

	return true;
}

//-----------------------------------------------------------------------------
// Purpose: Processes a single VR event
//-----------------------------------------------------------------------------

bool ViveInputClass::ProcessVREvent(const vr::VREvent_t & event, int filterOutIndex = -1)
{
	// if user supplied a device filter index only show events for that device
	if (filterOutIndex != -1)
		if (event.trackedDeviceIndex == filterOutIndex)
			return true;

	// print stuff for various events (this is not a complete list). Add/remove upon your own desire...
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

		case (vr::VREvent_Quit) :
		{
			char buf[1024];
			sprintf_s(buf, sizeof(buf), "(OpenVR) Received SteamVR Quit (%d)\n", vr::VREvent_Quit);
			printf_s(buf);

			return false;
		}
		break;

		case (vr::VREvent_ProcessQuit) :
		{
			char buf[1024];
			sprintf_s(buf, sizeof(buf), "(OpenVR) SteamVR Quit Process (%d)\n", vr::VREvent_ProcessQuit);
			printf_s(buf);

			return false;
		}
		break;

		case (vr::VREvent_QuitAborted_UserPrompt) :
		{
			char buf[1024];
			sprintf_s(buf, sizeof(buf), "(OpenVR) SteamVR Quit Aborted UserPrompt (%d)\n", vr::VREvent_QuitAborted_UserPrompt);
			printf_s(buf);

			return false;
		}
		break;

		case (vr::VREvent_QuitAcknowledged) :
		{
			char buf[1024];
			sprintf_s(buf, sizeof(buf), "(OpenVR) SteamVR Quit Acknowledged (%d)\n", vr::VREvent_QuitAcknowledged);
			printf_s(buf);

			return false;
		}
		break;

		case (vr::VREvent_TrackedDeviceRoleChanged) :
		{

			char buf[1024];
			sprintf_s(buf, sizeof(buf), "(OpenVR) TrackedDeviceRoleChanged: %d\n", event.trackedDeviceIndex);
			printf_s(buf);
		}
		break;

		case (vr::VREvent_TrackedDeviceUserInteractionStarted) :
		{
			char buf[1024];
			sprintf_s(buf, sizeof(buf), "(OpenVR) TrackedDeviceUserInteractionStarted: %d\n", event.trackedDeviceIndex);
			printf_s(buf);
		}
		break;

		// various events not handled/moved yet into the previous switch chunk.
		default: {
			char buf[1024];
			switch (event.eventType) {
			case vr::EVREventType::VREvent_ButtonTouch:
				sprintf_s(buf, sizeof(buf), "(OpenVR) Event: Touch Device: %d\n", event.trackedDeviceIndex);
				printf_s(buf);
				break;
			case vr::EVREventType::VREvent_ButtonUntouch:
				sprintf_s(buf, sizeof(buf), "(OpenVR) Event: Untouch Device: %d\n", event.trackedDeviceIndex);
				printf_s(buf);
				break;
			case vr::EVREventType::VREvent_ButtonPress:
				sprintf_s(buf, sizeof(buf), "(OpenVR) Event: Press Device: %d\n", event.trackedDeviceIndex);
				printf_s(buf);
				break;
			case vr::EVREventType::VREvent_ButtonUnpress:
				sprintf_s(buf, sizeof(buf), "(OpenVR) Event: Release Device: %d\n", event.trackedDeviceIndex);
				printf_s(buf);
				break;
			case vr::EVREventType::VREvent_EnterStandbyMode:
				sprintf_s(buf, sizeof(buf), "(OpenVR) Event: Enter StandbyMode: %d\n", event.trackedDeviceIndex);
				printf_s(buf);
				break;
			case vr::EVREventType::VREvent_LeaveStandbyMode:
				sprintf_s(buf, sizeof(buf), "(OpenVR) Event: Leave StandbyMode: %d\n", event.trackedDeviceIndex);
				printf_s(buf);
				break;
			case vr::EVREventType::VREvent_PropertyChanged:
				sprintf_s(buf, sizeof(buf), "(OpenVR) Event: Property Changed Device: %d\n", event.trackedDeviceIndex);
				printf_s(buf);
				break;
			case vr::EVREventType::VREvent_SceneApplicationChanged:
				sprintf_s(buf, sizeof(buf), "(OpenVR) Event: Scene Application Changed\n");
				printf_s(buf);
				break;
			case vr::EVREventType::VREvent_SceneFocusChanged:
				sprintf_s(buf, sizeof(buf), "(OpenVR) Event: Scene Focus Changed\n");
				printf_s(buf);
				break;
			case vr::EVREventType::VREvent_TrackedDeviceUserInteractionStarted:
				sprintf_s(buf, sizeof(buf), "(OpenVR) Event: Tracked Device User Interaction Started Device: %d\n", event.trackedDeviceIndex);
				printf_s(buf);
				break;
			case vr::EVREventType::VREvent_TrackedDeviceUserInteractionEnded:
				sprintf_s(buf, sizeof(buf), "(OpenVR) Event: Tracked Device User Interaction Ended Device: %d\n", event.trackedDeviceIndex);
				printf_s(buf);
				break;
			case vr::EVREventType::VREvent_ProcessDisconnected:
				sprintf_s(buf, sizeof(buf), "(OpenVR) Event: A process was disconnected\n");
				printf_s(buf);
				break;
			case vr::EVREventType::VREvent_ProcessConnected:
				sprintf_s(buf, sizeof(buf), "(OpenVR) Event: A process was connected\n");
				printf_s(buf);
				break;

			default:
				sprintf_s(buf, sizeof(buf), "(OpenVR) Unmanaged Event: %d Device: %d\n", event.eventType, event.trackedDeviceIndex);
				printf_s(buf);
				break;
			}
		}
		break;
	}

	return true;
}


// Get the quaternion representing the rotation
vr::HmdQuaternion_t ViveInputClass::GetRotation(vr::HmdMatrix34_t matrix) {
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

// Get the vector representing the position
vr::HmdVector3_t ViveInputClass::GetPosition(vr::HmdMatrix34_t matrix) {
	vr::HmdVector3_t vector;

	vector.v[0] = matrix.m[0][3];
	vector.v[1] = matrix.m[1][3];
	vector.v[2] = matrix.m[2][3];

	return vector;
}

vr::HmdVector3_t ViveInputClass::QuaternionToEulerAngle(vr::HmdQuaternion_t matrix) {
	vr::HmdVector3_t vector;
	vector.v[0] = atan2(2 * (matrix.w*matrix.x + matrix.y*matrix.z), 1 - 2 * (matrix.x*matrix.x + matrix.y*matrix.y));
	vector.v[1] = asin(2 * (matrix.w*matrix.y - matrix.z*matrix.x));
	vector.v[2] = atan2(2 * (matrix.w*matrix.z + matrix.x*matrix.y), 1 - 2 * (matrix.y*matrix.y + matrix.z*matrix.z));

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
void ViveInputClass::ParseTrackingFrame(int filterIndex) {

	vr::HmdVector3_t temp;
	char buf[1024];

	// Process SteamVR device states
	for (vr::TrackedDeviceIndex_t unDevice = 0; unDevice < vr::k_unMaxTrackedDeviceCount; unDevice++)
	{
		// if not connected just skip the rest of the routine
		if (!m_pHMD->IsTrackedDeviceConnected(unDevice)) {
			continue;
		}

		if (filterIndex != unDevice)
			continue;

		vr::TrackedDevicePose_t trackedDevicePose;
		vr::TrackedDevicePose_t *devicePose = &trackedDevicePose;

		vr::VRControllerState_t controllerState;
		vr::VRControllerState_t *ontrollerState_ptr = &controllerState;

		vr::HmdVector3_t position;
		vr::HmdQuaternion_t quaternion;


		/*
		if (vr::VRSystem()->IsInputFocusCapturedByAnotherProcess()) {
			char buf[1024];

			sprintf_s(buf, sizeof(buf), "\nInput Focus by Another Process\n");
			printf_s(buf);
		}
		*/

		bool bPoseValid = trackedDevicePose.bPoseIsValid;
		vr::HmdVector3_t vVel;
		vr::HmdVector3_t vAngVel;
		vr::ETrackingResult eTrackingResult;

		// Get what type of device it is and work with its data
		vr::ETrackedDeviceClass trackedDeviceClass = vr::VRSystem()->GetTrackedDeviceClass(unDevice);

		sprintf_s(buf, sizeof(buf), "\nClass%d\n", trackedDeviceClass);
		printf_s(buf);

		switch (trackedDeviceClass) {
		case vr::ETrackedDeviceClass::TrackedDeviceClass_HMD:
			// print stuff for the HMD here, see controller stuff in next case block

			// get pose relative to the safe bounds defined by the user
			vr::VRSystem()->GetDeviceToAbsoluteTrackingPose(vr::TrackingUniverseStanding, 0, &trackedDevicePose, 1);

			// get the position and rotation
			position = GetPosition(devicePose->mDeviceToAbsoluteTracking);
			quaternion = GetRotation(devicePose->mDeviceToAbsoluteTracking);

			// get some data
			vVel = trackedDevicePose.vVelocity;
			vAngVel = trackedDevicePose.vAngularVelocity;
			eTrackingResult = trackedDevicePose.eTrackingResult;
			bPoseValid = trackedDevicePose.bPoseIsValid;

			// print the tracking data
			/*char buf[1024];
			sprintf_s(buf, sizeof(buf), "\nHMD\nx: %.2f y: %.2f z: %.2f\n", position.v[0], position.v[1], position.v[2]);
			printf_s(buf);
			sprintf_s(buf, sizeof(buf), "qw: %.2f qx: %.2f qy: %.2f qz: %.2f\n", quaternion.w, quaternion.x, quaternion.y, quaternion.z);
			printf_s(buf);*/
			temp = QuaternionToEulerAngle(quaternion);
			for (int i = 0; i < 3; i++) { HMD[i] = position.v[i]; HMD[i + 3] = temp.v[i]; }

			// and print some more info to the user about the state of the device/pose
			switch (eTrackingResult) {
			case vr::ETrackingResult::TrackingResult_Uninitialized:
				
				infoStr = "Invalid tracking result";
				isStrGiven = true;
				break;
			case vr::ETrackingResult::TrackingResult_Calibrating_InProgress:
				sprintf_s(buf, sizeof(buf), "Calibrating in progress\n");
				printf_s(buf);
				break;
			case vr::ETrackingResult::TrackingResult_Calibrating_OutOfRange:
				
				infoStr = "Calibrating Out of range";
				isStrGiven = true;
				break;
			case vr::ETrackingResult::TrackingResult_Running_OK:
				sprintf_s(buf, sizeof(buf), "Running OK\n");
				printf_s(buf);
				break;
			case vr::ETrackingResult::TrackingResult_Running_OutOfRange:
				
				infoStr = "WARNING: Running Out of Range";
				isStrGiven = true;
				break;
			default:
				sprintf_s(buf, sizeof(buf), "Default\n");
				printf_s(buf);
				break;
			}

			if (bPoseValid)
			{
				sprintf_s(buf, sizeof(buf), "Valid pose\n");
				printf_s(buf);
			}

			else
			{
				infoStr = "Invalid pose";
				isStrGiven = true;
			}
			break;


		case vr::ETrackedDeviceClass::TrackedDeviceClass_Controller:
			// Simliar to the HMD case block above, please adapt as you like
			// to get away with code duplication and general confusion

			vr::VRSystem()->GetControllerStateWithPose(vr::TrackingUniverseStanding, unDevice, &controllerState, sizeof(controllerState), &trackedDevicePose);

			position = GetPosition(devicePose->mDeviceToAbsoluteTracking);
			quaternion = GetRotation(devicePose->mDeviceToAbsoluteTracking);

			vVel = trackedDevicePose.vVelocity;
			vAngVel = trackedDevicePose.vAngularVelocity;
			eTrackingResult = trackedDevicePose.eTrackingResult;
			bPoseValid = trackedDevicePose.bPoseIsValid;

			//char buf[1024];
			switch (vr::VRSystem()->GetControllerRoleForTrackedDeviceIndex(unDevice)) {
			case vr::TrackedControllerRole_Invalid:
				// invalid hand...
				
				infoStr = "Invalid hand";
				
				isStrGiven = true;


				/*
				sprintf_s(buf, sizeof(buf), "\nInvalid role id: %d\nx: %.2f y: %.2f z: %.2f\n", unDevice, position.v[0], position.v[1], position.v[2]);
				printf_s(buf);

				sprintf_s(buf, sizeof(buf), "\nState: x: %.2f y: %.2f\n", controllerState.rAxis[0].x, controllerState.rAxis[0].y);
				printf_s(buf);
				*/

				break;

			//
			case vr::TrackedControllerRole_LeftHand:

				/*sprintf_s(buf, sizeof(buf), "\nLeft Controller i: %d index: %d\nx: %.2f y: %.2f z: %.2f\n", unDevice, vr::VRSystem()->GetTrackedDeviceIndexForControllerRole(vr::TrackedControllerRole_LeftHand), position.v[0], position.v[1], position.v[2]);
				printf_s(buf);

				sprintf_s(buf, sizeof(buf), "qw: %.2f qx: %.2f qy: %.2f qz: %.2f\n", quaternion.w, quaternion.x, quaternion.y, quaternion.z);
				printf_s(buf);

				sprintf_s(buf, sizeof(buf), "State: x: %.2f y: %.2f\n", controllerState.rAxis[0].x, controllerState.rAxis[0].y);
				printf_s(buf);*/
				vr::VRSystem()->GetTrackedDeviceIndexForControllerRole(vr::TrackedControllerRole_LeftHand);
				temp = QuaternionToEulerAngle(quaternion);
				for (int i = 0; i < 3; i++) { leftHand[i] = position.v[i]; leftHand[i + 3] = temp.v[i]; }
				leftHand[6] = controllerState.rAxis[0].x;
				leftHand[7] = controllerState.rAxis[0].y;

				switch (eTrackingResult) {
				case vr::ETrackingResult::TrackingResult_Uninitialized:
					
					infoStr = "Invalid tracking result";
					isStrGiven = true;
					break;
				case vr::ETrackingResult::TrackingResult_Calibrating_InProgress:
					sprintf_s(buf, sizeof(buf), "Calibrating in progress\n");
					printf_s(buf);
					break;
				case vr::ETrackingResult::TrackingResult_Calibrating_OutOfRange:
					
					infoStr = "Calibrating Out of range";
					isStrGiven = true;
					break;
				case vr::ETrackingResult::TrackingResult_Running_OK:
					sprintf_s(buf, sizeof(buf), "Running OK\n");
					printf_s(buf);
					break;
				case vr::ETrackingResult::TrackingResult_Running_OutOfRange:
					
					infoStr = "WARNING: Running Out of Range";
					isStrGiven = true;

					break;
				default:
					sprintf_s(buf, sizeof(buf), "Default\n");
					printf_s(buf);
					break;
				}

				if (bPoseValid)
				{
					sprintf_s(buf, sizeof(buf), "Valid pose\n");
					printf_s(buf);
				}
					
				else
				{
					
					infoStr = "Invalid pose";
					isStrGiven = true;
				}
				break;

			case vr::TrackedControllerRole_RightHand:
				// incomplete code, look at left hand for reference
				vr::VRSystem()->GetTrackedDeviceIndexForControllerRole(vr::TrackedControllerRole_RightHand);
				temp = QuaternionToEulerAngle(quaternion);
				for (int i = 0; i < 3; i++) { rightHand[i] = position.v[i]; rightHand[i + 3] = temp.v[i]; }
				rightHand[6] = controllerState.rAxis[0].x;
				rightHand[7] = controllerState.rAxis[0].y;
				/*sprintf_s(buf, sizeof(buf), "\nRightController i: %d index: %d\nx: %.2f y: %.2f z: %.2f\n", unDevice, vr::VRSystem()->GetTrackedDeviceIndexForControllerRole(vr::TrackedControllerRole_LeftHand), position.v[0], position.v[1], position.v[2]);
				printf_s(buf);

				sprintf_s(buf, sizeof(buf), "qw: %.2f qx: %.2f qy: %.2f qz: %.2f\n", quaternion.w, quaternion.x, quaternion.y, quaternion.z);
				printf_s(buf);

				sprintf_s(buf, sizeof(buf), "State: x: %.2f y: %.2f\n", controllerState.rAxis[0].x, controllerState.rAxis[0].y);
				printf_s(buf);*/

				switch (eTrackingResult) {
				case vr::ETrackingResult::TrackingResult_Uninitialized:
					
					infoStr = "Invalid tracking result";
					isStrGiven = true;
					break;
				case vr::ETrackingResult::TrackingResult_Calibrating_InProgress:
					sprintf_s(buf, sizeof(buf), "Calibrating in progress\n");
					printf_s(buf);
					break;
				case vr::ETrackingResult::TrackingResult_Calibrating_OutOfRange:
					
					infoStr = "Calibrating Out of range";
					isStrGiven = true;
					break;
				case vr::ETrackingResult::TrackingResult_Running_OK:
					sprintf_s(buf, sizeof(buf), "Running OK\n");
					printf_s(buf);
					break;
				case vr::ETrackingResult::TrackingResult_Running_OutOfRange:
					
					infoStr = "WARNING: Running Out of Range";
					isStrGiven = true;

					break;
				default:
					sprintf_s(buf, sizeof(buf), "Default\n");
					printf_s(buf);
					break;
				}

				if (bPoseValid)
				{
					sprintf_s(buf, sizeof(buf), "Valid pose\n");
					printf_s(buf);
				}

				else
				{
					
					infoStr = "Invalid pose";
					isStrGiven = true;
				}

				break;

			default:
				// incomplete code, only here for switch reference
				
				infoStr = "Not supported";
				isStrGiven = true;
				break;
			}
			break;

		case vr::TrackedDeviceClass_GenericTracker:
			//char buf[1024];

			vr::VRSystem()->GetControllerStateWithPose(vr::TrackingUniverseStanding, unDevice, &controllerState, sizeof(controllerState), &trackedDevicePose);

			// get pose relative to the safe bounds defined by the user
			//vr::VRSystem()->GetDeviceToAbsoluteTrackingPose(vr::TrackingUniverseStanding, 0, &trackedDevicePose, 1);

			// get the position and rotation
			position = GetPosition(devicePose->mDeviceToAbsoluteTracking);
			quaternion = GetRotation(devicePose->mDeviceToAbsoluteTracking);

			position = GetPosition(devicePose->mDeviceToAbsoluteTracking);
			quaternion = GetRotation(devicePose->mDeviceToAbsoluteTracking);

			vVel = trackedDevicePose.vVelocity;
			vAngVel = trackedDevicePose.vAngularVelocity;
			eTrackingResult = trackedDevicePose.eTrackingResult;
			bPoseValid = trackedDevicePose.bPoseIsValid;

			sprintf_s(buf, sizeof(buf), "\nGeneric Tracker\nx: %.2f y: %.2f z: %.2f\n", position.v[0], position.v[1], position.v[2]);
			printf_s(buf);

			sprintf_s(buf, sizeof(buf), "State: x: %.2f y: %.2f\n", controllerState.rAxis[0].x, controllerState.rAxis[0].y);
			printf_s(buf);

			break;

		case vr::TrackedDeviceClass_TrackingReference:
			// incomplete code, only here for switch reference
			sprintf_s(buf, sizeof(buf), "Camera / Base Station");
			printf_s(buf);
			break;


		default: {
			
			infoStr = "Unsupported class: %d", trackedDeviceClass;
			isStrGiven = true;
			}
			break;
		}
	}
}

string ViveInputClass::PrintDevices() {

	string s;
	s = "Device list:\n---------------------------\n";

	// Process SteamVR device states
	for (vr::TrackedDeviceIndex_t unDevice = 0; unDevice < vr::k_unMaxTrackedDeviceCount; unDevice++)
	{
		// if no HMD is connected in slot 0 just skip the rest of the code
		// since a HMD must be present.
		if (!m_pHMD->IsTrackedDeviceConnected(unDevice))
			continue;

		vr::TrackedDevicePose_t trackedDevicePose;
		vr::TrackedDevicePose_t *devicePose = &trackedDevicePose;

		vr::VRControllerState_t controllerState;
		vr::VRControllerState_t *ontrollerState_ptr = &controllerState;

		// Get what type of device it is and work with its data
		vr::ETrackedDeviceClass trackedDeviceClass = vr::VRSystem()->GetTrackedDeviceClass(unDevice);
		switch (trackedDeviceClass) {
		case vr::ETrackedDeviceClass::TrackedDeviceClass_HMD:
			// print stuff for the HMD here, see controller stuff in next case block
			s = "Device %d: [HMD]", unDevice;
			break;

		case vr::ETrackedDeviceClass::TrackedDeviceClass_Controller:
			// Simliar to the HMD case block above, please adapt as you like
			// to get away with code duplication and general confusion

			//char buf[1024];
			switch (vr::VRSystem()->GetControllerRoleForTrackedDeviceIndex(unDevice)) {
			case vr::TrackedControllerRole_Invalid:
				// invalid hand...
				s += "Device %d: [Invalid Controller]", unDevice;
				break;

			case vr::TrackedControllerRole_LeftHand:
				s += "Device %d: [Controller - Left]", unDevice;
				break;

			case vr::TrackedControllerRole_RightHand:
				s += "Device %d: [Controller - Right]", unDevice;
				break;

			case vr::TrackedControllerRole_Treadmill:
				s += "Device %d: [Treadmill]", unDevice;
				break;
			}

			break;

		case vr::ETrackedDeviceClass::TrackedDeviceClass_GenericTracker:
			// print stuff for the HMD here, see controller stuff in next case block

			s += "Device %d: [GenericTracker]", unDevice;
			break;

		case vr::ETrackedDeviceClass::TrackedDeviceClass_TrackingReference:
			// print stuff for the HMD here, see controller stuff in next case block

			s += "Device %d: [TrackingReference]", unDevice;
			break;

		case vr::ETrackedDeviceClass::TrackedDeviceClass_DisplayRedirect:
			// print stuff for the HMD here, see controller stuff in next case block

			s += "Device %d: [DisplayRedirect]", unDevice;
			break;

		case vr::ETrackedDeviceClass::TrackedDeviceClass_Invalid:
			// print stuff for the HMD here, see controller stuff in next case block

			s += "Device %d: [Invalid]", unDevice;
			break;

		}

		char manufacturer[1024];
		vr::VRSystem()->GetStringTrackedDeviceProperty(unDevice, vr::ETrackedDeviceProperty::Prop_ManufacturerName_String, manufacturer, sizeof(manufacturer));

		char modelnumber[1024];
		vr::VRSystem()->GetStringTrackedDeviceProperty(unDevice, vr::ETrackedDeviceProperty::Prop_ModelNumber_String, modelnumber, sizeof(modelnumber));

		char serialnumber[1024];
		vr::VRSystem()->GetStringTrackedDeviceProperty(unDevice, vr::ETrackedDeviceProperty::Prop_SerialNumber_String, serialnumber, sizeof(serialnumber));

		s += " %s - %s [%s]\n", manufacturer, modelnumber, serialnumber;
	}
	s += "---------------------------\nEnd of device list\n\n";
	return s;

}
