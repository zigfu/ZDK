#include "stdafx.h"
#include "ZigNativeSpeechRecognizer.h"

#include "StringHelper.h"
#include "AutoCollection.h"

#include <vector>

#define INITGUID
#include <guiddef.h>


using namespace std;


// This is the class ID we expect for the Microsoft Speech recognizer.
//  Other values indicate that we're using a version of sapi.h that is incompatible with this code.
DEFINE_GUID(CLSID_ExpectedRecognizer, 0x495648e7, 0xf7ab, 0x4267, 0x8e, 0x0f, 0xca, 0xfb, 0x7a, 0x33, 0xc1, 0x60);


string iErrorMsgHeader = "ERROR in ZigNativeSpeechRecognizer\n";

const char* iDefaultLanguageCode = "409";	// (en-US)
const char* iDefaultAttributesStr = "Kinect=True";		
const char* iCustomAttributesStr = "Language=%s;Kinect=True";


// ---------------  Public Properties  ----------------------

HRESULT ZigNativeSpeechRecognizer::SetLanguage(const char* languageCode)
{
	USES_CONVERSION;	// necessary for A2W() string conversion macro

	HRESULT hr = S_OK;

	// Use the languageCode to create the RequiredAttributes string.
	const char* reqAtrb;
	string a = languageCode;
	string b = iDefaultLanguageCode;
	if(a == b) { reqAtrb = iDefaultAttributesStr; }
	else	   { char temp[200]; sprintf_s(temp, iCustomAttributesStr, languageCode); reqAtrb = temp; }

	ISpObjectToken *pEngineToken = NULL;
	hr = SpFindBestToken(SPCAT_RECOGNIZERS, A2W(reqAtrb), NULL, &pEngineToken);
	if (FAILED(hr))
	{ 
		debug->AppendToMessage("Failed to FindBestToken"); 
		if(hr == SPERR_NOT_FOUND) { debug->AppendToMessage(" - No items match the given attributes: %s", reqAtrb); }
	}
	else
	{
		// SetRecognizer(): This method cannot be called when the current SR engine is already running and 
		//  processing audio. In addition, when using the shared recognizer, it cannot be called if another
		//  application is also using the shared recognizer.
		hr = m_pSpeechRecognizer->SetRecognizer(pEngineToken);
		if (FAILED(hr)) 
		{
			debug->AppendToMessage("Failed to set the Recognizer Token.");
			if(hr == SPERR_ENGINE_BUSY) { debug->AppendToMessage(" - Recognition is currently running or other applications are connected to the shared recognizer.  Or SetLanguage has already been called with a different language code specified."); }
			else if(hr == E_INVALIDARG)	{ debug->AppendToMessage(" - pEngineToken is invalid or bad."); }
		}
		else
		{
			hr = CreateSpeechContext();
			if (FAILED(hr)) { debug->AppendToMessage("Failed to CreateSpeechContext."); SafeRelease(pEngineToken); }
		}
	}

	m_LanguagePackHasBeenSet = SUCCEEDED(hr);

	SafeRelease(pEngineToken);
	return hr;
}

LPCWSTR ADAPTATION_PROP_NAME = L"AdaptationOn";
HRESULT ZigNativeSpeechRecognizer::SetAdaptaionEnabled(bool doEnable)
{
	LONG value = doEnable ? 1 : 0;
	HRESULT hr = m_pSpeechRecognizer->SetPropertyNum(ADAPTATION_PROP_NAME, value);
	if(FAILED(hr)) { debug->AppendToMessage("Error setting AdaptaionEnabled to %s", doEnable); return hr; }
	return hr;
}
HRESULT ZigNativeSpeechRecognizer::GetAdaptaionEnabled(bool& isEnabled)
{
	LONG* value = new LONG;
	HRESULT hr = m_pSpeechRecognizer->GetPropertyNum(ADAPTATION_PROP_NAME, value);
	if(FAILED(hr)) { debug->AppendToMessage("Error getting AdaptaionEnabled"); return hr; }

	isEnabled = (*value != 0);
	return hr;
}

// ---------------  Init and Destroy  ----------------------

ZigNativeSpeechRecognizer::ZigNativeSpeechRecognizer() :
    m_pKinectAudioStream(NULL),
    m_pSpeechStream(NULL),
    m_pSpeechRecognizer(NULL),
    m_pSpeechContext(NULL),
	m_speechRecognitionHasStarted(false),
	debug (new SimpleMessageRecorder(iErrorMsgHeader))
{}

