using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Xml;
using System.Text;
using Zigfu.KinectAudio;
using Zigfu.Utility;


namespace Zigfu.Speech
{
    public sealed partial class ZigKinectSpeechRecognizer : Singleton<ZigKinectSpeechRecognizer>, IZigSpeechGrammarDelegate
    {
        const String ClassName = "ZigKinectSpeechRecognizer";
        const UInt32 ProcessSpeechInterval_MS = 16;

        const float DefaultConfidenceThreshold = 0.3f;
        const bool DefaultAdaptationEnabled = true;
    

        static public bool verbose = true;


        public float ConfidenceThreshold { get; set; }

        bool _adaptationEnabled = true;
        public bool AdaptationEnabled {
            get {
                return _adaptationEnabled;
            }
            set {
                if (verbose) { print(ClassName + " :: Set AdaptationEnabled to " + value); }

                Int32 hr = SR_SetAdaptaionEnabled(value);
                EvaluateHResult(hr);

                _adaptationEnabled = value;
            }
        }


        LanguagePack.DialectEnum _dialect = LanguagePack.DefaultDialect;
        public LanguagePack.DialectEnum Dialect { get { return _dialect; } }

        public bool HasBeenInitialized          { get; private set; }
        public bool SpeechRecognitionIsStarting { get { return (_startSR_Job != null); } }
        public bool SpeechRecognitionIsStopping { get { return (_stopSR_Job  != null);  } }
        public bool SpeechRecognitionHasStarted { get; private set; }


        List<ZigSpeechGrammar> _speechGrammars = new List<ZigSpeechGrammar>();

        ZigKinectAudioSource _kinectAudioSource;
        public ZigKinectAudioSource KinectAudioSource {
            get {
                if (!_kinectAudioSource) { _kinectAudioSource = ZigKinectAudioSource.Instance; }
                return _kinectAudioSource;
            }
        }


        #region Custom Events

        // --- OnStartedListening ---

        public event EventHandler StartedListening;

        void OnStartedListening()
        {
            if (verbose) { print(ClassName + " :: OnStartedListening"); }

            EventHandler handler = StartedListening;
            if (handler == null) { return; }
            handler(this, new EventArgs());
        }


        // --- OnStoppedListening ---

        public event EventHandler StoppedListening;

        void OnStoppedListening()
        {
            if (verbose) { print(ClassName + " :: OnStoppedListening"); }

            EventHandler handler = StoppedListening;
            if (handler == null) { return; }
            handler(this, new EventArgs());
        }


        // --- OnSpeechRecognized ---

        public sealed class SpeechRecognized_EventArgs : EventArgs
        {
            public String SemanticTag { get; private set; }
            public float Confidence { get; private set; }

            public SpeechRecognized_EventArgs(String semanticTag, float confidence)
            {
                SemanticTag = semanticTag;
                Confidence = confidence;
            }
        }

        public event EventHandler<SpeechRecognized_EventArgs> SpeechRecognized;

        void OnSpeechRecognized(String semanticTag, float confidence)
        {
            if (verbose) { print(ClassName + " :: OnSpeechRecognized:  " + "\"" + semanticTag + "\"" + "  confidence: " + confidence); }

            EventHandler<SpeechRecognized_EventArgs> handler = SpeechRecognized;
            if (handler == null) { return; }
            handler(this, new SpeechRecognized_EventArgs(semanticTag, confidence));
        }

        #endregion


        #region Init and Destroy

        void Awake()
        {
            if (verbose) { print(ClassName + " :: Awake"); }

            HasBeenInitialized = false;
            SpeechRecognitionHasStarted = false;
        }

        void Start()
        {
            if (verbose) { print(ClassName + " :: Start"); }

            // Since the ZigKinectAudioSource is soley responsible for intializing Kinect audio,
            //  ZigKinectSpeechRecognizers cannot Initialize until after ZigKinectAudioSource does.
            if (KinectAudioSource.HasBeenInitialized) 
                { Initialize(); }
            else 
                { KinectAudioSource.FinishedInitializing += AudioSourceFinishedInitializing_Handler; }
        }
    
