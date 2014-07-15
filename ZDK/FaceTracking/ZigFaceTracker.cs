namespace Zigfu.FaceTracking
{
    using UnityEngine;
    using System;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using Zigfu.Utility;


    public class ZigFaceTracker : MonoBehaviour
    {
        const String ClassName = "ZigFaceTracker";

        const float NominalFocalLengthInPixels = 285.63f;

        // A constant zoom factor is used for now, since Windows Kinect does not support different zoom levels.
        internal const float DefaultZoomFactor = 1.0f;

        public const FaceTrackingImageFormat VideoFormat = FaceTrackingImageFormat.FTIMAGEFORMAT_UINT8_B8G8R8X8;
        public const FaceTrackingImageFormat DepthFormat = FaceTrackingImageFormat.FTIMAGEFORMAT_UINT16_D13P3;


        public ZigFaceTransform FaceTransform { get; private set; }
        public ZigFaceTransform FaceTransformMirrored { get; private set; }

        public bool TrackSucceeded { 
            get { return (_frame.TrackSuccessful); }
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


        public FTImage ColorImage { get; private set; }
        public FTImage DepthImage { get; private set; }

        public CameraConfig ColorCameraConfig { get; private set; }
        public CameraConfig DepthCameraConfig { get; private set; }


        FTFaceTracker _faceTrackerInteropPtr;
        internal FTFaceTracker FaceTrackerPtr
        {
            get
            {
                return _faceTrackerInteropPtr;
            }
        }

        ZigFaceTrackFrame _frame;
        public ZigFaceTrackFrame FaceTrackFrame
        {
            get
            {
                return _frame;
            }
        }

        ZigFaceModel _faceModel;
        internal ZigFaceModel FaceModel
        {
            get
            {
                this.CheckPtrAndThrow();
                if (this._faceModel == null)
                {
                    FTModel faceTrackModelPtr;
                    faceTrackModelPtr = this._faceTrackerInteropPtr.GetFaceModel();
                    this._faceModel = new ZigFaceModel(this, faceTrackModelPtr);
                }

                return this._faceModel;
            }
        }


        #region Init and Destroy

        public static ZigFaceTracker Instance
        {
            get { return Singleton<ZigFaceTracker>.Instance; }
        }
        public static bool InstanceExists
        {
            get { return Singleton<ZigFaceTracker>.InstanceExists; }
        }


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

            ColorCameraConfig = new CameraConfig((uint)zigVideo.xres, (uint)zigVideo.yres, NominalFocalLengthInPixels, VideoFormat);
            DepthCameraConfig = new CameraConfig((uint)zigDepth.xres, (uint)zigDepth.yres, NominalFocalLengthInPixels, DepthFormat);

            _faceTrackerInteropPtr = new FTFaceTracker();
            _faceTrackerInteropPtr.Initialize(ColorCameraConfig, DepthCameraConfig, IntPtr.Zero, null);

            try
            {
                this._frame = this.CreateResult();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.CurrentCulture, "Failed to create face tracking result.", e));
            }

            ColorImage = new FTImage(ColorCameraConfig, colorImagePtr);
            DepthImage = new FTImage(DepthCameraConfig, depthImagePtr);
        }

        #endregion


        #region Update and Track

        void Update()
        {
            Verbose = _verbose;
        }

        public void UpdateVideoFrame(IntPtr nuiVideoFramePtr)
        {
            ColorImage.CopyFrom(nuiVideoFramePtr);
        }
        public void UpdateDepthFrame(IntPtr nuiDepthFramePtr)
        {
            DepthImage.CopyFrom(nuiDepthFramePtr);
        }

        public ZigFaceTrackFrame Track()
        {
            var sensorData = new SensorData(ColorImage, DepthImage, 1.0f, Point.Empty);
            FaceTrackingSensorData ftSensorData = sensorData.FaceTrackingSensorData;

            bool oldTrackSucceeded = TrackSucceeded;
            if (oldTrackSucceeded) 
                { _faceTrackerInteropPtr.ContinueTracking(ref ftSensorData, this._frame.ResultPtr); }
            else 
                { _faceTrackerInteropPtr.StartTracking(ref ftSensorData, this._frame.ResultPtr); }

            if (TrackSucceeded != oldTrackSucceeded)
            {
                if (TrackSucceeded) { OnFaceDetected(); }
                else                { OnFaceLost(); }
            }

            UpdateFaceTransform();

            return _frame;
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

            this._frame.ResultPtr.Get3DPose(out scale, out rotationXYZ, out translationXYZ);

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


        /// <summary>
        /// Creates a frame object instance. Can be used for caching of the face tracking
        /// frame. FaceTrackFrame should be disposed after use.
        /// </summary>
        /// <returns>
        /// newly created frame object
        /// </returns>
        internal ZigFaceTrackFrame CreateResult()
        {
            FTResult faceTrackResultPtr;
            ZigFaceTrackFrame faceTrackFrame = null;

            this.CheckPtrAndThrow();
            faceTrackResultPtr = this._faceTrackerInteropPtr.CreateFTResult();
            if (faceTrackResultPtr != null)
            {
                faceTrackFrame = new ZigFaceTrackFrame(faceTrackResultPtr, this);
            }

            return faceTrackFrame;
        }


        #region Helper

        private void CheckPtrAndThrow()
        {
            if (this._faceTrackerInteropPtr == null)
            {
                throw new InvalidOperationException("Native face tracker pointer in invalid state.");
            }
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
