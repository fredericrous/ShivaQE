// GlobalCbtHook.cpp
//   by Chris Wilson

#include "stdafx.h"
#include <windows.h>
#include "GlobalCbtHook.h"

extern "C" VOID __cdecl SetSharedMem(HWND lpszBuf, int pos);
extern "C" HWND __cdecl GetSharedMem( int pos );

HHOOK hookCbt = NULL;
HHOOK hookShell = NULL;
HHOOK hookCallWndProc = NULL;
HHOOK hookGetMsg = NULL;

//
// Store the application instance of this module to pass to
// hook initialization. This is set in DLLMain().
//
HINSTANCE g_appInstance = NULL;

typedef void (CALLBACK *HookProc)(int code, WPARAM w, LPARAM l);

static LRESULT CALLBACK CbtHookCallback(int code, WPARAM wparam, LPARAM lparam);
static LRESULT CALLBACK ShellHookCallback(int code, WPARAM wparam, LPARAM lparam);
static LRESULT CALLBACK CallWndProcHookCallback(int code, WPARAM wparam, LPARAM lparam);
static LRESULT CALLBACK GetMsgHookCallback(int code, WPARAM wparam, LPARAM lparam);

bool InitializeCbtHook(int threadID, HWND destination)
{
	if (g_appInstance == NULL)
	{
		return false;
	}

	SetSharedMem(destination, 0);

	hookCbt = SetWindowsHookEx(WH_CBT, (HOOKPROC)CbtHookCallback, g_appInstance, threadID);
	return hookCbt != NULL;
}

void UninitializeCbtHook()
{
	if (hookCbt != NULL)
		UnhookWindowsHookEx(hookCbt);
	hookCbt = NULL;
}

static LRESULT CALLBACK CbtHookCallback(int code, WPARAM wparam, LPARAM lparam)
{
	if (code >= 0)
	{
		UINT msg = 0;

		if (code == HCBT_ACTIVATE)
			msg = RegisterWindowMessage("SHIVAQE_HOOK_HCBT_ACTIVATE");
		else if (code == HCBT_CREATEWND)
			msg = RegisterWindowMessage("SHIVAQE_HOOK_HCBT_CREATEWND");
		else if (code == HCBT_DESTROYWND)
			msg = RegisterWindowMessage("SHIVAQE_HOOK_HCBT_DESTROYWND");
		else if (code == HCBT_MINMAX)
			msg = RegisterWindowMessage("SHIVAQE_HOOK_HCBT_MINMAX");
		else if (code == HCBT_MOVESIZE)
			msg = RegisterWindowMessage("SHIVAQE_HOOK_HCBT_MOVESIZE");
		else if (code == HCBT_SETFOCUS)
			msg = RegisterWindowMessage("SHIVAQE_HOOK_HCBT_SETFOCUS");
		else if (code == HCBT_SYSCOMMAND)
			msg = RegisterWindowMessage("SHIVAQE_HOOK_HCBT_SYSCOMMAND");

		HWND dstWnd = GetSharedMem(0);

		if (msg != 0)
			SendNotifyMessage(dstWnd, msg, wparam, lparam);
	}

	return CallNextHookEx(hookCbt, code, wparam, lparam);
}

bool InitializeShellHook(int threadID, HWND destination)
{
	if (g_appInstance == NULL)
	{
		return false;
	}
	
	SetSharedMem(destination,1);

	hookShell = SetWindowsHookEx(WH_SHELL, (HOOKPROC)ShellHookCallback, g_appInstance, threadID);
	return hookShell != NULL;
}

void UninitializeShellHook()
{
	if (hookShell != NULL)
		UnhookWindowsHookEx(hookShell);
	hookShell = NULL;
}

