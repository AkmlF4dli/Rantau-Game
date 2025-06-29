using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Linq;
using System.Reflection;

internal static class ExtractOnImport
{
    [Serializable]
    private class UpdateFileSettings
    {
        public List<string> extract_filenames = new List<string>();
        public List<string> overwrite_extentions = new List<string>();
        public List<string> overwrite_filenames = new List<string>();
        public List<string> overwrite_filefullpathnames = new List<string>();
        public List<string> remove_filenames = new List<string>();
        public List<string> remove_foldernames = new List<string>();
        public List<string> remove_packages = new List<string>();



        public bool isExtractFilename(string fullPath) {
            var filename = Path.GetFileName(fullPath);
            return extract_filenames.Any(e => e == filename);
        }

        public bool isMatchPathOrExtention(string fileName) {
            var ext = Path.GetExtension(fileName);
            return overwrite_filenames.Any(fileName.Contains) || overwrite_extentions.Any(e => e == ext);

        }
        public bool isMatchFullPath(string fullPath) {
            return overwrite_filefullpathnames.Any(fullPath.Contains);

        }
    }

    private const string InstallUniteVersion = "1.2.4";

    [InitializeOnLoadMethod]
    private static void ImportRPGMakerAssetsOnImport() {
        var packageRootPath = Path.Combine(Application.dataPath, "../Packages/jp.ggg.rpgmaker.unite");
        var packageArchivePath = Path.Combine(packageRootPath, "System/Archive");

        // zipファイルが存在しない場合は、新規PJ作成又は、既にアップデート処理済み
        // そのケースでは、新規PJ作成時に、最新バージョンのプログラムとStorageになっているため、本処理自体が不要である
        if (!Directory.Exists(packageArchivePath))
        {
            return;
        }
        // バージョンアップ中
        var LocalVersionCodePath = Path.Combine(packageRootPath, "versioncode.txt");
        {
            using var fs = File.Create(LocalVersionCodePath);
            using var writer = new StreamWriter(fs, Encoding.UTF8);
            writer.Write("Program Update");
        }

        AssetDatabase.StartAssetEditing();

        // 初回インストールかどうかの判別
        var StoragePath = Path.Combine(Application.dataPath, "RPGMaker", "Storage");
        var LocalVersionPath = Path.Combine(packageRootPath, "version.txt");
        // Storageは、すでに存在する場合には上書きしない
        if (!Directory.Exists(StoragePath))
        {
            var ProjectBasePath = Path.Combine(Application.dataPath, "../");
            // projectBaseを解凍する
            ZipFile.ExtractToDirectory(Path.Combine(packageArchivePath, $"project_base_v{InstallUniteVersion}.zip"), ProjectBasePath, true);
            EditorUtility.DisplayProgressBar("Extract RPGMAKERUNITE Package", $"Unzip masterdata_jp_v{InstallUniteVersion}.zip files ...", 0.5f);

            // 共通Storageを解凍する
            ZipFile.ExtractToDirectory(Path.Combine(packageArchivePath, $"masterdata_jp_v{InstallUniteVersion}.zip"), StoragePath, true);
            // 現在の言語設定
            var language = Application.systemLanguage;
            // 言語Storageを解凍する
            switch (language)
            {
                case SystemLanguage.Japanese:
                    break;
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.ChineseTraditional:
                    EditorUtility.DisplayProgressBar("Extract RPGMAKERUNITE Package", $"Unzip masterdata_ch_v{InstallUniteVersion}.zip files ...", 0.7f);
                    ZipFile.ExtractToDirectory(Path.Combine(packageArchivePath, $"masterdata_ch_v{InstallUniteVersion}.zip"), StoragePath, true);
                    break;
                default:
                    EditorUtility.DisplayProgressBar("Extract RPGMAKERUNITE Package", $"Unzip masterdata_en_v{InstallUniteVersion}.zip files ...", 0.7f);
                    ZipFile.ExtractToDirectory(Path.Combine(packageArchivePath, $"masterdata_en_v{InstallUniteVersion}.zip"), StoragePath, true);
                    break;
            }
        }
        else
        {
            // アップデートの場合
            // バージョンが逆行していないか確認する
            var currentUniteVersion = "1.0.0";
            if (File.Exists(LocalVersionPath))
            {
                using var reader = new StreamReader(LocalVersionPath, Encoding.UTF8);
                currentUniteVersion = reader.ReadToEnd();
            }
            var v1 = new Version(InstallUniteVersion);
            var v2 = new Version(currentUniteVersion);
            if (v1 > v2)
            {
                var settingJsonPath = Path.Combine(packageRootPath, "Editor/update_file_settings.json");
                var texts = File.ReadAllText(settingJsonPath);
                var update_setting = JsonUtility.FromJson<UpdateFileSettings>(texts);
                if (update_setting == null)
                {
                    Debug.LogError("Unite Update Exception on read update_file_settings.json section.");
                    return;
                }

                //　project_baseのzip展開
                var zipUnpackRootPAth = Path.Combine(Application.dataPath, "..");
                var zipFileName = Path.Combine(packageArchivePath, $"project_base_v{InstallUniteVersion}.zip");

                EditorUtility.DisplayProgressBar("Extract RPGMAKERUNITE Package", $"unapack update files ...", 0.8f);

                using (var archive = ZipFile.OpenRead(zipFileName))
                {
                    int entryCount = archive.Entries.Count;
                    foreach (var entry in archive.Entries)
                    {
                        var destPath = Path.Combine(zipUnpackRootPAth, entry.FullName);
                        // ディレクトリの判別、ZipArchiveEntryがnameを持っていない
                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            // Directoryがなかった場合は作成する
                            if (Directory.Exists(destPath) == false)
                            {
                                Directory.CreateDirectory(destPath);
                            }
                        }
                        else if (File.Exists(destPath) == false)
                        {
                            // ない場合はコピー
                            entry.ExtractToFile(destPath, true);
                        }
                        else
                        {
                            // ある場合は上書きチェック
                            // 以下の３パターンは更新がかかります
                            //・除外ファイル名にマッチしない
                            //・許可拡張子にマッチする
                            //・ファイル名がマッチする
                            if (update_setting.isExtractFilename(entry.Name) == false &&
                                (update_setting.isMatchPathOrExtention(entry.Name) ||
                                 update_setting.isMatchFullPath(entry.FullName)))
                            {
                                entry.ExtractToFile(destPath, true);
                            }
                        }
                    }
                }
                // 
                update_setting.remove_filenames.ForEach(file =>
                {
                    var fileName = Path.Combine(Application.dataPath, "../", file);
                    if (File.Exists(fileName))
                    {
                        // ファイルが存在する場合、削除
                        File.Delete(fileName);
                    }
                });
                // 
                update_setting.remove_foldernames.ForEach(folder =>
                {
                    var directoryName = Path.Combine(Application.dataPath, "../", folder);
                    if (Directory.Exists(directoryName))
                    {
                        Directory.Delete(directoryName, true);
                    }
                });

                try
                {
                    // manifest.jsonから不要なパッケージを削除
                    var manifestPath = Path.Combine(packageRootPath, "../manifest.json");
                    var manifestTexts = File.ReadAllLines(manifestPath);
                    var resultTexts = new List<string>();
                    foreach (var text in manifestTexts)
                    {
                        if (update_setting.remove_packages.Any(text.Contains) == false)
                        {
                            resultTexts.Add(text);
                        }
                    }
                    File.WriteAllLines(manifestPath, resultTexts);
                }
                catch
                {
                }
            }
        }

