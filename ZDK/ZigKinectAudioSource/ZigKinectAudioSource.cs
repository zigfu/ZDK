using UnityEngine;
using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Text;
using ZDK.Utility;


// Summary:
//      ZigKinectAudioSource is the Zig native equivalent of Microsoft.Kinect.KinectAudioSource.
//      ZigKinectAudioSource extends MonoBehaviour, using a Coroutine (CaptureAudio_Coroutine())
//      to update it's Kinect audio stream internally every CaptureAudioInterval_MS milliseconds.
//
//      Whereas Microsoft.Kinect.KinectAudioSource contains the methods Start() and Stop(),
//      ZigKinectAudioSource contains the equivalent methods StartCapturingAudio() and StopCapturingAudio().
//      
// Note:
//      Partial class definition also exists in ZigKinectAudioSource_ImportedFunctions.cs.
//      - It contains Declarations of external functions imported from ZigNativeKinectAudioSourceDll.dll.
//
//      Partial class definition also exists in ZigKinectAudioSource_CaptureAudio.cs.
//      - It contains functionality for capturing Kinect Audio Data and writing it to a Stream
// 
public sealed partial class ZigKinectAudioSource : Singleton<ZigKinectAudioSource>
{
    const String ClassName = "ZigKinectAudioSource";

    static public bool verbose = true;


    public ZigBeamAngleMode initialBeamAngleMode = DefaultZigBeamAngleMode;
    public ZigEchoCancellationMode initialEchoCancellationMode = DefaultZigEchoCancellationMode;
    public Int32 initialManualBeamAngle = 0;
    public Boolean initialAutomaticGainControlEnabled = false;
    public Boolean initialNoiseSuppression = true;

    public void ApplyInitialSettings()
    {
        if (verbose) { Debug.Log(ClassName + " :: ApplyInitialSettings"); }

        BeamAngleMode = initialBeamAngleMode;
        EchoCancellationMode = initialEchoCancellationMode;
        ManualBeamAngle = initialManualBeamAngle;
        AutomaticGainControlEnabled = initialAutomaticGainControlEnabled;
        NoiseSuppression = initialNoiseSuppression;
    }


    Double _beamAngleInDegrees = 0;
    Double _soundSourceAngleInDegrees = 0;
    Double _soundSourceAngleConfidence = 0;

    bool _hasBeenInitialized = false;


    public enum ZigBeamAngleMode
    {
        Automatic,      // Sensor sets the beam angle and adapts it to the strongest audio source. Use this for a low-volume loudspeaker and/or isotropic background noise. (This is the default option.)
        //Adaptive,     // Sensor sets the beam angle and adapts it to the strongest audio source. Use this for a high-volume loudspeaker and/or higher noise levels.
        Manual          // The user sets the beam angle to point in the direction of the audio source of interest.
    }
    const ZigBeamAngleMode DefaultZigBeamAngleMode = ZigBeamAngleMode.Automatic;

    public enum ZigEchoCancellationMode
    {
        CancellationAndSuppression,     // Apply both echo cancellation and acoustic echo suppression.
        CancellationOnly,               // Apply only echo cancellation.
        None                            // Apply neither. (This is the default option.)
    }
    const ZigEchoCancellationMode DefaultZigEchoCancellationMode = ZigEchoCancellationMode.None;
    public ZigEchoCancellationMode _echoCancellationMode = DefaultZigEchoCancellationMode;


    #region Init and Destroy

    void Start()
    {
        try
        {
            Initialize();
        }
        catch
        {
            return;
        }
    }

    // Summary:
    //     Initializes the AudioSource and applies Initial settings.  
    //     Must be called before any of this classes properties can be set
    void Initialize()
    {
        if (_hasBeenInitialized)
        {
            return;
        }

        if (verbose) { print(ClassName + " :: Initialize"); }

        Int32 hr = AS_InitializeAudioSource(false);
        if (!EvaluateHResult(hr))
        {
            throw new Exception(ClassName + " failed to initialize.");
        }

        UInt32 maxBufferLength = AS_GetAudioBufferMaxCapacity();
        _audioBuffer = new Byte[maxBufferLength];

        _hasBeenInitialized = true;

        ApplyInitialSettings();

        if (verbose) { PrintKinectAudioFormat(); }
    }

    void OnDestroy()
    {
        if (verbose) { print(ClassName + " :: OnDestroy"); }

        StopCapturingAudio();

        AS_Shutdown();

        _hasBeenInitialized = false;
    }

    #endregion


    #region Custom Events

    // --- OnBeamAngleChanged ---

    public sealed class BeamAngleChanged_EventArgs : EventArgs
    {
        public double Angle { get; private set; }

        public BeamAngleChanged_EventArgs(double angle)
        {
            Angle = angle;
        }
    }
    public event EventHandler<BeamAngleChanged_EventArgs> BeamAngleChanged;

