using System.IO;
using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.Editor.Common;
using RPGMaker.Codebase.Editor.Common.View;
using RPGMaker.Codebase.Editor.Common.Window;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGMaker.Codebase.Editor.MapEditor.Window.EventEdit
{
    public class CommandWindowOptionalCommands
    {
        public delegate void CallWidowClose();

        [Serializable]
        public class CommandData
        {
            public int m_GroupID;
            public string m_Name;
        }

        private List<CommandData> _CommandDataList = new List<CommandData>();

        private string _FileName = "CommandWindowOptionalCommands.json";
        private VisualElement _root;

        private readonly string CommandUxml ="Assets/RPGMaker/Codebase/Editor/Inspector/Map/Asset/inspector_mapEvent_command_OptionalCommands.uxml";
        private Dictionary<string, CommandWindowList.CommandData> _EventCommandDataList = new Dictionary<string, CommandWindowList.CommandData>(); //イベントボタンリスト
        private List<string> _ButtonList = new List<string>(); //ボタン機能名リスト

        private PopupFieldBase<string> _ButtonPopupEventADD;
        private PopupFieldBase<string> _DropdownGroupSelect;
        private BaseModalWindow.CallBackWidow _CallBack;
        private GroupBox[] GroupBoxs = new GroupBox[2];//グループボックスボタン登録用
        private List<VisualElement> _ToggleList = new List<VisualElement>();    //チェックボックスとボタンのリスト
        private Dictionary<int, List<string>> _CommandList = new Dictionary<int, List<string>>(); //登録ボタンリスト
        private CallWidowClose _CallWidowClose;
        private CommandWindowList _CommandWindowList;
        private Button _ButtonDelete;

        public VisualElement ShowWindow1(CommandWindowList commandWindow,Dictionary<string, CommandWindowList.CommandData> Lsit, BaseModalWindow.CallBackWidow callBack ,CallWidowClose callClose) 
        {
            _CommandWindowList = commandWindow;
            //CB登録
            if (callBack != null) _CallBack = callBack;
            _CallWidowClose = callClose;

            _EventCommandDataList = Lsit;

            //UI描画
            var commandTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CommandUxml);
            VisualElement commandFromUxml = commandTree.CloneTree();
            EditorLocalize.LocalizeElements(commandFromUxml);
            _root = commandFromUxml;
            GroupBox buttonAddGroupBox = commandFromUxml.Query<GroupBox>("ButtonAddGroup");
            VisualElement DropdownField0 = commandFromUxml.Query<VisualElement>("DropdownField0");
            VisualElement ButtonPopupField = commandFromUxml.Q<VisualElement>("ButtonPopupField");
            Label ButtonPopupFieldLabel = commandFromUxml.Q<Label>("ButtonPopupFieldLabel");

            //イベント選択ボタン
            ButtonAddSetting();
            _ButtonPopupEventADD.style.flexGrow = 1;
            _ButtonPopupEventADD.style.width =300;
            _ButtonPopupEventADD.style.height = 25;
            ButtonPopupField.Clear();
            ButtonPopupField.Add(_ButtonPopupEventADD);

            //グループ リスト設定
            List<string> ButtonList = new List<string>() { EditorLocalize.LocalizeText("WORD_6150") + " 0", EditorLocalize.LocalizeText("WORD_6150") + " 1" };
            _DropdownGroupSelect = new PopupFieldBase<string>(ButtonList,0);
            _DropdownGroupSelect.style.flexGrow = 1;
            _DropdownGroupSelect.style.width = 300;
            _DropdownGroupSelect.style.height = 25;
            DropdownField0.Clear();
            DropdownField0.Add(_DropdownGroupSelect);

            //イベント削除ボタン
            Button ButtonDelete = commandFromUxml.Query<Button>("ButtonDelete");
            ButtonDelete.clicked += ButtonDel;
            _ButtonDelete = ButtonDelete;

            //イベント追加ボタン
            Button buttonAdd = commandFromUxml.Query<Button>("ButtonAdd");
            buttonAdd.clicked += () =>
            {
                var Index = (int) _ButtonPopupEventADD.index;

                bool bRadioButton = false;
                if (_DropdownGroupSelect.index == 0)
                {
                    bRadioButton = false;
                }
                else if (_DropdownGroupSelect.index == 1)
                {
                    bRadioButton = true;
                }
                //新規イベントボタン追加
                ButtonAddGroup(Index, bRadioButton, true);
            };
            
            GroupBoxs[0] = commandFromUxml.Query<GroupBox>("ButtonGroup0"); ;
            GroupBoxs[0].text = EditorLocalize.LocalizeText("WORD_6150") + " 0";
            GroupBoxs[1] = commandFromUxml.Query<GroupBox>("ButtonGroup1"); ;
            GroupBoxs[1].text = EditorLocalize.LocalizeText("WORD_6150") + " 1";
            
            //よく使うボタンを先頭へ
            Toggle OptionalCommandCheck = commandFromUxml.Query<Toggle>("OptionalCommandCheck");
            OptionalCommandCheck.RegisterValueChangedCallback(v =>
            {
                RPGMakerDefaultConfigSingleton.instance.EventComanedOptionalCommandsFast = v.newValue;
                _CommandWindowList.ToolBerOptionalCommandsFast(v.newValue);
            });

            OptionalCommandCheck.value = RPGMakerDefaultConfigSingleton.instance.EventComanedOptionalCommandsFast;

            CommandsInfoLoad();
            CommandSetup();
            ChekcDeleteButton();
            return _root;
        }

        /// <summary>
        /// コマンドデータをロード
        /// </summary>
        void CommandsInfoLoad() 
        {
            var json = "{}";
            try
            {
               json = File.ReadAllText(_FileName);
            }
            catch (Exception)
            {
            }
            _CommandDataList = JsonHelper.FromJsonArray<CommandData>(json);

            foreach (var commandData in _CommandDataList)
            {
                int Index = commandData.m_GroupID;
                string CommandName = commandData.m_Name;
                //保存データ
                if (_CommandList.ContainsKey(Index) == false)
                {
                    //新規データ
                    _CommandList.Add(Index, new List<string>());
                    _CommandList[Index].Add(CommandName);
                }
                else
                {
                    //既存データ
                    if (_CommandList[Index].Contains(CommandName) == false)
                    {
                        _CommandList[Index].Add(CommandName);
                    }
                }
            }
        }

        /// <summary>
        /// コマンドデータを保存
        /// </summary>
        void CommandsInfoSave()
        {
            _CommandDataList.Clear();

            int Index = 0;

            foreach (var cmd in _CommandList)
            {
                foreach (var Command in cmd.Value)
                {
                    CommandData commandData = new CommandData();

                    commandData.m_GroupID = Index;
                    commandData.m_Name = Command;

                    _CommandDataList.Add(commandData);
                }
                Index++;
            }
            var stJson = JsonHelper.ToJsonArray(_CommandDataList);
            File.WriteAllText(_FileName, stJson);
        }

        /// <summary>
        /// 保存データからボタンを復帰
        /// </summary>
        private void CommandSetup() 
        {
            bool bGroup = false;
            //保存データから削除
            foreach (var CommandList in _CommandList)
            {
                foreach (var Item in CommandList.Value)
                {
                    if (_ButtonList.Contains(Item))
                    {
                        var Index = _ButtonList.IndexOf(Item);
                        //保存データからボタンを復帰
                        ButtonAddGroup(Index, bGroup);
                    }
                }
                bGroup = true;
            }
        }

        /// <summary>
        /// イベントコマンドのボタンリストを生成する
        /// </summary>
        private void ButtonAddSetting() 
        {
            List<string> ButtonList = new List<string>();
            List<string> ButtonCommandList = new List<string>();

            foreach (var data in _EventCommandDataList)
            {
                ButtonList.Add(data.Value._Name);
                ButtonCommandList.Add(data.Key);
            }
            _ButtonPopupEventADD = new PopupFieldBase<string>(ButtonList,0);
            _ButtonList = ButtonCommandList;
        }

        /// <summary>
        /// イベントコマンドのボタンを削除する
        /// </summary>
        private void ButtonDel()
        {
            List<Toggle> ButtonList = new List<Toggle>();

            bool bCheckDel = false;
            foreach (var toggle in _ToggleList)
            {
                if (((Toggle) toggle).value)
                {
                    bCheckDel = true;
                    break;
                }
            }
            //削除するボタンなし
            if (bCheckDel == false) return;

            //登録上限注意
            if (EditorUtility.DisplayDialog(EditorLocalize.LocalizeText("WORD_6150"), EditorLocalize.LocalizeText("WORD_6153"), EditorLocalize.LocalizeText("WORD_2900"), EditorLocalize.LocalizeText("WORD_1530")) == false)
            {
                return;
            }

            int Index = 0;
            foreach (var toggle in _ToggleList)
            {
                if ( ((Toggle)toggle).value)
                {
                    ButtonList.Add(((Toggle)toggle));
                    foreach (var GroupBox in GroupBoxs)
                    {
                        if (GroupBox.Query<Toggle>(name: toggle.name).First() != null)
                        {
                            GroupBox.Remove(toggle);
                            break;
                        }
                        Index++;
                    }
                }
            }

            foreach (var toggle in ButtonList)
            {
                _ToggleList.Remove(toggle);
                toggle.Clear();

                //保存データから削除
                foreach (var CommandList in _CommandList)
                {
                    if (CommandList.Value.Contains(toggle.name))
                    {
                        CommandList.Value.Remove(toggle.name);
                    }
                }
                CommandsInfoSave();
            }
        }

        /// <summary>
        /// グループにボタンを追加する
        /// </summary>
        /// <param name="Index"></param>
        /// <param name="bNew"></param>
        public void ButtonAddGroup(string ButtonName, bool bGroup) 
        {
            int Index = -1;
            //
            if (_ButtonList.Contains(ButtonName) == false) return;

            Index = _ButtonList.IndexOf(ButtonName);

            ButtonAddGroup(Index, bGroup, true);
        }

        /// <summary>
        /// 既に登録済みかをチェックする
        /// </summary>
        /// <param name="ButtonName"></param>
        /// <returns></returns>
        public bool ButtonAddCheck(string ButtonName) 
        {
            bool bCheck = false;
            //重複登録は、しない
            foreach (var Group in GroupBoxs)
            {
                var El = Group.Query<Toggle>(name: ButtonName).First();
                if (El != null)
                {
                    bCheck = true;
                    break;
                }
            }
            return bCheck;
        }
        /// <summary>
        /// グループがボタン登録上限かチェックする
        /// </summary>
        /// <param name="Group"></param>
        /// <returns></returns>
        public bool ChekcButtonGroupMax(int GroupIndex) {
            bool bCheck = false;
            //20ボタン以上は一つのグループに登録出来ない
            if (GroupBoxs[GroupIndex].childCount > 20)
            {
                bCheck = true;
            }
            return bCheck;
        }
        /// <summary>
        /// グループにボタンを追加する
        /// </summary>
        /// <param name="Index"></param>
        /// <param name="bNew"></param>
        private void ButtonAddGroup(int Index,bool bGroup,bool bNew = false) 
        {
            int GroupIndex = 0;
            if (bGroup)
            {
                GroupIndex = 1;
            }
            //20ボタン以上は一つのグループに登録出来ない
            if (GroupBoxs[GroupIndex].childCount > 20)
            {
                //新規登録時のみ
                if (bNew == true)
                {
                    //登録上限注意
                    EditorUtility.DisplayDialog(EditorLocalize.LocalizeText("WORD_6150"), EditorLocalize.LocalizeText("WORD_6152"), EditorLocalize.LocalizeText("WORD_2900"));
                }
                return;
            }

            var Data = _EventCommandDataList[_ButtonList[Index]];
            //重複登録は、しない
            foreach (var Group in GroupBoxs)
            {
                var El = Group.Query<Toggle>(name: _ButtonList[Index]).First();
                if (El != null)
                {
                    //既に登録済みです注意!
                    EditorUtility.DisplayDialog(EditorLocalize.LocalizeText("WORD_6150"), EditorLocalize.LocalizeText("WORD_6156"), EditorLocalize.LocalizeText("WORD_2900"));
                    return;
                }
            }

            Action action = () => 
            {
                var code = (int) Data._Event;
                _CallBack(code.ToString());
                _CallWidowClose();
            };

            Toggle toggle = (Toggle) CreateButton(_ButtonList[Index], Data._Name, action);

            toggle.RegisterValueChangedCallback<bool>( v =>
            {
                ChekcDeleteButton();
            });

            GroupBoxs[GroupIndex].Add(toggle);

            _ToggleList.Add(toggle);


            if (bNew == true)
            {
                //保存データ
                if(_CommandList.ContainsKey(GroupIndex) == false)
                {
                    //新規データ
                    _CommandList.Add(GroupIndex, new List<string>());
                    _CommandList[GroupIndex].Add(_ButtonList[Index]);
                }
                else
                {
                    //既存データ
                    if (_CommandList[GroupIndex].Contains(_ButtonList[Index]) == false)
                    {
                        _CommandList[GroupIndex].Add(_ButtonList[Index]);
                    }
                }
                CommandsInfoSave();
            }
        }

        /// <summary>
        /// チェックボタン付きのボタンを生成する
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="TextName"></param>
        /// <param name="clicked"></param>
        /// <returns></returns>
        private VisualElement CreateButton( string Name , string TextName, Action clicked) 
        {
            Button button = new Button();
            button.text = TextName;
            button.name = Name;
            button.clickable.clicked += clicked;

            button.style.height = 20;
            Toggle toggle = new Toggle();
            toggle.name = Name;

            toggle.style.position = Position.Relative;
            toggle.style.width = 180;
            toggle.style.height = 20;
            toggle.style.paddingTop = 0;
            toggle.style.paddingBottom = 0;

            button.style.position = Position.Absolute;
            button.style.left = 14;
            button.style.right = 0;
            button.style.width = 160;

            button.style.top = -2;
            button.style.paddingTop = 0;
            button.style.paddingBottom = 0;

            toggle.Add(button);

            return toggle;
        }

        /// <summary>
        /// 削除ボタンの状態をチェックする
        /// </summary>
        private void ChekcDeleteButton() 
        {
            bool bCheckDel = false;
            foreach (var toggle in _ToggleList)
            {
                if (((Toggle) toggle).value)
                {
                    bCheckDel = true;
                    break;
                }
            }
            _ButtonDelete.SetEnabled(bCheckDel);
        }

    }
}