// KinectGrip170_CLR20.h

#pragma once
// Which platform we are on?
#if _MSC_VER
#define UNITY_WIN 1
#else
#define UNITY_OSX 1
#endif

// Attribute to make function be exported from a plugin
#if UNITY_WIN
#define EXPORT_API __declspec(dllexport)
#else
#define EXPORT_API
#endif


//// Interface for Unity ////
extern "C" int EXPORT_API InitKinectInteraction();  // Should be called at Start()

extern "C" int EXPORT_API FinishKinectInteraction();  // Called when the application ends

extern "C" int EXPORT_API GetLHandStat();	// 0 = None, 1 = Grip, 2 = Release
extern "C" int EXPORT_API GetRHandStat();   // 0 = None, 1 = Grip, 2 = Release
extern "C" int EXPORT_API UpdateKinectData();	// Called from Unity Update()


