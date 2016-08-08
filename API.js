exports.methodNames = [
    "ShowCMDWindow",
    "HideCMDWindow",
    "AddGlobalHotkey",
    "IsProcessOpen",
    "Run",
    "GetWindow",
    "GetWindows",
    "AddSystemEventListener",
    "AddPowerModeChangeListener",
	// node-js only (i.e. not using cs-core)
	"launchingAtStartup", // technically a variable rather than a method, but works fine anyway
	"CreateTrayIcon"
];