using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using static V;

public static class Windows {
	[DllImport("user32.dll")]
	private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
	// delegate to filter which windows to include 
	public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

	/// <summary>Find all windows that match the given filter</summary>
	public static List<Window> FindWindows() {
		var windows = new List<Window>();
		EnumWindows((IntPtr handle, IntPtr param)=> {
			windows.Add(new Window(handle));
			return true; // continue, so the next window gets processed
		}, IntPtr.Zero);
		return windows;
	}
}

public class Window {
	public Window(IntPtr handle) {
		this.handle = handle;
		Show = async nothing=>Show_();
		Hide = async nothing=>Hide_();
		Activate = async nothing=>Activate_();
		GetText = async nothing=>GetText_();
		GetClass = async nothing=>GetClass_();
		GetProcessPath = async nothing=>GetProcessPath_();
		GetProcessName = async nothing=>GetProcessName_();
		GetProcessID = async nothing=>GetProcessID_();
		GetThreadID = async nothing=>GetThreadID_();
	}

	public IntPtr handle;
	public Func<object, Task<object>> Show;
	public bool Show_() {
		Log("Showing window:" + handle);
		ShowWindow(handle, WindowShowStyle.Show);
		/*var style = (WindowStyles)GetWindowLongPtr(handle, 0);
		style |= WindowStyles.WS_VISIBLE;
		SetWindowLongPtr(handle, GWL_STYLE, (IntPtr)style);*/
		return true;
	}
	public Func<object, Task<object>> Hide;
	public bool Hide_() {
		Log("Hiding window:" + handle);
		ShowWindow(handle, WindowShowStyle.Hide);
		return true;
	}
	public Func<object, Task<object>> Activate;
	public bool Activate_() {
		Log("Activating window:" + handle);
		SetForegroundWindow(handle);
		return true;
	}

	[DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hwnd, WindowShowStyle nCmdShow);
	[DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);

	/*public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex) {
		if (IntPtr.Size == 8)
			return GetWindowLongPtr64(hWnd, nIndex);
		return GetWindowLongPtr32(hWnd, nIndex);
	}
	[DllImport("user32.dll", EntryPoint = "GetWindowLong")] static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);
	[DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")] static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);
	static int GWL_STYLE = -16;

	// This helper static method is required because the 32-bit version of user32.dll does not contain this API
	// (on any versions of Windows), so linking the method will fail at run-time. The bridge dispatches the request
	// to the correct function (GetWindowLong in 32-bit mode and GetWindowLongPtr in 64-bit mode)
	public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong) {
		if (IntPtr.Size == 8)
			return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
		return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
	}
	[DllImport("user32.dll", EntryPoint = "SetWindowLong")] static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);
	[DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")] static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);*/

	/// <summary>Get the text/title for the window</summary>
	public Func<object, Task<object>> GetText;
	public string GetText_() {
		int size = GetWindowTextLength(handle);
		if (size > 0) {
			var builder = new StringBuilder(size + 1);
			GetWindowText(handle, builder, builder.Capacity);
			return builder.ToString();
		}
		return "";
	}

	[DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowTextLength(IntPtr hWnd);
	[DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

	public Func<object, Task<object>> GetClass;
	public string GetClass_() {
		var builder = new StringBuilder();
		var success = GetClassName(handle, builder, 1024);
		return builder.ToString();
	}

	[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)] static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

	public Func<object, Task<object>> GetProcessPath;
	// make-so: this works
	public string GetProcessPath_() {
		/*var builder = new StringBuilder(1024);
		//int pathLength = GetModuleFileName((IntPtr)GetProcessID_(), builder, builder.Capacity);
		return builder.ToString();*/

		/*var process = Process.GetProcessById((int)GetProcessID_());
		return process.MainModule.FileName;*/

		int capacity = 1024;
		StringBuilder pathBuilder = new StringBuilder(capacity);
		IntPtr hProcess = OpenProcess(ProcessAccessFlags.QueryLimitedInformation, false, (int)GetProcessID_());
		int pathLength = GetModuleFileNameEx(hProcess, IntPtr.Zero, pathBuilder, capacity);
		CloseHandle(hProcess);
		var fullPath = pathBuilder.ToString();
		//Log("FullPath)" + fullPath);
		return fullPath;
	}
	public Func<object, Task<object>> GetProcessName;
	public string GetProcessName_() {
		var path = GetProcessPath_();
		if (path.Length == 0)
			return "";
		var file = new FileInfo(path);
		return file.Name;
	}
	public Func<object, Task<object>> GetProcessID;
	public uint GetProcessID_() {
		uint processID = 0;
		uint threadID = GetWindowThreadProcessId(handle, out processID);
		return processID;
	}
	public Func<object, Task<object>> GetThreadID;
	public uint GetThreadID_() {
		uint processID = 0;
		uint threadID = GetWindowThreadProcessId(handle, out processID);
		return threadID;
	}

	[DllImport("user32", SetLastError = true)] static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
	//[DllImport("kernel32", SetLastError = true)] static extern int GetModuleFileName(IntPtr hModule, StringBuilder lpFilename, int nSize);

	[DllImport("psapi.dll", SetLastError = true)] static extern int GetModuleFileNameEx(IntPtr processHandle, IntPtr moduleHandle, StringBuilder pathBuilder, int pathSize);
	[Flags] public enum ProcessAccessFlags : uint {
		All = 0x001F0FFF,
		Terminate = 0x00000001,
		CreateThread = 0x00000002,
		VirtualMemoryOperation = 0x00000008,
		VirtualMemoryRead = 0x00000010,
		VirtualMemoryWrite = 0x00000020,
		DuplicateHandle = 0x00000040,
		CreateProcess = 0x000000080,
		SetQuota = 0x00000100,
		SetInformation = 0x00000200,
		QueryInformation = 0x00000400,
		QueryLimitedInformation = 0x00001000,
		Synchronize = 0x00100000
	}
	[DllImport("kernel32.dll", SetLastError = true)] public static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);
	[DllImport("kernel32.dll", SetLastError = true)]
	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
	[SuppressUnmanagedCodeSecurity] [return: MarshalAs(UnmanagedType.Bool)]
	static extern bool CloseHandle(IntPtr hObject);

	//[DllImport("kernel32.dll", SetLastError = true)] static extern bool QueryFullProcessImageName([In] IntPtr hProcess, [In] int dwFlags, [Out] StringBuilder lpExeName, ref int lpdwSize);
}