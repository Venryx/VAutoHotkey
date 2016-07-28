using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gma.UserActivityMonitor;
using static V;

public static class Methods {
	// set-up
	// ==========

	// HookManager events:
	// MouseMove, MouseMoveExt, MouseClick, MouseClickExt, MouseDown, MouseUp, MouseWheel, MouseDoubleClick
	// KeyPress, KeyUp, KeyDown

	static Dictionary<string, Keys> keyNameOverrides = new Dictionary<string, Keys> {
		{"Control", Keys.ControlKey}, {"Shift", Keys.ShiftKey}, {"Alt", Keys.Menu}
	};
	static Dictionary<Keys, Keys> keySimplifications = new Dictionary<Keys, Keys> {
		{Keys.Control, Keys.ControlKey}, {Keys.LControlKey, Keys.ControlKey}, {Keys.RControlKey, Keys.ControlKey},
		{Keys.Shift, Keys.ShiftKey}, {Keys.LShiftKey, Keys.ShiftKey}, {Keys.RShiftKey, Keys.ShiftKey},
		{Keys.Alt, Keys.Menu}, {Keys.LMenu, Keys.Menu}, {Keys.RMenu, Keys.Menu}
	};

	static Dictionary<Keys, bool> keyDowns = new Dictionary<Keys, bool>();
	public static void Init() {}
	static Methods() {
		Console.WriteLine("Setting up hooks/event-listeners");
		HookManager.KeyDown += (sender, e)=> {
			var key = keySimplifications.GetValueOrX(e.KeyData, e.KeyData);
			//Console.WriteLine("KeyDown) " + key + (key != e.KeyData ? "   [Raw: " + e.KeyData + "]" : ""));
			keyDowns[key] = true;

			foreach (var hotkey in globalHotkeys)
				// if we just pressed down a key in a hotkey, and that hotkey is now fulfilled, trigger its callback
				if (hotkey.keys.Any(a=>a == key) && hotkey.keys.All(a=>keyDowns.GetValueOrX(a))) {
					hotkey.callback.Invoke(null);
					if (hotkey.capture)
						e.Handled = true;
				}
		};
		HookManager.KeyUp += (sender, e)=> {
			var key = keySimplifications.GetValueOrX(e.KeyData, e.KeyData);
			//Console.WriteLine("KeyUp) " + key + (key != e.KeyData ? "   [Raw: " + e.KeyData + "]" : ""));
			keyDowns[key] = false;
		};
	}

	// general
	// ==========

	public static List<GlobalHotkey> globalHotkeys = new List<GlobalHotkey>();
	public class GlobalHotkey {
		public List<Keys> keys;
		public Func<object, Task<object>> callback;
		public bool capture;
		public override string ToString() { return keys.Select(a=>a.ToString()).JoinUsing(" "); }
	}

	public static void AddGlobalHotkey(string keysStr, dynamic options, Func<object, Task<object>> callback) {
		var keys = keysStr.Split('+').Select(a=>keyNameOverrides.GetValueOrX(a, ParseEnum<Keys>(a))).ToList();
		foreach (var key in keys)
			if (!keyDowns.ContainsKey(key))
				keyDowns[key] = false;
		var hotkey = new GlobalHotkey {keys = keys, callback = callback, capture = options.capture};
		//Log("Adding hotkey. Keys: " + hotkey + " Callback: " + callback);
		globalHotkeys.Add(hotkey);
	}

	public static bool IsProcessOpen(string processName) { return Process.GetProcessesByName(processName).Any(); }
}