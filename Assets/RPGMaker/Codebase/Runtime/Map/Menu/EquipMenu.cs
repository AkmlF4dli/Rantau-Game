using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.SystemSetting;
using RPGMaker.Codebase.Runtime.Common;
using RPGMaker.Codebase.Runtime.Map.Item;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TextMP = TMPro.TextMeshProUGUI;

namespace RPGMaker.Codebase.Runtime.Map.Menu
{
    /// <summary>
    /// 装備メニュー
    /// </summary>
    public class EquipMenu : WindowBase
    {
        /// <summary>
        /// Windowステータス
        /// </summary>
        public enum Window
        {
            STATUS = 0,
            EQUIP
        }
        /// <summary>
        /// アクター
        /// </summary>
        private string _actorId = "1";
        /// <summary>
        /// 説明欄
        /// </summary>
        [SerializeField] private TextMP _description1;
        [SerializeField] private TextMP _description2;
        /// <summary>
        /// 変更する装備一覧画面
        /// </summary>
        [SerializeField] private GameObject _equipImageGameObject;
        /// <summary>
        /// 装備変更画面
        /// </summary>
        private EquipmentEquips _equipmentEquips;
        /// <summary>
        /// 装備対象のキャラクター
        /// </summary>
        private EquipmentProfile _equipmentProfile;
        /// <summary>
        /// MenuBase
        /// </summary>
        private MenuBase _menuBase;
        /// <summary>
        /// RuntimeActorDataModel
        /// </summary>
        private RuntimeActorDataModel _runtimeActorDataModel;
        /// <summary>
        /// 装備対象のキャラクターのGameObject
        /// </summary>
        [SerializeField] private GameObject _statusWindowGameObject = null;
        /// <summary>
        /// 装備、最強装備、外すのコマンドエリア
        /// </summary>
        [SerializeField] private GameObject _topMenusArea = null;
        /// <summary>
        /// 現在のWindowステータス
        /// </summary>
        private Window _window;
        /// <summary>
        /// 装備、最強装備、外すのボタン
        /// </summary>
        private Button[] _buttons;
        
        //表示させるactorがPartyの何番目かを保持する
        private int _partyNumber = 0;

        /// <summary>
        /// 初期化処理
        /// </summary>
        /// <param name="base"></param>
        /// <param name="actorId"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void Init(MenuBase @base, string actorId) {
#else
        public async Task Init(MenuBase @base, string actorId) {
#endif
            _menuBase = @base;
            _actorId = actorId;
            _equipmentProfile = _statusWindowGameObject.GetComponent<EquipmentProfile>();
            _equipmentEquips = _equipImageGameObject.GetComponent<EquipmentEquips>();
            _runtimeActorDataModel = _GetActorInformation(_actorId);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _equipmentProfile.ChangeCharactor(_runtimeActorDataModel);
            _equipmentEquips.EquipsTypeDisplay(_runtimeActorDataModel, this);
#else
            await _equipmentProfile.ChangeCharactor(_runtimeActorDataModel);
            await _equipmentEquips.EquipsTypeDisplay(_runtimeActorDataModel, this);
#endif
            _window = Window.STATUS;
            MessageDisplay("");
            TopMenusWords();
            
            _buttons = _topMenusArea.GetComponentsInChildren<Button>();
            foreach (var select in _buttons)
            {
                SetupButtonEvent(select);
            }
            _buttons[0].Select();
            for (var i = 0; i < _buttons.Length; i++)
            {
                var nav = _buttons[i].navigation;
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnLeft = _buttons[i == 0 ? _buttons.Length - 1 : i - 1];
                nav.selectOnRight = _buttons[(i + 1) % _buttons.Length];

                _buttons[i].navigation = nav;
                _buttons[i].targetGraphic = _buttons[i].transform.Find("Highlight").GetComponent<Image>();

                if (i != 0)
                {
                    //最強装備と装備を外すの効果音は決定音ではないため、共通部品では鳴動しない
                    _buttons[i].GetComponent<WindowButtonBase>().SetSilentClick(true);
                }
            }
            
            //共通のウィンドウの適応
            Init();

            //フォーカス設定
            ChangeFocusList(true);
        }

