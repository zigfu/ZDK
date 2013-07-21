// This is the main DLL file.

#include "stdafx.h"
#include <Windows.h>
//#include <strsafe.h>
#include <iostream>
#include <NuiApi.h>
#include <KinectInteraction.h>

#include "KinectGrip.h"

#define SafeRelease(X) if(X) delete X;

using namespace std;


//----------------------------------------------------
//#define _WINDOWS
INuiSensor            *m_pNuiSensor;

INuiInteractionStream *m_nuiIStream;
HANDLE m_hProcesss;		// KinectDataProc Thread Handle
DWORD m_dwThreadID=0;

int m_LHandStat = 0;
int m_RHandStat = 0;

enum HANDGRIP_UPDATE_MODE
{
	FROM_UNITY,
	THREAD,
};

HANDGRIP_UPDATE_MODE m_mode = HANDGRIP_UPDATE_MODE::FROM_UNITY;	

class CInteractionClient:public INuiInteractionClient
{
public:
	CInteractionClient()
	{;}
	~CInteractionClient()
	{;}

	STDMETHOD(GetInteractionInfoAtLocation)(THIS_ DWORD skeletonTrackingId, NUI_HAND_TYPE handType, FLOAT x, FLOAT y, _Out_ NUI_INTERACTION_INFO *pInteractionInfo)
	{        
		if(pInteractionInfo)
		{
			pInteractionInfo->IsPressTarget         = false;
			pInteractionInfo->PressTargetControlId  = 0;
			pInteractionInfo->PressAttractionPointX = 0.f;
			pInteractionInfo->PressAttractionPointY = 0.f;
			pInteractionInfo->IsGripTarget          = true;
			return S_OK;
		}
		return E_POINTER;

		//return S_OK; 

	}

	STDMETHODIMP_(ULONG)    AddRef()                                    { return 2;     }
	STDMETHODIMP_(ULONG)    Release()                                   { return 1;     }
	STDMETHODIMP            QueryInterface(REFIID riid, void **ppv)     { return S_OK;  }

};

CInteractionClient m_nuiIClient;
//--------------------------------------------------------------------
HANDLE m_hNextColorFrameEvent;
HANDLE m_hNextDepthFrameEvent;
HANDLE m_hNextSkeletonEvent;
HANDLE m_hNextInteractionEvent;
HANDLE m_pColorStreamHandle;
HANDLE m_pDepthStreamHandle;
HANDLE m_hEvNuiProcessStop;
//-----------------------------------------------------------------------------------

int DrawColor(HANDLE h)
{
	return 0;
}

int DrawDepth(HANDLE h)
{
	NUI_IMAGE_FRAME pImageFrame;
	INuiFrameTexture* pDepthImagePixelFrame;
	HRESULT hr = m_pNuiSensor->NuiImageStreamGetNextFrame( h, 0, &pImageFrame );
	BOOL nearMode = TRUE;
	m_pNuiSensor->NuiImageFrameGetDepthImagePixelFrameTexture(m_pDepthStreamHandle, &pImageFrame, &nearMode, &pDepthImagePixelFrame);
	INuiFrameTexture * pTexture = pDepthImagePixelFrame;
	NUI_LOCKED_RECT LockedRect;  
	pTexture->LockRect( 0, &LockedRect, NULL, 0 );  
	if( LockedRect.Pitch != 0 )
	{
		HRESULT hr = m_nuiIStream->ProcessDepth(LockedRect.size,PBYTE(LockedRect.pBits),pImageFrame.liTimeStamp);
		if( FAILED( hr ) )
		{
			cout<<"Process Depth failed"<<endl;
		}
	}
	pTexture->UnlockRect(0);
	m_pNuiSensor->NuiImageStreamReleaseFrame( h, &pImageFrame );
	return 0;
}

