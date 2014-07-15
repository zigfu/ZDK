using UnityEngine;
using System;
using System.Runtime.InteropServices;
using Zigfu.Utility;


namespace Zigfu.KinectAudio
{
    public sealed partial class ZigKinectAudioSource : MonoBehaviour
    {

        const String DLL_DIR = @"";
        const String DLL_PATH = DLL_DIR + @"ZigNativeKinectAudioSourceDll";


        // Summary:
        //      Managed equivalent of WAVEFORMATEX
        [StructLayout(LayoutKind.Sequential)]
        public struct WaveFormat
        {
            public UInt16 AudioFormat;
            public UInt16 AudioChannels;
            public UInt32 AudioSamplesPerSecond;
            public UInt32 AudioAverageBytesPerSecond;
            public UInt16 AudioBlockAlign;
            public UInt16 AudioBitsPerSample;
            public UInt16 ExtraInfoSize;
        }


        [DllImport(DLL_PATH)]  static extern Int32 AS_SetManualBeamModeEnabled(Boolean doEnable);
        [DllImport(DLL_PATH)]  static extern Int32 AS_GetManualBeamModeEnabled(out Boolean outEnabled);
        [DllImport(DLL_PATH)]  static extern Int32 AS_SetBeamAngleInRadians(Double newAngle);
        [DllImport(DLL_PATH)]  static extern Double AS_GetBeamAngleInRadians();
        [DllImport(DLL_PATH)]  static extern Double AS_GetSourceAngleInRadians();
        [DllImport(DLL_PATH)]  static extern Double AS_GetSourceAngleConfidence();

        [DllImport(DLL_PATH)]  static extern Int32 AS_SetAutomaticGainControlEnabled(Boolean doEnable);
        [DllImport(DLL_PATH)]  static extern Int32 AS_GetAutomaticGainControlEnabled(out Boolean outEnabled);
        [DllImport(DLL_PATH)]  static extern Int32 AS_SetNoiseSuppressionEnabled(Boolean doEnable);
        [DllImport(DLL_PATH)]  static extern Int32 AS_GetNoiseSuppressionEnabled(out Boolean outEnabled);

        [DllImport(DLL_PATH)]  static extern Int32 AS_SetAcousticEchoCancellationLength(Int64 newLength);
        [DllImport(DLL_PATH)]  static extern Int32 AS_GetAcousticEchoCancellationLength(out Int64 outLength);
        [DllImport(DLL_PATH)]  static extern Int32 AS_SetAcousticEchoSuppressionCount(Int64 newCount);
        [DllImport(DLL_PATH)]  static extern Int32 AS_GetAcousticEchoSuppressionCount(out Int64 outCount);

        [DllImport(DLL_PATH)]  static extern Int32 AS_SetLockDownEnabled(Boolean doEnable);
        [DllImport(DLL_PATH)]  static extern Int32 AS_GetLockDownEnabled(out Boolean outEnabled);

        [DllImport(DLL_PATH)]  static extern Int32 AS_InitializeAudioSource(Boolean doInitializeKinect);
        [DllImport(DLL_PATH)]  static extern Int32 AS_CaptureAudio(UInt32 bufferSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] Byte[] buffer, out UInt32 numSamplesCaptured);
        [DllImport(DLL_PATH)]  static extern void AS_Destroy();

        [DllImport(DLL_PATH)]  static extern UInt32 AS_GetAudioBufferMaxCapacity();
        [DllImport(DLL_PATH)]  static extern WaveFormat AS_GetKinectWaveFormat();
        [DllImport(DLL_PATH)]  static extern IntPtr AS_GetLastRecordedErrorMessage(Boolean doClearMessage = false);

    }
}