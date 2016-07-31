using System.Collections;
using System.IO;
using System;
using System.Collections.Generic;

public static class FileManager {
	public static char PathSep = Path.DirectorySeparatorChar;
	//public static char PathSep = '/';
	public static DirectoryInfo root = new DirectoryInfo(".");
	public static DirectoryInfo csCore = new DirectoryInfo(".").GetFolder("CSCore/Main");

	public static DirectoryInfo GetFolder(string subpath = null) { return root.GetFolder(subpath); }
	public static FileInfo GetFile(string subpath = null) { return root.GetFile(subpath); }

	//public static string SimplifyPath(string path)
	public static string FormatPath(string path) {
		//var result = path.Replace('\\', '/');
		var result = path.Replace('/', PathSep).Replace('\\', PathSep);
		/*if (result.EndsWith("/")) // if final path-sep, remove it
			result = result.Substring(0, result.Length - 1);*/
		if (!result.EndsWith(PathSep.ToString())) // if final path-sep not-existing, add it
			result += PathSep;
		return result;
	}
	public static void CreateFoldersInPathIfMissing(string path) {
		path = FormatPath(path);

		string folder = path.Substring(0, path.LastIndexOf(PathSep) + 1);
		if (!Directory.Exists(folder))
			Directory.CreateDirectory(folder);
	}

	public static string GetFullPath(string pathFromRoot) { return FormatPath(root.VFullName() + pathFromRoot); }
	public static string GetRelativePath(string fullPath) { return FormatPath(fullPath).Replace(root.VFullName(), ""); }
}