#define USE_PARTIAL_LOOP
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

#endif
#if USE_PARTIAL_LOOP
using RPGMaker.Codebase.Runtime.Common;
#endif

namespace RPGMaker.Codebase.CoreSystem.Helper
{
    public class AddressableManager
    {
        //====================================================================================================
        // リソース読み込みクラス
        //====================================================================================================
        public class Load
        {
            private static readonly Dictionary<string, AssetEntity> _entities = new Dictionary<string, AssetEntity>();
#if UNITY_WEBGL
            private static uint _timestamp = 0;
#endif
            /// <summary>
            ///     Addressables.LoadAssetAsync() の代わり
            /// </summary>
            /// <typeparam name="T">読み込むデータのデータ型</typeparam>
            /// <param name="key">アドレス、ラベル</param>
            /// <param name="action">引数は ASyncOperationHandle ではなく T型 のインスタンス</param>
            public static void LoadAssetAsync<T>(string key, Action<T> action = null) {
                loadAssetAsync(key, action);
            }

            /// <summary>
            ///     Addressables.LoadAssetsAsync() の代わり
            /// </summary>
            /// <typeparam name="T">読み込むデータのデータ型</typeparam>
            /// <param name="key">アドレス、ラベル</param>
            /// <param name="actions">引数は ASyncOperationHandle ではなく T型 のインスタンス配列</param>
            public static void LoadAssetsAsync<T>(string key, Action<IList<T>> actions = null) {
                loadAssetAsync(key, null, actions);
            }

            /// <summary>
            ///     現在のロードファイル数を取得します
            /// </summary>
            public static int GetLoadCount() {
                var loadCount = 0;

                foreach (var pair in _entities)
                    if (pair.Value.LoadStatus == eLoadStatus.Load)
                        loadCount++;

                return loadCount;
            }

            /// <summary>
            ///     指定したアドレス、ラベルの読み込み完了を確認します
            /// </summary>
            public static bool CheckLoadDone<T>(string key) {
                var typekey = $"{key} {typeof(T)}";
                if (_entities.ContainsKey(typekey) == false) return false;

                if (_entities[typekey].LoadStatus != eLoadStatus.Ready) return false;

                return true;
            }

            /// <summary>
            ///     アセットアンロード
            /// </summary>
            /// <typeparam name="T">読み込んだデータのデータ型</typeparam>
            /// <param name="key">アドレス、ラベル</param>
            public static void Release<T>(string key) {
                var typekey = $"{key} {typeof(T)}";
                if (_entities.ContainsKey(typekey) == false) return;

                var entity = _entities[typekey];
                if (--entity.RefCount > 0) return;

                if (entity.LoadStatus == eLoadStatus.Ready)
                {
                    unload(typekey);
                }
                else if (entity.LoadStatus == eLoadStatus.Load)
                {
                    // ロード中は、ロード完了を待ってからアンロードする
                    entity.LoadCancel = true;
                    CoroutineAccessor.Start(loadCancel(typekey));
                }
            }

            /// <summary>
            ///     全アセットアンロード
            /// </summary>
            public static void ReleaseAll() {
                // key取得
                var key = new List<string>();
                foreach (var entity in _entities)
                    key.Add(entity.Key);

                // データリリース
                for (var i = 0; i < key.Count; i++)
                    if (--_entities[key[i]].RefCount > 0)
                        unload(key[i]);
            }

#if UNITY_WEBGL
            /// <summary>
            /// 指定の数値以前にロードされ再ロードされなかったロード済みエンティティを開放する。
            /// </summary>
            /// <param name="threshold"></param>
            public static void ReleaseLeastRecent(int threshold) {
                var list = _entities.Values.ToArray();
                foreach (var entity in list)
                {
                    if (entity.LoadStatus == eLoadStatus.Ready)
                    {
                        if (entity.Mark && _timestamp - entity.Timestamp >= threshold)
                        {
                            unload(entity.Key);
                            continue;
                        }
                    }
                }
            }

            /// <summary>
            /// 読み込み完了していて参照の無い（別のマップに切り替わったら不要となる）エンティティをマークする。
            /// </summary>
            public static void MarkForRelease() {
                foreach (var entity in _entities.Values.ToList())
                {
                    if (entity.LoadStatus == eLoadStatus.Ready && entity.RefCount == 0)
                    {
                        entity.Mark = true;
                    }
                }
            }

