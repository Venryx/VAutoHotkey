using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using System.Runtime.InteropServices;

public class Map_Dynamic : DynamicObject {
	IDictionary<string, object> source;
	public Map_Dynamic() : this(new ExpandoObject()) {}
	public Map_Dynamic(IDictionary<string, object> source) { this.source = source; }
	public object GetProperty(string name) {
		/*var type = _source.GetType();
		var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		return property?.GetValue(_source, null);*/
		return source.GetValueOrX(name);
	}
	public override bool TryGetMember(GetMemberBinder binder, out object result) {
		result = GetProperty(binder.Name);
		return true;
	}
	public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result) {
		result = GetProperty((string)indexes[0]);
		return true;
	}

	public static implicit operator Dictionary<string, object>(Map_Dynamic s) { return new Dictionary<string, object>(s.source); } 
}

public static class V {
	// general
	// ==========

	public static void Nothing() {}
	public static void Log(string message) { Console.WriteLine(message); }

	// exception rethrowing
	// ==========

	// maybe make-so: these process exception messages/stack-traces to remove not-useful stack-trace-entries
	public static void RethrowException(Exception ex, string rethrowExceptionMessage = null) {
		//var message = rethrowExceptionMessage ?? ex.Message;
		var message = rethrowExceptionMessage;

		Exception exception = null;
		try {
			// assume typed Exception has "new (String message, Exception innerException)" signature
			exception = (Exception)Activator.CreateInstance(ex.GetType(), message, ex);
		}
		catch {
			// constructor doesn't have the right constructor; eat the error and throw the original exception, as below
		}
		if (exception == null) // if creating rethrow-exception failed, fall back to just throwing exception
			exception = ex;

		throw exception;
	}
	//[DebuggerHidden]
	public static void RethrowInnerExceptionOf(TargetInvocationException ex, string rethrowExceptionMessage = null) {
		//var message = rethrowExceptionMessage ?? ex.InnerException.Message;
		var message = rethrowExceptionMessage;

		Exception exception = null;
		try {
			// assume typed Exception has "new (String message, Exception innerException)" signature
			exception = (Exception)Activator.CreateInstance(ex.InnerException.GetType(), message, ex.InnerException);
		}
		catch {
			// constructor doesn't have the right constructor; eat the error and throw the original inner-exception, as below
		}
		//if (exception == null) { //|| exception.InnerException == null || exception.Message != message) {
		if (exception == null) // if creating rethrow-exception failed, fall back to just throwing inner-exception
			exception = ex.InnerException;

		throw exception;
	}

	public static void LogInnerExceptionOf(TargetInvocationException ex) {
		// maybe temp
		var fullMessage = "";
		Exception currentEx = ex;
		while (currentEx.InnerException != null) {
			currentEx = currentEx.InnerException;
			//var currentExStr = currentEx.GetType().Name + ") " + currentEx.Message + "\nStack) " + currentEx.StackTrace;
			var currentExStr = currentEx.ToString();
			fullMessage = currentExStr + fullMessage;
		}
		Console.WriteLine(fullMessage);
	}
	
	public delegate bool ConsoleEventDelegate(int eventType);
	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

	public static T ParseEnum<T>(string enumName, bool firstLetterCaseMatters = true) {
		if (!firstLetterCaseMatters)
			enumName = enumName.Substring(0, 1).ToUpper() + enumName.Substring(1);
		return (T)System.Enum.Parse(typeof(T), enumName);
	}
}