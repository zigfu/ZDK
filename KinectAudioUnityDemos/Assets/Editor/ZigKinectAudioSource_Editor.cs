using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(ZigKinectAudioSource))]

// Summary:
//      This custom editor behaves in 2 different ways depending on whether or not EditorApplication.isPlaying.
//
//      If (EditorApplication.isPlaying or EditorApplication.isPaused):
//          OnInspectorGUI gets/sets the ZigKinectAudioSource's properties directly.
//      Else:
//          OnInspectorGUI() gets/sets the ZigKinectAudioSource's "initial" data members.
//          The ZigKinectAudioSource will automatically set it's properties to their corresponding
//          "initial" values when the EditorApplication has begun playing.
//
public class ZigKinectAudioSource_Editor : Editor
{
    const string ClassName = "ZigKinectAudioSource_Editor";

    static public bool verbose = false;


    ZigKinectAudioSource _target;

    // GUI Settings
    ZigKinectAudioSource.ZigBeamAngleMode _guiBeamAngleMode;
    ZigKinectAudioSource.ZigEchoCancellationMode _guiEchoCancellationMode;
    int _guiManualBeamAngle;
    bool _guiAutomaticGainControlEnabled;
    bool _guiNoiseSuppression;

    uint _guiReadStaleThreshold;

    // Stored copies of GUI Settings from previous OnInspectorGUI() call
    ZigKinectAudioSource.ZigBeamAngleMode _oldBeamAngleMode;
    ZigKinectAudioSource.ZigEchoCancellationMode _oldEchoCancellationMode;
    int _oldManualBeamAngle;
    bool _oldAutomaticGainControlEnabled;
    bool _oldNoiseSuppression;


    [MenuItem("GameObject/Create Other/Zig Kinect Audio Source")]
    static void CreateZigKinectAudioSource()
    {
        ZigKinectAudioSource zkas = ZigKinectAudioSource.Instance;
    }
    

    #region Init

    void OnEnable()
    {
        if (verbose) { Debug.Log(ClassName + " :: OnEnable"); }

        _target = (ZigKinectAudioSource)target;

        InitGuiSettings();
    }

    void InitGuiSettings()
    {
        _guiBeamAngleMode = _target.initialBeamAngleMode;
        _guiEchoCancellationMode = _target.initialEchoCancellationMode;
        _guiManualBeamAngle = _target.initialManualBeamAngle;
        _guiAutomaticGainControlEnabled = _target.initialAutomaticGainControlEnabled;
        _guiNoiseSuppression = _target.initialNoiseSuppression;

        _guiReadStaleThreshold = ZigKinectAudioSource.DefaultReadStaleThreshold_Milliseconds;
    }

    #endregion


    #region OnInspectorGUI

    public override void OnInspectorGUI()
    {
        if (OkayToCallAudioSourcesMethodsAndProperties)
        {
            SyncGuiWithAudioSourceProperties();
        }

        StoreCopiesOfCurrentGuiSettings();

        UpdateGui();

        if (GUI.changed)
        {
            OnGuiChanged();
        }
    }

    void SyncGuiWithAudioSourceProperties()
    {
        _guiBeamAngleMode = _target.BeamAngleMode;
        _guiEchoCancellationMode = _target.EchoCancellationMode;
        _guiManualBeamAngle = (int)_target.ManualBeamAngle;
        _guiAutomaticGainControlEnabled = _target.AutomaticGainControlEnabled;
        _guiNoiseSuppression = _target.NoiseSuppression;
    }

    void StoreCopiesOfCurrentGuiSettings()
    {
        _oldBeamAngleMode = _guiBeamAngleMode;
        _oldEchoCancellationMode = _guiEchoCancellationMode;
        _oldManualBeamAngle = _guiManualBeamAngle;
        _oldAutomaticGainControlEnabled = _guiAutomaticGainControlEnabled;
        _oldNoiseSuppression = _guiNoiseSuppression;
    }

