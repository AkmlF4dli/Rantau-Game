using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.Enum;
using RPGMaker.Codebase.Editor.Common;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.UIElements;

namespace RPGMaker.Codebase.Editor.MapEditor.Window.EventEdit
{
    public class CommandWindowList : CommandWindow
    {
        private VisualElement _root;

        private VisualElement[] _CommandWindow = new VisualElement[4];
        private readonly string[] commandUxml =
            {
                "Assets/RPGMaker/Codebase/Editor/Inspector/Map/Asset/inspector_mapEvent_command1.uxml",
                "Assets/RPGMaker/Codebase/Editor/Inspector/Map/Asset/inspector_mapEvent_command2.uxml",
                "Assets/RPGMaker/Codebase/Editor/Inspector/Map/Asset/inspector_mapEvent_command3.uxml",
            };
        private int _ShowWindowIndex = 0;

        public struct CommandData
        {
            public EventEnum _Event; //コマンドID
            public string _Name;  //コマンド名
        };
        public Dictionary<string, CommandData> EventCommandDataList = new Dictionary<string, CommandData>();
        private CommandWindowOptionalCommands _CommandWindowOptionalCommands;
        private Toolbar _Toolbar;

        public override void ShowWindow(string modalTitle, CallBackWidow callBack) {
            //CB登録
            if (callBack != null) _callBackWindow = callBack;

            Toolbar toolbar = new Toolbar();

            var btn1 = new ToolbarButton { text = EditorLocalize.LocalizeText("WORD_0890") };
            btn1.clicked += Button1Clicked;
            toolbar.Add(btn1);
            var btn2 = new ToolbarButton { text = EditorLocalize.LocalizeText("WORD_0890") };
            toolbar.Add(btn2);
            btn2.clicked += Button2Clicked;
            var btn3 = new ToolbarButton { text = EditorLocalize.LocalizeText("WORD_0890") };
            btn3.clicked += Button3Clicked;
            toolbar.Add(btn3);
            btn1.text += " 1 ";
            btn2.text += " 2 ";
            btn3.text += " 3 ";

            //よく使う
            var btn4 = new ToolbarButton { text = EditorLocalize.LocalizeText("WORD_6151") };
            btn4.name = "OptionalCommandsFast";
            //よく使うボタンを先頭へのチェック
            bool CheckFasr = RPGMakerDefaultConfigSingleton.instance.EventComanedOptionalCommandsFast;
            if (CheckFasr == false){
                toolbar.Add(btn4);
            }else{
                //先頭へ
                toolbar.Insert(0,btn4);
            }
            _Toolbar = toolbar;

            btn4.clicked += Button4Clicked;
            //Window表示
            var w = (CommandWindowList) WindowLayoutManager.GetActiveWindow(
                WindowLayoutManager.WindowLayoutId.MapEventCommandWindow1);

            //タイトル設定
            w.titleContent = new GUIContent(EditorLocalize.LocalizeText("WORD_1570"));
            _root = rootVisualElement;
            _root.Clear();

            _root.Add(toolbar);

            //サイズ指定
            w.minSize = new Vector2(390, 680);
            w.maxSize = w.minSize;
            //UI描画
            int Indxe = 0;
            foreach (var command in commandUxml)
            {
                var commandTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(command);
                VisualElement commandFromUxml = commandTree.CloneTree();
                EditorLocalize.LocalizeElements(commandFromUxml);
                commandFromUxml.style.flexGrow = 1;
                _CommandWindow[Indxe] = commandFromUxml;
                Indxe++;
            }

            EventCommandDataListInit();
            ButtonSetting();

            _CommandWindowOptionalCommands = new CommandWindowOptionalCommands();
            var WindowOptionalCommand = _CommandWindowOptionalCommands.ShowWindow1(this,EventCommandDataList,callBack,Close);
            _CommandWindow[3] = WindowOptionalCommand;
            if (CheckFasr)
            {
                //先頭のタブ表示
                Button4Clicked();
            }
            else
            {
                SetTabWindowShow(0);
            }
        }

        private void EventCommandDataListInit() {

            foreach (var data in CommandWindow.EventCommandList)
            {
                foreach (var CommandWindow in _CommandWindow)
                {
                    if (CommandWindow == null) continue;

                    Button button = CommandWindow.Query<Button>(data.Key);
                    if (button == null) continue;

                    CommandData Command = new CommandData();
                    if (EventCommandDataList.ContainsKey(data.Key))
                    {
                        continue;
                    }
                    Command._Event = data.Value;
                    Command._Name = button.text;
                    //コマンドリストを生成
                    EventCommandDataList.Add(data.Key, Command);
                }
            }
        }


