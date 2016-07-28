var g = global;
//var window = g;

//var robot = require("./build/Release/robotjs.node");
var edge = require("edge");

// JS packages
require("./Packages/General/ClassExtensions.js");
require("./Packages/General/Globals.js");
var V = require("./Packages/V/V.js").V;

// change how .cs files are loaded/"required" (to load as plain-text, rather than JS code)
/*var fs = require("fs");
require.extensions[".cs"] = function(module, filename) {
    module.exports.csStr = fs.readFileSync(filename, "utf8");
};

// load CS files, to be loaded in
var csStr = "";
//function IncludeCS(csFilePath) { csStr += require(csFilePath).csStr; }
function IncludeCS(csFilePath) { csStr += require(csFilePath).csStr; }
IncludeCS("./CS/Includes.cs");
IncludeCS("./CS/KeyboardInput.cs");
IncludeCS("./CS/V.cs");
IncludeCS("./CS/HookManager/HookManager.cs");
IncludeCS("./CS/HookManager/HookManager.Structures.cs");
IncludeCS("./CS/HookManager/HookManager.Windows.cs");
IncludeCS("./CS/HookManager/MouseEventExtArgs.cs");
IncludeCS("./CS/HookManager/GlobalEventProvider.cs");
IncludeCS("./CS/HookManager/HookManager.Callbacks.cs");

//var CallMethod_Internal = edge.func(require("path").join(__dirname, "KeyboardInput.cs"));
var CallMethod_Internal = edge.func(csStr);
function CallMethod(methodName, args___) { return CallMethod_Internal({methodName: methodName, args: V.Slice(arguments, 1)}, true); };*/

var CallMethod_Ext = edge.func({
    assemblyFile: "./CSCore/Main/bin/Debug/CSCore.dll",
    typeName: "Main",
    methodName: "CallMethod" // this must be Func<object,Task<object>>
});
g.CallMethod = function(methodName, args___) {
    var methodArgs = V.Slice(arguments, 1);
    return CallMethod_Ext({methodName: methodName, args: methodArgs}, true);
};

var CallMethodAsync_Ext = edge.func({
    assemblyFile: "./CSCore/Main/bin/Debug/CSCore.dll",
    typeName: "Main",
    methodName: "CallMethodAsync" // this must be Func<object,Task<object>>
});
g.CallMethodAsync = function(methodName, args___) {
    var methodArgs = V.Slice(arguments, 1);

    // if last argument is callback function, grab it, and remove it from argument list
    //var callback = methodArgs.slice(-1)[0] instanceof Function ? methodArgs.splice(-1)[0] : ()=>{};
    // always grab last arg as callback-function (note that all it does is tell you when the entry was added to the queue)
    /*var callback = methodArgs.splice(-1)[0] || ()=>{};
    return AddCallMethodEntry_Ext({methodName: methodName, args: methodArgs}, callback);*/

    return CallMethodAsync_Ext({methodName: methodName, args: methodArgs}, ()=>{});
}

require("./UserScript.js");
/*fs = require("fs");
eval(fs.readFileSync("./UserScript.js", "utf8"));*/

//var exit = false;
function WaitMoreIfNotReady() {
    //if (!exit)
    setTimeout(WaitMoreIfNotReady, 1000);
}
WaitMoreIfNotReady();