        void Initialize()
        {
            if (HasBeenInitialized) { return; }
            if (verbose) { print(ClassName + " :: Initialize"); }

            Int32 hr = SR_InitializeSpeechRecognizer();
            if (!EvaluateHResult(hr)) { return; }

            SubscribeTo_ZigKinectAudioSourceEvents(KinectAudioSource);
            ApplyInitialSettings();

            // The language can only be set once, and it must be set
            //  after SR_InitializeSpeechRecognizer, but before SR_StartSpeechRecognition.
            InitLanguage(_dialect);

            HasBeenInitialized = true;

            // Now that we are Initialized we can Register all our SpeechGrammars
            RegisterGrammars();
        }

        void InitLanguage(LanguagePack.DialectEnum dialect)
        {
            if (verbose) { print(ClassName + " :: InitLanguage: " + dialect); }

            string languageCode = LanguagePack.LanguagePackForDialect(dialect).Code;
            byte[] languageCode_bytes = Encoding.ASCII.GetBytes(languageCode);
            Int32 hr = SR_SetLanguage((UInt32)languageCode_bytes.Length, languageCode_bytes);
            EvaluateHResult(hr);
        }

        void RegisterGrammars()
        {
            if (verbose) { print(ClassName + " :: RegisterGrammars"); }

            foreach (var gr in GetComponentsInChildren<ZigSpeechGrammar>(true))
            {
                gr.Register(this);
            }
        }

        void SubscribeTo_ZigKinectAudioSourceEvents(ZigKinectAudioSource kinectAudioSource)
        {
            // Receive Notifications when the ZigKinectAudioSource stops audio capturing so we can stop ProcessingSpeech
            kinectAudioSource.AudioCapturingForSpeechRecognitionStopped += AudioCapturingForSpeechRecognitionStopped_Handler;
        }

        // Initial Settings
        public float initialConfidenceThreshold = DefaultConfidenceThreshold;
        public bool initialAdaptationEnabled = DefaultAdaptationEnabled;
        public LanguagePack.DialectEnum initialDialect = LanguagePack.DefaultDialect;
 
        public void ApplyInitialSettings()
        {
            if (verbose) { print(ClassName + " :: ApplyInitialSettings"); }

            ConfidenceThreshold = initialConfidenceThreshold;
            AdaptationEnabled = initialAdaptationEnabled;
            _dialect = initialDialect;
        }


        void OnDestroy()
        {
            if (verbose) { print(ClassName + " :: OnDestroy"); }

            Shutdown();
        }

        public void Shutdown()
        {
            if (verbose) { print(ClassName + " :: Shutdown "); }

            if (_kinectAudioSource)
            {
                _kinectAudioSource.AudioCapturingForSpeechRecognitionStopped -= AudioCapturingForSpeechRecognitionStopped_Handler;
                _kinectAudioSource.FinishedInitializing -= AudioSourceFinishedInitializing_Handler;
            }

            AbortStartingSpeechRecognition();
            AbortStoppingSpeechRecognition();
            StopSpeechRecognition();

            for (int i = _speechGrammars.Count - 1; i >= 0; i--)
            {
                _speechGrammars[i].Unregister();
            }

            SR_Destroy();

            HasBeenInitialized = false;
        }

        #endregion


        #region IZigSpeechGrammarDelegate

        bool IZigSpeechGrammarDelegate.RegisterGrammar(ZigSpeechGrammar gr, out UInt32 nativeID)
        {
            if (verbose) { print(ClassName + " :: RegisterGrammar " + gr); }

            nativeID = ZigSpeechGrammar.InvalidNativeID;

            if (!_speechGrammars.Contains(gr))
            {
                gr.transform.parent = transform;
                _speechGrammars.Add(gr);
            }

            if (!HasBeenInitialized) { return false; }

            nativeID = CreateNativeGrammarFromZigGrammar(gr);

            if (gr.WantsActive) { gr.Activate(); }
            else                { gr.Deactivate(); }

            return true;
        }

