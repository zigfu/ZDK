#pragma once

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN			// Exclude rarely-used stuff from Windows headers
#endif

#ifndef EXPORT_API
#define EXPORT_API __declspec(dllexport) 
#endif


#include "targetver.h"

// Windows Header Files
#include <windows.h>
#include <Shlobj.h>


// Safe release for interfaces
template<class Interface>
inline void SafeRelease( Interface *& pInterfaceToRelease )
{
    if ( pInterfaceToRelease != NULL )
    {
        pInterfaceToRelease->Release();
        pInterfaceToRelease = NULL;
    }
}