using System;
using System.Runtime.InteropServices;


namespace Zigfu.FaceTracking
{

    public class FTModel : Zigfu.Utility.NativeObjectWrapper
    {
        override protected String ClassName { get { return "FTModel"; } }

        public FTModel(IntPtr p) : base(p) { }
        ~FTModel() { Release(); }


        #region DLL Imports

        const String DLL_PATH = FtInterop.FacetrackingWrapperDll;

        [DllImport(DLL_PATH)]  static extern int FTM_Release(IntPtr This);

        [DllImport(DLL_PATH)]  static extern uint FTM_GetSUCount(IntPtr This);
        [DllImport(DLL_PATH)]  static extern uint FTM_GetAUCount(IntPtr This);
        [DllImport(DLL_PATH)]  static extern int FTM_GetTriangles(IntPtr This, out IntPtr trianglesPtr, out uint triangleCount);
        [DllImport(DLL_PATH)]  static extern uint FTM_GetVertexCount(IntPtr This);
        [DllImport(DLL_PATH)]  static extern int FTM_Get3DShape(IntPtr This, IntPtr shapeUnitCoeffsPtr, uint shapeUnitCount, IntPtr animUnitCoeffPtr, uint animUnitCount, float scale, ref Vector3DF rotationXYZ, ref Vector3DF translationXYZ, IntPtr vertices, uint vertexCount);
        [DllImport(DLL_PATH)]  static extern int FTM_GetProjectedShape(IntPtr This, CameraConfig cameraConfig, float zoomFactor, Point viewOffset, IntPtr shapeUnitCoeffPtr, uint shapeUnitCount, IntPtr animUnitCoeffsPtr, uint animUnitCount, float scale, ref Vector3DF rotationXYZ, ref Vector3DF translationXYZ, IntPtr vertices, uint vertexCount);

        #endregion


        #region Wrapped Native Function Calls

        public void Release()
        {
            LogStatus("FTM_Release");
            FTM_Release(p);
        }


        public uint GetSUCount()
        {
            return FTM_GetSUCount(p);
        }

        public uint GetAUCount()
        {
            return FTM_GetAUCount(p);
        }

        public void GetTriangles(out IntPtr trianglesPtr, out uint triangleCount)
        {
            int hr = FTM_GetTriangles(p, out trianglesPtr, out triangleCount);
            FtInterop.EvaluateHR(hr);
        }

        public uint GetVertexCount()
        {
            return FTM_GetVertexCount(p);
        }

        public void Get3DShape( IntPtr shapeUnitCoeffsPtr, uint shapeUnitCount, 
                                IntPtr animUnitCoeffPtr, uint animUnitCount, 
                                float scale, ref Vector3DF rotationXYZ, ref Vector3DF translationXYZ, 
                                IntPtr vertices, uint vertexCount)
        {
            int hr = FTM_Get3DShape(p, shapeUnitCoeffsPtr, shapeUnitCount, 
                                       animUnitCoeffPtr, animUnitCount, 
                                       scale, ref rotationXYZ, ref translationXYZ, 
                                       vertices, vertexCount);
            FtInterop.EvaluateHR(hr);
        }

        public void GetProjectedShape(CameraConfig cameraConfig, float zoomFactor, Point viewOffset, 
                                      IntPtr shapeUnitCoeffPtr, uint shapeUnitCount, 
                                      IntPtr animUnitCoeffsPtr, uint animUnitCount, 
                                      float scale, ref Vector3DF rotationXYZ, ref Vector3DF translationXYZ, 
                                      IntPtr vertices, uint vertexCount)
        {
            int hr = FTM_GetProjectedShape(p, cameraConfig, zoomFactor, viewOffset, 
                                              shapeUnitCoeffPtr, shapeUnitCount, 
                                              animUnitCoeffsPtr, animUnitCount, 
                                              scale, ref rotationXYZ, ref translationXYZ, 
                                              vertices, vertexCount);
            FtInterop.EvaluateHR(hr);
        }

        #endregion

    }
}
