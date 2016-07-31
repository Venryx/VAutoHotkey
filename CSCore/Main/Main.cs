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

public class Main_NodeJSEntryPoint {
	// this is the only entry point for calls from NodeJS; so add to queue, and have the processing occur on background thread
	public async Task<object> CallMethod(dynamic invokeArgs) {
		var result = Main.CallMethod(invokeArgs);
		Log("Result2:" + result);
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
				Log("Test200");
				var entry = callMethodQueue.Dequeue();
				var result = CallMethod_Internal(entry.methodName, entry.args);
				Log("Calling callback with result)" + result + " @methodname:" + entry.methodName);
				entry.callback(result);
			}
			Thread.Sleep(4); // sleeping too long, e.g. 10ms will result in touch events arriving several seconds delayed due to event overload
		}
	}

	static object CallMethod_Internal(string methodName, object[] args) {
		Console.WriteLine("Calling) " + methodName + " Args: " + args.Select(a=>a.GetType().Name).JoinUsing(","));
		try {
			var newArgs = new List<object>();
			foreach (var arg in args)
				if (arg is ExpandoObject)
					//newArgs.Add((IDictionary<string, object>)arg);
					newArgs.Add(new Map_Dynamic(arg));
				else
					newArgs.Add(arg);
			args = newArgs.ToArray();

			/*if (args.Length > methods[methodName].GetParameters().Length)
				args = args.Take(methods[methodName].GetParameters().Length).ToArray();*/
			var additionalArgsNeeded = methods[methodName].GetParameters().Length - args.Length;
			if (additionalArgsNeeded > 0)
				args = args.Concat(Enumerable.Range(0, additionalArgsNeeded).Select(a=>Type.Missing)).ToArray();

			var result = methods[methodName].Invoke(null, args);
			if (result == null)
				result = "@@@NULL@@@";
			Log("Result:" + result);
			return result;
		}
		catch (TargetInvocationException ex) {
			V.LogInnerExceptionOf(ex);
			return null;
			/*V.RethrowInnerExceptionOf(ex);
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