            /// <summary>
            /// マップ切替のタイミングで読み込まれるアセットのエンティティのタイムスタンプを変更する。
            /// </summary>
            public static void IncrementTimestamp() {
                _timestamp++;
            }
#endif

            /// <summary>
            ///     ロード本体
            /// </summary>
            private static void loadAssetAsync<T>(
                string key,
                Action<T> action = null,
                Action<IList<T>> actions = null
            ) {
                var typekey = $"{key} {typeof(T)}";
                if (_entities.ContainsKey(typekey))
                {
                    var entity = _entities[typekey];
                    entity.RefCount++;
#if UNITY_WEBGL
                    entity.Timestamp = _timestamp;
#endif
                    if (entity.LoadStatus == eLoadStatus.Ready)
                        // 既に読み込まれているならキャッシュで complete
                        loadCompleted(entity, action, actions);
                    else if (entity.LoadStatus == eLoadStatus.Load)
                        // 既に読み込み中なら読み込み完了イベントで complete
                        entity.Handle.Completed +=
                            op => { loadCompleted(entity, action, actions); };
                }
                else
                {
                    var entity = new AssetEntity();
                    entity.RefCount++;
                    entity.LoadStatus = eLoadStatus.Load;
#if UNITY_WEBGL
                    entity.Timestamp = _timestamp;
                    entity.Mark = false;
#endif
                    if (action != null)
                        entity.Handle = Addressables.LoadAssetAsync<T>(key);
                    else
                        entity.Handle = Addressables.LoadAssetsAsync<T>(key, null);

                    entity.Key = typekey;
                    entity.Handle.Completed +=
                        op =>
                        {
                            loadCompleted(entity, action, actions);
                            entity.LoadStatus = eLoadStatus.Ready;
                        };
                    _entities[typekey] = entity;
                }
            }

            /// <summary>
            ///     ロード完了
            /// </summary>
            private static void loadCompleted<T>(
                AssetEntity entity,
                Action<T> action = null,
                Action<IList<T>> actions = null
            ) {
                if (entity.LoadCancel)
                    // キャンセル指示が入ってるので、complete を成立させない
                    return;

                if (action != null)
                    action?.Invoke((T) entity.Handle.Result);
                else
                    actions?.Invoke((IList<T>) entity.Handle.Result);
            }

            /// <summary>
            ///     ロード中にキャンセル入った場合のアンロード処理
            ///     ロード完了を待ってからアンロードする
            /// </summary>
            private static IEnumerator loadCancel(string typekey) {
                var entity = _entities[typekey];
                while (entity.LoadStatus == eLoadStatus.Load) yield return null;

                unload(typekey);
            }

