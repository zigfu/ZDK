#pragma once

// For speech APIs
// NOTE: To ensure that application compiles and links against correct SAPI versions (from Microsoft Speech
//       SDK), VC++ include and library paths should be configured to list appropriate paths within Microsoft
//       Speech SDK installation directory before listing the default system include and library directories,
//       which might contain a version of SAPI that is not appropriate for use together with Kinect sensor.
#include <sapi.h>
#include <sphelper.h>
#include <string>

#include "KinectAudioSpecs.h"
#include "KinectAudioStream.h"
#include "SimpleMessageRecorder.h"


class ZigNativeSpeechRecognizer
{
public:

	// ----- Properties -----

	HRESULT SetLanguage(const char* code);

	// Summary:
	//		The SpeechRecognition Engine can adapt to it's environment and speaker over time, for more accurate results.
	//		 For long recognition sessions (a few hours or more), it may be beneficial to turn off adaptation of the acoustic model.
    //       This will prevent recognition accuracy from degrading over time.
	HRESULT SetAdaptaionEnabled(bool doEnable);
	HRESULT GetAdaptaionEnabled(bool& doEnable);

	inline bool GetSpeechRecognitionHasStarted() { return m_speechRecognitionHasStarted; }


	// ----- Init/Destroy -----

	ZigNativeSpeechRecognizer();
	~ZigNativeSpeechRecognizer();

	HRESULT Initialize(IMediaObject* pDMO, WAVEFORMATEX wfxOut);


	// ----- SpeechGrammar -----

	HRESULT CreateGrammarFromXml(const char* xmlFilePath, bool dynamic, DWORD& out_GrammarID);
	HRESULT RemoveGrammar (DWORD grammarID);
	HRESULT ActivateSpeechGrammar(DWORD grammarID);
	HRESULT DeactivateSpeechGrammar(DWORD grammarID);


	// ----- ProcessSpeech -----

	// Summary:
	//		Starts recognizing speech asynchronously.
	HRESULT StartSpeechRecognition();
	HRESULT StopSpeechRecognition();

	// Summary:
	//		Process recently triggered speech recognition events.
	// Returns:
	//		On failure: -1
	//		On Success: Number of phrases recognized
	//
	HRESULT ProcessSpeech();

	// Summary:
	//		These methods should be called immediately after any calls made to ProcessSpeech()
	//		 in order to retrieve the processing results.
	const char* GetLastRecognizedSpeech();
	float GetLastRecognizedSpeechConfidence();


	// ----- Error Reporting -----

	const char* GetLastRecordedErrorMessage(bool doClearMessage = false);


private:
	
	KinectAudioStream*		m_pKinectAudioStream;	// Audio stream captured from Kinect.
    ISpStream*				m_pSpeechStream;		// Stream given to speech recognition engine
    ISpRecognizer*          m_pSpeechRecognizer;
    ISpRecoContext*         m_pSpeechContext;

	SimpleMessageRecorder*	debug;

	bool					m_LanguagePackHasBeenSet;
	bool					m_speechRecognitionHasStarted;


	HRESULT VerifyCorrectSpeechAPI();
	HRESULT InitAudioStream(IMediaObject* pDMO, WAVEFORMATEX wfxOut);
	HRESULT CreateSpeechRecognizer();
	HRESULT CreateSpeechContext();

	void Destroy();


	HRESULT CreateGrammar (DWORD& out_GrammarID);
	// Note:
	//		To modify the rules of the grammar after it has been loaded, 
	//		 specify SPLO_DYNAMIC for the Options parameter, otherwise specify the SPLO_STATIC flag.
	HRESULT LoadGrammarFromXml(DWORD grammarID, const char* xmlFilePath, bool dynamic);
	bool TryGetSpeechGrammarWithID(DWORD grammarID, ISpRecoGrammar** out_Grammar);
	HRESULT SetSpeechGrammarActiveState(DWORD grammarID, bool newActiveState);
	

	bool TryGetSemanticTagFromRecoResult(ISpRecoResult* result, const SPPHRASEPROPERTY** out_ppSemanticTag);

	string m_lastRecognizedSpeech;
	float m_lastRecognizedSpeechConfidence;
	void OnSpeechRecognized(const SPPHRASEPROPERTY* pSemanticTag);
};