    void UpdateGui()
    {
        GUIContent toolTip;

        GUILayout.BeginVertical();
        {
            toolTip = new GUIContent("Beam Angle Mode", "How the beam angle is controlled.  If you need to determine the source angle of incoming sound, set this to Automatic.");
            _guiBeamAngleMode = (ZigKinectAudioSource.ZigBeamAngleMode)EditorGUILayout.EnumPopup(toolTip, _guiBeamAngleMode);

            if (_guiBeamAngleMode == ZigKinectAudioSource.ZigBeamAngleMode.Manual)
            {
                toolTip = new GUIContent("Beam Angle", "The sound source angle (in degrees) that the audio array is currently focusing on.");
                _guiManualBeamAngle = (int)EditorGUILayout.IntSlider(toolTip, _guiManualBeamAngle, (int)ZigKinectAudioSource.MinBeamAngle, (int)ZigKinectAudioSource.MaxBeamAngle);
                _guiManualBeamAngle = (int)ZigKinectAudioSource.ConvertAngleToAcceptableBeamAngle_InDegrees(_guiManualBeamAngle);
            }
            toolTip = new GUIContent("Echo Cancellation Mode", "Enable this if sound will be simultaneously captured and played back by a computer speaker.");
            _guiEchoCancellationMode = (ZigKinectAudioSource.ZigEchoCancellationMode)EditorGUILayout.EnumPopup(toolTip, _guiEchoCancellationMode);

            toolTip = new GUIContent("Automatic Gain Control", "Effectively reduces the volume if the signal is strong and raises it when it is weaker.  Recommended for non-speech scenarios.");
            _guiAutomaticGainControlEnabled = EditorGUILayout.Toggle(toolTip, _guiAutomaticGainControlEnabled);

            toolTip = new GUIContent("Noise Suppression", "Suppresses or reduces stationary background noise in the audio signal.");
            _guiNoiseSuppression = EditorGUILayout.Toggle(toolTip, _guiNoiseSuppression);

            toolTip = new GUIContent("Verbose", "Whether or not to log frequent status updates");
            ZigKinectAudioSource.verbose = EditorGUILayout.Toggle(toolTip, ZigKinectAudioSource.verbose);

            GUI_StartStopButton();
        }
        GUILayout.EndVertical();
    }

    void GUI_StartStopButton()
    {
        GUIContent toolTip;
        if (OkayToCallAudioSourcesMethodsAndProperties && _target.AudioCapturingHasStarted)
        {
            toolTip = new GUIContent("Stop", "Stop capturing audio");
            if (GUILayout.Button(toolTip))
            {
                _target.StopCapturingAudio();
            }
        }
        else
        {
            GUILayout.BeginHorizontal();
            {
                toolTip = new GUIContent("Start", "Start capturing audio");
                if (GUILayout.Button(toolTip))
                {
                    if (OkayToCallAudioSourcesMethodsAndProperties)
                    {
                        _target.StartCapturingAudio(_guiReadStaleThreshold);
                    }
                }
                toolTip = new GUIContent("Read Stale Threshold", "If there are no reads to the stream for longer than the time set here (in milliseconds), "
                                        + "the buffered audio will be discarded. This prevents stale data from being returned in scenarios such as speech recognition, "
                                        + "systems that display user dialogs, or other scenarios when the consumption of audio samples may be intermittent.");
                _guiReadStaleThreshold = (uint)EditorGUILayout.IntField(toolTip, (int)_guiReadStaleThreshold);
            }
            GUILayout.EndHorizontal();
        }
    }

    bool OkayToCallAudioSourcesMethodsAndProperties
    {
        get { return EditorApplication.isPlaying || EditorApplication.isPaused; }
    }

    void OnGuiChanged()
    {
        EditorUtility.SetDirty(_target);

        if (OkayToCallAudioSourcesMethodsAndProperties)
        {
            ApplySettingsToAudioSourcesProperties();
        }
        else
        {
            ApplySettingsToAudioSourcesInitialState();
        }
    }

    void ApplySettingsToAudioSourcesProperties()
    {
        if (verbose) { Debug.Log(ClassName + " :: ApplySettingsToAudioSourcesProperties"); }

        if (_guiBeamAngleMode != _oldBeamAngleMode) { _target.BeamAngleMode = _guiBeamAngleMode; }
        if (_guiEchoCancellationMode != _oldEchoCancellationMode) { _target.EchoCancellationMode = _guiEchoCancellationMode;  }
        if (_guiManualBeamAngle != _oldManualBeamAngle) { _target.ManualBeamAngle = _guiManualBeamAngle; }
        if (_guiAutomaticGainControlEnabled != _oldAutomaticGainControlEnabled) { _target.AutomaticGainControlEnabled = _guiAutomaticGainControlEnabled; }
        if (_guiNoiseSuppression != _oldNoiseSuppression) { _target.NoiseSuppression = _guiNoiseSuppression; }
    }

    void ApplySettingsToAudioSourcesInitialState()
    {
        if (verbose) { Debug.Log(ClassName + " :: ApplySettingsToAudioSourcesInitialState"); }

        _target.initialBeamAngleMode = _guiBeamAngleMode;
        _target.initialEchoCancellationMode = _guiEchoCancellationMode;
        _target.initialManualBeamAngle = _guiManualBeamAngle;
        _target.initialAutomaticGainControlEnabled = _guiAutomaticGainControlEnabled;
        _target.initialNoiseSuppression = _guiNoiseSuppression;
    }

    #endregion

}
