using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime;
using RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement;
using RPGMaker.Codebase.CoreSystem.Service.EventManagement;
using RPGMaker.Codebase.Runtime.Common;
using RPGMaker.Codebase.Runtime.Common.Enum;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TextMP = TMPro.TextMeshProUGUI;


namespace RPGMaker.Codebase.Runtime.Map.Menu
{
    public class DebugToolGroupMenu : WindowBase
    {
        // コアシステムサービス
        //メッセージの表示部位
        private TextMP _description;

        //グループリストを入れるエリア
        [SerializeField] private GameObject _groupArea = null;

        //値リストを入れるエリア
        [SerializeField] private GameObject _valueArea = null;

        private List<bool> _switchData = new List<bool>();
        private List<string> _variableData = new List<string>();
        private List<RuntimeSaveDataModel.SaveDataSelfSwitchesData> _selfSwitchData = new List<RuntimeSaveDataModel.SaveDataSelfSwitchesData>();
        private List<DebugToolGroupItem> _groupItems = new List<DebugToolGroupItem>();
        private DebugToolGroupItem _groupItem;

        private List<DebugToolValueItem> _valueItems = new List<DebugToolValueItem>();
        private DebugToolValueItem _valueItem;
        private string _nowMenu = "";

        //上に表示される項目の部分
        [SerializeField] private GameObject _topMenusArea = null;

        //クローンのもとになるオブジェクト
        [SerializeField] private GameObject groupItemObject = null;
        [SerializeField] private GameObject switchItemObject = null;
        [SerializeField] private GameObject selfSwitchItemObject = null;
        public DebugToolWindow Manager { get; private set; }

        List<string> _menuNameList = new List<string> { "switch", "variable", "mapEvent" };
        public enum WindowState
        {
            DataType = 0,
            GroupList,
            ValueList
        }

        private List<DebugToolButton> _menus;
        private WindowState _state;

        public WindowState State { get { return _state; } }

