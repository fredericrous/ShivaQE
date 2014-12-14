#include "stdafx.h"
#include <windows.h>
#include <memory.h>

#define SHMEMSIZE 16 // we allocate 16 Bytes of memory ... thats 4 hwnds

//
// Capture the application instance of this module to pass to
// hook initialization.
//
extern HINSTANCE g_appInstance;

static LPVOID lpvMem = NULL;				// shared memory pointer
static HANDLE hMapObject = NULL;		// file mapping handle

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD ul_reason_for_call, LPVOID lpReserved)
{

	BOOL fInit, fIgnore;

	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
		//
		// Capture the application instance of this module to pass to
		// hook initialization.
		//
		if (g_appInstance == NULL) {
			g_appInstance = hinstDLL;
		}

		// Create a named file mapping object
		hMapObject = CreateFileMapping(
			INVALID_HANDLE_VALUE,
			NULL,
			PAGE_READWRITE,
			0,
			SHMEMSIZE,
			TEXT("dllmemfilemap"));

		if(hMapObject == NULL) return FALSE;

		// The first process to attach initializes memory
		fInit = (GetLastError() != ERROR_ALREADY_EXISTS);

		// Get a pointer to the file-mapped shared memory
		lpvMem = MapViewOfFile(
			hMapObject,
			FILE_MAP_WRITE,
			0, 0, 0);
		if(lpvMem == NULL) return FALSE;

		// Initialize memory if this is the first process
		if(fInit) memset(lpvMem, '\0', SHMEMSIZE);

		break;

	case DLL_THREAD_ATTACH:
		break;

	case DLL_THREAD_DETACH:
		break;

	case DLL_PROCESS_DETACH:

		// Unmap shared memory from the process's address space
		fIgnore = UnmapViewOfFile(lpvMem);
		// Close the process's handle to the file-mapping object
		fIgnore = CloseHandle(hMapObject);

		break;

	default:
		OutputDebugString("That's weird.\n");
		break;
	}

	return TRUE;
	UNREFERENCED_PARAMETER( hinstDLL );
	UNREFERENCED_PARAMETER( lpReserved );

}

// The export mechanism used here is the __declspec(export)
// method supported by Microsoft Visual Studio, but any
// other export method supported by your development
// environment may be substituted.

#ifdef __cplusplus    // If used by C++ code, 
extern "C" {          // we need to export the C interface
#endif
 
// SetSharedMem sets the contents of the shared memory 
__declspec(dllexport) VOID __cdecl SetSharedMem(HWND value, int pos) 
{ 
    long * lpszTmp; 
 
    // Get the address of the shared memory block
    lpszTmp = (long*) lpvMem; 
	lpszTmp += (pos * 4);

    // Copy value to the memory
	*lpszTmp = (long)value;
	
} 
 
// GetSharedMem gets the contents of the shared memory
__declspec(dllexport) HWND __cdecl GetSharedMem( int pos ) 
{ 
    long * lpszTmp;

    // Get the address of the shared memory block
    lpszTmp = (long*) lpvMem;
	lpszTmp += (pos * 4);
 
    // Copy from shared memory into the caller's buffer
	return (HWND)*lpszTmp;
}
#ifdef __cplusplus
}
#endif