int DrawSkeleton()
{
	NUI_SKELETON_FRAME SkeletonFrame = {0};
	HRESULT hr = m_pNuiSensor->NuiSkeletonGetNextFrame( 0, &SkeletonFrame );
	if( FAILED( hr ) )
	{
		cout<<"Get Skeleton Image Frame Failed"<<endl;
		return -1;
	}

	bool bFoundSkeleton = true;
	bFoundSkeleton = true;  
	static int static_one_is_enough=0;
	if(static_one_is_enough==0)
	{
		cout<<"find skeleton !"<<endl;
		static_one_is_enough++;
	}

	m_pNuiSensor->NuiTransformSmooth(&SkeletonFrame,NULL); 

	Vector4 v;
	m_pNuiSensor->NuiAccelerometerGetCurrentReading(&v);
	// m_nuiIStream->ProcessSkeleton(i,&SkeletonFrame.SkeletonData[i],&v,SkeletonFrame.liTimeStamp);
	hr =m_nuiIStream->ProcessSkeleton(NUI_SKELETON_COUNT, 
		SkeletonFrame.SkeletonData,
		&v,
		SkeletonFrame.liTimeStamp);
	if( FAILED( hr ) )
	{
		cout<<"Process Skeleton failed"<<endl;
	}

	return 0;
}

int ShowInteraction()
{
	NUI_INTERACTION_FRAME Interaction_Frame;
	m_nuiIStream->GetNextFrame( 0,&Interaction_Frame );
	int trackingID = 0;
	int event = 0;

	//cout<<"show Interactions!"<<endl;
	for(int i=0;i<NUI_SKELETON_COUNT;i++)
	{
		trackingID = Interaction_Frame.UserInfos[i].SkeletonTrackingId;

		// TODO: Add multi user grip support later?
		/* 
		m_LHandStats[i] = 
			Interaction_Frame.UserInfos[i].HandPointerInfos[0].HandEventType;
		m_RHandStats[i] = 
			Interaction_Frame.UserInfos[i].HandPointerInfos[1].HandEventType;
		*/

		// Temp implementation
		event = Interaction_Frame.UserInfos[i].HandPointerInfos[0].HandEventType;
		if(event != 0)
			m_LHandStat = event;

		event = Interaction_Frame.UserInfos[i].HandPointerInfos[1].HandEventType;
		if(event != 0)
			m_RHandStat = event;
		
	}

	return 0;
}


DWORD WINAPI KinectDataThread(LPVOID pParam)
{
	HANDLE hEvents[5] = {m_hEvNuiProcessStop,m_hNextColorFrameEvent,
		m_hNextDepthFrameEvent,m_hNextSkeletonEvent,m_hNextInteractionEvent};

	while(1)
	{
		int nEventIdx;
		nEventIdx=WaitForMultipleObjects(sizeof(hEvents)/sizeof(hEvents[0]),
			hEvents,FALSE,100);
		if (WAIT_OBJECT_0 == WaitForSingleObject(m_hEvNuiProcessStop, 0))
		{
			break;
		}
		// Process signal events
		if (WAIT_OBJECT_0 == WaitForSingleObject(m_hNextColorFrameEvent, 0))
		{
			DrawColor(m_pColorStreamHandle);
		}
		if (WAIT_OBJECT_0 == WaitForSingleObject(m_hNextDepthFrameEvent, 0))
		{
			DrawDepth(m_pDepthStreamHandle);
		}
		if (WAIT_OBJECT_0 == WaitForSingleObject(m_hNextSkeletonEvent, 0))
		{
			DrawSkeleton();
		}
		if (WAIT_OBJECT_0 == WaitForSingleObject(m_hNextInteractionEvent, 0))
		{
			ShowInteraction();
		}
	}

	m_nuiIStream->Disable();
	CloseHandle(m_hEvNuiProcessStop);
	m_hEvNuiProcessStop = NULL;
	CloseHandle( m_hNextSkeletonEvent );
	CloseHandle( m_hNextDepthFrameEvent );
	CloseHandle( m_hNextColorFrameEvent );
	CloseHandle( m_hNextInteractionEvent );
	
	
	m_pNuiSensor->NuiShutdown();
	m_pNuiSensor->Release();

	return 0;
}