static LRESULT CALLBACK ShellHookCallback(int code, WPARAM wparam, LPARAM lparam)
{
	if (code >= 0)
	{
		UINT msg = 0;

		if (code == HSHELL_ACTIVATESHELLWINDOW)
			msg = RegisterWindowMessage("SHIVAQE_HOOK_HSHELL_ACTIVATESHELLWINDOW");
		else if (code == HSHELL_GETMINRECT)
			msg = RegisterWindowMessage("SHIVAQE_HOOK_HSHELL_GETMINRECT");
		else if (code == HSHELL_LANGUAGE)
			msg = RegisterWindowMessage("SHIVAQE_HOOK_HSHELL_LANGUAGE");
		else if (code == HSHELL_REDRAW)
			msg = RegisterWindowMessage("SHIVAQE_HOOK_HSHELL_REDRAW");
		else if (code == HSHELL_TASKMAN)
			msg = RegisterWindowMessage("SHIVAQE_HOOK_HSHELL_TASKMAN");
		else if (code == HSHELL_WINDOWACTIVATED)
			msg = RegisterWindowMessage("SHIVAQE_HOOK_HSHELL_WINDOWACTIVATED");
		else if (code == HSHELL_WINDOWCREATED)
			msg = RegisterWindowMessage("SHIVAQE_HOOK_HSHELL_WINDOWCREATED");
		else if (code == HSHELL_WINDOWDESTROYED)
			msg = RegisterWindowMessage("SHIVAQE_HOOK_HSHELL_WINDOWDESTROYED");

		HWND dstWnd = GetSharedMem(1);

		if (msg != 0)
			SendNotifyMessage(dstWnd, msg, wparam, lparam);
	}

	return CallNextHookEx(hookShell, code, wparam, lparam);
}

bool InitializeCallWndProcHook(int threadID, HWND destination)
{
	if (g_appInstance == NULL)
	{
		return false;
	}

	SetSharedMem(destination,2);

	hookCallWndProc = SetWindowsHookEx(WH_CALLWNDPROC, (HOOKPROC)CallWndProcHookCallback, g_appInstance, threadID);
	return hookCallWndProc != NULL;
}

void UninitializeCallWndProcHook()
{
	if (hookCallWndProc != NULL)
		UnhookWindowsHookEx(hookCallWndProc);
	hookCallWndProc = NULL;
}

static LRESULT CALLBACK CallWndProcHookCallback(int code, WPARAM wparam, LPARAM lparam)
{
	if (code >= 0)
	{
		UINT msg = 0;
		UINT msg2 = 0;

		msg = RegisterWindowMessage("SHIVAQE_HOOK_CALLWNDPROC");
		msg2 = RegisterWindowMessage("SHIVAQE_HOOK_CALLWNDPROC_PARAMS");

		HWND dstWnd = GetSharedMem(2);
		
		CWPSTRUCT* pCwpStruct = (CWPSTRUCT*)lparam;

		if (msg != 0 && pCwpStruct->message != msg && pCwpStruct->message != msg2)
		{
			SendNotifyMessage(dstWnd, msg, (WPARAM)pCwpStruct->hwnd, pCwpStruct->message);
			SendNotifyMessage(dstWnd, msg2, pCwpStruct->wParam, pCwpStruct->lParam);
		}
	}

	return CallNextHookEx(hookCallWndProc, code, wparam, lparam);
}

bool InitializeGetMsgHook(int threadID, HWND destination)
{
	if (g_appInstance == NULL)
	{
		return false;
	}

	SetSharedMem(destination,3);

	hookGetMsg = SetWindowsHookEx(WH_GETMESSAGE, (HOOKPROC)GetMsgHookCallback, g_appInstance, threadID);
	return hookGetMsg != NULL;
}

void UninitializeGetMsgHook()
{
	if (hookGetMsg != NULL)
		UnhookWindowsHookEx(hookGetMsg);
	hookGetMsg = NULL;
}

static LRESULT CALLBACK GetMsgHookCallback(int code, WPARAM wparam, LPARAM lparam)
{
	if (code >= 0)
	{
		UINT msg = 0;
		UINT msg2 = 0;

		msg = RegisterWindowMessage("SHIVAQE_HOOK_GETMSG");
		msg2 = RegisterWindowMessage("SHIVAQE_HOOK_GETMSG_PARAMS");

		HWND dstWnd = GetSharedMem(3);
		
		MSG* pMsg = (MSG*)lparam;

		if (msg != 0 && pMsg->message != msg && pMsg->message != msg2)
		{
			SendNotifyMessage(dstWnd, msg, (WPARAM)pMsg->hwnd, pMsg->message);
			SendNotifyMessage(dstWnd, msg2, pMsg->wParam, pMsg->lParam);
		}
	}

	return CallNextHookEx(hookGetMsg, code, wparam, lparam);
}
