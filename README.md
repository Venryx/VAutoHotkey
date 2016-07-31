# VAutoHotkey

Automation program similar to AutoHotkey, except with a C# core, and a JavaScript (NodeJS) scripting engine.

Example script:
```
AddGlobalHotkey("Control+Shift+Escape", {capture: true, onDown: function(error, data) {
    if (error)
		return Log("Error: " + error);

	// if explorer isn't running yet, start it
	if (!IsProcessOpen("explorer"))
        Run("C:\\Windows\\explorer.exe");
	// else, find its first window, then show and activate it
    else {
        var window = GetWindow({process: "explorer.exe", class: "CabinetWClass"});
        if (window) {
            window.Show();
            window.Activate();
        }
    }
}});
```

# Installation

1) Download and install NodeJS  
2) Download and install NPM  
3) Open the "VAutoHotkey" root folder, and double-click the "Install.bat" batch file.  

# Usage

1) Open the UserScript.js file, and add your own hotkeys/commands. (see below for options)  
2) Open the "VAutoHotkey" root folder, and double-click the "Start" shortcut. (it's set to launch as admin, which is needed for some functions)  

# Scripting

TODO