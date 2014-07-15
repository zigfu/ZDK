Upon building the ZDK Solution, this folder should contain the following:

FaceTrackData.dll
FaceTrackLib.dll
NativeFaceTrackLibWrapper.dll
ZDK.dll
ZigNativeKinectAudioSourceDll.dll


These all need to be copied into the Unity project's root directory.

Alternatively you could edit the Post-Build Events for each project so that it copies the built DLL directly to your Unity Project's root directory instead of the "Output - Unity Plugins" folder.