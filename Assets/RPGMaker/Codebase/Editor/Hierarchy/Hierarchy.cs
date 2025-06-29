using JetBrains.Annotations;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Armor;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.AssetManage;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.CharacterActor;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Class;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Enemy;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.EventCommon;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.EventMap;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Flag;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Item;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Map;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.SkillCustom;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.State;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.SystemSetting;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Troop;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Vehicle;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Weapon;
using RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement;
using RPGMaker.Codebase.CoreSystem.Service.EventManagement;
using RPGMaker.Codebase.CoreSystem.Service.MapManagement;
using RPGMaker.Codebase.Editor.Common;
using RPGMaker.Codebase.Editor.Hierarchy.Region.Animation;
using RPGMaker.Codebase.Editor.Hierarchy.Region.AssetManage;
using RPGMaker.Codebase.Editor.Hierarchy.Region.Base.View;
using RPGMaker.Codebase.Editor.Hierarchy.Region.Character;
using RPGMaker.Codebase.Editor.Hierarchy.Region.CommonEvent;
using RPGMaker.Codebase.Editor.Hierarchy.Region.Equip;
using RPGMaker.Codebase.Editor.Hierarchy.Region.Flags;
using RPGMaker.Codebase.Editor.Hierarchy.Region.Initialization;
using RPGMaker.Codebase.Editor.Hierarchy.Region.Map;
using RPGMaker.Codebase.Editor.Hierarchy.Region.Skill;
using RPGMaker.Codebase.Editor.Hierarchy.Region.Sound;
using RPGMaker.Codebase.Editor.Hierarchy.Region.State;
using RPGMaker.Codebase.Editor.Hierarchy.Region.Type;
using RPGMaker.Codebase.Editor.Inspector;
using RPGMaker.Codebase.Editor.MapEditor.Window.EventEdit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Display = RPGMaker.Codebase.Editor.Common.Enum.Display;

namespace RPGMaker.Codebase.Editor.Hierarchy
{
    public class HierarchyParams : ScriptableSingleton<HierarchyParams>
    {
        public List<string> SelectedElementParents;
        public string SelectedElementName;
        public float ScrollOffset;
    }

    /// <summary>
    /// Hierarchy全体を制御するクラス
    /// </summary>
    public static class Hierarchy
    {
        // 存在しないクラス名をButtonに追加して、どの種類のボタンか判別するために使用する。
        public const string ButtonTypeTag_WithEventSubWindows = "ButtonTypeTag_WithEventSubWindows";

        // core system service fields
        //-----------------------------------------------------------------------
        private static MapManagementService _mapManagementService;
        private static EventManagementService _eventManagementService;
        private static DatabaseManagementService _databaseManagementService;
        public static bool IsInitialized { get; set; }
        private static bool _isRefresh = false;

        public static event Action<VisualElement> ItemSelected;
        public static event Action HierarchyUpdated;

        /// <summary>
        /// MapManagementService
        /// </summary>
        public static MapManagementService mapManagementService
        {
            get
            {
                if (_mapManagementService == null)
                    _mapManagementService = new MapManagementService();
                return _mapManagementService;
            }
        }
        /// <summary>
        /// EventManagementService
        /// </summary>
        public static EventManagementService eventManagementService
        {
            get
            {
                if (_eventManagementService == null)
                    _eventManagementService = new EventManagementService();
                return _eventManagementService;
            }
        }
        /// <summary>
        /// DatabaseManagementService
        /// </summary>
        public static DatabaseManagementService databaseManagementService
        {
            get
            {
                if (_databaseManagementService == null)
                    _databaseManagementService = new DatabaseManagementService();
                return _databaseManagementService;
            }
        }

        // hierarchies
        //-----------------------------------------------------------------------
        private static AnimationHierarchy _animationHierarchy;
        private static AssetManageHierarchy _assetManageHierarchy;
        private static CharacterHierarchy _characterHierarchy;
        private static BattleHierarchy _battleHierarchy;
        private static CommonEventHierarchy _commonEventHierarchy;
        private static EquipHierarchy _equipHierarchy;
        private static FlagsHierarchy _flagsHierarchy;
        private static InitializationHierarchy _initializationHierarchy;
        private static MapHierarchy _mapHierarchy;
        private static MapSampleHierarchy _mapSampleHierarchy;
        private static SkillHierarchy _skillHierarchy;
        private static SoundHierarchy _soundHierarchy;
        private static StateHierarchy _stateHierarchy;
        private static TypeHierarchy _typeHierarchy;

        // element fields
        //-----------------------------------------------------------------------
        private static HierarchyWindow _hierarchyWindow;
        private static HierarchyView _hierarchyView;

        private static MapListWindow _mapListWindow;
        private static MapListView _mapListView;
        private static EventListWindow _eventListWindow;
        private static EventListView _eventListView;

        //イベントリストウィンドウ取得
        public static EventListWindow eventListWindow
        {
            get { return _eventListWindow; }
        }

        // state
        //-----------------------------------------------------------------------
        private static VisualElement _currentActiveItem;

        private static List<SelectableElementAndAction> _selectableElementAndActions;

        //-----------------------------------------------------------------------
        //
        // methods
        //
        //-----------------------------------------------------------------------

