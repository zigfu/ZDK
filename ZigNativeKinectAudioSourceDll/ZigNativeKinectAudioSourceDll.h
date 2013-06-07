#pragma once

#define _USE_MATH_DEFINES
#include <math.h>
#include <string>
#include <NuiApi.h>				// For Kinect SDK APIs
#include <dmo.h>				// For IMediaObject and related interfaces
#include <wmcodecdsp.h>			// For configuring DMO properties
#include <mmreg.h>				// For WAVEFORMATEX
#include <uuids.h>				// For FORMAT_WaveFormatEx and such

#include "StaticMediaBuffer.h"
#include "KinectAudioSpecs.h"


// ----------------------- Constants and Enums ---------------------------

// The BeamAngle may be set to angles in the range of [-50 to 50] degrees, in increments of 10.
const int iMinBeamAngleValue = 0;		// corresponds to -50 degrees
const int iMaxBeamAngleValue = 10;		// corresponds to 50 degrees
const int iBeamAngleIncrement = 10;	

const MIC_ARRAY_MODE iManualBeamMode = MICARRAY_EXTERN_BEAM;		// MICARRAY_EXTERN_BEAM is the only MIC_ARRAY_MODE that allows manual setting of the BeamAngle
const MIC_ARRAY_MODE iAutomaticBeamMode = MICARRAY_SINGLE_BEAM;		// MICARRAY_SINGLE_BEAM is the standard MIC_ARRAY_MODE for automatic BeamAngle setting

// Contains the possible AEC_SYSTEM_MODE's to which the Kinect Mic Array may be set
// (OPTIBEAM = Automatic Beamforming.  AEC = Acoustic Echo Cancellation)
enum MicrophoneMode
{ 
	MICMODE_SINGLE_CHANNEL			= SINGLE_CHANNEL_NSAGC,			
	MICMODE_SINGLE_CHANNEL_AEC		= SINGLE_CHANNEL_AEC,
	MICMODE_OPTIBEAM_ARRAY_ONLY		= OPTIBEAM_ARRAY_ONLY,			// This is the Standard mode
	MICMODE_OPTIBEAM_ARRAY_AND_AEC	= OPTIBEAM_ARRAY_AND_AEC,		// Use this if you expect to have sound playing from speakers
};
static const MicrophoneMode iDefaultMicrophoneMode = MICMODE_OPTIBEAM_ARRAY_AND_AEC;


// ------------------------------- Vars --------------------------------------

    INuiSensor*             m_pNuiSensor;			// Primary access to the Kinect device
    INuiAudioBeam*          m_pNuiAudioSource;		// Used to query Kinect audio beam and sound source angles.
    IMediaObject*           m_pDMO;					// Media object from which Kinect audio stream is captured.
    IPropertyStore*         m_pPropertyStore;		// Used to configure Kinect audio properties.
    StaticMediaBuffer       m_audioOutputBuffer;	// Holds captured audio data.

	double m_beamAngleInRadians = 0;
	double m_sourceAngle = 0;
	double m_sourceConfidence = 0;


extern "C" 
{
	// --------------------- Exported Function Declarations --------------------------

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
	void EXPORT_API AS_Shutdown();

	// Summary:
	//		If a method returns an HRESULT less than 0, an error message will have been set.
	//		This function returns that error message, then erases it
	EXPORT_API const char* AS_GetLastRecordedErrorMessage();

	// ----------------------- Internal Function Declarations ---------------------------

	HRESULT CreateSensor();
	HRESULT InitializeKinect(INuiSensor* sensor);
	HRESULT ConfigureAudioSource();
	HRESULT SetDmoOutputFormat();

	void UpdateBeamAndSourceAngle();

	HRESULT SetFeatureModeEnabled (bool doEnable);
	HRESULT GetFeatureModeEnabled (bool *outEnabled);
	void EnsureFeatureModeIsEnabled ();

	HRESULT SetMicrophoneMode(MicrophoneMode newMode);
	HRESULT GetMicrophoneMode (MicrophoneMode *outMode);

	HRESULT SetMicArrayProcessingMode(MIC_ARRAY_MODE newMode);
	HRESULT GetMicArrayProcessingMode (MIC_ARRAY_MODE *outMode);

	void CopyAudioBuffer(DWORD elemCnt, BYTE buffer[]);

	void AppendToErrorMessage(std::string message);
	void ClearLastRecordedErrorMessage();
}