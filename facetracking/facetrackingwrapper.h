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


		 
// // AddRef	Increments the reference count for an interface on an object. This method should be called for every new copy of a pointer to an interface on an object.		 
		 ULONG EXPORT_API FT_AddRef(IFTFaceTracker * pFT)
		 {
			 return pFT->AddRef();
		 }

// // ContinueTracking	Continues the face tracking process that was initiated by StartTracking. This method is faster than StartTracking and is used only for tracking. If the face being tracked moves too far from the previous location (for example, when the input frame rate is low), this method fails.
		HRESULT EXPORT_API FT_ContinueTracking(IFTFaceTracker * pFT, const FT_SENSOR_DATA *pSensorData, const FT_VECTOR3D headPoints[2], IFTResult *pFTResult)
		 {
			 return pFT->ContinueTracking(pSensorData, headPoints,pFTResult);
		 }

// // CreateFTResult	Creates a result object instance and returns its IFTResult interface. The returned interface refcount is incremented, so after you use it, you must release it by calling Release.		 
		 HRESULT EXPORT_API FT_CreateFTResult(IFTFaceTracker * pFT, IFTResult ** ppFTResult)
		 {
			return pFT->CreateFTResult(ppFTResult);
		 }

// // DetectFaces	Detects faces in the provided video frame. It returns an array of faces and the detection confidence level for each face. The confidence level is a value between 0 and 1 (where 0 is the lowest and 1 is highest).
		 HRESULT EXPORT_API FT_DetectFaces(IFTFaceTracker * pFT, const FT_SENSOR_DATA *pSensorData, const RECT *pRoi, FT_WEIGHTED_RECT *pFaces, UINT *pFaceCount)
		 {
			 return pFT->DetectFaces(pSensorData, pRoi, pFaces, pFaceCount);
		 }

// // GetFaceModel	Returns an IFTModel Interface interface to the loaded face model. The returned interface refcount is incremented, so after you use it, you must release it by calling Release.
		  HRESULT EXPORT_API FT_GetFaceModel(IFTFaceTracker * pFT, IFTModel ** ppModel)
		 {
			 return pFT->GetFaceModel(ppModel);
		 }
		 
// // GetShapeComputationState	Returns whether the shape unit (SU) computational state is enabled or disabled. If enabled, the face tracker continues refining the SUs.
// // GetShapeUnits	Returns shape units (SUs) that the face tracker is using. If the passed ppSUCoefs parameter is NULL, it returns number of SUs used in the loaded face model.
// // Initialize	Initializes an IFTFaceTracker instance.
		 HRESULT EXPORT_API FT_Initialize(IFTFaceTracker * pFT, const FT_CAMERA_CONFIG * pVideoCameraConfig, const FT_CAMERA_CONFIG *pDepthCameraConfig, FTRegisterDepthToColor depthToColorMappingFunc, PCWSTR pszModelPath)
		 {
			 return pFT->Initialize(pVideoCameraConfig, pDepthCameraConfig, depthToColorMappingFunc, pszModelPath);
		 }

// // QueryInterface	Retrieves pointers to the supported interfaces on an object. This method calls IFTFaceTracker::AddRef on the pointer it returns.
// // Release	Decrements the reference count for an interface on an object.
		 ULONG EXPORT_API FT_Release(IFTFaceTracker *pFT)
		 {
			 return pFT->Release();
		 }
// // Reset	Resets the IFTFaceTracker instance to a clean state (the same state that exists after calling the Initialize method).
// // SetShapeComputationState	Sets the shape unit (SU) computational state. This method allows you to enable or disable 3D-shape computation in the face tracker. If enabled, the face tracker will continue to refine SUs.
// // SetShapeUnits	Sets shape units (SUs) that the face tracker uses for passed values.
// // StartTracking	StartTracking Starts face tracking. StartTracking detects a face based on the passed parameters, then identifies characteristic points and begins tracking. This process is more expensive than simply tracking (done by calling ContinueTracking), but more robust. Therefore, if running at a high frame rate you should only use StartTracking to initiate the tracking process, and then you should use ContinueTracking to continue tracking. If the frame rate is low and the face tracker cannot keep up with fast face and head movement (or if there is too much motion blur), you can use StartTracking solely (instead of the usual sequence of StartTracking, ContinueTracking, ContinueTracking, and so on).






}