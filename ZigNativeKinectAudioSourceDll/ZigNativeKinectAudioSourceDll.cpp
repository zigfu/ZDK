#include "stdafx.h"
#include "ZigNativeKinectAudioSourceDll.h"

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
#include "SimpleMessageRecorder.h"


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

double					m_beamAngleInRadians = 0;
double					m_sourceAngle = 0;
double					m_sourceConfidence = 0;

bool					m_DMOisInitialized = false;

// When "LockDown" is enabled, properties may not be modified externally 
//	(all of the AS_SetProperty methods will return a FAIL code).
//	Additionally, audio cannot be captured through calls to AS_CaptureAudio.  
//
bool					m_lockDownIsEnabled = false;	


// --- Error Reporting  ---

const string AS_ErrorMsgHeader = "ERROR in ZigNativeAudioSourceDll\n";
SimpleMessageRecorder*	AS_Debug;
bool CheckAndLogErrorIfLockDownIsEnabled()
{
	if(m_lockDownIsEnabled) { AS_Debug->AppendToMessage("Kinect properties cannot be set when LockDown is enabled."); }
	return m_lockDownIsEnabled;
}



// ---------------  Non-Exported Functions  ----------------------

// --- Init ---

HRESULT CreateSensor()
{
	HRESULT hr = S_OK;

	INuiSensor * pNuiSensor;
	int iSensorCount = 0;
	hr = NuiGetSensorCount(&iSensorCount);
	if (FAILED(hr)) { AS_Debug->AppendToMessage("Failed to enumerate sensors"); return hr; }

	// Look at each Kinect sensor
	for (int i = 0; i < iSensorCount; ++i)
	{
		// Create the sensor so we can check status, if we can't create it, move on to the next
		hr = NuiCreateSensorByIndex(i, &pNuiSensor);	// Note: This doesn't necessarily create a new sensor - it may return a reference to a sensor already created externally
		if (FAILED(hr)) { continue; }

		// Get the status of the sensor, and if connected, then we can initialize it
		hr = pNuiSensor->NuiStatus();
		if (S_OK == hr) { m_pNuiSensor = pNuiSensor; break; }

		// This sensor wasn't OK, so release it since we're not using it
		pNuiSensor->Release();
	}

	if (NULL == m_pNuiSensor || FAILED(hr)) { AS_Debug->AppendToMessage("Error: No ready Kinect found"); return E_FAIL; }

	return hr;
}

HRESULT InitializeKinect(INuiSensor* sensor)
{
	HRESULT hr = sensor->NuiInitialize(NUI_INITIALIZE_FLAG_USES_AUDIO); 
	if (FAILED(hr))
	{
		SafeRelease(sensor);
		AS_Debug->AppendToMessage("Error: Another application is streaming from the same Kinect sensor");
	}
	return hr;
}

HRESULT ConfigureAudioSource()
{
	HRESULT hr = S_OK;

	hr = m_pNuiSensor->NuiGetAudioSource(&m_pNuiAudioSource);
	if (FAILED(hr)) { AS_Debug->AppendToMessage("Failed to get Audio Source!"); return hr; }

	hr = m_pNuiAudioSource->QueryInterface(IID_IMediaObject, (void**)&m_pDMO);
	if (FAILED(hr)) { AS_Debug->AppendToMessage("Failed to access the DMO!"); return hr; }

	hr = m_pNuiAudioSource->QueryInterface(IID_IPropertyStore, (void**)&m_pPropertyStore);
	if (FAILED(hr)) { AS_Debug->AppendToMessage("Failed to access the Audio Property store!"); return hr; }

	return hr;
}

