#define EXPORT_API __declspec(dllexport)

#ifndef min
#define min(a,b)            (((a) < (b)) ? (a) : (b))
#endif

#include <FaceTrackLib.h>


// ------------------------------------------------ //
// --------------   IFTFaceTracker   -------------- //
// ------------------------------------------------ //

extern "C" 
{

	ULONG EXPORT_API FT_Release(IFTFaceTracker *pFT)
	{
		return pFT->Release();
	}
	

	HRESULT EXPORT_API FT_Initialize(IFTFaceTracker * pFT, const FT_CAMERA_CONFIG * pVideoCameraConfig, const FT_CAMERA_CONFIG *pDepthCameraConfig, FTRegisterDepthToColor depthToColorMappingFunc, PCWSTR pszModelPath)
	{
		return pFT->Initialize(pVideoCameraConfig, pDepthCameraConfig, depthToColorMappingFunc, pszModelPath);
	}

	HRESULT EXPORT_API FT_Reset(IFTFaceTracker *pFT)
	{
		return pFT->Reset();
	}

	HRESULT EXPORT_API FT_CreateFTResult(IFTFaceTracker * pFT, IFTResult ** ppFTResult)
	{
		return pFT->CreateFTResult(ppFTResult);
	}

	HRESULT EXPORT_API FT_SetShapeUnits(IFTFaceTracker * pFT, FLOAT headScale, const FLOAT* pSUCoefs, UINT suCount)
	{
		return pFT->SetShapeUnits(headScale, pSUCoefs, suCount);
	}

    HRESULT EXPORT_API FT_GetShapeUnits(IFTFaceTracker * pFT, FLOAT* pHeadScale, FLOAT** ppSUCoefs, UINT* pSUCount, BOOL* pHaveConverged)
	{
		return pFT->GetShapeUnits(pHeadScale, ppSUCoefs, pSUCount, pHaveConverged);
	}

    HRESULT EXPORT_API FT_SetShapeComputationState(IFTFaceTracker * pFT, BOOL isEnabled)
	{
		return pFT->SetShapeComputationState(isEnabled);
	}

    HRESULT EXPORT_API FT_GetShapeComputationState(IFTFaceTracker * pFT, BOOL* pIsEnabled)
	{
		return pFT->GetShapeComputationState(pIsEnabled);
	}

	HRESULT EXPORT_API FT_GetFaceModel(IFTFaceTracker * pFT, IFTModel ** ppModel)
	{
		return pFT->GetFaceModel(ppModel);
	}

	HRESULT EXPORT_API FT_StartTracking(IFTFaceTracker *pFT, const FT_SENSOR_DATA *pSensorData, const RECT *pRoi, const FT_VECTOR3D headPoints[2], IFTResult *pFTResult)
	{
		return pFT->StartTracking(pSensorData, pRoi, headPoints, pFTResult);
	}
	HRESULT EXPORT_API FT_StartTracking_Default(IFTFaceTracker *pFT, const FT_SENSOR_DATA *pSensorData, IFTResult *pFTResult)
	{
		return pFT->StartTracking(pSensorData, NULL, NULL, pFTResult);
	}

	HRESULT EXPORT_API FT_ContinueTracking(IFTFaceTracker * pFT, const FT_SENSOR_DATA *pSensorData, const FT_VECTOR3D headPoints[2], IFTResult *pFTResult)
	{
		return pFT->ContinueTracking(pSensorData, headPoints, pFTResult);
	}
	HRESULT EXPORT_API FT_ContinueTracking_Default(IFTFaceTracker * pFT, const FT_SENSOR_DATA *pSensorData, IFTResult *pFTResult)
	{
		return pFT->ContinueTracking(pSensorData, NULL, pFTResult);
	}

	HRESULT EXPORT_API FT_DetectFaces(IFTFaceTracker * pFT, const FT_SENSOR_DATA *pSensorData, const RECT *pRoi, FT_WEIGHTED_RECT *pFaces, UINT *pFaceCount)
	{
		return pFT->DetectFaces(pSensorData, pRoi, pFaces, pFaceCount);
	}
	 
}


// ------------------------------------------------ //
// ----------------   IFTResult   ----------------- //
// ------------------------------------------------ //

extern "C" 
{

	ULONG EXPORT_API FTR_Release(IFTResult * pFTR)
	{
		return pFTR->Release();
	}
	

	HRESULT EXPORT_API FTR_Reset(IFTResult * pFTR)
	{
		return pFTR->Reset();
	}

	HRESULT EXPORT_API FTR_CopyTo(IFTResult * pFTR, IFTResult* pFTResultDst)
	{
		return pFTR->CopyTo(pFTResultDst);
	}

	HRESULT EXPORT_API FTR_GetStatus(IFTResult * pFTR)
	{
		return pFTR->GetStatus();
	}
		
	HRESULT EXPORT_API FTR_GetFaceRect(IFTResult * pFTR, RECT* pRect)
	{
		return pFTR->GetFaceRect(pRect);
	}
		
	HRESULT EXPORT_API FTR_Get2DShapePoints(IFTResult * pFTR, FT_VECTOR2D** ppPoints, UINT* pPointCount)
	{
		return pFTR->Get2DShapePoints(ppPoints, pPointCount);
	}
		
	HRESULT EXPORT_API FTR_Get3DPose(IFTResult * pFTR, FLOAT* pScale, FLOAT rotationXYZ[3], FLOAT translationXYZ[3])
	{
		return pFTR->Get3DPose(pScale, rotationXYZ, translationXYZ);
	}
		
	HRESULT EXPORT_API FTR_GetAUCoefficients(IFTResult * pFTR, FLOAT** ppCoefficients, UINT* pAUCount)
	{
		return pFTR->GetAUCoefficients(ppCoefficients, pAUCount);
	}

}


