AddGlobalHotkey("Control+Shift+Escape", {capture: true, onDown: function(error, data) {
    if (error)
		return Log("Error: " + error);

	// if explorer isn't running yet, start it
	if (!IsProcessOpen("explorer"))
        Run('"C:\\Windows\\explorer.exe"');
	// else, find its first window, then show and activate it
    else {
        var window = GetWindow({process: "explorer.exe", class: "CabinetWClass"});
        if (window) {
            window.Show();
            window.Activate();
        }
    }
}});