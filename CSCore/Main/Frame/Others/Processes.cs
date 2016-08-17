using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static V;

using Map = System.Collections.Generic.Dictionary<string, object>;

[Flags]
public enum ProcessAccessFlags : uint {
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
public static class Processes {
	[DllImport("psapi.dll", SetLastError = true)]
	public static extern int GetModuleFileNameEx(IntPtr processHandle, IntPtr moduleHandle, StringBuilder pathBuilder, int pathSize);
	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);
	[DllImport("kernel32.dll", SetLastError = true)]
	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
	[SuppressUnmanagedCodeSecurity] [return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool CloseHandle(IntPtr hObject);

	//[DllImport("kernel32.dll", SetLastError = true)] static extern bool QueryFullProcessImageName([In] IntPtr hProcess, [In] int dwFlags, [Out] StringBuilder lpExeName, ref int lpdwSize);

	public static string GetArgsStr(int processID) {
		try {
			// this approach is slower
			/*//var commandLine = new StringBuilder('"' + process.MainModule.FileName + '"');
			var commandLine = new StringBuilder();

			using (var searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + processID)) {
				foreach (var entry in searcher.Get()) {
					if (commandLine.Length > 0)
						commandLine.Append(" ");
					commandLine.Append(entry["CommandLine"]);
				}
			}

			return commandLine.ToString();*/

			return Processes_NTQuery.GetCommandLine(processID);
		}
		catch (Win32Exception ex) {
			if ((uint)ex.ErrorCode == 0x80004005)
				return "Could not retrieve process command-line (AccessDenied). Error) " + ex;
			throw;
		}
	}
}

public class VProcess {
	public VProcess(int id) {
		this.id = id;
		PostInit();
	}
	public VProcess(Process process) {
		id = process.Id;
		this.process = process;
		PostInit();
	}
	void PostInit() {
		// auto-set props for methods
		var typeInfo = VTypeInfo.Get(typeof(VProcess));
		foreach (var method in typeInfo.methods.Values) {
			if (method.memberInfo.Name.EndsWith("_")) {
				var prop = typeInfo.props[method.memberInfo.Name.Substring(0, method.memberInfo.Name.Length - 1)];
				if (prop.GetValue(this) == null)
					prop.SetValue(this, (Func<object, Task<object>>)(async options => {
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
	
	public int id;
	// Process.GetProcessByID is too slow to call every time (5ms), so only call if trying to access prop that needs it
	Process process;
	Process Process {
		get {
			if (process == null)
				process = Process.GetProcessById(id);
			return process;
		}
	}

	public Func<object, Task<object>> GetPath;
	//public string GetPath_() { return process.MainModule.FileName; }
	public string GetPath_() {
		// too slow (5ms)
		/*var process = Process.GetProcessById((int)GetProcessID_());
		return process.MainModule.FileName;*/

		StringBuilder pathBuilder = new StringBuilder(1024);
		IntPtr processHandle = Processes.OpenProcess(ProcessAccessFlags.QueryLimitedInformation, false, id);
		//int pathLength = GetModuleFileName((IntPtr)GetProcessID_(), builder, builder.Capacity);
		/*int pathLength =*/ Processes.GetModuleFileNameEx(processHandle, IntPtr.Zero, pathBuilder, pathBuilder.Capacity);
		Processes.CloseHandle(processHandle);
		var fullPath = pathBuilder.ToString();
		//Log("FullPath)" + fullPath);
		return fullPath;
	}

	public Func<object, Task<object>> GetName;
	//public string GetName_() { return process.ProcessName; }
	public string GetName_() {
		var path = GetPath_();
		if (path.Length == 0) return "";
		var file = new FileInfo(path);
		return Path.GetFileNameWithoutExtension(file.Name);
	}
	
	public Func<object, Task<object>> GetThreadIDs;
	public List<int> GetThreadIDs_() { return Process.Threads.OfType<ProcessThread>().Select(a=>a.Id).ToList(); }
	/*public Func<object, Task<object>> GetHandle;
	public IntPtr GetHandle_() { return Process.Handle; }*/
	public Func<object, Task<object>> GetArgsStr;
	public string GetArgsStr_() { return Processes.GetArgsStr(id); }

	public Func<object, Task<object>> Kill;
	public bool Kill_() {
		Process.Kill();
		return true;
	}

	public Func<object, Task<object>> GetMainWindow;
	public Window GetMainWindow_() { return new Window(Process.MainWindowHandle); }
}