            /// <summary>
            ///     アンロード
            /// </summary>
            public static void unload(string typekey) {
                var entity = _entities[typekey];
                Addressables.Release(entity.Handle);
                _entities.Remove(typekey);
            }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            // 同期読み込み
            public static T LoadAssetSync<T>(string path) {
#else
            public static async Task<T> LoadAssetSync<T>(string path, bool mapAsset = false) {
#endif
                var path_replace = path.Replace("\\", "/");

                // 親ディレクトリを含めたパスを取得
                int start = path_replace.LastIndexOf("/", path_replace.LastIndexOf("/") - 1) + 1;
                path_replace = path.Substring(start);

                // 拡張子がjsonだった場合はassetに変換
                if (System.IO.Path.GetExtension(path_replace) == ".json")
                {
                    var dir = System.IO.Path.GetDirectoryName(path_replace);
                    var name = System.IO.Path.GetFileNameWithoutExtension(path_replace);
                    dir = dir.Replace("JSON", "SO");
                    path_replace = dir + "/" + name + ".asset";
                }

                if (path_replace == "") return default;

                var typekey = $"{path_replace} {typeof(T)}";
                if (_entities.ContainsKey(typekey))
                {
                    var entity = _entities[typekey];
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    entity.RefCount++;
#else
                    if (!mapAsset) entity.RefCount++;
                    entity.Timestamp = _timestamp;
                    entity.Mark = false;
#endif

                    // ロード済みでNULLでなければ返す
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    if (entity.LoadStatus == eLoadStatus.Ready && entity.Handle.Result != null)
#else
                    if (entity.LoadStatus == eLoadStatus.Ready)
#endif
                        // 既に読み込まれているならキャッシュを返す
                        return (T) entity.Handle.Result;

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    if (entity.LoadStatus == eLoadStatus.Load)
#else
#endif
                    {
                        // 既に読み込み中なら読み込み完了まで待機
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        entity.Handle.WaitForCompletion();
                        entity.Key = typekey;
                        entity.LoadStatus = eLoadStatus.Ready;
                        _entities[typekey] = entity;
#else
                        while (entity.LoadStatus != eLoadStatus.Ready)
                        {
                            await UniteTask.Delay(1);
                        }
#endif
                        return (T) entity.Handle.Result;
                    }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    entity.LoadStatus = eLoadStatus.Load;
                    try
                    {
                        entity.Handle = Addressables.LoadAssetAsync<T>(path_replace);
                        // 読み込み完了まで待機
                        entity.Handle.WaitForCompletion();

                        entity.Key = typekey;
                        entity.LoadStatus = eLoadStatus.Ready;
                        _entities[typekey] = entity;

                        return (T) entity.Handle.Result;
                    }
                    catch (Exception)
                    {
                        // 読み込めない場合はdefault
                        return default;
                    }
#else
#endif
                }
                else
                {
                    var entity = new AssetEntity();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    entity.RefCount++;
#else
                    if (!mapAsset) entity.RefCount++;
#endif
                    entity.LoadStatus = eLoadStatus.Load;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
                    entity.Timestamp = _timestamp;
                    entity.Mark = false;
                    entity.Key = typekey;
                    _entities[typekey] = entity;
#endif

                    try
                    {
                        entity.Handle = Addressables.LoadAssetAsync<T>(path_replace);
                        // 読み込み完了まで待機
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        entity.Handle.WaitForCompletion();

                        entity.Key = typekey;
#else
                        await entity.Handle.Task;
#endif
                        entity.LoadStatus = eLoadStatus.Ready;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        _entities[typekey] = entity;
#else
#endif

                        return (T) entity.Handle.Result;
                    }
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    catch (Exception)
#else
                    catch (Exception e)
#endif
                    {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
                        Debug.LogException(e);
#endif
                        // 読み込めない場合はdefault
                        return default;
                    }
                }
            }

            public static async Task<bool> CheckResourceExistence(string path) {
                var path_replace = path.Replace("\\", "/");

                // 親ディレクトリを含めたパスを取得
                int start = path_replace.LastIndexOf("/", path_replace.LastIndexOf("/") - 1) + 1;
                path_replace = path.Substring(start);

                // 拡張子がjsonだった場合はassetに変換
                if (System.IO.Path.GetExtension(path_replace) == ".json")
                {
                    var dir = System.IO.Path.GetDirectoryName(path_replace);
                    var name = System.IO.Path.GetFileNameWithoutExtension(path_replace);
                    dir = dir.Replace("JSON", "SO");
                    path_replace = dir + "/" + name + ".asset";
                }

                if (path_replace == "") return default;

                // path_replace の存在有無を返却
                AsyncOperationHandle<IList<IResourceLocation>> checkHandle = Addressables.LoadResourceLocationsAsync(path_replace);
                await checkHandle.Task;
                if (checkHandle.Status == AsyncOperationStatus.Succeeded && checkHandle.Result.Count > 0)
                {
                    return true;
                }
                return false;
            }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            public static bool CheckResourceExistenceSync(string path) {
                var path_replace = path.Replace("\\", "/");

                // 親ディレクトリを含めたパスを取得
                int start = path_replace.LastIndexOf("/", path_replace.LastIndexOf("/") - 1) + 1;
                path_replace = path.Substring(start);

                // 拡張子がjsonだった場合はassetに変換
                if (System.IO.Path.GetExtension(path_replace) == ".json")
                {
                    var dir = System.IO.Path.GetDirectoryName(path_replace);
                    var name = System.IO.Path.GetFileNameWithoutExtension(path_replace);
                    dir = dir.Replace("JSON", "SO");
                    path_replace = dir + "/" + name + ".asset";
                }

                if (path_replace == "") return default;

                // path_replace の存在有無を返却
                AsyncOperationHandle<IList<IResourceLocation>> checkHandle = Addressables.LoadResourceLocationsAsync(path_replace);
                checkHandle.WaitForCompletion();
                if (checkHandle.Status == AsyncOperationStatus.Succeeded && checkHandle.Result.Count > 0)
                {
                    return true;
                }
                return false;
            }
#endif

            private enum eLoadStatus
            {
                None = 0,
                Load,
                Ready
            }

            private class AssetEntity
            {
                /// <summary>Addressables のロードハンドラ</summary>
                public AsyncOperationHandle Handle;

                /// <summary>アドレス、ラベル</summary>
                public string Key;

                /// <summary>ロード中にキャンセル指示が来たら true</summary>
                public bool LoadCancel;

                /// <summary>ロード状況</summary>
                public eLoadStatus LoadStatus = eLoadStatus.None;

                /// <summary>参照カウンタ</summary>
                public int RefCount;
#if UNITY_WEBGL
                /// <summary>最後に参照したタイムスタンプ（サイクリック）</summary>
                public uint Timestamp;
                /// <summary>
                /// Release用のMark
                /// </summary>
                public bool Mark;
#endif
            }
        }
#if UNITY_EDITOR // エディタのみ
        //====================================================================================================
        // アセットパス初回追加クラス
        //====================================================================================================
        public class RefreshAssetPath
        {
            [InitializeOnLoadMethod]
            private static void Initialize() {
                // ビルドボタンの押下
                BuildPlayerWindow.RegisterBuildPlayerHandler(BuildPlayerHandler);
            }

