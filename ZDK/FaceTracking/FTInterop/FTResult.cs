using System;
using System.Runtime.InteropServices;


namespace Zigfu.FaceTracking
{
    public class FTResult : Zigfu.Utility.NativeObjectWrapper
    {
        override protected String ClassName { get { return "FTResult"; } }

        public FTResult(IntPtr p) : base(p) { }
        ~FTResult() { Release(); }


        #region DLL Imports

        const String DLL_PATH = FtInterop.FacetrackingWrapperDll;

        [DllImport(DLL_PATH)]  static extern int FTR_Release(IntPtr This);

        [DllImport(DLL_PATH)]  static extern int FTR_Reset(IntPtr This);
        [DllImport(DLL_PATH)]  static extern int FTR_CopyTo(IntPtr This, [In] FTResult destResult);
        [DllImport(DLL_PATH)]  static extern int FTR_GetStatus(IntPtr This);
        [DllImport(DLL_PATH)]  static extern int FTR_GetFaceRect(IntPtr This, out Rect rect);
        [DllImport(DLL_PATH)]  static extern int FTR_Get2DShapePoints(IntPtr This, out IntPtr pointsPtr, out uint pointCount);
        [DllImport(DLL_PATH)]  static extern int FTR_Get3DPose(IntPtr This, out float scale, out Vector3DF rotationXYZ, out Vector3DF translationXYZ);
        [DllImport(DLL_PATH)]  static extern int FTR_GetAUCoefficients(IntPtr This, out IntPtr animUnitCoeffsPtr, out uint animUnitCount);

        #endregion


        #region Wrapped Native Function Calls

        public void Release()
        {
            LogStatus("FTR_Release");
            FTR_Release(p);
        }


        public void Reset()
        {
            LogStatus("Reset");
            int hr = FTR_Reset(p);
            FtInterop.EvaluateHR(hr);
        }

        public void CopyTo([In] FTResult destResult)
        {
            LogStatus("CopyTo");
            int hr = FTR_CopyTo(p, destResult);
            FtInterop.EvaluateHR(hr);
        }

        public int GetStatus()
        {
            int hr = FTR_GetStatus(p);
            LogStatus("GetStatus: " + FtInterop.DecodeHR(hr));
            return hr;
        }

        public void GetFaceRect(out Rect rect)
        {
            LogStatus("GetFaceRect");
            int hr = FTR_GetFaceRect(p, out rect);
            FtInterop.EvaluateHR(hr);
        }

        public void Get2DShapePoints(out IntPtr pointsPtr, out uint pointCount)
        {
            LogStatus("Get2DShapePoints");
            int hr = FTR_Get2DShapePoints(p, out pointsPtr, out pointCount);
            FtInterop.EvaluateHR(hr);
        }

        public void Get3DPose(out float scale, out Vector3DF rotationXYZ, out Vector3DF translationXYZ)
        {
            LogStatus("Get3DPose");
            int hr = FTR_Get3DPose(p, out scale, out rotationXYZ, out translationXYZ);
            FtInterop.EvaluateHR(hr);
        }

        public void GetAUCoefficients(out IntPtr animUnitCoeffsPtr, out uint animUnitCount)
        {
            LogStatus("GetAUCoefficients");
            int hr = FTR_GetAUCoefficients(p, out animUnitCoeffsPtr, out animUnitCount);
            FtInterop.EvaluateHR(hr);
        }

        #endregion


        #region Helper

        public bool Succeeded { get { return (GetStatus() >= 0); } }

        #endregion

    }

}