        FieldInfo _autoChangeFocusedColorFi;

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void Init(DebugToolWindow manager) {
#else
        public async Task Init(DebugToolWindow manager) {
#endif
            _state = WindowState.DataType;

            // 設定値をコピー。
            var runtimeSaveDataModel = DataManager.Self().GetRuntimeSaveDataModel();
            _switchData.Clear();
            CopySwitchData(_switchData, runtimeSaveDataModel.switches.data);
            _variableData.Clear();
            CopyVariableData(_variableData, runtimeSaveDataModel.variables.data);
            _selfSwitchData.Clear();
            CopySelfSwitchData(_selfSwitchData, runtimeSaveDataModel.selfSwitches);

            //MenuBase = manager as MenuBase;
            Manager = manager;

            _autoChangeFocusedColorFi = typeof(WindowButtonBase).GetField("AutoChangeFocusdColor", BindingFlags.NonPublic | BindingFlags.Instance);

            //リストの初期化
            _groupItems = new List<DebugToolGroupItem>();

            int menuIndex = 0;
            //メニューの初期化
            _menus = new List<DebugToolButton>();
            for (var i = 0; i < _topMenusArea.transform.childCount; i++)
            {
                var button = _topMenusArea.transform.Find("Item" + (i + 1)).GetComponent<DebugToolButton>();
                button.OnSelected += (button) =>
                {
                    DebugToolButton.blockEvent = true;
                    _ = FocusGroupAsync(button);
                };
                _menus.Add(button);
                var windowButtonBase = button.GetComponent<WindowButtonBase>();
                if (_autoChangeFocusedColorFi != null)
                {
                    //Debug.Log($"_autoChangeFocusedColorFi.SetValue: {windowButtonBase.name}");
                    _autoChangeFocusedColorFi.SetValue(windowButtonBase, true);
                }
                if (windowButtonBase.IsHighlight())
                {
                    menuIndex = i;
                }
            }

            //十字キーでの操作登録
            for (var i = 0; i < _menus.Count; i++)
            {
                var nav = _menus[i].navigation;
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnLeft = _menus[i == 0 ? _menus.Count - 1 : i - 1];
                nav.selectOnRight = _menus[(i + 1) % _menus.Count];

                _menus[i].navigation = nav;
                _menus[i].targetGraphic = _menus[i].transform.Find("Highlight").GetComponent<Image>();
            }

            //グループの初期化
            if (_groupArea.transform.childCount > 0)
            {
                foreach (Transform child in _groupArea.transform)
                {
                    GameObject.Destroy(child.gameObject);
                }
            }

            _description = transform.Find("MenuArea/Description/DescriptionText").GetComponent<TextMP>();

            //共通のウィンドウの適応
            Init();

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            UpdateGroupList(false);
#else
            await UpdateGroupList(false);
#endif

            //メニューの先頭にフォーカスをあてておく
            /*if (_menus.Count > 0)
            {
                _menus[0].GetComponent<DebugToolButton>().Select();
            }*/


#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            MenusEvent(GetMenuNameByIndex(menuIndex));
#else
            await MenusEventAsync(GetMenuNameByIndex(menuIndex));
#endif

            _state = WindowState.DataType;
            //フォーカス制御
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            ChangeFocusList(false/*true*/);
#else
            await ChangeFocusList(false/*true*/);
#endif
        }

        public void Final() {
            // 変更を反映する。
            var runtimeSaveDataModel = DataManager.Self().GetRuntimeSaveDataModel();
            CopySwitchData(runtimeSaveDataModel.switches.data, _switchData);
            _switchData.Clear();
            CopyVariableData(runtimeSaveDataModel.variables.data, _variableData);
            _variableData.Clear();
            CopySelfSwitchData(runtimeSaveDataModel.selfSwitches, _selfSwitchData);
            _selfSwitchData.Clear();
        }

        async Task FocusGroupAsync(DebugToolButton button) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            await Task.Delay(1);
#else
            await UniteTask.Delay(1);
#endif
            var index = int.Parse(button.gameObject.name.Substring(4)) - 1;
            _nowMenu = GetMenuNameByIndex(index);
            _state = WindowState.GroupList;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            ChangeFocusList(false);
#else
            await ChangeFocusList(false);
#endif
            _state = WindowState.DataType;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            await Task.Delay(2);
#else
            await UniteTask.Delay(2);
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            ChangeFocusList(false);
#else
            await ChangeFocusList(false);
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            await Task.Delay(2);
#else
            await UniteTask.Delay(2);
#endif
            DebugToolButton.blockEvent = false;
        }

        int GetIndexByMenuName(string menuName) {
            return _menuNameList.IndexOf(menuName);
        }

        string GetMenuNameByIndex(int index) {
            return _menuNameList[index];
        }

        void CopySwitchData(List<bool> dst, List<bool> src) {
            for (int i = 0; i < src.Count; i++)
            {
                if (i >= dst.Count)
                {
                    dst.Add(src[i]);
                }
                else
                {
                    if (dst[i] != src[i]) dst[i] = src[i];
                }
            }
        }

        void CopyVariableData(List<string> dst, List<string> src) {
            for (int i = 0; i < src.Count; i++)
            {
                if (i >= dst.Count)
                {
                    dst.Add(src[i]);
                }
                else
                {
                    if (dst[i] != src[i]) dst[i] = src[i];
                }
            }
        }

        void CopySelfSwitchData(List<RuntimeSaveDataModel.SaveDataSelfSwitchesData> dst, List<RuntimeSaveDataModel.SaveDataSelfSwitchesData> src) {
            for (int i = 0; i < src.Count; i++)
            {
                RuntimeSaveDataModel.SaveDataSelfSwitchesData dstData = null;
                if (i >= dst.Count)
                {
                    dstData = new RuntimeSaveDataModel.SaveDataSelfSwitchesData();
                    dst.Add(dstData);
                }
                else
                {
                    dstData = dst[i];
                }
                if (dstData.data == null)
                {
                    dstData.data = new List<bool>();
                }
                for (int j = 0; j < src[i].data.Count; j++)
                {
                    if (j >= dstData.data.Count)
                    {
                        dstData.data.Add(src[i].data[j]);
                    }
                    else
                    {
                        if (dstData.data[j] != src[i].data[j]) dstData.data[j] = src[i].data[j];
                    }
                }
                if (dst[i].id != src[i].id) dst[i].id = src[i].id;
            }
        }

