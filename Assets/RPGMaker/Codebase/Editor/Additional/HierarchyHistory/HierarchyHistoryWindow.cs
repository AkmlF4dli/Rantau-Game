using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.EventMap;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Map;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Troop;
using RPGMaker.Codebase.CoreSystem.Service.EventManagement;
using RPGMaker.Codebase.CoreSystem.Service.MapManagement;
using RPGMaker.Codebase.Editor.Common;
using RPGMaker.Codebase.Editor.Hierarchy.Region.Base.View;
using RPGMaker.Codebase.Editor.Hierarchy.Region.Character.View;
using RPGMaker.Codebase.Editor.Hierarchy.Region.Character.View.Component;

namespace RPGMaker.Codebase.Editor.Additional
{
    public class HierarchyHistoryWindow : EditorWindow
    {
        [Serializable]
        public class ItemInfo
        {
            public bool valid;
            public bool locked;
            public string key;
            public List<string> texts;
            public string label;
            public string wname;
            public string vname;

            public class ItemInfo2 : ScriptableObject
            {
                public bool valid;
                public bool locked;
                public string key;
                public List<string> texts;
                public string label;
                public string wname;
                public string vname;
            }

            public ItemInfo(string key, List<string> texts, string label, string wname, string vname) {
                this.valid = true;
                this.locked = false;
                this.key = key;
                this.texts = texts;
                this.label = label;
                this.wname = wname;
                this.vname = vname;
            }

            public ItemInfo2 CreateInstance() {
                var itemInfo = ScriptableObject.CreateInstance<ItemInfo2>();
                itemInfo.valid = valid;
                itemInfo.locked = locked;
                itemInfo.key = key;
                itemInfo.texts = texts;
                itemInfo.label = label;
                itemInfo.wname = wname;
                itemInfo.vname = vname;

                return itemInfo;
            }
        }

        [Serializable]
        public class ItemInfos: ScriptableObject
        {
            public ItemInfo[] root;
        }

        public static int HistoryCountLimit = 50;
        const string HierarchyHistoryFilename = "HierarchyHistory.json";
        const string WindowOpenKeyName = "Unite/HierarchyHistory/WindowOpen";
        private List<ItemInfo> _itemList;
        private ListView _listView;
        private ItemInfo _selectedItemInfo = new ItemInfo(null, null, null, null, null);
        int _updatingLabelCount = 0;

        protected void Awake() {
            titleContent = new GUIContent(EditorLocalize.LocalizeText("WORD_5028"));
        }

