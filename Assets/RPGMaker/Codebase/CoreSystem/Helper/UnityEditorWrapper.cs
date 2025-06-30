using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.CoreSystem.Helper
{
    public class UnityEditorWrapper
    {
        // AssetDatabase
        public class AssetDatabaseWrapper
        {
            public static void Refresh() {
#if UNITY_EDITOR
                // AssetDatabase.Refresh();
#endif
            }

            public static void Refresh2() {
#if UNITY_EDITOR
                AssetDatabase.Refresh();
#endif
            }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            public static string LoadJsonString(string path) {
#else
            public static async Task<string> LoadJsonString(string path) {
#endif
#if UNITY_EDITOR && !UNITE_WEBGL_TEST
                var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs, Encoding.GetEncoding("UTF-8"));
                return sr.ReadToEnd();
#else
#if !UNITY_WEBGL
                return AddressableManager.Load.LoadAssetSync<TextAsset>(path)?.text;
#else
                return (await AddressableManager.Load.LoadAssetSync<TextAsset>(path))?.text;
#endif
#endif
            }
#if UNITY_EDITOR && UNITY_WEBGL
            public static string LoadJsonStringSync(string path) {
                var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs, Encoding.GetEncoding("UTF-8"));
                return sr.ReadToEnd();
            }
#endif

            public static void CreateAsset(Object asset, string path) {
#if UNITY_EDITOR
                AssetDatabase.CreateAsset(asset, path);
#endif
            }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            public static Object LoadAssetAtPath(string assetPath, Type type) {
#else
            public static async Task<Object> LoadAssetAtPath(string assetPath, Type type) {
#endif
#if UNITY_EDITOR && !UNITE_WEBGL_TEST
                return AssetDatabase.LoadAssetAtPath(assetPath, type);
#else
                switch (type.Name)
                {
                    case "TextAsset":
#if !UNITY_WEBGL
                        return AddressableManager.Load.LoadAssetSync<TextAsset>(assetPath);
#else
                        return await AddressableManager.Load.LoadAssetSync<TextAsset>(assetPath);
#endif
                    case "Sprite":
#if !UNITY_WEBGL
                        return AddressableManager.Load.LoadAssetSync<Sprite>(assetPath);
#else
                        return await AddressableManager.Load.LoadAssetSync<Sprite>(assetPath);
#endif
                    case "Texture":
#if !UNITY_WEBGL
                        return AddressableManager.Load.LoadAssetSync<Texture>(assetPath);
#else
                        return await AddressableManager.Load.LoadAssetSync<Texture>(assetPath);
#endif
                }
                return null;
#endif
            }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            public static T LoadAssetAtPath<T>(string assetPath) where T : Object {
#else
            public static async Task<T> LoadAssetAtPath<T>(string assetPath, bool mapAsset = false) where T : Object {
#endif
#if UNITY_EDITOR && !UNITE_WEBGL_TEST
                return AssetDatabase.LoadAssetAtPath<T>(assetPath);
#else
#if !UNITY_WEBGL
                return AddressableManager.Load.LoadAssetSync<T>(assetPath);
#else
                return await AddressableManager.Load.LoadAssetSync<T>(assetPath, mapAsset);
#endif
#endif
            }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            public static string[] FindAssets(string filter, string[] searchInFolders) {
#if UNITY_EDITOR
                return AssetDatabase.FindAssets(filter, searchInFolders);
#else
                return new string[0];
#endif
            }

            public static string GUIDToAssetPath(string guid) {
#if UNITY_EDITOR
                return AssetDatabase.GUIDToAssetPath(guid);
#else
                return "";
#endif
            }

            public static void SaveAssets() {
#if UNITY_EDITOR
                AssetDatabase.SaveAssets();
#endif
            }

            public static string CreateFolder(string parentFolder, string newFolderName) {
#if UNITY_EDITOR
                return AssetDatabase.CreateFolder(parentFolder, newFolderName);
#else
                return "";
#endif
            }
#endif
        }

        // PrefabUtility
        public class PrefabUtilityWrapper
        {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            public static GameObject LoadPrefabContents(string assetPath) {
#else
            public static async Task<GameObject> LoadPrefabContents(string assetPath, bool mapAsset = false) {
#endif
#if UNITY_EDITOR && !UNITE_WEBGL_TEST
                if (!File.Exists(assetPath))
                {
                    var oldAssetPath = assetPath;
                    assetPath = assetPath.Replace("SavedMaps", "SampleMaps");
                    if (assetPath != oldAssetPath)
                    {
                        DebugUtil.LogWarning(
                            $"ロードするプレハブファイルが存在しなかったので、ディレクトリを変更したパスでロードします" +
                            $" (\"{oldAssetPath}\" → \"{assetPath}\")。");
                    }
                }

                return PrefabUtility.LoadPrefabContents(assetPath);
#else
#if !UNITY_WEBGL
                return AddressableManager.Load.LoadAssetSync<GameObject>(assetPath);
#else
                return (await AddressableManager.Load.LoadAssetSync<GameObject>(assetPath, mapAsset));
#endif
#endif
            }

            public static void UnloadPrefabContents(GameObject prefab) {
#if UNITY_EDITOR && !UNITE_WEBGL_TEST
                PrefabUtility.UnloadPrefabContents(prefab);
#endif
            }

            public static GameObject SaveAsPrefabAsset(GameObject instanceRoot, string assetPath) {
#if UNITY_EDITOR && !UNITE_WEBGL_TEST
                return PrefabUtility.SaveAsPrefabAsset(instanceRoot, assetPath);
#else
                return null;
#endif
            }

            public static void RemovePrefabAsset(string assetPath) {
#if UNITY_EDITOR && !UNITE_WEBGL_TEST
                AssetDatabase.DeleteAsset(assetPath);
#else
#endif
            }
        }

        // FileUtil
        public class FileUtilWrapper
        {
            public static void CopyFileOrDirectory(string source, string dest) {
#if UNITY_EDITOR && !UNITE_WEBGL_TEST
                FileUtil.CopyFileOrDirectory(source, dest);
#endif
            }

            public static bool DeleteFileOrDirectory(string path) {
#if UNITY_EDITOR && !UNITE_WEBGL_TEST
                return FileUtil.DeleteFileOrDirectory(path);
#else
                return false;
#endif
            }
        }
    }
}