#pragma once

#include "stdafx.h"
#include "mediaobj.h"
#include "KinectAudioSpecs.h"

// Summary:
//		IMediaBuffer implementation for a statically allocated buffer.
//		Intended to store Kinect audio data.
//
class StaticMediaBuffer : public IMediaBuffer
{
public:

	StaticMediaBuffer::StaticMediaBuffer() : m_dataLength(0) {}

	// IUnknown methods
	inline STDMETHODIMP_(ULONG) StaticMediaBuffer::AddRef() { return 2; }
	inline STDMETHODIMP_(ULONG) StaticMediaBuffer::Release() { return 1; }
	inline STDMETHODIMP StaticMediaBuffer::QueryInterface(REFIID riid, void **ppv)
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
	inline STDMETHODIMP StaticMediaBuffer::SetLength(DWORD length) {m_dataLength = length; return NOERROR;}
	inline STDMETHODIMP StaticMediaBuffer::GetMaxLength(DWORD *pMaxLength) {*pMaxLength = sizeof(m_pData); return NOERROR;}
	inline STDMETHODIMP StaticMediaBuffer::GetBufferAndLength(BYTE **ppBuffer, DWORD *pLength)
	{
		if (ppBuffer)
		{
			*ppBuffer = m_pData;
		}
		if (pLength)
		{
			*pLength = m_dataLength;
		}
		return NOERROR;
	}
	inline void StaticMediaBuffer::Init(ULONG ulData)
	{
		m_dataLength = ulData;
	}

protected:

	BYTE m_pData[AudioSamplesPerSecond * AudioBlockAlign];	// Statically allocated buffer used to hold audio data returned by IMediaObject
	ULONG m_dataLength;		// Amount of data currently being held in m_pData

};