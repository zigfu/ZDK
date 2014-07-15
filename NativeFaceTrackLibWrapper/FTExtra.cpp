#define EXPORT_API __declspec(dllexport)

#ifndef min
#define min(a,b)	(((a) < (b)) ? (a) : (b))
#endif

#include <windows.h>	
#include "NuiAPI.h"		
#include <FaceTrackLib.h>


extern "C" 
{
	HRESULT EXPORT_API FTI_CopyFrom(IFTImage * pFTI, NUI_IMAGE_FRAME* pSrcNUIImageFrame)
	{
		if(pFTI == NULL) { return -2; }
		if(pSrcNUIImageFrame == NULL) { return -2; }

		INuiFrameTexture* pTexture = pSrcNUIImageFrame->pFrameTexture;
		NUI_LOCKED_RECT LockedRect;
		HRESULT hr = pTexture->LockRect(0, &LockedRect, NULL, 0);
		if(FAILED(hr)) { return hr; }
		if (LockedRect.Pitch == 0) { return -2; }	// If here, buffer length of the image texture is incorrect
		  
		size_t size = min(pFTI->GetBufferSize(), (UINT)pTexture->BufferLen());
		memcpy(pFTI->GetBuffer(), LockedRect.pBits, size);
		
		return hr;
	}
}
