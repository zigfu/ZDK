using UnityEngine;
using System;
using System.Runtime.InteropServices;
using ZDK.Utility;


// Summary:
//      Declares Functions Imported from ZigNativeKinectAudioSourceDll.dll.
//      Main class definition exists in ZigKinectAudioSource.cs.
//      Partial class definition also exists in ZigKinectAudioSource_CaptureAudio.cs.
//
public sealed partial class ZigKinectAudioSource : Singleton<ZigKinectAudioSource>
{

    const String AS_DLL_DIR = @"Assets/Zigfu/Scripts/_Internal/";
    const String AS_DLL_PATH = AS_DLL_DIR + @"ZigNativeKinectAudioSourceDll";


    // Summary:
    //      Managed equivalent of WAVEFORMATEX
    [StructLayout(LayoutKind.Sequential)]
    public struct WAVEFORMAT
    {
        public UInt16 AudioFormat;
        public UInt16 AudioChannels;
        public UInt32 AudioSamplesPerSecond;
        public UInt32 AudioAverageBytesPerSecond;
        public UInt16 AudioBlockAlign;
        public UInt16 AudioBitsPerSample;
        public UInt16 ExtraInfoSize;
    }


    [DllImport(AS_DLL_PATH)]  static extern Int32 AS_SetManualBeamModeEnabled(Boolean doEnable);
    [DllImport(AS_DLL_PATH)]  static extern Int32 AS_GetManualBeamModeEnabled(out Boolean outEnabled);
    [DllImport(AS_DLL_PATH)]  static extern Int32 AS_SetBeamAngleInRadians(Double newAngle);
    [DllImport(AS_DLL_PATH)]  static extern Double AS_GetBeamAngleInRadians();
    [DllImport(AS_DLL_PATH)]  static extern Double AS_GetSourceAngleInRadians();
    [DllImport(AS_DLL_PATH)]  static extern Double AS_GetSourceAngleConfidence();

    [DllImport(AS_DLL_PATH)]  static extern Int32 AS_SetAutomaticGainControlEnabled(Boolean doEnable);
    [DllImport(AS_DLL_PATH)]  static extern Int32 AS_GetAutomaticGainControlEnabled(out Boolean outEnabled);
    [DllImport(AS_DLL_PATH)]  static extern Int32 AS_SetNoiseSuppressionEnabled(Boolean doEnable);
    [DllImport(AS_DLL_PATH)]  static extern Int32 AS_GetNoiseSuppressionEnabled(out Boolean outEnabled);

    [DllImport(AS_DLL_PATH)]  static extern Int32 AS_SetAcousticEchoCancellationLength(UInt32 newLength);
    [DllImport(AS_DLL_PATH)]  static extern Int32 AS_GetAcousticEchoCancellationLength(out UInt32 outLength);
    [DllImport(AS_DLL_PATH)]  static extern Int32 AS_SetAcousticEchoSuppressionCount(UInt32 newCount);
    [DllImport(AS_DLL_PATH)]  static extern Int32 AS_GetAcousticEchoSuppressionCount(out UInt32 outCount);

    [DllImport(AS_DLL_PATH)]  static extern Int32 AS_InitializeAudioSource(Boolean doInitializeKinect);
    [DllImport(AS_DLL_PATH)]  static extern Int32 AS_CaptureAudio(UInt32 bufferSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] Byte[] buffer, out UInt32 numSamplesCaptured);
    [DllImport(AS_DLL_PATH)]  static extern void AS_Shutdown();

    [DllImport(AS_DLL_PATH)]  static extern UInt32 AS_GetAudioBufferMaxCapacity();
    [DllImport(AS_DLL_PATH)]  static extern WAVEFORMAT AS_GetKinectWaveFormat();
    [DllImport(AS_DLL_PATH)]  static extern IntPtr AS_GetLastRecordedErrorMessage();

}
