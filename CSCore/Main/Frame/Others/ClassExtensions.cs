using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using static System.String;

public static class ClassExtensions {
	//public static T GetValueOrX<T>(this ExpandoObject s, string propName, T defaultValueX = default(T)) {
	// for ExpandoObjects/dynamic
	/*public static T Get<T>(this object s, string propName, T defaultValueX = default(T)) {
		var sAsDict = (IDictionary<string, object>)s;
		return (T)sAsDict.GetValueOrX(propName, defaultValueX);
	}*/

	// IEnumerable<T>
	public static string JoinUsing(this IEnumerable list, string separator) { return Join(separator, list.Cast<string>().ToArray()); }

	// List<T>
	//public static T GetValueOrX<T>(this List<T> s, int index, T defaultValue = default(T)) { return index >= 0 && index < s.Count ? s[index] : defaultValue; }

	// IDictionary<TKey, TValue>
	public static TValue GetValueOrX<TKey, TValue>(this IDictionary<TKey, TValue> self, TKey key, TValue defaultValueX = default(TValue)) {
		TValue val;
		if (self.TryGetValue(key, out val))
			return val;
		return defaultValueX;
	}
	/*public static TValueForItem GetValueOrX<TKey, TValue, TValueForItem>(this IDictionary<TKey, TValue> self, TKey key, TValueForItem defaultValueX = default(TValueForItem)) where TValueForItem : TValue {
		TValue val;
		if (self.TryGetValue(key, out val))
			return (TValueForItem)val;
		return defaultValueX;
	}*/
}