        UInt32 CreateNativeGrammarFromZigGrammar(ZigSpeechGrammar zigGrammar)
        {
            if (verbose) { print(ClassName + " :: CreateNativeGrammarFromZigGrammar " + zigGrammar); }

            String path = GetTempGrammarXmlStoragePath();

            LanguagePack.DialectEnum tempDialect = zigGrammar.Dialect;
            zigGrammar.Dialect = _dialect;
            {
                zigGrammar.SaveAsXml(path);
            }
            zigGrammar.Dialect = tempDialect;

            return CreateNativeGrammarFromXml(path);
        }

        static UInt32 CreateNativeGrammarFromXml(String xmlFilePath)
        {
            byte[] path = Encoding.ASCII.GetBytes(xmlFilePath);
            UInt32 grammarID;

            // NOTE: Dynamic Grammars are not currently supported.  
            //  Therefore the Native Grammar created by SR_CreateGrammarFromXml(), 
            //  and referenced by the grammarID out argument, cannot be edited.
            bool dynamic = false;

            Int32 hr = SR_CreateGrammarFromXml((UInt32)path.Length, path, dynamic, out grammarID);
            if (!EvaluateHResult(hr)) { throw new Exception("Failed to CreateNativeGrammarFromXml."); }

            return grammarID;
        }

        static String GetTempGrammarXmlStoragePath()
        {
            String fileName = "ZigSpeechGrammar_temp.grxml";
            String directory = Application.temporaryCachePath;
            String filePath = directory + "/" + fileName;
            return filePath;
        }


        bool IZigSpeechGrammarDelegate.UnregisterGrammar(ZigSpeechGrammar gr)
        {
            if (SpeechRecognitionHasStarted)   { return false; }
            if (!_speechGrammars.Contains(gr)) { return false; }
            if (verbose) { print(ClassName + " :: UnregisterGrammar " + gr); }

            Int32 hr = SR_RemoveGrammar(gr.NativeID);
            EvaluateHResult(hr);
        
            _speechGrammars.Remove(gr);
            Destroy(gr.gameObject);

            return true;
        }


        bool IZigSpeechGrammarDelegate.ActivateGrammar(ZigSpeechGrammar gr)
        {
            if (verbose) { print(ClassName + " :: ActivateGrammar " + gr); }
            return SetGrammarActivationStatus(gr, true);
        }
        bool IZigSpeechGrammarDelegate.DeactivateGrammar(ZigSpeechGrammar gr)
        {
            if (verbose) { print(ClassName + " :: DeactivateGrammar " + gr); }
            return SetGrammarActivationStatus(gr, false);
        }
        bool SetGrammarActivationStatus(ZigSpeechGrammar gr, bool doActivate)
        {
            if (!_speechGrammars.Contains(gr)) { return false; }
            if (!SpeechRecognitionHasStarted)  { return false; }

            Int32 hr;
            if (doActivate) { hr = SR_ActivateSpeechGrammar(gr.NativeID); }
            else            { hr = SR_DeactivateSpeechGrammar(gr.NativeID); }
            return ZigKinectSpeechRecognizer.EvaluateHResult(hr);
        }

        #endregion


        #region StartSpeechRecognition

        StartSpeechRecognition_Job _startSR_Job = null;
        public void StartSpeechRecognition()       { StartSpeechRecognition(false); }
        public void StartSpeechRecognition_Async() { StartSpeechRecognition(true); }
        void StartSpeechRecognition(bool async)
        {
            if (SpeechRecognitionIsStopping) { return; }    //AbortStoppingSpeechRecognition();
            if (SpeechRecognitionHasStarted) { return; }
            if (SpeechRecognitionIsStarting) { return; }

            string asyncSuffixStr = async ? "_Async" : String.Empty;
            if (verbose) { print(ClassName + " :: StartSpeechRecognition" + asyncSuffixStr); }

            var audioProcessingIntent = ZigKinectAudioSource.ZigAudioProcessingIntent.SpeechRecognition;
            KinectAudioSource.StartCapturingAudio(audioProcessingIntent);

            if (!HasBeenInitialized) { Initialize(); }

            if (async)
            {
                _startSR_Job = new StartSpeechRecognition_Job();
                _startSR_Job.Start();   // Now don't touch any data in the job class until IsDone is true.
            }
            else
            {
                Int32 hr = SR_StartSpeechRecognition();
                OnSpeechRecognitionFinishedStarting(hr);
            }
        }