// ------------------------------------------------ //
// -----------------   IFTImage   ----------------- //
// ------------------------------------------------ //

extern "C" 
{

	ULONG EXPORT_API FTI_Release(IFTImage * pFTI)
	{
		return pFTI->Release();
	}


	HRESULT EXPORT_API FTI_Allocate(IFTImage * pFTI, UINT width, UINT height, FTIMAGEFORMAT format)
	{
		return pFTI->Allocate(width, height, format);
	}
	
	HRESULT EXPORT_API FTI_Attach(IFTImage * pFTI, UINT width, UINT height, void* pData, FTIMAGEFORMAT format, UINT stride)
	{
		return pFTI->Attach(width, height, pData, format, stride);
	}

	HRESULT EXPORT_API FTI_Reset(IFTImage * pFTI)
	{
		return pFTI->Reset();
	}
		
	UINT EXPORT_API FTI_GetWidth(IFTImage * pFTI)
	{
		return pFTI->GetWidth();
	}
		
	UINT EXPORT_API FTI_GetHeight(IFTImage * pFTI)
	{
		return pFTI->GetHeight();
	}
		
	UINT EXPORT_API FTI_GetStride(IFTImage * pFTI)
	{
		return pFTI->GetStride();
	}
		
	UINT EXPORT_API FTI_GetBytesPerPixel(IFTImage * pFTI)
	{
		return pFTI->GetBytesPerPixel();
	}

	UINT EXPORT_API FTI_GetBufferSize(IFTImage * pFTI)
	{
		return pFTI->GetBufferSize();
	}

	FTIMAGEFORMAT EXPORT_API FTI_GetFormat(IFTImage * pFTI)
	{
		return pFTI->GetFormat();
	}

	EXPORT_API BYTE* FTI_GetBuffer(IFTImage * pFTI)
	{
		return pFTI->GetBuffer();
	}

	BOOL EXPORT_API FTI_IsAttached(IFTImage * pFTI)
	{
		return pFTI->IsAttached();
	}
		
	HRESULT EXPORT_API FTI_CopyTo(IFTImage * pFTI, IFTImage* pDestImage, const RECT* pSrcRect, UINT destRow, UINT destColumn)
	{
		return pFTI->CopyTo(pDestImage, pSrcRect, destRow, destColumn);
	}
		
	HRESULT EXPORT_API FTI_DrawLine(IFTImage * pFTI, POINT startPoint, POINT endPoint, UINT32 color, UINT lineWidthPx)
	{
		return pFTI->DrawLine(startPoint, endPoint, color, lineWidthPx);
	}
}


// ------------------------------------------------ //
// -----------------   IFTModel   ----------------- //
// ------------------------------------------------ //

extern "C" 
{

	ULONG EXPORT_API FTM_Release(IFTModel * pFTM)
	{
		return pFTM->Release();
	}


	UINT EXPORT_API FTM_GetSUCount(IFTModel * pFTM)
	{
		return pFTM->GetSUCount();
	}

	UINT EXPORT_API FTM_GetAUCount(IFTModel * pFTM)
	{
		return pFTM->GetAUCount();
	}
		
	HRESULT EXPORT_API FTM_GetTriangles(IFTModel * pFTM, FT_TRIANGLE** ppTriangles, UINT* pTriangleCount)
	{
		return pFTM->GetTriangles(ppTriangles, pTriangleCount);
	}
		
	UINT EXPORT_API FTM_GetVertexCount(IFTModel * pFTM)
	{
		return pFTM->GetVertexCount();
	}
		
	HRESULT EXPORT_API FTM_Get3DShape(IFTModel * pFTM, const FLOAT* pSUCoefs, UINT suCount, const FLOAT* pAUCoefs, UINT auCount, FLOAT scale, const FLOAT roationXYZ[3], const FLOAT translationXYZ[3], 
		FT_VECTOR3D* pVertices, UINT vertexCount)
	{
		return pFTM->Get3DShape(pSUCoefs, suCount, pAUCoefs, auCount, scale, roationXYZ, translationXYZ, pVertices, vertexCount);
	}

	HRESULT EXPORT_API FTM_GetProjectedShape(IFTModel * pFTM, const FT_CAMERA_CONFIG* pCameraConfig, 
		FLOAT zoomFactor, POINT viewOffset, const FLOAT* pSUCoefs, UINT suCount, const FLOAT* pAUCoefs, 
		UINT auCount, FLOAT scale, const FLOAT rotationXYZ[3], const FLOAT translationXYZ[3], 
		FT_VECTOR2D* pVertices, UINT vertexCount)
	{
		return pFTM->GetProjectedShape(pCameraConfig, zoomFactor, viewOffset, pSUCoefs, suCount, 
				pAUCoefs, auCount, scale, rotationXYZ, translationXYZ, pVertices, vertexCount);
	}
}