#pragma once

#include "stdafx.h"
#include <string>

using namespace std;

class StringHelper
{
public:

	static inline bool ByteArrayToCString(BYTE byteArray[], DWORD length, char** pCString)
	{
		if(pCString == NULL) { return false; }

		char* temp = (char*)calloc(length + 1, sizeof(char));

		for (DWORD i = 0; i < length; i++)
		{
			temp[i] = (char)byteArray[i];
		}
		temp[length] = NULL;	// NULL-terminated

		*pCString = temp;

		return true;
	}

	static inline string LPCWSTR_to_stdString(LPCWSTR inStr)
	{
		wstring temp = inStr;
		string outStr;
		outStr.resize(temp.size());
		copy(temp.begin(), temp.end(), outStr.begin());

		return outStr;
	}	

};
