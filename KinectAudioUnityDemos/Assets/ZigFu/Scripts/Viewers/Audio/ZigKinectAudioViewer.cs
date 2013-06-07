using UnityEngine;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;

[RequireComponent(typeof(ZigBeamAngleViewer))]

public class ZigKinectAudioViewer : MonoBehaviour
{
    const string ClassName = "ZigKinectAudioViewer";

    const int RefreshInterval_MS = 16;
    const float MinAmplitude = 0.0f;
    const float MaxAmplitude = 1.0f;


    public static bool verbose = true;

    public Renderer targetRenderer;
    public int textureWidth = 780;
    public int textureHeight = 100;
    public Color backgroundColor = Color.black;
    public Color waveformColor = Color.green;

    public enum WaveRenderStyle
    {
        Dots,
        VerticalLines
    }
    public WaveRenderStyle renderStyle = WaveRenderStyle.VerticalLines;


    ZigKinectAudioSource _kinectAudioSource;
    ZigBeamAngleViewer _beamAngleViewer;

    Stream _audioStream;
    const int SamplesPerMillisecond = 16;
    const int BytesPerSample = 2;
    readonly byte[] _audioBuffer = new byte[ZigKinectAudioSource.CaptureAudioInterval_MS * SamplesPerMillisecond * BytesPerSample];

    AudioToEnergy _audioToEnergy;
    float[] _energyBuffer;
    uint _energyBufferStartIndex;

    Color[] _blankCanvas;
    Texture2D _textureRef;


    #region Init and Destroy

    void Start()
    {
        if (verbose) { print(ClassName + " :: Start"); }

        _kinectAudioSource = ZigKinectAudioSource.Instance;

        _beamAngleViewer = GetComponent<ZigBeamAngleViewer>();
        _beamAngleViewer.getRenderingAreaHandler = GetBeamAngleRenderingArea;

        uint energyBufferSize = (uint)(textureWidth * 1.25f);
        _energyBuffer = new float[energyBufferSize];
        _audioToEnergy = new AudioToEnergy(energyBufferSize);

        CreateBlankCanvas();
        _textureRef = InitTexture();
        InitTargetRendererWithTexture(_textureRef);

        // Receive Notifications when the ZigKinectAudioSource starts/stops audio capturing so we can start/stop the rendering of its AudioStream.
        _kinectAudioSource.AudioCapturingStarted += AudioCapturingStarted_Handler;
        _kinectAudioSource.AudioCapturingStopped += AudioCapturingStopped_Handler;

        StartUpdating();
    }

    void CreateBlankCanvas()
    {
        _blankCanvas = new Color[textureWidth * textureHeight];
        for (var i = 0; i < _blankCanvas.Length; i++)
        {
            _blankCanvas[i] = backgroundColor;
        }
    }

    Texture2D InitTexture()
    {
        _textureRef = new Texture2D(textureWidth, textureHeight);
        _textureRef.wrapMode = TextureWrapMode.Clamp;

        return _textureRef;
    }

    void InitTargetRendererWithTexture(Texture2D pTexture)
    {
        if (targetRenderer == null)
        {
            targetRenderer = renderer;
        }

        if (null != targetRenderer)
        {
            targetRenderer.material.mainTexture = pTexture;
        }
    }

    void OnDestroy()
    {
        if (verbose) { print(ClassName + "::OnDestroy"); }

        StopUpdating();

        _kinectAudioSource.AudioCapturingStarted -= AudioCapturingStarted_Handler;
        _kinectAudioSource.AudioCapturingStopped -= AudioCapturingStopped_Handler;
    }

    #endregion


    #region Update

    bool _updateEnabled = false;
    public void StartUpdating()
    {
        if (verbose) { print(ClassName + "::StartUpdating"); }

        if (_updateEnabled)
        {
            return;
        }

        _audioStream = _kinectAudioSource.StartCapturingAudio();

        StartCoroutine(Update_Coroutine());

        _updateEnabled = true;
    }
    public void StopUpdating()
    {
        if (verbose) { print(ClassName + "::StopUpdating"); }

        if (!_updateEnabled)
        {
            return;
        }

        StopCoroutine("Update_Coroutine");

        _updateEnabled = false;
    }