            private static void BuildPlayerHandler(BuildPlayerOptions options) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
                _ = BuildPlayerHandlerAsync(options);
            }

            private static async Task BuildPlayerHandlerAsync(BuildPlayerOptions options) {
#endif
#if USE_PARTIAL_LOOP
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                UpdateLoopInfo();
#else
                await UpdateLoopInfo();
#endif
#endif
                // json→so変換
                ScriptableObjectOperator.CreateSO();
                UnityEditorWrapper.AssetDatabaseWrapper.Refresh2();

                // AssetDatabaseを一時停止
                AssetDatabase.StartAssetEditing();

                // BuildSettingの設定
                BuildSetting();

                // フォルダが存在しない
                if (!Directory.Exists("Assets/AddressableAssetsData"))
                {
                    //新規作成する
                    var settings = AddressableAssetSettings.Create("Assets/AddressableAssetsData",
                        "AddressableAssetSettings", true, true);
                    AddressableAssetSettingsDefaultObject.Settings = settings;
                    Path.RefreshAssetPath(settings, true);
                    UnityEditorWrapper.AssetDatabaseWrapper.Refresh();
                }
                else
                {
                    //すでに存在する設定ファイルを読み込み
                    var settings = AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>("Assets/AddressableAssetsData/AddressableAssetSettings.asset");
                    AddressableAssetSettingsDefaultObject.Settings = settings;
                    Path.RefreshAssetPath(settings, false);
                    UnityEditorWrapper.AssetDatabaseWrapper.Refresh();
                }

                // AssetDatabaseを再開
                AssetDatabase.StopAssetEditing();

                // コンテンツをビルド
                AddressableAssetSettings.BuildPlayerContent();
                // Playerをビルド
                BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
            }

            private static void BuildSetting() {
                // 登録先のパス
                var pathList = new List<string>
                {
                    "Assets/RPGMaker/Codebase/Runtime/Title/Title.unity",
                    "Assets/RPGMaker/Codebase/Runtime/Map/SceneMap.unity",
                    "Assets/RPGMaker/Codebase/Runtime/Battle/Battle.unity",
                    "Assets/RPGMaker/Codebase/Runtime/GameOver/GameOver.unity"
                };

                // 登録されていないければ登録
                var scenes = EditorBuildSettings.scenes.ToList();
                var isChange = false;
                for (var i = 0; i < pathList.Count; i++)
                {
                    var isFind = false;

                    for (var i2 = 0; i2 < scenes.Count(); i2++)
                        if (scenes[i2].path == pathList[i])
                        {
                            isFind = true;
                            break;
                        }

                    if (isFind == false)
                    {
                        var scene = new EditorBuildSettingsScene(pathList[i], true);
                        scenes.Add(scene);
                        isChange = true;
                    }
                }

                // 更新があれば追加
                if (isChange)
                    EditorBuildSettings.scenes = scenes.ToArray();
            }

#if USE_PARTIAL_LOOP
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            private static void UpdateLoopInfo() {
#else
            private static async Task UpdateLoopInfo() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var soundManager = SoundManager.Self();
                for (int i = 0; i < 2; i++)
                {
                    soundManager.LoadLoopInfo(i);
                }
#else
                await SoundManager.InitLoopInfo();
#endif
            }
#endif
        }

