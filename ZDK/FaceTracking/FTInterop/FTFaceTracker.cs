using System;
using System.Runtime.InteropServices;


namespace Zigfu.FaceTracking
{

    public class FTFaceTracker : Zigfu.Utility.NativeObjectWrapper
    {
        override protected String ClassName { get { return "FTFaceTracker"; } }

        public FTFaceTracker() : this(NativeMethods.FTCreateFaceTracker(IntPtr.Zero)) { }
        public FTFaceTracker(IntPtr p) : base(p) { }
        ~FTFaceTracker() { Release(); }

        
        #region DLL Imports

        const String DLL_PATH = FtInterop.FacetrackingWrapperDll;

        [DllImport(DLL_PATH)]  static extern int FT_Release(IntPtr This);

        [DllImport(DLL_PATH)]  static extern int FT_Initialize(IntPtr This, CameraConfig videoCameraConfig, CameraConfig depthCameraConfig, IntPtr depthToColorMappingFunc, string modelPath);
        [DllImport(DLL_PATH)]  static extern int FT_Reset(IntPtr This);
        [DllImport(DLL_PATH)]  static extern int FT_CreateFTResult(IntPtr This, out IntPtr faceTrackResult);
        [DllImport(DLL_PATH)]  static extern int FT_SetShapeUnits(IntPtr This, float scale, float[] shapeUnitCoeffsPtr, uint shapeUnitCount);
        [DllImport(DLL_PATH)]  static extern int FT_GetShapeUnits(IntPtr This, out float scale, out IntPtr shapeUnitCoeffsPtr, [In, Out] ref uint shapeUnitCount, [MarshalAs(UnmanagedType.Bool)] out bool haveConverged);
        [DllImport(DLL_PATH)]  static extern int FT_SetShapeComputationState(IntPtr This, [MarshalAs(UnmanagedType.Bool)] bool isEnabled);
        [DllImport(DLL_PATH)]  static extern int FT_GetComputationState(IntPtr This, [MarshalAs(UnmanagedType.Bool)] out bool isEnabled);
        [DllImport(DLL_PATH)]  static extern int FT_GetFaceModel(IntPtr This, out IntPtr model);
        [DllImport(DLL_PATH)]  static extern int FT_StartTracking(IntPtr This, ref FaceTrackingSensorData sensorData, ref Rect roi, HeadPoints headPoints, IntPtr faceTrackResult);
        [DllImport(DLL_PATH)]  static extern int FT_StartTracking_Default(IntPtr This, ref FaceTrackingSensorData sensorData, IntPtr faceTrackResult);
        [DllImport(DLL_PATH)]  static extern int FT_ContinueTracking(IntPtr This, ref FaceTrackingSensorData sensorData, HeadPoints headPoints, IntPtr faceTrackResult);
        [DllImport(DLL_PATH)]  static extern int FT_ContinueTracking_Default(IntPtr This, ref FaceTrackingSensorData sensorData, IntPtr faceTrackResult);
        [DllImport(DLL_PATH)]  static extern int FT_DetectFaces(IntPtr This, ref FaceTrackingSensorData sensorData, ref Rect roi, IntPtr faces, ref uint facesCount);

        #endregion


        #region Wrapped Native Function Calls

        public void Release()
        {
            LogStatus("FT_Release");
            FT_Release(p);
        }


        public void Initialize(CameraConfig videoCameraConfig, CameraConfig depthCameraConfig, IntPtr depthToColorMappingFunc, string modelPath)
        {
            LogStatus("Initialize");
            int hr = FT_Initialize(p, videoCameraConfig, depthCameraConfig, depthToColorMappingFunc, modelPath);
            FtInterop.EvaluateHR(hr);
        }

        public void Reset()
        {
            LogStatus("Reset");
            int hr = FT_Reset(p);
            FtInterop.EvaluateHR(hr);
        }

        public FTResult CreateFTResult()
        {
            LogStatus("CreateFTResult");

            IntPtr nativeResult;
            int hr = FT_CreateFTResult(p, out nativeResult);
            FtInterop.EvaluateHR(hr);

            return new FTResult(nativeResult);
        }

        public void SetShapeUnits(float scale, float[] shapeUnitCoeffsPtr, uint shapeUnitCount)
        {
            LogStatus("SetShapeUnits");
            int hr = FT_SetShapeUnits(p, scale, shapeUnitCoeffsPtr, shapeUnitCount);
            FtInterop.EvaluateHR(hr);
        }

        public void GetShapeUnits(out float scale, out IntPtr shapeUnitCoeffsPtr, [In, Out] ref uint shapeUnitCount, [MarshalAs(UnmanagedType.Bool)] out bool haveConverged)
        {
            LogStatus("GetShapeUnits");
            int hr = FT_GetShapeUnits(p, out scale, out shapeUnitCoeffsPtr, ref shapeUnitCount, out haveConverged);
            FtInterop.EvaluateHR(hr);
        }

        public void SetShapeComputationState([MarshalAs(UnmanagedType.Bool)] bool isEnabled)
        {
            LogStatus("SetShapeComputationState");
            int hr = FT_SetShapeComputationState(p, isEnabled);
            FtInterop.EvaluateHR(hr);
        }

        public void GetComputationState([MarshalAs(UnmanagedType.Bool)] out bool isEnabled)
        {
            LogStatus("GetComputationState");
            int hr = FT_GetComputationState(p, out isEnabled);
            FtInterop.EvaluateHR(hr);
        }

        public FTModel GetFaceModel()
        {
            LogStatus("GetFaceModel");

            IntPtr nativeModel;
            int hr = FT_GetFaceModel(p, out nativeModel);
            FtInterop.EvaluateHR(hr);

            return new FTModel(nativeModel);
        }

        public void StartTracking(ref FaceTrackingSensorData sensorData, ref Rect roi, HeadPoints headPoints, FTResult faceTrackResult)
        {
            LogStatus("StartTracking");
            int hr = FT_StartTracking(p, ref sensorData, ref roi, headPoints, faceTrackResult.NativeObject);
            FtInterop.EvaluateHR(hr);
        }
        public void StartTracking(ref FaceTrackingSensorData sensorData, FTResult faceTrackResult)
        {
            LogStatus("FT_StartTracking_Default");
            int hr = FT_StartTracking_Default(p, ref sensorData, faceTrackResult.NativeObject);
            FtInterop.EvaluateHR(hr);
        }

        public void ContinueTracking(ref FaceTrackingSensorData sensorData, HeadPoints headPoints, FTResult faceTrackResult)
        {
            LogStatus("ContinueTracking");
            int hr = FT_ContinueTracking(p, ref sensorData, headPoints, faceTrackResult.NativeObject);
            FtInterop.EvaluateHR(hr);
        }
        public void ContinueTracking(ref FaceTrackingSensorData sensorData, FTResult faceTrackResult)
        {
            LogStatus("ContinueTracking");
            int hr = FT_ContinueTracking_Default(p, ref sensorData, faceTrackResult.NativeObject);
            FtInterop.EvaluateHR(hr);
        }

        public void DetectFaces(ref FaceTrackingSensorData sensorData, ref Rect roi, IntPtr faces, ref uint facesCount)
        {
            LogStatus("DetectFaces");
            int hr = FT_DetectFaces(p, ref sensorData, ref roi, faces, ref facesCount);
            FtInterop.EvaluateHR(hr);
        }

        #endregion

    }

}
