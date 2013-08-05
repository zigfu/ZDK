using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using Zigfu.Utility;


namespace Zigfu.KinectAudio
{
    // Summary:
    //      Contains functionality for capturing Kinect Audio Data and writing it to a Stream at a regular interval.
    //
    // Note:
    //      Main class definition exists in ZigKinectAudioSource.cs.
    //      Partial class definition also exists in ZigKinectAudioSource_ImportedFunctions.cs.
    //
    public sealed partial class ZigKinectAudioSource : Singleton<ZigKinectAudioSource>
    {
        public const UInt32 DefaultReadStaleThreshold_Milliseconds = 500;
        const UInt32 CaptureAudioInterval_MS = 30;


        public UInt32 ReadStaleThreshold_Milliseconds {
            get {
                return (_audioStream == null) ? DefaultReadStaleThreshold_Milliseconds : GetTimeSpanMSForAudioStreamByteCount(_audioStream.ReadStaleThreshold_Bytes);
            }
        }
        public bool AudioCapturingHasStarted { get; private set; }
    

        Byte[] _audioBuffer;
        AudioStream _audioStream;


        public enum ZigAudioProcessingIntent
        {
            CaptureMutableAudio,
            SpeechRecognition
        }
        const ZigAudioProcessingIntent DefaultAudioProcessingIntent = ZigAudioProcessingIntent.CaptureMutableAudio;

        ZigAudioProcessingIntent _audioProcessingIntent = DefaultAudioProcessingIntent;
        public ZigAudioProcessingIntent AudioProcessingIntent
        {
            get
            {
                return _audioProcessingIntent;
            }
            set
            {
                if (verbose) { print(ClassName + " :: Set AudioProcessingIntent to " + value); }

                ZigAudioProcessingIntent oldValue = _audioProcessingIntent;
                ZigAudioProcessingIntent newValue = value;

                bool valueChanged = (newValue != oldValue);
                bool audioCapturingHasStarted = AudioCapturingHasStarted;

                if (valueChanged && audioCapturingHasStarted)
                    { StopCapturingAudio(); }


                    bool doEnableLockDown = (newValue == ZigAudioProcessingIntent.SpeechRecognition);
                    Int32 hr = AS_SetLockDownEnabled(doEnableLockDown);
                    EvaluateHResult(hr);
                    _audioProcessingIntent = newValue;


                if (valueChanged && audioCapturingHasStarted)
                    { StartCapturingAudio(newValue); }
            }
        }


        #region Custom Events

        // --- OnMutableAudioCapturingStarted ---

        public sealed class MutableAudioCapturingStarted_EventArgs : EventArgs
        {
            public Stream AudioStream { get; private set; }

            public MutableAudioCapturingStarted_EventArgs(Stream audioStream)
            {
                AudioStream = audioStream;
            }
        }

        public event EventHandler<MutableAudioCapturingStarted_EventArgs> MutableAudioCapturingStarted;

        void OnMutableAudioCapturingStarted()
        {
            if (verbose) { print(ClassName + " :: OnMutableAudioCapturingStarted"); }

            EventHandler<MutableAudioCapturingStarted_EventArgs> handler = MutableAudioCapturingStarted;
            if (handler != null)
            {
                handler(this, new MutableAudioCapturingStarted_EventArgs(_audioStream));
            }
        }


        // --- OnMutableAudioCapturingStopped ---

        public event EventHandler MutableAudioCapturingStopped;

        void OnMutableAudioCapturingStopped()
        {
            if (verbose) { print(ClassName + " :: OnMutableAudioCapturingStopped"); }

            EventHandler handler = MutableAudioCapturingStopped;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }


        // --- OnAudioCapturingForSpeechRecognitionStarted ---

        public event EventHandler AudioCapturingForSpeechRecognitionStarted;

        void OnAudioCapturingForSpeechRecognitionStarted()
        {
            if (verbose) { print(ClassName + " :: OnAudioCapturingForSpeechRecognitionStarted"); }

            EventHandler handler = AudioCapturingForSpeechRecognitionStarted;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }


        // --- OnAudioCapturingForSpeechRecognitionStopped ---

        public event EventHandler AudioCapturingForSpeechRecognitionStopped;

