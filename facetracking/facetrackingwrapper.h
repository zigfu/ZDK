#define EXPORT_API __declspec(dllexport)

#include <FaceTrackLib.h>

extern "C"
{
		 IFTFaceTracker * EXPORT_API CreateFaceTracker()
		 {
			return FTCreateFaceTracker();
		 }

		 HRESULT EXPORT_API FT_Initialize(IFTFaceTracker * pFT, const FT_CAMERA_CONFIG * pVideoCameraConfig, const FT_CAMERA_CONFIG *pDepthCameraConfig, FTRegisterDepthToColor depthToColorMappingFunc, PCWSTR pszModelPath)
		 {
			 return pFT->Initialize(pVideoCameraConfig, pDepthCameraConfig, depthToColorMappingFunc, pszModelPath);
		 }

}