        /// <summary>
        /// 初期化処理
        /// </summary>
        public static bool Init(bool isFixReset = false) {
            //static 変数が false に落ちない限り、一度しか通さない
            //初回起動時や、Runtime実行により初期化された直後のみ通す
            if (IsInitialized && !isFixReset) return false;
            IsInitialized = true;
            if (_mapManagementService == null)
            {
                _mapManagementService = new MapManagementService();
                _eventManagementService = new EventManagementService();
                _databaseManagementService = new DatabaseManagementService();
                _selectableElementAndActions = new List<SelectableElementAndAction>();

                _initializationHierarchy = new InitializationHierarchy();
                _characterHierarchy = new CharacterHierarchy();
                _battleHierarchy = new BattleHierarchy();
                _soundHierarchy = new SoundHierarchy();
                _skillHierarchy = new SkillHierarchy();
                _stateHierarchy = new StateHierarchy();
                _equipHierarchy = new EquipHierarchy();
                _typeHierarchy = new TypeHierarchy();
                _animationHierarchy = new AnimationHierarchy();
                _commonEventHierarchy = new CommonEventHierarchy();
                _assetManageHierarchy = new AssetManageHierarchy();
                _flagsHierarchy = new FlagsHierarchy();
                _mapHierarchy = new MapHierarchy();
                _mapSampleHierarchy = new MapSampleHierarchy();

                HierarchyParams.instance.SelectedElementParents = new List<string>();
            }
            _isRefresh = false;

            // ウィンドウ初期化
            _hierarchyWindow = WindowLayoutManager
                .GetOrOpenWindow(WindowLayoutManager.WindowLayoutId.DatabaseHierarchyWindow) as HierarchyWindow;
            if (_hierarchyWindow == null)
                throw new Exception("cannot instantiate hierarchy window.");
            _hierarchyWindow.titleContent = new GUIContent(EditorLocalize.LocalizeText("WORD_1561"));

            // Troopなど、シーンウィンドウにフォーカスが持って行かれてしまう場合カーソルが効かなくなるので
            // シーンウィンドウへの描画などの処理が完了した後にフォーカスを戻す
            EditorApplication.delayCall += () => { _hierarchyWindow.Focus(); };


            // ビューを初期化
            if (_hierarchyView == null)
                _hierarchyView = new HierarchyView(
                    _initializationHierarchy.View,
                    _characterHierarchy.View,
                    _battleHierarchy.View,
                    _soundHierarchy.View,
                    _skillHierarchy.View,
                    _stateHierarchy.View,
                    _equipHierarchy.View,
                    _typeHierarchy.View,
                    _animationHierarchy.View,
                    _commonEventHierarchy.View,
                    _assetManageHierarchy.View,
                    _flagsHierarchy.View,
                    _mapHierarchy.View,
                    _mapSampleHierarchy.View
                );

            // ウィンドウ内にビューを設置
            _hierarchyWindow.rootVisualElement.Clear();
            _hierarchyWindow.rootVisualElement.Add(_hierarchyView);

            // ウィンドウ初期化
            _mapListWindow = WindowLayoutManager.GetOrOpenWindow(WindowLayoutManager.WindowLayoutId.MapListWindow) as MapListWindow;
            if (_mapListWindow == null)
            {
                throw new Exception("cannot instantiate map list window.");
            }

            // ビューを初期化
            if (_mapListView == null)
            {
                _mapListView = _mapHierarchy.View.GetMapListView();
            }

            // ウィンドウ内にビューを設置
            _mapListWindow.rootVisualElement.Clear();
            _mapListWindow.rootVisualElement.Add(_mapListView);

            // ビューを初期化
            if (_eventListView == null)
            {
                // _eventListWindowはここではまだ初期化しない。マップリストでイベント編集モードでマップが選択されたときに生成する。
                _eventListView = _mapHierarchy.View.GetEventListView();
            }

            // マップリストウィンドウの高さの比率を設定する。
            var weight = EditorPrefs.GetFloat(_mapListWindowWeightKeyName, 0.3f);
            var heights = Docker.GetDockedWindowHeights(new List<EditorWindow>() { _mapListWindow, _hierarchyWindow });
            if (heights[0] > 0 && heights[1] > 0)
            {
                // マップリストウィンドウ、RPG Dataウィンドウの順にドッキングしている。
                var mapListHeight = Mathf.Floor((heights[0] + heights[1]) * weight);
                Docker.SetDockWindowHeight(_mapListWindow, _hierarchyWindow, mapListHeight);
            }

            _ = Refresh();

            // キャッシュクリアし、タイトルにフォーカス
            Inspector.Inspector.ClearCached();
            SelectButton("title_button");

            return true;
        }

        const string _mapListWindowWeightKeyName = "Unite/MapListWindowWeight";

        //<summary>
        //マップリストウィンドウの高さの比率を記録する。
        //</summary>
        public static void SaveMapListWindowWeight() {
            float weight = 0;
            var heights = Docker.GetDockedWindowHeights(new List<EditorWindow>() { _mapListWindow, _eventListWindow, _hierarchyWindow });
            if (heights[0] > 0 && heights[1] > 0 && heights[2] > 0)
            {
                // マップリストウィンドウ、イベントリストウィンドウ、RPG Dataウィンドウの順にドッキングしている。
                weight = (heights[0] + heights[1]) / (heights[0] + heights[1] + heights[2]);
            }
            else
            {
                heights = Docker.GetDockedWindowHeights(new List<EditorWindow>() { _mapListWindow, _hierarchyWindow });
                if (heights[0] > 0 && heights[1] > 0)
                {
                    // マップリストウィンドウ、RPG Dataウィンドウの順にドッキングしている。
                    weight = heights[0] / (heights[0] + heights[1]);
                }
            }
            if (weight > 0)
            {
                // 比率を記録。
                EditorPrefs.SetFloat(_mapListWindowWeightKeyName, weight);
            }
        }

