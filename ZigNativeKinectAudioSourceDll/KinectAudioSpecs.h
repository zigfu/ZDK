#pragma once

#include <windows.h>
#include <mmreg.h>			// For WAVEFORMATEX

// Kinect audio stream Constants
static const WORD       AudioFormat = WAVE_FORMAT_PCM;
static const WORD       AudioChannels = 1;					
static const DWORD      AudioSamplesPerSecond = 16000;			// 16khz
static const DWORD      AudioAverageBytesPerSecond = 32000;	
static const WORD       AudioBlockAlign = 2;			
static const WORD       AudioBitsPerSample = 16;				// 16-bit