    void OnBeamAngleChanged()
    {
        if (verbose) { Debug.Log(ClassName + " :: OnBeamAngleChanged"); }

        EventHandler<BeamAngleChanged_EventArgs> handler = BeamAngleChanged;
        if (handler != null)
        {
            handler(this, new BeamAngleChanged_EventArgs(BeamAngle));
        }
    }


    // --- OnSoundSourceAngleChanged ---

    public sealed class SoundSourceAngleChanged_EventArgs : EventArgs
    {
        public double Angle { get; private set; }
        public double ConfidenceLevel { get; private set; }

        public SoundSourceAngleChanged_EventArgs(double angle, double confidenceLevel)
        {
            Angle = angle;
            ConfidenceLevel = confidenceLevel;
        }
    }
    public event EventHandler<SoundSourceAngleChanged_EventArgs> SoundSourceAngleChanged;

    void OnSoundSourceAngleChanged()
    {
        if (verbose) { Debug.Log(ClassName + " :: OnSoundSourceAngleChanged"); }

        EventHandler<SoundSourceAngleChanged_EventArgs> handler = SoundSourceAngleChanged;
        if (handler != null)
        {
            handler(this, new SoundSourceAngleChanged_EventArgs(SoundSourceAngle, SoundSourceAngleConfidence));
        }
    }

    #endregion


    #region Properties

    // Summary:
    //      Beam/SoundSource angle bounds, in degrees
    public static Double MinBeamAngle { get { return -50; } }
    public static Double MaxBeamAngle { get { return 50; } }
    public static Double MinSoundSourceAngle { get { return -50; } }
    public static Double MaxSoundSourceAngle { get { return 50; } }

    // Summary:
    //      Gets the sound source angle (in degrees) that the audio array is currently focusing on
    public Double BeamAngle
    {
        get { return _beamAngleInDegrees; }
        private set
        {
            int oldAngle_deg = Mathf.RoundToInt((float)_beamAngleInDegrees);
            int newAngle_deg = (int)ConvertAngleToAcceptableBeamAngle_InDegrees(value);

            if (newAngle_deg != oldAngle_deg)
            {
                if (verbose) { Debug.Log("Set BeamAngle to " + newAngle_deg); }

                Double newAngle_Rad = newAngle_deg * Mathf.Deg2Rad;
                Int32 hr = AS_SetBeamAngleInRadians(newAngle_Rad);
                if (!EvaluateHResult(hr))
                {
                    return;
                }

                _beamAngleInDegrees = newAngle_deg;

                OnBeamAngleChanged();
            }
        }
    }
    // Summary:
    //     Gets or sets manual Beam angle, in degrees.
    //
    // Remarks:
    //     Only valid when BeamAngleMode is set to Manual.
    public Double ManualBeamAngle
    {
        get
        {
            return (BeamAngleMode == ZigBeamAngleMode.Manual) ? BeamAngle : 0;
        }
        set
        {
            if (BeamAngleMode == ZigBeamAngleMode.Manual)
            {
                BeamAngle = value;
            }
        }
    }
    // Summary:
    //     Gets or sets the beam angle mode that determines how the beam angle is controlled.
    //      Default is Automatic.
    public ZigBeamAngleMode BeamAngleMode
    {
        get
        {
            Boolean isEnabled;
            Int32 hr = AS_GetManualBeamModeEnabled(out isEnabled);
            EvaluateHResult(hr);
            return isEnabled ? ZigBeamAngleMode.Manual : ZigBeamAngleMode.Automatic;
        }
        set
        {
            if (verbose) { Debug.Log("Set BeamAngleMode to " + value); }
            Int32 hr = AS_SetManualBeamModeEnabled(value == ZigBeamAngleMode.Manual);
            EvaluateHResult(hr);
        }
    }

    // Summary:
    //     Gets the most recent sound source position observed, in degrees.
    //
    // Remarks:
    //     This property gets updated every time ProcessAudio() is called.
    public Double SoundSourceAngle
    {
        get { return _soundSourceAngleInDegrees; }
        private set
        {
            int oldAngle_deg = Mathf.RoundToInt((float)_soundSourceAngleInDegrees);
            _soundSourceAngleInDegrees = Mathf.RoundToInt((float)value);

            if (_soundSourceAngleInDegrees != oldAngle_deg)
            {
                if (verbose) { Debug.Log("Set SoundSourceAngle to " + _soundSourceAngleInDegrees); }

                OnSoundSourceAngleChanged();
            }
        }
    }
    // Summary:
    //     Gets the most recent measurement of our confidence in the sound source position.
    //      This value is in the range [0,1], 1 being the highest possible confidence.
    //
    // Remarks:
    //     This property gets updated  every time ProcessAudio() is called.
    public Double SoundSourceAngleConfidence
    {
        get { return _soundSourceAngleConfidence; }
    }

