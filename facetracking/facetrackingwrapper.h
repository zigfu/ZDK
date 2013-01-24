#define EXPORT_API __declspec(dllexport)

#include <FaceTrackLib.h>

extern "C"
{
		 IFTFaceTracker * EXPORT_API FT_CreateFaceTracker()
		 {
			return FTCreateFaceTracker();
		 }
		/*struct FT_CAMERA_CONFIG
		{   
			// Note that camera pixels should be square
			UINT  Width;            // frame width in pixels, allowed range - 1-UINT_MAX
			UINT  Height;           // frame height in pixels, allowed range - 1-UINT_MAX
			FLOAT FocalLength;      // camera’s focal length in pixels, allowed range - 0-FLOAT_MAX, where 0 value means - use an estimated focal length (average for most cameras, the tracking precision may degrade)
		};
		*/
		 const FT_CAMERA_CONFIG DefaultCameraConfig = {640, 480, 1.0}; // width, height, focal length


		 HRESULT EXPORT_API FT_Initialize(IFTFaceTracker * pFT, const FT_CAMERA_CONFIG * pVideoCameraConfig, const FT_CAMERA_CONFIG *pDepthCameraConfig, FTRegisterDepthToColor depthToColorMappingFunc, PCWSTR pszModelPath)
		 {
			 return pFT->Initialize(pVideoCameraConfig, pDepthCameraConfig, depthToColorMappingFunc, pszModelPath);
		 }
		 
		 ULONG EXPORT_API FT_AddRef(IFTFaceTracker * pFT)
		 {
			 return pFT->AddRef();
		 }

		 HRESULT EXPORT_API FT_ContinueTracking(IFTFaceTracker * pFT, const FT_SENSOR_DATA *pSensorData, const FT_VECTOR3D headPoints[2], IFTResult *pFTResult)
		 {
			 return pFT->ContinueTracking(pSensorData, headPoints,pFTResult);
		 }

		 HRESULT EXPORT_API FT_CreateFTResult(IFTFaceTracker * pFT, IFTResult ** ppFTResult)
		 {
			return pFT->CreateFTResult(ppFTResult);
		 }

		 HRESULT EXPORT_API FT_DetectFaces(IFTFaceTracker * pFT, const FT_SENSOR_DATA *pSensorData, const RECT *pRoi, FT_WEIGHTED_RECT *pFaces, UINT *pFaceCount)
		 {
			 return pFT->DetectFaces(pSensorData, pRoi, pFaces, pFaceCount);
		 }

		 HRESULT EXPORT_API FT_GetFaceModel(IFTFaceTracker * pFT, IFTModel ** ppModel)
		 {
			 return pFT->GetFaceModel(ppModel);
		 }


}