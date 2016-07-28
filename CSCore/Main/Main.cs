using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gma.UserActivityMonitor;
using static V;

public class Main {
	Dictionary<string, MethodInfo> methods;
	public Main() {
		methods = typeof(Methods).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).ToDictionary(a=>a.Name, a=>a);
	}

	bool backgroundThreadStarted;
	public void StartBackgroundThreadIfNotStarted() {
		if (backgroundThreadStarted)
			return;
		backgroundThreadStarted = true;

		// set up pre-quit handler, so we can unregister hooks before quit
		preQuitHandler = eventType=> {
			if (eventType == 2)
				Shutdown();
			return false;
		};
		SetConsoleCtrlHandler(preQuitHandler, true);

		// run the below loop in a separate thread
		// (this thread is in charge of checking for input events, as well as processing queued CallMethod calls)
		Task.Run((Action)BackgroundThreadLoop);
	}
	ConsoleEventDelegate preQuitHandler;
	public bool quit;

	class CallMethodEntry {
		public string methodName;
		public object[] args;
	}
	Queue<CallMethodEntry> callMethodQueue = new Queue<CallMethodEntry>();
	void BackgroundThreadLoop() {
		// just keep looping here, processing input events and CallMethod calls, till NodeJS tells us to stop
		while (!quit) {
			Application.DoEvents(); // process input-events
			//while (callMethodQueue.Count > 0) {
			if (callMethodQueue.Count > 0) {
				var entry = callMethodQueue.Dequeue();
				CallMethod_Internal(entry.methodName, entry.args);
			}
			Thread.Sleep(4); // sleeping too long, e.g. 10ms will result in touch events arriving several seconds delayed due to event overload
		}
	}

	object CallMethod_Internal(string methodName, object[] args) {
		Console.WriteLine("Calling) " + methodName + " Args: " + args.Select(a=>a.GetType().Name).JoinUsing(","));
		try {
			return methods[methodName].Invoke(null, args);
		}
		catch (TargetInvocationException ex) {
			/*V.LogInnerExceptionOf(ex);
			return null;*/
			V.RethrowInnerExceptionOf(ex);
			throw null; // this never actually runs, but lets method compile
		}
	}

	public void Shutdown() {
		HookManager.ForceUnsunscribeFromGlobalKeyboardEvents();
		HookManager.ForceUnsunscribeFromGlobalMouseEvents();
		quit = true;
	}

	// this is the only entry point for calls from NodeJS; so add to queue, and have the processing occur on background thread
	public async Task<object> CallMethod(dynamic invokeArgs) {
		StartBackgroundThreadIfNotStarted();
		return CallMethod_Internal((string)invokeArgs.methodName, (object[])invokeArgs.args);
	}
	public async Task<object> CallMethodAsync(dynamic invokeArgs) {
		StartBackgroundThreadIfNotStarted();
		// proceed to add call-method entry to the queue (to be executed by the background thread)
		callMethodQueue.Enqueue(new CallMethodEntry {methodName = (string)invokeArgs.methodName, args = (object[])invokeArgs.args});
		// sadly, since we're enqueuing, we can't give a standard return-value; thus the NodeJS standard callback doesn't hold any data
		// > (though you can pass a custom callback function for the equivalent)
		return true;
	}
}