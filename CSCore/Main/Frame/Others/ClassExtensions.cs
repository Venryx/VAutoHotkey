using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static System.String;

public static class ClassExtensions {
	// IEnumerable<T>
	public static string JoinUsing(this IEnumerable list, string separator) { return Join(separator, list.Cast<string>().ToArray()); }

	// List<T>
	//public static T GetValueOrX<T>(this List<T> s, int index, T defaultValue = default(T)) { return index >= 0 && index < s.Count ? s[index] : defaultValue; }

	// Dictionary<TKey, TValue>
	public static TValue GetValueOrX<TKey, TValue>(this Dictionary<TKey, TValue> self, TKey key, TValue defaultValueX = default(TValue)) {
		TValue val;
		if (self.TryGetValue(key, out val))
			return val;
		return defaultValueX;
	}
}