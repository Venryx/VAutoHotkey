var g = global;
for (var propName of g.API.methodNames) {
    eval("var " + propName + " = g." + propName + ";");
    eval("var " + propName + "Async = g." + propName + "Async;");
}

HideCMDWindow();
CreateTrayIcon();

AddGlobalHotkey("Control+Shift+Escape", {capture: true, onDown: function(data) {
	// if explorer isn't running yet, start it
	if (!IsProcessOpen("explorer"))
        Run("C:\\Windows\\explorer.exe");
	// else, find its first window, then show and activate it
    else {
        var window = GetWindow({processName: "explorer.exe", class: "CabinetWClass"});
        if (window) {
            window.Show();
            window.Activate();
        }
    }
}});