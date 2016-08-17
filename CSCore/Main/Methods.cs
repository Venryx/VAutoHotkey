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

		HookManager.KeyDownOrHeld += (sender, e)=> {
			var key = keySimplifications.GetValueOrX(e.KeyData, e.KeyData);
			// if key is already known as down, ignore (we only care about when it's first pressed down)
			if (keyDowns.GetValueOrX(key)) return;
			//Log("KeyDown) " + key + (key != e.KeyData ? "   [Raw: " + e.KeyData + "]" : ""));
			keyDowns[key] = true;

			foreach (var hotkey in globalHotkeys)
				// if we just downed a key in a hotkey, and that hotkey is now fulfilled, trigger its onDown callback
				if (hotkey.onDown != null && hotkey.keys.Any(a=>a == key) && hotkey.keys.All(a=>keyDowns.GetValueOrX(a))) {
					// if supposed to be exact modifier match, but it isn't, then don't trigger hotkey's event
					if (hotkey.exactModifiersMatch && !(
						keyDowns.GetValueOrX(Keys.ControlKey) == hotkey.keys.Contains(Keys.ControlKey)
						&& keyDowns.GetValueOrX(Keys.ShiftKey) == hotkey.keys.Contains(Keys.ShiftKey)
						&& keyDowns.GetValueOrX(Keys.Menu) == hotkey.keys.Contains(Keys.Menu)
					)) continue;

					hotkey.onDown.Invoke(null);
					if (hotkey.capture)
						e.Handled = true;
				}
		};
		HookManager.KeyUp += (sender, e)=> {
			var key = keySimplifications.GetValueOrX(e.KeyData, e.KeyData);
			//Log("KeyUp) " + key + (key != e.KeyData ? "   [Raw: " + e.KeyData + "]" : ""));
			keyDowns[key] = false;

			Func<Keys, bool> keyDownOrJustUpped = key2=>keyDowns.GetValueOrX(key) || key2 == key;
			foreach (var hotkey in globalHotkeys)
				// if we just upped a key in a hotkey, and all that other hotkey's keys are still down, trigger its onUp callback
				if (hotkey.onUp != null && hotkey.keys.Any(a=>a == key) && hotkey.keys.All(a=>keyDownOrJustUpped(a))) {
					// if supposed to be exact modifier match, but it isn't, then don't trigger hotkey's event
					if (hotkey.exactModifiersMatch && !(
						keyDownOrJustUpped(Keys.ControlKey) == hotkey.keys.Contains(Keys.ControlKey)
						&& keyDownOrJustUpped(Keys.ShiftKey) == hotkey.keys.Contains(Keys.ShiftKey)
						&& keyDownOrJustUpped(Keys.Menu) == hotkey.keys.Contains(Keys.Menu)
					)) continue;

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
		public bool capture;
		public bool exactModifiersMatch;
		public Func<object, Task<object>> onDown;
		public Func<object, Task<object>> onUp;
		public override string ToString() { return keys.Select(a=>a.ToString()).JoinUsing(" "); }
	}

	/*public class AddGlobalHotkey_Options {
		public bool capture;
		public Func<object, Task<object>> onDown;
		public Func<object, Task<object>> onUp;
	}*/
	public static void AddGlobalHotkey(string keysStr, dynamic options) {
		var capture = options.capture ?? true;
		var exactModifiersMatch = options.exactModifierMatch ?? true;
		var onDown = (Func<object, Task<object>>)options.onDown;
		var onUp = (Func<object, Task<object>>)options.onUp;

		var keys = keysStr.Split('+').Select(a=>keyNameOverrides.GetValueOrXNullable(a, null) ?? ParseEnum<Keys>(a)).ToList();
		foreach (var key in keys)
			if (!keyDowns.ContainsKey(key))
				keyDowns[key] = false;
		var hotkey = new GlobalHotkey {keys = keys, capture = capture, exactModifiersMatch = exactModifiersMatch, onDown = onDown, onUp = onUp};
		//Log("Adding hotkey. Keys: " + hotkey + " Callback: " + callback);
		globalHotkeys.Add(hotkey);
	}

	//public static bool IsProcessOpen(string processName) { return Process.GetProcessesByName(processName).Any(); }
	public static VProcess GetProcess(dynamic options) {
		if (options is string)
			options = new Map_Dynamic(new Map {{"name", options}});

		var processes = (IEnumerable<VProcess>)GetProcesses(options);
		return processes.FirstOrDefault();
	}
	public static List<VProcess> GetProcesses(dynamic options) {
		//var searchType = V.TryParseEnum<SearchType>(options.searchType) ?? SearchType.EnumWindows;
		var skip = (int?)options.skip ?? 0;

		IEnumerable<VProcess> result = Process.GetProcesses().Select(a=>new VProcess(a));
		//result = result.Where(a=>DoesProcessMatch(a, options)).Skip(skip);
		result = result.Where(a => DoesProcessMatch(a, options)).Skip(skip);
		return result.ToList();
	}
	static bool DoesProcessMatch(VProcess process, dynamic options) {
		var path = (string)options.path;
		var name = (string)options.name;
		var id = (int?)options.id ?? -1;
		var threadIDs_contains = (int?)options.threadIDs_contains ?? -1;
		//var handle = (string)options.handle;
		var argsStr = (string)options.argsStr;
		var argsStr_contains = (string)options.argsStr_contains;

		return (path == null || process.GetPath_() == path)
			&& (name == null || process.GetName_() == name)
			&& (id == -1 || process.id == id)
			&& (threadIDs_contains == -1 || process.GetThreadIDs_().Any(b=>b == threadIDs_contains))
			//&& (handle == null || process.GetHandle_().ToString() == handle)
			&& (argsStr == null || process.GetArgsStr_() == argsStr)
			&& (argsStr_contains == null || process.GetArgsStr_().Contains(argsStr_contains));
	}

	public static VProcess Run(string command, dynamic options = null) {
		options = options ?? new Map_Dynamic();
		var useCMD = options.useCMD ?? false;
		var newCMD = options.newCMD ?? false;
		//var showType = V.TryParseEnum<ProcessWindowStyle>(options.showType) ?? ProcessWindowStyle.Normal; // (resolved later)
		var startFolder = (string)options.startFolder ?? "@file-folder";
		var autoEscapeFilename = options.autoEscapeFilename ?? true;
		var args_fromOptions = (string)options.arguments;

		// resolve file-str and args-str
		string filename;
		string args_fromCommand;
		// if file-str is unambiguously provided using quotes
		if (command.StartsWith("\"")) {
			filename = command.SubstringSE(1, command.IndexOf_X(1, "\""));
			args_fromCommand = command.Substring(command.IndexOf_X(1, "\"") + 2);
		}
		else {
			var filenameStopPos = autoEscapeFilename && command.Contains(".") && command.Contains(" ")
				? command.Substring(command.IndexOf(".")).Contains(" ") ? command.IndexOf(" ", command.IndexOf(".")) : command.Length
				: command.Contains(" ") ? command.IndexOf(" ") : command.Length;
			filename = command.Substring(0, filenameStopPos);
			args_fromCommand = filenameStopPos < command.Length ? command.Substring(filenameStopPos + 1) : "";
		}
		filename = filename.Replace("/", "\\");
		var args = (args_fromCommand.Length > 0 ? args_fromCommand + " " : "") + args_fromOptions;
		var file = new FileInfo(filename);

		var showType = V.TryParseEnum<ProcessWindowStyle>(options.showType)
			?? (file.Extension == ".bat" || file.Extension == ".cmd" ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal);

		var startInfo = new ProcessStartInfo();
		startInfo.WindowStyle = showType;
		startInfo.CreateNoWindow = !newCMD;
		if (startFolder != null)
			startInfo.WorkingDirectory = startFolder == "@file-folder" ? file.DirectoryName : startFolder;
		if (useCMD) {
			startInfo.FileName = "cmd.exe";
			startInfo.Arguments = "/c " + filename + (args.Length > 0 ? " " + args : "");
		}
		else {
			startInfo.FileName = filename;
			startInfo.Arguments = args;
		}

		var process = new Process();
		process.StartInfo = startInfo;
		process.Start();
		return new VProcess(process);
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
		var searchType = V.TryParseEnum<SearchType>(options.searchType) ?? SearchType.EnumWindows;
		var skip = (int?)options.skip ?? 0;

		// window match options (early)
		var className = (string)options.@class;
		var text = (string)options.text;

		IEnumerable<Window> result = new List<Window>();
		if (searchType == SearchType.EnumWindows)
			result = Windows.FindWindows();
		if (searchType == SearchType.FindWindow)
			result = new List<Window> {new Window(FindWindow(className, text))};
		if (searchType == SearchType.ProcessMainWindows)
			result = Process.GetProcesses().Select(a=>new Window(a.MainWindowHandle));
		result = result.Where(a=>DoesWindowMatch(a, options)).Skip(skip);
		return result.ToList();
	}
	static bool DoesWindowMatch(Window window, dynamic options) {
		/*var processPath = (string)options.processPath;
		var processName = (string)options.processName;
		var processID = (int?)options.processID ?? -1;*/
		var processMatchOptions = options.process;
		if (processMatchOptions is string)
			processMatchOptions = new Map_Dynamic(new Map {{"name", processMatchOptions}});
		var threadID = (int?)options.threadID ?? -1;
		var handle = (string)options.handle;
		//var className = (string)((Map)options).GetValueOrX("class");
		var className = (string)options.@class;
		var text = (string)options.text;
		var text_contains = (string)options.text_contains;
		//var hiddenWindows = (bool?)options.hiddenWindows ?? false;

		return /*(processPath == null || process.GetPath_() == processPath)
			&& (processName == null || process.GetName_() == processName)
			&& (processID == -1 || process.GetID_() == processID)*/
			(processMatchOptions == null || DoesProcessMatch(window.GetProcess_(), processMatchOptions))
			&& (threadID == -1 || window.GetThreadID_() == threadID)
			&& (handle == null || window.handle.ToString() == handle)
			&& (className == null || window.GetClass_() == className)
			&& (text == null || window.GetText_() == text)
			&& (text_contains == null || window.GetText_().Contains(text_contains));
	}

	[DllImport("user32.dll")] static extern IntPtr FindWindow(string className, string text);

	//static Window cmdWindow;
	public static void ShowCMDWindow() {
		var cmdProcess = Process.GetCurrentProcess().GetParentProcess();
		/*dynamic options = new Map_Dynamic();
		options.process = new Map_Dynamic(new Map {{"id", cmdProcess.Id}});
		var cmdWindow = GetWindow(options);*/
		var cmdWindow = GetWindow(new Map_Dynamic(new Map {{"process", new Map_Dynamic(new Map {{"id", cmdProcess.Id}})}}));
		cmdWindow.Show_();
	}
	public static void HideCMDWindow() {
		var cmdProcess = Process.GetCurrentProcess().GetParentProcess();
		var cmdWindow = GetWindow(new Map_Dynamic(new Map {{"process", new Map_Dynamic(new Map {{"id", cmdProcess.Id}})}}));
		cmdWindow.Hide_();
	}

	public static List<NotifyIcon> trayIcons = new List<NotifyIcon>();

	public static NotifyIcon standardTrayIcon;
	//public static void CreateStandardTrayIcon(Func<object, Task<object>> reload, Func<object, Task<object>> exit) {
	public static void CreateStandardTrayIcon(Func<object, Task<object>> exit) {
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
					Process.Start(FileManager.GetFile("Start.bat").FullName);

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
			standardTrayIcon = icon;
			trayIcons.Add(icon);
			UpdateTrayIconMenuItems();
			Application.Run();
		});
		notifyThread.Start();
	}
	static void UpdateTrayIconMenuItems() {
		var cmdProcess = Process.GetCurrentProcess().GetParentProcess();
		standardTrayIcon.ContextMenu.MenuItems[0].Text = !new Window(cmdProcess.MainWindowHandle).IsVisible_() ? "Show CMD Window" : "Hide CMD Window";
	}

	//public static async void CreateTrayIcon(dynamic options) {
	public static void CreateTrayIcon(dynamic options) {
		var title = (string)options.title ?? "User actions";
		var menuItems = (dynamic[])options.menuItems;

		//var finalMenuItems = await Task.WhenAll(menuItems.Select(async a=> {
		var finalMenuItems = menuItems.Select(a=> {
			var text = a.text as string;
			var textFunc = a.text as Func<object, Task<object>>;
			var textFunc_async = a.text_async as Func<object, Task<object>>;
			//var action = (string)a.action;
			var onClick = (Func<object, Task<object>>)a.onClick;

			/*if (textFunc != null)
				// (we have to await even for the sync textFunc here, since we're in the init method, for which the js is locked waiting)
				text = (string)await textFunc(null);
			if (textFunc_async != null)
				text = (string)await textFunc_async(null);*/

			/*if (action == "close tray icon")
				return new MenuItem(a.text, (EventHandler)((sender, e) => {
					onClick(null);
				}));*/
			var result = new MenuItem(text, ((sender, e)=> {
				onClick(null);
			}));

			if (textFunc != null)
				result.SetMeta("textFunc", textFunc);
			if (textFunc_async != null)
				result.SetMeta("textFunc_async", textFunc_async);

			return result;
		//}).ToArray());
		}).ToArray();
		
		var notifyThread = new Thread(()=>{
			NotifyIcon icon = null;
			Action updateTrayIconText = () => {
				foreach (var item in icon.ContextMenu.MenuItems.OfType<MenuItem>()) {
					var textFunc = item.GetMeta<Func<object, Task<object>>>("textFunc");
					var textFunc_async = item.GetMeta<Func<object, Task<object>>>("textFunc_async");
					if (textFunc != null)
						item.Text = (string)textFunc(null).Result;
					else if (textFunc_async != null) {
						item.Text += " [...]";
						Task.Run(async () => item.Text = (string)await textFunc_async(null));
					}
				}
			};

			icon = new NotifyIcon();
			icon.Icon = Icon.ExtractAssociatedIcon(FileManager.csCore.GetFile("UserIcon_x16.ico").FullName);
			icon.Visible = true;
			icon.Text = title;
			icon.ContextMenu = new ContextMenu(finalMenuItems);
			icon.ContextMenu.Popup += (sender, args)=>updateTrayIconText();
			trayIcons.Add(icon);

			Task.Run(updateTrayIconText);

			//UpdateTrayIconMenuItems();
			Application.Run();
		});
		notifyThread.Start();
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

	public static void ShowMessageBox(dynamic options) {
		var title = (string)options.title ?? "";
		var message = (string)options.message;
		MessageBox.Show(null, message, title);
	}
}