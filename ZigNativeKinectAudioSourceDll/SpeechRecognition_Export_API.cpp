#pragma once

#include "stdafx.h"
#include "SpeechRecognition_Export_API.h"

#include "ZigNativeKinectAudioSourceDll.h"
#include "ZigNativeSpeechRecognizer.h"
#include "SimpleMessageRecorder.h"

#include "StringHelper.h"


// -------------------  SpeechRecognizer  --------------------------

ZigNativeSpeechRecognizer* speechRecognizer;


// -------------------  Error Reporting  --------------------------

const string SR_ErrorMsgHeader = "ERROR in SpeechRecognition_Export_API\n";
SimpleMessageRecorder*	SR_Debug;

bool CheckAndLogErrorIfSpeechRecognizerIsNull(const char* functionName)
{
	if(functionName == NULL) { functionName = "_"; }
	bool isNull = (speechRecognizer == NULL);
	if(isNull) { SR_Debug->AppendToMessage("Error in %s(): SpeechRecognizer reference is NULL.", functionName); }
	return isNull;
}


extern "C" 
{

	// -------------------  Properties  --------------------------

	HRESULT EXPORT_API SR_SetLanguage(DWORD stringLength, BYTE languageCode[])
	{
		if(CheckAndLogErrorIfSpeechRecognizerIsNull("SR_SetLanguage")) { return -1; } 

		char* languageCode_cstr;
		bool success = StringHelper::ByteArrayToCString(languageCode, stringLength, &languageCode_cstr);
		if(!success) { SR_Debug->AppendToMessage("Failed to convert languageCode from ByteArray to CString."); return -1; }

		HRESULT hr = speechRecognizer->SetLanguage(languageCode_cstr);
		if (FAILED(hr))
		{
			SR_Debug->AppendToMessage(speechRecognizer->GetLastRecordedErrorMessage());
			SR_Debug->AppendToMessage("Error occurred while trying to SetLanguage.");
			return hr;
		}
		return hr;
	}

	HRESULT EXPORT_API SR_SetAdaptaionEnabled(bool doEnable)
	{
		if(CheckAndLogErrorIfSpeechRecognizerIsNull("SR_SetAdaptaionEnabled")) { return -1; } 
		return speechRecognizer->SetAdaptaionEnabled(doEnable);
	}
	HRESULT EXPORT_API SR_GetAdaptaionEnabled(bool& doEnable)
	{
		if(CheckAndLogErrorIfSpeechRecognizerIsNull("SR_GetAdaptaionEnabled")) { return -1; } 
		return speechRecognizer->GetAdaptaionEnabled(doEnable);
	}


	// -------------------  Init and Destroy  ---------------------------

	HRESULT EXPORT_API SR_InitializeSpeechRecognizer()
	{
		SR_Debug = new SimpleMessageRecorder(SR_ErrorMsgHeader);

		speechRecognizer = new ZigNativeSpeechRecognizer();
		IMediaObject* dmo;
		if(!TryGetDmo(&dmo)) { SR_Debug->AppendToMessage("Failed to get reference to the DMO."); return -1; }

		HRESULT hr = speechRecognizer->Initialize(dmo, AS_GetKinectWaveFormat());
		if (FAILED(hr)) { SR_Debug->AppendToMessage(speechRecognizer->GetLastRecordedErrorMessage()); return hr; }

		return hr;
	}

	void EXPORT_API SR_Destroy()
	{
		if(NULL != speechRecognizer)
		{
			delete speechRecognizer;
			speechRecognizer = NULL;
		}
	}

	// ---------------------  ProcessSpeech  -----------------------------

	HRESULT EXPORT_API SR_StartSpeechRecognition()
	{
		HRESULT hr = S_OK;

		if(CheckAndLogErrorIfSpeechRecognizerIsNull("SR_StartSpeechRecognition")) { return -1; } 

		hr = speechRecognizer->StartSpeechRecognition();
		if (FAILED(hr))
		{
			SR_Debug->AppendToMessage(speechRecognizer->GetLastRecordedErrorMessage());
			SR_Debug->AppendToMessage("Failed to StartSpeechRecognition.");
			return hr;
		}

		return hr;
	}

	HRESULT EXPORT_API SR_StopSpeechRecognition()
	{
		if(CheckAndLogErrorIfSpeechRecognizerIsNull("SR_StopSpeechRecognition")) { return -1; } 

		HRESULT hr = speechRecognizer->StopSpeechRecognition();
		if (FAILED(hr))
		{
			SR_Debug->AppendToMessage(speechRecognizer->GetLastRecordedErrorMessage());
			SR_Debug->AppendToMessage("Error occurred while trying to StopSpeechRecognition.");
			return hr;
		}

		return hr;
	}

	HRESULT EXPORT_API SR_ProcessSpeech()
	{
		if(CheckAndLogErrorIfSpeechRecognizerIsNull("SR_ProcessSpeech")) { return -1; } 

		HRESULT hr = speechRecognizer->ProcessSpeech();
		if(FAILED(hr))
		{
			SR_Debug->AppendToMessage(speechRecognizer->GetLastRecordedErrorMessage());
			SR_Debug->AppendToMessage("SpeechRecognizer failed to ProcessSpeech.");
		}

		return hr;
	}

	EXPORT_API const char* SR_GetLastRecognizedSpeech()
	{
		if(CheckAndLogErrorIfSpeechRecognizerIsNull("SR_GetLastRecognizedSpeech")) { return NULL; } 

		return speechRecognizer->GetLastRecognizedSpeech();
	}

	float EXPORT_API SR_GetLastRecognizedSpeechConfidence()
	{
		if(CheckAndLogErrorIfSpeechRecognizerIsNull("SR_GetLastRecognizedSpeechConfidence")) { return -1; } 

		return speechRecognizer->GetLastRecognizedSpeechConfidence();
	}


	// ---------------------  SpeechGrammars  -----------------------------

	HRESULT EXPORT_API SR_CreateGrammarFromXml(DWORD stringLength, BYTE grammarXmlFilePath[], bool dynamic, DWORD& out_GrammarID)
	{
		if(CheckAndLogErrorIfSpeechRecognizerIsNull("SR_CreateGrammarFromXml")) { return -1; } 

		char* filePath_cstr;
		bool success = StringHelper::ByteArrayToCString(grammarXmlFilePath, stringLength, &filePath_cstr);
		if(!success) 
		{ 
			SR_Debug->AppendToMessage("Failed to convert grammarXmlFilePath from ByteArray to CString."); 
			return -1; 
		}

		HRESULT hr = speechRecognizer->CreateGrammarFromXml(filePath_cstr, dynamic, out_GrammarID);
		if (FAILED(hr))
		{
			SR_Debug->AppendToMessage(speechRecognizer->GetLastRecordedErrorMessage());
			SR_Debug->AppendToMessage("Error occurred while trying to CreateGrammarFromXml.");
			return hr;
		}

		return hr;
	}

	HRESULT EXPORT_API SR_RemoveGrammar(DWORD grammarID)
	{
		if(CheckAndLogErrorIfSpeechRecognizerIsNull("SR_RemoveGrammar")) { return -1; } 

		HRESULT hr = speechRecognizer->RemoveGrammar(grammarID);
		if (FAILED(hr))
		{
			SR_Debug->AppendToMessage(speechRecognizer->GetLastRecordedErrorMessage());
			SR_Debug->AppendToMessage("Error occurred while trying to Remove Grammar with ID: %i", (int)grammarID);
			return hr;
		}

		return hr;
	}

	HRESULT EXPORT_API SR_ActivateSpeechGrammar(DWORD grammarID)
	{
		if(CheckAndLogErrorIfSpeechRecognizerIsNull("SR_ActivateSpeechGrammar")) { return -1; } 

		HRESULT hr = speechRecognizer->ActivateSpeechGrammar(grammarID);
		if (FAILED(hr))
		{
			SR_Debug->AppendToMessage(speechRecognizer->GetLastRecordedErrorMessage());
			SR_Debug->AppendToMessage("Error occurred while trying to Activate SpeechGrammar with ID: %i", (int)grammarID);
			return hr;
		}

		return hr;
	}

	HRESULT EXPORT_API SR_DeactivateSpeechGrammar(DWORD grammarID)
	{
		if(CheckAndLogErrorIfSpeechRecognizerIsNull("SR_DeactivateSpeechGrammar")) { return -1; } 

		HRESULT hr = speechRecognizer->DeactivateSpeechGrammar(grammarID);
		if (FAILED(hr))
		{
			SR_Debug->AppendToMessage(speechRecognizer->GetLastRecordedErrorMessage());
			SR_Debug->AppendToMessage("Error occurred while trying to Deactivate SpeechGrammar with ID: %i", (int)grammarID);
			return hr;
		}

		return hr;
	}


	EXPORT_API const char* SR_GetLastRecordedErrorMessage(bool doClearMessage)
	{
		return SR_Debug->GetLastRecordedMessage(doClearMessage);
	}
}