        /// <summary>
        /// キャラクターの情報部分取得
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private RuntimeActorDataModel _GetActorInformation(string id) {
            var saveData = DataManager.Self().GetRuntimeSaveDataModel();
            for (var i = 0; i < saveData.runtimeActorDataModels.Count; i++)
                if (saveData.runtimeActorDataModels[i].actorId == id)
                    return saveData.runtimeActorDataModels[i];

            return null;
        }

        /// <summary>
        /// リストのフォーカス制御
        /// </summary>
        private void ChangeFocusList(bool initialize) {
            if (_window == Window.STATUS)
            {
                //第一階層のメニューを選択可能とする
                int num = 0;
                for (var i = 0; i < _buttons.Length; i++)
                {
                    _buttons[i].GetComponent<WindowButtonBase>().SetEnabled(true);
                    if (_buttons[i].GetComponent<WindowButtonBase>().IsHighlight())
                        num = i;
                }
                //現在の状態に応じてフォーカス設定位置を変更する
                if (!initialize)
                    _buttons[num].GetComponent<Button>().Select();
                else
                    _buttons[0].GetComponent<Button>().Select();
            }
            else 
            {
                //第一階層のメニューは選択不可とする
                for (var i = 0; i < _buttons.Length; i++)
                {
                    _buttons[i].GetComponent<WindowButtonBase>().SetEnabled(false);
                }
            }
        }

        /// <summary>
        /// ステータスの反映
        /// </summary>
        /// <param name="parameters"></param>
        public void StatusPlus(List<int> parameters) {
            _equipmentProfile.StatusPlus(parameters);
        }

        /// <summary>
        /// 右のステータス変更
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="typeId"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void EqValuesDisplay(List<int> parameters, string typeId, int index) {
            _equipmentProfile.EqValuesDisplay(parameters, typeId, index);
        }
#else
        public async Task EqValuesDisplay(List<int> parameters, string typeId, int index) {
            await _equipmentProfile.EqValuesDisplay(parameters, typeId, index);
        }
#endif

        /// <summary>
        /// メッセージの表示を行う
        /// </summary>
        /// <param name="message"></param>
        public void MessageDisplay(string message) {
            if (string.IsNullOrEmpty(message))
            {
                message = "";
            }
            var width = _description1.transform.parent.GetComponent<RectTransform>().sizeDelta +
                        _description1.GetComponent<RectTransform>().sizeDelta;
            _description1.text = "";
            _description2.text = "";
            if (message.Contains("\\n"))
            {
                var textList = message.Split("\\n");
                _description1.text = textList[0];
                if (width.x >= _description1.preferredWidth)
                {
                    _description2.text = textList[1];
                    return;
                }
                message = textList[0];
            }
            var isNextLine = false;
            _description1.text = "";
            _description2.text = "";
            for (int i = 0; i < message.Length; i++)
            {
                if (!isNextLine)
                {
                    _description1.text += message[i];
                    if (width.x <= _description1.preferredWidth)
                    {
                        var lastChara = _description1.text.Substring(_description1.text.Length - 1);
                        _description2.text += lastChara;
                        _description1.text = _description1.text.Remove(_description1.text.Length - 1);
                        isNextLine = true;
                    }
                }
                else
                {
                    _description2.text += message[i];
                    if (width.x <= _description2.preferredWidth)
                    {
                        _description2.text = _description2.text.Remove(_description2.text.Length - 1);
                        break;
                    }

                }

            }
        }

        /// <summary>
        /// 装備変更
        /// </summary>
        /// <param name="equipTypeId"></param>
        /// <param name="itemId"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void EquipChange( string itemId, int equipIndex) {
#else
        public async Task EquipChange( string itemId, int equipIndex) {
#endif
            //装備変更を試みる
            var equipTypes = DataManager.Self().GetSystemDataModel().equipTypes;
            SystemSettingDataModel.EquipType equipTypeData = null;
            for (int i = 0; i < equipTypes.Count; i++)
                if (equipTypes[i].id == _runtimeActorDataModel.equips[equipIndex].equipType)
                {
                    equipTypeData = equipTypes[i];
                    break;
                }
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            ItemManager.ChangeEquipment(_runtimeActorDataModel, equipTypeData, itemId, equipIndex);
#else
            await ItemManager.ChangeEquipment(_runtimeActorDataModel, equipTypeData, itemId, equipIndex);
#endif

            //ここで装備窓が閉じられステータスが開かれる
            _equipmentEquips.EquipsSelectClose();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            ReLoad();
#else
            await ReLoad();
#endif
            _window = Window.EQUIP;
            _equipmentEquips.SelectedEquip();

            //装備音
            SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_SE, DataManager.Self().GetSystemDataModel().soundSetting.equip);
            SoundManager.Self().PlaySe();
        }

