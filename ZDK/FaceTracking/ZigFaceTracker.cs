using UnityEngine;
using System;
using System.Runtime.InteropServices;
using Zigfu.Utility;


namespace Zigfu.FaceTracking
{
    public class ZigFaceTracker : Singleton<ZigFaceTracker>
    {
        const String ClassName = "ZigFaceTracker";

        public const FaceTrackingImageFormat VideoFormat = FaceTrackingImageFormat.FTIMAGEFORMAT_UINT8_B8G8R8X8;
        public const FaceTrackingImageFormat DepthFormat = FaceTrackingImageFormat.FTIMAGEFORMAT_UINT16_D13P3;


        public ZigFaceTransform FaceTransform { get; private set; }
        public ZigFaceTransform FaceTransformMirrored { get; private set; }

        public bool TrackSucceeded { 
            get { return (Result != null && Result.Succeeded); }
        }

        [SerializeField]
        bool _verbose = false;
        public bool Verbose
        {
            get { return _verbose; }
            set
            {
                _verbose = value;
                FTFaceTracker.verbose = _verbose;
                FTImage.verbose = _verbose;
                FTModel.verbose = _verbose;
                FTResult.verbose = _verbose;
            }
        }


        public FTFaceTracker Tracker { get; private set; }
        public FTResult Result { get; private set; }

        public FTImage ColorImage { get; private set; }
        public FTImage DepthImage { get; private set; }

        public CameraConfig VideoCamConfig { get; private set; }
        public CameraConfig DepthCamConfig { get; private set; }


        #region Init and Destroy

        void Awake()
        {
            LogStatus("Awake");

            Verbose = _verbose;

            FaceTransform = new ZigFaceTransform();
            FaceTransformMirrored = new ZigFaceTransform();
        }

        public void Initialize(
            ZigImage zigVideo, 
            ZigDepth zigDepth, 
            IntPtr colorImagePtr, 
            IntPtr depthImagePtr)
        {
            LogStatus("Initialize");

            VideoCamConfig = new CameraConfig((uint)zigVideo.xres, (uint)zigVideo.yres, 0, VideoFormat);
            DepthCamConfig = new CameraConfig((uint)zigDepth.xres, (uint)zigDepth.yres, 0, DepthFormat);

            Tracker = new FTFaceTracker();
            Tracker.Initialize(VideoCamConfig, DepthCamConfig, IntPtr.Zero, null);

            ColorImage = new FTImage(VideoCamConfig, colorImagePtr);
            DepthImage = new FTImage(DepthCamConfig, depthImagePtr);

            Result = Tracker.CreateFTResult();
        }

        #endregion


        #region Update and Track

        public void UpdateVideoFrame(IntPtr nuiVideoFramePtr)
        {
            ColorImage.CopyFrom(nuiVideoFramePtr);
        }
        public void UpdateDepthFrame(IntPtr nuiDepthFramePtr)
        {
            DepthImage.CopyFrom(nuiDepthFramePtr);
        }

        public void Track()
        {
            var sensorData = new SensorData(ColorImage, DepthImage, 1.0f, Point.Empty);
            FaceTrackingSensorData ftSensorData = sensorData.FaceTrackingSensorData;

            bool oldTrackSucceeded = TrackSucceeded;
            if (oldTrackSucceeded)  { Tracker.ContinueTracking(ref ftSensorData, Result); }
            else                    { Tracker.StartTracking(ref ftSensorData, Result); }

            if (TrackSucceeded != oldTrackSucceeded)
            {
                if (TrackSucceeded) { OnFaceDetected(); }
                else                { OnFaceLost(); }
            }

            UpdateFaceTransform();
        }

        void OnFaceDetected()
        {
            LogStatus("OnFaceDetected");
        }
        void OnFaceLost()
        {
            LogStatus("OnFaceLost");
        }

        void UpdateFaceTransform()
        {
            if (!TrackSucceeded) { return; }

            ZigFaceTransform newT = new ZigFaceTransform();

            float scale;
            Vector3DF rotationXYZ = new Vector3DF();
            Vector3DF translationXYZ = new Vector3DF();

            Result.Get3DPose(out scale, out rotationXYZ, out translationXYZ);

            newT.SetPosition(translationXYZ);
            newT.position.z *= -1;

            newT.SetEulerAngles(rotationXYZ);
            newT.eulerAngles.x *= -1;
            newT.eulerAngles.y *= -1;

            FaceTransform = newT;


            // Mirrored Transform

            ZigFaceTransform mirrorT = new ZigFaceTransform();

            mirrorT.position.x = newT.position.x * -1;
            mirrorT.position.y = newT.position.y;
            mirrorT.position.z = newT.position.z;

            mirrorT.eulerAngles.x = newT.eulerAngles.x;
            mirrorT.eulerAngles.y = newT.eulerAngles.y * -1;
            mirrorT.eulerAngles.z = newT.eulerAngles.z * -1;

            FaceTransformMirrored = mirrorT;
        }

        #endregion


        #region Helper

        /// <summary>
        /// Returns Animation Units (AUs) coefficients. These coefficients represent deformations 
        /// of the 3D mask caused by the moving parts of the face (mouth, eyebrows, etc). Use the 
        /// AnimationUnit enum to index these co-efficients
        /// </summary>
        /// <returns>
        /// The animation unit coefficients.
        /// </returns>
        public EnumIndexableCollection<AnimationUnit, float> GetAnimationUnitCoefficients()
        {
            if (!TrackSucceeded) { return null; }

            IntPtr animUnitCoeffsPtr;
            uint pointsCount;
            Result.GetAUCoefficients(out animUnitCoeffsPtr, out pointsCount);
            float[] animUnitCoeffs = null;
            if (pointsCount > 0)
            {
                animUnitCoeffs = new float[pointsCount];
                Marshal.Copy(animUnitCoeffsPtr, animUnitCoeffs, 0, animUnitCoeffs.Length);
            }

            return new EnumIndexableCollection<AnimationUnit, float>(animUnitCoeffs);
        }

        public static void PrintAnimCoefs(EnumIndexableCollection<AnimationUnit, float> animCoefs)
        {
            string output = "";
            foreach (AnimationUnit au in Enum.GetValues(typeof(AnimationUnit)))
            {
                String valStr = animCoefs[au].ToString("0.00");
                output += au + ":  " + valStr + "\n";
            }
            Debug.Log(output);
        }

        void LogStatus(String msg)
        {
            if (!_verbose) { return; }
            Debug.Log(ClassName + ":: " + msg);
        }

        #endregion

    }
}