    IEnumerator Update_Coroutine()
    {
        while (true)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            {
                if (_updateEnabled)
                {
                    Update_Tick();
                }
            }
            stopWatch.Stop();

            int elapsedTime = stopWatch.Elapsed.Milliseconds;
            float waitTime = 0.001f * Mathf.Max(0, RefreshInterval_MS - elapsedTime);
            yield return new WaitForSeconds(waitTime);
        }
    }

    void Update_Tick()
    {
        GetLatestEnergy();

        //PrintEnergyBuffer();
        //PrintAudioBuffer();

        if (renderStyle == WaveRenderStyle.Dots)
        {
            RenderWaveformAsDots();
        }
        else if (renderStyle == WaveRenderStyle.VerticalLines)
        {
            RenderWaveformAsVerticalLines();
        }
    }

    void GetLatestEnergy()
    {
        int readCount = _audioStream.Read(_audioBuffer, 0, _audioBuffer.Length);
        _audioToEnergy.ConvertAudioToEnergy(_audioBuffer, readCount, ref _energyBuffer, out _energyBufferStartIndex);
    }

    #endregion


    #region Render

    void ClearTexture()
    {
        _textureRef.SetPixels(_blankCanvas, 0);
    }

    void RenderWaveformAsDots()
    {
        ClearTexture();

        int numSamples = _energyBuffer.Length;
        float widthRatio = (float)textureWidth / numSamples;
        
        for (var i = 0; i < numSamples; i++)
        {
            float sample = _energyBuffer[(_energyBufferStartIndex + i) % numSamples];
            int x = (int)(widthRatio * i);
            int y = (int)MathHelper.ConvertFromRangeToRange(MinAmplitude, MaxAmplitude, 0, textureHeight, sample);
            _textureRef.SetPixel(x, y, waveformColor);
        }

        _textureRef.Apply();
    }

    void RenderWaveformAsVerticalLines()
    {
        ClearTexture();

        int numSamples = _energyBuffer.Length;
        float halfHeight = 0.5f * textureHeight;
        float widthRatio = (float)textureWidth / numSamples;

        Color[] waveformColors = new Color[textureHeight];
        for (int i = 0; i < waveformColors.Length; i++)
        {
            waveformColors[i] = waveformColor;
        }

        for (var i = 0; i < numSamples; i++)
        {
            float sample = _energyBuffer[(_energyBufferStartIndex + i) % numSamples];
            uint x = (uint)(widthRatio * i);

            float amp = Mathf.Abs(MathHelper.ConvertFromRangeToRange(MinAmplitude, MaxAmplitude, -halfHeight, halfHeight, sample));
            uint lineStartY = (uint)(halfHeight - amp);
            uint lineHeight = (uint)(2 * amp);

            _textureRef.SetPixels((int)x, (int)lineStartY, 1, (int)lineHeight, waveformColors);
        }


        _textureRef.Apply();
    }

    Rect GetWaveformRenderingArea()
    {
        float tW = textureWidth;
        float tH = textureHeight;
        float sW = Screen.width;
        float sH = Screen.height;

        // Define Bottom-Center Area
        float bvHeight = _beamAngleViewer.textureHeight;
        float yPad = 20;
        float x = 0.5f * (sW - tW);
        float y = sH - tH - yPad - bvHeight;

        return new Rect(x, y, tW, tH);
    }

    Rect GetBeamAngleRenderingArea(ZigBeamAngleViewer bv)
    {
        Rect wfArea = GetWaveformRenderingArea();

        // Render the BeamAngleView directly beneath the waveform view
        float tW = bv.textureWidth;
        float tH = bv.textureHeight;

        float yPad = 10;
        float x = wfArea.x;
        float y = wfArea.yMax + yPad;

        return new Rect(x, y, tW, tH);
    }

    #endregion


    #region GUI

    void OnGUI()
    {
        GUI.DrawTexture(GetWaveformRenderingArea(), _textureRef);
    }

    #endregion


    #region ZigKinectAudioSource Event Handlers

    void AudioCapturingStarted_Handler(object sender, ZigKinectAudioSource.AudioCapturingStarted_EventArgs e)
    {
        if (verbose) { print(ClassName + " :: AudioCapturingStarted_Handler"); }

        _audioStream = e.AudioStream;
        StartUpdating();
    }

    void AudioCapturingStopped_Handler(object sender, EventArgs e)
    {
        if (verbose) { print(ClassName + " :: AudioCapturingStopped_Handler"); }

        StopUpdating();
        _audioStream = null;
    }

    #endregion


    #region Helper Methods

    void PrintAudioBuffer(int readCount)
    {
        print("-------  PrintAudioBuffer  -------");

        StringBuilder sb = new StringBuilder();
        const int inc = 20;
        for (int i = 0; i < readCount; i += inc)
        {
            float sample = _audioBuffer[i];
            sb.Append(sample.ToString("F2") + ", ");
        }
        print(sb.ToString());

        print("-----------------------------------");
    }

    void PrintEnergyBuffer()
    {
        print("-------  PrintEnergyBuffer  -------");

        StringBuilder sb = new StringBuilder();
        const int inc = 20;
        int numSamples = _energyBuffer.Length;
        for (int i = 0; i < numSamples; i += inc)
        {
            float sample = _energyBuffer[(_energyBufferStartIndex + i) % numSamples];
            sb.Append(sample.ToString("F2") + ", ");
        }
        print(sb.ToString());

        print("-----------------------------------");
    }

    #endregion
}