        //====================================================================================================
        // アセットパス操作クラス
        //====================================================================================================
        public class Path
        {
            // 追加するパスのタイプ
            public enum AssetType
            {
                ANIMATIONS = 0,
                BATTLEBACK = 1,
                CHARACTERS = 2,
                OBJECTS = 3,
                ENEMIES = 4,
                MAPS = 5,
                TITLES = 6,
                MOVIES = 7,
                SOUNDS = 8,
                NONE = 9
            }

            // 追加するタイプに応じたグループ名
            private static readonly string[] AssetPath =
            {
                "animations",
                "battleback",
                "characters",
                "objects",
                "enemies",
                "maps",
                "titles",
                "movies",
                "sounds",
                "others"
            };

            private static AddressableAssetSettings _settings; // アセットの設定

            // AddressableAssetSettings を取得する
            private static AddressableAssetSettings GetSettings() {
                if (_settings != null) return _settings;

                var guidList = AssetDatabase.FindAssets("t:AddressableAssetSettings");

                var guid = guidList.FirstOrDefault();
                var path = AssetDatabase.GUIDToAssetPath(guid);

                var settings = AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(path);

                _settings = settings;

                return settings;
            }

            // アセットパスの再取得を行う（初回のみ）
            public static void RefreshAssetPath(AddressableAssetSettings settings, bool isFirstCreate = true) {
                _settings = settings;
                var groups = _settings.groups;

                UnityEditorWrapper.AssetDatabaseWrapper.Refresh();

                AssetDatabase.StartAssetEditing();

                // データ確認先
                string[] path =
                {
                        "Assets/RPGMaker/Storage/Animation",
                        "Assets/RPGMaker/Storage/Images/Background",
                        "Assets/RPGMaker/Storage/Images/Characters",
                        "Assets/RPGMaker/Storage/Images/Objects",
                        "Assets/RPGMaker/Storage/Images/Enemy",
                        "Assets/RPGMaker/Storage/Images/Map",
                        "Assets/RPGMaker/Storage/Images/Titles1",
                        "Assets/RPGMaker/Storage/Movies",
                        "Assets/RPGMaker/Storage/Sounds"
                    };

                // データ拡張子
                string[] extension =
                {
                        "*.prefab",
                        "*.png",
                        "*.png",
                        "*.png",
                        "*.png",
                        "*.png",
                        "*.png",
                        "*.mp4",
                        "*.ogg"
                    };

                for (var i = 0; i < path.Length; i++)
                    try
                    {
#if !UNITY_WEBGL
#else
                        if (!Directory.Exists(path[i]))
                        {
                            Debug.LogWarning($"{path[i]} not found");
                            continue;
                        }
#endif
                        // ディレクトリ内のファイル全取得
                        var data_path = Directory.GetFiles(path[i], extension[i],
                            SearchOption.AllDirectories);

#if !UNITY_WEBGL
                        if ((AssetType) Enum.ToObject(typeof(AssetType), i) == AssetType.MOVIES)
                        {
                            //StreamingAssets配下の*.mp4ファイルを削除する。
                            foreach (var file in Directory.GetFiles("Assets/StreamingAssets", "*.mp4", SearchOption.TopDirectoryOnly))
                            {
                                AssetDatabase.DeleteAsset(file);
                            }
                        }
#else
                        if ((AssetType) Enum.ToObject(typeof(AssetType), i) == AssetType.MOVIES) {
                            //StreamingAssetsにコピーする。
                            for (var i2 = 0; i2 < data_path.Length; i2++)
                            {
                                AssetDatabase.CopyAsset(data_path[i2], $"Assets/StreamingAssets{data_path[i2].Substring(path[i].Length)}");
                                data_path[i2] = data_path[i2].Replace("\\", "/");
                                UnsetAddressOfAsset(data_path[i2], (AssetType) Enum.ToObject(typeof(AssetType), i));
                            }
                            DeleteAssetGroup(AssetPath[i]);
                        }
                        else
#endif
                        if ((AssetType) Enum.ToObject(typeof(AssetType), i) != AssetType.SOUNDS)
                        {
                            for (var i2 = 0; i2 < data_path.Length; i2++)
                            {
                                data_path[i2] = data_path[i2].Replace("\\", "/");
                                SetAddressToAsset(data_path[i2], (AssetType) Enum.ToObject(typeof(AssetType), i));
                            }
                        }
                        else
                        {
                            for (var i2 = 0; i2 < data_path.Length; i2++)
                            {
                                data_path[i2] = data_path[i2].Replace("\\", "/");
                                string[] data_path_split = data_path[i2].Split("/");
                                SetAddressToAsset(data_path[i2], (AssetType) Enum.ToObject(typeof(AssetType), i), data_path_split[data_path_split.Length - 2], "ogg");
                            }

                            data_path = Directory.GetFiles(path[i], "*.wav",
                                SearchOption.AllDirectories);
                            for (var i2 = 0; i2 < data_path.Length; i2++)
                            {
                                data_path[i2] = data_path[i2].Replace("\\", "/");
                                string[] data_path_split = data_path[i2].Split("/");
                                SetAddressToAsset(data_path[i2], (AssetType) Enum.ToObject(typeof(AssetType), i), data_path_split[data_path_split.Length - 2], "wav");
                            }

#if USE_PARTIAL_LOOP
                            data_path = Directory.GetFiles(path[i], "*.asset",
                                SearchOption.AllDirectories);
                            for (var i2 = 0; i2 < data_path.Length; i2++)
                            {
                                data_path[i2] = data_path[i2].Replace("\\", "/");
                                string[] data_path_split = data_path[i2].Split("/");
                                SetAddressToAsset(data_path[i2], (AssetType) Enum.ToObject(typeof(AssetType), i), data_path_split[data_path_split.Length - 2], "asset");
                            }
#endif
                        }
                    }
#if !UNITY_WEBGL
                    catch (IOException)
#else
                    catch (IOException e)
#endif
                    {
#if !UNITY_WEBGL
#else
                        Debug.LogException(e);
#endif
                    }

                //-------------------------------------------
                // 別で初期時に追加したいアセット
                // データ確認先
                string[] pathAdd =
                {
                        "Assets/RPGMaker/Storage", // so
                        "Assets/RPGMaker/Storage/Images/System/Balloon", // Images系
                        "Assets/RPGMaker/Storage/Images/Faces", // Images系
                        "Assets/RPGMaker/Storage/Images/Parallaxes", // Images系
                        "Assets/RPGMaker/Storage/Images/Pictures", // Images系
                        "Assets/RPGMaker/Storage/Images/SV_Actors", // Images系
                        "Assets/RPGMaker/Storage/Images/SV_Enemy", // Images系
                        "Assets/RPGMaker/Storage/Images/System", // Images系
                        "Assets/RPGMaker/Storage/Images/Titles2", // Images系
                        "Assets/RPGMaker/Storage/Images/Ui", // Images系
                        "Assets/RPGMaker/Storage/Map/TileAssets", // タイルアセット
                        "Assets/RPGMaker/Storage/Map/SavedMaps", // マップprefab
                        "Assets/RPGMaker/Storage/Animation/Prefab", // アニメーションデータ
                        "Assets/RPGMaker/Storage/Animation/Effekseer", // アニメーションデータ
                        "Assets/RPGMaker/Codebase/Runtime/Map/Asset/Prefab", //Eventで使用する部品
                        "Assets/RPGMaker/Codebase/Runtime/Battle", // 戦闘用のwindow 
                        "Assets/RPGMaker/Codebase/Runtime/Map/Shop/Asset/Prefab", // ショップ
                        "Assets/RPGMaker/Codebase/Runtime/Map/Minimap", // ミニマップ
                        "Assets/RPGMaker/Codebase/Runtime/Map/Minimap/Frame", // ミニマップ飾り
                        "Assets/RPGMaker/Codebase/Runtime/Map", // material
                        "Assets/RPGMaker/Storage/AssetManage", // AssetManager 
                    };

                // データ拡張子
                string[] extensionAdd =
                {
                        "*.asset",
                        "*.png",
                        "*.png",
                        "*.png",
                        "*.png",
                        "*.png",
                        "*.png",
                        "*.png",
                        "*.png",
                        "*.png",
                        "*.asset",
                        "*.prefab",
                        "*.prefab",
                        "*.asset",
                        "*.prefab",
                        "*.prefab",
                        "*.prefab",
                        "*.asset",
                        "*.png",
                        "*.mat",
                        "*.asset",
                    };

                // 追加
                for (var i = 0; i < pathAdd.Length; i++)
                {
                    try
                    {
                        // soのみ別処理
                        if (i == 0)
                        {
                            var data_path = Directory.GetDirectories(pathAdd[i], "SO",
                                SearchOption.AllDirectories);

                            for (var i2 = 0; i2 < data_path.Length; i2++)
                            {
                                var asset_path = Directory.GetFiles(data_path[i2], extensionAdd[i],
                                    SearchOption.AllDirectories);

                                for (var i3 = 0; i3 < asset_path.Length; i3++)
                                {
                                    asset_path[i3] = asset_path[i3].Replace("\\", "/");
                                    SetAddressToAsset(asset_path[i3]);
                                }
                            }
                        }
                        else
                        {
                            // ディレクトリ内のファイル全取得
                            var data_path = Directory.GetFiles(pathAdd[i], extensionAdd[i],
                                SearchOption.AllDirectories);
                            for (var i2 = 0; i2 < data_path.Length; i2++)
                            {
                                data_path[i2] = data_path[i2].Replace("\\", "/");
                                SetAddressToAsset(data_path[i2]);
                            }
                        }
                    }
                    catch (IOException)
                    {
                    }
                }

                // 最後にグループを追加
                if (isFirstCreate)
                    GetOrCreateGroup("Asset_end");

                AssetDatabase.StopAssetEditing();
            }

