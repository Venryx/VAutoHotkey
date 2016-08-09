HideCMDWindow();
CreateTrayIcon();

AddGlobalHotkey('Control+Shift+N', {capture: true, onDown: function(data) {
	// if explorer isn't running yet, start it
	if (!IsProcessOpen('explorer'))
        Run('C:/Windows/explorer.exe');
	// else, find its first window, then show and activate it
    else {
        var window = GetWindow({processName: 'explorer.exe', class: 'CabinetWClass'});
        if (window) {
            window.Show();
            window.Activate();
        }
    }
}});