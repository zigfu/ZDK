using UnityEngine;

public class ZigBeamAngleViewer : MonoBehaviour {

    const string ClassName = "ZigBeamAngleViewer";


    public Renderer targetRenderer;
    public int textureWidth = 780;            
    public int textureHeight = 40;
    public Color backgroundColor = Color.black;
    public Color beamAngleDrawColor = Color.cyan;
    public Color soundSourceAngleDrawColor = Color.green;

    public static bool verbose = true;


    Color[] _blankCanvas;
    Texture2D _textureRef;

    // The areas of the texture that the indicators will be rendered within
    Rect _beamAngleIndicatorArea;
    Rect _soundSourceAngleIndicatorArea;

    ZigKinectAudioSource _kinectAudioSource;


    // Summary:
    //     This delegate method is responsible for returning the screen area to render to
    public delegate Rect GetRenderingAreaDelegate(ZigBeamAngleViewer bv);
    public GetRenderingAreaDelegate getRenderingAreaHandler;


    #region Init and Destroy

    void Start()
    {
        if (verbose) { print(ClassName + "::Start"); }

        _kinectAudioSource = ZigKinectAudioSource.Instance;
        AttachKinectAudioSourceEventHandlers(_kinectAudioSource);

        CreateBlankCanvas();
        _textureRef = InitTexture();
        InitTargetRendererWithTexture(_textureRef);
        InitIndicatorAreas();

        RenderBeamAngleIndicator(_kinectAudioSource.BeamAngle);
        RenderSoundSourceAngleIndicator(_kinectAudioSource.SoundSourceAngle, _kinectAudioSource.SoundSourceAngleConfidence);
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

    void InitIndicatorAreas()
    {
        float beamAreaHeight = textureHeight * 0.3f;
        float beamAreaTop = textureHeight - beamAreaHeight;
        _beamAngleIndicatorArea = new Rect(0, beamAreaTop, textureWidth, beamAreaHeight);

        float srcAreaHeight = textureHeight - beamAreaHeight;
        _soundSourceAngleIndicatorArea = new Rect(0, 0, textureWidth, srcAreaHeight);
    }

    void AttachKinectAudioSourceEventHandlers(ZigKinectAudioSource pKinectAudioSource)
    {
        pKinectAudioSource.BeamAngleChanged += BeamAngleChangedHandler;
        pKinectAudioSource.SoundSourceAngleChanged += SoundSourceAngleChangedHandler;
    }

    void OnDestroy()
    {
        if (verbose) { print(ClassName + " :: OnDestroy"); }

        _kinectAudioSource .BeamAngleChanged -= BeamAngleChangedHandler;
        _kinectAudioSource.SoundSourceAngleChanged -= SoundSourceAngleChangedHandler;
    }

    #endregion


    #region BeamAngle Event Handlers

    void BeamAngleChangedHandler(object sender, ZigKinectAudioSource.BeamAngleChanged_EventArgs e)
    {
        if (verbose) { print(ClassName + "::BeamAngleChangedHandler : Angle = " + (int)e.Angle); }

        RenderBeamAngleIndicator(e.Angle);
    }

    void SoundSourceAngleChangedHandler(object sender, ZigKinectAudioSource.SoundSourceAngleChanged_EventArgs e)
    {
        if (verbose) { print(ClassName + "::SoundSourceAngleChangedHandler : Angle = " + (int)e.Angle + ", Confidence = " + e.ConfidenceLevel.ToString("F2")); }

        RenderSoundSourceAngleIndicator(e.Angle, e.ConfidenceLevel);
    }

    #endregion


    #region Rendering

    void RenderBeamAngleIndicator(double beamAngle)
    {
        ClearBeamAngleIndicatorArea();

        uint centerX = (uint)ConvertBeamAngleToXPos(beamAngle);
        int xMin = (int)_beamAngleIndicatorArea.xMin;
        int xMax = (int)_beamAngleIndicatorArea.xMax - 1;
        int yMin = (int)_beamAngleIndicatorArea.yMin;

        uint blockWidth = 9;
        uint blockHeight = (uint)_beamAngleIndicatorArea.height;

        int tempX = (int)(centerX - blockWidth * 0.5f);
        if (tempX < xMin) { tempX = xMin; }
        else if (tempX > xMax - blockWidth) { tempX = xMax - (int)blockWidth; }
        uint x = (uint)tempX;

        uint y = (uint)yMin;

        Color[] colors = new Color[blockWidth * blockHeight];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = beamAngleDrawColor;
        }

        _textureRef.SetPixels((int)x, (int)y, (int)blockWidth, (int)blockHeight, colors);

        _textureRef.Apply();
    }

