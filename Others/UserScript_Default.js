AddGlobalHotkey("Control+Shift+Escape", {capture: true, onDown: function(error, data) {
    if (error)
		return Log("Error: " + error);
	if (IsProcessOpen("explorer"))
        Log("Windows Explorer is open.");
}});