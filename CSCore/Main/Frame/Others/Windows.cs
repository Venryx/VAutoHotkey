using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static V;

using Map = System.Collections.Generic.Dictionary<string, object>;

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
		Show = async args=>Show_();
		Hide = async args=>Hide_();
		Activate = async args=>Activate_();
		GetText = async args=>GetText_();
		GetClass = async args=>GetClass_();
		GetProcessPath = async args=>GetProcessPath_();
		GetProcessName = async args=>GetProcessName_();
		GetProcessID = async args=>GetProcessID_();
		GetThreadID = async args=>GetThreadID_();
		IsVisible = async args=>IsVisible_();
		SetPosition = async args=>SetPosition_(Main.ProcessArgs(args)[0]);
		SetSize = async args=>SetSize_(Main.ProcessArgs(args)[0]);
		SetPositionAndSize = async args=>SetPositionAndSize_(Main.ProcessArgs(args)[0]);

		// auto-set props for methods
		var typeInfo = VTypeInfo.Get(typeof(Window));
		foreach (var method in typeInfo.methods.Values) {
			if (method.memberInfo.Name.EndsWith("_")) {
				var prop = typeInfo.props[method.memberInfo.Name.Substring(0, method.memberInfo.Name.Length - 1)];
				if (prop.GetValue(this) == null)
					prop.SetValue(this, (Func<object, Task<object>>)(async options=> {
						await Task.Delay(0); // hide VS warning
						if (method.memberInfo.GetParameters().Length == 0)
							return method.Call(this);
						if (method.memberInfo.GetParameters().Length == 1 && method.memberInfo.GetParameters()[0].ParameterType == typeof(DynamicObject))
							return method.Call(this, options);
						throw new Exception($"NodeJS-accessible prop does not exist for method '{method.memberInfo.Name}'.");
					}));
			}
		}
	}
	
	public IntPtr handle;

	public Func<object, Task<object>> Show;
	public bool Show_() {
		//Log("Showing window:" + handle);
		ShowWindow(handle, WindowShowStyle.Show);
		/*var style = (WindowStyles)GetWindowLongPtr(handle, 0);
		style |= WindowStyles.WS_VISIBLE;
		SetWindowLongPtr(handle, GWL_STYLE, (IntPtr)style);*/
		return true;
	}
	public Func<object, Task<object>> Hide;
	public bool Hide_() {
		//Log("Hiding window:" + handle);
		ShowWindow(handle, WindowShowStyle.Hide);
		return true;
	}
	public Func<object, Task<object>> Activate;
	public bool Activate_() {
		//Log("Activating window:" + handle);
		SetForegroundWindow(handle);
		return true;
	}

	public Func<object, Task<object>> IsVisible;
	public bool IsVisible_() { return IsWindowVisible(handle); }

	[DllImport("user32", SetLastError = true)]
	static extern bool IsWindowVisible(IntPtr windowHandle);

	public Func<object, Task<object>> IsNormal;
	public bool IsNormal_() { return GetWindowState_() == WindowState.Normal; }
	public Func<object, Task<object>> IsMinimized;
	public bool IsMinimized_() { return GetWindowState_() == WindowState.Minimized; }
	public Func<object, Task<object>> IsMaximized;
	public bool IsMaximized_() { return GetWindowState_() == WindowState.Maximized; }

	public Func<object, Task<object>> GetWindowState;
	public WindowState GetWindowState_() {
		var placement = new WindowPlacement();
		GetWindowPlacement(handle, ref placement);
		/*if (placement.showCmd == 1)
			return WindowState.Normal;
		if (placement.showCmd == 2)
			return WindowState.Minimized;
		if (placement.showCmd == 3)
			return WindowState.Maximized;
		return WindowState.Unknown;*/
		return placement.showCmd;
	}
	[DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetWindowPlacement(IntPtr hWnd, ref WindowPlacement lpwndpl);

	public Func<object, Task<object>> Minimize;
	public bool Minimize_() {
		ShowWindow(handle, WindowShowStyle.Minimize);
		return true;
	}
	public Func<object, Task<object>> Restore;
	public bool Restore_() {
		ShowWindow(handle, WindowShowStyle.Restore);
		return true;
	}
	public Func<object, Task<object>> Maximize;
	public bool Maximize_() {
		ShowWindow(handle, WindowShowStyle.Maximize);
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

	public Func<object, Task<object>> SetPosition;
	public bool SetPosition_(dynamic options) {
		/*var optionsMap = new Map {{"x", (int?)options.x ?? -1}, {"y", (int?)options.y ?? -1}};
		return SetPositionAndSize_(new Map_Dynamic(optionsMap));*/
		var x = (int?)options.x ?? -1;
		var y = (int?)options.y ?? -1;
		bool worked = SetWindowPos(handle, SpecialWindowHandles.NoTopMost, x, y, 0, 0, SetWindowPosFlags.NOSIZE);
		if (!worked) throw new Win32Exception();
		return true;
	}
	public Func<object, Task<object>> SetSize;
	public bool SetSize_(dynamic options) {
		var width = (int?)options.width ?? -1;
		var height = (int?)options.height ?? -1;
		bool worked = SetWindowPos(handle, SpecialWindowHandles.NoTopMost, 0, 0, width, height, SetWindowPosFlags.NOREPOSITION);
		if (!worked) throw new Win32Exception();
		return true;
	}
	public Func<object, Task<object>> SetPositionAndSize;
	public bool SetPositionAndSize_(dynamic options) {
		var x = (int?)options.x ?? -1;
		var y = (int?)options.y ?? -1;
		var width = (int?)options.width ?? -1;
		var height = (int?)options.height ?? -1;
		//bool worked = MoveWindow(handle, x, y, width, height, false);
		bool worked = SetWindowPos(handle, IntPtr.Zero, x, y, width, height, 0);
		if (!worked) throw new Win32Exception();
		return true;
	}

	//[DllImport("user32.dll", SetLastError = true)] static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);
	[DllImport("user32.dll", SetLastError = true)]
	static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, SetWindowPosFlags uFlags);
	/// <summary>Window handles (HWND) used for hWndInsertAfter</summary>
	public static class SpecialWindowHandles {
		public static IntPtr
		NoTopMost = new IntPtr(-2),
		TopMost = new IntPtr(-1),
		Top = new IntPtr(0),
		Bottom = new IntPtr(1);
	}
	public enum SetWindowPosFlags {
		NOSIZE = 0x0001,
		NOMOVE = 0x0002,
		NOZORDER = 0x0004,
		NOREDRAW = 0x0008,
		NOACTIVATE = 0x0010,
		DRAWFRAME = 0x0020,
		FRAMECHANGED = 0x0020,
		SHOWWINDOW = 0x0040,
		HIDEWINDOW = 0x0080,
		NOCOPYBITS = 0x0100,
		NOOWNERZORDER = 0x0200,
		NOREPOSITION = 0x0200,
		NOSENDCHANGING = 0x0400,
		DEFERERASE = 0x2000,
		ASYNCWINDOWPOS = 0x4000
	}
}