    void RenderSoundSourceAngleIndicator(double soundSourceAngle, double confidenceLevel)
    {
        ClearSoundSourceAngleIndicatorArea();

        Color indicatorColor = soundSourceAngleDrawColor;
        indicatorColor.a = Mathf.Max(0.3f, (float)confidenceLevel);

        uint centerX = (uint)ConvertBeamAngleToXPos(soundSourceAngle);
        int xMin = (int)_soundSourceAngleIndicatorArea.xMin;
        int xMax = (int)_soundSourceAngleIndicatorArea.xMax - 1;
        int yMin = (int)_soundSourceAngleIndicatorArea.yMin;

        uint blockWidth = 9;
        uint blockHeight = (uint)_soundSourceAngleIndicatorArea.height;

        int tempX = (int)(centerX - blockWidth * 0.5f);
        if (tempX < xMin) { tempX = xMin; }
        else if (tempX > xMax - blockWidth) { tempX = xMax - (int)blockWidth; }
        uint x = (uint)tempX;

        uint y = (uint)yMin;

        Color[] colors = new Color[blockWidth * blockHeight];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = indicatorColor;
        }

        _textureRef.SetPixels((int)x, (int)y, (int)blockWidth, (int)blockHeight, colors);

        _textureRef.Apply();
    }

    void ClearBeamAngleIndicatorArea()
    {
        Rect r = _beamAngleIndicatorArea;
        _textureRef.SetPixels((int)r.xMin, (int)r.yMin, (int)r.width, (int)r.height, _blankCanvas, 0);
    }

    void ClearSoundSourceAngleIndicatorArea()
    {
        Rect r = _soundSourceAngleIndicatorArea;
        _textureRef.SetPixels((int)r.xMin, (int)r.yMin, (int)r.width, (int)r.height, _blankCanvas, 0);
    }

    void ClearTexture()
    {
        _textureRef.SetPixels(_blankCanvas, 0);
    }

    #endregion


    #region OnGUI

    void OnGUI()
    {
        GUI.DrawTexture(GetRenderingArea(), _textureRef);
    }

    Rect GetRenderingArea()
    {
        Rect r;
        if (null != getRenderingAreaHandler)
        {
            r = getRenderingAreaHandler(this);
        }
        else
        {
            r = GetDefaultRenderingArea();
        }
        return r;
    }

    Rect GetDefaultRenderingArea()
    {
        float tW = textureWidth;
        float tH = textureHeight;
        float sW = Screen.width;
        float sH = Screen.height;

        // Define Bottom-Center Area
        float yPad = 10;
        float x = 0.5f * (sW - tW);
        float y = sH - tH - yPad;

        return new Rect(x, y, tW, tH);
    }

    #endregion


    #region Helper Methods

    float ConvertBeamAngleToXPos(double beamAngle)
    {
        float minAngle = (float)ZigKinectAudioSource.MinBeamAngle;
        float maxAngle = (float)ZigKinectAudioSource.MaxBeamAngle;
        return MathHelper.ConvertFromRangeToRange(minAngle, maxAngle, 0, _beamAngleIndicatorArea.width, (float)beamAngle);
    }
    float ConvertSoundSourceAngleToXPos(double soundSourceAngle)
    {
        float minAngle = (float)ZigKinectAudioSource.MinSoundSourceAngle;
        float maxAngle = (float)ZigKinectAudioSource.MaxSoundSourceAngle;
        return MathHelper.ConvertFromRangeToRange(minAngle, maxAngle, 0, _soundSourceAngleIndicatorArea.width, (float)soundSourceAngle);
    }

    #endregion
}
