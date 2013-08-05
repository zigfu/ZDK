using System;
using System.Runtime.InteropServices;


namespace Zigfu.FaceTracking
{

    public class FTImage : Zigfu.Utility.NativeObjectWrapper
    {
        override protected String ClassName { get { return "FTImage"; } }

        public FTImage(IntPtr p) : base(p) { }
        ~FTImage() { Release(); }

        public FTImage() : this(NativeMethods.FTCreateImage()) { }
        public FTImage(CameraConfig camConfig) : this(camConfig, IntPtr.Zero) { }
        public FTImage(CameraConfig camConfig, IntPtr imagePtr) : this(NativeMethods.FTCreateImage())
        {
            if (imagePtr == IntPtr.Zero)
            {
                Allocate(
                    camConfig.Width, camConfig.Height, camConfig.ImageFormat);
            }
            else
            {
                Attach(
                    camConfig.Width, camConfig.Height,
                    imagePtr, camConfig.ImageFormat, camConfig.Stride);
            }
        }


        #region DLL Imports

        const String DLL_PATH = FtInterop.FacetrackingWrapperDll;

        [DllImport(DLL_PATH)]  static extern int FTI_Release(IntPtr This);

        [DllImport(DLL_PATH)]  static extern int FTI_Allocate(IntPtr This, uint width, uint height, FaceTrackingImageFormat format);
        [DllImport(DLL_PATH)]  static extern int FTI_Attach(IntPtr This, uint width, uint height, IntPtr dataPtr, FaceTrackingImageFormat format, uint stride);
        [DllImport(DLL_PATH)]  static extern int FTI_Reset(IntPtr This);
        [DllImport(DLL_PATH)]  static extern uint FTI_GetWidth(IntPtr This);
        [DllImport(DLL_PATH)]  static extern uint FTI_GetHeight(IntPtr This);
        [DllImport(DLL_PATH)]  static extern uint FTI_GetStride(IntPtr This);
        [DllImport(DLL_PATH)]  static extern uint FTI_GetBytesPerPixel(IntPtr This);
        [DllImport(DLL_PATH)]  static extern uint FTI_GetBufferSize(IntPtr This);
        [DllImport(DLL_PATH)]  static extern FaceTrackingImageFormat FTI_GetFormat(IntPtr This);
        [DllImport(DLL_PATH)]  static extern IntPtr FTI_GetBuffer(IntPtr This);
        [DllImport(DLL_PATH)]  static extern bool FTI_IsAttached(IntPtr This);
        [DllImport(DLL_PATH)]  static extern int FTI_CopyTo(IntPtr This, [In] FTImage destImage, [In] ref Rect srcRect, uint destRow, uint destColumn);
        [DllImport(DLL_PATH)]  static extern int FTI_DrawLine(IntPtr This, Point startPoint, Point endPoint, uint color, uint lineWidthPx);

        [DllImport(DLL_PATH)]  static extern int FTI_CopyFrom(IntPtr This, IntPtr pSrcNUIImageFrame);

        #endregion


        #region Wrapped Native Function Calls

        public void Release()
        {
            LogStatus("FTI_Release");
            FTI_Release(p);
        }


        public void Allocate(uint width, uint height, FaceTrackingImageFormat format)
        {
            LogStatus("Allocate");
            int hr = FTI_Allocate(p, width, height, format);
            FtInterop.EvaluateHR(hr);
        }

        public void Attach(uint width, uint height, IntPtr dataPtr, FaceTrackingImageFormat format, uint stride)
        {
            LogStatus("Attach");
            int hr = FTI_Attach(p, width, height, dataPtr, format, stride);
            FtInterop.EvaluateHR(hr);
        }

        public void Reset()
        {
            LogStatus("Reset");
            int hr = FTI_Reset(p);
            FtInterop.EvaluateHR(hr);
        }

        public uint GetWidth()
        {
            return FTI_GetWidth(p);
        }

        public uint GetHeight()
        {
            return FTI_GetHeight(p);
        }

        public uint GetStride()
        {
            return FTI_GetStride(p);
        }

        public uint GetBytesPerPixel()
        {
            return FTI_GetBytesPerPixel(p);
        }

        public uint GetBufferSize()
        {
            return FTI_GetBufferSize(p);
        }

        public FaceTrackingImageFormat GetFormat()
        {
            return FTI_GetFormat(p);
        }

        public IntPtr GetBuffer()
        {
            return FTI_GetBuffer(p);
        }

        public bool IsAttached()
        {
            return FTI_IsAttached(p);
        }

        public void CopyTo([In] FTImage destImage, [In] ref Rect srcRect, uint destRow, uint destColumn)
        {
            LogStatus("CopyTo");
            int hr = FTI_CopyTo(p, destImage, ref srcRect, destRow, destColumn);
            FtInterop.EvaluateHR(hr);
        }

        public void DrawLine(Point startPoint, Point endPoint, uint color, uint lineWidthPx)
        {
            LogStatus("DrawLine");
            int hr = FTI_DrawLine(p, startPoint, endPoint, color, lineWidthPx);
            FtInterop.EvaluateHR(hr);
        }

        #endregion


        #region Helper

        public void CopyFrom(IntPtr pImageFrame)
        {
            LogStatus("FTI_CopyFrom");
            int hr = FTI_CopyFrom(p, pImageFrame);
            FtInterop.EvaluateHR(hr);
        }

        #endregion

    }

}