ZigNativeSpeechRecognizer::~ZigNativeSpeechRecognizer(void)
{
	Destroy();
}

void ZigNativeSpeechRecognizer::Destroy()
{
	StopSpeechRecognition();

	WORD numGrammars = AutoCollection<ISpRecoGrammar>::GetCollectionSize();
	for (DWORD i = 0; i < numGrammars; i++) { RemoveGrammar(i); }

	SafeRelease(m_pSpeechContext);
	SafeRelease(m_pKinectAudioStream);
	SafeRelease(m_pSpeechStream);
	SafeRelease(m_pSpeechRecognizer);
}


HRESULT ZigNativeSpeechRecognizer::Initialize(IMediaObject* pDMO, WAVEFORMATEX waveFormat) 
{
	HRESULT hr = S_OK;

	hr = VerifyCorrectSpeechAPI();
	if (FAILED(hr)) { debug->AppendToMessage("Failed to Verify Speech API"); return hr; }

	hr = InitAudioStream(pDMO, waveFormat);
	if (FAILED(hr)) { debug->AppendToMessage("Failed to Init the KinectAudioStream"); return hr; }

	hr = CreateSpeechRecognizer();
	if (FAILED(hr)) { debug->AppendToMessage("Failed to CreateSpeechRecognizer."); return hr; }

	return hr;
}

HRESULT ZigNativeSpeechRecognizer::VerifyCorrectSpeechAPI()
{
	if (CLSID_ExpectedRecognizer != CLSID_SpInprocRecognizer)
    {
        debug->AppendToMessage("ZigNativeKinectAudioSourceDll was compiled against an incompatible version of sapi.h.  "
			"To ensure that application compiles and links against correct SAPI versions (from Microsoft Speech SDK), "
							"VC++ include and library paths should be configured to list appropriate paths within Microsoft "
							"Speech SDK installation directory before listing the default system include and library directories, "
							"which might contain a version of SAPI that is not appropriate for use together with Kinect sensor.");
        return -1;
    }

	return S_OK;
}

HRESULT ZigNativeSpeechRecognizer::InitAudioStream(IMediaObject* pDMO, WAVEFORMATEX waveFormat) 
{
	IStream* pStream = NULL;
    m_pKinectAudioStream = new KinectAudioStream(pDMO);

    HRESULT hr = m_pKinectAudioStream->QueryInterface(IID_IStream, (void**)&pStream);
	if(FAILED(hr)) { debug->AppendToMessage("KinectAudioStream failed to QueryInterface into pStream"); }
	else {
        hr = CoCreateInstance(CLSID_SpStream, NULL, CLSCTX_INPROC_SERVER, __uuidof(ISpStream), (void**)&m_pSpeechStream);
		if(FAILED(hr)) { debug->AppendToMessage("Failed to CoCreateInstance with m_pSpeechStream"); }
		else {
            hr = m_pSpeechStream->SetBaseStream(pStream, SPDFID_WaveFormatEx, &waveFormat);
			if(FAILED(hr)) { debug->AppendToMessage("SpeechStream failed to SetBaseStream"); }
        }
    }

	SafeRelease(pStream);
	return hr;
}

HRESULT ZigNativeSpeechRecognizer::CreateSpeechRecognizer()
{
	HRESULT hr = S_OK;

	hr = CoCreateInstance(CLSID_SpInprocRecognizer, NULL, CLSCTX_INPROC_SERVER, __uuidof(ISpRecognizer), (void**)&m_pSpeechRecognizer);
	if (FAILED(hr)) { debug->AppendToMessage("Failed to CoCreateInstance"); return hr; }

	hr = m_pSpeechRecognizer->SetInput(m_pSpeechStream, FALSE);
	if (FAILED(hr)) { debug->AppendToMessage("Failed to set the input stream for speech"); return hr; }

	return hr;
}

HRESULT ZigNativeSpeechRecognizer::CreateSpeechContext()
{
	HRESULT hr = m_pSpeechRecognizer->CreateRecoContext(&m_pSpeechContext);
	if (FAILED(hr)) { debug->AppendToMessage("SpeechRecognizer failed to CreateRecoContext."); return hr; }
	return hr;
}


// ---------------  SpeechRecognitionGrammar  ----------------------

HRESULT ZigNativeSpeechRecognizer::CreateGrammarFromXml(const char* xmlFilePath, bool dynamic, DWORD& out_GrammarID)
{
	HRESULT hr = S_OK;
	out_GrammarID = 0;

	hr = CreateGrammar(out_GrammarID);
	if(FAILED(hr)) { return hr; }
	
	hr = LoadGrammarFromXml(out_GrammarID, xmlFilePath, dynamic);
	if(FAILED(hr)) { return hr; }

	return hr;
}

