using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.SystemSetting;
using RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement;
using RPGMaker.Codebase.CoreSystem.Service.RuntimeDataManagement;
using RPGMaker.Codebase.Runtime.Common;
using RPGMaker.Codebase.Runtime.Common.Enum;
using RPGMaker.Codebase.Runtime.Map.Component.Character;
using RPGMaker.Codebase.Runtime.Map.Item;
using RPGMaker.Codebase.Runtime.Title;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RPGMaker.Codebase.Runtime.Map.Menu
{
    /// <summary>
    ///     タイトル画面またはゲームのメインメニューから開くセーブロード画面
    /// </summary>
    public class SaveMenu : WindowBase
    {
        /// <summary>
        ///     セーブロード画面で行う操作
        /// </summary>
        public enum Operation
        {
            /// <summary>ゲームデータのセーブ</summary>
            Save,

            /// <summary>セーブデータのロード</summary>
            Load
        }

        public const            int             SAVE_DATA_NUM_MAX = 20;
        [SerializeField] private TextMeshProUGUI _descriptionTitle = null;
        [SerializeField] private Button          _downArrowButton  = null;
        private                  RectTransform   _itemPrefabRect;
        private                  MenuBase        _menuBase;
        private                  Operation       _operation      = Operation.Load;
        [SerializeField] private SaveItem        _saveItem       = null;
        [SerializeField] private RectTransform   _saveItemParent = null;

        private readonly List<SaveItem> _saveItems = new List<SaveItem>();

        [SerializeField] private ScrollRect    _scrollRect     = null;
        [SerializeField] private Button        _upArrowButton  = null;

        private bool _LoadingFlg = false;//セーブデータのロード中フラグ U274

        public override void Update() {
            base.Update();

            if (InputHandler.OnDown(Common.Enum.HandleType.Back) && gameObject.activeSelf)
            {
                SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_SE, systemSettingDataModel.soundSetting.cancel);
                SoundManager.Self().PlaySe();

                // タイトル画面で開いている場合の処理
                if (_operation == Operation.Load)
                {
                    var titleController = GetComponentInParent<TitleController>();
                    titleController.CloseContinue();
                }
            }
        }

        public void Init(WindowBase manager) {
            base.Init();

            switch (SceneManager.GetActiveScene().name)
            {
                //タイトル画面の場合はロード画面を表示
                case "Title":
                    _descriptionTitle.text = TextManager.loadMessage;
                    _operation = Operation.Load;
                    break;
                //その他ケースではセーブ画面を表示
                default:
                    _descriptionTitle.text = TextManager.saveMessage;
                    _operation = Operation.Save;
                    break;
            }

            _menuBase = manager as MenuBase;
            _itemPrefabRect = _saveItem.GetComponent<RectTransform>();

            //キー登録
            _gameState = GameStateHandler.GameState.MENU;

            if (MapEventExecutionController.Instance.IsPauseEvent())
            {
                InputDistributor.AddInputHandler(GameStateHandler.GameState.EVENT, HandleType.Back, Back, "SaveMenuBack");
                InputDistributor.AddInputHandler(GameStateHandler.GameState.EVENT, HandleType.RightClick, Back, "SaveMenuBack");
            }

            _LoadingFlg = false;

            // U316 スクロールスピード
            _scrollRect.scrollSensitivity = 20.0f;

            //画面が生成されるまで1フレーム待ち
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            TimeHandler.Instance.AddTimeActionFrame(1, SetupSaveDataItem, false);
#else
            TimeHandler.Instance.AddTimeActionFrame(1, () => { _ = SetupSaveDataItem(); }, false);
#endif
        }

        public new void Back() {
            if (MapEventExecutionController.Instance.IsPauseEvent() && MenuBase.IsEventToSave)
            {
                InputDistributor.RemoveInputHandler(GameStateHandler.GameState.EVENT, HandleType.Back, Back, "SaveMenuBack");
                InputDistributor.RemoveInputHandler(GameStateHandler.GameState.EVENT, HandleType.RightClick, Back, "SaveMenuBack");
                gameObject.SetActive(false);

                //U336 キャンセルSE追加
                SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_SE, systemSettingDataModel.soundSetting.cancel);
                SoundManager.Self().PlaySe();

                if (_menuBase != null)
                    _menuBase?.AllClose();
                else
                    ResumeEvent();

                MenuBase.IsEventToSave = false;
                return;
            }

            _menuBase?.BackMenu();
        }

        private void ResumeEvent() {
            MapEventExecutionController.Instance.ResumeEvent();
        }

        /// <summary>
        ///     セーブデータの総数を取得する
        /// </summary>
        /// <returns>パスにfileが含まれるjsonファイルの個数</returns>
        public int GetSaveQuantity() {
            var runtimeDataManagementService = new RuntimeDataManagementService();
            return runtimeDataManagementService.GetSaveFileCount();
        }
        
        public bool IsAutoSaveFile() {
            var runtimeDataManagementService = new RuntimeDataManagementService();
            return runtimeDataManagementService.IsAutoSaveFile();
        }

        /// <summary>
        ///     セーブデータの配置
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void SetupSaveDataItem() {
#else
        private async Task SetupSaveDataItem() {
#endif
            _saveItems.ForEach(v => Destroy(v.gameObject));
            _saveItems.Clear();
            transform.Find("MenuArea/FileLoad/Scroll View").gameObject.SetActive(true);

            // オートセーブ用の項目を作成
            if (systemSettingDataModel.optionSetting.enabledAutoSave == 1)
            {
                var saveData = GetSaveData("file0");
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var saveItem = CreateSaveItem(saveData, 0);
#else
                var saveItem = await CreateSaveItem(saveData, 0);
#endif
                _saveItems.Add(saveItem);
            }

            // ファイル1~20のセーブデータ項目を作成
            for (var i = 1; i <= SAVE_DATA_NUM_MAX; i++)
            {
                var saveData = GetSaveData($"file{i}");
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var saveItem = CreateSaveItem(saveData, i);
#else
                var saveItem = await CreateSaveItem(saveData, i);
#endif
                _saveItems.Add(saveItem);
            }

            //十字キーでの操作登録
            for (var i = 0; i < _saveItems.Count; i++)
            {
                var nav = _saveItems[i].ItemButton.navigation;
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnUp = _saveItems[i == 0 ? _saveItems.Count - 1 : i - 1].ItemButton;
                nav.selectOnDown = _saveItems[(i + 1) % _saveItems.Count].ItemButton;
                _saveItems[i].ItemButton.navigation = nav;
            }

            _saveItemParent.sizeDelta =
                new Vector2(_saveItemParent.sizeDelta.x, (_itemPrefabRect.rect.height + 20) * _saveItems.Count);
            _saveItems[0].ItemButton.Select();
        }

        /// <summary>
        ///     セーブデータ項目の作成
        /// </summary>
        /// <param name="runtimeSaveDataModel">参照するセーブデータ</param>
        /// <param name="number">セーブデータの番号、0でオートセーブ用</param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private SaveItem CreateSaveItem(RuntimeSaveDataModel saveData, int number) {
#else
        private async Task<SaveItem> CreateSaveItem(RuntimeSaveDataModel saveData, int number) {
#endif
            var saveItem = Instantiate(_saveItem, _saveItemParent.transform);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            saveItem.Init(saveData, number, _operation);
#else
            await saveItem.Init(saveData, number, _operation);
#endif

            //OnFocus、OnClick追加
            saveItem.GetComponent<WindowButtonBase>().OnFocus = new UnityEngine.Events.UnityEvent();
            saveItem.GetComponent<WindowButtonBase>().OnFocus.AddListener(() =>
            {
                _selectedItem = saveItem.gameObject;
                CheckArrowButton();
            });
            saveItem.GetComponent<WindowButtonBase>().OnClick = new Button.ButtonClickedEvent();
            saveItem.GetComponent<WindowButtonBase>().OnClick.AddListener(() =>
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                OnSaveItemClicked(saveItem);
#else
                _ = OnSaveItemClicked(saveItem);
#endif
            });

            //スクロール制御の追加
            saveItem.GetComponent<WindowButtonBase>().ScrollView = transform.Find("MenuArea/FileLoad/Scroll View").gameObject;
            saveItem.GetComponent<WindowButtonBase>().Content = transform.Find("MenuArea/FileLoad/Scroll View/Viewport/Content").gameObject;

            //ロード、セーブについてはブザー音は本クラス内で鳴動させる
            saveItem.GetComponent<WindowButtonBase>().SetSilentClick(true);

            saveItem.gameObject.SetActive(true);
            return saveItem;
        }

        /// <summary>
        ///     ローカルに保存されているセーブデータの取得
        /// </summary>
        /// <param name="fileName">ファイル名</param>
        /// <returns>取得成功でセーブデータを返す。失敗でnull</returns>
        private RuntimeSaveDataModel GetSaveData(string fileName) {
            if (string.IsNullOrEmpty(fileName)) return null;

            //セーブデータを手動で編集されたなどの理由で、JSONとして不正な状態になっているセーブデータが
            //存在した場合に読み飛ばすため、try catch で括る
            RuntimeSaveDataModel retData = null;
            try
            {
                var runtimeDataManagementService = new RuntimeDataManagementService();
                var str = runtimeDataManagementService.LoadSaveData(fileName);
                if (str == null) return null;

                // 型を合わせる
                var inputString = new TextAsset(str);
                retData = JsonUtility.FromJson<RuntimeSaveDataModel>(inputString.text);
            } catch (Exception) {}

            return retData;
        }

        /// <summary>
        ///     セーブデータ項目をクリックした際に呼び出すコールバック
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void OnSaveItemClicked(SaveItem clickedItem) {
#else
        private async Task OnSaveItemClicked(SaveItem clickedItem) {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            ButtonProcessing(null, clickedItem.gameObject);
#else
            await ButtonProcessing(null, clickedItem.gameObject);
#endif

            if (_operation == Operation.Save)
            {
                if (clickedItem.SaveFileNo != 0)
                {
                    //セーブした回数をインクリメント
                    DataManager.Self().GetRuntimeSaveDataModel().runtimeSystemConfig.saveCount++;

                    //現在再生されているBGMを保存する
                    if (SoundManager.Self().IsNowBgmPlaying() && SoundManager.Self().GetBgmSound() != null)
                    {
                        var sound = SoundManager.Self().GetBgmSound();
                        DataManager.Self().GetRuntimeSaveDataModel().runtimeSystemConfig.bgmOnSave.name = sound.name;
                        DataManager.Self().GetRuntimeSaveDataModel().runtimeSystemConfig.bgmOnSave.volume = sound.volume;
                        DataManager.Self().GetRuntimeSaveDataModel().runtimeSystemConfig.bgmOnSave.pitch = sound.pitch;
                        DataManager.Self().GetRuntimeSaveDataModel().runtimeSystemConfig.bgmOnSave.pan = sound.pan;
                    }
                    else
                    {
                        DataManager.Self().GetRuntimeSaveDataModel().runtimeSystemConfig.bgmOnSave.name = "";
                    }
                    
                    //現在再生されているBGSを保存する
                    if (SoundManager.Self().IsNowBgsPlaying() && SoundManager.Self().GetBgsSound() != null)
                    {
                        var sound = SoundManager.Self().GetBgsSound();
                        DataManager.Self().GetRuntimeSaveDataModel().runtimeSystemConfig.bgsOnSave.name = sound.name;
                        DataManager.Self().GetRuntimeSaveDataModel().runtimeSystemConfig.bgsOnSave.volume = sound.volume;
                        DataManager.Self().GetRuntimeSaveDataModel().runtimeSystemConfig.bgsOnSave.pitch = sound.pitch;
                        DataManager.Self().GetRuntimeSaveDataModel().runtimeSystemConfig.bgsOnSave.pan = sound.pan;
                    }
                    else
                    {
                        DataManager.Self().GetRuntimeSaveDataModel().runtimeSystemConfig.bgsOnSave.name = "";
                    }

                    // 現在のマップ配置データを保存
                    {
                        RuntimeOnMapDataModel RuntimeOnMapDataModel = new RuntimeOnMapDataModel();
                        // 現在のマップイベント
                        foreach (var ev in MapEventExecutionController.Instance.GetEvents())
                            RuntimeOnMapDataModel.onMapDatas.Add(new RuntimeOnMapDataModel.OnMapData().Create(ev));
                        // パーティ
                        for (int i = 0; i < MapManager.PartyOnMap?.Count; i++)
                            RuntimeOnMapDataModel.onMapDatas.Add(new RuntimeOnMapDataModel.OnMapData().Create(MapManager.GetPartyGameObject(i).GetComponent<ActorOnMap>(), i + 1));
                        // プレイヤー
                        RuntimeOnMapDataModel.onMapDatas.Add(
                            new RuntimeOnMapDataModel.OnMapData().Create(MapManager.GetCharacterGameObject().GetComponent<ActorOnMap>(), 0));

                        DataManager.Self().GetRuntimeSaveDataModel().RuntimeOnMapDataModel = RuntimeOnMapDataModel;
                    }

                    // 現在のマップ配置データを保持（イベント外）
                    {
                        //タイマー
                        float timer = -1;
                        var obj = HudDistributor.Instance.NowHudHandler().GetGameTimer();
                        if (obj != null)
                        {
                            timer = obj.GetGameTimer();
                        }
                        DataManager.Self().GetRuntimeSaveDataModel().runtimeScreenDataModel.timer = timer;

                        //ピクチャ
                        HudDistributor.Instance.NowHudHandler().SavePicture();
                    }

                    // 有効な項目にセーブを実施
                    var runtimeDataManagementService = new RuntimeDataManagementService();
                    var data = DataManager.Self().GetRuntimeSaveDataModel();
                    runtimeDataManagementService.SaveSaveData(data, clickedItem.SaveFileNo);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    clickedItem.Refresh(data);
#else
                    await clickedItem.Refresh(data);
#endif

                    //セーブ音
                    SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_SE, systemSettingDataModel.soundSetting.save);
                    SoundManager.Self().PlaySe();
                }
                else
                {
                    //オートセーブの項目にはセーブさせない
                    //ブザー音
                    SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_SE, systemSettingDataModel.soundSetting.buzzer);
                    SoundManager.Self().PlaySe();
                }
            }
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ButtonProcessing(OnComplete callback, GameObject obj) {
#else
        public async Task ButtonProcessing(OnComplete callback, GameObject obj) {
#endif
            SaveItem saveItem = null;
            for (int i = 0; i < _saveItems.Count; i++)
                if (_saveItems[i].gameObject == _selectedItem)
                {
                    saveItem = _saveItems[i];
                    break;
                }

            if (_operation == Operation.Load)
            {
                //ロード中は無視する
                if (_LoadingFlg) return;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var data = DataManager.Self().LoadSaveData(saveItem.SaveFileNo);
#else
                var data = await DataManager.Self().LoadSaveData(saveItem.SaveFileNo);
#endif
                if (data != null)
                {
                    //U197 エデッタ限定でサウンド設定を反映する
#if (UNITY_EDITOR)
                    DatabaseManagementService _databaseManagementService;
                    SystemSettingDataModel editor_systemSettingDataModel;
                    _databaseManagementService = new DatabaseManagementService();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    editor_systemSettingDataModel = _databaseManagementService.LoadSystem();
#else
                    editor_systemSettingDataModel = await _databaseManagementService.LoadSystem();
#endif
                    systemSettingDataModel.bgm = editor_systemSettingDataModel.bgm;
                    data.runtimeSystemConfig.battleBgm.name = editor_systemSettingDataModel.bgm.battleBgm.name;
                    data.runtimeSystemConfig.battleBgm.pan = editor_systemSettingDataModel.bgm.battleBgm.pan;
                    data.runtimeSystemConfig.battleBgm.volume = editor_systemSettingDataModel.bgm.battleBgm.volume;
                    data.runtimeSystemConfig.battleBgm.pitch = editor_systemSettingDataModel.bgm.battleBgm.pitch;

                    data.runtimeSystemConfig.victoryMe.name = editor_systemSettingDataModel.bgm.victoryMe.name;
                    data.runtimeSystemConfig.victoryMe.volume = editor_systemSettingDataModel.bgm.victoryMe.volume;
                    data.runtimeSystemConfig.victoryMe.pan = editor_systemSettingDataModel.bgm.victoryMe.pan;
                    data.runtimeSystemConfig.victoryMe.pitch = editor_systemSettingDataModel.bgm.victoryMe.pitch;

                    data.runtimeSystemConfig.defeatMe.name = editor_systemSettingDataModel.bgm.defeatMe.name;
                    data.runtimeSystemConfig.defeatMe.volume = editor_systemSettingDataModel.bgm.defeatMe.volume;
                    data.runtimeSystemConfig.defeatMe.pan = editor_systemSettingDataModel.bgm.defeatMe.pan;
                    data.runtimeSystemConfig.defeatMe.pitch = editor_systemSettingDataModel.bgm.defeatMe.pitch;
#endif
                    _LoadingFlg = true;
                    DataManager.Self().ReloadGameParty();
                    // セーブデータの読込成功
                    HudDistributor.Instance.StaticHudHandler().FadeOut(() => {
                        if (transform != null && transform.parent != null && transform.parent.gameObject != null)
                        {
                            transform.parent.gameObject.SetActive(false);
                            _LoadingFlg = false;
                        }

                        SceneManager.LoadScene("SceneMap");
                    }, UnityEngine.Color.black, 0.5f, true);

                    //ロード音
                    SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_SE, systemSettingDataModel.soundSetting.load);
                    SoundManager.Self().PlaySe();
                }
                else
                {
                    //セーブデータの読込失敗
                    //ブザー音
                    SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_SE, systemSettingDataModel.soundSetting.buzzer);
                    SoundManager.Self().PlaySe();
                }
            }
        }

        /// <summary>
        ///     下向き矢印ボタン押下時のコールバック
        /// </summary>
        public void OnDownArrowClicked() {
            if (_selectedItem == null)
                return;

            var index = _saveItems.FindIndex(v => _selectedItem == v.gameObject);
            if (index == -1) return;
            index = Math.Min(index + 1, _saveItems.Count);

            var saveItem = _saveItems[index];
            saveItem.ItemButton.Select();
        }

        /// <summary>
        ///     上向き矢印ボタン押下時のコールバック
        /// </summary>
        public void OnUpArrowClicked() {
            if (_selectedItem == null) return;

            var index = _saveItems.FindIndex(v => _selectedItem == v.gameObject);
            if (index == -1) return;
            index = Math.Max(index - 1, 0);

            var saveItem = _saveItems[index];
            saveItem.ItemButton.Select();
        }

        /// <summary>
        ///     矢印ボタンの表示について確認を行う
        /// </summary>
        /// <param name="index">選択中の項目のインデックス。選択中の項目が無ければ-1を格納する</param>
        private void CheckArrowButton() {
            _upArrowButton.gameObject.SetActive(!Mathf.Approximately(_scrollRect.verticalNormalizedPosition, 1f));
            _downArrowButton.gameObject.SetActive(!Mathf.Approximately(1f - _scrollRect.verticalNormalizedPosition,
                1f));
        }

#if UNITY_EDITOR
        Button _saveButton;

        public void SaveFocused() {
            for (var i = 0; i < _saveItems.Count; i++)
            {
                var button = _saveItems[i].ItemButton.GetComponent<WindowButtonBase>();
                if (button.IsHighlight())
                {
                    _saveButton = _saveItems[i].ItemButton.GetComponent<Button>();
                    break;
                }
            }
        }

        public async void ChangeFocused() {
            //少し待たないとフォーカスが移らないため、待つ
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            await Task.Delay(10);
#else
            await UniteTask.Delay(10);
#endif

            //フォーカス再設定処理
            _saveButton?.Select();
        }
#endif
    }
}