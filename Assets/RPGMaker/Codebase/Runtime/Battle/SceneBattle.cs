using JetBrains.Annotations;
using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Common;
using RPGMaker.Codebase.CoreSystem.Knowledge.Misc;
using RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement;
using RPGMaker.Codebase.CoreSystem.Service.RuntimeDataManagement;
using RPGMaker.Codebase.Runtime.Battle.Sprites;
using RPGMaker.Codebase.Runtime.Battle.Window;
using RPGMaker.Codebase.Runtime.Common;
using RPGMaker.Codebase.Runtime.Common.Component;
using RPGMaker.Codebase.Runtime.Map;
using System;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif
using System.Linq;
using RPGMaker.Codebase.Runtime.Common.Component.Hud.PostEffect;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DataManager = RPGMaker.Codebase.Runtime.Common.DataManager;
using RPGMaker.Codebase.Runtime.GameOver;
using UnityEngine.Rendering.Universal;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace RPGMaker.Codebase.Runtime.Battle
{
    /// <summary>
    /// 戦闘シーンのコマンドやメッセージのウィンドウ、[敵キャラ]やサイドビューの[アクター]の画像を管理するクラス
    /// </summary>
    public class SceneBattle : SceneBase
    {
        /// <summary>
        /// コマンドウィンドウの作成先
        /// </summary>
        [SerializeField] private GameObject _commandWindowParent;
        /// <summary>
        /// 戻るボタン
        /// </summary>
        [SerializeField] private Button _backButton;
        public static bool BackButton;
        /// <summary>
        /// 戦闘シーン用のスプライトセット。[背景][アクター][敵キャラ]を含む
        /// </summary>
        [SerializeField] private SpritesetBattle _spriteset;
        /// <summary>
        /// 前画面に戻るときの抑制するフラグ
        /// </summary>
        public static bool IsUpdateSuppress;
        /// <summary>
        /// [ステータス]ウィンドウ
        /// </summary>
        private WindowBattleStatus _statusWindow;
        /// <summary>
        /// [パーティ]コマンドウィンドウ
        /// </summary>
        private WindowPartyCommand _partyCommandWindow;
        /// <summary>
        /// [アクター]コマンドウィンドウ
        /// </summary>
        private WindowActorCommand _actorCommandWindow;
        /// <summary>
        /// [アイテム]ウィンドウ
        /// </summary>
        private WindowBattleItem _itemWindow;
        /// <summary>
        /// [アクター]選択ウィンドウ
        /// </summary>
        private WindowBattleActor _actorWindow;
        /// <summary>
        /// [スキル]ウィンドウ
        /// </summary>
        private WindowBattleSkill _skillWindow;
        /// <summary>
        /// [敵キャラ]選択ウィンドウ
        /// </summary>
        private WindowBattleEnemy _enemyWindow;
        /// <summary>
        /// ログウィンドウ
        /// </summary>
        private WindowBattleLog _logWindow;
        /// <summary>
        /// ヘルプウィンドウ
        /// </summary>
        private WindowHelp _helpWindow;
        /// <summary>
        /// メッセージウィンドウ
        /// </summary>
        private WindowMessage _messageWindow;

        /// <summary>
        /// 遷移先の画面
        /// </summary>
        public GameStateHandler.GameState NextScene { get; set; }
        
        // エフェクト無効カメラ
        private Camera _ignoreEffectCamera;

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
        private GameObject _blackPlate;
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
        private void Awake() {
            _blackPlate = CreateBlackPlate(_commandWindowParent.transform.parent);
        }

        GameObject CreateBlackPlate(Transform parent) {
            var blackPlate = new GameObject("Black Plate");
            blackPlate.transform.SetParent(parent);
            var image = blackPlate.AddComponent<Image>();
            var rectTransform = blackPlate.GetComponent<RectTransform>();
            rectTransform.anchoredPosition3D = Vector3.zero;
            rectTransform.sizeDelta = new Vector2(1920, 1080);  //十分なサイズ。
            rectTransform.localScale = Vector3.one;
            image.color = Color.black;
            return blackPlate;
        }

        public void RemoveBlackPlate() {
            if (_blackPlate != null)
            {
                Destroy(_blackPlate, 0.1f);
                _blackPlate = null;
            }

        }
#endif
       //U368 ゲームオブジェクトを降順ソートする
       //敵のサイドビュー時の描画順を正しくする目的(前衛が手前、後衛が後ろ)
       private void GameObjetcSort() 
       {
            List<Transform> objList = new List<Transform>();

            // 子階層のGameObject取得
            var SideViewObj = GameObject.Find("SideView");
            var childCount = SideViewObj.transform.childCount;

            //Itemを追加
            for (int i = 0; i < childCount; i++)
            {
                if (SideViewObj.transform.GetChild(i).gameObject.name.Contains("Items"))
                {
                    objList.Add(SideViewObj.transform.GetChild(i));
                }
            }

            // オブジェクトを名前で降順ソート
            objList.Sort((obj1, obj2) => string.Compare(obj2.name, obj1.name));

            // ソート結果順にGameObjectの順序を反映
            foreach (var obj in objList)
            {
                obj.SetSiblingIndex(childCount - 1);
            }
        }

        /// <summary>
        /// シーンの開始
        /// </summary>
        protected override void Start() {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            StartAsync();
        }
        private async void StartAsync() {
            AddressableManager.Load.ReleaseLeastRecent(2);
#endif
            base.Start();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            base.Init();
#else
            await base.Init();
#endif

            //U368 ゲームオブジェクトを降順ソートする
            GameObjetcSort();

            //状態の更新
            GameStateHandler.SetGameState(GameStateHandler.GameState.BATTLE);
            //HUD系UIハンドリング
            HudDistributor.Instance.AddHudHandler(new HudHandler(gameObject));
            //音関連の初期化
            SoundManager.Self().Init();

            //indowの作成
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var window = new WindowInitialize().Create(_commandWindowParent);
#else
            var window = await new WindowInitialize().Create(_commandWindowParent);
#endif

            //要素の取得
            _logWindow = window.transform.Find("WindowBattleLog").GetComponent<WindowBattleLog>();
            _statusWindow = window.transform.Find("WindowBattleStatus").GetComponent<WindowBattleStatus>();
            _partyCommandWindow = window.transform.Find("WindowPartyCommand").GetComponent<WindowPartyCommand>();
            _actorCommandWindow = window.transform.Find("WindowActorCommand").GetComponent<WindowActorCommand>();
            _helpWindow = window.transform.Find("WindowHelp").GetComponent<WindowHelp>();
            _skillWindow = window.transform.Find("WindowBattleSkill").GetComponent<WindowBattleSkill>();
            _itemWindow = window.transform.Find("WindowBattleItem").GetComponent<WindowBattleItem>();
            _actorWindow = window.transform.Find("WindowBattleActor").GetComponent<WindowBattleActor>();
            _enemyWindow = window.transform.Find("WindowBattleEnemy").GetComponent<WindowBattleEnemy>();
            _messageWindow = window.transform.Find("WindowMessage").GetComponent<WindowMessage>();

            //初期状態設定
            _actorCommandWindow.gameObject.SetActive(true);
            _statusWindow.gameObject.SetActive(true);
            _skillWindow.gameObject.SetActive(true);
            _itemWindow.gameObject.SetActive(true);
            //Uniteではコマンド選択終了後は、ログWindowをActive、ステータスWindowを非Activeとする
            _logWindow.gameObject.SetActive(false);

            bool battleTest = false;
#if UNITY_EDITOR && !UNITE_WEBGL_TEST
            battleTest  = BattleTest.Instance.TryInitialize();
            if (battleTest)
            {
                var inputSystemObj = Resources.Load("InputSystem");
                Instantiate(inputSystemObj);
            }
#endif

#if DEBUG
            if (string.IsNullOrWhiteSpace(BattleSceneTransition.Instance.SelectTroopId))
            {
                var encounterTroopId =
                    BattleSceneTransition.Instance.EncounterDataModel?.troopList?.Count > 0
                        ? BattleSceneTransition.Instance.EncounterDataModel.troopList[0].troopId
                        : null;

                var (troopId, warningString) =
                    !string.IsNullOrEmpty(encounterTroopId)
                        ? (
                            encounterTroopId,
                            "このバトル用に設定されたエンカウンター情報から"
                        )
                        : (
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                            new DatabaseManagementService().LoadTroop()[0].id,
#else
                            (await new DatabaseManagementService().LoadTroop())[0].id,
#endif
                            "データベースの敵グループ情報から"
                        );

                BattleSceneTransition.Instance.SelectTroopId = troopId;
                DebugUtil.LogWarning(string.Concat(
                    "現在 (2022/04/12) 敵グループ相手のバトルしかできないので、",
                    $"{warningString}最初の敵グループid ({troopId}) を、",
                    "敵として設定しました。"));
            }
#endif
            //アクターの逃走状態を初期化
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var battlers = DataManager.Self().GetGameParty().BattleMembers();
#else
            var battlers = await DataManager.Self().GetGameParty().BattleMembers();
#endif
            for (int i = 0; i < battlers.Count; i++)
            {
                battlers[i].IsEscaped = false;
            }
            //TroopID
            var id = BattleSceneTransition.Instance.SelectTroopId;
            //逃走可否
            var canEscape = BattleSceneTransition.Instance.CanEscape;
            //敗北可否
            var canLose = BattleSceneTransition.Instance.CanLose;
            //戦闘背景1
            var backImage1 = BattleSceneTransition.Instance.EncounterDataBackgroundImage1;
            //戦闘背景2
            var backImage2 = BattleSceneTransition.Instance.EncounterDataBackgroundImage2;

            //idが空の場合には、先頭の敵グループIDを設定
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            if (id == "") id = new DatabaseManagementService().LoadTroop()[0].id;
#else
            if (id == "") id = (await new DatabaseManagementService().LoadTroop())[0].id;
#endif

            //画角対応として、バトル画面内に存在する全てのUIに対して、Scale設定を行う
            var scales = _spriteset.transform.parent.GetComponentsInChildren<CanvasScaler>();
            var displaySize = DataManager.Self().GetSystemDataModel()
                .DisplaySize[DataManager.Self().GetSystemDataModel().displaySize];
            foreach (var scale in scales)
            {
                scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                //scale.referenceResolution = displaySize;
            }

            //逃走可否、敗北可否をBattleManagerに登録
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            BattleManager.Setup(id, canEscape, canLose, this, battleTest);
#else
            await BattleManager.Setup(id, canEscape, canLose, this, battleTest);
#endif
            //先制攻撃、不意打ち設定
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            BattleManager.OnEncounter();
#else
            await BattleManager.OnEncounter();
#endif

            //戻るボタンは初期状態で非表示
            _backButton.gameObject.SetActive(false);
            //戻るボタンのCB登録
            _backButton.onClick.RemoveAllListeners();
            _backButton.onClick.AddListener(OnClickBackButton);

            //表示に必要なオブジェクトを生成
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            CreateDisplayObjects();
#else
            await CreateDisplayObjects();
#endif
            //バトル開始用のフェードイン処理
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            FadeIn();
#else
#endif
            //フォーカス補正用クラスを追加
            gameObject.AddComponent<SelectedGameObjectManager>();
            // ポストエフェクト用コンポーネント追加
            gameObject.AddComponent<PostEffect>();

            if (!Commons.IsURP())
            {
                _ignoreEffectCamera = new GameObject("IgnoreEffectCamera").AddComponent<Camera>();
                _ignoreEffectCamera.CopyFrom(Camera.main);
                _ignoreEffectCamera.gameObject.AddComponent<CameraResolutionManager>();
                var layerMask = LayerMask.NameToLayer("IgnorePostEffect");
                Camera.main.cullingMask = ~(1 << layerMask);
                _ignoreEffectCamera.cullingMask = 1 << layerMask;
                _ignoreEffectCamera.clearFlags = CameraClearFlags.Nothing;
                _ignoreEffectCamera.transform.SetParent(gameObject.transform);
            }
            else
            {
                var cameraData = Camera.main.GetUniversalAdditionalCameraData();
                cameraData.SetRenderer(Commons.UniteRendererDataOffset);
            }

            //BGM再生
            SoundCommonDataModel bgm = new SoundCommonDataModel(
                DataManager.Self().GetRuntimeSaveDataModel().runtimeSystemConfig.battleBgm.name,
                DataManager.Self().GetRuntimeSaveDataModel().runtimeSystemConfig.battleBgm.pan,
                DataManager.Self().GetRuntimeSaveDataModel().runtimeSystemConfig.battleBgm.pitch,
                DataManager.Self().GetRuntimeSaveDataModel().runtimeSystemConfig.battleBgm.volume
            );
            if (BattleSceneTransition.Instance.EncounterDataModel != null)
            {
                if (BattleSceneTransition.Instance.EncounterDataModel.bgm.name != "")
                {
                    bgm = new SoundCommonDataModel(
                        BattleSceneTransition.Instance.EncounterDataModel.bgm.name,
                        BattleSceneTransition.Instance.EncounterDataModel.bgm.pan,
                        BattleSceneTransition.Instance.EncounterDataModel.bgm.pitch,
                        BattleSceneTransition.Instance.EncounterDataModel.bgm.volume
                    );
                }
            }

            //鳴動していたサウンドを停止
            SoundManager.Self().StopBgs();
            SoundManager.Self().StopBgm();
            SoundManager.Self().StopMe();
            SoundManager.Self().StopSe();

            //バトル用BGM鳴動開始
            BattleManager.PlayBattleBgm(bgm);

            //バトルシーケンス開始
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            BattleManager.StartBattle();
#else
            await BattleManager.StartBattle();
            RemoveBlackPlate();
            await FadeIn();
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            if (battleTest)
            {
                //戦闘テストであることを設定
                BattleManager.SetBattleTest(true);
            }
#endif

            //TimeHandlerによるUpdate処理を開始
            TimeHandler.Instance.AddTimeActionEveryFrame(UpdateTimeHandler);
        }

        /// <summary>
        /// 破棄処理
        /// </summary>
        private void OnDestroy() {
            TimeHandler.Instance?.RemoveTimeAction(UpdateTimeHandler);
        }
        
        /// <summary>
        /// Update処理
        /// </summary>
        public void UpdateTimeHandler() {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            _ = UpdateTimeHandlerAsync();
        }
        public async Task UpdateTimeHandlerAsync() {
#endif
            IsUpdateSuppress = false;
            var active = IsActive();

            //元々は非Activeかつ、バトルイベントでAbortが呼ばれていた場合には、ここでAbortする処理が存在したが、Uniteではイベントから直接実施
            //フェード、色調変更、フラッシュ、画面の揺れ、天候、画像表示のUpdateもここで実施していたが、Uniteではイベントで直接実施

            //ステータスWindowの更新
            UpdateStatusWindow();

            //BattleManager側の更新
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            if (active && !IsBusy()) UpdateBattleProcess();
#else
            if (active && !IsBusy()) await UpdateBattleProcess();
#endif


            //各WindowのUpdate処理
            _spriteset.UpdateTimeHandler();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _logWindow.UpdateTimeHandler();
#else
            await _logWindow.UpdateTimeHandler();
#endif
            _statusWindow.UpdateTimeHandler();
            _partyCommandWindow.UpdateTimeHandler();
            _actorCommandWindow.UpdateTimeHandler();
            _helpWindow.UpdateTimeHandler();
            _skillWindow.UpdateTimeHandler();
            _itemWindow.UpdateTimeHandler();
            _actorWindow.UpdateTimeHandler();
            _enemyWindow.UpdateTimeHandler();
            _messageWindow.UpdateTimeHandler();

            //バトル時の入力操作更新
            //UniteではInputSystemを利用
            InputHandler.Watch();

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) && DEBUG
            // 戦闘テストの終了テスト用。
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                if (Input.GetKeyUp(KeyCode.X))
                    if (BattleTest.Instance.TryTerminate())
                        return;
#endif
        }

        private void LateUpdate() {
            if (BackButton)
            {
                BackButton = false;
            }
        }

        /// <summary>
        /// 戦闘段階のアップデート
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void UpdateBattleProcess() {
#else
        private async Task UpdateBattleProcess() {
#endif
            if (!IsAnyInputWindowActive() || BattleManager.IsAborting() || BattleManager.IsBattleEnd())
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                BattleManager.UpdateBattleProcess();
                ChangeInputWindow();
#else
                await BattleManager.UpdateBattleProcess();
                await ChangeInputWindow();
#endif
            }
        }

        /// <summary>
        /// 入力ウィンドウがアクティブか
        /// </summary>
        /// <returns></returns>
        private bool IsAnyInputWindowActive() {
            return _partyCommandWindow.Active ||
                   _actorCommandWindow.Active ||
                   _skillWindow.Active ||
                   _itemWindow.Active ||
                   _actorWindow.Active ||
                   _enemyWindow.Active;
        }

        /// <summary>
        /// [パーティ]か[アクター]のコマンドウィンドウの選択、非選択を状態に応じて切り替え
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void ChangeInputWindow() {
#else
        private async Task ChangeInputWindow() {
#endif
            if (BattleManager.IsInputting())
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                if (BattleManager.Actor() != null)
#else
                if (await BattleManager.Actor() != null)
#endif
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    StartActorCommandSelection();
#else
                    await StartActorCommandSelection();
#endif
                }
                else
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    StartPartyCommandSelection();
#else
                    await StartPartyCommandSelection();
#endif
                }
            }
            else
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                EndCommandSelection();
#else
                await EndCommandSelection();
