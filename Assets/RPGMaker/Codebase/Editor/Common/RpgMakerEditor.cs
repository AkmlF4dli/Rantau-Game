using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Common;
using RPGMaker.Codebase.CoreSystem.Knowledge.Enum;
using RPGMaker.Codebase.CoreSystem.Lib.Auth;
using RPGMaker.Codebase.CoreSystem.Lib.Migration;
using RPGMaker.Codebase.CoreSystem.Service.EventManagement;
using RPGMaker.Codebase.CoreSystem.Service.MapManagement;
using RPGMaker.Codebase.Editor.Common.View;
using RPGMaker.Codebase.Editor.Common.Window;
using RPGMaker.Codebase.Editor.Hierarchy.Region.Base.View;
using RPGMaker.Codebase.Editor.Hierarchy.Region.CommonEvent.View;
using RPGMaker.Codebase.Editor.MapEditor.Window.EventEdit;
using RPGMaker.Codebase.Runtime.Addon;
using RPGMaker.Codebase.Runtime.Common;
using RPGMaker.Codebase.Runtime.Title;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGMaker.Codebase.Editor.Common
{
    public class RpgMakerEditorParam : ScriptableSingleton<RpgMakerEditorParam>
    {
        public RpgMakerEditor RpgMakerEditor;
        public string ActiveBuildSetting;
        public bool IsWindowInitialized;

        public void SetEditor(RpgMakerEditor editor) {
            IsWindowInitialized = false;
            RpgMakerEditor = editor;
        }
    }

    public class RpgMakerEditor
    {
        private static readonly string LocalVersionCodePath = Directory.GetCurrentDirectory() + "/Packages/jp.ggg.rpgmaker.unite/versioncode.txt";

        private bool IsNowVersionUpProgress() {
            return File.Exists(LocalVersionCodePath);
        }

        static readonly string WLT_UNITE_FOCUS_LAYOUT = "Assets/RPGMaker/Codebase/Editor/Layouts/UniteBaseLayout.wlt";
        static readonly string WLT_UNITE_WITH_UNITY_LAYOUT = "Assets/RPGMaker/Codebase/Editor/Layouts/UniteWithUnityLayout.wlt";
        static readonly string WLT_UNITY_ONLY_LAYOUT = "Assets/RPGMaker/Codebase/Editor/Layouts/DatabaseLayout.wlt";

        //----------------------------------------------------------------------------------------------------------------------------------
        //
        // properties / consts
        //
        //----------------------------------------------------------------------------------------------------------------------------------
        internal const string RpgMakerUniteMenuItemPath = "RPG Maker/Layout/RPG Maker Focused Mode";
        internal const string UnityEditorMenuItemPath = "RPG Maker/Layout/Unity Editor";
        internal const string RpgMakerUniteWindowMenuItemPath = "RPG Maker/Layout/RPG Maker+Unity Editor";
        internal const string SettingWindowMenuItemPath = "RPG Maker/Revert Layout";
        internal const string EventCommandSettingWindowMenuItemPath = "RPG Maker/EventCommand Mode";
        internal const string EventCommandSettingNormalWindowMenuItemPath = "RPG Maker/EventCommand/NormalMode";
        internal const string EventCommandSettingButtonListWindowMenuItemPath = "RPG Maker/EventCommand/ButtonMode";

        private static MenuWindow _menuWindow;
        private static RPGMakerDefaultConfigSingleton.INITIALIZE_STATE _initializeState;
        private static bool IsStartRPGMakerProgress;
        private static bool IsMenuProgress;
        public static bool IsImportEffekseer = false;

        public static bool IsBattleTestPlay = false;

        /// <summary>
        /// 煩雑なのでInstanceを持ってくるプロパティを設置…
        /// </summary>
        private static RpgMakerEditor Instance => RpgMakerEditorParam.instance.RpgMakerEditor;

        static string _lastActiveMapEvent;

        //----------------------------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        //----------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------
        // 起動時の初期化処理
        //----------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Unity立ち上げ時の処理
        /// </summary>
        [InitializeOnLoadMethod]
        public static void InitializeOnLoad() {

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
            // Already initialized, skip the work
            if (RpgMakerEditorParam.instance.RpgMakerEditor != null)
            {
                return;
            }
            if (IsStartRPGMakerProgress)
            {
                return;
            }
            IsStartRPGMakerProgress = true;

            // VSP実装の削除対応
            var vspPath = Path.Combine(Application.dataPath, "..", "Packages", "com.unity.vsp.rpgmaker");
            if (Directory.Exists(vspPath))
            {
                Directory.Delete(vspPath, true);
            }

            AnalyticsManager.PostEvent(
                AnalyticsManager.EventName.action,
                AnalyticsManager.EventParameter.initialize);
            AddonManager.Instance.Refresh();



            var editor = new RpgMakerEditor();
            RpgMakerEditorParam.instance.SetEditor(editor);
            _ = editor.StartRpgMaker();
        }

        //----------------------------------------------------------------------------------------------------------------------------------
        // LayoutMenu
        //----------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// RPG Maker Uniteを開く
        /// </summary>
        [MenuItem(RpgMakerUniteMenuItemPath, priority = 100)]
        private static void RpgMakerUniteMenu() {
            // If first initialization is necessary, do it in here:
            if (IsMenuProgress ||
                IsStartRPGMakerProgress ||
                RPGMakerDefaultConfigSingleton.instance.UniteMode == RPGMakerDefaultConfigSingleton.RpgMakerUniteModeId)
            {
                Debug.Log("IsMenuProgress" + IsMenuProgress);
                Debug.Log("IsStartRPGMakerProgress" + IsStartRPGMakerProgress);
                Debug.Log("UniteMode" + RPGMakerDefaultConfigSingleton.instance.UniteMode);

                return;
            }
            IsMenuProgress = true;
            Instance?.LayoutUpdate_UniteFocusLayout();
            IsMenuProgress = false;
        }

        /// <summary>
        /// RPG Maker Unite Window (開発用)を開く
        /// </summary>
        [MenuItem(RpgMakerUniteWindowMenuItemPath, priority = 100)]
        private static void RpgMakerUniteWindow() {
            if (IsMenuProgress ||
                IsStartRPGMakerProgress ||
                RPGMakerDefaultConfigSingleton.instance.UniteMode == RPGMakerDefaultConfigSingleton.RpgMakerUniteWindowModeId)
            {
                return;
            }
            IsMenuProgress = true;
            Instance?.LayoutUpdate_UniteAndUnity_Layout();
            IsMenuProgress = false;
        }

        /// <summary>
        /// Unity Editor。
        /// </summary>
        [MenuItem(UnityEditorMenuItemPath, priority = 100)]
        private static void UnityEditorMenu() {
            if (IsMenuProgress ||
                IsStartRPGMakerProgress ||
                RPGMakerDefaultConfigSingleton.instance.UniteMode == RPGMakerDefaultConfigSingleton.DefaultEditorModeId)
            {
                return;
            }
            IsMenuProgress = true;
            Instance?.LayoutUpdate_UnityOnly_Layout();
            IsMenuProgress = false;
        }

        /// <summary>
        /// メニュー項目のチェック表示/非表示設定。
        /// </summary>
        private static void UpdateEditorModeMenuChecked() {
            DebugUtil.Log($"RPGMakerDefaultConfigSingleton.instance.UniteMode={RPGMakerDefaultConfigSingleton.instance.UniteMode}");
            Menu.SetChecked(RpgMakerUniteMenuItemPath, RPGMakerDefaultConfigSingleton.instance.UniteMode == RPGMakerDefaultConfigSingleton.RpgMakerUniteModeId);
            Menu.SetChecked(UnityEditorMenuItemPath, RPGMakerDefaultConfigSingleton.instance.UniteMode == RPGMakerDefaultConfigSingleton.DefaultEditorModeId);
            Menu.SetChecked(RpgMakerUniteWindowMenuItemPath, RPGMakerDefaultConfigSingleton.instance.UniteMode == RPGMakerDefaultConfigSingleton.RpgMakerUniteWindowModeId);
            Menu.SetChecked(SettingWindowMenuItemPath, RPGMakerDefaultConfigSingleton.instance.RevertLayoutSetting);
        }

        /// <summary>
        /// レイアウト初期化設定
        /// </summary>
        [MenuItem(SettingWindowMenuItemPath, priority = 101)]
        private static void SettingMenu() {
            RPGMakerDefaultConfigSingleton.instance.RevertLayoutSetting =
                !RPGMakerDefaultConfigSingleton.instance.RevertLayoutSetting;
            Menu.SetChecked(SettingWindowMenuItemPath, RPGMakerDefaultConfigSingleton.instance.RevertLayoutSetting);
        }

        /// <summary>
        /// イベントコマンドの表示選択
        /// </summary>
        [MenuItem(EventCommandSettingNormalWindowMenuItemPath, priority = 102)]
        private static void EventCommandSettingNormalMenu() {
            RPGMakerDefaultConfigSingleton.instance.EventComanedMod = 0;
            UpdateEventComandMenuCheck();
        }
        [MenuItem(EventCommandSettingButtonListWindowMenuItemPath, priority = 102)]
        private static void EventCommandSettingButtonListMenu() {
            RPGMakerDefaultConfigSingleton.instance.EventComanedMod = 1;
            UpdateEventComandMenuCheck();
        }

        //----------------------------------------------------------------------------------------------------------------------------------
        // instance section
        //----------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Unity立ち上げ直後だと、Systemのエラーになるため、asyncで実行
        /// </summary>
        /// <returns></returns>
        private async Task StartRpgMaker() {

            // 初期処理（再起動する場合があります）
            _initializeState = RPGMakerDefaultConfigSingleton.InitializeDefaultSettingsForRPGMakerUnite();

            // 初期化未実施の場合は、後続の処理を行わない
            if (_initializeState == RPGMakerDefaultConfigSingleton.INITIALIZE_STATE.UNINITIALIZED)
            {
                IsStartRPGMakerProgress = false;
            }
            else if (_initializeState == RPGMakerDefaultConfigSingleton.INITIALIZE_STATE.INITIALIZED)
            {
                // システム言語更新
                EditorLocalize.TrySetSystemLanguage(RPGMakerDefaultConfigSingleton.instance.UniteEditorLanguage);

                // バージョンアップ処理中の場合には、バージョンアップ処理完了まで待つ
                while (IsNowVersionUpProgress())
                {
                    await Task.Delay(5000);
                }

                if (!Auth.IsAuthenticated)
                {
                    while (!Auth.IsAuthenticated)
                    {
                        await LoginWindow.OpenAsync();
                        // ウィンドウクローズ直後の再呼び出しは処理が戻らなくなる可能性があるため少し待機
                        await Task.Delay(1000);
                    }
                }

                // Asmdefの更新処理
                UpdateASMDEFReference();

                // 認証通過後に呼ぶこと
                MigrationCore.Migrate();

                // 通知の実行
                EditorNotify.NoticeAsync();

                // 再コンパイル時などでのレイアウト更新前にdelayを入れないとUnityEditor.SceneHierarchyWindow.OnDisable等の実行中に処理が走りエラーが出る
                // 回避には最初からDelayCallにUI初期化を置くなどの改修が必要
                await Task.Delay(1);

                // レイアウト更新
                if (RPGMakerDefaultConfigSingleton.instance.RevertLayoutSetting || RPGMakerDefaultConfigSingleton.instance.UniteMode == RPGMakerDefaultConfigSingleton.InitializeModeId)
                {
                    // 初期レイアウト適応
                    switch (RPGMakerDefaultConfigSingleton.instance.UniteMode)
                    {
                        case RPGMakerDefaultConfigSingleton.RpgMakerUniteModeId:
                        case RPGMakerDefaultConfigSingleton.InitializeModeId:
                            Instance?.LayoutUpdate_UniteFocusLayout();
                            break;
                        case RPGMakerDefaultConfigSingleton.RpgMakerUniteWindowModeId:
                            Instance?.LayoutUpdate_UniteAndUnity_Layout();
                            break;
                        default:
                            //IsStartRPGMakerProgress = false;
                            //NoticeAsync();
                            break;
                    }
                }
                else
                {
                    // レイアウトを変更しない場合も明示的再描画
                    InitWindows();
                }

                UpdateEditorModeMenuChecked();
                UpdateLanguageMenuCheckMark(EditorLocalize.GetNowLanguage());

                IsStartRPGMakerProgress = false;
                IsMenuProgress = false;

                UpdateEventComandMenuCheck();

                //U261 アイテムデータの効果データ、その他のスキル、コモンイベントの変換Indexから各UIDを保存する
                //起動時実行
                ItemDataOtherEffectsIdtoUid();
            }
            else
            {
                // 設定を行った後、再起動を行う
                RebootUnityEditor();
            }
            // ステート変更時処理の登録
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetModeId"></param>
        private void ApplyEditorMode(string targetModeId) {
            ModeService.ChangeModeById(targetModeId);
            RPGMakerDefaultConfigSingleton.instance.UniteMode = targetModeId;
            UpdateEditorModeMenuChecked();
        }

        /// <summary>
        /// 
        /// </summary>
        void LayoutUpdate_UniteFocusLayout() {
            // モードを保存
            ApplyEditorMode(RPGMakerDefaultConfigSingleton.RpgMakerUniteModeId);
            // rpgmaker.mode内でも指定しているが、適切に適用されないようなので、ここで適用する。
            LayoutUtility.LoadLayout(WLT_UNITE_FOCUS_LAYOUT);
            // レイアウトを変更した場合は明示的に再初期化
            InitWindows();
            // 初期シーンを開く
            EditorSceneManager.OpenScene("Assets/RPGMaker/Codebase/Runtime/Title/Title.unity");
            // 保存
            EditorApplication.ExecuteMenuItem("File/Save");
        }

        /// <summary>
        /// 
        /// </summary>
        void LayoutUpdate_UniteAndUnity_Layout() {
            // モードを保存
            ApplyEditorMode(RPGMakerDefaultConfigSingleton.RpgMakerUniteWindowModeId);
            LayoutUtility.LoadLayout(WLT_UNITE_WITH_UNITY_LAYOUT);
            // レイアウトを変更した場合は明示的に再初期化
            InitWindows();
        }

        /// <summary>
        /// 
        /// </summary>
        void LayoutUpdate_UnityOnly_Layout() {
            // モードを保存
            ApplyEditorMode(RPGMakerDefaultConfigSingleton.DefaultEditorModeId);
            LayoutUtility.LoadLayout(WLT_UNITY_ONLY_LAYOUT);
        }

        // Unity6向けのasmdef更新処理
        private bool UpdateASMDEFReference() {
            var guid = AssetDatabase.AssetPathToGUID("Packages/com.unity.render-pipelines.universal/Runtime/2D/Unity.RenderPipelines.Universal.2D.Runtime.asmdef");
            if (string.IsNullOrEmpty(guid))
            {
                return false;
            }

            var asmdefPath = Path.Combine(Application.dataPath, "RPGMaker/Codebase/CoreSystem/RPGMaker.CodeBase.CoreSystem.asmdef");
            var lines = File.ReadAllLines(asmdefPath).ToList();

            // 重複チェック
            if (lines.Any(l => l.Contains(guid)))
            {
                return false;
            }

            // 整形の実行
            var resultlines = new List<string>();
            lines.ForEach(line =>
            {
                // versionDefinesを差し替える
                if (line.Contains("\"versionDefines\": [],"))
                {
                    resultlines.Add("    \"versionDefines\": [");
                    resultlines.Add("        {");
                    resultlines.Add("            \"name\": \"com.unity.render-pipelines.universal\",");
                    resultlines.Add("            \"expression\": \"16.0.0\",");
                    resultlines.Add("            \"define\": \"UNITE_ASMREF_URP2D\"");
                    resultlines.Add("        }");
                    resultlines.Add("    ],");
                }
                else
                {
                    resultlines.Add(line);
                    if (line.Contains("references"))
                    {
                        // referencesの次の行に押し込む
                        resultlines.Add($"        \"GUID:{guid}\",");
                    }
                }
            });
            File.WriteAllLines(asmdefPath, resultlines);
            return true;
        }

        //----------------------------------------------------------------------------------------------------------------------------------
        // ヘルプメニュー
        //----------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Help。
        /// </summary>
        [MenuItem("RPG Maker/RPG Maker Unite Help...", priority = 990)]
        private static void HelpMenu() {
            MenuEditorView.Help();
        }

        /// <summary>
        /// About。
        /// </summary>
        [MenuItem("Help/About RPG Maker Unite...", priority = 991)]
        private static void AboutMenu() {
            MenuEditorView.About();
        }

        //----------------------------------------------------------------------------------------------------------------------------------
        // その他
        //----------------------------------------------------------------------------------------------------------------------------------
        public static void InitWindows() {
            // 現在のモードがUnityモードであれば、Windowを作成せずに終了
            if (RPGMakerDefaultConfigSingleton.instance.UniteMode == RPGMakerDefaultConfigSingleton.DefaultEditorModeId)
            {
                return;
            }

            // 共通メニューウィンドウ
            _menuWindow = WindowLayoutManager.GetOrOpenWindow(WindowLayoutManager.WindowLayoutId.MenuWindow) as MenuWindow;
            _menuWindow.titleContent = new GUIContent(EditorLocalize.LocalizeText("WORD_1560"));
            _menuWindow.Init();

            // ヒエラルキー
            //Hierarchy.Hierarchy.IsInitialized = !hierarchyForceReset;
            if (!Hierarchy.Hierarchy.Init(true))
            {
                Hierarchy.Hierarchy.SetInspector();

                //初回又は、BuildSettingsのデータが変わっていた場合に、フォントなどのUIパターンを適用しなおす
                if (RpgMakerEditorParam.instance.ActiveBuildSetting != EditorUserBuildSettings.activeBuildTarget.ToString())
                {
                    RpgMakerEditorParam.instance.ActiveBuildSetting = EditorUserBuildSettings.activeBuildTarget.ToString();
                    //フォント適用しなおしはよばない
                    //FontManager.InitializeFont();
                }
            }
            else
            {
                // データベースエディタ
                DatabaseEditor.DatabaseEditor.Init();
                // マップエディタ
                MapEditor.MapEditor.Init();

                // インスペクター開いていたもの開く
                Hierarchy.Hierarchy.SetInspector();
                RpgMakerEditorParam.instance.IsWindowInitialized = true;
                WindowLayoutManager.GetOrOpenWindow(WindowLayoutManager.WindowLayoutId.DatabaseSceneWindow);

                EraceUnitySystemMenu();
            }
        }

        static bool HasParentOfClass<T>(VisualElement element) {
            var current = element;
            while (current != null)
            {
                if (current is T)
                {
                    return true;
                }
                current = current.hierarchy.parent;
            }
            return false;
        }

        public static void UpdateLastActiveMenuEvent() {
            {
                var ve = Hierarchy.Hierarchy.GetActiveVisualElement();
                string name = null;
                if (HasParentOfClass<MapListView>(ve))
                {
                    name = ve.name;
                }
                else if (HasParentOfClass<EventListView>(ve))
                {
                    //イベント編集後、マップ編集やバトル編集をした場合、マップイベントが得られてしまうので、判定を追加。
                    var mapListView = Hierarchy.Hierarchy.GetMapListView();
                    var mode = mapListView.GetCurrentEditMode();
                    if (mode == MapListView.EditMode.EditMap || mode == MapListView.EditMode.EditBattle)
                    {
                        ve = mapListView.GetActiveMapButton();
                    }
                    name = ve.name;
                }
                else if (HasParentOfClass<CommonEventHierarchyView>(ve))
                {
                    name = ve.name;
                }
                if (name != null)
                {
                    _lastActiveMapEvent = name;
                }
            }
        }

        /// <summary>
        /// Playmodeの状態が変わった時に実行される
        /// </summary>
        private static void OnPlayModeStateChanged(PlayModeStateChange state) {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    // バトルテストの場合は元のインスペクタをリセットせずに保持
                    if (IsBattleTestPlay == false)
                    {
                        // 実行前に一部イベント系のダイアログを閉じる
                        EditorWindow.GetWindow<CommandSettingWindow>()?.Close();
                        EditorWindow.GetWindow<ExecutionContentsWindow>()?.Close();
                        EditorWindow.GetWindow<CommandWindow>()?.Close();

                        InitWindows();

                        new MapManagementService().ResetMap();
                        AssetDatabase.Refresh();
                        TitleController.IsTitleSkip = DataManager.Self().GetSystemDataModel().optionSetting.optSkipTitleScreen == 1;
                    }
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    UpdateLastActiveMenuEvent();
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    InitWindows();
                    // バトルテストの場合はバトルのインスペクタに戻す
                    if (IsBattleTestPlay == true)
                    {
                        Hierarchy.Hierarchy.SelectButton("battleSceneButton");
                    }
                    else if (_lastActiveMapEvent != null)
                    {
                        Hierarchy.Hierarchy.SelectButton(_lastActiveMapEvent, Hierarchy.Hierarchy.FirstView.MapEventListView);
                        _lastActiveMapEvent = null;
                    }
                    IsBattleTestPlay = false;
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="flg"></param>
        public static void DataCheckMenuButton(bool flg) {
            _menuWindow?.SetButtonEnabled(flg);
        }


        /// <summary>
        /// UniteFocusModeで上部のシステムメニューを消す
        /// </summary>
        private static void EraceUnitySystemMenu() {
            BindingFlags AnyBind = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            Assembly Asm = typeof(UnityEditor.Editor).Assembly;

            Type mainViewT = Asm.GetType("UnityEditor.MainView");
            var mainView = Resources.FindObjectsOfTypeAll(mainViewT)[0];

            var topViewHeight = mainViewT.GetField("m_TopViewHeight", AnyBind);
            var useTopView = mainViewT.GetField("m_UseTopView", AnyBind);
            topViewHeight.SetValue(mainView, -1);
            useTopView.SetValue(mainView, false);

            Type toolbarT = Asm.GetType("UnityEditor.Toolbar");
            var toolbar = Resources.FindObjectsOfTypeAll(toolbarT);
            if (toolbar.Length > 0)
            {
                Type viewT = mainViewT.BaseType;
                var bar = toolbar[0];
                MethodInfo removeChild = mainViewT.GetMethod("RemoveChild", AnyBind, null, new Type[] { viewT }, null);
                removeChild.Invoke(mainView, new object[] { bar });
                UnityEngine.Object.DestroyImmediate(bar);
            }

            PropertyInfo windowPosition = mainViewT.GetProperty("windowPosition", AnyBind);
            var pos = (Rect) windowPosition.GetValue(mainView);

            MethodInfo SetPosition = mainViewT.GetMethod("SetPosition", AnyBind);
            SetPosition.Invoke(mainView, new object[] { pos });
        }

        /// <summary>
        /// 言語設定メニュー関係
        /// </summary>
        private const string TraitMenu_UniteEditorLanguaeg_Japanese = "RPG Maker/UNITE Language/Japanese";
        private const string TraitMenu_UniteEditorLanguaeg_English = "RPG Maker/UNITE Language/English";
        private const string TraitMenu_UniteEditorLanguaeg_Chinese = "RPG Maker/UNITE Language/Chinese";

        /// <summary>
        /// メニュー：言語：日本語
        /// </summary>
        [MenuItem(TraitMenu_UniteEditorLanguaeg_Japanese, priority = 201)]
        static void SetLanguage_Japanese() {
            UpdateLanguageSetting(SystemLanguage.Japanese);

        }
        /// <summary>
        /// メニュー：言語：英語
        /// </summary>
        [MenuItem(TraitMenu_UniteEditorLanguaeg_English, priority = 202)]
        static void SetLanguage_English() {
            UpdateLanguageSetting(SystemLanguage.English);
        }
        /// <summary>
        /// メニュー：言語：中国語
        /// </summary>
        [MenuItem(TraitMenu_UniteEditorLanguaeg_Chinese, priority = 203)]
        static void SetLanguage_Chinese() {
            UpdateLanguageSetting(SystemLanguage.Chinese);
        }

        /// <summary>
        /// 言語切替時のレイアウト更新処理
        /// </summary>
        /// <param name="language"></param>
        static void UpdateLanguageSetting(SystemLanguage language) {
            if (EditorLocalize.TrySetSystemLanguage(language) == false)
            {
                return;
            }

            RPGMakerDefaultConfigSingleton.instance.UniteEditorLanguage = language;
            // Hierarchy再構築のため再起動
            RebootUnityEditor();
        }

        /// <summary>
        /// メニューチェック状況更新
        /// </summary>
        /// <param name="language"></param>
        static void UpdateLanguageMenuCheckMark(SystemLanguage language) {
            Menu.SetChecked(TraitMenu_UniteEditorLanguaeg_Japanese, language == SystemLanguage.Japanese);
            Menu.SetChecked(TraitMenu_UniteEditorLanguaeg_English, language == SystemLanguage.English);
            Menu.SetChecked(TraitMenu_UniteEditorLanguaeg_Chinese, language == SystemLanguage.Chinese);
        }

        /// <summary>
        /// Uniteの再起動の実行
        /// </summary>
        private static void RebootUnityEditor() {
            var restartEditorAndRecompileScripts = typeof(EditorApplication).GetMethod("RestartEditorAndRecompileScripts", BindingFlags.NonPublic | BindingFlags.Static);
            restartEditorAndRecompileScripts.Invoke(null, null);
        }

        /// <summary>
        /// イベントコマンドチェック状態の更新
        /// </summary>
        private static void UpdateEventComandMenuCheck() {
            Menu.SetChecked(EventCommandSettingNormalWindowMenuItemPath, RPGMakerDefaultConfigSingleton.instance.EventComanedMod == 0);
            Menu.SetChecked(EventCommandSettingButtonListWindowMenuItemPath, RPGMakerDefaultConfigSingleton.instance.EventComanedMod == 1);
        }

        /// <summary>
        /// アイテムデータの効果データ、その他のスキル、コモンイベントの変換
        /// Indexから各UIDを保存する
        /// </summary>
        private static void ItemDataOtherEffectsIdtoUid() {
            //アイテムデータの全ロード
            var ItemDatas = Editor.Hierarchy.Hierarchy.databaseManagementService.LoadItem();
            //
            foreach (var itemData in ItemDatas)
            {
                //使用者
                if (itemData.userEffect.otherEffects.Count != 0)
                {
                    OtherEffectsIdtoUid( itemData.userEffect.otherEffects);
                }
                //ターゲット
                if (itemData.targetEffect.otherEffects.Count != 0)
                {
                    OtherEffectsIdtoUid( itemData.targetEffect.otherEffects);
                }
            }
        }

        /// <summary>
        /// その他の効果Indexから各UIDを保存する
        /// </summary>
        /// <param name="otherEffects"></param>
        private static void OtherEffectsIdtoUid(List<TraitCommonDataModel> otherEffects) {
            //スキルデータ
            var skillDataModels = Editor.Hierarchy.Hierarchy.databaseManagementService.LoadSkillCustom();
            //コモンイベント
            var ecategoryBlockntCommonDataModels = new EventManagementService().LoadEventCommon();

            //UIDリスト保存用
            var skillUidList = new List<string>();
            foreach (var skilData in skillDataModels)
            {
                //変換時に1 2 3は追加しない(基本スキルは除外)
                if (skilData.basic.id != "1" && skilData.basic.id != "2" && skilData.basic.id != "3")
                {
                    skillUidList.Add(skilData.basic.id);
                }
            }
            var commonuidList = new List<string>();
            foreach (var c in ecategoryBlockntCommonDataModels)commonuidList.Add(c.eventId);

            //データ変換
            foreach (var otherEffect in otherEffects)
            {
                if ( ( otherEffect.categoryId != (int) EffectCategoryEnum.OTHER) ||
                     ( (otherEffect.traitsId != (int) EffectOtherEnum.MASTER_SKILL) && (otherEffect.traitsId != (int) EffectOtherEnum.COMMON_EVENT) )
                   )
                {
                    otherEffect.effectUID = "";
                    continue;
                }
                //Index からUIDを設定する
                if (otherEffect.traitsId == (int) EffectOtherEnum.MASTER_SKILL)
                {
                    //既に適応済み?
                    if (skillUidList.Contains(otherEffect.effectUID) == false)
                    {
                        otherEffect.effectUID = skillDataModels[otherEffect.effectId].basic.id;
                    }
                }
                else
                if (otherEffect.traitsId == (int) EffectOtherEnum.COMMON_EVENT)
                {
                    if ( (ecategoryBlockntCommonDataModels.Count == 0) || (ecategoryBlockntCommonDataModels.Count <= otherEffect.effectId) )
                    {
                        //念のため空を指定しておく
                        otherEffect.effectUID = "";
                        otherEffect.effectId = 0;
                        continue;
                    }

                    //既に適応済み?
                    if (commonuidList.Contains(otherEffect.effectUID) == false)
                    {
                        otherEffect.effectUID = ecategoryBlockntCommonDataModels[otherEffect.effectId].eventId;
                    }
                }
            }
        }
    }
}