using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using static System.String;

public static class ClassExtensions {
	// string
	// string
	public static string TrimStart(this string s, int length) { return s.Substring(length); }
	public static string TrimEnd(this string s, int length) { return s.Substring(0, s.Length - length); }
	public static string SubstringSE(this string self, int startIndex, int enderIndex) { return self.Substring(startIndex, enderIndex - startIndex); }
	public static int IndexOf_X(this string s, int x, string str) { // (0-based)
		var currentPos = -1;
		for (var i = 0; i <= x; i++) {
			var subIndex = s.IndexOf(str, currentPos + 1);
			if (subIndex == -1)
				return -1; // no such xth index
			currentPos = subIndex;
		}
		return currentPos;
	}
	public static int IndexOf_XFromLast(this string s, int x, string str) { // (0-based)
		var currentPos = (s.Length - str.Length) + 1; // index just after the last-index-where-match-could-occur
		for (var i = 0; i <= x; i++) {
			var subIndex = s.LastIndexOf(str, currentPos - 1);
			if (subIndex == -1)
				return -1; // no such xth index
			currentPos = subIndex;
		}
		return currentPos;
	}
	public static string Replace_Regex(this string s, string regexMatch, string replaceWith) { return new Regex(regexMatch).Replace(s, replaceWith); }

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

	// IDictionary<TKey, TValue of struct> (e.g. IDictionary<string, bool>)
	public static TValue? GetValueOrXNullable<TKey, TValue>(this IDictionary<TKey, TValue> obj, TKey key, TValue? defaultValueX = default(TValue?)) where TValue : struct {
		TValue result;
		if (obj.TryGetValue(key, out result))
			return result;
		return null;
	}

	// DirectoryInfo
	public static DirectoryInfo VCreate(this DirectoryInfo folder) { folder.Create(); return folder; }
	public static DirectoryInfo GetFolder(this DirectoryInfo folder, string subpath) { return new DirectoryInfo(folder.FullName + (subpath != null && subpath.StartsWith("/") ? "" : "/") + subpath); }
	public static string GetSubpathOfDescendent(this DirectoryInfo folder, DirectoryInfo descendent) { return descendent.FullName.Substring(folder.FullName.Length); }
	public static string GetSubpathOfDescendent(this DirectoryInfo folder, FileInfo descendent) { return descendent.FullName.Substring(folder.FullName.Length); }
	public static FileInfo GetFile(this DirectoryInfo folder, string subpath) { return new FileInfo(folder.FullName + (subpath != null && subpath.StartsWith("/") ? "" : "/") + subpath); }
	public static void CopyTo(this DirectoryInfo source, DirectoryInfo target) {
		if (source.FullName == target.FullName)
			throw new Exception("Source and destination cannot be the same.");
		// fix for if root-call folder has files but not folders
		if (!target.Exists)
			target.Create();
		foreach (DirectoryInfo dir in source.GetDirectories())
			dir.CopyTo(target.CreateSubdirectory(dir.Name));
		foreach (FileInfo file in source.GetFiles())
			file.CopyTo(Path.Combine(target.FullName, file.Name));
	}
	public static DirectoryInfo GetSubfolder(this DirectoryInfo folder, string subpath) // use this if you want to make sure you're accessing a subfolder (e.g. so as to ensure not deleting a master folder)
	{
		var result = folder.GetFolder(subpath);
		if (folder.VFullName() != result.VFullName())
			return result;
		return null;
	}
	//public static void DeleteToRecycleBin(this DirectoryInfo folder) { V.DeleteFileOrFolderToRecycleBin(folder.FullName); }
	public static DirectoryInfo[] GetDirectories_Safe(this DirectoryInfo s, string searchPattern = null, SearchOption searchOption = SearchOption.TopDirectoryOnly)
	{
		if (!s.Exists)
			return new DirectoryInfo[0];
		if (searchOption == SearchOption.AllDirectories)
			return s.GetDirectories(searchPattern ?? "*", searchOption);
		if (searchPattern != null)
			return s.GetDirectories(searchPattern);
		return s.GetDirectories();
	}
	public static string VFullName(this DirectoryInfo s, DirectoryInfo relativeTo = null)
	{
		var result = FileManager.FormatPath(s.FullName);
		if (relativeTo != null)
			result = result.Substring(relativeTo.VFullName().Length);
		return result;
	}

	// Process
	public static Process GetParentProcess(this Process s) {
		var query = $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {s.Id}";
		var search = new ManagementObjectSearcher("root\\CIMV2", query);
		var results = search.Get().GetEnumerator();
		results.MoveNext();
		var queryObj = results.Current;
		var parentId = (uint)queryObj["ParentProcessId"];
		return Process.GetProcessById((int)parentId);
	}
}