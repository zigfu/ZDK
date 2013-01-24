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
		 HRESULT FT_Reset(IFTFaceTracker *pFT)
		 {
			 return pFT->Reset();
		 }

// // SetShapeComputationState	Sets the shape unit (SU) computational state. This method allows you to enable or disable 3D-shape computation in the face tracker. If enabled, the face tracker will continue to refine SUs.
// // SetShapeUnits	Sets shape units (SUs) that the face tracker uses for passed values.



// // StartTracking	StartTracking Starts face tracking. StartTracking detects a face based on the passed parameters, then identifies characteristic points and begins tracking. This process is more expensive than simply tracking (done by calling ContinueTracking), but more robust. Therefore, if running at a high frame rate you should only use StartTracking to initiate the tracking process, and then you should use ContinueTracking to continue tracking. If the frame rate is low and the face tracker cannot keep up with fast face and head movement (or if there is too much motion blur), you can use StartTracking solely (instead of the usual sequence of StartTracking, ContinueTracking, ContinueTracking, and so on).		 
		 /*
			pSensorData
			Type: FT_SENSOR_DATA
			Input from the video camera and depth sensor (depth is optional).

			pRoi
			Type: RECT
			Optional, NULL if not provided. Region of interest in the passed video frame where the face tracker should search for a face to initiate tracking.

			headPoints
			Type: FT_VECTOR3D
			Optional, NULL if not provided. Array that contains two 3D points in camera space if known (for example, from a Kinect skeleton). The first element is the neck position and the second element is the head center position. The camera space is defined as: right handed, the origin at the camera optical center; Y points up; units are in meters.

			pFTResult
			Type: IFTResult
			IFTResult Interface pointer that receives computed face tracking results.
			
			Return Value
			Type: HRESULT
			If the method succeeds, the return value is S_OK. If the method fails due to programmatic errors, the return value can be FT_ERROR_UNINITIALIZED, E_INVALIDARG, E_POINTER. To check if the face tracking was successful, you should call the IFTResult::GetStatus method.
		*/
		 HRESULT EXPORT_API StartTracking(IFTFaceTracker *pFT, const FT_SENSOR_DATA *pSensorData, const RECT *pRoi, const FT_VECTOR3D headPoints[2], IFTResult *pFTResult)
		 {
			 pFT->StartTracking(pSensorData, pRoi, headPoints, pFTResult);
		 }







//FTResult Interface

// // AddRef	Increments the reference count for an interface on an object. This method should be called for every new copy of a pointer to an interface on an object.
		 ULONG EXPORT_API FTR_AddRef(IFTResult * pFTR)
		 {
			 return pFTR->AddRef();
		 }
// // CopyTo	Copies all data from this instance to another instance of IFTResult.
		 HRESULT EXPORT_API FTR_CopyTo(IFTResult * pFTR, IFTResult *pFTResultDst)
		 {
			 return pFTR->CopyTo(pFTResultDst);
		 }


// // Get2DShapePoints	Gets the (x,y) coordinates of the key points on the aligned face in video frame coordinates.
// // Get3DPose	Gets 3D pose information.
// // GetAUCoefficients	Gets animation unit (AU) coefficients, which represent deformations of the 3D mask caused by the moving parts of the face (mouth, eyebrows, and so on).
// // GetFaceRect	Gets a face rectangle in video frame coordinates.
		HRESULT EXPORT_API FTR_GetFaceRect(IFTResult *pFTR, RECT * pRect)
		{
			return pFTR->GetFaceRect(pRect);
		}

// // GetStatus	Gets the result of the face tracking operation.
// // QueryInterface	Retrieves pointers to the supported interfaces on an object. This method calls IFTModel::AddRef on the pointer it returns.
// // Release	Decrements the reference count for an interface on an object.
		 ULONG EXPORT_API FTR_Release(IFTResult * pFTR)
		 {
			 return pFTR->Release();
		 }

// // Reset	Resets this instance to a clean state.
		 HRESULT  EXPORT_API FTR_Reset(IFTResult * pFTR)
		 {
			 return pFTR->Reset();
		 }



	

///IFTImage interface
		 	IFTImage * EXPORT_API FT_CreateImage()
			{
				return FTCreateImage(); 
			}

// // AddRef	Increments the reference count for an interface on an object. This method should be called for every new copy of a pointer to an interface on an object.
			ULONG EXPORT_API FTI_AddRef(IFTImage * pFTI)
			{
				return pFTI->AddRef();
			}

// // Allocate	Allocates memory for the image of passed width, height and format. The memory is owned by this interface and is released when the interface is released or when another Allocate call happens. Allocate deallocates currently allocated memory if its internal buffers are not big enough to fit new image data. If its internal buffers are big enough, no new allocation occurs.
			HRESULT EXPORT_API RTI_Allocate(IFTImage * pFTI, UINT width, UINT height, FTIMAGEFORMAT format)
			{
				pFTI->Allocate(width, height, format);
			}
// // Attach	Attaches this interface to external memory pointed to by pData, which is assumed to be sufficiently large to contain an image of the given size and format. The memory referenced by pData is not deallocated when this interface is released. The caller owns the image buffer in this case and is responsible for its lifetime management.
// // CopyTo	Non-allocating copy method. It copies this image data to pDestImage. It fails, if pDestImage doesn't have the right size or format. If pDestImage has a different format, then this method attempts to convert pixels to pDestImage image format (if possible and supported).
// // DrawLine	Draws a line on the image.
// // GetBuffer	Gets the format of the image.
// // GetBufferSize	Gets the buffer size of the image.
// // GetBytesPerPixel	Gets the bytes per pixel of the image.
// // GetFormat	Gets the format of the image.
// // GetHeight	Gets the height of the image.
// // GetStride	Gets the stride of the image.
// // GetWidth	Gets the width of the image.
// // IsAttached	Gets the image buffer ownership state.
// // QueryInterface	Retrieves pointers to the supported interfaces on an object. This method calls IFTFaceTracker::AddRef on the pointer it returns.
// // Release	Decrements the reference count for an interface on an object.
			ULONG EXPORT_API FTI_Release(IFTImage * pFTI)
			{
				return pFTI->Release();
			}

// // Reset	Frees internal memory and sets this image to the empty state (0 size).
			HRESULT EXPORT_API FTI_Reset(IFTImage * pFTI)
			{
				return pFTI->Reset();
			}


}