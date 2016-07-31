var g = global;
//var window = g;

//var robot = require("./build/Release/robotjs.node");
var edge = require("edge");

// JS packages
require("./Packages/General/ClassExtensions.js");
require("./Packages/General/Globals.js");
var V = require("./Packages/V/V.js").V;

function ProcessMethodArgs(args) {
    for (var prop in args) (function(prop) {
        if (typeof args[prop] == "object")
            ProcessMethodArgs(args[prop]);
        else if (args[prop] instanceof Function) {
            var innerHandler = args[prop];
            args[prop] = function() {
                try {
                    innerHandler.apply(null, arguments);
                } catch(ex) {
                    Log(ex.stack);
                }
            }
        }
    })(prop);
}

function ProcessResult(obj) {
    for (var prop in obj) (function(prop) {
        /*if (typeof obj[prop] == "object")
            ProcessResult(obj[prop]);
        else*/ if (obj[prop] instanceof Function) {
            var innerFunc = obj[prop];
            obj[prop] = function() {
                var methodArgs = V.Slice(arguments, 1);
                ProcessMethodArgs(methodArgs);
                Log("Calling_Early_2) " + methodName + " Args: " + methodArgs.Select(a=>a.constructor.name).JoinUsing(","));
                var result = innerFunc({methodName: methodName, args: methodArgs}, true);
                if (result == "@@@NULL@@@")
                    result = null;
                ProcessResult(result);
                Log("Result4)" + result);
                return result;
            }
        }
    })(prop);
}

var CallMethod_Ext = edge.func({
    assemblyFile: "./CSCore/Main/bin/Debug/CSCore.dll",
    typeName: "Main_NodeJSEntryPoint",
    methodName: "CallMethod" // this must be Func<object,Task<object>>
});
g.CallMethod = function(methodName, args___) {
    var methodArgs = V.Slice(arguments, 1);
    ProcessMethodArgs(methodArgs);
    Log("Calling_Early) " + methodName + " Args: " + methodArgs.Select(a=>a.constructor.name).JoinUsing(","));
    var result = CallMethod_Ext({methodName: methodName, args: methodArgs}, true);
    if (result == "@@@NULL@@@")
        result = null;
    ProcessResult(result);
    Log("Result3)" + result);
    return result;
};

var CallMethodAsync_Ext = edge.func({
    assemblyFile: "./CSCore/Main/bin/Debug/CSCore.dll",
    typeName: "Main_NodeJSEntryPoint",
    methodName: "CallMethodAsync" // this must be Func<object,Task<object>>
});
g.CallMethodAsync = function(methodName, args___) {
    var methodArgs = V.Slice(arguments, 1);
    ProcessMethodArgs(methodArgs);

    // if last argument is callback function, grab it, and remove it from argument list
    //var callback = methodArgs.slice(-1)[0] instanceof Function ? methodArgs.splice(-1)[0] : ()=>{};

    // always grab last arg as callback-function (note that all it does is tell you when the entry was added to the queue)
    /*var callback = methodArgs.splice(-1)[0] || ()=>{};
    return CallMethodAsync_Ext({methodName: methodName, args: methodArgs}, callback);*/

    //return CallMethodAsync_Ext({methodName: methodName, args: methodArgs}, ()=>{});

    var callback = methodArgs.splice(-1)[0] || ()=>{};

    Log("Callback)" + callback);

    Log("Calling_Early(Async)) " + methodName + " Args: " + methodArgs.Select(a=>a.constructor.name).JoinUsing(","));

    var callbackWrapper = function(result) {
        ProcessResult(result);
        callback(result);
    };

    return CallMethodAsync_Ext({methodName: methodName, args: methodArgs, callback: callbackWrapper}, ()=>{});
};

g.API = require("./API.js");
for (var methodName of g.API.methodNames) (function(methodName){
    g[methodName] = function(args___) { return g.CallMethod.apply(null, [methodName].concat(V.AsArray(arguments))); }
    g[methodName + "Async"] = function(args___) { return g.CallMethodAsync.apply(null, [methodName].concat(V.AsArray(arguments))); }
})(methodName);

fs = require("fs");
// if UserScript does not exist, create it from default-file
try { fs.statSync("./UserScript.js", "utf8").isFile(); }
catch(ex) { // if file doesn't exist
    fs.createReadStream("./Others/UserScript_Default.js").pipe(fs.createWriteStream("UserScript.js"));
}

require("./UserScript.js");

//var exit = false;
function WaitMoreIfNotReady() {
    //if (!exit)
    setTimeout(WaitMoreIfNotReady, 1000);
}
WaitMoreIfNotReady();