    // Summary:
    //     Gets or sets a value indicating whether Automatic Gain Control is enabled.
    //      OFF by default. Recommended for non-speech scenarios.  Maps to the DMO property
    //     MFPKEY_WMAAECMA_FEATR_AGC.
    public Boolean AutomaticGainControlEnabled
    {
        get
        {
            Boolean isEnabled;
            Int32 hr = AS_GetAutomaticGainControlEnabled(out isEnabled);
            EvaluateHResult(hr);
            return isEnabled;
        }
        set
        {
            if (verbose) { Debug.Log("Set AutomaticGainControlEnabled to " + value); }
            Int32 hr = AS_SetAutomaticGainControlEnabled(value);
            EvaluateHResult(hr);
        }
    }
    // Summary:
    //     Gets or sets a value indicating whether Noise Suppression is enabled.  ON
    //     by default.  Maps to the DMO property MFPKEY_WMAAECMA_FEATR_NS.
    public Boolean NoiseSuppression
    {
        get
        {
            Boolean isEnabled;
            Int32 hr = AS_GetNoiseSuppressionEnabled(out isEnabled);
            EvaluateHResult(hr);
            return isEnabled;
        }
        set
        {
            if (verbose) { Debug.Log("Set NoiseSuppression to " + value); }
            Int32 hr = AS_SetNoiseSuppressionEnabled(value);
            EvaluateHResult(hr);
        }
    }
    // Summary:
    //     Gets or sets the echo cancellation and suppression mode.  Default is None
    //     (i.e.: echo cancellation and echo suppression are both turned off).
    public ZigEchoCancellationMode EchoCancellationMode
    {
        get
        {
            return _echoCancellationMode;
        }
        set
        {
            if (verbose) { Debug.Log("Set EchoCancellationMode to " + value); }

            Int32 hr = 0;
            UInt32 newSprsnCount = 0;
            UInt32 newCnclLength = 0;
            switch (value)
            {
                case ZigEchoCancellationMode.CancellationAndSuppression:
                    newSprsnCount = 2;
                    newCnclLength = 256;
                    break;
                case ZigEchoCancellationMode.CancellationOnly:
                    newSprsnCount = 0;
                    newCnclLength = 256;
                    break;
                case ZigEchoCancellationMode.None:
                    newSprsnCount = 0;
                    newCnclLength = 128;
                    break;
                default: break;
            }

            hr = AS_SetAcousticEchoSuppressionCount(newSprsnCount);
            if (!EvaluateHResult(hr))
            {
                return;
            }
            hr = AS_SetAcousticEchoCancellationLength(newCnclLength);
            if (!EvaluateHResult(hr))
            {
                return;
            }

            _echoCancellationMode = value;
        }
    }
    // Summary:
    //		Returns a struct that describes the Kinects audio format
    public WAVEFORMAT GetKinectWaveFormat()
    {
        return AS_GetKinectWaveFormat();
    }

    #endregion


    #region Utility Methods

    // Summary:
    //      BeamAngle must be between -50 and 50 degrees, and evenly divisible by 10
    static public Double ConvertAngleToAcceptableBeamAngle_InDegrees(Double angle)
    {
        float clampedAngle = Mathf.Clamp((float)angle, (float)MinBeamAngle, (float)MaxBeamAngle);
        Double roundedAngle = 10 * (Mathf.RoundToInt(0.1f * clampedAngle));
        return roundedAngle;
    }

    public void PrintKinectAudioFormat()
    {
        WAVEFORMAT wf = GetKinectWaveFormat();
        StringBuilder sb = new StringBuilder();

        sb.Append("--------------  PrintKinectAudioFormat  -------------");

        sb.Append("\nAudioFormat:                 " + wf.AudioFormat);
        sb.Append("\nAudioChannels:               " + wf.AudioChannels);
        sb.Append("\nAudioSamplesPerSecond:       " + wf.AudioSamplesPerSecond);
        sb.Append("\nAudioAverageBytesPerSecond:  " + wf.AudioAverageBytesPerSecond);
        sb.Append("\nAudioBlockAlign:             " + wf.AudioBlockAlign);
        sb.Append("\nAudioBitsPerSample:          " + wf.AudioBitsPerSample);

        sb.Append("\n-----------------------------------------------------");

        Debug.Log(sb.ToString());
    }


    Boolean EvaluateHResult(Int32 hr)
    {
        if (hr < 0)
        {
            IntPtr ptr = AS_GetLastRecordedErrorMessage();
            String errMsg = Marshal.PtrToStringAnsi(ptr);
            errMsg += "HRESULT: " + hr;
            Debug.LogError(errMsg);
        }

        return hr >= 0;
    }

    #endregion

}