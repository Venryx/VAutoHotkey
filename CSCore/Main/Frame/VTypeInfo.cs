using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class VTypeInfo {
	static Dictionary<Type, VTypeInfo> cachedTypeInfo = new Dictionary<Type, VTypeInfo>();
	public static VTypeInfo Get(Type type) {
		/*if (!cachedTypeInfo.ContainsKey(type))
			Set(type);
		return cachedTypeInfo[type];*/

		// maybe temp (or conversely, maybe use approach elsewhere)
		VTypeInfo result;
		if (cachedTypeInfo.TryGetValue(type, out result))
			return result;
		Set(type);
		return cachedTypeInfo[type];
	}
	public static void Set(Type type) { cachedTypeInfo[type] = BuildTypeInfo(type); }
	static VTypeInfo BuildTypeInfo(Type type) {
		var result = new VTypeInfo();
		result.tags = type.GetCustomAttributes(true).OfType<Attribute>().ToList();
		foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
			if (!field.Name.StartsWith("<")) // anonymous types will have some extra field names starting with '<'
				result.props[field.Name] = VPropInfo.Get(field);
		foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
			result.props[property.Name] = VPropInfo.Get(property);
		foreach (MethodBase method in type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(member=>member is MethodBase)) { // include constructors
			var finalMethodName = method.Name; // maybe todo: add numbering system for if a method name is shared (e.g. one in base class, and one in derived)
			while (result.methods.ContainsKey(finalMethodName))
				finalMethodName += "_2";
			result.methods.Add(finalMethodName, VMethodInfo.Get(method));
		}
		return result;
	}

	public List<Attribute> tags; // convenience property
	public Dictionary<string, VPropInfo> props = new Dictionary<string, VPropInfo>();
	public Dictionary<string, VMethodInfo> methods = new Dictionary<string, VMethodInfo>();

	public VMethodInfo GetMethod(string name) { return methods.ContainsKey(name) ? methods[name] : null; }
}

public class VPropInfo {
	static Dictionary<MemberInfo, VPropInfo> cachedPropInfo = new Dictionary<MemberInfo, VPropInfo>();
	public static VPropInfo Get(MemberInfo prop) { // "fields" are considers "properties" as well
		if (!cachedPropInfo.ContainsKey(prop))
			cachedPropInfo[prop] = BuildPropInfo(prop);
		return cachedPropInfo[prop];
	}
	public static void Set(MemberInfo prop) { cachedPropInfo[prop] = BuildPropInfo(prop); }
	static VPropInfo BuildPropInfo(MemberInfo prop) {
		var result = new VPropInfo();
		result.memberInfo = prop;
		result.tags = prop.GetCustomAttributes(true).OfType<Attribute>().ToList();
		return result;
	}

	public MemberInfo memberInfo;
	public string Name { get { return memberInfo.Name; } }
	public List<Attribute> tags;

	public Type GetPropType() { return memberInfo is PropertyInfo ? ((PropertyInfo)memberInfo).PropertyType : ((FieldInfo)memberInfo).FieldType; }
	public object GetValue(object objParent) {
		if (memberInfo is FieldInfo)
			return ((FieldInfo)memberInfo).GetValue(objParent);
		return ((PropertyInfo)memberInfo).GetValue(objParent, null);
	}
	public void SetValue(object objParent, object value) {
		// maybe temp; fixes that List(Type) from JS gets received as a List(MonoType)
		//if (GetPropType() == typeof(List<Type>) && value != null && value.GetType().Name == "System.Collections.Generic.List`1[System.MonoType]")
		if (GetPropType() == typeof(List<Type>) && value != null && value.GetType() != typeof(List<Type>)) {
			var newList = new List<Type>();
			foreach (var obj in value as IList)
				newList.Add(obj as Type);
			value = newList;
		}

		if (memberInfo is FieldInfo)
			((FieldInfo)memberInfo).SetValue(objParent, value);
		else
			((PropertyInfo)memberInfo).SetValue(objParent, value, null);

		// custom
		//ObserverCore.NotifyPropSet(objParent, memberInfo.Name, value);
	}
}

/*public static class VMethodInfo_ClassExtensions {
	// VMethodInfo
	public static object CallIfExists(this VMethodInfo s, object objParent, params object[] args) {
		if (DebugFlags.catchErrors) {
			try {
				if (s != null)
					return s.Call(objParent, args);
				return null;
			}
			catch (Exception ex) {
				var exCopy = ex;
				//V.Nothing()
				V.Break();
				//Debug.LogException(ex);
				throw;
			}
		}

		if (s != null) {
			//return method.Call_WithProfiling(objParent, true, args);

			// inline of VMethodInfo.Call_WithProfiling method
			// ==========
			if (args.Length > s.memberInfo.GetParameters().Length)
				args = args.Take(s.memberInfo.GetParameters().Length).ToArray();
		
			object result;
			var S = s.memberInfo.Profile_LastDataFrame();
			try {
				result = s.memberInfo.Invoke(objParent, args);
				S._____(null);
			}
			catch (TargetInvocationException ex) {
				S._____(null);
				VDebug.RethrowInnerExceptionOf(ex);
				throw null; // this never actually runs, but lets method compile
			}
			return result;
			// ==========
		}
		return null;
	}
}*/
public class VMethodInfo {
	static Dictionary<MemberInfo, VMethodInfo> cachedMethodInfo = new Dictionary<MemberInfo, VMethodInfo>();
	public static VMethodInfo Get(MethodBase method) {
		if (!cachedMethodInfo.ContainsKey(method))
			Set(method);
		return cachedMethodInfo[method];
	}
	public static void Set(MethodBase method) { cachedMethodInfo[method] = BuildMethodInfo(method); }
	static VMethodInfo BuildMethodInfo(MethodBase method) {
		var result = new VMethodInfo();
		result.memberInfo = method;
		result.tags = method.GetCustomAttributes(true).OfType<Attribute>().ToList();
		return result;
	}

	public MethodBase memberInfo;
	public List<Attribute> tags;

	public object Call(object objParent, params object[] args) {
		if (args.Length > memberInfo.GetParameters().Length)
			args = args.Take(memberInfo.GetParameters().Length).ToArray();

		object result;
		try {
			result = memberInfo.Invoke(objParent, args);
		}
		catch (TargetInvocationException ex) {
			V.RethrowInnerExceptionOf(ex);
			throw null; // this never actually runs, but lets method compile
		}
		return result;
	}
	public object Call_Advanced(object objParent, bool profile, params object[] args) {
		if (args.Length > memberInfo.GetParameters().Length)
			args = args.Take(memberInfo.GetParameters().Length).ToArray();
		
		object result;
		try {
			result = memberInfo.Invoke(objParent, args);
		}
		catch (TargetInvocationException ex) {
			V.RethrowInnerExceptionOf(ex);
			throw null; // this never actually runs, but lets method compile
		}
		return result;
	}
}