HRESULT ZigNativeSpeechRecognizer::CreateGrammar (DWORD& out_GrammarID)
{
	HRESULT hr = S_OK;
	out_GrammarID = 0;

	ISpRecoGrammar* newGrammar;
	hr = m_pSpeechContext->CreateGrammar(0, &newGrammar);
	if (FAILED(hr)) { debug->AppendToMessage("SpeechContext failed to CreateGrammar."); return hr; }

	out_GrammarID = AutoCollection<ISpRecoGrammar>::Collect(*newGrammar);
	return hr;
}

HRESULT ZigNativeSpeechRecognizer::LoadGrammarFromXml(DWORD grammarID, const char* xmlFilePath, bool dynamic)
{
	USES_CONVERSION;	// necessary for A2W() string conversion macro

	HRESULT hr = S_OK;

	ISpRecoGrammar* spGrammar;
	if(!TryGetSpeechGrammarWithID(grammarID, &spGrammar)) { return -1; }

	SPLOADOPTIONS spLoadOptions = dynamic ? SPLO_DYNAMIC : SPLO_STATIC;
	hr = spGrammar->LoadCmdFromFile(A2W(xmlFilePath), spLoadOptions);	
	if (FAILED(hr)) { debug->AppendToMessage("Failed to Load SpeechGrammar from file:  %s", xmlFilePath); return hr; }

	return hr;
}

HRESULT ZigNativeSpeechRecognizer::RemoveGrammar (DWORD grammarID)
{
	HRESULT hr = DeactivateSpeechGrammar(grammarID);
	AutoCollection<ISpRecoGrammar>::RemoveObject(grammarID);
	return hr;
}

HRESULT ZigNativeSpeechRecognizer::ActivateSpeechGrammar(DWORD grammarID)
{
	return SetSpeechGrammarActiveState(grammarID, true);
}
HRESULT ZigNativeSpeechRecognizer::DeactivateSpeechGrammar(DWORD grammarID)
{
	return SetSpeechGrammarActiveState(grammarID, false);
}
HRESULT ZigNativeSpeechRecognizer::SetSpeechGrammarActiveState(DWORD grammarID, bool newActiveState)
{
	HRESULT hr = S_OK;

	ISpRecoGrammar* spGrammar;
	if(!TryGetSpeechGrammarWithID(grammarID, &spGrammar)) { return -1; }

	SPRULESTATE newRuleState = newActiveState ? SPRS_ACTIVE : SPRS_INACTIVE;
	hr = spGrammar->SetRuleState(NULL, NULL, newRuleState);
	if (FAILED(hr)) { debug->AppendToMessage("SpeechGrammar failed to SetRuleState to %s.", newActiveState); return hr; }

	return hr;
}

bool ZigNativeSpeechRecognizer::TryGetSpeechGrammarWithID(DWORD grammarID, ISpRecoGrammar** out_Grammar)
{
	bool success = AutoCollection<ISpRecoGrammar>::TryGetObject(grammarID, out_Grammar);
	if(!success) { debug->AppendToMessage("No SpeechGrammar exists with ID of %i.", (int)grammarID); return false; }
	return success;
}


// ---------------  ProcessSpeech  ----------------------

HRESULT ZigNativeSpeechRecognizer::StartSpeechRecognition()
{
	HRESULT hr = S_OK;

	if(m_speechRecognitionHasStarted) { return S_OK; }

	if(!m_LanguagePackHasBeenSet)
	{ 
		hr = SetLanguage(iDefaultLanguageCode);
		if (FAILED(hr)) { debug->AppendToMessage("Failed to SetLanguage to the default language."); return hr; }
	}
	
	// Begin capturing audio from the Kinect Device
	hr = m_pKinectAudioStream->StartCapture();
	if (FAILED(hr)) { debug->AppendToMessage("Failed to start capturing audio from the KinectAudioStream."); return hr; }
	
	// Set RecognitionState to SPRST_ACTIVE. (SpeechEngine will only read audio when there are active Grammars) 
	hr = m_pSpeechRecognizer->SetRecoState(SPRST_ACTIVE);
	if (FAILED(hr)) { debug->AppendToMessage("Failed to set RecognitionState to SPRST_ACTIVE."); return hr; }
	
	// Only receive RECOGNITION events
	hr = m_pSpeechContext->SetInterest(SPFEI(SPEI_RECOGNITION), SPFEI(SPEI_RECOGNITION));
	if (FAILED(hr)) { debug->AppendToMessage("SpeechContext failed to SetInterest to SPEI_RECOGNITION."); return hr; }

	// Ensure that engine is recognizing speech (not paused)
	hr = m_pSpeechContext->Resume(0);
	if (FAILED(hr)) { debug->AppendToMessage("SpeechContext failed to ensure an unpaused state."); return hr; }
	
	m_speechRecognitionHasStarted = true;
	return hr;
}

