var g = global, CallMethod = g.CallMethod, CallMethodAsync = g.CallMethodAsync;

//V.Sleep(3000); // uncomment to sleep, so you have time to attach debugger

CallMethodAsync("AddGlobalHotkey", "Control+Shift+Escape", {capture: true}, function(error, data) {
    if (error) return Log("Error) " + error);
	if (CallMethod("IsProcessOpen", "ProcessHacker"))
        Log("Yay!");
});