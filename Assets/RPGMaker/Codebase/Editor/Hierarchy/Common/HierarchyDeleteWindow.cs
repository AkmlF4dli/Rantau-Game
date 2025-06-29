using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.Editor.Common;
using RPGMaker.Codebase.Editor.Common.Window;
using RPGMaker.Codebase.Editor.MapEditor.Component.CommandEditor;
using RPGMaker.Codebase.Editor.Hierarchy;
using RPGMaker.Codebase.Editor.Hierarchy.Common;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;

namespace RPGMaker.Codebase.Editor.Common.View
{
    public class HierarchyDeleteWindow : BaseModalWindow
    {
        private VisualElement _root;
        private ListView _listView;
        private string _MapID;
        private Button _DeleteButton;
        private CommonMapHierarchyView _CommonMapHierarchyView;
        private readonly string commandUxml =
            "Assets/RPGMaker/Codebase/Editor/Inspector/Map/Asset/EventDeleteWindow.uxml";

        struct DeleteItem
        {
            public string text;
            public string name;
        }
        public struct Items
        {
            Label IDLabel;
            Label NameLabel;
        };

        List<bool> _listState = new List<bool>();

        public override void ShowWindow(string modalTitle, CallBackWidow callBack)
        {
            //CB登録
            if (callBack != null) _callBackWindow = callBack;

            //Window表示
            var w = (HierarchyDeleteWindow) WindowLayoutManager.GetActiveWindow(
                WindowLayoutManager.WindowLayoutId.EventDeleteWindow);

            //タイトル設定
            w.titleContent = new GUIContent(EditorLocalize.LocalizeText("WORD_0573"));
            _root = rootVisualElement;
            _root.Clear();

            //サイズ指定
            w.minSize = new Vector2(200, 200);

            //UI描画
            var commandTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(commandUxml);
            VisualElement commandFromUxml = commandTree.CloneTree();
            EditorLocalize.LocalizeElements(commandFromUxml);
            commandFromUxml.style.flexGrow = 1;
            _root.Add(commandFromUxml);
            //削除リスト
            _listView = _root.Q<ListView>("DeleteList");
            //削除ボタン
            _DeleteButton = _root.Q<Button>("DeleteButton");
            _DeleteButton.clicked += EventDeleteMultSelectEventPage;
            _DeleteButton.SetEnabled(false);
        }

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="mapHierarchyInfo"></param>
        /// <param name="MapId"></param>
        public void Init(IMapHierarchyInfo mapHierarchyInfo, string MapId, CommonMapHierarchyView mapHierarchyView) 
        {
            var localText19 = EditorLocalize.LocalizeText("WORD_0019");
            List<DeleteItem> DeleteItemList = new List<DeleteItem>();

            _MapID = MapId;
            _CommonMapHierarchyView = mapHierarchyView;
            //リストを構築する
            var matchingMapDataList = mapHierarchyInfo.EventMapDataModels.Where(m => m.mapId == MapId).ToList();

            for (int i = 0; i < matchingMapDataList.Count; i++)
            {
                if (matchingMapDataList[i].pages.Count > 0)
                {
                    var eventEntity = matchingMapDataList[i];
                    string EventText = !string.IsNullOrEmpty(eventEntity.name)
                                                 ? eventEntity.name
                                                 : $"EV{matchingMapDataList.IndexOf(eventEntity) + 1:000}";

                    // ページ一覧（クリックするとイベントエディタを開く）
                    for (int i2 = 0; i2 < eventEntity.pages.Count; i2++)
                    {
                        var page = eventEntity.pages[i2];
                        var pageNum = page.page;
                        var btnLoadEventPage = new DeleteItem
                        {
                            text = EventText+ " : " +localText19 + (page.page + 1)
                        };

                        btnLoadEventPage.name = CommonMapHierarchyView.GetEventPageButtonName(eventEntity.eventId, pageNum);
                        DeleteItemList.Add(btnLoadEventPage);

                        _listState.Add(false);
                    }
                }
            }
            //リスト設定
            _listView.makeItem += () =>
            {
                var toggle = new Toggle();

                toggle.RegisterValueChangedCallback(e =>
                {
                    var index = (int) toggle.userData;
                    _listState[index] = e.newValue;

                    if (DeleteCheck())
                    {
                        _DeleteButton.SetEnabled(true);
                    }
                    else
                    {
                        _DeleteButton.SetEnabled(false);
                    }
                });
                return toggle;
            };

            _listView.bindItem = (item, index) =>
            {
                var toggle = (Toggle) item;
                toggle.text = DeleteItemList[index].text;
                toggle.name = DeleteItemList[index].name;
                
                toggle.SetValueWithoutNotify(_listState[index]);
                toggle.userData = index;
            };
            _listView.itemsSource = DeleteItemList;
            _listView.selectionType = SelectionType.Multiple;
        }

