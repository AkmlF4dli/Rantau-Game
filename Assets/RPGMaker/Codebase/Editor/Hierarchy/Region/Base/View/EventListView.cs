using RPGMaker.Codebase.Editor.Common;
using RPGMaker.Codebase.Editor.Common.View;
using RPGMaker.Codebase.Editor.Hierarchy.Common;
using RPGMaker.Codebase.Editor.Hierarchy.Region.Map.View;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGMaker.Codebase.Editor.Hierarchy.Region.Base.View
{
    /// <summary>
    /// イベントリストの表示を行うクラス
    /// </summary>
    public class EventListView : AbstractHierarchyView
    {
        // const
        //--------------------------------------------------------------------------------------------------------------
        protected override string MainUxml { get { return "Assets/RPGMaker/Codebase/Editor/Hierarchy/Region/Base/Asset/event_list_view.uxml"; } }

        // リージョン別ヒエラルキー
        //--------------------------------------------------------------------------------------------------------------
        private VisualElement _eventListVe;
        private VisualElement _eventListArea;
        private MapHierarchyView _mapHierarchyView;

        // UI要素
        //--------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Hierarchy
        /// </summary>
        /// <param name="mapHierarchy"></param>
        public EventListView(
            VisualElement eventListVe,
            MapHierarchyView mapHierarchyView
        ) {
            _mapHierarchyView = mapHierarchyView;
            InitUI();
            _eventListArea = UxmlElement.Q<VisualElement>("event_list_area");
            SetEventVe(eventListVe);
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

            UxmlElement.style.flexGrow = 0;
            this.style.flexGrow = 1;
            this.RegisterCallback<MouseUpEvent>(evt =>
            {
                if (evt.button != (int) MouseButton.RightMouse) return;
                var menu = new GenericMenu();
                // イベントの新規作成。
                menu.AddItem(
                    new GUIContent(EditorLocalize.LocalizeText("WORD_0010")),
                    false,
                    () =>
                    {
                        MapEditor.MapEditor.LaunchEventPutMode(_mapHierarchyView.GetActiveMapEntity());
                        _mapHierarchyView.GetMapHierarchyInfo().ExecEventType = ExecEventType.Here;
                    });

                menu.ShowAsContext();
            });

            UxmlElement.Q<ScrollView>("hierarchy_scroll").horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        }

        public void SetEventVe(VisualElement eventListVe) {
            _eventListVe = eventListVe;

            _eventListArea.Clear();
            _eventListArea.Add(_eventListVe);


            _eventListVe.Q<Foldout>().value = true;
            _eventListVe.Q<Toggle>().style.display = DisplayStyle.None;
            _eventListVe.Q<VisualElement>("unity-content").style.marginLeft = -24;
        }

        public VisualElement GetPopupArea() {
            return UxmlElement.Q<VisualElement>("PopupArea");
        }

        public VisualElement GetEventListVisualElement() {
            return _eventListVe;
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