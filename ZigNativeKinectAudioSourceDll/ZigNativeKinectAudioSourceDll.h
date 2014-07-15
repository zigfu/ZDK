#pragma once

#include <dmo.h>				// For IMediaObject and related interfaces
#include <mmreg.h>				// For WAVEFORMATEX


extern "C" 
{
	// --------------------- Properties --------------------------

	// Summary:
	//		The large, constant, maximum capacity of m_audioOutputBuffer.
	//		This should be the size of the buffer you pass into AS_CaptureAudio()
	//		in order to guarentee it is big enough to hold the processed audio samples
	DWORD EXPORT_API AS_GetAudioBufferMaxCapacity();
	
	// Summary:
	//		When this mode is enabled, BeamAngle is no longer automatically updated,
	//		and it can be set by the application.
	HRESULT EXPORT_API AS_SetManualBeamModeEnabled (bool doEnable);
	HRESULT EXPORT_API AS_GetManualBeamModeEnabled (bool *outEnabled);

	// Summary:
	//		The direction that the sensor is actively listening.
	double EXPORT_API AS_GetBeamAngleInRadians();
	HRESULT EXPORT_API AS_SetBeamAngleInRadians(double newAngle);

	// Summary:
	//		The direction the Kinect believes prominent audio is currently coming from
	double EXPORT_API AS_GetSourceAngleInRadians();

	// Summary:
	//		How certain the Kinect currently is of sourceAngle's accuracy
	double EXPORT_API AS_GetSourceAngleConfidence();

	// Summary:
	//		Automatic gain control is a digital signal processing (DSP) component that adjusts the gain so that 
	//		the output level of the signal remains within the same approximate range. 
	HRESULT EXPORT_API AS_SetAutomaticGainControlEnabled (bool doEnable);
	HRESULT EXPORT_API AS_GetAutomaticGainControlEnabled (bool *outEnable);

	// Summary:
	//		Noise suppression is a digital signal processing (DSP) component that suppresses 
	//		or reduces stationary background noise in the audio signal.
	HRESULT EXPORT_API AS_SetNoiseSuppressionEnabled (bool doEnable);
	HRESULT EXPORT_API AS_GetNoiseSuppressionEnabled (bool *outEnable);

	// Summary:
	//		The AEC algorithm uses an adaptive filter whose length is determined by the duration of the echo. 
	//		It is recommended that applications use one of the following values: 128, 256, 512, 1024
	HRESULT EXPORT_API AS_SetAcousticEchoCancellationLength (LONG newLength);
	HRESULT EXPORT_API AS_GetAcousticEchoCancellationLength (LONG *outLength);

	// Summary:
	//		The Voice Capture DSP can perform AES on the residual signal after echo cancellation.
	//		This property can have the value 0, 1, or 2. The default value is 0.
	HRESULT EXPORT_API AS_SetAcousticEchoSuppressionCount (LONG newCount);
	HRESULT EXPORT_API AS_GetAcousticEchoSuppressionCount (LONG *outCount);

	// Summary:
	//		Returns a struct that describes the Kinects audio format
	WAVEFORMATEX EXPORT_API AS_GetKinectWaveFormat();

	HRESULT EXPORT_API AS_SetLockDownEnabled (bool doEnable);
	HRESULT EXPORT_API AS_GetLockDownEnabled (bool &outEnabled);


	// --------------------- Functions --------------------------

	// Summary:
	//		Initialize Kinect audio capture/control objects.
	//		If doInitializeKinect is true then Kinect will be initialized via INuiSensor::NuiInitialize(),
	//		otherwise the application must initialize Kinect prior to calling this method
	HRESULT EXPORT_API AS_InitializeAudioSource(bool doInitializeKinect);

	// Summary:
	//		Loads m_audioOutputBuffer with the most recent Kinect audio data,
	//		and updates beamAngle, sourceAngle, and sourceConfidence.
	//		This function should be called often, at regular intervals, and probably on its own separate thread
	//		Upon returning, buffer will contain the latest processed samples, and numSamplesProcessed will
	//		be set to the number of samples processed.
	//
	// IMPORTANT:
	//		buffer must be of size AS_GetAudioBufferMaxCapacity() in order to ensure it
	//		is large enough to hold all the processed samples.
	//
	HRESULT EXPORT_API AS_CaptureAudio(DWORD bufferSize, BYTE buffer[], DWORD *numSamplesCaptured);

	// Summary:
	//		Releases all memory allocated within ZigNativeAudioSourceDll
	void EXPORT_API AS_Destroy();

	// Summary:
	//		If a method returns an HRESULT less than 0, an error message will have been set.
	//		This function returns that error message, then erases it
	EXPORT_API const char* AS_GetLastRecordedErrorMessage(bool doClearMessage = false);

}


bool TryGetDmo(IMediaObject** out_pDMO);     	