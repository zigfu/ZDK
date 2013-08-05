using UnityEngine;
using System;
using System.Runtime.InteropServices;
using Zigfu.Utility;


namespace Zigfu.Speech
{
    public sealed partial class ZigKinectSpeechRecognizer : Singleton<ZigKinectSpeechRecognizer>, IZigSpeechGrammarDelegate
    {

        const String DLL_DIR = @"";
        const String DLL_PATH = DLL_DIR + @"ZigNativeKinectAudioSourceDll";


        [DllImport(DLL_PATH)]  static extern Int32 SR_SetLanguage(UInt32 stringLength, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] Byte[] languageCode);
        [DllImport(DLL_PATH)]  static extern Int32 SR_SetAdaptaionEnabled(bool doEnable);
        [DllImport(DLL_PATH)]  static extern Int32 SR_GetAdaptaionEnabled(out bool doEnable);

        [DllImport(DLL_PATH)]  static extern Int32 SR_InitializeSpeechRecognizer();
        [DllImport(DLL_PATH)]  static extern void  SR_Destroy();

        [DllImport(DLL_PATH)]  static extern Int32 SR_StartSpeechRecognition();
        [DllImport(DLL_PATH)]  static extern Int32 SR_StopSpeechRecognition();
        [DllImport(DLL_PATH)]  static extern Int32 SR_ProcessSpeech();
        [DllImport(DLL_PATH)]  static extern IntPtr SR_GetLastRecognizedSpeech();
        [DllImport(DLL_PATH)]  static extern float SR_GetLastRecognizedSpeechConfidence();

        [DllImport(DLL_PATH)]  static extern Int32 SR_CreateGrammarFromXml(UInt32 stringLength, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] Byte[] grammarXmlFileName, Boolean dynamic, out UInt32 out_GrammarID);
        [DllImport(DLL_PATH)]  static extern Int32 SR_RemoveGrammar(UInt32 grammarID);
        [DllImport(DLL_PATH)]  static extern Int32 SR_ActivateSpeechGrammar(UInt32 grammarID);
        [DllImport(DLL_PATH)]  static extern Int32 SR_DeactivateSpeechGrammar(UInt32 grammarID);

        [DllImport(DLL_PATH)]  static extern IntPtr SR_GetLastRecordedErrorMessage(Boolean doClearMessage = false);

    }
}