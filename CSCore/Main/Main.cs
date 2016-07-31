using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gma.UserActivityMonitor;
using static V;

//using Map = System.Collections.Generic.Dictionary<string, object>;

public class Main_NodeJSEntryPoint {
	// this is the only entry point for calls from NodeJS; so add to queue, and have the processing occur on background thread
	public async Task<object> CallMethod(dynamic invokeArgs) {
		var result = Main.CallMethod(invokeArgs);
		return result;
	}
	public async Task<object> CallMethodAsync(dynamic invokeArgs) {
		Main.CallMethodAsync(invokeArgs);
		return true;
	}
}

public static class Main {
	static Dictionary<string, MethodInfo> methods;
	static Main() {
		methods = typeof(Methods).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).ToDictionary(a=>a.Name, a=>a);
		//Methods.Init();
	}

	static bool backgroundThreadStarted;
	public static void StartBackgroundThreadIfNotStarted() {
		if (backgroundThreadStarted)
			return;
		backgroundThreadStarted = true;
		Log("Starting background thread");

		// set up pre-quit handler, so we can unregister hooks before quit
		preQuitHandler = eventType=> {
			if (eventType == 2)
				Shutdown();
			return false;
		};
		SetConsoleCtrlHandler(preQuitHandler, true);

		// run the below loop in a separate thread
		// (this thread is in charge of checking for input events, as well as processing queued CallMethod calls)
		Task.Run((Action)BackgroundThreadMethod);
	}
	static ConsoleEventDelegate preQuitHandler;
	public static bool quit;

	class CallMethodEntry {
		public string methodName;
		public object[] args;
		public Func<object, Task<object>> callback;
	}
	static Queue<CallMethodEntry> callMethodQueue = new Queue<CallMethodEntry>();
	static void BackgroundThreadMethod() {
		Methods.SetUpHooks();

		// just keep looping here, processing input events and CallMethod calls, till NodeJS tells us to stop
		while (!quit) {
			Application.DoEvents(); // process input-events
			//while (callMethodQueue.Count > 0) {
			if (callMethodQueue.Count > 0) {
				var entry = callMethodQueue.Dequeue();
				//Log("Calling method async) " + entry.methodName + " Args: " + entry.args.Select(a=>a.GetType().Name).JoinUsing(","));
				var result = CallMethod_Internal(entry.methodName, entry.args);
				//Log("Returning async result) " + result + " @methodname:" + entry.methodName);
				entry.callback(result);
			}
			Thread.Sleep(4); // sleeping too long, e.g. 10ms will result in touch events arriving several seconds delayed due to event overload
		}
	}

	public static object[] ProcessArgs(object args) { return ProcessArgs(args as object[]); }
	public static object[] ProcessArgs(object[] args) {
		var newArgs = new List<object>();
		foreach (var arg in args)
			newArgs.Add(ProcessArg(arg));
		return newArgs.ToArray();
	}
	public static object ProcessArg(object arg) {
		if (arg is ExpandoObject)
			//return (IDictionary<string, object>)arg;
			return new Map_Dynamic((ExpandoObject)arg);
		return arg;
	}
	static object CallMethod_Internal(string methodName, object[] args) {
		Log("Calling method) " + methodName + " Args: " + args.Select(a=>a.GetType().Name).JoinUsing(","));
		try {
			args = ProcessArgs(args);

			/*if (args.Length > methods[methodName].GetParameters().Length)
				args = args.Take(methods[methodName].GetParameters().Length).ToArray();*/
			var additionalArgsNeeded = methods[methodName].GetParameters().Length - args.Length;
			if (additionalArgsNeeded > 0)
				args = args.Concat(Enumerable.Range(0, additionalArgsNeeded).Select(a=>Type.Missing)).ToArray();

			var result = methods[methodName].Invoke(null, args);
			if (result == null)
				result = "@@@NULL@@@";
			Log("Returning result) " + result);
			return result;
		}
		catch (TargetInvocationException ex) {
			LogInnerExceptionOf(ex);
			return null;
			/*RethrowInnerExceptionOf(ex);
			throw null; // this never actually runs, but lets method compile*/
		}
	}

	public static void Shutdown() {
		Log("Shutting down");
		HookManager.ForceUnsunscribeFromGlobalKeyboardEvents();
		HookManager.ForceUnsunscribeFromGlobalMouseEvents();
		quit = true;
	}

	// this is the only entry point for calls from NodeJS; so add to queue, and have the processing occur on background thread
	public static object CallMethod(dynamic invokeArgs) {
		StartBackgroundThreadIfNotStarted();
		return CallMethod_Internal((string)invokeArgs.methodName, (object[])invokeArgs.args);
	}
	public static void CallMethodAsync(dynamic invokeArgs) {
		StartBackgroundThreadIfNotStarted();
		// proceed to add call-method entry to the queue (to be executed by the background thread)
		callMethodQueue.Enqueue(new CallMethodEntry {methodName = (string)invokeArgs.methodName, args = (object[])invokeArgs.args, callback = invokeArgs.callback});
		// sadly, since we're enqueuing, we can't give a standard return-value; thus the NodeJS standard callback doesn't hold any data
		// > (though you can pass a custom callback function for the equivalent)
		//return true;
	}
}