#endif
            }
        }

        /// <summary>
        /// シーンの停止
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public override void Stop() {
#else
        public override async Task Stop() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            base.Stop();
#else
            await base.Stop();
#endif

            //フェードアウト処理
            //フェードアウト後にTerminateを実行する
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            FadeOut(Terminate);
#else
            await FadeOut(() => { _ = Terminate(); });
#endif

            _statusWindow.Close();
            _partyCommandWindow.Close();
            _actorCommandWindow.Close();
        }

        /// <summary>
        /// 遷移前のシーン中断
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void Terminate() {
#else
        public async Task Terminate() {
#endif
            //GameParty終期化処理
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataManager.Self().GetGameParty().OnBattleEnd();
#else
            await DataManager.Self().GetGameParty().OnBattleEnd();
#endif
            //GameTroop終期化処理
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataManager.Self().GetGameTroop().OnBattleEnd();
#else
            await DataManager.Self().GetGameTroop().OnBattleEnd();
#endif

            //サウンド停止
            SoundManager.Self().StopBgs();
            SoundManager.Self().StopBgm();
            SoundManager.Self().StopMe();
            SoundManager.Self().StopSe();
            
            // ポストエフェクトを全て削除
            var postEffect = gameObject.GetComponent<PostEffect>();
            postEffect.RemoveAll();

            //バトル終了
            BattleManager.IsBattle = false;

#if UNITY_EDITOR
            if (BattleTest.Instance.TryTerminate()) return;
#endif

            //マップへ戻る処理 (Unite固有）
            if (NextScene == GameStateHandler.GameState.MAP)
            {
                // HUD系UIハンドリング
                HudDistributor.Instance.RemoveHudHandler();

                //状態の更新
                GameStateHandler.SetGameState(GameStateHandler.GameState.MAP);

                //MAPを有効にする
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                MapManager.BattleToMap();
#else
                await MapManager.BattleToMap();
#endif

                // オートセーブの実施
                var systemSettingDataModel = DataManager.Self().GetSystemDataModel();
                if (systemSettingDataModel.optionSetting.enabledAutoSave == 1)
                {
                    var runtimeDataManagementService = new RuntimeDataManagementService();
                    var data = DataManager.Self().GetRuntimeSaveDataModel();
                    runtimeDataManagementService.SaveAutoSaveData(data);
                }

                //バトルをUnloadする
                //SceneManager.UnloadSceneAsync("Battle");

                //マップのBGM、BGSに戻す
                BattleManager.ReplayBgmAndBgs();

                //初期化する
                BattleManager.InitMembers();
            }
            else if (NextScene == GameStateHandler.GameState.TITLE)
            {
                SceneManager.LoadScene("Title");
            }
            //GAMEOVER
            else
            {
                //ゲームオーバー時の復活処理へ
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                SceneGameOver.RespawnPointExec(false);
#else
                await SceneGameOver.RespawnPointExec(false);
#endif

                //SceneManager.LoadScene("GameOver");
            }
        }

        /// <summary>
        /// GAMEOVER処理
        /// イベントから実行する
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void GameOver() {
#else
        public static async Task GameOver() {
#endif
            BattleManager.SceneBattle.NextScene = GameStateHandler.GameState.GAME_OVER;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            BattleManager.SceneBattle.Stop();
#else
            await BattleManager.SceneBattle.Stop();
#endif
        }

        /// <summary>
        /// タイトルへ戻る
        /// イベントから実行する
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void BackTitle() {
#else
        public static async Task BackTitle() {
#endif
            BattleManager.SceneBattle.NextScene = GameStateHandler.GameState.TITLE;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            BattleManager.SceneBattle.Stop();
#else
            await BattleManager.SceneBattle.Stop();
#endif
        }

        /// <summary>
        /// [ステータス]ウィンドウのアップデート
        /// </summary>
        public void UpdateStatusWindow() {
            if (DataManager.Self().GetGameMessage().IsBusy())
            {
                _statusWindow.Close();
                _partyCommandWindow.Close();
                _actorCommandWindow.Close();
            }
            else if (IsActive() && !_messageWindow.IsClosing())
            {
                _statusWindow.Open();
            }
        }

        /// <summary>
        /// 表示に必要なオブジェクトを生成。 スプライトセット、ウィンドウレイヤー、ウィンドウなど
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void CreateDisplayObjects() {
#else
        public async Task CreateDisplayObjects() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            CreateSpriteset();
            CreateAllWindows();
#else
            await CreateSpriteset();
            await CreateAllWindows();
#endif
            BattleManager.SetLogWindow(_logWindow);
            BattleManager.SetStatusWindow(_statusWindow);
            BattleManager.SetCommandWindowParent(_commandWindowParent);
            BattleManager.SetSpriteset(_spriteset);
        }

        /// <summary>
        /// 戦闘シーンに必要なスプライトセットを生成。 [アクター][敵キャラ]など
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void CreateSpriteset() {
            _spriteset.Initialize();
        }
#else
        public async Task CreateSpriteset() {
            await _spriteset.Initialize();
        }
#endif

        /// <summary>
        /// 戦闘シーンに必要なすべてのウィンドウを生成
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void CreateAllWindows() {
            CreateLogWindow();
            CreateStatusWindow();
            CreatePartyCommandWindow();
            CreateActorCommandWindow();
            CreateHelpWindow();
            CreateSkillWindow();
            CreateItemWindow();
            CreateActorWindow();
            CreateEnemyWindow();
            CreateMessageWindow();
        }
#else
        public async Task CreateAllWindows() {
            await CreateLogWindow();
            await CreateStatusWindow();
            await CreatePartyCommandWindow();
            await CreateActorCommandWindow();
            await CreateHelpWindow();
            await CreateSkillWindow();
            await CreateItemWindow();
            await CreateActorWindow();
            await CreateEnemyWindow();
            await CreateMessageWindow();
        }
#endif

        /// <summary>
        /// ログウィンドウ(Window_BattleLog)を生成
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void CreateLogWindow() {
            _logWindow.Initialize();
        }
#else
        public async Task CreateLogWindow() {
            await _logWindow.Initialize();
        }
#endif

        /// <summary>
        /// [ステータス]ウィンドウ(Window_BattleStatus)を生成
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void CreateStatusWindow() {
            _statusWindow.Initialize();
        }
#else
        public async Task CreateStatusWindow() {
            await _statusWindow.Initialize();
        }
#endif

        /// <summary>
        /// [パーティ]コマンドウィンドウ(Window_PartyCommand)を生成
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void CreatePartyCommandWindow() {
#else
        public async Task CreatePartyCommandWindow() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _partyCommandWindow.Initialize();
            _partyCommandWindow.SetHandler("fight", CommandFight);
            _partyCommandWindow.SetHandler("escape", CommandEscape);
#else
            await _partyCommandWindow.Initialize();
            _partyCommandWindow.SetHandler("fight", () => { _ = CommandFight(); });
            _partyCommandWindow.SetHandler("escape", () => { _ = CommandEscape(); });
#endif
            _partyCommandWindow.Deselect();
        }

        /// <summary>
        /// [アクター]コマンドウィンドウ(Window_ActorCommand)を生成
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void CreateActorCommandWindow() {
#else
        public async Task CreateActorCommandWindow() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _actorCommandWindow.Initialize();
            _actorCommandWindow.SetHandler("attack", CommandAttack);
            _actorCommandWindow.SetHandler("skill", CommandSkill);
            _actorCommandWindow.SetHandler("defence", CommandGuard);
            _actorCommandWindow.SetHandler("item", CommandItem);
            _actorCommandWindow.SetHandler("cancel", SelectPreviousCommand);
#else
            await _actorCommandWindow.Initialize();
            _actorCommandWindow.SetHandler("attack", () => { _ = CommandAttack(); });
            _actorCommandWindow.SetHandler("skill", () => { _ = CommandSkill(); });
            _actorCommandWindow.SetHandler("defence", () => { _ = CommandGuard(); });
            _actorCommandWindow.SetHandler("item", () => { _ = CommandItem(); });
            _actorCommandWindow.SetHandler("cancel", () => { _ = SelectPreviousCommand(); });
#endif
        }

        /// <summary>
        /// ヘルプウィンドウ(Window_Help)を生成
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void CreateHelpWindow() {
            _helpWindow.Initialize();
            _helpWindow.Visible = false;
        }
#else
        public async Task CreateHelpWindow() {
            await _helpWindow.Initialize();
            _helpWindow.Visible = false;
        }
#endif

        /// <summary>
        /// [スキル]ウィンドウ(Window_BattleSkill)を生成
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void CreateSkillWindow() {
#else
        public async Task CreateSkillWindow() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _skillWindow.Initialize();
#else
            await _skillWindow.Initialize();
#endif
            _skillWindow.SetHelpWindow(_helpWindow);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _skillWindow.SetHandler("ok", OnSkillOk);
            _skillWindow.SetHandler("cancel", OnSkillCancel);
#else
            _skillWindow.SetHandler("ok", () => { _ = OnSkillOk(); });
            _skillWindow.SetHandler("cancel", () => { _ = OnSkillCancel(); });
#endif
        }

        /// <summary>
        /// [アイテム]ウィンドウ(Window_BattleItem)を生成
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void CreateItemWindow() {
#else
        public async Task CreateItemWindow() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _itemWindow.Initialize();
#else
            await _itemWindow.Initialize();
#endif
            _itemWindow.SetHelpWindow(_helpWindow);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _itemWindow.SetHandler("ok", OnItemOk);
            _itemWindow.SetHandler("cancel", OnItemCancel);
#else
            _itemWindow.SetHandler("ok", () => { _ = OnItemOk(); });
            _itemWindow.SetHandler("cancel", () => { _ = OnItemCancel(); });
#endif
        }

        /// <summary>
        /// [アクター]選択ウィンドウ(Window_BattleActor)を生成
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void CreateActorWindow() {
#else
        public async Task CreateActorWindow() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _actorWindow.Initialize();
            _actorWindow.SetHandler("ok", OnActorOk);
            _actorWindow.SetHandler("cancel", OnActorCancel);
#else
            await _actorWindow.Initialize();
            _actorWindow.SetHandler("ok", () => { _ = OnActorOk(); });
            _actorWindow.SetHandler("cancel", () => { _ = OnActorCancel(); });
#endif
        }

        /// <summary>
        /// [敵キャラ]選択ウィンドウ(Window_BattleEnemy)を生成
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void CreateEnemyWindow() {
#else
        public async Task CreateEnemyWindow() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _enemyWindow.Initialize();
            _enemyWindow.SetHandler("ok", OnEnemyOk);
            _enemyWindow.SetHandler("cancel", OnEnemyCancel);
#else
            await _enemyWindow.Initialize();
            _enemyWindow.SetHandler("ok", () => { _ = OnEnemyOk(); });
            _enemyWindow.SetHandler("cancel", () => { _ = OnEnemyCancel(); });
#endif
       }

        /// <summary>
        /// メッセージウィンドウ(Window_Message)を生成
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void CreateMessageWindow() {
            _messageWindow.Initialize();
        }
#else
        public async Task CreateMessageWindow() {
            await _messageWindow.Initialize();
        }
#endif

        /// <summary>
        /// [ステータス]の回復
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void RefreshStatus() {
            _statusWindow.Refresh();
        }
#else
        public async Task RefreshStatus() {
            await _statusWindow.Refresh();
        }
#endif

        /// <summary>
        /// [パーティ]コマンドの選択開始
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void StartPartyCommandSelection() {
#else
        public async Task StartPartyCommandSelection() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            RefreshStatus();
#else
            await RefreshStatus();
#endif
            _statusWindow.Deselect();
            _statusWindow.Open();
            _actorCommandWindow.Close();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _partyCommandWindow.Setup();
#else
            await _partyCommandWindow.Setup();
#endif

            //以降、UniteのWindowやボタン制御関連処理
            //直前のWindowを表示
            _partyCommandWindow.gameObject.SetActive(true);
            _statusWindow.gameObject.SetActive(true);
            _backButton.gameObject.SetActive(false);
            _logWindow.gameObject.SetActive(false);

            //ボタンは全て有効にする
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _partyCommandWindow.Show();
#else
            await _partyCommandWindow.Show();
#endif
        }

        /// <summary>
        /// [戦う]コマンドのハンドラ
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void CommandFight() {
            SelectNextCommand();
        }
#else
        public async Task CommandFight() {
            await SelectNextCommand();
        }
#endif

        /// <summary>
        /// [逃げる]コマンドのハンドラ
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void CommandEscape() {
            BattleManager.ProcessEscape();
            ChangeInputWindow();
        }
#else
        public async Task CommandEscape() {
            await BattleManager.ProcessEscape();
            await ChangeInputWindow();
        }
#endif

        /// <summary>
        /// [アクター]コマンドの選択開始
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void StartActorCommandSelection() {
#else
        public async Task StartActorCommandSelection() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _statusWindow.Select(BattleManager.Actor().Index());
#else
            _statusWindow.Select(await (await BattleManager.Actor()).Index());
#endif
            _partyCommandWindow.Close();
            _actorCommandWindow.gameObject.SetActive(true);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _actorCommandWindow.Setup(BattleManager.Actor());
            _actorCommandWindow.Show();
#else
            await _actorCommandWindow.Setup(await BattleManager.Actor());
            await _actorCommandWindow.Show();
#endif

            //以降、UniteのWindowやボタン制御関連処理
            //対象のWindowを表示
            _statusWindow.gameObject.SetActive(true);

            //戻るボタン制御
            _backButton.gameObject.SetActive(true);
        }

        /// <summary>
        /// [攻撃]コマンドのハンドラ
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void CommandAttack() {
            BattleManager.InputtingAction().SetAttack();
            SelectEnemySelection();
        }
#else
        public async Task CommandAttack() {
            (await BattleManager.InputtingAction()).SetAttack();
            await SelectEnemySelection();
        }
#endif

        /// <summary>
        /// [スキル]コマンドのハンドラ
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void CommandSkill() {
#else
        public async Task CommandSkill() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _skillWindow.SetActor(BattleManager.Actor());
            _skillWindow.SetStypeId(_actorCommandWindow.CurrentExt());
            //_skillWindow.Refresh();
            _skillWindow.Show();
#else
            await _skillWindow.SetActor(await BattleManager.Actor());
            await _skillWindow.SetStypeId(_actorCommandWindow.CurrentExt());
            //_skillWindow.Refresh();
            await _skillWindow.Show();
#endif
            _skillWindow.Activate();

            //以降、UniteのWindowやボタン制御関連処理
            _actorCommandWindow.gameObject.transform.localScale = new Vector3(0f,0f,0f);
            _partyCommandWindow.Close();
            _statusWindow.Close();

            //_actorCommandWindow.SetHandler("cancel", OnSkillCancel);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _skillWindow.SetHandler("cancel", OnSkillCancel);
#else
            _skillWindow.SetHandler("cancel", () => { _ = OnSkillCancel(); });
#endif

            //対象のWindowを表示
            _skillWindow.gameObject.SetActive(true);

            //直前のWindowは非表示
            _statusWindow.gameObject.SetActive(false);
            _actorCommandWindow.gameObject.SetActive(false);
        }

        /// <summary>
        /// [防御]コマンドのハンドラ
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void CommandGuard() {
            BattleManager.InputtingAction()?.SetGuard();
            SelectNextCommand();
        }
#else
        public async Task CommandGuard() {
            (await BattleManager.InputtingAction())?.SetGuard();
            await SelectNextCommand();
        }
#endif

        /// <summary>
        /// [アイテム]コマンドのハンドラ
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void CommandItem() {
#else
        public async Task CommandItem() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _itemWindow.Refresh();
            _itemWindow.Show();
#else
            await _itemWindow.Refresh();
            await _itemWindow.Show();
#endif
            _itemWindow.Activate();

            //以降、UniteのWindowやボタン制御関連処理
            //_actorCommandWindow.SetHandler("cancel", OnItemCancel);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _itemWindow.SetHandler("cancel", OnItemCancel);
#else
            _itemWindow.SetHandler("cancel", () => { _ = OnItemCancel(); });
#endif

            //対象のWindowを表示
            _itemWindow.gameObject.SetActive(true);

            //直前のWindowは非表示
            _statusWindow.gameObject.SetActive(false);
            _actorCommandWindow.gameObject.SetActive(false);
        }

        /// <summary>
        /// ひとつ先のコマンドを選択
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SelectNextCommand() {
            BattleManager.SelectNextCommand();
            ChangeInputWindow();
        }
#else
        public async Task SelectNextCommand() {
            await BattleManager.SelectNextCommand();
            await ChangeInputWindow();
        }
#endif

        /// <summary>
        /// ひとつ前のコマンドを選択
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SelectPreviousCommand() {
            BattleManager.SelectPreviousCommand();
            ChangeInputWindow();
        }
#else
        public async Task SelectPreviousCommand() {
            await BattleManager.SelectPreviousCommand();
            await ChangeInputWindow();
        }
#endif

        /// <summary>
        /// [アクター]選択ウィンドウの準備
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SelectActorSelection() {
#else
        public async Task SelectActorSelection() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _actorWindow.Refresh();
            _actorWindow.Show();
#else
            await _actorWindow.Refresh();
            await _actorWindow.Show();
#endif
            _actorWindow.Activate();

            //直前のWindowは非表示
            _actorCommandWindow.gameObject.SetActive(false);
            _statusWindow.gameObject.SetActive(false);
            _skillWindow.gameObject.SetActive(false);
            _itemWindow.gameObject.SetActive(false);
        }

        /// <summary>
        /// [アクター]選択ウィンドウで[OK]が選択された時のハンドラ
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void OnActorOk() {
#else
        public async Task OnActorOk() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var action = BattleManager.InputtingAction();
#else
            var action = await BattleManager.InputtingAction();
#endif
            if (action != null)
                action.SetTarget(_actorWindow.Index());
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _actorWindow.Hide();
            _skillWindow.Hide();
            _itemWindow.Hide();
            SelectNextCommand();
#else
            await _actorWindow.Hide();
            await _skillWindow.Hide();
            await _itemWindow.Hide();
            await SelectNextCommand();
#endif
        }

        /// <summary>
        /// [アクター]選択ウィンドウで[キャンセル]が選択された時のハンドラ
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void OnActorCancel() {
#else
        public async Task OnActorCancel() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _actorWindow.Hide();
#else
            await _actorWindow.Hide();
#endif
            switch (_actorCommandWindow.CurrentSymbol())
            {
                case "skill":
                    //直前のWindowを表示
                    _skillWindow.gameObject.SetActive(true);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _skillWindow.Show();
#else
                    await _skillWindow.Show();
#endif
                    _skillWindow.Activate();
                    break;
                case "item":
                    //直前のWindowを表示
                    _itemWindow.gameObject.SetActive(true);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _itemWindow.Show();
#else
                    await _itemWindow.Show();
#endif
                    _itemWindow.Activate();
                    break;
            }
        }

        /// <summary>
        /// [敵キャラ]選択ウィンドウの準備
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SelectEnemySelection() {
#else
        public async Task SelectEnemySelection() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _enemyWindow.Refresh();
            _enemyWindow.Show();
            _enemyWindow.Select(0);
#else
            await _enemyWindow.Refresh();
            await _enemyWindow.Show();
            await _enemyWindow.Select(0);
#endif
            _enemyWindow.Activate();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _enemyWindow.SetHandler("cancel", OnEnemyCancel);
#else
            _enemyWindow.SetHandler("cancel", () => { _ = OnEnemyCancel(); });
#endif

            //直前のWindowは非表示
            _actorCommandWindow.gameObject.SetActive(false);
            _statusWindow.gameObject.SetActive(false);
            _skillWindow.gameObject.SetActive(false);
            _itemWindow.gameObject.SetActive(false);
        }

        /// <summary>
        /// [敵キャラ]選択ウィンドウで[OK]が選択された時のハンドラ
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void OnEnemyOk() {
            var action = BattleManager.InputtingAction();
            action?.SetTarget(_enemyWindow.EnemyIndex());
            _enemyWindow.Hide();
            _skillWindow.Hide();
            _itemWindow.Hide();
            SelectNextCommand();
        }
#else
        public async Task OnEnemyOk() {
            var action = await BattleManager.InputtingAction();
            action?.SetTarget(await _enemyWindow.EnemyIndex());
            await _enemyWindow.Hide();
            await _skillWindow.Hide();
            await _itemWindow.Hide();
            await SelectNextCommand();
        }
#endif

        /// <summary>
        /// [敵キャラ]選択ウィンドウで[キャンセル]が選択された時のハンドラ
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void OnEnemyCancel() {
#else
        public async Task OnEnemyCancel() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _actorCommandWindow.SetHandler("cancel", SelectPreviousCommand);
            _enemyWindow.Hide();
#else
            _actorCommandWindow.SetHandler("cancel", () => { _ = SelectPreviousCommand(); });
            await _enemyWindow.Hide();
#endif
            _enemyWindow.Deactivate();
            switch (_actorCommandWindow.CurrentSymbol())
            {
                case "attack":
                    //直前のWindowを表示
                    _statusWindow.gameObject.SetActive(true);
                    _actorCommandWindow.gameObject.SetActive(true);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _actorCommandWindow.Show();
#else
                    await _actorCommandWindow.Show();
#endif
                    _actorCommandWindow.Activate();
                    _actorCommandWindow.Select(0);
                    break;
                case "skill":
                    _skillWindow.gameObject.SetActive(true);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _skillWindow.Show();
#else
                    await _skillWindow.Show();
#endif
                    _skillWindow.Activate();
                    break;
                case "item":
                    _itemWindow.gameObject.SetActive(true);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _itemWindow.Show();
#else
                    await _itemWindow.Show();
#endif
                    _itemWindow.Activate();
                    break;
            }
        }

        /// <summary>
        /// [スキル]ウィンドウで[OK]が選択された時のハンドラ
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void OnSkillOk() {
#else
        public async Task OnSkillOk() {
#endif
            var skill = _skillWindow.Item();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var action = BattleManager.InputtingAction();
#else
            var action = await BattleManager.InputtingAction();
#endif
            action.SetSkill(skill.ItemId);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            BattleManager.Actor().SetLastBattleSkill(skill);
            OnSelectAction();
#else
            (await BattleManager.Actor()).SetLastBattleSkill(skill);
            await OnSelectAction();
#endif
        }

        /// <summary>
        /// [スキル]ウィンドウで[キャンセル]が選択された時のハンドラ
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void OnSkillCancel() {
#else
        public async Task OnSkillCancel() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _skillWindow.Hide();
#else
            await _skillWindow.Hide();
#endif
            _skillWindow.Deactivate();
            _actorCommandWindow.gameObject.transform.localScale = new Vector3(1f,1f,1f);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _actorCommandWindow.Setup(BattleManager.Actor());
#else
            await _actorCommandWindow.Setup(await BattleManager.Actor());
#endif
            _actorCommandWindow.Select(0);
            _actorCommandWindow.Activate();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _actorCommandWindow.SetHandler("cancel", SelectPreviousCommand);
#else
            _actorCommandWindow.SetHandler("cancel", () => { _ = SelectPreviousCommand(); });
#endif
            //直前のWindowを表示
            _actorCommandWindow.gameObject.SetActive(true);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _actorCommandWindow.Show();
#else
            await _actorCommandWindow.Show();
#endif
            _statusWindow.gameObject.SetActive(true);
        }

        /// <summary>
        /// [アイテム]ウィンドウで[OK]が選択された時のハンドラ
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void OnItemOk() {
#else
        public async Task OnItemOk() {
#endif
            var item = _itemWindow.Item();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var action = BattleManager.InputtingAction();
#else
            var action = await BattleManager.InputtingAction();
#endif
            action.SetItem(item.ItemId);
            DataManager.Self().GetGameParty().SetLastItem(item);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            OnSelectAction();
#else
            await OnSelectAction();
#endif
        }

        /// <summary>
        /// [アイテム]ウィンドウで[キャンセル]が選択された時のハンドラ
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void OnItemCancel() {
#else
        public async Task OnItemCancel() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _itemWindow.Hide();
#else
            await _itemWindow.Hide();
#endif
            _itemWindow.Deactivate();

            _actorCommandWindow.gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _actorCommandWindow.Setup(BattleManager.Actor());
#else
            await _actorCommandWindow.Setup(await BattleManager.Actor());
#endif
            _actorCommandWindow.Select(0);
            _actorCommandWindow.Activate();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _actorCommandWindow.SetHandler("cancel", SelectPreviousCommand);
#else
            _actorCommandWindow.SetHandler("cancel", () => { _ = SelectPreviousCommand(); });
#endif

            //直前のWindowを表示
            _actorCommandWindow.gameObject.SetActive(true);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _actorCommandWindow.Show();
#else
            await _actorCommandWindow.Show();
#endif
            _statusWindow.gameObject.SetActive(true);
        }

        /// <summary>
        /// アイテムかスキルが選択された時のハンドラ
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void OnSelectAction() {
#else
        public async Task OnSelectAction() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var action = BattleManager.InputtingAction();
            _skillWindow.Hide();
            _itemWindow.Hide();
#else
            var action = await BattleManager.InputtingAction();
            await _skillWindow.Hide();
            await _itemWindow.Hide();
#endif

            if (!action.NeedsSelection())
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                SelectNextCommand();
#else
                await SelectNextCommand();
#endif
            else if (action.IsForOpponent())
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                SelectEnemySelection();
#else
                await SelectEnemySelection();
#endif
            else
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                SelectActorSelection();
#else
                await SelectActorSelection();
#endif
        }

        /// <summary>
        /// コマンド選択の終了処理
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void EndCommandSelection() {
#else
        public async Task EndCommandSelection() {
#endif
            _partyCommandWindow.Close();
            _actorCommandWindow.Close();
            _statusWindow.Deselect();

            //元々は全てActiveであったため、コマンド入力が一通り完了後は元の状態に戻す
            _actorCommandWindow.gameObject.SetActive(true);
            _skillWindow.gameObject.SetActive(true);
            _itemWindow.gameObject.SetActive(true);
            _backButton.gameObject.SetActive(false);

            //ボタンは全て無効にする
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _partyCommandWindow.Hide();
            _actorCommandWindow.Hide();
            _enemyWindow.Hide();
#else
            await _partyCommandWindow.Hide();
            await _actorCommandWindow.Hide();
            await _enemyWindow.Hide();
#endif
        }

        /// <summary>
        /// フェードイン
        /// </summary>
        /// <param name="callBack"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void FadeIn([CanBeNull] Action callBack = null) {
#else
        public async Task FadeIn([CanBeNull] Action callBack = null) {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            HudDistributor.Instance.NowHudHandler().DisplayInit();
#else
            await HudDistributor.Instance.NowHudHandler().DisplayInit();
#endif
            HudDistributor.Instance.NowHudHandler().FadeIn(callBack, true);
        }

        /// <summary>
        /// フェードアウト
        /// </summary>
        /// <param name="callBack"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void FadeOut([CanBeNull] Action callBack = null) {
#else
        public async Task FadeOut([CanBeNull] Action callBack = null) {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            HudDistributor.Instance.NowHudHandler().DisplayInit();
#else
            await HudDistributor.Instance.NowHudHandler().DisplayInit();
#endif
            HudDistributor.Instance.NowHudHandler().FadeOut(callBack, Color.black);
        }

        public void OnClickBackButton() {
            BackButton = true;
        }

#if UNITY_EDITOR
        /// <summary>
        ///     戦闘テスト。
        /// </summary>
        public class BattleTest
        {
            private static BattleTest _instance;
            private        bool       _isBattleTest;

            private BattleTest() {
            }

            public static BattleTest Instance
            {
                get
                {
                    _instance ??= new BattleTest();
                    return _instance;
                }
            }

            public static event Action<string> ScenePlayEndEvent;

            /// <summary>
            ///     戦闘テストの初期化を試行。
            /// </summary>
            /// <returns>戦闘テストフラグ</returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            public bool TryInitialize() {
#else
            public async Task<bool> TryInitialize() {
#endif
                // タイトルシーンを経由せずにバトルシーンを再生した？
                _isBattleTest = DataManager.Self().GetRuntimeSaveDataModel() == null;

                if (!_isBattleTest)
                {
                    return false;
                }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                DataManager.Self().CreateGame(BattleSceneTransition.Instance);
#else
                await DataManager.Self().CreateGame(BattleSceneTransition.Instance);
#endif
                DataManager.Self().CreateLoadGame();

                BattleSceneTransition.Instance.CanEscape = true;
                BattleSceneTransition.Instance.CanLose = true;

                // 非アクティブになっているのでアクティブ化。
                SceneManager.GetActiveScene().GetRootGameObjects().Single(go => go.name == "EventSystem").SetActive(true);

                // キー等の割り当てをUnite用に差し替え
                string jsonData = File.ReadAllText(Application.dataPath + "/RPGMaker/InputSystem/rpgmaker.inputactions");
                InputActionAsset inputAction = new InputActionAsset();
                inputAction.LoadFromJson(jsonData);
                SceneManager.GetActiveScene().GetRootGameObjects().Single(go => go.name == "EventSystem").GetComponent<InputSystemUIInputModule>().actionsAsset = inputAction;

                //プレイ時間初期化
                TimeHandler.Instance.SetPlayTime(null);
                

                return true;
            }

            /// <summary>
            ///     戦闘テストの終期化を試行。
            /// </summary>
            /// <returns>戦闘テストだったフラグ</returns>
            public bool TryTerminate() {
                if (!_isBattleTest) return false;

                ScenePlayEndEvent?.Invoke(SceneManager.GetActiveScene().name);
                _instance = null;
                return true;
            }
        }
#endif
    }
}