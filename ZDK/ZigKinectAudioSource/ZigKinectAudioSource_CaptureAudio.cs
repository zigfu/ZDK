using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;

// Summary:
//      Contains functionality for capturing Kinect Audio Data and writing it to a Stream at a regular interval.
//
// Note:
//      Main class definition exists in ZigKinectAudioSource.cs.
//      Partial class definition also exists in ZigKinectAudioSource_ImportedFunctions.cs.
//
public sealed partial class ZigKinectAudioSource : MonoBehaviour
{
    public const UInt32 DefaultReadStaleThreshold_Milliseconds = 2000;
    public const Int32 CaptureAudioInterval_MS = 16;


    Byte[] _audioBuffer;
    MemoryStream _audioStream;

    bool _audioCapturingHasStarted = false;
    public bool AudioCapturingHasStarted { get { return _audioCapturingHasStarted; } }


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
    //      and starts capturing audio data streamed out of a sensor. 
    //      readStaleThreshold is the maximum length of time before data is discarded.
    //
    public Stream StartCapturingAudio()
    {
        return StartCapturingAudio(DefaultReadStaleThreshold_Milliseconds);
    }
    public Stream StartCapturingAudio(UInt32 readStaleThreshold_Milliseconds)
    {
        TimeSpan ts = TimeSpan.FromMilliseconds(readStaleThreshold_Milliseconds);
        return StartCapturingAudio(ts);
    }
    public Stream StartCapturingAudio(TimeSpan readStaleThreshold)
    {
        if (verbose) { print(ClassName + " :: StartCapturingAudio : readStaleThreshold = " + readStaleThreshold.TotalMilliseconds); }

        uint audioStreamCapacity = GetAudioStreamCapacityForReadStaleThreshold(readStaleThreshold);
        if (_audioCapturingHasStarted)
        {
            return _audioStream;
        }

        if (!_hasBeenInitialized)
        {
            Initialize();
        }

        _audioStream = new MemoryStream((int)audioStreamCapacity);

        StartCoroutine(CaptureAudio_Coroutine());

        _audioCapturingHasStarted = true;
        OnAudioCapturingStarted();

        return _audioStream;
    }

    // Summary:
    //      Stops capturing audio data and closes the audio stream. 
    //
    public void StopCapturingAudio()
    {
        if (!_audioCapturingHasStarted)
        {
            return;
        }

        if (verbose) { print(ClassName + " :: StopCapturingAudio"); }

        _audioCapturingHasStarted = false;

        StopCoroutine("CaptureAudio_Coroutine");
        _audioStream.Close();

        OnAudioCapturingStopped();
    }

    // Summary:
    //      Calls CaptureAudio_Tick every CaptureAudioInterval_MS milliseconds
    IEnumerator CaptureAudio_Coroutine()
    {
        while (true)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            {
                if (_audioCapturingHasStarted)
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

        WriteToAudioStream(readCount);
        
        RefreshAudioBeamInfo();
    }

    void WriteToAudioStream(uint writeCount)
    {
        MemoryStream s = _audioStream;

        long pos = s.Position;
        if (s.Length + writeCount >= s.Capacity)
        {
            // Transfer remaining unread bytes to start of stream
            long newLength = s.Length - pos;
            Byte[] tempBuffer = new Byte[newLength];
            s.Read(tempBuffer, 0, tempBuffer.Length);
            pos = s.Position = 0;
            s.Write(tempBuffer, 0, tempBuffer.Length);
            s.SetLength(newLength);
        }

        // Write new bytes to end, then return to stored position
        s.Position = s.Length;
        s.Write(_audioBuffer, 0, (int)writeCount);
        s.Position = pos;
    }

    void RefreshAudioBeamInfo()
    {
        BeamAngle = AS_GetBeamAngleInRadians() * Mathf.Rad2Deg;

        _soundSourceAngleConfidence = AS_GetSourceAngleConfidence();
        SoundSourceAngle = AS_GetSourceAngleInRadians() * Mathf.Rad2Deg;
    }

    #endregion


    #region Helper

    uint GetAudioStreamCapacityForReadStaleThreshold(TimeSpan readStaleThreshold)
    {
        WAVEFORMAT wf = GetKinectWaveFormat();
        float audioBytesPerMillisecond = wf.AudioSamplesPerSecond * wf.AudioBlockAlign * 0.001f;
        uint audioStreamCapacity = (uint)(readStaleThreshold.TotalMilliseconds * audioBytesPerMillisecond);
        return audioStreamCapacity;
    }

    #endregion

}