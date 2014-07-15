namespace Zigfu.FaceTracking
{
    using UnityEngine;
    using System;
    using System.Runtime.InteropServices;


    public static class FtInterop
    {
        internal const string FaceTrackLibDll = "FaceTrackLib.dll";
        internal const string FacetrackingWrapperDll = "NativeFaceTrackLibWrapper.dll";


        public static bool EvaluateHR(int hr, bool throwOnFail = true, bool logError = true)
        {
            if (hr == 0) { return true; }

            if (logError) { UnityEngine.Debug.LogError(FtInterop.DecodeHR(hr)); }
            if (throwOnFail) { throw new Exception(FtInterop.DecodeHR(hr)); }

            return false;
        }

        public static string DecodeHR(int hr)
        {
            string str = string.Empty;
            if (!Enum.IsDefined(typeof(ErrorCode), hr))
            {
                str = "Unrecognized code: " + hr;
            }
            else
            {
                str = Enum.GetName(typeof(ErrorCode), hr);
            }
            return str;
        }
    }

    public static class NativeMethods
    {
        [DllImport(FtInterop.FaceTrackLibDll, CharSet = CharSet.Unicode)]
        public static extern IntPtr FTCreateFaceTracker(IntPtr reserved);

        [DllImport(FtInterop.FaceTrackLibDll, CharSet = CharSet.Unicode)]
        public static extern IntPtr FTCreateImage();
    }


    /// <summary>
    /// Indices for Animation Units Coefficients
    /// </summary>
    public enum AnimationUnit
    {
        LipRaiser,
        JawLower,
        LipStretcher,
        BrowLower,
        LipCornerDepressor,
        BrowRaiser
    }

    /// <summary>
    /// Indices for feature points in 3D shape and projected 3D shape with
    /// descriptive names to identify them easily
    /// </summary>
    public enum FeaturePoint
    {
        TopSkull = 0,
        TopRightForehead = 1,
        MiddleTopDipUpperLip = 7,
        AboveChin = 9,
        BottomOfChin = 10,
        RightOfRightEyebrow = 15,
        MiddleTopOfRightEyebrow = 16,
        LeftOfRightEyebrow = 17,
        MiddleBottomOfRightEyebrow = 18,
        AboveMidUpperRightEyelid = 19,
        OuterCornerOfRightEye = 20,
        MiddleTopRightEyelid = 21,
        MiddleBottomRightEyelid = 22,
        InnerCornerRightEye = 23,
        UnderMidBottomRightEyelid = 24,
        RightSideOfChin = 30,
        OutsideRightCornerMouth = 31,
        RightOfChin = 32,
        RightTopDipUpperLip = 33,
        TopLeftForehead = 34,
        MiddleTopLowerLip = 40,
        MiddleBottomLowerLip = 41,
        LeftOfLeftEyebrow = 48,
        MiddleTopOfLeftEyebrow = 49,
        RightOfLeftEyebrow = 50,
        MiddleBottomOfLeftEyebrow = 51,
        AboveMidUpperLeftEyelid = 52,
        OuterCornerOfLeftEye = 53,
        MiddleTopLeftEyelid = 54,
        MiddleBottomLeftEyelid = 55,
        InnerCornerLeftEye = 56,
        UnderMidBottomLeftEyelid = 57,
        LeftSideOfCheek = 63,
        OutsideLeftCornerMouth = 64,
        LeftOfChin = 65,
        LeftTopDipUpperLip = 66,
        OuterTopRightPupil = 67,
        OuterBottomRightPupil = 68,
        OuterTopLeftPupil = 69,
        OuterBottomLeftPupil = 70,
        InnerTopRightPupil = 71,
        InnerBottomRightPupil = 72,
        InnerTopLeftPupil = 73,
        InnerBottomLeftPupil = 74,
        RightTopUpperLip = 79,
        LeftTopUpperLip = 80,
        RightBottomUpperLip = 81,
        LeftBottomUpperLip = 82,
        RightTopLowerLip = 83,
        LeftTopLowerLip = 84,
        RightBottomLowerLip = 85,
        LeftBottomLowerLip = 86,
        MiddleBottomUpperLip = 87,
        LeftCornerMouth = 88,
        RightCornerMouth = 89,
        BottomOfRightCheek = 90,
        BottomOfLeftCheek = 91,
        AboveThreeFourthRightEyelid = 95,
        AboveThreeFourthLeftEyelid = 96,
        ThreeFourthTopRightEyelid = 97,
        ThreeFourthTopLeftEyelid = 98,
        ThreeFourthBottomRightEyelid = 99,
        ThreeFourthBottomLeftEyelid = 100,
        BelowThreeFourthRightEyelid = 101,
        BelowThreeFourthLeftEyelid = 102,
        AboveOneFourthRightEyelid = 103,
        AboveOneFourthLeftEyelid = 104,
        OneFourthTopRightEyelid = 105,
        OneFourthTopLeftEyelid = 106,
        OneFourthBottomRightEyelid = 107,
        OneFourthBottomLeftEyelid = 108
    }

    public enum ErrorCode
    {
        Success = 0,                            // S_OK - No Error. Success.
        InvalidModels = unchecked((int)0x8fac0001),   // FT_ERROR_INVALID_MODELS - Returned when the face tracking models loaded by the tracking engine have incorrect format
        InvalidInputImage = unchecked((int)0x8fac0002),   // FT_ERROR_INVALID_INPUT_IMAGE - Returned when passed input image is invalid
        FaceDetectorFailed = unchecked((int)0x8fac0003),   // FT_ERROR_FACE_DETECTOR_FAILED - Returned when face tracking fails due to face detection errors
        ActiveAppearanceModelFailed = unchecked((int)0x8fac0004),   // FT_ERROR_AAM_FAILED - Returned when face tracking fails due to errors in tracking individual face parts
        NeuralNetworkFailed = unchecked((int)0x8fac0005),   // FT_ERROR_NN_FAILED - Returned when face tracking fails due to inability of the Neural Network to find nose, mouth corners and eyes
        FaceTrackerUninitialized = unchecked((int)0x8fac0006),   // FT_ERROR_UNINITIALIZED - Returned when uninitialized face tracker is used
        InvalidModelPath = unchecked((int)0x8fac0007),   // FT_ERROR_INVALID_MODEL_PATH - Returned when a file path to the face model files is invalid or when the model files could not be located
        EvaluationFailed = unchecked((int)0x8fac0008),   // FT_ERROR_EVAL_FAILED - Returned when face tracking worked but later evaluation found that the quality of the results was poor
        InvalidCameraConfig = unchecked((int)0x8fac0009),   // FT_ERROR_INVALID_CAMERA_CONFIG - Returned when the passed camera configuration is invalid
        Invalid3DHint = unchecked((int)0x8fac000a),   // FT_ERROR_INVALID_3DHINT - Returned when the passed 3D hint vectors contain invalid values (for example out of range)
        HeadSearchFailed = unchecked((int)0x8fac000b),   // FT_ERROR_HEAD_SEARCH_FAILED - Returned when the system cannot find the head area in the passed data based on passed 3D hint vectors or region of interest rectangle
        UserLost = unchecked((int)0x8fac000c),   // FT_ERROR_USER_LOST - Returned when the user ID of the subject being tracked is switched or lost so we should call StartTracking on next call for tracking face
        KinectDllLoadFailed = unchecked((int)0x8fac000d),    // FT_ERROR_KINECT_DLL_FAILED - Returned when Kinect DLL failed to load

        InvalidArg = unchecked((int)0x80070057),
        InvalidPointer = unchecked((int)0x80004003)
    };

    public enum FaceTrackingImageFormat
    {
        FTIMAGEFORMAT_INVALID = 0,    // Invalid format
        FTIMAGEFORMAT_UINT8_GR8 = 1,    // Grayscale image where each pixel is 1 byte (or 8 bits). 
        FTIMAGEFORMAT_UINT8_R8G8B8 = 2,    // RGB image (same as ARGB but without an alpha channel).
        FTIMAGEFORMAT_UINT8_X8R8G8B8 = 3,    // Same as ARGB (the alpha channel byte is present but not used). 
        FTIMAGEFORMAT_UINT8_A8R8G8B8 = 4,    // ARGB format (the first byte is the alpha transparency channel; remaining bytes are 8-bit red, green, and blue channels). 
        FTIMAGEFORMAT_UINT8_B8G8R8X8 = 5,    // Same as BGRA (the alpha channel byte is present but not used). 
        FTIMAGEFORMAT_UINT8_B8G8R8A8 = 6,    // BGRA format (the last byte is the alpha transparency channel; remaining bytes are 8-bit red, green, and blue channels). 
        FTIMAGEFORMAT_UINT16_D16 = 7,    // 16-bit per pixel depth data that represents the distance to a pixel in millimeters. 
        FTIMAGEFORMAT_UINT16_D13P3 = 8     // 16-bit per pixel depth data that represents the distance to a pixel in millimeters. The last three bits represent the user ID (Kinect's depth data format).
    }


    public class ZigFaceTransform
    {
        public Vector3 position = Vector3.zero;
        public Vector3 eulerAngles = Vector3.zero;

        public void SetPosition(Vector3DF vec3DF)
        {
            position.x = vec3DF.X;
            position.y = vec3DF.Y;
            position.z = vec3DF.Z;
        }
        public void SetEulerAngles(Vector3DF vec3DF)
        {
            eulerAngles.x = vec3DF.X;
            eulerAngles.y = vec3DF.Y;
            eulerAngles.z = vec3DF.Z;
        }


        public static ZigFaceTransform Lerp(ZigFaceTransform from, ZigFaceTransform to, float t)
        {
            ZigFaceTransform result = new ZigFaceTransform();

            result.position = Vector3.Lerp(from.position, to.position, t);

            Quaternion oldRotQ = Quaternion.Euler(from.eulerAngles);
            Quaternion targetRotQ = Quaternion.Euler(to.eulerAngles);
            result.eulerAngles = Quaternion.Lerp(oldRotQ, targetRotQ, t).eulerAngles;

            return result;
        }

        public override string ToString()
        {
            Vector3 pos = position;
            Vector3 rot = eulerAngles;
            String str = "ZigFaceTransform:\n"
                        + "  position:     ("
                            + pos.x.ToString("0.00") + ", "
                            + pos.y.ToString("0.00") + ", "
                            + pos.z.ToString("0.00") + ")\t\t"
                        + "  eulerAngles:  ("
                            + rot.x.ToString("0.00") + ", "
                            + rot.y.ToString("0.00") + ", "
                            + rot.z.ToString("0.00") + ")";
            return str;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class CameraConfig
    {
        /// <summary>
        /// Max width or height of the camera input frames in pixels supported by FT API. This allows to use cameras up to 256 megapixels. 
        /// </summary>
        public const uint MaxResolution = 16384;

        // Note that camera pixels should be square
        private readonly uint width;            // in pixels, allowed range - 1-UINT_MAX
        private readonly uint height;           // in pixels, allowed range - 1-UINT_MAX
        private readonly float focalLength;      // in pixels, allowed range - 0-FLOAT_MAX, where 0 value means - use an average focal length for modern video cameras
        private readonly FaceTrackingImageFormat imageFormat;
        private readonly uint bytesPerPixel;
        private readonly uint stride;
        private readonly uint frameBufferLength;

        public CameraConfig(uint width, uint height, float focalLength, FaceTrackingImageFormat imageFormat)
        {
            this.width = width;
            this.height = height;
            this.focalLength = focalLength;
            this.imageFormat = imageFormat;

            //this.bytesPerPixel = Image.FormatToSize(this.imageFormat);
            this.bytesPerPixel = FormatToSize(this.imageFormat);

            this.stride = this.width * this.bytesPerPixel;

            switch (this.imageFormat)
            {
                case FaceTrackingImageFormat.FTIMAGEFORMAT_UINT8_GR8:
                case FaceTrackingImageFormat.FTIMAGEFORMAT_UINT8_R8G8B8:
                case FaceTrackingImageFormat.FTIMAGEFORMAT_UINT8_X8R8G8B8:
                case FaceTrackingImageFormat.FTIMAGEFORMAT_UINT8_A8R8G8B8:
                case FaceTrackingImageFormat.FTIMAGEFORMAT_UINT8_B8G8R8X8:
                case FaceTrackingImageFormat.FTIMAGEFORMAT_UINT8_B8G8R8A8:
                    this.frameBufferLength = this.height * this.stride;
                    break;
                case FaceTrackingImageFormat.FTIMAGEFORMAT_UINT16_D16:
                case FaceTrackingImageFormat.FTIMAGEFORMAT_UINT16_D13P3:
                    this.frameBufferLength = this.height * this.width;
                    break;
                default:
                    throw new ArgumentException("Invalid image format specified");
            }
        }

        public static uint FormatToSize(FaceTrackingImageFormat format)
        {
            switch (format)
            {
                case FaceTrackingImageFormat.FTIMAGEFORMAT_INVALID:
                    return 0;
                case FaceTrackingImageFormat.FTIMAGEFORMAT_UINT8_GR8:
                    return 1;
                case FaceTrackingImageFormat.FTIMAGEFORMAT_UINT8_R8G8B8:
                    return 3;
                case FaceTrackingImageFormat.FTIMAGEFORMAT_UINT8_X8R8G8B8:
                    return 4;
                case FaceTrackingImageFormat.FTIMAGEFORMAT_UINT8_A8R8G8B8:
                    return 4;
                case FaceTrackingImageFormat.FTIMAGEFORMAT_UINT8_B8G8R8X8:
                    return 4;
                case FaceTrackingImageFormat.FTIMAGEFORMAT_UINT8_B8G8R8A8:
                    return 4;
                case FaceTrackingImageFormat.FTIMAGEFORMAT_UINT16_D16:
                    return 2;
                case FaceTrackingImageFormat.FTIMAGEFORMAT_UINT16_D13P3:
                    return 2;
                default:
                    throw new ArgumentException("Invalid image format specified");
            }
        }

        /// <summary>
        /// Width in pixels, allowed range - 1-MaxResolution
        /// </summary>
        public uint Width
        {
            get { return this.width; }
        }

        /// <summary>
        /// Height in pixels, allowed range - 1-MaxResolution
        /// </summary>
        public uint Height
        {
            get { return this.height; }
        }

        public FaceTrackingImageFormat ImageFormat
        {
            get { return this.imageFormat; }
        }

        public uint Stride
        {
            get { return this.stride; }
        }

        public uint FrameBufferLength
        {
            get { return this.frameBufferLength; }
        }
    }

    public class SensorData
    {
        private readonly FTImage videoFrame;
        private readonly FTImage depthFrame;
        private readonly float zoomFactor;
        private readonly Point viewOffset;

        public SensorData(FTImage videoFrame, FTImage depthFrame, float zoomFactor, Point viewOffset)
        {
            this.videoFrame = videoFrame;
            this.depthFrame = depthFrame;
            this.zoomFactor = zoomFactor;
            this.viewOffset = viewOffset;
        }

        internal FaceTrackingSensorData FaceTrackingSensorData
        {
            get
            {
                var faceTrackSensorData = new FaceTrackingSensorData
                {
                    VideoFrame = this.videoFrame != null ? this.videoFrame.NativeObject : IntPtr.Zero,
                    DepthFrame = this.depthFrame != null ? this.depthFrame.NativeObject : IntPtr.Zero,
                    ZoomFactor = this.zoomFactor,
                    ViewOffset = this.viewOffset
                };

                return faceTrackSensorData;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FaceTrackingSensorData
    {
        public IntPtr VideoFrame;  // video frame from a video camera
        public IntPtr DepthFrame;  // depth frame (optional) from the depth camera
        public float ZoomFactor;   // video frame zoom factor (it is 1.0f if there is no zoom)
        public Point ViewOffset;   // X, Y coordinates of the top-left corner of the view area in the camera video frame (hardware resolution could be higher than what is being processed by this API)
    }

    [StructLayout(LayoutKind.Sequential)]
    public class HeadPoints
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        private Vector3DF[] points;

        public Vector3DF[] Points
        {
            set { this.points = value; }
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct FaceTriangle
    {
        private int first;  // index of first vertex
        private int second;  // index of second vertex
        private int third;  // index of third vertex

        public FaceTriangle(int first, int second, int third)
        {
            this.first = first;
            this.second = second;
            this.third = third;
        }

        /// <summary>
        /// Index of the first vertex
        /// </summary>
        public int First
        {
            get { return this.first; }
            set { this.first = value; }
        }

        /// <summary>
        /// Index of the second vertex
        /// </summary>
        public int Second
        {
            get { return this.second; }
            set { this.second = value; }
        }

        /// <summary>
        /// Index of the third vertex
        /// </summary>
        public int Third
        {
            get { return this.third; }
            set { this.third = value; }
        }

        public static bool operator ==(FaceTriangle triangle1, FaceTriangle triangle2)
        {
            return triangle1.Equals(triangle2);
        }

        public static bool operator !=(FaceTriangle triangle1, FaceTriangle triangle2)
        {
            return !triangle1.Equals(triangle2);
        }

        public override int GetHashCode()
        {
            return first ^ second ^ third;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is FaceTriangle))
            {
                return false;
            }

            return Equals((FaceTriangle)obj);
        }

        public bool Equals(FaceTriangle other)
        {
            return this.first == other.first && this.second == other.second && this.third == other.third;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PointF
    {
        private readonly float x;
        private readonly float y;

        public PointF(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        /// <summary>
        /// Gets an point with 0,0 co-ordinates
        /// </summary>
        public static PointF Empty
        {
            get { return new PointF(0, 0); }
        }

        /// <summary>
        /// Returns X co-ordinate
        /// </summary>
        public float X
        {
            get { return x; }
        }

        /// <summary>
        /// Returns Y co-ordinate
        /// </summary>
        public float Y
        {
            get { return y; }
        }

        public static bool operator ==(PointF point1, PointF point2)
        {
            return point1.Equals(point2);
        }

        public static bool operator !=(PointF point1, PointF point2)
        {
            return !point1.Equals(point2);
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ y.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is PointF))
            {
                return false;
            }

            return Equals((PointF)obj);
        }

        public bool Equals(PointF other)
        {
            if (x != other.x)
            {
                return false;
            }

            return y == other.y;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Point
    {
        public Point(int x, int y)
            : this()
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Gets an point with 0,0 co-ordinates
        /// </summary>
        public static Point Empty
        {
            get { return new Point(0, 0); }
        }

        /// <summary>
        /// Returns X co-ordinate
        /// </summary>
        public int X { get; private set; }

        /// <summary>
        /// Returns Y co-ordinate
        /// </summary>
        public int Y { get; private set; }

        public static bool operator ==(Point point1, Point point2)
        {
            return point1.Equals(point2);
        }

        public static bool operator !=(Point point1, Point point2)
        {
            return !point1.Equals(point2);
        }

        public override int GetHashCode()
        {
            return X ^ Y;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Point))
            {
                return false;
            }

            return Equals((Point)obj);
        }

        public bool Equals(Point other)
        {
            if (X != other.X)
            {
                return false;
            }

            return Y == other.Y;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public Rect(int left, int top, int right, int bottom)
            : this()
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        /// <summary>
        /// Gets an rectangle with 0 length & height
        /// </summary>
        public static Rect Empty
        {
            get { return new Rect(0, 0, 0, 0); }
        }

        /// <summary>
        /// Left bound of the rectangle
        /// </summary>
        public int Left { get; set; }

        /// <summary>
        /// Top bound of the rectangle
        /// </summary>
        public int Top { get; set; }

        /// <summary>
        /// Right bound of the rectangle
        /// </summary>
        public int Right { get; set; }

        /// <summary>
        /// Bottom bound of the rectangle
        /// </summary>
        public int Bottom { get; set; }

        /// <summary>
        /// Width of the rectangle
        /// </summary>
        public int Width
        {
            get { return Right - Left; }
        }

        /// <summary>
        /// Height of the rectangle
        /// </summary>
        public int Height
        {
            get { return Bottom - Top; }
        }

        public static bool operator ==(Rect point1, Rect point2)
        {
            return point1.Equals(point2);
        }

        public static bool operator !=(Rect point1, Rect point2)
        {
            return !point1.Equals(point2);
        }

        public override int GetHashCode()
        {
            return Left ^ Right ^ Top ^ Bottom;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Rect))
            {
                return false;
            }

            return Equals((Rect)obj);
        }

        public bool Equals(Rect other)
        {
            if (Left != other.Left)
            {
                return false;
            }

            if (Top != other.Top)
            {
                return false;
            }

            if (Right != other.Right)
            {
                return false;
            }

            return Bottom == other.Bottom;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Vector3DF
    {
        public Vector3DF(float x, float y, float z)
            : this()
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3DF Empty
        {
            get
            {
                return new Vector3DF(0.0f, 0.0f, 0.0f);
            }
        }

        public float X { get; set; }

        public float Y { get; set; }

        public float Z { get; set; }

        public static bool operator ==(Vector3DF vector1, Vector3DF vector2)
        {
            return vector1.Equals(vector2);
        }

        public static bool operator !=(Vector3DF vector1, Vector3DF vector2)
        {
            return !vector1.Equals(vector2);
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Vector3DF))
            {
                return false;
            }

            return Equals((Vector3DF)obj);
        }

        public bool Equals(Vector3DF other)
        {
            if (X != other.X)
            {
                return false;
            }

            if (Y != other.Y)
            {
                return false;
            }

            return Z == other.Z;
        }
    }

}