        public override void Update() {
            base.Update();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            _ = UpdateAsync();
        }
        private async Task UpdateAsync() {
#endif
            if (InputHandler.OnDown(HandleType.Back) || InputHandler.OnDown(HandleType.RightClick))
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                Back();
#else
                await BackAsync();
#endif
            }
        }

        /// <summary>
        /// リストのフォーカス位置を変更する
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void ChangeFocusList(bool initialize) {
#else
        private async Task ChangeFocusList(bool initialize) {
#endif
            if (_state == WindowState.DataType)
            {
                //第二階層のメニューのハイライト表示を固定する。
                for (var i = 0; i < _groupItems.Count; i++)
                {
                    var button = _groupItems[i].gameObject.GetComponent<WindowButtonBase>();
                    if (_autoChangeFocusedColorFi != null)
                    {
                        _autoChangeFocusedColorFi.SetValue(button, false);
                    }
                }
                //第一階層のメニューを選択可能とする
                int num = 0;
                for (var i = 0; i < _menus.Count; i++)
                {
                    var windowButtonBase = _menus[i].GetComponent<WindowButtonBase>();
                    if (_autoChangeFocusedColorFi != null)
                    {
                        //Debug.Log($"_autoChangeFocusedColorFi.SetValue: {windowButtonBase.name}");
                        _autoChangeFocusedColorFi.SetValue(windowButtonBase, true);
                    }
                    if (windowButtonBase.IsHighlight())
                    {
                        num = i;
                    }
                }
                //先頭にフォーカスをあてる
                if (_menus.Count > 0)
                {
                    if (initialize)
                    {
                        num = 0;
                    }
                    _menus[num].GetComponent<DebugToolButton>().Select();
                }
            }
            else if (_state == WindowState.GroupList)
            {
                //第一階層のメニューのハイライト表示を固定する。
                for (var i = 0; i < _menus.Count; i++)
                {
                    var windowButtonBase = _menus[i].GetComponent<WindowButtonBase>();
                    if (_autoChangeFocusedColorFi != null)
                    {
                        _autoChangeFocusedColorFi.SetValue(windowButtonBase, false);
                    }
                }
                int num = 0;

                //フォーカス設定
                for (var i = 0; i < _groupItems.Count; i++)
                {
                    var button = _groupItems[i].gameObject.GetComponent<WindowButtonBase>();
                    button.GetComponent<WindowButtonBase>().SetEnabled(true);
                    if (_autoChangeFocusedColorFi != null)
                    {
                        _autoChangeFocusedColorFi.SetValue(button, true);
                    }
                    if (button.GetComponent<WindowButtonBase>().IsHighlight())
                    {
                        num = i;
                    }
                }
                //フォーカスをあてる
                if (_groupItems.Count > 0)
                {
                    var prefix = (_nowMenu == "switch" ? "S" : _nowMenu == "variable" ? "V" : "E");
                    if (!_groupItems[num].name.StartsWith(prefix))
                    {
                        for (var i = 0; i < _groupItems.Count; i++)
                        {
                            if (_groupItems[i].name.StartsWith(prefix))
                            {
                                num = i;
                                break;
                            }
                        }
                    }
                    if (initialize)
                    {
                        num = 0;
                    }
                    _groupItems[num].GetComponent<DebugToolButton>().Select();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    RefreshValueList(_groupItems[num]);
#else
                    await RefreshValueList(_groupItems[num]);
#endif
                }
            }
            else if (_state == WindowState.ValueList)
            {
                //第一階層のメニューのハイライト表示を固定する。
                for (var i = 0; i < _menus.Count; i++)
                {
                    var windowButtonBase = _menus[i].GetComponent<WindowButtonBase>();
                    if (_autoChangeFocusedColorFi != null)
                    {
                        _autoChangeFocusedColorFi.SetValue(windowButtonBase, false);
                    }
                }
                //第二階層のメニューのハイライト表示を固定する。
                for (var i = 0; i < _groupItems.Count; i++)
                {
                    var button = _groupItems[i].gameObject.GetComponent<WindowButtonBase>();
                    if (_autoChangeFocusedColorFi != null)
                    {
                        _autoChangeFocusedColorFi.SetValue(button, false);
                    }
                }

                int num = 0;

                //フォーカスをあてる
                if (_valueItems.Count > 0)
                {
                    if (initialize)
                    {
                        num = 0;
                    }
                    if (_valueItems[num].name.StartsWith("E"))
                    {
                        _valueItems[num].transform.Find($"SelfSwitchLayout/SelfSwitch1").GetComponent<Button>().Select();
                    }
                    else
                    {
                        _valueItems[num].GetComponent<Button>().Select();
                    }
                }
            }
        }

        //上の項目による表示切替
        public void MenusEvent(string menus) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            _ = MenusEventAsync(menus);
        }
        public async Task MenusEventAsync(string menus) {
#endif
            //メッセージ枠のクリア
            //DescriptionClear();
            _nowMenu = menus;
            _state = WindowState.GroupList;

            //リスト更新
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            UpdateGroupList(true);
#else
            await UpdateGroupList(true);
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _ = DelayCallAsync(1, () =>
#else
            _ = DelayCallAsync(1, async () =>
#endif
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                ChangeFocusList(false);
#else
                await ChangeFocusList(false);
#endif
                if (_groupItems.Count > 0)
                {
                    int num = -1;
                    WindowButtonBase button = null;
                    //フォーカス設定
                    for (var i = 0; i < _groupItems.Count; i++)
                    {
                        button = _groupItems[i].gameObject.GetComponent<WindowButtonBase>();
                        if (button.GetComponent<WindowButtonBase>().IsHighlight())
                        {
                            num = i;
                        }
                    }
                    var prefix = (_nowMenu == "switch" ? "S" : _nowMenu == "variable" ? "V" : "E");
                    if (num < 0 || !_groupItems[num].name.StartsWith(prefix))
                    {
                        for (var i = 0; i < _groupItems.Count; i++)
                        {
                            if (_groupItems[i].name.StartsWith(prefix))
                            {
                                num = i;
                                break;
                            }
                        }
                    }
                    button = _groupItems[num].gameObject.GetComponent<WindowButtonBase>();
                    button.GetComponent<WindowButtonBase>().SetHighlight(true);
                }
            });
        }