HRESULT SetDmoOutputFormat()
{
	HRESULT hr = S_OK;

	WAVEFORMATEX wfxOut = AS_GetKinectWaveFormat();
	DMO_MEDIA_TYPE mt = {0};
	hr = MoInitMediaType(&mt, sizeof(WAVEFORMATEX));
	if (FAILED(hr)) { AS_Debug->AppendToMessage("Failed to set the audio wave format"); return hr; }

	mt.majortype = MEDIATYPE_Audio;
	mt.subtype = MEDIASUBTYPE_PCM;
	mt.lSampleSize = 0;
	mt.bFixedSizeSamples = TRUE;
	mt.bTemporalCompression = FALSE;
	mt.formattype = FORMAT_WaveFormatEx;	
	memcpy_s(mt.pbFormat, sizeof(WAVEFORMATEX), &wfxOut, sizeof(WAVEFORMATEX));

	hr = m_pDMO->SetOutputType(0, &mt, 0); 
	if (FAILED(hr)) { AS_Debug->AppendToMessage("DMO failed to SetOutputType."); return hr; }

	MoFreeMediaType(&mt);
	if (FAILED(hr)) { AS_Debug->AppendToMessage("Call to MoFreeMediaType failed."); return hr; }

	m_DMOisInitialized = true;

	return hr;
}


// --- Update ---

// Summary:
//		Obtain beam angle, source angle, and source confidence from INuiAudioBeam afforded by microphone array
//		Note: This method should only be called after IMediaObject::ProcessOutput is called and has returned S_OK
void UpdateBeamAndSourceAngle()
{
	m_pNuiAudioSource->GetBeam(&m_beamAngleInRadians);
	m_pNuiAudioSource->GetPosition(&m_sourceAngle, &m_sourceConfidence);
}


// ---------------  Non-Exported Properties  ----------------------

static const PROPERTYKEY FeatureModeKey = MFPKEY_WMAAECMA_FEATURE_MODE;
HRESULT SetFeatureModeEnabled(bool doEnable)
{
	if(CheckAndLogErrorIfLockDownIsEnabled()) { return -1; }

	PROPVARIANT pv;		
	PropVariantInit(&pv);

	pv.vt = VT_BOOL;
	pv.boolVal = doEnable ? VARIANT_TRUE : VARIANT_FALSE; 
	HRESULT hr = m_pPropertyStore->SetValue(FeatureModeKey, pv);
	if(FAILED(hr)) { AS_Debug->AppendToMessage("Error setting FeatureModeEnabled to %s", doEnable); }
		
	PropVariantClear(&pv);
	return hr;
}
HRESULT GetFeatureModeEnabled(bool *outEnabled)
{
	PROPVARIANT pv;		
	HRESULT hr = m_pPropertyStore->GetValue(FeatureModeKey, &pv);
	if(FAILED(hr)) { AS_Debug->AppendToMessage("Failed to get FeatureModeEnabled"); return hr; }

	*outEnabled = (pv.boolVal == VARIANT_TRUE) ? true : false;
	return hr;
}
void EnsureFeatureModeIsEnabled ()
{
	bool isEnabled;
	HRESULT hr = GetFeatureModeEnabled(&isEnabled);
	if(FAILED(hr)) { AS_Debug->AppendToMessage("Failed to EnsureFeatureModeIsEnabled"); return; }

	if(!isEnabled)
	{
		hr = SetFeatureModeEnabled(true);
		if(FAILED(hr)) { AS_Debug->AppendToMessage("Failed to EnsureFeatureModeIsEnabled"); return; }
	}
}

