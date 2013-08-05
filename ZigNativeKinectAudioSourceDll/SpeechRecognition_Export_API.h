#pragma once


extern "C" 
{

	// -------------------  Properties  --------------------------

	HRESULT EXPORT_API SR_SetLanguage(DWORD stringLength, BYTE languageCode[]);
	HRESULT EXPORT_API SR_SetAdaptaionEnabled(bool doEnable);
	HRESULT EXPORT_API SR_GetAdaptaionEnabled(bool& doEnable);

	// -------------------  Init and Destroy  ---------------------------

	HRESULT EXPORT_API SR_InitializeSpeechRecognizer();
	void EXPORT_API SR_Destroy();

	// ---------------------  ProcessSpeech  -----------------------------

	HRESULT EXPORT_API SR_StartSpeechRecognition();
	HRESULT EXPORT_API SR_StopSpeechRecognition();
	HRESULT EXPORT_API SR_ProcessSpeech();
	EXPORT_API const char* SR_GetLastRecognizedSpeech();
	float EXPORT_API SR_GetLastRecognizedSpeechConfidence();

	// ---------------------  SpeechGrammars  -----------------------------

	HRESULT EXPORT_API SR_CreateGrammarFromXml(DWORD stringLength, BYTE grammarXmlFilePath[], bool dynamic, DWORD& out_GrammarID);
	HRESULT EXPORT_API SR_RemoveGrammar(DWORD grammarID);
	HRESULT EXPORT_API SR_ActivateSpeechGrammar(DWORD grammarID);
	HRESULT EXPORT_API SR_DeactivateSpeechGrammar(DWORD grammarID);


	EXPORT_API const char* SR_GetLastRecordedErrorMessage(bool doClearMessage);

}
