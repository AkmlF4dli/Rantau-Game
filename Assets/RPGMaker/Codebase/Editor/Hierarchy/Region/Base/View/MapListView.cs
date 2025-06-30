using RPGMaker.Codebase.Editor.Common;
using RPGMaker.Codebase.Editor.Common.View;
using RPGMaker.Codebase.Editor.Hierarchy.Common;
using RPGMaker.Codebase.Editor.Hierarchy.Region.Map.View;
using RPGMaker.Codebase.Editor.MapEditor.Window.EventEdit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.UIElements;
using UnityEditor;

namespace RPGMaker.Codebase.Editor.Hierarchy.Region.Base.View
{
    /// <summary>
    ///マップリストの表示を行うクラス
    /// </summary>
    public class MapListView : AbstractHierarchyView
    {
        // const
        //--------------------------------------------------------------------------------------------------------------
        protected override string MainUxml { get { return "Assets/RPGMaker/Codebase/Editor/Hierarchy/Region/Base/Asset/map_list_view.uxml"; } }
        const string MainUss = "Assets/RPGMaker/Codebase/Editor/Hierarchy/Region/Base/Asset/map_list_view.uss";

        // リージョン別ヒエラルキー
        //--------------------------------------------------------------------------------------------------------------
        private readonly VisualElement _mapListVe;
        private VisualElement _mapListArea;
        private MapHierarchyView _mapHierarchyView;

        // UI要素
        //--------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Hierarchy
        /// </summary>
        /// <param name="mapHierarchy"></param>
        public MapListView(
            VisualElement mapListVe,
            MapHierarchyView mapHierarchyView
        ) {
            _mapListVe = mapListVe;
            _mapListVe.Q<Toggle>().style.display = DisplayStyle.None;
            _mapListVe.Q<VisualElement>("unity-content").style.marginLeft = -24;
            _mapHierarchyView = mapHierarchyView;
            InitUI();
            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(MainUss);
            UxmlElement.styleSheets.Add(styleSheet);
        }

        public enum EditMode
        {
            EditMap,
            EditBattle,
            EditEvent
        }
        EditMode[] _editModes = (EditMode[]) System.Enum.GetValues(typeof(EditMode));
        string _modeKeyName = "Unite/MapEditMode";

        public EditMode GetCurrentEditMode() {
            foreach (var editMode in _editModes)
            {
                var button = UxmlElement.Q<Button>(editMode.ToString(), new string[] { "selected" });
                if (button != null)
                {
                    return editMode;
                }
            }
            return EditMode.EditMap;
        }

        public void ModeButtonClicked(EditMode editMode) {
            var button = UxmlElement.Q<Button>(editMode.ToString());
            var mapVe = GetActiveMapButton();
            if (button.ClassListContains("selected"))
            {
                if (mapVe != null)
                {
                    button.AddToClassList("focused");
                }
                return;
            }
            EditorPrefs.SetString(_modeKeyName, editMode.ToString());
            // 選択状態と非選択状態を切り替える
            foreach (var mb in _editModes)
            {
                button = UxmlElement.Q<Button>(mb.ToString());
                if (mb == editMode)
                {
                    button.AddToClassList("selected");
                }
                else
                {
                    button.RemoveFromClassList("selected");
                }
            }
            if (mapVe != null)
            {
                LaunchEditMode(mapVe.name);
            }
            else
            {
                button.RemoveFromClassList("focused");
            }
        }

        public Button GetActiveMapButton() {
            var mapVe = _mapListVe.Q<Button>(null, "active");
            return mapVe;
        }

        /// <summary>
        /// マップリスト内のIDがmapIdのマップをactiveにする。
        /// </summary>
        /// <param name="mapId"></param>
        public void SetActiveMapButton(string mapId) {
            var mapVe = _mapListVe.Q<Button>(mapId);
            if (mapVe != null)
            {
                mapVe.AddToClassList("active");
            }
        }


        public void LaunchEditMode(string mapId) {
            var button = UxmlElement.Q<Button>(null, "selected");
            button.AddToClassList("focused");
            AnalyticsManager.PostEventFromHierarchy(button);
            var mapEntity = Editor.Hierarchy.Hierarchy.mapManagementService.LoadMapById(mapId);
            switch ((EditMode) button.userData)
            {
                case EditMode.EditMap:
                    WindowLayoutManager.CloseEventSubWindows();
                    Editor.Hierarchy.Hierarchy.CloseEventListWindow();
                    MapEditor.MapEditor.LaunchMapEditMode(mapEntity);
                    break;

                case EditMode.EditBattle:
                    WindowLayoutManager.CloseEventSubWindows();
                    Editor.Hierarchy.Hierarchy.CloseEventListWindow();
                    MapEditor.MapEditor.LaunchBattleEditMode(mapEntity);
                    break;

                case EditMode.EditEvent:
                    Editor.Hierarchy.Hierarchy.OpenAndDockEventListWindow();
                    var eventHierarchyView = _mapHierarchyView.GetEventHierarchyView();
                    var mapHierarchyInfo = _mapHierarchyView.GetMapHierarchyInfo();
                    eventHierarchyView.CreateEventContent(mapHierarchyInfo);

                    var eventListView = _mapHierarchyView.GetEventListView();
                    eventListView?.SetEventVe(eventHierarchyView.GetEventFoldout());

                    Inspector.Inspector.Clear(true);
                    //Editor.Hierarchy.Hierarchy.InvokeSelectableElementAction(eventHierarchyView.GetEventFoldout());
                    //すでにEventEditWindowが存在し、同じマップを抱えているなら、再利用する。
                    var eventEditWindow = WindowLayoutManager.GetWindowFromResources<EventEditWindow>() as EventEditWindow;
                    if (eventEditWindow != null && eventEditWindow.GetMapId() == mapEntity.id)
                    {
                        // EventEditWindowを前面に表示する
                        WindowLayoutManager.GetOrOpenWindow(WindowLayoutManager.WindowLayoutId.MapEventEditWindow);
                    }
                    else
                    {
                        MapEditor.MapEditor.LaunchEventPutMode(mapEntity);
                        mapHierarchyInfo.ExecEventType = ExecEventType.Here;
                    }
                    break;
            }
        }