        async Task DelayCallAsync(int milliSec, System.Action action) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            await Task.Delay(milliSec);
#else
            await UniteTask.Delay(milliSec);
#endif
            action();
        }

        //アイテム表示
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void UpdateGroupList(bool focus) {
#else
        private async Task UpdateGroupList(bool focus) {
#endif
            if (_groupItems.Count == 0)
            {
                var databaseManagementService = new DatabaseManagementService();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var switches = databaseManagementService.LoadFlags().switches;
#else
                var switches = (await databaseManagementService.LoadFlags()).switches;
#endif
                for (var i = 0; i < switches.Count; i += 10)
                {
                    //switches[i].name
                    int end = Mathf.Min(i + 10 - 1, switches.Count - 1);
                    //Switchの表示項目のクローンを生成
                    var item = Instantiate(groupItemObject);
                    item.transform.SetParent(_groupArea.transform, false);
                    item.SetActive(true);
                    item.name = $"S{i + 1}";
                    //項目の代入を開始
                    _groupItem = item.AddComponent<DebugToolGroupItem>();
                    _groupItem.Init(i, $"S [{i + 1:D4}-{end + 1:D4}]",
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        "switch", null/*this*/, RefreshValueListAndFocus, RefreshValueList);
#else
                        "switch", null/*this*/, RefreshValueListAndFocus, (debugToolGroupItem) => { _ = RefreshValueList(debugToolGroupItem); });
#endif
                    _groupItems.Add(_groupItem);
                }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var variables = databaseManagementService.LoadFlags().variables;
#else
                var variables = (await databaseManagementService.LoadFlags()).variables;
#endif
                for (var i = 0; i < variables.Count; i += 10)
                {
                    int end = Mathf.Min(i + 10 - 1, variables.Count - 1);
                    //Variableの表示項目のクローンを生成
                    var item = Instantiate(groupItemObject);
                    item.transform.SetParent(_groupArea.transform, false);
                    item.SetActive(true);
                    item.name = $"V{i + 1}";
                    //項目の代入を開始
                    _groupItem = item.AddComponent<DebugToolGroupItem>();
                    _groupItem.Init(i, $"V [{i + 1:D4}-{end + 1:D4}]",
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        "variable", null/*this*/, RefreshValueListAndFocus, RefreshValueList);
#else
                        "variable", null/*this*/, RefreshValueListAndFocus, (debugToolGroupItem) => { _ = RefreshValueList(debugToolGroupItem); });
#endif
                    _groupItems.Add(_groupItem);
                }

                var mapDataModel = MapManager.CurrentMapDataModel;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var eventMapEntities = new EventManagementService().LoadEventMapEntitiesByMapId(mapDataModel.id);
#else
                var eventMapEntities = await new EventManagementService().LoadEventMapEntitiesByMapId(mapDataModel.id);
#endif
                for (var i = 0; i < eventMapEntities.Count; i += 10)
                {
                    int end = Mathf.Min(i + 10 - 1, eventMapEntities.Count - 1);
                    //SelfSwitchの表示項目のクローンを生成
                    var item = Instantiate(groupItemObject);
                    item.transform.SetParent(_groupArea.transform, false);
                    item.SetActive(true);
                    item.name = $"E{i + 1}";
                    //項目の代入を開始
                    _groupItem = item.AddComponent<DebugToolGroupItem>();
                    //var serialNumber = i + 1;// eventMapEntities[i].SerialNumber;
                    _groupItem.Init(i, $"Ev[{i + 1:D4}-{end + 1:D4}]",
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        "mapEvent", null/*this*/, RefreshValueListAndFocus, RefreshValueList);
#else
                        "mapEvent", null/*this*/, RefreshValueListAndFocus, (debugToolGroupItem) => { _ = RefreshValueList(debugToolGroupItem); });
#endif
                    _groupItems.Add(_groupItem);
                }


                //十字キーでの操作登録
                var selects = _groupArea.GetComponentsInChildren<Button>();
                for (var i = 0; i < selects.Length; i++)
                {
                    var nav = selects[i].navigation;
                    nav.mode = Navigation.Mode.Explicit;
                    nav.selectOnUp = selects[i == 0 ? selects.Length - 1 : i - 1];
                    nav.selectOnDown = selects[(i + 1) % selects.Length];

                    selects[i].navigation = nav;
                    selects[i].targetGraphic = selects[i].transform.Find("Highlight").GetComponent<Image>();
                }
            }

            if (focus)
            {
                //フォーカス制御
                _state = WindowState.GroupList;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                ChangeFocusList(false);
#else
                await ChangeFocusList(false);
#endif
            }
        }

        void RefreshValueListAndFocus(DebugToolGroupItem groupItem) {
            _ = RefreshValueListAndFocusAsync(groupItem);
        }
        async Task RefreshValueListAndFocusAsync(DebugToolGroupItem groupItem) {
            UpdateGroupHighlight(groupItem);
            _state = WindowState.GroupList;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            ChangeFocusList(false);
#else
            await ChangeFocusList(false);
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            await Task.Delay(2);
#else
            await UniteTask.Delay(2);
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            RefreshValueList(groupItem);
#else
            await RefreshValueList(groupItem);
#endif

            //フォーカス制御
            _state = WindowState.ValueList;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            ChangeFocusList(false);
#else
            await ChangeFocusList(false);
#endif
        }

        void UpdateGroupHighlight(DebugToolGroupItem groupItem) {
            for (var i = 0; i < _groupItems.Count; i++)
            {
                var button = _groupItems[i].gameObject.GetComponent<WindowButtonBase>();
                button.GetComponent<WindowButtonBase>().SetHighlight(false);
            }
            groupItem.GetComponent<WindowButtonBase>().SetHighlight(true);
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void RefreshValueList(DebugToolGroupItem groupItem) {
#else
        public async Task RefreshValueList(DebugToolGroupItem groupItem) {
#endif
            if (_valueArea.transform.childCount > 0)
            {
                foreach (Transform child in _valueArea.transform)
                {
                    if (child.gameObject == switchItemObject || child.gameObject == selfSwitchItemObject) continue;
                    GameObject.Destroy(child.gameObject);
                }
            }
            _valueItems.Clear();

            var startIndex = groupItem.GetStartIndex();
            var menus = groupItem.GetMenus();
            switch (menus)
            {
                case "switch":
                    {
                        var databaseManagementService = new DatabaseManagementService();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        var switches = databaseManagementService.LoadFlags().switches;
#else
                        var switches = (await databaseManagementService.LoadFlags()).switches;
#endif
                        for (var i = 0; i < 10; i++)
                        {
                            if (startIndex + i >= switches.Count) break;
                            var item = Instantiate(switchItemObject);
                            item.transform.SetParent(_valueArea.transform, false);
                            item.SetActive(true);
                            item.name = $"Sw{startIndex + i + 1}";

                            _valueItem = item.AddComponent<DebugToolValueItem>();
                            _valueItem.Init(startIndex + i, $"{startIndex + i + 1:D4}:{switches[startIndex + i].name}", _switchData,
                                "switch", null/*this*/, ChangeWindowStateToValueList, OnValueItemSelected);
                            _valueItems.Add(_valueItem);
                        }
                        break;
                    }

                case "variable":
                    {
                        var databaseManagementService = new DatabaseManagementService();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        var variables = databaseManagementService.LoadFlags().variables;
#else
                        var variables = (await databaseManagementService.LoadFlags()).variables;
#endif
                        for (var i = 0; i < 10; i++)
                        {
                            if (startIndex + i >= variables.Count) break;
                            var item = Instantiate(switchItemObject);
                            item.transform.SetParent(_valueArea.transform, false);
                            item.SetActive(true);
                            item.name = $"Va{startIndex + i + 1}";

                            _valueItem = item.AddComponent<DebugToolValueItem>();
                            _valueItem.Init(startIndex + i, $"{startIndex + i + 1:D4}:{variables[startIndex + i].name}", _variableData,
                                    "variable", null/*this*/, ChangeWindowStateToValueList, OnValueItemSelected);
                            _valueItems.Add(_valueItem);
                        }
                        break;
                    }

                case "mapEvent":
                    {
                        var mapDataModel = MapManager.CurrentMapDataModel;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        var eventMapEntities = new EventManagementService().LoadEventMapEntitiesByMapId(mapDataModel.id);
#else
                        var eventMapEntities = await new EventManagementService().LoadEventMapEntitiesByMapId(mapDataModel.id);
#endif
                        for (var i = 0; i < 10; i++)
                        {
                            if (startIndex + i >= eventMapEntities.Count) break;
                            //SelfSwitchの表示項目のクローンを生成
                            var item = Instantiate(selfSwitchItemObject);
                            item.transform.SetParent(_valueArea.transform, false);
                            item.SetActive(true);
                            item.name = $"Ev{startIndex + i + 1}";
                            //項目の代入を開始
                            _valueItem = item.AddComponent<DebugToolValueItem>();
                            //var serialNumber = i + 1;// eventMapEntities[i].SerialNumber;
                            _valueItem.Init(startIndex + i, $"{startIndex + i + 1:D4}:{eventMapEntities[startIndex + i].name}", _selfSwitchData,
                                "selfSwitch", null/*this*/, ChangeWindowStateToValueList, OnValueItemSelected, eventMapEntities[startIndex + i].eventId);
                            _valueItems.Add(_valueItem);
                        }
                        break;
                    }

            }

            //十字キーでの操作登録
            if (menus == "switch" || menus == "variable")
            {
                var selects = _valueArea.GetComponentsInChildren<Button>();
                //var selects = _valueItems.Select(x => x.getcomponent<Button>();
                for (var i = 0; i < selects.Length; i++)
                {
                    var nav = selects[i].navigation;
                    nav.mode = Navigation.Mode.Explicit;
                    nav.selectOnUp = selects[i == 0 ? selects.Length - 1 : i - 1];
                    nav.selectOnDown = selects[(i + 1) % selects.Length];
                    nav.selectOnLeft = null;
                    nav.selectOnRight = null;

                    selects[i].navigation = nav;
                    selects[i].targetGraphic = selects[i].transform.Find("Highlight").GetComponent<Image>();
                }
            }
            else if (menus == "mapEvent")
            {
                var selects = _valueArea.GetComponentsInChildren<Button>();
                //var selects = _valueItems;
                var rowCount = selects.Length / 4;
                for (int i = 0; i < rowCount; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        var nav = selects[i * 4 + j].navigation;
                        nav.mode = Navigation.Mode.Explicit;

                        nav.selectOnUp = selects[((i + rowCount - 1) % rowCount) * 4 + j];
                        nav.selectOnDown = selects[((i + 1) % rowCount) * 4 + j];
                        nav.selectOnLeft = selects[i * 4 + ((j + 4 - 1) % 4)];
                        nav.selectOnRight = selects[i * 4 + ((j + 1) % 4)];

                        selects[i].navigation = nav;
                        selects[i].targetGraphic = selects[i * 4 + j].transform.Find("Highlight").GetComponent<Image>();
                    }
                }

            }
            UpdateMenuHighlight(menus);

        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        void OnValueItemSelected(DebugToolValueItem valueItem) {
#else
        async Task OnValueItemSelected(DebugToolValueItem valueItem) {
#endif
            if (_state != WindowState.ValueList)
            {
                _state = WindowState.ValueList;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                ChangeFocusList(false);
#else
                await ChangeFocusList(false);
#endif
            }
        }

        void ChangeWindowStateToValueList(DebugToolValueItem valueItem) {
        }

        void UpdateMenuHighlight(string menus) {
            foreach (var menu in _menus)
            {
                menu.GetComponent<WindowButtonBase>().SetHighlight(false);
            }
            var index = GetIndexByMenuName(menus);
            _menus[index].GetComponent<WindowButtonBase>().SetHighlight(true);
        }

        //メッセージの表示を行う
        public void Description(string descriptionText) {
            _description.text = descriptionText;
        }

        public void DescriptionClear() {
            _description.text = "";
        }

        public override void Back() {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            _ = BackAsync();
        }
        public override async Task BackAsync() {
#endif
            if (_state == WindowState.DataType)
            {
                Manager.BackMenu();
            }
            else if (_state == WindowState.GroupList)
            {
                _state = WindowState.DataType;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                ChangeFocusList(false);
#else
                await ChangeFocusList(false);
#endif
                //第二階層のメニューのハイライト表示を消す。
                for (var i = 0; i < _groupItems.Count; i++)
                {
                    var button = _groupItems[i].gameObject.GetComponent<WindowButtonBase>();
                    button.GetComponent<WindowButtonBase>().SetHighlight(false);
                }
            }
            else if (_state == WindowState.ValueList)
            {
                _state = WindowState.GroupList;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                ChangeFocusList(false);
#else
                await ChangeFocusList(false);
#endif
            }
        }

    }
}
