# VAutoHotkey

Automation program similar to AutoHotkey, except with a C# core, and a JavaScript (NodeJS) scripting engine.

Example script:
```
AddGlobalHotkey("Control+Shift+Escape", {capture: true}, function(error, data) {
	if (IsProcessOpen("ProcessHacker"))
        Log("ProcessHacker.exe is running...");
});
```