            // 指定された名前のグループを取得もしくは作成します
            private static AddressableAssetGroup GetOrCreateGroup(string groupName) {
                var settings = GetSettings();
                var groups = settings.groups;
                AddressableAssetGroup group = null;
                for (int i = 0; i < groups.Count; i++)
                    if (groups[i].name == groupName)
                    {
                        group = groups[i];
                        break;
                    }

                // 既に指定された名前のグループが存在する場合は
                // そのグループを返します
                if (group != null) return group;

                // Content Packing & Loading
                var bunlAssetGroupSchema = ScriptableObject.CreateInstance<BundledAssetGroupSchema>();
                bunlAssetGroupSchema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;

                // Content Update Restriction
                var contentUpdateGroupSchema = ScriptableObject.CreateInstance<ContentUpdateGroupSchema>();

                // AddressableAssetGroup の Inspector に表示されている Schema
                var schemas = new List<AddressableAssetGroupSchema>
                {
                    bunlAssetGroupSchema,
                    contentUpdateGroupSchema
                };

                // 指定された名前のグループを作成して返します
                return settings.CreateGroup(groupName, false, false,
                    true, schemas);
            }

            // 指定されたアセットにアドレスを割り当ててグループに追加します
            public static void SetAddressToAsset(
                string path,
                AssetType assetType = AssetType.NONE,
                string pathName = null,
                string extention = null
            ) {
                // フォルダが存在しない
                if (!Directory.Exists("Assets/AddressableAssetsData"))
                    return;

                // 親ディレクトリを含めたパスを取得
                int start = path.LastIndexOf("/", path.LastIndexOf("/") - 1) + 1 ;
                pathName = path.Substring(start);

                AddressableAssetGroup targetParent;
                targetParent = GetOrCreateGroup(AssetPath[(int) assetType]);

                SetAddressToAsset(path, pathName, targetParent);
            }