static const PROPERTYKEY MicrophoneModeKey = MFPKEY_WMAAECMA_SYSTEM_MODE;
static const char* GetStringForMicrophoneMode(MicrophoneMode mode)
{
	string str;
	switch(mode)
	{
		case MICMODE_SINGLE_CHANNEL			: str = "MICMODE_SINGLE_CHANNEL";			break;
		case MICMODE_SINGLE_CHANNEL_AEC		: str = "MICMODE_SINGLE_CHANNEL_AEC";		break;
		case MICMODE_OPTIBEAM_ARRAY_ONLY	: str = "MICMODE_OPTIBEAM_ARRAY_ONLY";		break;
		case MICMODE_OPTIBEAM_ARRAY_AND_AEC	: str = "MICMODE_OPTIBEAM_ARRAY_AND_AEC";	break;
		default:							  str = "UNRECOGNIZED MODE";				break;
	}
	return str.c_str();
}
HRESULT SetMicrophoneMode(MicrophoneMode newMode)
{
	PROPVARIANT pv;
	PropVariantInit(&pv);

	pv.vt = VT_I4;
	pv.lVal = (LONG)(newMode); 
	HRESULT hr = m_pPropertyStore->SetValue(MicrophoneModeKey, pv);
	if(FAILED(hr)) { AS_Debug->AppendToMessage("Failed to set MicrophoneMode to  %s", GetStringForMicrophoneMode(newMode)); }
		
	PropVariantClear(&pv);
	return hr;
}
HRESULT GetMicrophoneMode(MicrophoneMode *outMode)
{
	PROPVARIANT pv;		
	HRESULT hr = m_pPropertyStore->GetValue(MicrophoneModeKey, &pv);
	if(FAILED(hr)) { AS_Debug->AppendToMessage("Failed to get MicrophoneMode"); return hr; }

	*outMode = (MicrophoneMode)pv.intVal;
	return hr;
}


static const PROPERTYKEY MicArrayProcessingModeKey = MFPKEY_WMAAECMA_FEATR_MICARR_MODE;
const char* GetStringForMIC_ARRAY_MODE(MIC_ARRAY_MODE mode)
{
	string str;
	switch(mode)
	{
		case MICARRAY_SINGLE_CHAN	:	str = "MICARRAY_SINGLE_CHAN";	break;
		case MICARRAY_SIMPLE_SUM	:	str = "MICARRAY_SIMPLE_SUM";	break;
		case MICARRAY_SINGLE_BEAM	:	str = "MICARRAY_SINGLE_BEAM";	break;
		case MICARRAY_FIXED_BEAM	:	str = "MICARRAY_FIXED_BEAM";	break;
		case MICARRAY_EXTERN_BEAM	:	str = "MICARRAY_EXTERN_BEAM";	break;
		default:						str = "UNRECOGNIZED MODE";		break;
	}
	return str.c_str();
}
HRESULT SetMicArrayProcessingMode(MIC_ARRAY_MODE newMode)
{
	EnsureFeatureModeIsEnabled();

	PROPVARIANT pv;
	PropVariantInit(&pv);

	pv.vt = VT_I4;
	pv.lVal = (LONG)(newMode); 
	HRESULT hr = m_pPropertyStore->SetValue(MicArrayProcessingModeKey, pv);
	if(FAILED(hr)) { AS_Debug->AppendToMessage("Failed to set MicArrayProcessingMode to %s", GetStringForMIC_ARRAY_MODE(newMode)); }
		
	PropVariantClear(&pv);
	return hr;
}
HRESULT GetMicArrayProcessingMode(MIC_ARRAY_MODE *outMode)
{
	PROPVARIANT pv;		
	HRESULT hr = m_pPropertyStore->GetValue(MicArrayProcessingModeKey, &pv);
	if(FAILED(hr)) { AS_Debug->AppendToMessage("Failed to get MicArrayProcessingMode"); return hr; }

	*outMode = (MIC_ARRAY_MODE)pv.intVal;
	return hr;
}