        private void CreateGUI()
        {
            rootVisualElement.style.flexGrow = 1;   //EventListViewをウィンドウいっぱいに表示させる。
            // メインUXML読込
            var mainUXMLTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/RPGMaker/Codebase/Editor/Additional/HierarchyHistory/HierarchyHistory.uxml");
            // スタイルシート読込
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/RPGMaker/Codebase/Editor/Additional/HierarchyHistory/HierarchyHistory.uss");

            var itemTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/RPGMaker/Codebase/Editor/Additional/HierarchyHistory/HierarchyHistory_Item.uxml");

            rootVisualElement.Add(EditorLocalize.LocalizeElements(mainUXMLTree.Instantiate()));

            rootVisualElement.styleSheets.Add(styleSheet);

            _itemList = LoadItemInfoList();

            // 履歴リストビューの初期化
            _listView = rootVisualElement.Q<ListView>();
            _listView.makeItem = () =>
            {
                var item = EditorLocalize.LocalizeElements(itemTree.Instantiate());
                return item;
            };
            _listView.bindItem = (item, index) =>
            {
                var itemInfo = _itemList[index];
                var selected = (itemInfo.key == _selectedItemInfo.key && itemInfo.wname == _selectedItemInfo.wname);
                var instance = itemInfo.CreateInstance();
                item.Bind(new SerializedObject(instance));
                var bt = item.Q<Button>();
                bt.SetEnabled(itemInfo.valid);
                bt.clicked += () =>
                {
                    SelectItem(itemInfo);
                };
                var toggle = item.Q<Toggle>();
                toggle.SetEnabled(itemInfo.valid);
                toggle.RegisterValueChangedCallback(evt =>
                {
                    var newValue = evt.newValue;
                    var index = GetItemInfoIndexForListViewItem((evt.currentTarget as VisualElement).parent);
                    if (index >= 0)
                    {
                        var itemInfo = _itemList[index];
                        if (newValue == itemInfo.locked) return;
                        itemInfo.locked = newValue;
                        MoveItemToLatestPosition(itemInfo.key, itemInfo.wname);
                        RebuildListView();
                    }
                });
                var bg = item.Q<VisualElement>("background");
                var label = item.Q<Label>();
                label.SetEnabled(itemInfo.valid);
                if (selected)
                {
                    bg.AddToClassList("selected");
                    bg.RemoveFromClassList("locked");
                }
                else
                {
                    bg.RemoveFromClassList("selected");
                    if (itemInfo.locked)
                    {
                        bg.AddToClassList("locked");
                    }
                    else
                    {
                        bg.RemoveFromClassList("locked");
                    }
                }
                if (itemInfo.locked || selected)
                {
                    label.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
                }
                else
                {
                    label.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Normal);
                }
                item.RegisterCallback<MouseUpEvent>(evt =>
                {
                    if (evt.button != (int) MouseButton.RightMouse) return;
                    var index = GetItemInfoIndexForListViewItem(evt.currentTarget as VisualElement);
                    if (index < 0) return;
                    var menu = new GenericMenu();
                    menu.AddItem(
                        new GUIContent(EditorLocalize.LocalizeText("WORD_1061")),
                        false,
                        () =>
                        {
                            DeleteItem(index);
                            SaveItemInfoList(_itemList);
                        });
                    menu.ShowAsContext();
                });
                if (itemInfo.valid)
                {
                    var lastClickTime = 0d;
                    const double clickInterval = 0.3;
                    item.RegisterCallback<MouseDownEvent>(evt =>
                    {
                        if (evt.button == (int) MouseButton.LeftMouse)
                        {
                            if (EditorApplication.timeSinceStartup - lastClickTime < clickInterval)
                            {
                                // ダブルクリックされたときの処理
                                var index = GetItemInfoIndexForListViewItem(evt.currentTarget as VisualElement);
                                if (index >= 0)
                                {
                                    SelectItem(_itemList[index]);
                                }
                            }
                            lastClickTime = EditorApplication.timeSinceStartup;
                        }
                    });
                }
                item.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Delete)
                    {
                        var index = GetItemInfoIndexForListViewItem(evt.currentTarget as VisualElement);
                        if (index >= 0)
                        {
                            DeleteItem(index);
                            SaveItemInfoList(_itemList);
                        }
                    }
                });
            };
            _listView.RegisterCallback<DragExitedEvent>((e) =>
            {
                var itemsSource = (_listView.itemsSource as List<ItemInfo>);
                _itemList = itemsSource.OrderBy(x => !x.locked).ToList();
                _listView.itemsSource = _itemList;
                RebuildListView();
                SaveItemInfoList(_itemList);
            });

            _listView.itemsSource = _itemList;
            _listView.reorderable = true;
            RebuildListView();

            Hierarchy.Hierarchy.HierarchyUpdated -= UpdateLabels;
            Hierarchy.Hierarchy.HierarchyUpdated += UpdateLabels;
            BattleHierarchyView.ContentsRefreshed -= UpdateLabels;
            BattleHierarchyView.ContentsRefreshed += UpdateLabels;
        }

        private void OnFocus() {
            rootVisualElement.Focus();
        }

        private void OnDestroy() {
            Hierarchy.Hierarchy.HierarchyUpdated -= UpdateLabels;
            BattleHierarchyView.ContentsRefreshed -= UpdateLabels;

            //ユーザー操作でウィンドウが閉じられたと推測される場合は、記録する。
            Func<Task> delayFunc = async () =>
            {
                var stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();
                await Task.Delay(1);
                stopWatch.Stop();
                var milliseconds = stopWatch.ElapsedMilliseconds;
                if (milliseconds < 100)
                {
                    EditorPrefs.SetBool(WindowOpenKeyName, false);
                }
            };
            _ = delayFunc();
        }

        List<ItemInfo> LoadItemInfoList() {
            var json = "{}";
            try
            {
                json = File.ReadAllText(HierarchyHistoryFilename);
            }
            catch (Exception)
            {
            }
            return JsonHelper.FromJsonArray<ItemInfo>(json);
        }

        void SaveItemInfoList(List<ItemInfo> itemList) {
            File.WriteAllText(HierarchyHistoryFilename, $"[{string.Join(",", itemList.Select(x => JsonUtility.ToJson(x)))}]");
        }

        void SetCurrentActiveItem(VisualElement ve) {
            _selectedItemInfo.key = null;
            _selectedItemInfo.wname = null;
            if (ve != null)
            {
                string wname = null;
                string vname = null;
                GetTreeRecur(ve.parent, ref wname, ref vname);
                _selectedItemInfo.key = ve.name;
                _selectedItemInfo.wname = wname;
            }
        }

        void RebuildListView() {
            _listView.schedule.Execute(() => {
                var index = _itemList.FindIndex(x => x.key == _selectedItemInfo.key && x.wname == _selectedItemInfo.wname);
                if (index >= 0)
                {
                    _listView.ScrollToItem(index);
                }
            }).StartingIn(100);
            _listView.Rebuild();
        }

        string GetVisualElementText(VisualElement ve) {
            string veText = null;
            if (ve is Button)
            {
                veText = (ve as Button).text;
            }
            else
            if (ve is Foldout)
            {
                veText = (ve as Foldout).text;
                if (string.IsNullOrEmpty(veText))
                {
                    var children = ve.parent.Children().ToList();
                    var index = children.FindIndex(x => x == ve);
                    if (index + 1 < children.Count)
                    {
                        var lbl = children[index + 1] as Label;
                        if (lbl != null)
                        {
                            veText = lbl.text;
                        }
                    }
                }
            }
            return veText;
        }

        string GetEventName(EventMapDataModel eventMapDataModel, DataModelCache cache) {
            return !string.IsNullOrEmpty(eventMapDataModel.name) ? eventMapDataModel.name : $"EV{cache.GetEventMapDataModels(eventMapDataModel.mapId).IndexOf(eventMapDataModel) + 1:000}";
        }

        class DataModelCache {
            List<EventMapDataModel> _eventMapDataModels;
            MapManagementService _mapManagementService;
            List<TroopDataModel> _troopDataModelList;
            Dictionary<string, MapDataModel> _idMapDataModelDic;
            Dictionary<string, List<EventMapDataModel>> _mapIdEventDataModelListDic;
            Dictionary<string, EventMapDataModel> _idEventMapDataModelDic;
            Dictionary<string, TroopDataModel> _idTroopDataModelDic;
            Dictionary<string, TroopDataModel> _battleEventIdTroopDataModelDic;

            public DataModelCache() {
                _eventMapDataModels = new EventManagementService().LoadEventMap();
                _mapManagementService = new MapManagementService();
                _troopDataModelList = Hierarchy.Hierarchy.databaseManagementService.LoadTroop();
                _idMapDataModelDic = new Dictionary<string, MapDataModel>();
                _mapIdEventDataModelListDic = new Dictionary<string, List<EventMapDataModel>>();
                _idEventMapDataModelDic = new Dictionary<string, EventMapDataModel>();
                _idTroopDataModelDic = new Dictionary<string, TroopDataModel>();
                _battleEventIdTroopDataModelDic = new Dictionary<string, TroopDataModel>();
            }

            public EventMapDataModel GetEventMapDataModel(string eventId) {
                if (_idEventMapDataModelDic.ContainsKey(eventId))
                {
                    return _idEventMapDataModelDic[eventId];
                }
                var eventMapDataModel = _eventMapDataModels.FirstOrDefault(x => x.eventId == eventId);
                _idEventMapDataModelDic.Add(eventId, eventMapDataModel);
                return eventMapDataModel;
            }

            public MapDataModel GetMapDataModel(string mapId) {
                if (_idMapDataModelDic.ContainsKey(mapId))
                {
                    return _idMapDataModelDic[mapId];
                }
                MapDataModel mapDataModel = null;
                try
                {
                    mapDataModel = _mapManagementService.LoadMapById(mapId);
                } catch(Exception)
                {
                    //FileNotFound
                }
                _idMapDataModelDic.Add(mapId, mapDataModel);
                return mapDataModel;
            }

            public List<EventMapDataModel> GetEventMapDataModels(string mapId) {
                if (_mapIdEventDataModelListDic.ContainsKey(mapId))
                {
                    return _mapIdEventDataModelListDic[mapId];
                }
                var eventMapDataModels = _eventMapDataModels.Where(m => m.mapId == mapId).ToList();
                _mapIdEventDataModelListDic.Add(mapId, eventMapDataModels);
                return eventMapDataModels;
            }

            public TroopDataModel GetTroopDataModel(string troopId) {
                if (_idTroopDataModelDic.ContainsKey(troopId))
                {
                    return _idTroopDataModelDic[troopId];
                }
                var troopDataModel = _troopDataModelList.FirstOrDefault(x => x.id == troopId);
                _idTroopDataModelDic.Add(troopId, troopDataModel);
                return troopDataModel;
            }

            public TroopDataModel GetTroopDataModelByBattleEventId(string battleEventId) {
                if (_battleEventIdTroopDataModelDic.ContainsKey(battleEventId))
                {
                    return _battleEventIdTroopDataModelDic[battleEventId];
                }
                var troopDataModel = _troopDataModelList.FirstOrDefault(x => x.battleEventId == battleEventId);
                _battleEventIdTroopDataModelDic.Add(battleEventId, troopDataModel);
                return troopDataModel;
            }
        }

        void UpdateLabels() {
            _updatingLabelCount++;
            if (_updatingLabelCount >= 2) return;
            _ = UpdateLabelAsync();
        }

        async Task UpdateLabelAsync() {
            var lastUpdatingLabelCount = 0;
            do
            {
                lastUpdatingLabelCount = _updatingLabelCount;
                await Task.Delay(100);
                SetCurrentActiveItem(Hierarchy.Hierarchy.GetActiveVisualElement());
                var cache = new DataModelCache();
                foreach (var itemInfo in _itemList)
                {
                    string label = null;
                    var texts = itemInfo.texts;
                    if (itemInfo.wname == "HierarchyView" || itemInfo.wname == null)
                    {
                        var ve = Hierarchy.Hierarchy.GetVisualElement(itemInfo.key);
                        if (ve != null)
                        {
                            itemInfo.valid = true;
                            var veText = GetVisualElementText(ve);
                            if (veText == itemInfo.texts[0])
                            {
                                if (itemInfo.vname == "TroopListView")
                                {
                                    if (itemInfo.texts.Count == 2)
                                    {
                                        Match match;
                                        if ((match = Regex.Match(itemInfo.key, @"^(.+)_foldout$")).Success)
                                        {
                                            var troopId = match.Groups[1].Value;
                                            var troopDataModel = cache.GetTroopDataModel(troopId);
                                            if (troopDataModel?.name == itemInfo.texts[1])
                                            {
                                                continue;
                                            }
                                        }
                                        else if ((match = Regex.Match(itemInfo.key, @"^(.+)-\d+$")).Success)
                                        {
                                            var battleEventId = match.Groups[1].Value;
                                            var troopDataModel = cache.GetTroopDataModelByBattleEventId(battleEventId);
                                            if (troopDataModel?.name == itemInfo.texts[1])
                                            {
                                                continue;
                                            }
                                        }
                                    } else
                                    {
                                        continue;
                                    }
                                }
                                else if (itemInfo.wname == "EventListView")
                                {
                                    if (itemInfo.texts.Count == 3)
                                    {
                                        var match = Regex.Match(itemInfo.key, @"^(.+)_page_(\d+)$");
                                        if (match.Success)
                                        {
                                            var eventId = match.Groups[1].Value;
                                            var eventMapDataModel = cache.GetEventMapDataModel(eventId);
                                            if (eventMapDataModel != null)
                                            {
                                                //セクションイベント
                                                var mapId = eventMapDataModel.mapId;
                                                var mapDataModel = cache.GetMapDataModel(mapId);
                                                if (mapDataModel != null)
                                                {
                                                    if (mapDataModel.name == itemInfo.texts[1] && GetEventName(eventMapDataModel, cache) == itemInfo.texts[2])
                                                    {
                                                        continue;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            (string wname, string vname, string text, List<string> list, List<string> texts2) = GetInfo(ve, cache);
                            label = string.Join("/", list);
                            texts = texts2;
                        }
                    }
                    else if (itemInfo.wname == "MapListView")
                    {
                        var mapId = itemInfo.key;
                        var mapDataModel = cache.GetMapDataModel(mapId);
                        if (mapDataModel != null)
                        {
                            label = $"{EditorLocalize.LocalizeText("WORD_1564")}/{mapDataModel.name}";
                        }
                    }
                    else if (itemInfo.wname == "EventListView")
                    {
                        var match = Regex.Match(itemInfo.key, @"^(.+)_page_(\d+)$");
                        if (match.Success)
                        {
                            var eventId = match.Groups[1].Value;
                            var eventMapDataModel = cache.GetEventMapDataModel(eventId);
                            if (eventMapDataModel != null)
                            {
                                //イベントはマップイベント
                                if (int.TryParse(match.Groups[2].Value, out int page) && page < eventMapDataModel.pages.Count)
                                {
                                    var mapId = eventMapDataModel.mapId;
                                    var mapDataModel = cache.GetMapDataModel(mapId);
                                    if (mapDataModel != null)
                                    {
                                        var eventMapDataModels = cache.GetEventMapDataModels(mapId);
                                        var evName = !string.IsNullOrEmpty(eventMapDataModel.name) ? eventMapDataModel.name : $"EV{eventMapDataModels.IndexOf(eventMapDataModel) + 1:000}";
                                        label = $"({mapDataModel.name})/{EditorLocalize.LocalizeText("WORD_0014")}/{evName}/{EditorLocalize.LocalizeText("WORD_0019")}{(page + 1)}";
                                    }
                                }
                            }
                        }
                    }
                    itemInfo.valid = (label != null);
                    if (itemInfo.valid)
                    {
                        itemInfo.label = label;
                        itemInfo.texts = texts;
                    }
                }
                _listView.itemsSource = _itemList;
                RebuildListView();
                SaveItemInfoList(_itemList);
            } while (_updatingLabelCount > lastUpdatingLabelCount);
            _updatingLabelCount = 0;
        }

        int GetItemInfoIndexForListViewItem(VisualElement ve) {
            var key = ve.Q<TextField>("key").text;
            var wname = ve.Q<TextField>("wname").text;
            var index = _itemList.FindIndex(x => x.key == key && x.wname == wname);
            return index;
        }

        void SelectItem(ItemInfo itemInfo) {
            // ヒエラルキー中の該当する項目を選択。
            var firstView = (itemInfo.wname == "MapListView" || itemInfo.wname == "EventListView" ? RPGMaker.Codebase.Editor.Hierarchy.Hierarchy.FirstView.MapEventListView : RPGMaker.Codebase.Editor.Hierarchy.Hierarchy.FirstView.OutlineView);
            Hierarchy.Hierarchy.SelectButton(itemInfo.key, firstView);
        }

        void DeleteItem(int index) {
            _itemList.RemoveAt(index);
            _listView.itemsSource = _itemList;
            RebuildListView();
        }

        void MoveItemToLatestPosition(string key, string wname) {
            var index = _itemList.FindIndex(x => x.key == key && x.wname == wname);
            if (index < 0)
            {
                Debug.Log($"key/wname not found: {key}/{wname}");
                return;
            }
            var item = _itemList[index];
            _itemList.RemoveAt(index);
            index = _itemList.FindIndex(x => x.locked == false);
            if (index < 0)
            {
                if (_itemList.Count == 0)
                {
                    index = 0;
                } else
                {
                    index = _itemList.Count;
                }
            }
            _itemList.Insert(index, item);
        }

        (string, string, string, List<string>, List<string>) GetInfo(VisualElement ve, DataModelCache cache)
        {
            var texts = new List<string>();
            string wname = null;
            string vname = null;
            var list = GetTreeRecur(ve.parent, ref wname, ref vname);
            string text = GetVisualElementText(ve);
            if (text != null)
            {
                list.Add(text);
                texts.Add(text ?? string.Empty);
            }
            if (vname == "TroopListView")
            {
                //敵グループ、バトルイベント
                texts.Add(list[4]); //敵グループ名
            }
            else if (vname == "OutlineHierarchyView")
            {
                texts.Add(list[2]); //チャプター名
                if (ve.name.StartsWith("section-foldout"))
                {
                    texts.Add(list[3]); //セクション名
                }
                else if (Regex.Match(ve.name, @"_page_\d+$").Success)    //アウトラインイベント
                {
                    if (list.Count == 7)    //チャプターイベント
                    {
                        texts.Add(list[5]); //イベント名
                    }
                    else if (list.Count == 8)   //セクションイベント
                    {
                        texts.Add(list[3]); //セクション名
                        texts.Add(list[4]); //マップ名
                        texts.Add(list[6]); //イベント名
                    }
                }
                else if ((ve.name.EndsWith("_edit") || ve.name.EndsWith("_battle")) && list.Count() == 6)
                {
                    if (list.Count == 6)   //セクション
                    {
                        //マップ編集/バトル編集
                        texts.Add(list[3]); //セクション名
                        texts.Add(list[4]); //マップ名
                    }
                }
            }
            else if (wname == "EventListView")
            {
                if (Regex.Match(ve.name, @"_page_\d+$").Success)    //マップイベント
                {
                    if (list.Count == 4)
                    {
                        texts.Add(list[0]); //マップ名
                        texts.Add(list[2]); //イベント名
                    }
                }
            }
            if (wname == "MapListView" || wname == "EventListView")
            {
                var match = Regex.Match(ve.name, @"^(.+)_page_\d+$");
                if (match.Success)
                {
                    var eventId = match.Groups[1].Value;
                    var eventMapDataModel = cache.GetEventMapDataModel(eventId);
                    if (eventMapDataModel != null)
                    {
                        //イベントはマップイベント
                        var mapId = eventMapDataModel.mapId;
                        var mapDataModel = cache.GetMapDataModel(mapId);
                        if (mapDataModel != null) {
                            list.Insert(0, $"({mapDataModel.name})");
                            texts.Add(mapDataModel.name);
                        }
                    }
                }
            }
            return (wname, vname, text, list, texts);
        }

        void AddItem(VisualElement ve, bool first) {
            SetCurrentActiveItem(Hierarchy.Hierarchy.GetActiveVisualElement());
            if (_itemList == null)
            {
                return;
            }
            var cache = new DataModelCache();
            (string wname, string vname, string text, List<string> list, List<string> texts) = GetInfo(ve, cache);
            if (wname == null)
            {
                if (first)
                {
                    //初回呼び出しで、veからwname情報を取り出せなかったら、ve.nameでveを取得して直してリトライする。
                    ve = Hierarchy.Hierarchy.GetVisualElement(ve.name);
                    if (ve != null)
                    {
                        AddItem(ve, false);
                    }
                }
                return;
            }
            var key = ve.name;
            var label = string.Join("/", list);
            var index = _itemList.FindIndex(x => x.key == key && x.wname == wname);
            if (index >= 0)
            {
                _itemList[index].label = label;
                _itemList[index].texts = texts;
                if (!_itemList[index].locked)
                {
                    MoveItemToLatestPosition(key, wname);
                }
            }
            else
            {
                _itemList.Add(new ItemInfo(ve.name, texts, label, wname, vname));
                MoveItemToLatestPosition(key, wname);
                LimitItemListCount();

            }
            SaveItemInfoList(_itemList);

            RebuildListView();
        }

        void LimitItemListCount() {
            int lockedCount = 0;
            foreach (var itemInfo in _itemList)
            {
                if (!itemInfo.locked) break;
                lockedCount++;
            }
            var keptCount = lockedCount + HistoryCountLimit;
            _itemList.RemoveRange(keptCount, _itemList.Count - keptCount);
        }

        public List<string> GetTreeRecur(VisualElement ve, ref string wname, ref string vname) {
            if (ve == null)
            {
                return new List<string>();
            }
            if (ve is HierarchyView)
            {
                wname = "HierarchyView";
                return new List<string>() { "RPG Data" };
            }
            if (ve is MapListView)
            {
                wname = "MapListView";
                return new List<string>() { EditorLocalize.LocalizeText("WORD_1564") };
            }
            if (ve is EventListView)
            {
                wname = "EventListView";
                return new List<string>() { EditorLocalize.LocalizeText("WORD_0014") };
            }
            if (vname == null)
            {
                if (ve is TroopListView)
                {
                    vname = "TroopListView";
                }
                else if (ve is BattleHierarchyView)
                {
                    vname = "BattleHierarchyView";
                }
            }

            string label = null;
            if (ve is Toggle)
            {
                label = (ve as Toggle).text;
            } else if (ve is Foldout)
            {
                var foldout = (ve as Foldout);
                if (foldout.Q<Toggle>().style.display != DisplayStyle.None && foldout.Q<VisualElement>().style.display != DisplayStyle.None)
                {
                    if (!string.IsNullOrEmpty(foldout.text)) {
                        label = foldout.text;
                    }
                    else
                    {
                        //敵グループ名
                        var parent = foldout.parent;
                        if (parent != null && parent.childCount == 2){
                            var children = parent.Children().ToList();
                            if (children[0] == foldout && children[1] is Label)
                            {
                                label = (children[1] as Label).text;
                            }
                        }
                    }
                }
            }
            var list = GetTreeRecur(ve.parent, ref wname, ref vname);
            if (label != null)
            {
                list.Add(label);
            }
            return list;
        }

        static void AddItemToHierarchyHistory(VisualElement ve) {
            if (!Docker.IsEditorWindowOpen<HierarchyHistoryWindow>() && !EditorPrefs.GetBool(WindowOpenKeyName, false))
            {
                return;
            }
            var wnd = EditorWindow.GetWindow<HierarchyHistoryWindow>();
            wnd.AddItem(ve, true);
        }

        [InitializeOnLoadMethod]
        static void InitializeOnLoadMethod() {
            Hierarchy.Hierarchy.ItemSelected -= AddItemToHierarchyHistory;
            Hierarchy.Hierarchy.ItemSelected += AddItemToHierarchyHistory;
        }

        [MenuItem("RPG Maker/HierarchyHistory", priority = 802)]
        public static void ShowHierarchyHistoryWindow() {
            EditorPrefs.SetBool(WindowOpenKeyName, true);
            _ = GetWindow<HierarchyHistoryWindow>();
        }

    }

}