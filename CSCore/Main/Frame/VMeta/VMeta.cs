using System.Collections.Generic;
using System.Linq;

public class VNullClass {} // for null
public class VMeta {
	public static VMeta main = new VMeta();

	public Dictionary<int, Dictionary<object, VWeakReference>> weakMeta = new Dictionary<int, Dictionary<object, VWeakReference>>();
	public Dictionary<int, Dictionary<object, object>> strongMeta = new Dictionary<int, Dictionary<object, object>>();
	public VNullClass SetMeta(object obj, object metaKey, VNullClass metaValue, bool useStrongStorage = true) { return SetMeta<VNullClass>(obj, metaKey, metaValue, useStrongStorage); } // for null
	public T SetMeta<T>(object obj, object metaKey, T metaValue, bool useStrongStorage = true) {
		var objHash = obj != null ? obj.GetHashCode() : -1;
		if (useStrongStorage) {
			if (!strongMeta.ContainsKey(objHash))
				strongMeta[objHash] = new Dictionary<object, object>();
			strongMeta[objHash][metaKey] = metaValue;
		}
		else {
			var valueRef = new VWeakReference(metaValue);
			if (!weakMeta.ContainsKey(objHash))
				weakMeta[objHash] = new Dictionary<object, VWeakReference>();
			weakMeta[objHash][metaKey] = valueRef;
		}
		return metaValue;
	}

	// probably todo: have use the faster TryGetValue system
	public T GetMeta<T>(object obj, object metaKey) {
		var result = GetMeta(obj, metaKey);
		return (T)result;
	}
	public object GetMeta(object obj, object metaKey) { return GetMeta<object>(obj, metaKey, null); }
	public T GetMeta<T>(object obj, object metaKey, T returnValueIfMissing, bool useStrongStorage = true) {
		var objHash = obj != null ? obj.GetHashCode() : -1;
		if (useStrongStorage)
			return strongMeta.ContainsKey(objHash) && strongMeta[objHash].ContainsKey(metaKey) ? (T)strongMeta[objHash][metaKey] : returnValueIfMissing;
		return weakMeta.ContainsKey(objHash) && weakMeta[objHash].ContainsKey(metaKey) ? (T)weakMeta[objHash][metaKey].Target : returnValueIfMissing;
	}

	public void RemoveMeta(object obj, object metaKey, bool useStrongStorage = true) {
		var objHash = obj != null ? obj.GetHashCode() : -1;
		if (useStrongStorage)
			strongMeta[objHash].Remove(metaKey);
		else
			weakMeta[objHash].Remove(metaKey);
	}

	public Dictionary<object, object> GetMetaSet_Strong(object obj) {
		var objHash = obj != null ? obj.GetHashCode() : -1;
		return strongMeta.ContainsKey(objHash) ? strongMeta[objHash] : new Dictionary<object, object>();
	}
	public void ClearMeta(object obj, bool useStrongStorage = true) {
		var objHash = obj != null ? obj.GetHashCode() : -1;
		if (useStrongStorage)
			strongMeta.Remove(objHash);
		else
			weakMeta.Remove(objHash);
	}
}