HRESULT ZigNativeSpeechRecognizer::StopSpeechRecognition()
{
	if(!m_speechRecognitionHasStarted) { return S_OK; }

	HRESULT hr = S_OK;
	m_speechRecognitionHasStarted = false;

	hr = m_pSpeechContext->Pause(0);
	if (FAILED(hr)) { debug->AppendToMessage("SpeechContext failed to Pause."); return hr; }

	hr = m_pSpeechRecognizer->SetRecoState(SPRST_INACTIVE_WITH_PURGE);
	if (FAILED(hr)) { debug->AppendToMessage("SpeechRecognizer failed to SetRecoState to SPRST_INACTIVE_WITH_PURGE."); return hr; }

	hr = m_pKinectAudioStream->StopCapture();
	if (FAILED(hr)) { debug->AppendToMessage("KinectAudioStream failed to StopCapture."); return hr; }

	return hr;
}


HRESULT ZigNativeSpeechRecognizer::ProcessSpeech()
{
	if(!m_speechRecognitionHasStarted) { debug->AppendToMessage("Failed to ProcessSpeech because SpeechRecognition has not started."); return -1; }

	int numPhrasesRecognized = 0;

	SPEVENT curEvent;
	ULONG fetched = 0;
	do
	{
		m_pSpeechContext->GetEvents(1, &curEvent, &fetched);
		if (fetched == 0) { break; }
		if (curEvent.eEventId != SPEI_RECOGNITION) { continue; }
		if (curEvent.elParamType != SPET_LPARAM_IS_OBJECT) { continue; }
		
		ISpRecoResult* result = reinterpret_cast<ISpRecoResult*>(curEvent.lParam);

		const SPPHRASEPROPERTY* pSemanticTag;
		if(TryGetSemanticTagFromRecoResult(result, &pSemanticTag))
		{
			OnSpeechRecognized(pSemanticTag);
			numPhrasesRecognized++;
		}

	} 
	while (numPhrasesRecognized == 0);

	return numPhrasesRecognized;
}

bool ZigNativeSpeechRecognizer::TryGetSemanticTagFromRecoResult(ISpRecoResult* result, const SPPHRASEPROPERTY** out_ppSemanticTag)
{
	if(out_ppSemanticTag == NULL) { return false; }

	HRESULT hr = S_OK;

	SPPHRASE* pPhrase = NULL;         
	hr = result->GetPhrase(&pPhrase);
	if (FAILED(hr)) { return false; }
		
	const SPPHRASEPROPERTY* pSemanticTag = NULL;
	const SPPHRASEPROPERTY* phraseProperties = pPhrase->pProperties;
	if (phraseProperties != NULL)
	{
		 pSemanticTag = phraseProperties->pFirstChild;
	}
	::CoTaskMemFree(pPhrase);
	if (pSemanticTag == NULL) { return false; }

	*out_ppSemanticTag = pSemanticTag;
	return true;
}

void ZigNativeSpeechRecognizer::OnSpeechRecognized(const SPPHRASEPROPERTY* pSemanticTag)
{
	LPCWSTR recognizedSpeech = pSemanticTag->pszValue;
	float confidence = pSemanticTag->SREngineConfidence;

	m_lastRecognizedSpeech = StringHelper::LPCWSTR_to_stdString(recognizedSpeech);
	m_lastRecognizedSpeechConfidence = confidence;
}

const char* ZigNativeSpeechRecognizer::GetLastRecognizedSpeech()
{
	static string msg;
	msg = m_lastRecognizedSpeech;
	return msg.c_str();
}
float ZigNativeSpeechRecognizer::GetLastRecognizedSpeechConfidence()
{
	return m_lastRecognizedSpeechConfidence;
}


// --------------------  Error Reporting  --------------------------

const char* ZigNativeSpeechRecognizer::GetLastRecordedErrorMessage(bool doClearMessage)
{
	return debug->GetLastRecordedMessage(doClearMessage);
}