            // 指定されたアセットにアドレスを割り当ててグループに追加します
            private static void SetAddressToAsset(string path, string pathName, AddressableAssetGroup targetParent) {
                var settings = GetSettings();
                var guid = AssetDatabase.AssetPathToGUID(path);
                if (guid == "")
                {
                    AssetDatabase.Refresh();
                    guid = AssetDatabase.AssetPathToGUID(path);
                }

                var entry = settings.FindAssetEntry(guid);

                if (entry != null)
                    // すでに同じアドレスかグループが設定されている場合処理をスキップします
                    if (entry.address == pathName || entry.parentGroup == targetParent)
                        return;

                // アセットをグループに追加します
                entry = settings.CreateOrMoveEntry(guid, targetParent);

                if (entry == null)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("[AddressableUtils] Failed AddressableAssetSettings.CreateOrMoveEntry.");
                    sb.AppendLine($"path: {path}");
                    sb.AppendLine($"address: {pathName}");
                    sb.AppendLine($"targetParent: {targetParent.Name}");
                    return;
                }

                // アセットにアドレスを割り当てます
                entry.SetAddress(pathName);
            }
#if !UNITY_WEBGL
#else

            //指定されたアセットのグループを削除します。
            public static void DeleteAssetGroup(string groupName) {
                var settings = GetSettings();
                var groups = settings.groups;
                AddressableAssetGroup group = null;
                for (int i = 0; i < groups.Count; i++)
                    if (groups[i].name == groupName)
                    {
                        group = groups[i];
                        break;
                    }

                if (group != null)
                {
                    settings.RemoveGroup(group);
                }
            }
            // 指定されたアセットのアドレスをグループから削除します
            public static void UnsetAddressOfAsset(
                string path,
                AssetType assetType = AssetType.NONE,
                string pathName = null,
                string extention = null
            ) {
                // フォルダが存在しない
                if (!Directory.Exists("Assets/AddressableAssetsData"))
                    return;

                // 親ディレクトリを含めたパスを取得
                int start = path.LastIndexOf("/", path.LastIndexOf("/") - 1) + 1;
                pathName = path.Substring(start);

                AddressableAssetGroup targetParent;
                targetParent = GetOrCreateGroup(AssetPath[(int) assetType]);

                var settings = GetSettings();
                var guid = AssetDatabase.AssetPathToGUID(path);
                if (guid == "")
                {
                    AssetDatabase.Refresh();
                    guid = AssetDatabase.AssetPathToGUID(path);
                }

                var entry = settings.FindAssetEntry(guid);

                if (entry != null)
                {
                    settings.RemoveAssetEntry(guid);
                }
            }
#endif