        /// <summary>
        /// イベントページが複数選択されているイベントを削除する
        /// 削除警告付き
        /// </summary>
        private void EventDeleteMultSelectEventPage() {

            var Items = _listView.itemsSource;

            //削除する見のがない場合は無視
            if (_listState.Contains(true) == false) return;

            //削除確認
            if (EditorUtility.DisplayDialog(
                                            EditorLocalize.LocalizeText("WORD_5021"),
                                            EditorLocalize.LocalizeText("WORD_5022"),
                                            EditorLocalize.LocalizeText("WORD_5023"),
                                            EditorLocalize.LocalizeText("WORD_5024")) == true)
            {
                var DeleteEventNameList = new Dictionary<string, List<int>>();
                //選択されているイベントを削除リストに登録

                int Index = 0;

                foreach (var item in Items)
                {
                    DeleteItem DeleteItem = (DeleteItem)item;
                    string eventId = "";
                    int pageNum = -1;
                    //未選択
                    if (_listState[Index] == false)
                    {
                        Index++;
                        continue;
                    }
                    CommonMapHierarchyView.GetButtonNameEventPage(DeleteItem.name, out eventId, out pageNum);

                    if (DeleteEventNameList.ContainsKey(eventId) == false)
                    {
                        DeleteEventNameList.Add(eventId, new List<int>());
                    }
                    //削除リストに登録
                    DeleteEventNameList[eventId].Add(pageNum);
                    Index++;
                }

                //削除
                foreach (var DeleteEvent in DeleteEventNameList)
                {
                    DeleteEvent.Value.Sort();

                    var EventMapDataModelEntity = AbstractCommandEditor.GetEventMapDataModelByEventId(DeleteEvent.Key);
                    MapEditor.MapEditor.DeleteMultPage(EventMapDataModelEntity, DeleteEvent.Value);
                }
                //リストを更新
                _ = Hierarchy.Hierarchy.Refresh(Hierarchy.Enum.Region.Map, AbstractHierarchyView.RefreshTypeEventEvDelete + "," + _MapID, false);
                
                var EventFoldout = _CommonMapHierarchyView.GetEventFoldout();
                //先頭を選択する
                var FastButton = EventFoldout.Query<Button>().First();
                if (FastButton != null)
                {
                    Hierarchy.Hierarchy.CompensateActiveButton(FastButton.name);
                }
                else
                {
                    WindowLayoutManager.CloseEventSubWindows();
                    MapEditor.MapEditor.EventEditWindowRefresh();
                }

                UnityEditorWrapper.AssetDatabaseWrapper.Refresh2();
                Close();
            }
            return;
        }

        /// <summary>
        /// 削除チェックがついているかを確認する
        /// </summary>
        /// <returns></returns>
        private bool DeleteCheck() 
        {
            bool bCheck = false;

            foreach(var bState in _listState)
            {
                if (bState)
                {
                    bCheck  = true;
                    break;
                }
            }
            return bCheck;
        }

    }
}