        private class StartSpeechRecognition_Job : ThreadedJob
        {
            public Int32 outHResult;

            // Note: DON'T use the Unity API inside ThreadFunction
            protected override void ThreadFunction()
            {
                outHResult = SR_StartSpeechRecognition();
            }

            // This is executed by the Unity main thread when the job is finished
            protected override void OnFinished()
            {
                print("StartSpeechRecognition_Job :: OnFinished");
            }
        }

        void Update_startSR_Job()
        {
            if (_startSR_Job == null) { return; }
            if (_startSR_Job.Update()) 
            {
                Int32 hr = _startSR_Job.outHResult;
                _startSR_Job = null;
                OnSpeechRecognitionFinishedStarting(hr);
            }
        }

        void AbortStartingSpeechRecognition()
        {
            if (!SpeechRecognitionIsStarting) { return; }
            if (verbose) { print(ClassName + " :: AbortStartingSpeechRecognition"); }

            _startSR_Job.Abort();
            _startSR_Job = null;
        }

        void OnSpeechRecognitionFinishedStarting(Int32 hr)
        {
            if (verbose) { print(ClassName + " :: OnSpeechRecognitionFinishedStarting"); }

            if (!EvaluateHResult(hr)) { return; }

            SpeechRecognitionHasStarted = true;

            // If the KinectAudioSource has stopped capturing audio or changed its processing intent
            //  since the time we began starting SpeechRecognition, then we need to stop SpeechRecognition.
            if (!KinectAudioSource.AudioCapturingHasStarted 
                || KinectAudioSource.AudioProcessingIntent != ZigKinectAudioSource.ZigAudioProcessingIntent.SpeechRecognition) 
            {
                StopSpeechRecognition_Async();
                return;
            }

            StartCoroutine(ProcessSpeech_Coroutine_MethodName);
            ApplyActivationStateForGrammars();

            OnStartedListening();
        }

        void ApplyActivationStateForGrammars()
        {
            if (verbose) { print(ClassName + " :: ApplyActivationStateForGrammars"); }

            foreach (var gr in _speechGrammars)
            {
                if (gr.WantsActive) { gr.Activate(); }
                else { gr.Deactivate(); }
            }
        }

        #endregion


        #region StopSpeechRecognition

        StopSpeechRecognition_Job _stopSR_Job = null;
        public void StopSpeechRecognition()       { StopSpeechRecognition(false); }
        public void StopSpeechRecognition_Async() { StopSpeechRecognition(true); }
        void StopSpeechRecognition(bool async)
        {
            if (SpeechRecognitionIsStarting)  { return; }   //AbortStartingSpeechRecognition();
            if (!SpeechRecognitionHasStarted) { return; }
            if (SpeechRecognitionIsStopping)  { return; }

            string asyncSuffixStr = async ? "_Async" : String.Empty;
            if (verbose) { print(ClassName + " :: StopSpeechRecognition" + asyncSuffixStr); }

            SpeechRecognitionHasStarted = false;
            StopCoroutine(ProcessSpeech_Coroutine_MethodName);

            if (async)
            {
                _stopSR_Job = new StopSpeechRecognition_Job();
                _stopSR_Job.Start();   // Now don't touch any data in the job class until IsDone is true.
            }
            else
            {
                Int32 hr = SR_StopSpeechRecognition();
                OnSpeechRecognitionFinishedStopping(hr);
            }
        }

        private class StopSpeechRecognition_Job : ThreadedJob
        {
            public Int32 outHResult;