            // アセットバンドルをビルドする
            public static void Build() {
                AddressableAssetSettings.BuildPlayerContent();
            }
        }
#endif
    }

    public class CoroutineAccessor : MonoBehaviour
    {
        /// <summary>
        ///     StartCoroutine
        /// </summary>
        public static Coroutine Start(IEnumerator routine) {
#if USING_UniRx
            return UniRx.MainThreadDispatcher.StartCoroutine(routine);
#else
            return Instance.StartCoroutine(routine);
#endif
        }

        /// <summary>
        ///     StopCoroutine
        /// </summary>
        public static void Stop(Coroutine coroutine) {
            Instance.StopCoroutine(coroutine);
        }

        /// <summary>
        ///     StopCoroutine Reference is cleard by null.
        /// </summary>
        public static void StopNull(ref Coroutine coroutine) {
            Stop(coroutine);
            coroutine = null;
        }
#if !USING_UniRx
        /// <summary>
        ///     GameObject アタッチなしで StartCoroutine を使うための instance
        /// </summary>
        private static CoroutineAccessor Instance
        {
            get
            {
                if (instance == null)
                {
                    var obj = new GameObject(nameof(CoroutineAccessor));
                    DontDestroyOnLoad(obj);
                    instance = obj.AddComponent<CoroutineAccessor>();
                }

                return instance;
            }
        }

        private static CoroutineAccessor instance;
        /// <summary>
        ///     OnDisable
        /// </summary>
        private void OnDisable() {
            if (instance != null)
            {
                Destroy(instance.gameObject);
                instance = null;
            }
        }
#endif
    }

    public class AddressableExt
    {
        public string folderName;
        public string extention;
    }
}