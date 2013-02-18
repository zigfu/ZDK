#define EXPORT_API __declspec(dllexport) 

#include <windows.h>
#include "NuiAPI.h"
#include <FaceTrackLib.h>

extern "C" 
{	
	IFTFaceTracker* pFaceTracker;

	IFTImage* pColorFrame = NULL;
	IFTImage* pDepthFrame = NULL;

	FT_SENSOR_DATA sensorData;
	POINT p;

	IFTResult* pFTResult = NULL;

	FT_CAMERA_CONFIG ZigImage = {640, 480, 531.15f};
	FT_CAMERA_CONFIG ZigDepth = {320, 240, 285.63f};

	bool isTracked = false;

	HRESULT EXPORT_API FT_CreateFaceTracker()
	{
		pFaceTracker = FTCreateFaceTracker();
		if (!pFaceTracker)
		{
			return -1;
		}
		return 0;
	}

	HRESULT EXPORT_API FT_InitFaceTracker()
	{
		HRESULT hr =  pFaceTracker->Initialize(&ZigImage, &ZigDepth, NULL, NULL); 
		if( FAILED(hr) )
		{
		  return hr;
		}
			
	    pColorFrame = FTCreateImage();
		if(!pColorFrame)
		{
		  return E_OUTOFMEMORY;
		}

		hr = pColorFrame->Allocate(640, 480, FTIMAGEFORMAT_UINT8_B8G8R8X8);
		if (FAILED(hr))
		{
			return hr;
		}

	    pDepthFrame = FTCreateImage();
		if(!pDepthFrame)
		{
		  return E_OUTOFMEMORY;
		}

	    hr = pDepthFrame->Allocate(320, 240, FTIMAGEFORMAT_UINT16_D13P3);
		if (FAILED(hr))
		{
			return hr;
		}

		hr = pFaceTracker->CreateFTResult(&pFTResult);
		if(FAILED(hr))
		{
			return hr;
		}
		return hr;
	}

	void EXPORT_API FT_ProcessVideoFrame(NUI_IMAGE_FRAME* pImageFrame)
	{
		INuiFrameTexture* pTexture = pImageFrame->pFrameTexture;
		NUI_LOCKED_RECT LockedRect;
		pTexture->LockRect(0, &LockedRect, NULL, 0);
		if (LockedRect.Pitch)
		{  
			memcpy(pColorFrame->GetBuffer(), PBYTE(LockedRect.pBits), min(pColorFrame->GetBufferSize(), UINT(pTexture->BufferLen())));
		}
		else
		{
			//TODO: If here, Buffer length of the image texture is incorrect maybe throw an error.
		}
	}


	void EXPORT_API FT_ProcessDepthFrame( NUI_IMAGE_FRAME* pImageFrame )
	{
		INuiFrameTexture* pTexture = pImageFrame->pFrameTexture;
		NUI_LOCKED_RECT LockedRect;
		pTexture->LockRect(0, &LockedRect, NULL, 0);
		if (LockedRect.Pitch)
		{   
			memcpy(pDepthFrame->GetBuffer(), PBYTE(LockedRect.pBits), min(pDepthFrame->GetBufferSize(), UINT(pTexture->BufferLen())));
		}
		else
		{
			//TODO: If here, Buffer length of the depth texture is incorrect maybe throw an error.
		}
	}

	//Not the pretiest way to repesent a transform, but structs seem to be auto seralized between
	//mangaged and unmanaded memory and marsheling arrays was giving me issues
	#pragma pack(1)
	typedef struct FT_FaceTransform
	{
		float positionX;
		float positionY;
		float positionZ;
		float rotationX;
		float rotationY;
		float rotationZ;
	};
	#pragma pack()

	FT_FaceTransform  EXPORT_API FT_GetFaceTransform()
	{
		FT_FaceTransform transform = FT_FaceTransform();
		transform.positionX = 0;
		transform.positionY = 0;
		transform.positionZ = 0;
		transform.rotationX = 0;
		transform.rotationY = 0;
		transform.rotationZ = 0;

		if(isTracked && SUCCEEDED(pFTResult->GetStatus()))
		{
			float pScale = 0.0f;
			float rotationXYZ[3] = {0,0,0};
			float translationXYZ[3] = {0,0,0};

			HRESULT hr = pFTResult->Get3DPose(&pScale,rotationXYZ,translationXYZ);
			if (FAILED(hr))
			{
			}
			transform.positionX = translationXYZ[0];
			transform.positionY = translationXYZ[1];
			transform.positionZ = translationXYZ[2];

			transform.rotationX = rotationXYZ[0];
			transform.rotationY = rotationXYZ[1];
			transform.rotationZ = rotationXYZ[2];
		}
		return transform;
	}

	HRESULT EXPORT_API FT_TrackFrame()
	{
		HRESULT hr = 0;
		
		p.x = 0;
		p.y = 0;
		sensorData.pVideoFrame = pColorFrame;
		sensorData.pDepthFrame = pDepthFrame;
		sensorData.ZoomFactor = 1.0f;  // Not used must be 1.0
		sensorData.ViewOffset = p;    // Not used must be (0,0)

		if(!isTracked)
		{
		  // Initiate face tracking. This call is more expensive and
		  // searches the input image for a face.
		  hr = pFaceTracker->StartTracking(&sensorData, NULL, NULL, pFTResult);
		  if(SUCCEEDED(hr) && SUCCEEDED(pFTResult->GetStatus()))
		  {
			isTracked = true;
			return 1;
		  }
		  else
		  {
			  isTracked = false;
			 return hr;
		  }
		}
		else
		{
		  // Continue tracking. It uses a previously known face position,
		  // so it is an inexpensive call.
		  hr = pFaceTracker->ContinueTracking(&sensorData, NULL, pFTResult);
		  if(FAILED(hr) || FAILED(pFTResult->GetStatus()))
		  {
			isTracked = false;
			return -1;
		  }
		}
		return hr;
	}

	void EXPORT_API FT_ShutDown()
	{
		//pFTResult->Release();
		pColorFrame->Release();
		pDepthFrame->Release();
		pFaceTracker->Release();
	}
}	