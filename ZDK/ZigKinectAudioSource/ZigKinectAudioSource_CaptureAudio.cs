using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using ZDK.ZigKinectAudioSource;
using ZDK.Utility;


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


    #region Custom Events

    // --- OnAudioCapturingStarted ---

    public sealed class AudioCapturingStarted_EventArgs : EventArgs
    {
        public Stream AudioStream { get; private set; }

        public AudioCapturingStarted_EventArgs(Stream audioStream)
        {
            AudioStream = audioStream;
        }
    }

    public event EventHandler<AudioCapturingStarted_EventArgs> AudioCapturingStarted;

    void OnAudioCapturingStarted()
    {
        if (verbose) { print(ClassName + " :: OnAudioCapturingStarted"); }

        EventHandler<AudioCapturingStarted_EventArgs> handler = AudioCapturingStarted;
        if (handler != null)
        {
            handler(this, new AudioCapturingStarted_EventArgs(_audioStream));
        }
    }


    // --- OnAudioCapturingStopped ---

    public event EventHandler AudioCapturingStopped;

    void OnAudioCapturingStopped()
    {
        if (verbose) { print(ClassName + " :: OnAudioCapturingStopped"); }

        EventHandler handler = AudioCapturingStopped;
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
    public Stream StartCapturingAudio()
    {
        return StartCapturingAudio(ReadStaleThreshold_Milliseconds);
    }
    public Stream StartCapturingAudio(UInt32 readStaleThreshold_Milliseconds)
    {
        TimeSpan ts = TimeSpan.FromMilliseconds(readStaleThreshold_Milliseconds);
        return StartCapturingAudio(ts);
    }
    public Stream StartCapturingAudio(TimeSpan readStaleThreshold)
    {
        if (AudioCapturingHasStarted)
        {
            return _audioStream;
        }

        if (verbose) { print(ClassName + " :: StartCapturingAudio : readStaleThreshold = " + readStaleThreshold.TotalMilliseconds); }

        if (!_hasBeenInitialized)
        {
            Initialize();
        }

        uint readStaleThreshold_bytes = GetAudioStreamByteCountForTimeSpan_MS((uint)readStaleThreshold.TotalMilliseconds);
        _audioStream = new AudioStream(readStaleThreshold_bytes);

        StartCoroutine(CaptureAudio_Coroutine_MethodName);

        AudioCapturingHasStarted = true;
        OnAudioCapturingStarted();

        return _audioStream;
    }

    // Summary:
    //      Stops capturing audio data and closes the audio stream. 
    //
    public void StopCapturingAudio()
    {
        if (!AudioCapturingHasStarted)
        {
            return;
        }

        if (verbose) { print(ClassName + " :: StopCapturingAudio"); }

        AudioCapturingHasStarted = false;

        StopCoroutine(CaptureAudio_Coroutine_MethodName);
        _audioStream.Close();

        OnAudioCapturingStopped();
    }

    // Summary:
    //      Calls CaptureAudio_Tick every CaptureAudioInterval_MS milliseconds
    const string CaptureAudio_Coroutine_MethodName = "CaptureAudio_Coroutine";
    IEnumerator CaptureAudio_Coroutine()
    {
        while (true)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            {
                if (AudioCapturingHasStarted)
                {
                    CaptureAudio_Tick();
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
    void CaptureAudio_Tick()
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