            // Note: DON'T use the Unity API inside ThreadFunction
            protected override void ThreadFunction()
            {
                outHResult = SR_StopSpeechRecognition();
            }

            // This is executed by the Unity main thread when the job is finished
            protected override void OnFinished()
            {
                print("StopSpeechRecognition_Job :: OnFinished");
            }
        }

        void Update_stopSR_Job()
        {
            if (_stopSR_Job == null) { return; }
            if (_stopSR_Job.Update()) 
            {
                Int32 hr = _stopSR_Job.outHResult;
                _stopSR_Job = null;
                OnSpeechRecognitionFinishedStopping(hr);
            }
        }

        void AbortStoppingSpeechRecognition()
        {
            if (!SpeechRecognitionIsStopping) { return; }
            if (verbose) { print(ClassName + " :: AbortStoppingSpeechRecognition"); }

            _stopSR_Job.Abort();
            _stopSR_Job = null;
        }

        void OnSpeechRecognitionFinishedStopping(Int32 hr)
        {
            if (verbose) { print(ClassName + " :: OnSpeechRecognitionFinishedStopping"); }

            EvaluateHResult(hr);

            OnStoppedListening();
        }

        #endregion


        void Update()
        {
            Update_startSR_Job();
            Update_stopSR_Job();
        }


        #region ProcessSpeech

        // Summary:
        //      Calls ProcessSpeech_Tick every ProcessSpeechInterval_MS milliseconds
        const string ProcessSpeech_Coroutine_MethodName = "ProcessSpeech_Coroutine";
        IEnumerator ProcessSpeech_Coroutine()
        {
            while (true)
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                {
                    if (SpeechRecognitionHasStarted)
                    {
                        ProcessSpeech_Tick();
                    }
                }
                stopWatch.Stop();

                int elapsedTime = stopWatch.Elapsed.Milliseconds;
                float waitTime = 0.001f * Mathf.Max(0, ProcessSpeechInterval_MS - elapsedTime);
                yield return new WaitForSeconds(waitTime);
            }
        }

        void ProcessSpeech_Tick()
        {
            Int32 hr = SR_ProcessSpeech();
            if (!EvaluateHResult(hr)) { return; }

            uint numPhrasesRecognized = (uint)hr;
            if (numPhrasesRecognized > 0)
            {
                if (numPhrasesRecognized > 1)
                {
                    UnityEngine.Debug.LogWarning("SR_ProcessSpeech returned " + numPhrasesRecognized 
                        + " recognized phrases.  However it is currently assumed that only one phrase will be recognized at a time.");
                }

                float confidence = SR_GetLastRecognizedSpeechConfidence();
                if (confidence > ConfidenceThreshold)
	            {
                    IntPtr ptr = SR_GetLastRecognizedSpeech();
                    String semanticTag = Marshal.PtrToStringAnsi(ptr);

                    OnSpeechRecognized(semanticTag, confidence);
	            }
            }
        }

        #endregion


        #region ZigKinectAudioSource Event Handlers

        void AudioSourceFinishedInitializing_Handler(object sender, EventArgs e)
        {
            if (verbose) { print(ClassName + " :: AudioSourceFinishedInitializing_Handler"); }

            if (!HasBeenInitialized)
            {
                Initialize();
            }
        }

        void AudioCapturingForSpeechRecognitionStopped_Handler(object sender, EventArgs e)
        {
            if (verbose) { print(ClassName + " :: AudioCapturingForSpeechRecognitionStopped_Handler"); }

            StopSpeechRecognition_Async();
        }

        #endregion


        #region Utility Methods

        public static Boolean EvaluateHResult(Int32 hr)
        {
            if (hr < 0)
            {
                IntPtr ptr = SR_GetLastRecordedErrorMessage(true);
                String errMsg = Marshal.PtrToStringAnsi(ptr);
                errMsg += "HRESULT: " + hr;
                UnityEngine.Debug.LogError(errMsg);
            }

            return hr >= 0;
        }

        #endregion

    }
}