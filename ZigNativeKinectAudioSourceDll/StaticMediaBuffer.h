#pragma once

#include "stdafx.h"
#include "mediaobj.h"
#include "KinectAudioSpecs.h"


// Summary:
//		IMediaBuffer implementation for a statically allocated buffer.
//		 Intended to store Kinect audio data.
//
class StaticMediaBuffer : public IMediaBuffer
{
public:

	StaticMediaBuffer() : m_dataLength(0) {}

	// IUnknown methods
	inline STDMETHODIMP_(ULONG) AddRef() { return 2; }
	inline STDMETHODIMP_(ULONG) Release() { return 1; }
	inline STDMETHODIMP QueryInterface(REFIID riid, void **ppv)
	{
		if (riid == IID_IUnknown)
		{
			AddRef();
			*ppv = (IUnknown*)this;
			return NOERROR;
		}
		else if (riid == IID_IMediaBuffer)
		{
			AddRef();
			*ppv = (IMediaBuffer*)this;
			return NOERROR;
		}
		else
		{
			return E_NOINTERFACE;
		}
	}

	// IMediaBuffer methods
	inline STDMETHODIMP SetLength(DWORD length) {m_dataLength = length; return NOERROR;}
	inline STDMETHODIMP GetMaxLength(DWORD *pMaxLength) {*pMaxLength = sizeof(m_pData); return NOERROR;}
	inline STDMETHODIMP GetBufferAndLength(BYTE **ppBuffer, DWORD *pLength)
	{
		if (ppBuffer)	{ *ppBuffer = m_pData; }
		if (pLength)	{ *pLength = m_dataLength; }

		return NOERROR;
	}
	inline void Init(ULONG ulData)
	{
		m_dataLength = ulData;
	}

protected:

	BYTE m_pData[AudioSamplesPerSecond * AudioBlockAlign];	// Statically allocated buffer used to hold audio data returned by IMediaObject
	ULONG m_dataLength;		// Amount of data currently being held in m_pData

};