extern "C" 
{

	// -------------------  Exported Properties  --------------------------

	DWORD EXPORT_API AS_GetAudioBufferMaxCapacity()
	{
		DWORD maxLength = 0;
		m_audioOutputBuffer.GetMaxLength(&maxLength);
		return maxLength;
	}

	HRESULT EXPORT_API AS_SetManualBeamModeEnabled(bool doEnable)
	{
		if(CheckAndLogErrorIfLockDownIsEnabled()) { return -1; }

		MIC_ARRAY_MODE newMode = doEnable ? iManualBeamMode : iAutomaticBeamMode;
		HRESULT hr = SetMicArrayProcessingMode(newMode);
		if(FAILED(hr)) { AS_Debug->AppendToMessage("Error setting ManualBeamModeEnabled to %s", doEnable); return hr; }

		return hr;
	}
	HRESULT EXPORT_API AS_GetManualBeamModeEnabled(bool *outEnabled)
	{
		MIC_ARRAY_MODE pMode;
		HRESULT hr = GetMicArrayProcessingMode(&pMode);
		if(FAILED(hr)) { AS_Debug->AppendToMessage("Error getting ManualBeamModeEnabled"); return hr; }

		*outEnabled = (pMode == iManualBeamMode);
		return hr;
	}

	HRESULT EXPORT_API AS_SetBeamAngleInRadians(double newAngle)
	{
		if(CheckAndLogErrorIfLockDownIsEnabled()) { return -1; }

		HRESULT hr = m_pNuiAudioSource->SetBeam(newAngle);
		if(FAILED(hr)) { AS_Debug->AppendToMessage("Error setting BeamAngleInRadians to %f", (float)newAngle); return hr; }

		m_beamAngleInRadians = newAngle;

		return hr;
	}
	double EXPORT_API AS_GetBeamAngleInRadians()
	{
		return m_beamAngleInRadians;
	}

	double EXPORT_API AS_GetSourceAngleInRadians()
	{
		return m_sourceAngle;
	}

	double EXPORT_API AS_GetSourceAngleConfidence()
	{
		return m_sourceConfidence;
	}

	static const PROPERTYKEY AutomaticGainControlKey = MFPKEY_WMAAECMA_FEATR_AGC;
	HRESULT EXPORT_API AS_SetAutomaticGainControlEnabled(bool doEnable)
	{
		if(CheckAndLogErrorIfLockDownIsEnabled()) { return -1; }

		EnsureFeatureModeIsEnabled();

		PROPVARIANT pv;		
		PropVariantInit(&pv);

		pv.vt = VT_BOOL;
		pv.boolVal = doEnable ? VARIANT_TRUE : VARIANT_FALSE; 
		HRESULT hr = m_pPropertyStore->SetValue(AutomaticGainControlKey, pv);
		if(FAILED(hr)) { AS_Debug->AppendToMessage("Error setting AutomaticGainControlEnabled to %s", doEnable); }

		PropVariantClear(&pv);
		return hr;
	}
	HRESULT EXPORT_API AS_GetAutomaticGainControlEnabled(bool *outEnable)
	{
		PROPVARIANT pv;		
		HRESULT hr = m_pPropertyStore->GetValue(AutomaticGainControlKey, &pv);
		if(FAILED(hr)) { AS_Debug->AppendToMessage("Error getting AutomaticGainControlEnabled"); return hr; }

		*outEnable = (pv.boolVal == VARIANT_TRUE) ? true : false;
		return hr;
	}

	static const PROPERTYKEY NoiseSuppressionKey = MFPKEY_WMAAECMA_FEATR_NS;
	HRESULT EXPORT_API AS_SetNoiseSuppressionEnabled(bool doEnable)
	{
		if(CheckAndLogErrorIfLockDownIsEnabled()) { return -1; }

		EnsureFeatureModeIsEnabled();

		PROPVARIANT pv;		
		PropVariantInit(&pv);

		pv.vt = VT_I4;
		pv.lVal = doEnable ? (LONG)1 : (LONG)0; 
		HRESULT hr = m_pPropertyStore->SetValue(NoiseSuppressionKey, pv);
		if(FAILED(hr)) { AS_Debug->AppendToMessage("Error setting NoiseSuppressionEnabled to %s", doEnable); }

		PropVariantClear(&pv);
		return hr;
	}
	HRESULT EXPORT_API AS_GetNoiseSuppressionEnabled(bool *outEnable)
	{
		PROPVARIANT pv;		
		HRESULT hr = m_pPropertyStore->GetValue(NoiseSuppressionKey, &pv);
		if(FAILED(hr)) { AS_Debug->AppendToMessage("Error getting NoiseSuppressionEnabled"); return hr; }

		*outEnable = (pv.lVal == 1) ? true : false;
		return hr;
	}

	static const PROPERTYKEY AcousticEchoCancellationLengthKey = MFPKEY_WMAAECMA_FEATR_ECHO_LENGTH;
	HRESULT EXPORT_API AS_SetAcousticEchoCancellationLength(LONG newLength)
	{
		if(CheckAndLogErrorIfLockDownIsEnabled()) { return -1; }

		EnsureFeatureModeIsEnabled();

		// Constrain newLength to recommended values
		if(newLength <= 128)		{ newLength = 128; }
		else if(newLength <= 256)	{ newLength = 256; }
		else if(newLength <= 512)	{ newLength = 512; }
		else						{ newLength = 1024; }

		PROPVARIANT pv;		
		PropVariantInit(&pv);

		pv.vt = VT_I4;
		pv.lVal = newLength; 
		HRESULT hr = m_pPropertyStore->SetValue(AcousticEchoCancellationLengthKey, pv);
		if(FAILED(hr)) { AS_Debug->AppendToMessage("Error setting AcousticEchoCancellationLength to %i", newLength); }

		PropVariantClear(&pv);
		return hr;
	}
	HRESULT EXPORT_API AS_GetAcousticEchoCancellationLength(LONG *outLength)
	{
		PROPVARIANT pv;		
		HRESULT hr = m_pPropertyStore->GetValue(AcousticEchoCancellationLengthKey, &pv);
		if(FAILED(hr)) { AS_Debug->AppendToMessage("Error getting AcousticEchoCancellationLength"); return hr; }

		*outLength = pv.lVal;
		return hr;
	}

	static const PROPERTYKEY AcousticEchoSuppressionCountKey = MFPKEY_WMAAECMA_FEATR_AES;
	HRESULT EXPORT_API AS_SetAcousticEchoSuppressionCount(LONG newCount)
	{
		if(CheckAndLogErrorIfLockDownIsEnabled()) { return -1; }

		EnsureFeatureModeIsEnabled();

		// echoSuppressionCount must be either 0, 1, or 2
		if(newCount < 0)	  { newCount = 0; }
		else if(newCount > 2) { newCount = 2; }
		
		PROPVARIANT pv;		
		PropVariantInit(&pv);

		pv.vt = VT_I4;
		pv.lVal = newCount; 
		HRESULT hr = m_pPropertyStore->SetValue(AcousticEchoSuppressionCountKey, pv);
		if(FAILED(hr)) { AS_Debug->AppendToMessage("Error setting AcousticEchoSuppressionCount to %i", newCount); }
	
		PropVariantClear(&pv);
		return hr;
	}
	HRESULT EXPORT_API AS_GetAcousticEchoSuppressionCount(LONG *outCount)
	{
		PROPVARIANT pv;		
		HRESULT hr = m_pPropertyStore->GetValue(AcousticEchoSuppressionCountKey, &pv);
		if(FAILED(hr)) { AS_Debug->AppendToMessage("Error getting AcousticEchoSuppressionCount"); return hr; }

		*outCount = pv.lVal;
		return hr;
	}

	WAVEFORMATEX EXPORT_API AS_GetKinectWaveFormat()
	{
		WAVEFORMATEX wf = { AudioFormat, AudioChannels, AudioSamplesPerSecond, AudioAverageBytesPerSecond, AudioBlockAlign, AudioBitsPerSample, 0};
		return wf;
	}

	HRESULT EXPORT_API AS_SetLockDownEnabled(bool doEnable)
	{
		m_lockDownIsEnabled = doEnable;
		return S_OK;
	}
	HRESULT EXPORT_API AS_GetLockDownEnabled(bool &outEnabled)
	{
		outEnabled = m_lockDownIsEnabled;
		return S_OK;
	}


	// ---------------  Exported Functions  ----------------------


	HRESULT EXPORT_API AS_InitializeAudioSource(bool doInitializeKinect)
	{
		HRESULT hr = S_OK;

		AS_Debug = new SimpleMessageRecorder(AS_ErrorMsgHeader);


		if(NULL == m_pNuiSensor)
		{
			hr = CreateSensor();
			if (FAILED(hr)) { AS_Debug->AppendToMessage("Failed to create sensor"); return hr; }
		}

		if(doInitializeKinect)
		{
			hr = InitializeKinect(m_pNuiSensor);
			if (FAILED(hr)) { AS_Debug->AppendToMessage("Failed to initialize Kinect"); return hr; }
		}

		hr = ConfigureAudioSource();
		if (FAILED(hr)) { AS_Debug->AppendToMessage("Failed to configure AudioSource"); return hr; }

		hr = SetMicrophoneMode(iDefaultMicrophoneMode);
		if (FAILED(hr)) { AS_Debug->AppendToMessage("Failed to set MicrophoneMode to default."); return hr; }

		hr = SetDmoOutputFormat();
		if (FAILED(hr)) { AS_Debug->AppendToMessage("Failed to set DmoOutputFormat"); return hr; }


		return hr;
	}

	void EXPORT_API AS_Destroy()
	{
		SafeRelease(m_pNuiSensor);
		SafeRelease(m_pNuiAudioSource);
		SafeRelease(m_pDMO);
		SafeRelease(m_pPropertyStore);

		m_DMOisInitialized = false;
		m_lockDownIsEnabled = true;
	}

	HRESULT EXPORT_API AS_CaptureAudio(DWORD bufferSize, BYTE buffer[], DWORD *pNumSamplesCaptured)
	{
		HRESULT hr = S_OK;

		if (!pNumSamplesCaptured)
		{
			AS_Debug->AppendToMessage("Failed to capture audio output.  pNumSamplesCaptured was NULL.");
			return E_INVALIDARG;
		}

		*pNumSamplesCaptured = 0;

		if (m_lockDownIsEnabled)
		{
			AS_Debug->AppendToMessage("Cannot capture audio from Kinect device when LockDown is enabled");
			return -1;
		}

		BYTE *pCapturedSamples = NULL;

		DMO_OUTPUT_DATA_BUFFER DMOOutputDataBuffer = {0};
		DMOOutputDataBuffer.pBuffer = &m_audioOutputBuffer;

		bool bufferHasBeenSuccessfullyFilled = false;
		do
		{
			m_audioOutputBuffer.Init(0);
			DMOOutputDataBuffer.dwStatus = 0;
			hr = m_pDMO->ProcessOutput(0, 1, &DMOOutputDataBuffer, &DMOOutputDataBuffer.dwStatus);

			if (FAILED(hr))
			{
				AS_Debug->AppendToMessage("A call to IMediaObject::ProcessOutput failed.");
				break;
			}

			if (hr == S_FALSE) { *pNumSamplesCaptured = 0; }
			else { m_audioOutputBuffer.GetBufferAndLength(&pCapturedSamples, pNumSamplesCaptured); }
			
			if (*pNumSamplesCaptured > 0) { UpdateBeamAndSourceAngle(); }

			bufferHasBeenSuccessfullyFilled = !(DMOOutputDataBuffer.dwStatus & DMO_OUTPUT_DATA_BUFFERF_INCOMPLETE);

		} while (!bufferHasBeenSuccessfullyFilled);

		if(bufferHasBeenSuccessfullyFilled)
		{
			// Copy captured samples into buffer
			DWORD copyAmount = min(bufferSize, *pNumSamplesCaptured);
			for (DWORD i = 0; i < copyAmount; ++i)
			{
				buffer[i] = pCapturedSamples[i];
			}
		}

		return hr;
	}

	EXPORT_API const char* AS_GetLastRecordedErrorMessage(bool doClearMessage)
	{
		return AS_Debug->GetLastRecordedMessage(doClearMessage);
	}

}


bool TryGetDmo(IMediaObject** out_pDMO) 
{
	if(out_pDMO == NULL)	{ return false; }
	if(!m_DMOisInitialized) { return false; }

	*out_pDMO = m_pDMO; 
	return true;
}  