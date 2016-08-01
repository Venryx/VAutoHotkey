using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using SystemEventsN;
using Gma.UserActivityMonitor;
using Microsoft.Win32;
using static V;

using Map = System.Collections.Generic.Dictionary<string, object>;
using SystemEvents = SystemEventsN.SystemEvents;

public static class Methods {
	// set-up
	// ==========

	// HookManager events:
	// MouseMove, MouseMoveExt, MouseClick, MouseClickExt, MouseDown, MouseUp, MouseWheel, MouseDoubleClick
	// KeyPress, KeyUp, KeyDown

	static Dictionary<string, Keys> keyNameOverrides = new Dictionary<string, Keys> {
		{"Control", Keys.ControlKey}, {"Shift", Keys.ShiftKey}, {"Alt", Keys.Menu},
		{"`", Keys.Oemtilde}, {"~", Keys.Oemtilde},
	};
	static Dictionary<Keys, Keys> keySimplifications = new Dictionary<Keys, Keys> {
		{Keys.Control, Keys.ControlKey}, {Keys.LControlKey, Keys.ControlKey}, {Keys.RControlKey, Keys.ControlKey},
		{Keys.Shift, Keys.ShiftKey}, {Keys.LShiftKey, Keys.ShiftKey}, {Keys.RShiftKey, Keys.ShiftKey},
		{Keys.Alt, Keys.Menu}, {Keys.LMenu, Keys.Menu}, {Keys.RMenu, Keys.Menu}
	};

	static Dictionary<Keys, bool> keyDowns = new Dictionary<Keys, bool>();
	/// <summary>Important note: must be called in the same thread that handles the hooks!</summary>
	public static void SetUpHooks() {
		Log("Setting up hooks/event-listeners");
		//HookManager.EnsureSubscribedToGlobalKeyboardEvents();
		//HookManager.EnsureSubscribedToGlobalMouseEvents();

		HookManager.KeyDown += (sender, e)=> {
			var key = keySimplifications.GetValueOrX(e.KeyData, e.KeyData);
			//Log("KeyDown) " + key + (key != e.KeyData ? "   [Raw: " + e.KeyData + "]" : ""));
			keyDowns[key] = true;

			foreach (var hotkey in globalHotkeys)
				// if we just pressed down a key in a hotkey, and that hotkey is now fulfilled, trigger its onDown callback
				if (hotkey.onDown != null && hotkey.keys.Any(a=>a == key) && hotkey.keys.All(a=>keyDowns.GetValueOrX(a))) {
					hotkey.onDown.Invoke(null);
					if (hotkey.capture)
						e.Handled = true;
				}
		};
		// make-so: these get called for ctrl+shift+esc hotkey keys
		HookManager.KeyUp += (sender, e)=> {
			var key = keySimplifications.GetValueOrX(e.KeyData, e.KeyData);
			//Log("KeyUp) " + key + (key != e.KeyData ? "   [Raw: " + e.KeyData + "]" : ""));
			keyDowns[key] = false;

			foreach (var hotkey in globalHotkeys)
				// if we just pressed up a key in a hotkey, and that hotkey was just fulfilled, trigger its onUp callback
				if (hotkey.onUp != null && hotkey.keys.Any(a=>a == key) && hotkey.keys.All(a=>keyDowns.GetValueOrX(a))) {
					hotkey.onUp.Invoke(null);
					if (hotkey.capture)
						e.Handled = true;
				}
		};
	}

	// general
	// ==========

	public static List<GlobalHotkey> globalHotkeys = new List<GlobalHotkey>();
	public class GlobalHotkey {
		public List<Keys> keys;
		public Func<object, Task<object>> onDown;
		public Func<object, Task<object>> onUp;
		public bool capture;
		public override string ToString() { return keys.Select(a=>a.ToString()).JoinUsing(" "); }
	}

	/*public class AddGlobalHotkey_Options {
		public bool capture;
		public Func<object, Task<object>> onDown;
		public Func<object, Task<object>> onUp;
	}*/
	public static void AddGlobalHotkey(string keysStr, dynamic options) {
		var capture = options.capture ?? true;
		var onDown = (Func<object, Task<object>>)options.onDown;
		var onUp = (Func<object, Task<object>>)options.onUp;

		var keys = keysStr.Split('+').Select(a=>keyNameOverrides.GetValueOrXNullable(a, null) ?? ParseEnum<Keys>(a)).ToList();
		foreach (var key in keys)
			if (!keyDowns.ContainsKey(key))
				keyDowns[key] = false;
		var hotkey = new GlobalHotkey {keys = keys, capture = capture, onDown = onDown, onUp = onUp};
		//Log("Adding hotkey. Keys: " + hotkey + " Callback: " + callback);
		globalHotkeys.Add(hotkey);
	}

	public static bool IsProcessOpen(string processName) { return Process.GetProcessesByName(processName).Any(); }

	public static void Run(string command, dynamic options = null) {
		options = options ?? new Map_Dynamic();
		var useCMD = options.useCMD ?? true;
		var cmdHidden = options.cmdHidden ?? true;

		if (useCMD) {
			var startInfo = new ProcessStartInfo();
			if (cmdHidden)
				startInfo.WindowStyle = ProcessWindowStyle.Hidden;
			startInfo.FileName = "cmd.exe";
			startInfo.Arguments = "/C " + command;
			var process = new Process();
			process.StartInfo = startInfo;
			process.Start();
		}
		else
			Process.Start(command);
	}
	