        // zipファイルを共通テンポラリフォルダにコピーする
        {
            // 自分のバージョンより新しいものは残すようにする？
#if UNITY_EDITOR_WIN
            // 2階層上
            var folderSub = "/../../";
#else
        // 4階層上
        var folderSub = "/../../../../";
#endif
            var temporaryFolderPath = Path.Combine(Application.persistentDataPath + folderSub, ".RPGMaker");

            // 一番高いバージョンを検出する
            var myVersion = new Version(InstallUniteVersion);
            var higherVersion = new Version(InstallUniteVersion);
            if (Directory.Exists(temporaryFolderPath))
            {
                // 親ディレクトリ内のすべてのサブディレクトリを取得
                var directories = Directory.GetDirectories(temporaryFolderPath);
                foreach (var dir in directories)
                {
                    if (Version.TryParse(Path.GetFileName(dir), out var version))
                    {
                        if (version > higherVersion)
                        {
                            higherVersion = version;
                        }
                        else
                        {
                            // 他のフォルダは消す
                            Directory.Delete(dir, true);
                        }
                    }
                }
                // 不要な旧バージョンのZipを削除
                var fileinfo = Directory.GetFiles(temporaryFolderPath);
                foreach (var file in fileinfo)
                {
                    if (Path.GetExtension(file) == ".zip")
                    {
                        File.Delete(file);
                    }
                }
            }
            // 自分が一番新しいバージョンの場合はZipをコピーする
            if (myVersion == higherVersion)
            {
                var versionFolderPath = Path.Combine(temporaryFolderPath, InstallUniteVersion);
                if (!Directory.Exists(versionFolderPath))
                {
                    Directory.CreateDirectory(versionFolderPath);
                }
                var s_templateNames = new List<string>
                {
                    "project_base_v",
                    "masterdata_jp_v",
                    "masterdata_en_v",
                    "masterdata_ch_v",
                    "defaultgame_jp_v",
                    "defaultgame_en_v",
                    "defaultgame_ch_v",
                };
                s_templateNames.ForEach(t =>
                    {
                        var filename = $"{t}{InstallUniteVersion}.zip";
                        var sourceZipFile = Path.Combine(packageArchivePath, filename);
                        if (File.Exists(sourceZipFile))
                        {
                            File.Copy(sourceZipFile, Path.Combine(versionFolderPath, filename), true);
                        }
                    });
            }
        }

        // Versionのファイルを更新
        {
            using var writer = new StreamWriter(LocalVersionPath, false, Encoding.UTF8);
            writer.Write(InstallUniteVersion);
        }

        // 終了処理
        // テンポラリとzipフォルダの削除
        try
        {
            EditorUtility.DisplayProgressBar("Extract RPGMAKERUNITE Package", $"Remove package zipfiles ...", 1f);

            var ZipPath = "Packages/jp.ggg.rpgmaker.unite/System";
            Directory.Delete(ZipPath, true);
            File.Delete(ZipPath + ".meta");

            File.Delete(LocalVersionCodePath);
            File.Delete(LocalVersionCodePath + ".meta");
        }
        catch
        {
        }

        EditorUtility.DisplayProgressBar("Extract RPGMAKERUNITE Package", $"save update file ...", 1f);

        EditorUtility.ClearProgressBar();

        // AssetDatabaseを再開
        AssetDatabase.StopAssetEditing();

        // リコンパイルの要求
        //EditorApplication.UnlockReloadAssemblies();
        //AssetDatabase.Refresh();
        //CompilationPipeline.RequestScriptCompilation();
        RebootUnityEditor();
    }

    public static void RebootUnityEditor() {
        var restartEditorAndRecompileScripts = typeof(EditorApplication).GetMethod("RestartEditorAndRecompileScripts", BindingFlags.NonPublic | BindingFlags.Static);
        restartEditorAndRecompileScripts.Invoke(null, null);
    }
}