        /// <summary>
        /// 画面のリロードを行う
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void ReLoad() {
#else
        private async Task ReLoad() {
#endif
            //装備を変更した場合、GameActorにも反映する

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var actors = DataManager.Self().GetGameParty().Actors;
#else
            var actors = await DataManager.Self().GetGameParty().GetActors();
#endif
            for (int i = 0; i < actors.Count; i++)
                if (actors[i].ActorId == _actorId)
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    actors[i].ResetActorData();
#else
                    await actors[i].ResetActorData();
#endif

            //ステータスに武器のパラメーターを加算するので武器が後
            _runtimeActorDataModel = _GetActorInformation(_actorId);
            //キャラクターステータスの再描画部分
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _equipmentProfile.ChangeCharactor(_runtimeActorDataModel);
#else
            await _equipmentProfile.ChangeCharactor(_runtimeActorDataModel);
#endif
            //武器防具装備の再描画部分
            //更新部分
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _equipmentEquips.EquipsTypeDisplay(_runtimeActorDataModel, this);
#else
            await _equipmentEquips.EquipsTypeDisplay(_runtimeActorDataModel, this);
#endif
            //_window = Window.EQUIP;
            //フォーカス設定
            ChangeFocusList(false);

        }

        /// <summary>
        /// 装備ボタンのクリック処理
        /// </summary>
        /// <param name="obj"></param>
        public void ClickEquipButton(GameObject obj) {
            _equipmentEquips.SelectedEquip();
            //フォーカス設定
            _window = Window.EQUIP;
            ChangeFocusList(false);
        }

        /// <summary>
        /// ボタンに対するイベント設定
        /// </summary>
        /// <param name="selectable"></param>
        private void SetupButtonEvent(Button selectable)
        {
            EventTrigger trigger = selectable.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = selectable.gameObject.AddComponent<EventTrigger>();
            }
            trigger.triggers.Clear();

            EventTrigger.Entry selectEntry = new EventTrigger.Entry();
            selectEntry.eventID = EventTriggerType.Select;

            EventTrigger.Entry deselectEntry = new EventTrigger.Entry();
            deselectEntry.eventID = EventTriggerType.Deselect;

            trigger.triggers.Add(selectEntry);
            trigger.triggers.Add(deselectEntry);
        }

        /// <summary>
        /// 装備品を外す
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void RemoveEquip(int equipIndex) {
#else
        public async Task RemoveEquip(int equipIndex) {
#endif
            var equipTypes = DataManager.Self().GetSystemDataModel().equipTypes;
            SystemSettingDataModel.EquipType equipTypeData = null;
            for (int i = 0; i < equipTypes.Count; i++)
                if (equipTypes[i].id == _runtimeActorDataModel.equips[equipIndex].equipType)
                {
                    equipTypeData = equipTypes[i];
                    break;
                }
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            ItemManager.RemoveEquipment(_runtimeActorDataModel, equipTypeData, equipIndex);
#else
            await ItemManager.RemoveEquipment(_runtimeActorDataModel, equipTypeData, equipIndex);
#endif

            //ここで装備窓が閉じられステータスが開かれる
            _equipmentEquips.EquipsSelectClose();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            ReLoad();
#else
            await ReLoad();
#endif
            _window = Window.EQUIP;
            _equipmentEquips.SelectedEquip();

            //装備音
            SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_SE, DataManager.Self().GetSystemDataModel().soundSetting.equip);
            SoundManager.Self().PlaySe();
        }

        /// <summary>
        /// 装備品を全て外す
        /// </summary>
        public void RemoveEquips() {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            _ = RemoveEquipsAsync();
        }
        public async Task RemoveEquipsAsync() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            ItemManager.RemoveAllEquipment(_runtimeActorDataModel);
#else
            await ItemManager.RemoveAllEquipment(_runtimeActorDataModel);
#endif
            //装備音
            SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_SE, DataManager.Self().GetSystemDataModel().soundSetting.equip);
            SoundManager.Self().PlaySe();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            ReLoad();