	public enum SearchType {
		EnumWindows,
		FindWindow,
		ProcessMainWindows
	}
	public static Window GetWindow(dynamic options) {
		var windows = (IEnumerable<Window>)GetWindows(options);
		return windows.FirstOrDefault();
	}
	public static List<Window> GetWindows(dynamic options) {
		var searchType = (SearchType?)options.searchType ?? SearchType.EnumWindows;
		var processPath = (string)options.processPath;
		var processName = (string)options.processName;
		var processID = (int?)options.processID ?? -1;
		var threadID = (int?)options.threadID ?? -1;
		var handle = (string)options.handle;
		//var className = (string)((Map)options).GetValueOrX("class");
		var className = (string)options.@class;
		var text = (string)options.text;
		var text_contains = (string)options.text_contains;
		//var hiddenWindows = (bool?)options.hiddenWindows ?? false;
		var skip = (int?)options.skip ?? 0;

		Func<Window, bool> windowFilter = a=>
			(processPath == null || a.GetProcessPath_() == processPath)
			&& (processName == null || a.GetProcessName_() == processName)
			&& (processID == -1 || a.GetProcessID_() == processID)
			&& (threadID == -1 || a.GetThreadID_() == threadID)
			&& (handle == null || a.handle.ToString() == handle)
			&& (className == null || a.GetClass_() == className)
			&& (text == null || a.GetText_() == text)
			&& (text_contains == null || a.GetText_().Contains(text_contains));

		IEnumerable<Window> result = new List<Window>();
		if (searchType == SearchType.EnumWindows)
			result = Windows.FindWindows();
		if (searchType == SearchType.FindWindow)
			result = new List<Window> {new Window(FindWindow(className, text))};
		if (searchType == SearchType.ProcessMainWindows)
			result = Process.GetProcesses().Select(a=>new Window(a.MainWindowHandle));
		result = result.Where(windowFilter).Skip(skip);
		return result.ToList();
	}

	[DllImport("user32.dll")] static extern IntPtr FindWindow(string className, string text);

	//static Window cmdWindow;
	public static void ShowCMDWindow() {
		var cmdProcess = Process.GetCurrentProcess().GetParentProcess();
		/*if (cmdWindow == null)
			cmdWindow = new Window(cmdProcess.MainWindowHandle);*/
		var cmdWindow = GetWindow(new Map_Dynamic(new Map {{"processID", cmdProcess.Id}}));
		cmdWindow.Show_();
	}
	public static void HideCMDWindow() {
		var cmdProcess = Process.GetCurrentProcess().GetParentProcess();
		/*if (cmdWindow == null)
			cmdWindow = new Window(cmdProcess.MainWindowHandle);*/
		var cmdWindow = GetWindow(new Map_Dynamic(new Map {{"processID", cmdProcess.Id}}));
		cmdWindow.Hide_();
	}

	public static NotifyIcon trayIcon;
	//public static void CreateTrayIcon(Func<object, Task<object>> reload, Func<object, Task<object>> exit) {
	public static void CreateTrayIcon(Func<object, Task<object>> exit) {
		var notifyThread = new Thread(()=>{
			var icon = new NotifyIcon();
			icon.Icon = Icon.ExtractAssociatedIcon(FileManager.csCore.GetFile("Icon_x16.ico").FullName);
			icon.Visible = true;
			icon.Text = "UserScript.js";
			icon.ContextMenu = new ContextMenu(new []{
				new MenuItem("Hide CMD Window", (sender, e)=> {
					if (icon.ContextMenu.MenuItems[0].Text == "Show CMD Window")
						ShowCMDWindow();
					else
						HideCMDWindow();
					UpdateTrayIconMenuItems();
				}),
				new MenuItem("-"), 
				new MenuItem("Reload script", (sender, e)=> {
					Process.Start(FileManager.GetFile("Start.lnk").FullName);

					Main.Shutdown(); // shutdown self

					var cmdProcess = Process.GetCurrentProcess().GetParentProcess();
					cmdProcess.Kill(); // kill cmd
					//reload(null);
					exit(null); // kill node
				}),
				new MenuItem("Edit script", (sender, e)=> {
					Process.Start(FileManager.GetFile("UserScript.js").FullName);
				}),
				new MenuItem("Open script folder", (sender, e)=> { Process.Start("explorer.exe", FileManager.root.FullName); }),
				new MenuItem("-"),
				new MenuItem("Exit", (sender, e)=> {
					Main.Shutdown(); // shutdown self

					var cmdProcess = Process.GetCurrentProcess().GetParentProcess();
					cmdProcess.Kill(); // kill cmd
					exit(null); // kill node

					//Application.Exit(); // (we're in node process, so no need to kill self)
				})
			});
			trayIcon = icon;
			UpdateTrayIconMenuItems();
			Application.Run();
		});
		notifyThread.Start();
	}
	static void UpdateTrayIconMenuItems() {
		var cmdProcess = Process.GetCurrentProcess().GetParentProcess();
		trayIcon.ContextMenu.MenuItems[0].Text = !new Window(cmdProcess.MainWindowHandle).IsVisible_() ? "Show CMD Window" : "Hide CMD Window";
	}

	/*public class OnEvent_Class {
		public Window window;
		public SystemEvents @event;
	}*/
	public static void AddSystemEventListener(string eventTypeName, Func<object, Task<object>> onEvent) {
		var eventType = ParseEnum<SystemEvents>(eventTypeName);
		var listenerThread = new Thread(()=> {
			var listener = new SystemListener(eventType);
			listener.SystemEvent += (sender, e)=> {
				var window = new Window(e.windowHandle);
				//if (@event == null || e.systemEvent == @event)
				onEvent(new {window = window, @event = e.systemEvent});
			};
			Application.Run();
		});
		listenerThread.Start();
	}

	public static void AddPowerModeChangeListener(Func<object, Task<object>> listener) {
		Microsoft.Win32.SystemEvents.PowerModeChanged += (sender, e) => {
			listener(e.Mode);
		};
	}
}