        /// <summary>
        /// 各ボタン押下時のCB登録
        /// </summary>
        private void ButtonSetting() {
            foreach (var data in CommandWindow.EventCommandList)
            {
                foreach (var CommandWindow in _CommandWindow)
                {
                    if (CommandWindow == null) continue;

                    Button button = CommandWindow.Query<Button>(data.Key);
                    if (button == null) continue;
                    button.Clear();
                    button.clickable.clicked += () =>
                    {
                        var code = (int) data.Value;
                        _callBackWindow(code.ToString());
                        Close();
                    };

                    button.RegisterCallback<MouseDownEvent>(evt =>
                    {
                        if (evt.button != (int) MouseButton.RightMouse) return;
                        var menu = new GenericMenu();
                        //登録可能か?
                        var bCheck = _CommandWindowOptionalCommands.ButtonAddCheck(data.Key);

                        if (bCheck == false)
                        {
                            bool bChekcButtonGroupMax = _CommandWindowOptionalCommands.ChekcButtonGroupMax(0);
                            if (bChekcButtonGroupMax == false)
                            {
                                // グループ0追加
                                menu.AddItem(new GUIContent(EditorLocalize.LocalizeText("WORD_6154")), false, () =>
                                {

                                    _CommandWindowOptionalCommands.ButtonAddGroup(data.Key, false);
                                });
                            }
                            else
                            {
                                // グループ0追加
                                menu.AddDisabledItem(new GUIContent(EditorLocalize.LocalizeText("WORD_6154")));
                            }
                            
                            bChekcButtonGroupMax = _CommandWindowOptionalCommands.ChekcButtonGroupMax(1);
                            if (bChekcButtonGroupMax == false)
                            {
                                // グループ1追加
                                menu.AddItem(new GUIContent(EditorLocalize.LocalizeText("WORD_6155")), false, () =>
                                {
                                    _CommandWindowOptionalCommands.ButtonAddGroup(data.Key, true);
                                });
                            }
                            else
                            {
                                // グループ1追加
                                menu.AddDisabledItem(new GUIContent(EditorLocalize.LocalizeText("WORD_6155")));
                            }
                        }
                        else
                        {
                            // グループ0追加
                            menu.AddDisabledItem(new GUIContent(EditorLocalize.LocalizeText("WORD_6154")));
                            // グループ1追加
                            menu.AddDisabledItem(new GUIContent(EditorLocalize.LocalizeText("WORD_6155")));
                        }
                        menu.ShowAsContext();
                    });
                }
            }
        }

        /// <summary>
        /// マップイベント、バトルイベントに応じてボタンの有効状態の切り替えを行う
        /// </summary>
        /// <param name="enabled"></param>
        public override void SetBattleOrMapEnabled(EventCodeList.EventType eventType) {
            foreach (var data in CommandWindow.EventCommandList)
            {
                foreach (var CommandWindow in _CommandWindow)
                {
                    Button button = CommandWindow.Query<Button>(data.Key);
                    if (button == null) continue;
                    if (!EventCodeList.CheckEventCodeExecute((int) data.Value, eventType, false))
                        button.SetEnabled(false);
                    else
                        button.SetEnabled(true);
                }
            }
        }

        /// <summary>
        ///     カスタム移動コマンド内で使えないコマンドを無効化。
        /// </summary>
        public override void SetCustomMoveEnabled() {
            foreach (var name in CommandButtonDisableList)
            {
                foreach (var CommandWindow in _CommandWindow)
                {
                    Button button = CommandWindow.Query<Button>(name);
                    if (button == null) continue;
                    button?.SetEnabled(false);
                }
            }
        }

        /// <summary>
        /// タブ表示の切替
        /// </summary>
        /// <param name="Index"></param>
        private void SetTabWindowShow(int Index) {
            var root = rootVisualElement;
            if (_ShowWindowIndex != -1)
            {
                //現在表示中の物を外す
                HideWindow(root,_ShowWindowIndex);
            }
            ShowWindow(root,Index);
            _ShowWindowIndex = Index;
        }

        /// <summary>
        /// タブ1を押した時
        /// </summary>
        private void Button1Clicked() {
            SetTabWindowShow(0);
        }

        /// <summary>
        /// タブ２を押した時
        /// </summary>
        private void Button2Clicked() {
            SetTabWindowShow(1);
        }

        /// <summary>
        /// タブ3を押した時
        /// </summary>
        private void Button3Clicked() {
            SetTabWindowShow(2);
        }

        /// <summary>
        /// タブ4を押した時
        /// </summary>
        private void Button4Clicked() {
            SetTabWindowShow(3);
        }

        /// <summary>
        /// 表示する
        /// </summary>
        /// <param name="Root"></param>
        private void ShowWindow(VisualElement Root,int Index) {
            //登録記されていない
            if (Root.Contains(_CommandWindow[Index]) == false)
            {
                if (_CommandWindow[Index] != null)
                {
                    Root.Add(_CommandWindow[Index]);
                }
            }
        }

        /// <summary>
        /// 非表示にする
        /// </summary>
        /// <param name="Root"></param>
        private void HideWindow(VisualElement Root, int Index) {
            //登録されている
            if (Root.Contains(_CommandWindow[Index]))
            {
                if (_CommandWindow[Index] != null)
                {
                    Root.Remove(_CommandWindow[Index]);
                }
            }
        }

        /// <summary>
        /// よく使うボタンの移動        /// </summary>
        /// <param name="bChecl"></param>
        public void ToolBerOptionalCommandsFast(bool bChecl) 
        {
            var OptionalCommandsFast = _Toolbar.Q("OptionalCommandsFast");
            //よく使うボタンを先頭へのチェック
            bool CheckFasr = RPGMakerDefaultConfigSingleton.instance.EventComanedOptionalCommandsFast;
            if (CheckFasr)
            {
                _Toolbar.RemoveAt(3);
                _Toolbar.Insert(0, OptionalCommandsFast);
            }
            else
            {
                _Toolbar.RemoveAt(0);
                _Toolbar.Add(OptionalCommandsFast);
            }
        }
    }
}