int KinectDataProc()
{
	HANDLE hEvents[5] = {m_hEvNuiProcessStop,m_hNextColorFrameEvent,
		m_hNextDepthFrameEvent,m_hNextSkeletonEvent,m_hNextInteractionEvent};

	int nEventIdx;
	
	nEventIdx = WaitForMultipleObjects(sizeof(hEvents)/sizeof(hEvents[0]),
		hEvents,FALSE,100);
	
	if (WAIT_OBJECT_0 == WaitForSingleObject(m_hEvNuiProcessStop, 0))
	{
		//break;
		return -3;
	}
	// Process signal events
	if (WAIT_OBJECT_0 == WaitForSingleObject(m_hNextColorFrameEvent, 0))
	{
		DrawColor(m_pColorStreamHandle);
	}
	if (WAIT_OBJECT_0 == WaitForSingleObject(m_hNextDepthFrameEvent, 0))
	{
		DrawDepth(m_pDepthStreamHandle);
	}
	if (WAIT_OBJECT_0 == WaitForSingleObject(m_hNextSkeletonEvent, 0))
	{
		DrawSkeleton();
	}
	if (WAIT_OBJECT_0 == WaitForSingleObject(m_hNextInteractionEvent, 0))
	{
		ShowInteraction();
	}
	
	return 0;
}

DWORD ConnectKinect()
{
	
	INuiSensor * pNuiSensor;
	HRESULT hr;
	int iSensorCount = 0;
	
	hr = NuiGetSensorCount(&iSensorCount);

	if (FAILED(hr))
	{
		return hr;
	}
	// Look at each Kinect sensor
	for (int i = 0; i < iSensorCount; ++i)
	{
		// Create the sensor so we can check status, if we can't create it, move on to the next
		hr = NuiCreateSensorByIndex(i, &pNuiSensor);
		if (FAILED(hr))
		{
			continue;
		}
		// Get the status of the sensor, and if connected, then we can initialize it
		hr = pNuiSensor->NuiStatus();
		if (S_OK == hr)
		{
			m_pNuiSensor = pNuiSensor;
			break;
		}
		// This sensor wasn't OK, so release it since we're not using it
		pNuiSensor->Release();
	}

	if (NULL != m_pNuiSensor)
	{
		if (SUCCEEDED(hr))
		{   
			hr = m_pNuiSensor->NuiInitialize(\
				NUI_INITIALIZE_FLAG_USES_DEPTH_AND_PLAYER_INDEX|\
				NUI_INITIALIZE_FLAG_USES_COLOR|\
				NUI_INITIALIZE_FLAG_USES_SKELETON);
			if( hr != S_OK )
			{
				cout<<"NuiInitialize failed"<<endl;
				return hr;
			}

			m_hNextColorFrameEvent = CreateEvent( NULL, TRUE, FALSE, NULL );
			m_pColorStreamHandle = NULL;

			hr = m_pNuiSensor->NuiImageStreamOpen(
				NUI_IMAGE_TYPE_COLOR,NUI_IMAGE_RESOLUTION_640x480, 0, 2, 
				m_hNextColorFrameEvent, &m_pColorStreamHandle);
			if( FAILED( hr ) )
			{
				cout<<"Could not open image stream video"<<endl;
				return hr;
			}

			m_hNextDepthFrameEvent = CreateEvent( NULL, TRUE, FALSE, NULL );
			m_pDepthStreamHandle = NULL;

			hr = m_pNuiSensor->NuiImageStreamOpen( 
				NUI_IMAGE_TYPE_DEPTH_AND_PLAYER_INDEX,
				NUI_IMAGE_RESOLUTION_640x480, 0, 2, 
				m_hNextDepthFrameEvent, &m_pDepthStreamHandle);
			if( FAILED( hr ) )
			{
				cout<<"Could not open depth stream video"<<endl;
				return hr;
			}
			m_hNextSkeletonEvent = CreateEvent( NULL, TRUE, FALSE, NULL );
			hr = m_pNuiSensor->NuiSkeletonTrackingEnable( 
				m_hNextSkeletonEvent, 
				NUI_SKELETON_TRACKING_FLAG_ENABLE_IN_NEAR_RANGE//|
				);
			//0);
			if( FAILED( hr ) )
			{
				cout<<"Could not open skeleton stream video"<<endl;
				return hr;
			}
		}
	}
	if (NULL == m_pNuiSensor || FAILED(hr))
	{
		cout<<"No ready Kinect found!"<<endl;
		return E_FAIL;
	}
	return hr;
}

