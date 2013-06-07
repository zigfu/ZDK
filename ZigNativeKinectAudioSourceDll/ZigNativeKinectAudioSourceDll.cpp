#include "stdafx.h"
#include "ZigNativeKinectAudioSourceDll.h"


// Summary:
//		This file contains declarations of external functions imported from ZigNativeKinectAudioSourceDll.dll.
//		The ZigNativeKinectAudioSourceDll project contains Kinect Audio API functions written in native C.
//		It is intended to be built as a dll and imported by the Managed C# class ZigKinectAudioSource.
//	
// Note:
//		For documentation, see header file (ZigNativeKinectAudioSourceDll.h) 
//
extern "C" 
{
	// -------------------  Get/Set Properties  --------------------------

	DWORD EXPORT_API AS_GetAudioBufferMaxCapacity()
	{
		DWORD maxLength = 0;
		m_audioOutputBuffer.GetMaxLength(&maxLength);
		return maxLength;
	}

	HRESULT EXPORT_API AS_SetManualBeamModeEnabled (bool doEnable)
	{
		MIC_ARRAY_MODE newMode = doEnable ? iManualBeamMode : iAutomaticBeamMode;
		HRESULT hr = SetMicArrayProcessingMode(newMode);
		if(FAILED(hr))
		{
			std::string boolStr = doEnable ? "true" : "false";
			char msg[100];
			sprintf_s(msg, "Error setting ManualBeamModeEnabled to %s", boolStr.c_str());
			AppendToErrorMessage(msg);
		}
		return hr;
	}
	HRESULT EXPORT_API AS_GetManualBeamModeEnabled (bool *outEnabled)
	{
		MIC_ARRAY_MODE pMode;
		HRESULT hr = GetMicArrayProcessingMode(&pMode);
		if(FAILED(hr))
		{
			AppendToErrorMessage("Error getting ManualBeamModeEnabled");
			return hr;
		}

		*outEnabled = (pMode == iManualBeamMode);
		return hr;
	}

	HRESULT EXPORT_API AS_SetBeamAngleInRadians(double newAngle)
	{
		HRESULT hr = m_pNuiAudioSource->SetBeam(newAngle);
		if(FAILED(hr))
		{
			char msg[100];
			sprintf_s(msg, "Error setting BeamAngleInRadians to %f", newAngle);
			AppendToErrorMessage(msg);
			return hr;
		}

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
	HRESULT EXPORT_API AS_SetAutomaticGainControlEnabled (bool doEnable)
	{
		EnsureFeatureModeIsEnabled();

		PROPVARIANT pv;		
		PropVariantInit(&pv);

		pv.vt = VT_BOOL;
		pv.boolVal = doEnable ? VARIANT_TRUE : VARIANT_FALSE; 
		HRESULT hr = m_pPropertyStore->SetValue(AutomaticGainControlKey, pv);
		if(FAILED(hr))
		{
			std::string boolStr = doEnable ? "true" : "false";
			char msg[100];
			sprintf_s(msg, "Error setting AutomaticGainControlEnabled to %s", boolStr.c_str());
			AppendToErrorMessage(msg);
		}

		PropVariantClear(&pv);

		return hr;
	}
	HRESULT EXPORT_API AS_GetAutomaticGainControlEnabled (bool *outEnable)
	{
		PROPVARIANT pv;		
		HRESULT hr = m_pPropertyStore->GetValue(AutomaticGainControlKey, &pv);
		if(FAILED(hr))
		{
			AppendToErrorMessage("Error getting AutomaticGainControlEnabled");
			return hr;
		}

		*outEnable = (pv.boolVal == VARIANT_TRUE) ? true : false;
		return hr;
	}

	static const PROPERTYKEY NoiseSuppressionKey = MFPKEY_WMAAECMA_FEATR_NS;
	HRESULT EXPORT_API AS_SetNoiseSuppressionEnabled (bool doEnable)
	{
		EnsureFeatureModeIsEnabled();

		PROPVARIANT pv;		
		PropVariantInit(&pv);

		pv.vt = VT_I4;
		pv.lVal = doEnable ? (LONG)1 : (LONG)0; 
		HRESULT hr = m_pPropertyStore->SetValue(NoiseSuppressionKey, pv);
		if(FAILED(hr))
		{
			std::string boolStr = doEnable ? "true" : "false";
			char msg[100];
			sprintf_s(msg, "Error setting NoiseSuppressionEnabled to %s", boolStr.c_str());
			AppendToErrorMessage(msg);
		}

		PropVariantClear(&pv);

		return hr;
	}
	HRESULT EXPORT_API AS_GetNoiseSuppressionEnabled (bool *outEnable)
	{
		PROPVARIANT pv;		
		HRESULT hr = m_pPropertyStore->GetValue(NoiseSuppressionKey, &pv);
		if(FAILED(hr))
		{
			AppendToErrorMessage("Error getting NoiseSuppressionEnabled");
			return hr;
		}

		*outEnable = (pv.lVal == 1) ? true : false;
		return hr;
	}

	static const PROPERTYKEY AcousticEchoCancellationLengthKey = MFPKEY_WMAAECMA_FEATR_ECHO_LENGTH;
	HRESULT EXPORT_API AS_SetAcousticEchoCancellationLength (LONG newLength)
	{
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
		if(FAILED(hr))
		{
			char msg[100];
			sprintf_s(msg, "Error setting AcousticEchoCancellationLength to %i", newLength);
			AppendToErrorMessage(msg);
		}

		PropVariantClear(&pv);

		return hr;
	}
	HRESULT EXPORT_API AS_GetAcousticEchoCancellationLength (LONG *outLength)
	{
		PROPVARIANT pv;		
		HRESULT hr = m_pPropertyStore->GetValue(AcousticEchoCancellationLengthKey, &pv);
		if(FAILED(hr))
		{
			AppendToErrorMessage("Error getting AcousticEchoCancellationLength");
			return hr;
		}

		*outLength = pv.lVal;
		return hr;
	}

	static const PROPERTYKEY AcousticEchoSuppressionCountKey = MFPKEY_WMAAECMA_FEATR_AES;
	HRESULT EXPORT_API AS_SetAcousticEchoSuppressionCount (LONG newCount)
	{
		EnsureFeatureModeIsEnabled();

		// echoSuppressionCount must be either 0, 1, or 2
		if(newCount < 0)	  { newCount = 0; }
		else if(newCount > 2) { newCount = 2; }
		
		PROPVARIANT pv;		
		PropVariantInit(&pv);

		pv.vt = VT_I4;
		pv.lVal = newCount; 
		HRESULT hr = m_pPropertyStore->SetValue(AcousticEchoSuppressionCountKey, pv);
		if(FAILED(hr))
		{
			char msg[100];
			sprintf_s(msg, "Error setting AcousticEchoSuppressionCount to %i", newCount);
			AppendToErrorMessage(msg);
		}

		PropVariantClear(&pv);

		return hr;
	}
	HRESULT EXPORT_API AS_GetAcousticEchoSuppressionCount (LONG *outCount)
	{
		PROPVARIANT pv;		
		HRESULT hr = m_pPropertyStore->GetValue(AcousticEchoSuppressionCountKey, &pv);
		if(FAILED(hr))
		{
			AppendToErrorMessage("Error getting AcousticEchoSuppressionCount");
			return hr;
		}

		*outCount = pv.lVal;
		return hr;
	}

	WAVEFORMATEX EXPORT_API AS_GetKinectWaveFormat()
	{
		WAVEFORMATEX wf = { AudioFormat, AudioChannels, AudioSamplesPerSecond, AudioAverageBytesPerSecond, AudioBlockAlign, AudioBitsPerSample, 0};
		return wf;
	}


	// ---------------  Init and Shutdown  ----------------------

	HRESULT EXPORT_API AS_InitializeAudioSource(bool doInitializeKinect)
	{
		HRESULT hr = S_OK;

		if(NULL == m_pNuiSensor)
		{
			hr = CreateSensor();
			if (FAILED(hr))
			{
				AppendToErrorMessage("Failed to create sensor");
				return hr;
			}
		}

		if(doInitializeKinect)
		{
			hr = InitializeKinect(m_pNuiSensor);
			if (FAILED(hr))
			{
				AppendToErrorMessage("Failed to initialize Kinect");
				return hr;
			}
		}

		hr = ConfigureAudioSource();
		if (FAILED(hr))
		{
			AppendToErrorMessage("Failed to configure AudioSource");
			return hr;
		}

		hr = SetMicrophoneMode(iDefaultMicrophoneMode);
		if (FAILED(hr))
		{
			AppendToErrorMessage("Failed to set MicrophoneMode to default value of " + iDefaultMicrophoneMode);
			return hr;
		}

		hr = SetDmoOutputFormat();
		if (FAILED(hr))
		{
			AppendToErrorMessage("Failed to set DmoOutputFormat");
			return hr;
		}

		return hr;
	}

	HRESULT CreateSensor()
	{
		HRESULT hr = S_OK;

		INuiSensor * pNuiSensor;
		int iSensorCount = 0;
		hr = NuiGetSensorCount(&iSensorCount);
		if (FAILED(hr))
		{
			AppendToErrorMessage("Failed to enumerate sensors!");
			return hr;
		}

		// Look at each Kinect sensor
		for (int i = 0; i < iSensorCount; ++i)
		{
			// Create the sensor so we can check status, if we can't create it, move on to the next
			hr = NuiCreateSensorByIndex(i, &pNuiSensor);
			if (FAILED(hr))
			{
				continue;
			}

			// Get the status of the sensor, and if connected, then we can initialize it
			hr = pNuiSensor->NuiStatus();
			if (S_OK == hr)
			{
				m_pNuiSensor = pNuiSensor;
				break;
			}

			// This sensor wasn't OK, so release it since we're not using it
			pNuiSensor->Release();
		}

		if (NULL == m_pNuiSensor || FAILED(hr))
		{
			AppendToErrorMessage("No ready Kinect found!");
			return E_FAIL;
		}

		return hr;
	}

	HRESULT InitializeKinect(INuiSensor* sensor)
	{
		HRESULT hr = sensor->NuiInitialize(NUI_INITIALIZE_FLAG_USES_AUDIO); 
		if (FAILED(hr))
		{
			SafeRelease(sensor);
			AppendToErrorMessage("Error: Another application is streaming from the same Kinect sensor");
		}
		return hr;
	}

	HRESULT ConfigureAudioSource()
	{
		HRESULT hr = S_OK;

		hr = m_pNuiSensor->NuiGetAudioSource(&m_pNuiAudioSource);
		if (FAILED(hr))
		{
			AppendToErrorMessage("Failed to get Audio Source!");
			return hr;
		}

		hr = m_pNuiAudioSource->QueryInterface(IID_IMediaObject, (void**)&m_pDMO);
		if (FAILED(hr))
		{
			AppendToErrorMessage("Failed to access the DMO!");
			return hr;
		}

		hr = m_pNuiAudioSource->QueryInterface(IID_IPropertyStore, (void**)&m_pPropertyStore);
		if (FAILED(hr))
		{
			AppendToErrorMessage("Failed to access the Audio Property store!");
			return hr;
		}

		return hr;
	}

	HRESULT SetDmoOutputFormat()
	{
		HRESULT hr = S_OK;

		WAVEFORMATEX wfxOut = AS_GetKinectWaveFormat();
		DMO_MEDIA_TYPE mt = {0};
		hr = MoInitMediaType(&mt, sizeof(WAVEFORMATEX));
		if (FAILED(hr))
		{
			AppendToErrorMessage("Failed to set the audio wave format");
			return hr;
		}

		mt.majortype = MEDIATYPE_Audio;
		mt.subtype = MEDIASUBTYPE_PCM;
		mt.lSampleSize = 0;
		mt.bFixedSizeSamples = TRUE;
		mt.bTemporalCompression = FALSE;
		mt.formattype = FORMAT_WaveFormatEx;	
		memcpy_s(mt.pbFormat, sizeof(WAVEFORMATEX), &wfxOut, sizeof(WAVEFORMATEX));

		hr = m_pDMO->SetOutputType(0, &mt, 0); 
		MoFreeMediaType(&mt);

		return hr;
	}

	/// <summary>
	/// Releases allocated memory
	/// </summary>
	void EXPORT_API AS_Shutdown()
	{
		if (NULL != m_pNuiSensor)
		{
			m_pNuiSensor->NuiShutdown();
		}

		SafeRelease(m_pNuiSensor);
		SafeRelease(m_pNuiAudioSource);
		SafeRelease(m_pDMO);
		SafeRelease(m_pPropertyStore);
	}


	// ---------------  Update  ----------------------

	/// <summary>
	/// Capture new audio data from the Kinect and copies it into buffer
	/// </summary>
	HRESULT EXPORT_API AS_CaptureAudio(DWORD bufferSize, BYTE buffer[], DWORD *pNumSamplesCaptured)
	{
		if(pNumSamplesCaptured)
		{
			*pNumSamplesCaptured = 0;
		}

		BYTE *pCapturedSamples = NULL;

		DMO_OUTPUT_DATA_BUFFER DMOOutputDataBuffer = {0};
		DMOOutputDataBuffer.pBuffer = &m_audioOutputBuffer;

		HRESULT hr = S_OK;

		bool bufferHasBeenSuccessfullyFilled = false;
		do
		{
			m_audioOutputBuffer.Init(0);
			DMOOutputDataBuffer.dwStatus = 0;
			hr = m_pDMO->ProcessOutput(0, 1, &DMOOutputDataBuffer, &DMOOutputDataBuffer.dwStatus);

			if (FAILED(hr))
			{
				AppendToErrorMessage("Failed to capture audio output.");
				break;
			}

			if (hr == S_FALSE)
			{
				*pNumSamplesCaptured = 0;
			}
			else
			{
				m_audioOutputBuffer.GetBufferAndLength(&pCapturedSamples, pNumSamplesCaptured);
			}
			
			if (*pNumSamplesCaptured > 0)
			{
				UpdateBeamAndSourceAngle();
			}

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

	/// <summary>
	/// Obtain beam angle, source angle, and source confidence from INuiAudioBeam afforded by microphone array
	///	Note: This method shouldonly be called after IMediaObject::ProcessOutput is called and has returned S_OK
	/// </summary>
	void UpdateBeamAndSourceAngle()
	{
		m_pNuiAudioSource->GetBeam(&m_beamAngleInRadians);
		m_pNuiAudioSource->GetPosition(&m_sourceAngle, &m_sourceConfidence);
	}


	// ---------------  Private Utility Methods  ----------------------

	static const PROPERTYKEY FeatureModeKey = MFPKEY_WMAAECMA_FEATURE_MODE;
	HRESULT SetFeatureModeEnabled (bool doEnable)
	{
		PROPVARIANT pv;		
		PropVariantInit(&pv);

		pv.vt = VT_BOOL;
		pv.boolVal = doEnable ? VARIANT_TRUE : VARIANT_FALSE; 
		HRESULT hr = m_pPropertyStore->SetValue(FeatureModeKey, pv);
		if(FAILED(hr))
		{
			AppendToErrorMessage("Failed to set FeatureModeEnabled to " + doEnable);
		}
		
		PropVariantClear(&pv);

		return hr;
	}
	HRESULT GetFeatureModeEnabled (bool *outEnabled)
	{
		PROPVARIANT pv;		
		HRESULT hr = m_pPropertyStore->GetValue(FeatureModeKey, &pv);
		if(FAILED(hr))
		{
			AppendToErrorMessage("Failed to get FeatureModeEnabled");
			return hr;
		}

		*outEnabled = (pv.boolVal == VARIANT_TRUE) ? true : false;
		return hr;
	}
	void EnsureFeatureModeIsEnabled ()
	{
		bool isEnabled;
		HRESULT hr = GetFeatureModeEnabled(&isEnabled);
		if(FAILED(hr))
		{
			AppendToErrorMessage("Failed to EnsureFeatureModeIsEnabled");
			return;
		}

		if(!isEnabled)
		{
			hr = SetFeatureModeEnabled(true);
			if(FAILED(hr))
			{
				AppendToErrorMessage("Failed to EnsureFeatureModeIsEnabled");
				return;
			}
		}
	}

	static const PROPERTYKEY MicrophoneModeKey = MFPKEY_WMAAECMA_SYSTEM_MODE;
	HRESULT SetMicrophoneMode(MicrophoneMode newMode)
	{
		PROPVARIANT pv;
		PropVariantInit(&pv);

		pv.vt = VT_I4;
		pv.lVal = (LONG)(newMode); 
		HRESULT hr = m_pPropertyStore->SetValue(MicrophoneModeKey, pv);
		if(FAILED(hr))
		{
			AppendToErrorMessage("Failed to set MicrophoneMode to " + newMode);
		}
		
		PropVariantClear(&pv);

		return hr;
	}
	HRESULT GetMicrophoneMode (MicrophoneMode *outMode)
	{
		PROPVARIANT pv;		
		HRESULT hr = m_pPropertyStore->GetValue(MicrophoneModeKey, &pv);
		if(FAILED(hr))
		{
			AppendToErrorMessage("Failed to get MicrophoneMode");
			return hr;
		}

		*outMode = (MicrophoneMode)pv.intVal;
		return hr;
	}

	static const PROPERTYKEY MicArrayProcessingModeKey = MFPKEY_WMAAECMA_FEATR_MICARR_MODE;
	HRESULT SetMicArrayProcessingMode(MIC_ARRAY_MODE newMode)
	{
		EnsureFeatureModeIsEnabled();

		PROPVARIANT pv;
		PropVariantInit(&pv);

		pv.vt = VT_I4;
		pv.lVal = (LONG)(newMode); 
		HRESULT hr = m_pPropertyStore->SetValue(MicArrayProcessingModeKey, pv);
		if(FAILED(hr))
		{
			AppendToErrorMessage("Failed to set MicArrayProcessingMode to " + newMode);
		}
		
		PropVariantClear(&pv);

		return hr;
	}
	HRESULT GetMicArrayProcessingMode (MIC_ARRAY_MODE *outMode)
	{
		PROPVARIANT pv;		
		HRESULT hr = m_pPropertyStore->GetValue(MicArrayProcessingModeKey, &pv);
		if(FAILED(hr))
		{
			AppendToErrorMessage("Failed to get MicArrayProcessingMode");
			return hr;
		}

		*outMode = (MIC_ARRAY_MODE)pv.intVal;
		return hr;
	}

	// --------------------  Error Messages  --------------------------

	const std::string iErrorMsgHeader = "ERROR in ZigNativeAudioSourceDll\n";
	std::string m_errorMessage = iErrorMsgHeader;
	void AppendToErrorMessage(std::string message)
	{
		m_errorMessage.append(message + "\n");
	}
	EXPORT_API const char* AS_GetLastRecordedErrorMessage()
	{
		static std::string msg;
		msg = m_errorMessage;

		ClearLastRecordedErrorMessage();
		return msg.c_str();
	}
	void ClearLastRecordedErrorMessage()
	{
		m_errorMessage = iErrorMsgHeader;
	}
}