        /// <summary>
        /// Hierarchy更新
        /// </summary>
        /// <param name="targetRegion"></param>
        public static async Task Refresh(Enum.Region targetRegion = Enum.Region.All, string updateData = null, bool isRefresh = true, bool isForce = false) {
            //Hierarchyに対する更新が多重に行われた場合、先勝ちとする
            if (_isRefresh && !isForce) return;
            _isRefresh = true;

            if (targetRegion != Enum.Region.Map && targetRegion != Enum.Region.TileGroup)

                await Task.Delay(2);

            switch (targetRegion)
            {
                case Enum.Region.All:
                    _animationHierarchy.Refresh();
                    _assetManageHierarchy.Refresh();
                    _characterHierarchy.Refresh();
                    _battleHierarchy.Refresh(updateData);
                    _commonEventHierarchy.Refresh();
                    _equipHierarchy.Refresh();
                    _flagsHierarchy.Refresh();
                    _initializationHierarchy.Refresh();
                    _mapHierarchy.Refresh(updateData);
                    _mapSampleHierarchy.Refresh();
                    _skillHierarchy.Refresh();
                    _soundHierarchy.Refresh();
                    _stateHierarchy.Refresh();
                    _typeHierarchy.Refresh();
                    break;
                case Enum.Region.Database:
                    _animationHierarchy.Refresh();
                    _assetManageHierarchy.Refresh();
                    _characterHierarchy.Refresh();
                    _battleHierarchy.Refresh(updateData);
                    _commonEventHierarchy.Refresh();
                    _equipHierarchy.Refresh();
                    _flagsHierarchy.Refresh();
                    _initializationHierarchy.Refresh();
                    _mapHierarchy.Refresh(updateData);
                    _mapSampleHierarchy.Refresh();
                    _skillHierarchy.Refresh();
                    _soundHierarchy.Refresh();
                    _stateHierarchy.Refresh();
                    _typeHierarchy.Refresh();
                    break;
                case Enum.Region.Initialization:
                    _initializationHierarchy.Refresh();
                    break;
                case Enum.Region.Character:
                    _characterHierarchy.Refresh();
                    break;
                case Enum.Region.Battle:
                    _battleHierarchy.Refresh(updateData);
                    break;
                case Enum.Region.Sound:
                    _soundHierarchy.Refresh();
                    break;
                case Enum.Region.Skill:
                    _skillHierarchy.Refresh();
                    break;
                case Enum.Region.StateEdit:
                    _stateHierarchy.Refresh();
                    break;
                case Enum.Region.Equip:
                    _equipHierarchy.Refresh();
                    break;
                case Enum.Region.TypeEdit:
                    _typeHierarchy.Refresh();
                    break;
                case Enum.Region.Animation:
                    _animationHierarchy.Refresh();
                    break;
                case Enum.Region.CommonEvent:
                    _commonEventHierarchy.Refresh();
                    break;
                case Enum.Region.AssetManage:
                    _assetManageHierarchy.Refresh();
                    break;
                case Enum.Region.FlagsEdit:
                    _flagsHierarchy.Refresh();
                    break;
                case Enum.Region.Map:
                    _mapHierarchy.Refresh(updateData);
                    break;
                case Enum.Region.TileGroup:
                    _mapHierarchy.View.RefreshTileGroupContents();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            UpdateMenuWindow();

            //上記の変更を反映後にフォーカス更新
            if (isRefresh)
            {
                await Task.Delay(1);
                UpdateHierarchy();
            }
            _isRefresh = false;
        }

        /// <summary>
        /// メニューウィンドウのボタン状態更新
        /// </summary>
        private static void UpdateMenuWindow() {
            if (_databaseManagementService.LoadSystem().initialParty.startMap.mapId == "" ||
                _databaseManagementService.LoadCharacterActor().Count == 0 ||
                _databaseManagementService.LoadCharacterActorClass().Count == 0 ||
                _mapManagementService.LoadMaps().Count == 0 || _eventManagementService.LoadEvent().Count == 0)
            {
                RpgMakerEditor.DataCheckMenuButton(false);
            }
            else
            {
                RpgMakerEditor.DataCheckMenuButton(true);

            }
        }

        /// <summary>
        /// Hierarchyのスクロール位置を設定する
        /// </summary>
        private static async void UpdateHierarchy() {
            var hierarchyParams = HierarchyParams.instance;
            try
            {
                if (hierarchyParams.SelectedElementName != "")
                {
                    //ボタンの位置が指定されている場合
                    //特にマップは、アウトラインエディターに複数存在するため、親が全て一致している部品を選択状態とする
                    List<VisualElement> btnList = new List<VisualElement>();
                    VisualElement rootVisualElement = null;

                    {
                        btnList = _mapListWindow.rootVisualElement.Query<VisualElement>().Name(hierarchyParams.SelectedElementName).ToList();
                        if (btnList.Count > 0)
                        {
                            rootVisualElement = _mapListWindow.rootVisualElement;
                        }
                        else
                        if (_eventListWindow != null)
                        {
                            btnList = _eventListWindow.rootVisualElement.Query<VisualElement>().Name(hierarchyParams.SelectedElementName).ToList();
                            if (btnList.Count > 0)
                            {
                                rootVisualElement = _eventListWindow.rootVisualElement;
                            }
                        }
                    }
                    if (btnList.Count == 0)
                    {
                        // そうでなければ、ヒエラルキーウィンドウから探す。
                        btnList = _hierarchyWindow.rootVisualElement.Query<VisualElement>().Name(hierarchyParams.SelectedElementName).ToList();
                        if (btnList.Count > 0)
                        {
                            rootVisualElement = _hierarchyWindow.rootVisualElement;
                        }
                    }
                    // イベントリストウィンドウのボタンの場合、イベントリストウィンドウ内のみinactiveにする。
                    if (btnList.FindIndex(x => x.GetClasses().Contains("event-region")) >= 0)
                    {
                        _hierarchyView.DeactivateAllItemsForEventRegion();
                    }
                    else
                    {
                        // ヒエラルキーウィンドウとマップリストウィンドウのボタンをinactiveに。
                        _hierarchyView.DeactivateAllItems();
                    }

                    VisualElement btn = null;

                    if (btnList.Count == 1)
                    {
                        btn = btnList[0];
                    }
                    else
                    {
                        for (int i = 0; i < btnList.Count; i++)
                        {
                            bool flg = false;
                            int cnt = 0;
                            VisualElement btnWork = btnList[i];

                            do
                            {
                                if (btnWork != null && btnWork.name != null && btnWork.name != "")
                                {
                                    if (hierarchyParams.SelectedElementParents[cnt] != btnWork.name)
                                    {
                                        break;
                                    }
                                    else if (cnt + 1 == hierarchyParams.SelectedElementParents.Count)
                                    {
                                        flg = true;
                                        break;
                                    }
                                    cnt++;
                                }
                                btnWork = btnWork.parent;
                            } while (btnWork != null);

                            if (flg)
                            {
                                btn = btnList[i];
                                break;
                            }
                        }
                    }

                    if (btn != null)
                    {
                        //ボタンの色を変更する
                        btn.AddToClassList("active");
                        var label = rootVisualElement.Query<VisualElement>().Name(btn.name + "_label").First();
                        label?.AddToClassList("active");
                        ItemSelected?.Invoke(btn);

                        //編集項目まで開く
                        //SetActiveItem(btn);
                        VisualElement btnWork = btn;
                        do
                        {
                            if (btnWork is Foldout)
                                ((Foldout) btnWork).value = true;

                            btnWork = btnWork.parent;
                        } while (btnWork != null);

                        await Task.Delay(10);

                        //その位置までスクロールする
                        var scrollView = rootVisualElement.Query<ScrollView>().First();
                        if (scrollView.Contains(btn))
                        {
                            //ボタンの座標を取得する
                            Vector2 pos = VisualElementExtensionMethods.GetPosition(btn);
                            //現在のスクロール位置と比較して、ボタンが画面外に存在した場合には、ボタンが見える位置までスクロールする
                            if (!(pos.y >= scrollView.scrollOffset.y && pos.y <= scrollView.scrollOffset.y + _hierarchyView.layout.height))
                            {
                                scrollView.scrollOffset = pos;
                            }
                        }
                    }
                    else
                    {
                        await Task.Delay(10);
                        UpdateHierarchy();
                        return;
                    }
                }
            }
            catch (Exception)
            {
            }
            HierarchyUpdated?.Invoke();
        }

        /// <summary>
        /// Hierarchy内の項目選択時に実行するActionの登録
        /// </summary>
        /// <param name="selectableElement"></param>
        /// <param name="action"></param>
        public static void AddSelectableElementAndAction(VisualElement selectableElement, Action action) {
            selectableElement.AddToClassList("selectableElement");
            _selectableElementAndActions.Add(new SelectableElementAndAction(selectableElement, action));
        }

        public enum FirstView
        {
            None,
            MapEventListView,
            OutlineView
        }
        /// <summary>
        /// アクティブな項目がなければ指定名のボタンを選択する (選択時の処理も実行)。
        /// </summary>
        /// <param name="buttonName"></param>
        public static void CompensateActiveButton(string buttonName, FirstView firstView = FirstView.None) {
            // アクティブな項目があれば何もしない。
            if (_hierarchyView.GetActiveClassItem() != null) return;

            SelectButton(buttonName, firstView);
        }

        /// <summary>
        /// MapListViewのインスタンスを返す。
        /// </summary>
        public static MapListView GetMapListView() {
            return _mapListView;
        }

        /// <summary>
        /// 指定名のボタンを選択する (選択時の処理も実行)。
        /// </summary>
        /// <param name="name"></param>        
        public static void SelectButton(string name, FirstView firstView = FirstView.None) {
            VisualElement targetElement = null;
            //activeがアウトラインの中でなければマップリスト、イベントリストから優先してボタンを探す。
            var outlineFirst = (_hierarchyView.Q<VisualElement>(null, new string[] { "active" }) != null);
            if (firstView != FirstView.None)
            {
                outlineFirst = (firstView == FirstView.OutlineView);
            }
            else if (name.EndsWith("_edit") || name.Contains("_page_"))
            {
                //マップやイベントの場合は、MapListViewやEventListViewを先に検索する。
                outlineFirst = false;
            }
            if (!outlineFirst)
            {
                //アウトライン以外でイベントやマップを探す。
                var index = name.IndexOf("_page_");
                if (index >= 0)
                {
                    var eventId = name.Substring(0, index);
                    var eventMapDataModel = _eventManagementService.LoadEventMap().FirstOrDefault(x => x.eventId == eventId);
                    if (eventMapDataModel != null)
                    {
                        //イベントはマップイベント
                        var mapId = eventMapDataModel.mapId;
                        targetElement = _mapListView.GetItem<Button>(mapId);
                        if (targetElement != null)
                        {
                            //EventEditWindowが存在して、それが抱えるマップが選択したいマップと一致するなら、マップのクリックアクションを実行せずに、シンプルにマップリストのマップをactiveにし、イベントリストウィンドウを表示する。
                            var eventEditWindow = WindowLayoutManager.GetWindowFromResources<EventEditWindow>() as EventEditWindow;
                            if (eventEditWindow != null && eventEditWindow.GetMapId() == mapId && !targetElement.GetClasses().Contains("active"))
                            {
                                _mapListView.SetActiveMapButton(mapId);
                                _mapHierarchy.View.MapClicked(targetElement);
                            }
                            //マップをactive状態にする。
                            if (!targetElement.GetClasses().Contains("active"))
                            {
                                //該当マップをactiveにしたい。
                                Action hierarchyUpdatedCallback = null;
                                hierarchyUpdatedCallback = () =>
                                {
                                    HierarchyUpdated -= hierarchyUpdatedCallback;
                                    if (_mapListView.GetCurrentEditMode() != MapListView.EditMode.EditEvent)
                                    {
                                        //「イベント編集」をフォーカスする。
                                        _mapListView.ModeButtonClicked(MapListView.EditMode.EditEvent);
                                    }
                                    else
                                    {
                                        //マップのイベントリストに更新させる。
                                        _mapHierarchy.View.MapClicked(targetElement);
                                    }
                                    targetElement = _eventListView.GetItem<Button>(name);
                                    if (targetElement != null)
                                    {
                                        InvokeSelectableElementAction(targetElement);
                                    }
                                    return;
                                };
                                HierarchyUpdated += hierarchyUpdatedCallback;
                                InvokeSelectableElementAction(targetElement);
                                return;
                            }
                            //マップは既にactiveなので、イベント編集モードに強制する。
                            if (_mapListView.GetCurrentEditMode() != MapListView.EditMode.EditEvent)
                            {
                                _mapListView.ModeButtonClicked(MapListView.EditMode.EditEvent);
                            }
                            targetElement = _eventListView.GetItem<Button>(name);
                            if (targetElement != null)
                            {
                                InvokeSelectableElementAction(targetElement);
                            }
                            return;
                        }
                    }
                }
                index = name.IndexOf("_edit");
                if (index >= 0)
                {
                    var mapId = name.Substring(0, index);
                    targetElement = _mapListView.GetItem<Button>(mapId);
                    if (targetElement != null)
                    {
                        //マップを選択する。
                        InvokeSelectableElementAction(targetElement);
                        _mapHierarchy.View.MapClicked(targetElement);
                        return;
                    }
                }
                targetElement = _mapListView.GetItem<Button>(name);
                if (targetElement != null)
                {
                    //マップを選択する。
                    InvokeSelectableElementAction(targetElement);
                    _mapHierarchy.View.MapClicked(targetElement);
                    return;
                }
                else
                {
                    targetElement = _eventListView.GetItem<Button>(name);
                }
            }
            if (targetElement == null)
            {
                targetElement = _hierarchyView.GetItem<Button>(name);
                if (targetElement == null)
                {
                    targetElement = _hierarchyView.GetItem<Foldout>(name);
                }
            }
            if (targetElement == null) return;

            InvokeSelectableElementAction(targetElement);
        }


        /// <summary>
        /// 現在"active"なVisualElementを返す。
        /// </summary>
        public static VisualElement GetActiveVisualElement() {
            var ve = _hierarchyView.Q<VisualElement>(null, new string[] { "active" });
            if (ve != null) return ve;
            ve = _eventListView.Q<VisualElement>(null, new string[] { "active" });
            if (ve != null) return ve;
            ve = _mapListView.Q<VisualElement>(null, new string[] { "active" });
            return ve;
        }

        /// <summary>
        /// 現在"active"なクラスを持つViewを返す。
        /// </summary>
        public static FirstView GetCurrentFirstView() {
            var outlineFirst = (_hierarchyView.Q<VisualElement>(null, new string[] { "active" }) != null);
            if (outlineFirst)
            {
                return FirstView.OutlineView;
            }
            if (_mapListView.Q<VisualElement>(null, new string[] { "active" }) != null || _eventListView.Q<VisualElement>(null, new string[] { "active" }) != null)
            {
                return FirstView.MapEventListView;
            }
            return FirstView.None;
        }

        /// <summary>
        /// 指定名のボタンを取得する。
        /// </summary>
        /// <param name="name"></param>        
        public static VisualElement GetVisualElement(string name) {
            var ve = _hierarchyView.GetItem<Button>(name) as VisualElement;
            if (ve == null)
            {
                ve = _hierarchyView.GetItem<Foldout>(name) as VisualElement;
            }
            return ve;
        }

        /// <summary>
        /// ヒエラルキー中のボタンがクリックされた。
        /// </summary>
        /// <param name="targetElement"></param>
        public static void InvokeSelectableElementAction(VisualElement targetElement) {
            // イベント関連のウィンドウを使用するもの以外は、イベント関連のウィンドウが開いていれば閉じる。
            if (!targetElement.ClassListContains(ButtonTypeTag_WithEventSubWindows))
            {
                WindowLayoutManager.CloseEventSubWindows();
            }

            if (_selectableElementAndActions != null)
                for (int i = 0; i < _selectableElementAndActions.Count; i++)
                    if (_selectableElementAndActions[i].Element == targetElement)
                    {
                        _selectableElementAndActions[i].InvokeAction();
                        break;
                    }

            SetActiveItem(targetElement);

            AnalyticsManager.PostEventFromHierarchy(targetElement);
            ItemSelected?.Invoke(targetElement);
        }

        /// <summary>
        /// Hierarchy内の項目をActiveにし、親Foldoutを全てオープンする
        /// </summary>
        /// <param name="item"></param>
        private static void SetActiveItem(VisualElement item) {
            // イベントリストウィンドウのボタンの場合、イベントリストウィンドウ内のみinactiveにする。
            if (item.GetClasses().Contains("event-region"))
            {
                _hierarchyView.DeactivateAllItemsForEventRegion();
            }
            else
            {
                _hierarchyView.DeactivateAllItems();
            }
            _currentActiveItem = item;
            _currentActiveItem.AddToClassList("active");

            // 親Foldoutを全てオープンする
            var targetElement = _currentActiveItem.parent;
            while (targetElement != null)
            {
                if (targetElement is Foldout foldout) foldout.value = true;

                targetElement = targetElement.parent;
            }

            ScrollTo(item);
        }

        /// <summary>
        /// 複数選択を行ない、同じ所が選択された際は、選択解除される
        /// </summary>
        /// <param name="targetElement"></param>
        public static void InvokeMultSelectableElementAction(VisualElement targetElement) {
            // イベント関連のウィンドウを使用するもの以外は、イベント関連のウィンドウが開いていれば閉じる。
            if (!targetElement.ClassListContains(ButtonTypeTag_WithEventSubWindows))
            {
                WindowLayoutManager.CloseEventSubWindows();
            }
            targetElement.ToggleInClassList("active");
        }

        /// <summary>
        /// 選択状態を全解除する
        /// </summary>
        public static void ResetActiveItem() {
            _hierarchyView.DeactivateAllItemsForEventRegion();
        }

        /// <summary>
        /// Hierarchy内を指定位置までスクロールする
        /// </summary>
        /// <param name="item"></param>
        public static void ScrollTo(VisualElement item) {
            if (item == null) return;
            HierarchyParams.instance.SelectedElementName = item.name;

            //親のnameが指定されている場合に、全て配列に格納する
            HierarchyParams.instance.SelectedElementParents.Clear();
            VisualElement ve = item;
            do
            {
                if (ve != null && ve.name != null && ve.name != "")
                {
                    HierarchyParams.instance.SelectedElementParents.Add(ve.name);
                }
                ve = ve.parent;
            } while (ve != null);

            UpdateHierarchy();
        }

        /// <summary>
        /// 1つ前の項目を選択状態にする
        /// </summary>
        public static void SelectPrevItem() {
            SelectItemAt(-1);
        }

        /// <summary>
        /// 1つ先の項目を選択状態にする
        /// </summary>
        public static void SelectNextItem() {
            SelectItemAt(1);
        }

        /// <summary>
        /// 指定位置の項目を選択状態にする
        /// </summary>
        /// <param name="targetIndexFromCurrent"></param>
        private static void SelectItemAt(int targetIndexFromCurrent) {
            if (_currentActiveItem == null) return;

            var targetElement =
                GetElementOfIndexFromCurrent(_currentActiveItem, _currentActiveItem, targetIndexFromCurrent);
            if (targetElement != null) InvokeSelectableElementAction(targetElement);
        }

        [CanBeNull]
        private static VisualElement GetElementOfIndexFromCurrent(
            VisualElement targetElement,
            VisualElement baseElement,
            int targetIndexFromCurrent
        ) {
            var candidate = baseElement.parent;
            while (candidate != null)
            {
                if (candidate is Foldout foldout)
                {
                    var selectableElements = foldout.Query(null, "selectableElement").ToList();
                    var currentIndex = selectableElements.FindIndex(item => item == targetElement);
                    var targetIndex = currentIndex + targetIndexFromCurrent;

                    return selectableElements.ElementAtOrDefault(targetIndex) ??
                           GetElementOfIndexFromCurrent(targetElement, foldout, targetIndexFromCurrent);
                }

                candidate = candidate.parent;
            }

            return null;
        }

        /// <summary>
        /// MapのFoldoutを設定する
        /// </summary>
        public static void SetMapFoldout() {
            UpdateHierarchy();
        }

        /// <summary>
        ///     最後に開いていたインスペクターを開く
        /// </summary>
        public static void SetInspector() {
            var inspectorParams = InspectorParams.instance;
            if (inspectorParams.displayIndex != (int) Display.None)
            {
                bool ret = Inspector.Inspector.IsCached();
                if (ret)
                {
                    if (inspectorParams.displayIndex == (int) Display.MapBackground ||
                        inspectorParams.displayIndex == (int) Display.MapBackgroundCol ||
                        inspectorParams.displayIndex == (int) Display.MapDistant ||
                        inspectorParams.displayIndex == (int) Display.MapEdit ||
                        inspectorParams.displayIndex == (int) Display.MapEvent ||
                        inspectorParams.displayIndex == (int) Display.Encounter ||
                        inspectorParams.displayIndex == (int) Display.MapPreview)
                    {
                        Inspector.Inspector.Clear(true);
                        MapEditor.MapEditor.Refresh();
                    }
                    else
                    {
                        Inspector.Inspector.Refresh();
                    }
                }
                else
                {
                    if (inspectorParams.displayIndex != (int) Display.None)
                        switch ((Display) inspectorParams.displayIndex)
                        {
                            case Display.Title:
                                Inspector.Inspector.TitleView();
                                break;
                            case Display.UiCommon:
                                Inspector.Inspector.UiCommonEditView();
                                break;
                            case Display.GameMenu:
                                Inspector.Inspector.GameMenuView(inspectorParams.Number);
                                break;
                            case Display.Option:
                                Inspector.Inspector.OptionView();
                                break;
                            case Display.BattleMenu:
                                Inspector.Inspector.BattleMenuView();
                                break;
                            case Display.Word:
                                Inspector.Inspector.WordView(inspectorParams.Number);
                                break;
                            case Display.UiTalk:
                                Inspector.Inspector.UiTalkEditView(inspectorParams.Number);
                                break;
                            case Display.Job:
                                Inspector.Inspector.JobCommonView();
                                break;
                            case Display.Sound:
                                Inspector.Inspector.SoundView(inspectorParams.Type, inspectorParams.Number);
                                break;
                            case Display.Character:
                                var characters = _databaseManagementService.LoadCharacterActor();
                                CharacterActorDataModel character = null;
                                for (int i = 0; i < characters.Count; i++)
                                    if (characters[i].uuId == inspectorParams.Uuid)
                                    {
                                        character = characters[i];
                                        break;
                                    }
                                if (character == null) return;
                                Inspector.Inspector.CharacterView(inspectorParams.Number, inspectorParams.Uuid,
                                    _characterHierarchy.View);
                                break;
                            case Display.CharacterEarlyParty:
                                Inspector.Inspector.CharacterEarlyPartyView();
                                break;
                            case Display.CharacterVehicles:
                                var vehicles = _databaseManagementService.LoadCharacterVehicles();
                                VehiclesDataModel vehicle = null;
                                for (int i = 0; i < vehicles.Count; i++)
                                    if (vehicles[i].id == inspectorParams.Uuid)
                                    {
                                        vehicle = vehicles[i];
                                        break;
                                    }
                                if (vehicle == null) return;
                                Inspector.Inspector.VehiclesView(inspectorParams.Uuid);
                                break;
                            case Display.CharacterClass:
                                var classes = _databaseManagementService.LoadCharacterActorClass();
                                ClassDataModel characterClass = null;
                                for (int i = 0; i < classes.Count; i++)
                                    if (classes[i].id == inspectorParams.Uuid)
                                    {
                                        characterClass = classes[i];
                                        break;
                                    }
                                if (characterClass == null) return;
                                Inspector.Inspector.ClassView(inspectorParams.Uuid, _characterHierarchy.View);
                                break;
                            case Display.SkillCommon:
                                Inspector.Inspector.SkillCommonView();
                                break;
                            case Display.SkillCustom:
                                var skills = _databaseManagementService.LoadSkillCustom();
                                SkillCustomDataModel custom = null;
                                for (int i = 0; i < skills.Count; i++)
                                    if (skills[i].basic.id == inspectorParams.Uuid)
                                    {
                                        custom = skills[i];
                                        break;
                                    }
                                if (custom == null) return;
                                Inspector.Inspector.SkillCustomView(custom);
                                break;
                            case Display.Enemy:
                                var enemies = _databaseManagementService.LoadEnemy();
                                EnemyDataModel enemy = null;
                                for (int i = 0; i < enemies.Count; i++)
                                    if (enemies[i].id == inspectorParams.Uuid)
                                    {
                                        enemy = enemies[i];
                                        break;
                                    }
                                if (enemy == null) return;
                                Inspector.Inspector.CharacterEnemyView(inspectorParams.Uuid, _battleHierarchy.View);
                                break;
                            case Display.BattleScene:
                                Inspector.Inspector.BattleSceneView();
                                break;
                            case Display.Troop:
                                var troops = _databaseManagementService.LoadTroop();
                                TroopDataModel troop = null;
                                for (int i = 0; i < troops.Count; i++)
                                    if (troops[i].id == inspectorParams.Uuid)
                                    {
                                        troop = troops[i];
                                        break;
                                    }
                                if (troop == null) return;
                                Inspector.Inspector.TroopSceneView(inspectorParams.Uuid, _battleHierarchy.View,
                                    inspectorParams.Number);
                                break;
                            case Display.Encounter:
                                var maps = _mapManagementService.LoadMaps();
                                MapDataModel encounter = null;
                                for (int i = 0; i < maps.Count; i++)
                                    if (maps[i].id == inspectorParams.Uuid)
                                    {
                                        encounter = maps[i];
                                        break;
                                    }
                                MapEditor.MapEditor.LaunchBattleEditMode(encounter);
                                break;
                            case Display.StateEdit:
                                var states = _databaseManagementService.LoadStateEdit();
                                StateDataModel stateDataModel = null;
                                for (int i = 0; i < states.Count; i++)
                                    if (states[i].id == inspectorParams.Uuid)
                                    {
                                        stateDataModel = states[i];
                                        break;
                                    }
                                if (stateDataModel == null) return;
                                Inspector.Inspector.StateEditView(stateDataModel);
                                break;
                            case Display.Weapon:
                                var weapons = _databaseManagementService.LoadWeapon();
                                WeaponDataModel weaponDataModel = null;
                                for (int i = 0; i < weapons.Count; i++)
                                    if (weapons[i].basic.id == inspectorParams.Uuid)
                                    {
                                        weaponDataModel = weapons[i];
                                        break;
                                    }
                                if (weaponDataModel == null) return;
                                Inspector.Inspector.WeaponEditView(weaponDataModel);
                                break;
                            case Display.Armor:
                                var armors = _databaseManagementService.LoadArmor();
                                ArmorDataModel armorDataModel = null;
                                for (int i = 0; i < armors.Count; i++)
                                    if (armors[i].basic.id == inspectorParams.Uuid)
                                    {
                                        armorDataModel = armors[i];
                                        break;
                                    }
                                if (armorDataModel == null) return;
                                Inspector.Inspector.ArmorEditView(armorDataModel);
                                break;
                            case Display.Item:
                                var items = _databaseManagementService.LoadItem();
                                ItemDataModel itemDataModel = null;
                                for (int i = 0; i < items.Count; i++)
                                    if (items[i].basic.id == inspectorParams.Uuid)
                                    {
                                        itemDataModel = items[i];
                                        break;
                                    }
                                if (itemDataModel == null) return;
                                Inspector.Inspector.ItemEditView(itemDataModel);
                                break;
                            case Display.AttributeTypeEdit:
                                var elements = _databaseManagementService.LoadSystem().elements;
                                SystemSettingDataModel.Element elementDataModel = null;
                                for (int i = 0; i < elements.Count; i++)
                                    if (elements[i].id == inspectorParams.Uuid)
                                    {
                                        elementDataModel = elements[i];
                                        break;
                                    }
                                if (elementDataModel == null) return;
                                Inspector.Inspector.AttributeTypeEditView(elementDataModel);
                                break;
                            case Display.SkillTypeEdit:
                                var skillTypes = _databaseManagementService.LoadSystem().skillTypes;
                                SystemSettingDataModel.SkillType skillTypeDataModel = null;
                                for (int i = 0; i < skillTypes.Count; i++)
                                    if (skillTypes[i].id == inspectorParams.Uuid)
                                    {
                                        skillTypeDataModel = skillTypes[i];
                                        break;
                                    }
                                if (skillTypeDataModel == null) return;
                                Inspector.Inspector.SkillTypeEditView(skillTypeDataModel);
                                break;
                            case Display.WeaponTypeEdit:
                                var weaponTypes = _databaseManagementService.LoadSystem().weaponTypes;
                                SystemSettingDataModel.WeaponType weaponTypeDataModel = null;
                                for (int i = 0; i < weaponTypes.Count; i++)
                                    if (weaponTypes[i].id == inspectorParams.Uuid)
                                    {
                                        weaponTypeDataModel = weaponTypes[i];
                                        break;
                                    }
                                if (weaponTypeDataModel == null) return;
                                Inspector.Inspector.WeaponTypeEditView(weaponTypeDataModel);
                                break;
                            case Display.ArmorTypeEdit:
                                var armorTypes = _databaseManagementService.LoadSystem().armorTypes;
                                SystemSettingDataModel.ArmorType armorTypeDataModel = null;
                                for (int i = 0; i < armorTypes.Count; i++)
                                    if (armorTypes[i].id == inspectorParams.Uuid)
                                    {
                                        armorTypeDataModel = armorTypes[i];
                                        break;
                                    }
                                if (armorTypeDataModel == null) return;
                                Inspector.Inspector.ArmorTypeEditView(armorTypeDataModel);
                                break;
                            case Display.EquipmentTypeEdit:
                                var equipTypes = _databaseManagementService.LoadSystem().equipTypes;
                                SystemSettingDataModel.EquipType equipTypeDataModel = null;
                                for (int i = 0; i < equipTypes.Count; i++)
                                    if (equipTypes[i].id == inspectorParams.Uuid)
                                    {
                                        equipTypeDataModel = equipTypes[i];
                                        break;
                                    }
                                if (equipTypeDataModel == null) return;
                                Inspector.Inspector.EquipmentTypeEditView(equipTypeDataModel);
                                break;
                            case Display.Animation:
                                Inspector.Inspector.AnimEditView(inspectorParams.Number);
                                break;
                            case Display.CommonEvent:
                                var commonEvents = _eventManagementService.LoadEventCommon();
                                EventCommonDataModel commonEventDataModel = null;
                                for (int i = 0; i < commonEvents.Count; i++)
                                    if (commonEvents[i].eventId == inspectorParams.Uuid)
                                    {
                                        commonEventDataModel = commonEvents[i];
                                        break;
                                    }
                                if (commonEventDataModel == null) return;
                                Inspector.Inspector.CommonEventEditView(commonEventDataModel);
                                break;
                            case Display.AssetManage:
                                var assetManages = _databaseManagementService.LoadAssetManage();
                                AssetManageDataModel assetManage = null;
                                for (int i = 0; i < assetManages.Count; i++)
                                    if (assetManages[i].id == inspectorParams.Uuid)
                                    {
                                        assetManage = assetManages[i];
                                        break;
                                    }
                                if (assetManage == null) return;
                                Inspector.Inspector.AssetManageEditView(assetManage);
                                break;
                            case Display.SwitchEdit:
                                var switches = _databaseManagementService.LoadFlags().switches;
                                FlagDataModel.Switch switchDataModel = null;
                                for (int i = 0; i < switches.Count; i++)
                                    if (switches[i].id == inspectorParams.Uuid)
                                    {
                                        switchDataModel = switches[i];
                                        break;
                                    }
                                if (switchDataModel == null) return;
                                Inspector.Inspector.SwitchEditView(switchDataModel);
                                break;
                            case Display.VariableEdit:
                                var variables = _databaseManagementService.LoadFlags().variables;
                                FlagDataModel.Variable variableDataModel = null;
                                for (int i = 0; i < variables.Count; i++)
                                    if (variables[i].id == inspectorParams.Uuid)
                                    {
                                        variableDataModel = variables[i];
                                        break;
                                    }
                                if (variableDataModel == null) return;
                                Inspector.Inspector.VariableEditView(variableDataModel);
                                break;
                            case Display.EnvironmentEdit:
                                Inspector.Inspector.EnvironmentEditView();
                                break;
                            case Display.MapEdit:
                                var maps2 = _mapManagementService.LoadMaps();
                                MapDataModel mapDataModel = null;
                                for (int i = 0; i < maps2.Count; i++)
                                    if (maps2[i].id == inspectorParams.Uuid)
                                    {
                                        mapDataModel = maps2[i];
                                        break;
                                    }
                                if (mapDataModel == null) return;
                                MapEditor.MapEditor.LaunchMapEditMode(mapDataModel);
                                break;
                            case Display.MapDistant:
                                var maps3 = _mapManagementService.LoadMaps();
                                MapDataModel mapDistantDataModel = null;
                                for (int i = 0; i < maps3.Count; i++)
                                    if (maps3[i].id == inspectorParams.Uuid)
                                    {
                                        mapDistantDataModel = maps3[i];
                                        break;
                                    }
                                if (mapDistantDataModel == null) return;
                                MapEditor.MapEditor.LaunchBattleEditMode(mapDistantDataModel);
                                break;
                            case Display.MapBackground:
                                var maps4 = _mapManagementService.LoadMaps();
                                MapDataModel mapBackgroundDataModel = null;
                                for (int i = 0; i < maps4.Count; i++)
                                    if (maps4[i].id == inspectorParams.Uuid)
                                    {
                                        mapDataModel = maps4[i];
                                        break;
                                    }
                                if (mapBackgroundDataModel == null) return;
                                Inspector.Inspector.MapBackgroundView(mapBackgroundDataModel);
                                break;
                            case Display.MapBackgroundCol:
                                var tiles = _mapManagementService.LoadTileTable();
                                TileDataModel mapBackgroundColDataModel = null;
                                for (int i = 0; i < tiles.Count; i++)
                                    if (tiles[i].id == inspectorParams.Uuid)
                                    {
                                        mapBackgroundColDataModel = tiles[i].TileDataModel;
                                        break;
                                    }
                                if (mapBackgroundColDataModel == null) return;
                                Inspector.Inspector.MapBackgroundCollisionView(mapBackgroundColDataModel);
                                break;
                            case Display.MapTile:
                                var tiles2 = _mapManagementService.LoadTileTable();
                                TileDataModel mapTileDataModel = null;
                                for (int i = 0; i < tiles2.Count; i++)
                                    if (tiles2[i].id == inspectorParams.Uuid)
                                    {
                                        mapTileDataModel = tiles2[i].TileDataModel;
                                        break;
                                    }
                                if (mapTileDataModel == null) return;
                                Inspector.Inspector.MapTileView(mapTileDataModel);
                                break;
                            case Display.MapEvent:
                                var maps5 = _mapManagementService.LoadMaps();
                                MapDataModel eventMapDataModel = null;
                                for (int i = 0; i < maps5.Count; i++)
                                    if (maps5[i].id == inspectorParams.Uuid)
                                    {
                                        eventMapDataModel = maps5[i];
                                        break;
                                    }

                                var evMap = _eventManagementService.LoadEventMap();
                                EventMapDataModel eventDataModel = null;
                                for (int i = 0; i < evMap.Count; i++)
                                    if (evMap[i].eventId == inspectorParams.Type)
                                    {
                                        eventDataModel = evMap[i];
                                        break;
                                    }

                                if (eventMapDataModel == null || eventDataModel == null) return;
                                MapEditor.MapEditor.LaunchEventEditMode(eventMapDataModel, eventDataModel,
                                    inspectorParams.Number);
                                break;
                        }
                }
            }
            UpdateMenuWindow();
        }

        /// <summary>
        /// Mapで最後に選択された項目を再設定する
        /// </summary>
        public static void MapLastSelect() {
            _mapHierarchy.View.SetInit();
            _ = Refresh(Enum.Region.Map);
        }

        // onClick Actions
        //-----------------------------------------------------------------------
        private class SelectableElementAndAction
        {
            private readonly Action _action;

            public SelectableElementAndAction(VisualElement element, Action action) {
                Element = element;
                _action = action;
            }

            public VisualElement Element { get; }

            public void InvokeAction() {
                _action.Invoke();
            }
        }

        const string _eventListWindowWeightKeyName = "Unite/EventListWindowWeight";
        //<summary>
        // イベントリストウィンドウを開き、マップリストウィンドウの下にドッキングさせる。
        // イベントリストウィンドウとマップリストウィンドウの高さの比率が、前回と同様となるよう調整する。
        //</summary>
        public static void OpenAndDockEventListWindow() {
            if (_eventListWindow == null)
            {
                var childWeight = EditorPrefs.GetFloat(_eventListWindowWeightKeyName, 0.5f);
                _eventListWindow = WindowLayoutManager.OpenAndDockWindow(
                    WindowLayoutManager.WindowLayoutId.EventListWindow,
                    WindowLayoutManager.WindowLayoutId.MapListWindow,
                    Docker.DockPosition.Bottom,
                    childWeight
                ) as EventListWindow;
                _eventListWindow.rootVisualElement.Clear();
                _eventListWindow.rootVisualElement.Add(_eventListView);
            }
        }

        //<summary>
        // イベントリストウィンドウを閉じる。
        // その際。イベントリストウィンドウが消える分だけ、マップリストウィンドウを広げる。
        // マップリストウィンドウとイベントリストウィンドウの高さの比率を覚えておく。
        //</summary>
        public static void CloseEventListWindow() {
            if (_eventListWindow != null)
            {
                var heights = Docker.GetDockedWindowHeights(new List<EditorWindow>() { _mapListWindow, _eventListWindow, _hierarchyWindow });
                if (heights[0] > 0 && heights[1] > 0)
                {
                    // マップリストウィンドウの下にイベントリストウィンドウがドッキングしている。
                    var childWeight = heights[1] / (heights[0] + heights[1]);
                    // 比率を記録。
                    EditorPrefs.SetFloat(_eventListWindowWeightKeyName, childWeight);
                }
                float mapListHeight = -1;
                if (heights[0] > 0 && heights[1] > 0 && heights[2] > 0)
                {
                    // マップリストウィンドウの下にイベントリストウィンドウが、イベントリストウィンドウの下にヒエラルキーウィンドウがドッキングしている。
                    mapListHeight = heights[0] + heights[1];
                }
                WindowLayoutManager.CloseWindow(WindowLayoutManager.WindowLayoutId.EventListWindow);
                _eventListWindow = null;
                if (mapListHeight > 0)
                {
                    // マップリストウィンドウを広げる。
                    Docker.SetDockWindowHeight(_mapListWindow, _hierarchyWindow, mapListHeight);
                }
            }
        }
    }

    public static class VisualElementExtensionMethods
    {
        public static Vector2 GetPosition(this VisualElement self) {
            var element = self;
            var position = Vector2.zero;

            while (element is { parent: { } })
            {
                position += element.layout.position;
                element = element.parent;
            }

            return position;
        }
    }
}