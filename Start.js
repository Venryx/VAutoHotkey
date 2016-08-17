var g = global;
//var window = g;

var process = require("process");

//var robot = require("./build/Release/robotjs.node");
var edge = require("edge");

// JS packages
require("./Packages/General/ClassExtensions.js");
require("./Packages/General/Globals.js");
var V = require("./Packages/V/V.js").V;

function InMethodArgs_ProcessObject(obj) {
    var processSub = sub=>{
        if (typeof sub == "object")
            return InMethodArgs_ProcessObject(sub);
        if (sub instanceof Function) {
            var func = sub;
            return function() {
                var argsForFunc = V.AsArray(arguments);
                //Log("args count:" + args.length + ";" + ToJSON(args) + ";" + (typeof args[0]));
                for (var i in argsForFunc)
                    argsForFunc[i] = InResult_ProcessObject(argsForFunc[i]);

                try { func.apply(null, argsForFunc); }
                catch(ex) { Log(ex.stack); }
            }
        }
        return sub;
    };
    if (obj instanceof Array)
        return obj.map(processSub);
    if (typeof obj == "object")
        return obj.Select(processSub);
    return obj;
}

function InResult_ProcessObject(obj) {
    if (obj == null || obj == "@@@NULL@@@")
        return null;
    if (typeof obj == "object")
        return obj.Select(sub=>{
            if (typeof sub == "object")
                return InResult_ProcessObject(sub);
            if (typeof sub == "function" || sub instanceof Function) {
                var func = sub;
                return function() {
                    var argsForFunc = V.AsArray(arguments);
                    argsForFunc = InMethodArgs_ProcessObject(argsForFunc);
                    //Log("Calling_Early_2) " + methodName + " Args: " + methodArgs.Select(a=>a.constructor.name).JoinUsing(","));
                    //var result = innerFunc({methodName: methodName, args: methodArgs}, true);
                    var result = func(argsForFunc, true);
                    result = InResult_ProcessObject(result);
                    //Log("Result4)" + result);
                    return result;
                }
            }
            return sub;
        });
    return obj;
}

var CallMethod_Ext = edge.func({
    assemblyFile: "./CSCore/Main/bin/Debug/CSCore.dll",
    typeName: "Main_NodeJSEntryPoint",
    methodName: "CallMethod" // this must be Func<object,Task<object>>
});
g.CallMethod = function(methodName, args___) {
    var methodArgs = V.Slice(arguments, 1);
    methodArgs = InMethodArgs_ProcessObject(methodArgs);
    //Log("[JS]Calling) " + methodName + " Args: " + methodArgs.Select(a=>a.constructor.name).JoinUsing(","));
    var result = CallMethod_Ext({methodName: methodName, args: methodArgs}, true);
    result = InResult_ProcessObject(result);
    //Log("[JS]Returning result) " + result);
    return result;
};

var CallMethodAsync_Ext = edge.func({
    assemblyFile: "./CSCore/Main/bin/Debug/CSCore.dll",
    typeName: "Main_NodeJSEntryPoint",
    methodName: "CallMethodAsync" // this must be Func<object,Task<object>>
});
g.CallMethodAsync = function(methodName, args___) {
    return new Promise((resolve, reject)=> {
        var methodArgs = V.Slice(arguments, 1);
        methodArgs = InMethodArgs_ProcessObject(methodArgs);

        /*var callback = methodArgs.splice(-1)[0] || ()=>{};
        var callbackWrapper = function(result) {
            InResult_ProcessObject(result);
            //Log("[JS]Returning async result) " + result);
            callback(result);
        };
        //Log("[JS]Calling method async) " + methodName + " Args: " + methodArgs.Select(a=>a.constructor.name).JoinUsing(","));
        CallMethodAsync_Ext({methodName: methodName, args: methodArgs, callback: callbackWrapper}, ()=>{});*/

        var callback = function(result) {
            result = InResult_ProcessObject(result);
            //Log("[JS]Returning async result) " + result);
            //callback(result);
            resolve(result);
        };
        //Log("[JS]Calling method async) " + methodName + " Args: " + methodArgs.Select(a=>a.constructor.name).JoinUsing(","));
        CallMethodAsync_Ext({methodName: methodName, args: methodArgs, callback: callback}, (a,b)=>{
            //Log("Successfully queued async method-call. (error:" + a + " result:" + b + ")");
        });
    });
};

g.API = require("./API.js");
for (var methodName of g.API.methodNames) (function(methodName){
    g[methodName] = function(args___) { return g.CallMethod.apply(null, [methodName].concat(V.AsArray(arguments))); }
    g[methodName + "Async"] = function(args___) { return g.CallMethodAsync.apply(null, [methodName].concat(V.AsArray(arguments))); }
})(methodName);

// special ones
g.launchingAtStartup = process.argv.some(a=>a == "-atStartup");
g.CreateStandardTrayIcon = function() {
    /*var reloadFunc = function() {
        process.exit(); // just exit; C# side will have already launched a new instance of the Start.bat batch file
    };*/
    var exitFunc = function() { process.exit(); };
    return g.CallMethod.apply(null, ["CreateStandardTrayIcon", exitFunc].concat(V.AsArray(arguments)));
};

fs = require("fs");
// if UserScript does not exist, create it from default-file
try { fs.statSync("./UserScript.js", "utf8").isFile(); }
catch(ex) { // if file doesn't exist
    fs.createReadStream("./ScriptExamples/Default.js").pipe(fs.createWriteStream("UserScript.js"));
}

g.async = require('asyncawait/async');
g.await = require('asyncawait/await');

// user script
// ==========
/*var g = global;
for (var propName of g.API.methodNames) {
    eval("var " + propName + " = g." + propName + ";");
    eval("var " + propName + "Async = g." + propName + "Async;");
}*/
require("./UserScript.js");
// ==========

//var exit = false;
function WaitMoreIfNotReady() {
    //if (!exit)
    setTimeout(WaitMoreIfNotReady, 1000);
}
WaitMoreIfNotReady();