#else
            await ReLoad();
#endif
        }

        /// <summary>
        /// 最強装備を行う
        /// </summary>
        public void Strongest() {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            _ = StrongestAsync();
        }
        public async Task StrongestAsync() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            ItemManager.StrongestEquipment(_runtimeActorDataModel);
#else
            await ItemManager.StrongestEquipment(_runtimeActorDataModel);
#endif
            //装備音
            SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_SE, DataManager.Self().GetSystemDataModel().soundSetting.equip);
            SoundManager.Self().PlaySe();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            ReLoad();
#else
            await ReLoad();
#endif
        }

        /// <summary>
        /// Windowステータス設定
        /// </summary>
        /// <param name="window"></param>
        public void SetWindow(Window window) {
            _window = window;
            //フォーカス設定
            ChangeFocusList(true);
        }

        /// <summary>
        /// Windowステータス取得
        /// </summary>
        /// <returns></returns>
        public Window GetWindowStatus() {
            return _window;
        }

        /// <summary>
        /// 戻る処理
        /// </summary>
        public void BackMenu() {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            _ = BackMenuAsync();
        }
        public async Task BackMenuAsync() {
#endif
            switch (_window)
            {
                case Window.STATUS:
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _menuBase.BackMenu();
#else
                    await _menuBase.BackMenuAsync();
#endif
                    break;
                case Window.EQUIP:
                    var equipTypes = DataManager.Self().GetSystemDataModel().equipTypes;
                    _equipmentEquips.transform.Find("Scroll View").gameObject.SetActive(false);
                    for (var i = 0; i < equipTypes.Count; i++)
                        _equipmentEquips.transform.Find("Scroll View/Viewport/Content/" + equipTypes[i].id)?.gameObject
                            .SetActive(false);

                    if ( _equipImageGameObject.transform.Find("EquipTypeScrollView").gameObject.activeSelf)
                    {
                        //フォーカス設定
                        _window = Window.STATUS;
                        _equipmentEquips.BackEquip();
                        ChangeFocusList(false);
                    }
                    else
                    {
                        _equipImageGameObject.transform.Find("EquipTypeScrollView").gameObject.SetActive(true);
                        _equipmentEquips.BackEquip();
                        _equipmentProfile.EqValueInit();
                        _equipmentEquips.RemoveFocus();
                    }
                    break;
            }
        }

        /// <summary>
        /// 装備、最強装備、外すの文言を用語から設定する
        /// </summary>
        private void TopMenusWords() {
            for (var i = 1; i <= _topMenusArea.transform.childCount; i++)
            {
                TextMP topMenusText = _topMenusArea.transform.Find("Item" + i + "/Name").GetComponent<TextMP>();
                switch (i)
                {
                    case 1:
                        topMenusText.text = TextManager.equip;
                        break;
                    case 2:
                        topMenusText.text = TextManager.optimize;
                        break;
                    case 3:
                        topMenusText.text = TextManager.clear;
                        break;
                }
            }
        }

        /// <summary>
        /// キャラクターIDの制御
        /// </summary>
        /// <param name="value"></param>
        public void CharacterChange(string value) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            _ = CharacterChangeAsync(value);
        }
        public async Task CharacterChangeAsync(string value) {
#endif
            var saveData = DataManager.Self().GetRuntimeSaveDataModel();
            if (saveData.runtimePartyDataModel.actors.Count == 1) return;
            
            for (var i = 0; i < saveData.runtimePartyDataModel.actors.Count; i++)
                if (saveData.runtimePartyDataModel.actors[i] == _actorId)
                    _partyNumber = i;

            if (value == "plus")
            {
                _partyNumber++;
            }
            if (value == "minus")
            {
                _partyNumber--;
            }
            var partyCount = saveData.runtimePartyDataModel.actors.Count;
            _partyNumber = (_partyNumber + partyCount) % partyCount;

            _actorId = saveData.runtimePartyDataModel.actors[_partyNumber];

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            ReLoad();
#else
            await ReLoad();
#endif
        }
    }
}