// Previous main()
int Init(HANDGRIP_UPDATE_MODE a_mode)
{
	cout << "test" << endl;
	
	ConnectKinect();
	
	HRESULT hr;
	
	m_hNextInteractionEvent = CreateEvent( NULL,TRUE,FALSE,NULL );
	m_hEvNuiProcessStop = CreateEvent(NULL,TRUE,FALSE,NULL);
	hr = NuiCreateInteractionStream(m_pNuiSensor,(INuiInteractionClient *)&m_nuiIClient,&m_nuiIStream);
	
	if( FAILED( hr ) )
	{
		cout<<"Could not open Interation stream video"<<endl;
		return hr;
	}
	
	// hr = NuiCreateInteractionStream(m_pNuiSensor,0,&m_nuiIStream);
	hr = m_nuiIStream->Enable(m_hNextInteractionEvent);
	
	if( FAILED( hr ) )
	{
		cout<<"Could not open Interation stream video"<<endl;
		return hr;
	}

	if(a_mode == HANDGRIP_UPDATE_MODE::THREAD)
		m_hProcesss = CreateThread(NULL, 0, KinectDataThread, 0, 0, &m_dwThreadID);
	
	return 0;
}


int Finish()
{	
	
	if(m_mode != HANDGRIP_UPDATE_MODE::THREAD)
	{
		// Must DISABLE the stream before Release the sensor
		m_nuiIStream->Disable();

		CloseHandle(m_hEvNuiProcessStop);
		m_hEvNuiProcessStop = NULL;

		CloseHandle( m_hNextInteractionEvent );
		CloseHandle( m_hNextSkeletonEvent );
		CloseHandle( m_hNextDepthFrameEvent );
		CloseHandle( m_hNextColorFrameEvent );
		
		m_pNuiSensor->NuiShutdown();
		m_pNuiSensor->Release();
	}

	/*
	if(m_mode == HANDGRIP_UPDATE_MODE::THREAD)
	{
		PDWORD pdwExitCode = 0;
		GetExitCodeThread(m_hProcesss, pdwExitCode);

		if( *pdwExitCode == STILL_ACTIVE)
			TerminateThread(m_hProcesss, *pdwExitCode);
	}
	*/
	return 0;
}



///// Interfaces for Unity /////
extern "C" int EXPORT_API InitKinectInteraction(int a_mode)
{
	m_mode = static_cast<HANDGRIP_UPDATE_MODE>( a_mode );
	int ret = Init(m_mode);

	return ret;
}


extern "C" int EXPORT_API FinishKinectInteraction()
{
	Finish();
	
	return 0;
}

// Called from Unity Update()
extern "C" int EXPORT_API UpdateKinectData()	
{
	switch (m_mode)
	{
	case FROM_UNITY:
		return KinectDataProc();
		break;
	case THREAD:
		return -1;
		break;
	default:
		return -2;
		break;
	}
}

// Get Left Hand Grip State
// 0 = None, 1 = Grip, 2 = Release
extern "C" int EXPORT_API GetLHandStat()
{
	return m_LHandStat;
}

// Get Right Hand Grip State
// 0 = None, 1 = Grip, 2 = Release
extern "C" int EXPORT_API GetRHandStat()
{
	return m_RHandStat;
}