        void OnAudioCapturingForSpeechRecognitionStopped()
        {
            if (verbose) { print(ClassName + " :: OnAudioCapturingForSpeechRecognitionStopped"); }

            EventHandler handler = AudioCapturingForSpeechRecognitionStopped;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        #endregion


        #region Capture Audio

        // Summary:
        //      Opens an audio data stream (16-bit PCM format, sampled at 16 kHz)
        //       and starts capturing audio data streamed out of a sensor. 
        //      readStaleThreshold is the maximum length of time allowed to go by
        //       without a Read of the audiostream before data is discarded.
        //
        public Stream StartCapturingAudio(ZigAudioProcessingIntent audioProcessingIntent)
        {
            return StartCapturingAudio(audioProcessingIntent, ReadStaleThreshold_Milliseconds);
        }
        public Stream StartCapturingAudio(ZigAudioProcessingIntent audioProcessingIntent, UInt32 readStaleThreshold_Milliseconds)
        {
            TimeSpan readStaleThreshold = TimeSpan.FromMilliseconds(readStaleThreshold_Milliseconds);
            return StartCapturingAudio(audioProcessingIntent, readStaleThreshold);
        }
        public Stream StartCapturingAudio(ZigAudioProcessingIntent audioProcessingIntent, TimeSpan readStaleThreshold)
        {
            if (AudioCapturingHasStarted)
            {
                if (audioProcessingIntent != this.AudioProcessingIntent)
                {
                    throw new Exception("Failed to StartCapturingAudio for " + audioProcessingIntent.ToString()
                        + " because this ZigKinectAudioSource is already capturing audio for " + this.AudioProcessingIntent
                        + ".");
                }
            }
        
            if (verbose)
            {
                print(ClassName + " :: StartCapturingAudio :"
                                + "  audioProcessingIntent = " + audioProcessingIntent.ToString()
                                + "  readStaleThreshold = " + readStaleThreshold.TotalMilliseconds);
            }

            if (!HasBeenInitialized) { Initialize(); }

            this.AudioProcessingIntent = audioProcessingIntent;
            AudioCapturingHasStarted = true;
            switch (AudioProcessingIntent)
            {
                case ZigAudioProcessingIntent.CaptureMutableAudio: StartCapturingMutableAudio(readStaleThreshold); break;
                case ZigAudioProcessingIntent.SpeechRecognition: StartCapturingAudioForSpeechRecognition(); break;
            }
        
            return this._audioStream;
        }

        // Summary:
        //      Stops capturing audio data and closes the audio stream. 
        //
        public void StopCapturingAudio()
        {
            if (!AudioCapturingHasStarted) { return; }
            if (verbose) { print(ClassName + " :: StopCapturingAudio"); }

            AudioCapturingHasStarted = false;
            switch (AudioProcessingIntent)
            {
                case ZigAudioProcessingIntent.CaptureMutableAudio:  StopCapturingMutableAudio(); break;
                case ZigAudioProcessingIntent.SpeechRecognition:    StopCapturingAudioForSpeechRecognition(); break;
            }
        }

        #endregion


        #region Capture Mutable Audio 

        Stream StartCapturingMutableAudio(TimeSpan readStaleThreshold)
        {
            uint readStaleThreshold_bytes = GetAudioStreamByteCountForTimeSpan_MS((uint)readStaleThreshold.TotalMilliseconds);
            _audioStream = new AudioStream(readStaleThreshold_bytes);

            StartCoroutine(CaptureMutableAudio_Coroutine_MethodName);
            OnMutableAudioCapturingStarted();
            return _audioStream;
        }

        void StopCapturingMutableAudio()
        {
            StopCoroutine(CaptureMutableAudio_Coroutine_MethodName);
            _audioStream.Close();
            _audioStream = null;
            OnMutableAudioCapturingStopped();
        }

        // Summary:
        //      Calls CaptureAudio_Tick every CaptureAudioInterval_MS milliseconds
        const string CaptureMutableAudio_Coroutine_MethodName = "CaptureMutableAudio_Coroutine";
        IEnumerator CaptureMutableAudio_Coroutine()
        {
            while (true)
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                {
                    if (AudioCapturingHasStarted)
                    {
                        CaptureMutableAudio_Tick();
                    }
                }
                stopWatch.Stop();

                int elapsedTime = stopWatch.Elapsed.Milliseconds;
                float waitTime = 0.001f * Mathf.Max(0, CaptureAudioInterval_MS - elapsedTime);
                yield return new WaitForSeconds(waitTime);
            }
        }

        // Summary:
        //		Polls Kinect for latest audio data and writes it to _audioStream.
        //		Updates beamAngle, sourceAngle, and sourceConfidence, (which will dispatch events if they changed values.)
        void CaptureMutableAudio_Tick()
        {
            uint readCount;
            Int32 hr = AS_CaptureAudio((UInt32)_audioBuffer.Length, _audioBuffer, out readCount);
            if (!EvaluateHResult(hr))
            {
                return;
            }

            if (readCount > 0)
            {
                _audioStream.AppendBytes(_audioBuffer, 0, readCount);
                RefreshAudioBeamInfo();
            }
        }

        void RefreshAudioBeamInfo()
        {
            BeamAngle = AS_GetBeamAngleInRadians() * Mathf.Rad2Deg;

            _soundSourceAngleConfidence = AS_GetSourceAngleConfidence();
            SoundSourceAngle = AS_GetSourceAngleInRadians() * Mathf.Rad2Deg;
        }

        #endregion


        #region Capture Audio For SpeechRecognition

        void StartCapturingAudioForSpeechRecognition()
        {
            OnAudioCapturingForSpeechRecognitionStarted();
        }

        void StopCapturingAudioForSpeechRecognition()
        {
            OnAudioCapturingForSpeechRecognitionStopped();
        }

        #endregion


        #region Helper

        UInt32 GetAudioStreamByteCountForTimeSpan_MS(UInt32 timeSpan_ms)
        {
            WaveFormat wf = GetKinectWaveFormat();
            float audioBytesPerMillisecond = wf.AudioAverageBytesPerSecond * 0.001f;
            UInt32 byteCount = (UInt32)(timeSpan_ms * audioBytesPerMillisecond);
            return byteCount;
        }

        UInt32 GetTimeSpanMSForAudioStreamByteCount(UInt32 byteCount)
        {
            WaveFormat wf = GetKinectWaveFormat();
            float audioBytesPerMillisecond = wf.AudioAverageBytesPerSecond * 0.001f;
            UInt32 timeSpan_ms = (UInt32)(byteCount / audioBytesPerMillisecond);
            return timeSpan_ms;
        }

        #endregion

    }
}