        /// <summary>
        /// 各コンテンツデータの初期化
        /// </summary>
        override protected void InitContentsData() {
            //xmlの読み込み後に、xmlに定義してあるものはすべての右クリックを無効にする
            UxmlElement.Query<VisualElement>().ForEach(element =>
            {
                //右クリックしたときに飛んでくる
                element.RegisterCallback<MouseUpEvent>(evt =>
                {
                    evt.StopPropagation();
                });
            });

            UxmlElement.Q<ScrollView>("hierarchy_scroll").horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            foreach (var editMode in _editModes)
            {
                var button = UxmlElement.Q<Button>(editMode.ToString());
                button.userData = editMode;
                button.RegisterCallback<ClickEvent>(ev =>
                {
                    ModeButtonClicked((EditMode) (ev.currentTarget as Button).userData);
                });
            }

            var modeName = EditorPrefs.GetString(_modeKeyName, "EditMap");
            HierarchyView.DeactivateallItemsCallback -= DeactivateallItemsCallback;
            HierarchyView.DeactivateallItemsCallback += DeactivateallItemsCallback;
            ModeButtonClicked(new List<EditMode>(_editModes).Find(x => x.ToString() == modeName));
            _mapListArea = UxmlElement.Q<VisualElement>("map_list_area");
            _mapListArea.Clear();
            _mapListArea.Add(_mapListVe);

            // すべてのFoldoutを閉じる（初期状態）
            //ExpandAllFoldouts();
            var foldout = _mapListVe.Q<Foldout>();
            foldout.value = true;

            //なぜか閉じてしまうので、無理やり開く。（後で調査したい。）
            Func<Foldout, Task> procAsync = async (Foldout foldout) =>
            {
                await Task.Delay(1);
                foldout.value = true;
            };
            _ = procAsync(foldout);
        }

        void DeactivateallItemsCallback() {
            if (GetActiveMapButton() == null)
            {
                var button = UxmlElement.Q<Button>(null, "selected");
                button.RemoveFromClassList("focused");
            }
            //マップが選択されなくなったら、イベントリストを閉じてもよいが、
            //利便性のために、イベントリストを閉じるのを、EventEditCanvasが破棄されたときまで遅らせる。
            Func<Task> procAsync = async () =>
            {
                await Task.Delay(1);
                var mapVe = GetActiveMapButton();
                if (mapVe == null)
                {
                    //イベントリストウィンドウは残したまま、フォーカスだけ消す。
                    _mapHierarchyView.GetEventListView().DeactivateAllItems();
                }
                else
                {
                    var button = UxmlElement.Q<Button>(null, "selected");
                    button.AddToClassList("focused");
                }
                Editor.Hierarchy.Hierarchy.SaveMapListWindowWeight();
            };

            _ = procAsync();
        }

        public bool IsFocusedAfterUpdatingState() {
            var button = UxmlElement.Q<Button>(null, "selected");
            var focused = (GetActiveMapButton() != null);
            if (focused)
            {
                button.AddToClassList("focused");
            }
            else
            {
                button.RemoveFromClassList("focused");
            }
            return focused;
        }

        public VisualElement GetPopupArea() {
            return UxmlElement.Q<VisualElement>("PopupArea");
        }

        public VisualElement GetMapListVisualElement() {
            return _mapListVe;
        }

        public void CollapseAllFoldouts() {
        }

        public void ExpandAllFoldouts() {
            UxmlElement.Query<Foldout>().ForEach(f => { f.value = true; });
        }

        public void DeactivateAllItems() {
            UxmlElement.Query<Foldout>().ForEach(f => { f.RemoveFromClassList("active"); });
            UxmlElement.Query<Button>().ForEach(f => { f.RemoveFromClassList("active"); });
            UxmlElement.Query<Label>().ForEach(f => { f.RemoveFromClassList("active"); });
        }

        public VisualElement GetActiveClassItem() {
            return UxmlElement.Q(null, new[] { "active" });
        }

        public T GetItem<T>(string name = null, params string[] classes) where T : VisualElement {
            return UxmlElement.Q<T>(name, classes);
        }
    }
}