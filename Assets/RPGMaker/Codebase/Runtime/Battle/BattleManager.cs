using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Common;
using RPGMaker.Codebase.Runtime.Battle.Objects;
using RPGMaker.Codebase.Runtime.Battle.Sprites;
using RPGMaker.Codebase.Runtime.Battle.Window;
using RPGMaker.Codebase.Runtime.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace RPGMaker.Codebase.Runtime.Battle
{
    /// <summary>
    /// 戦闘の進行を制御する静的クラス
    /// </summary>
    public static class BattleManager
    {
        /// <summary>
        /// [static] 行動状態
        /// </summary>
        private static string _phase;
        /// <summary>
        /// [static] [逃走可]
        /// </summary>
        private static bool _canEscape;
        /// <summary>
        /// [static] [敗北可]
        /// </summary>
        private static bool _canLose;
        /// <summary>
        /// [static] [戦闘テスト]か
        /// </summary>
        private static bool _battleTest;
        /// <summary>
        /// [static] [先制攻撃]か
        /// </summary>
        private static bool _preemptive;
        /// <summary>
        /// [static] [不意打ち]か
        /// </summary>
        private static bool _surprise;
        /// <summary>
        /// [static] アクター番号
        /// </summary>
        private static int _actorIndex;
        /// <summary>
        /// [static] 強制行動のアクター
        /// </summary>
        private static GameBattler _actionForcedBattler;
        /// <summary>
        /// [static] 強制行動中
        /// </summary>
        private static bool _actionForced;
        /// <summary>
        /// [static] 戦闘BGM
        /// </summary>
        private static SoundCommonDataModel _mapBgm;
        /// <summary>
        /// [static] 戦闘BGS
        /// </summary>
        private static SoundCommonDataModel _mapBgs;
        /// <summary>
        /// [static] アクションを行うバトラーの配列(行動順)
        /// </summary>
        private static List<GameBattler> _actionBattlers;
        /// <summary>
        /// [static] 対象バトラー
        /// </summary>
        private static GameBattler _subject;
        /// <summary>
        /// [static] アクション
        /// </summary>
        private static GameAction _action;
        /// <summary>
        /// [static] 目標バトラーの配列
        /// </summary>
        private static List<GameBattler> _targets;
        /// <summary>
        /// [static] 使用者への影響バトラー
        /// </summary>
        private static GameBattler _targetMyself;
        /// <summary>
        /// [static] 使用者への影響バトラー（フラグ）
        /// </summary>
        private static bool _isTargetMyself;
        /// <summary>
        /// [static] ログウィンドウ
        /// </summary>
        private static WindowBattleLog _logWindow;
        /// <summary>
        /// [static] ステータスウィンドウ
        /// </summary>
        private static WindowBattleStatus _statusWindow;
        /// <summary>
        /// [static] スプライトセット
        /// </summary>
        private static GameObject _commandWindowParent;
        /// <summary>
        /// [static] スプライトセット
        /// </summary>
        private static SpritesetBattle _spriteset;
        /// <summary>
        /// [static] 逃走確率
        /// </summary>
        private static double _escapeRatio;
        /// <summary>
        /// [static] 逃走成功か
        /// </summary>
        private static bool _escaped;
        /// <summary>
        /// [static] 報酬
        /// </summary>
        private static BattleRewards _rewards;
        /// <summary>
        /// 強制行動中かどうか
        /// </summary>
        private static bool _turnForced;
        /// <summary>
        /// SceneBattleのインスタンス
        /// </summary>
        public static SceneBattle SceneBattle { get; set; }

        /// <summary>
        /// 現在バトル中かどうか
        /// </summary>
        public static bool IsBattle { get; set; }

        public static Canvas GetCanvas() {
            return _spriteset?.transform.parent.GetComponent<Canvas>();
        }
        public static Canvas GetCanvasUI() {
            return _commandWindowParent?.transform.parent.GetComponent<Canvas>();
        }

        /// <summary>
        /// 戦闘の設定
        /// </summary>
        /// <param name="troopId"></param>
        /// <param name="canEscape"></param>
        /// <param name="canLose"></param>
        /// <param name="sceneBattle"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void Setup(string troopId, bool canEscape, bool canLose, SceneBattle sceneBattle, bool battleTest) {
#else
        public static async Task Setup(string troopId, bool canEscape, bool canLose, SceneBattle sceneBattle, bool battleTest) {
#endif
            //SceneBattle保持
            SceneBattle = sceneBattle;
            //バトル用の変数の初期化処理
            InitMembers();
            //逃走可能、敗北可能フラグを保持
            _canEscape = canEscape;
            _canLose = canLose;

            //パーティの刷新
            if (battleTest)
            {
                var party = new GameParty();
                DataManager.Self().SetGamePartyBattleTest(party);
            }
            else
            {
                //GameActorを念のため、最新の状態にする
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var actors = DataManager.Self().GetGameParty().Actors;
#else
                var actors = await DataManager.Self().GetGameParty().GetActors();
#endif
                for (int i = 0; i < actors.Count; i++)
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    actors[i].ResetActorData(true);
#else
                    await actors[i].ResetActorData(true);
#endif
            }

            //敵グループ設定
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataManager.Self().SetTroopForBattle(new GameTroop(troopId));
#else
            var gameTroop = new GameTroop(troopId);
            await gameTroop.InitForConstructor(troopId);
            await DataManager.Self().SetTroopForBattle(gameTroop);
#endif

            //逃走確率作成
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            MakeEscapeRatio();
#else
            await MakeEscapeRatio();
#endif
        }

        /// <summary>
        /// メンバ変数の初期化
        /// </summary>
        public static void InitMembers() {
            _phase = "init";
            _canEscape = false;
            _canLose = false;
            _battleTest = false;
            _preemptive = false;
            _surprise = false;
            _actorIndex = -1;
            _actionForced = false;
            _actionForcedBattler = null;
            _mapBgm = null;
            _mapBgs = null;
            _actionBattlers = new List<GameBattler>();
            _subject = null;
            _action = null;
            _targets = new List<GameBattler>();
            _targetMyself = null;
            _logWindow = null;
            _statusWindow = null;
            _spriteset = null;
            _escapeRatio = 0;
            _escaped = false;
            _rewards = new BattleRewards();
            _turnForced = false;
        }

        /// <summary>
        /// [戦闘テスト]での実行か
        /// </summary>
        /// <returns></returns>
        public static bool IsBattleTest() {
            return _battleTest;
        }

        /// <summary>
        /// [テスト戦闘]状態か設定
        /// </summary>
        /// <param name="battleTest"></param>
        public static void SetBattleTest(bool battleTest) {
            _battleTest = battleTest;
        }

        /// <summary>
        /// ログウィンドウを取得
        /// </summary>
        /// <param name="logWindow"></param>
        public static WindowBattleLog GetLogWindow() {
            return _logWindow;
        }

        /// <summary>
        /// ログウィンドウを設定
        /// </summary>
        /// <param name="logWindow"></param>
        public static void SetLogWindow(WindowBattleLog logWindow) {
            _logWindow = logWindow;
        }

        /// <summary>
        /// ステータスウィンドウを設定
        /// </summary>
        /// <param name="statusWindow"></param>
        public static void SetStatusWindow(WindowBattleStatus statusWindow) {
            _statusWindow = statusWindow;
        }

        public static void SetCommandWindowParent(GameObject commandWindowParent) {
            _commandWindowParent = commandWindowParent;
        }
        public static GameObject GetCommandWindowParent() {
            return _commandWindowParent;
        }

        /// <summary>
        /// スプライトセットを設定
        /// </summary>
        /// <param name="spriteset"></param>
        public static void SetSpriteset(SpritesetBattle spriteset) {
            _spriteset = spriteset;
        }

        /// <summary>
        /// スプライトセットを取得
        /// </summary>
        /// <returns></returns>
        public static SpritesetBattle GetSpriteSet() {
            return _spriteset;
        }

        /// <summary>
        /// エンカウント時に呼ばれるハンドラ。 [先制攻撃][不意打ち]の判定
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void OnEncounter() {
            _preemptive = TforuUtility.MathRandom() < RatePreemptive();
            _surprise = TforuUtility.MathRandom() < RateSurprise() && !_preemptive;
        }
#else
        public static async Task OnEncounter() {
            _preemptive = TforuUtility.MathRandom() < await RatePreemptive();
            _surprise = TforuUtility.MathRandom() < await RateSurprise() && !_preemptive;
        }
#endif

        /// <summary>
        /// 先制攻撃の確率
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static double RatePreemptive() {
            return DataManager.Self().GetGameParty().RatePreemptive(DataManager.Self().GetGameTroop().Agility());
        }
#else
        public static async Task<double> RatePreemptive() {
            return await DataManager.Self().GetGameParty().RatePreemptive(await DataManager.Self().GetGameTroop().Agility());
        }
#endif

        /// <summary>
        /// 不意打ちの確率
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static double RateSurprise() {
            return DataManager.Self().GetGameParty().RateSurprise(DataManager.Self().GetGameTroop().Agility());
        }
#else
        public static async Task<double> RateSurprise() {
            return await DataManager.Self().GetGameParty().RateSurprise(await DataManager.Self().GetGameTroop().Agility());
        }
#endif

        /// <summary>
        /// 戦闘BGMを再生
        /// </summary>
        public static async void PlayBattleBgm(SoundCommonDataModel bgm) {
            _mapBgm = SoundManager.Self().GetBgmSound();
            _mapBgs = SoundManager.Self().GetBgsSound();
            SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_BGM,
                bgm);
            await SoundManager.Self().PlayBgm();
            SoundManager.Self().StopBgs();
        }

        /// <summary>
        /// 勝利MEを再生
        /// </summary>
        public static async void PlayVictoryMe() {
            SoundManager.Self().StopBgm();
            SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_ME,
                new SoundCommonDataModel(
                    DataManager.Self().GetRuntimeSaveDataModel().runtimeSystemConfig.victoryMe.name,
                    DataManager.Self().GetRuntimeSaveDataModel().runtimeSystemConfig.victoryMe.pan,
                    DataManager.Self().GetRuntimeSaveDataModel().runtimeSystemConfig.victoryMe.pitch,
                    DataManager.Self().GetRuntimeSaveDataModel().runtimeSystemConfig.victoryMe.volume
                ));
            await SoundManager.Self().PlayMe();
        }

        /// <summary>
        /// 敗北MEを再生
        /// </summary>
        public static async void PlayDefeatMe() {
            SoundManager.Self().StopBgm();
            SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_ME,
                new SoundCommonDataModel(
                    DataManager.Self().GetRuntimeSaveDataModel().runtimeSystemConfig.defeatMe.name,
                    DataManager.Self().GetRuntimeSaveDataModel().runtimeSystemConfig.defeatMe.pan,
                    DataManager.Self().GetRuntimeSaveDataModel().runtimeSystemConfig.defeatMe.pitch,
                    DataManager.Self().GetRuntimeSaveDataModel().runtimeSystemConfig.defeatMe.volume
                ));
            await SoundManager.Self().PlayMe();
        }

        /// <summary>
        /// BGMとBGSの続きを再生
        /// </summary>
        public static async void ReplayBgmAndBgs() {
            SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_BGM, _mapBgm);
            SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_BGS, _mapBgs);

            await SoundManager.Self().PlayBgm();
            await SoundManager.Self().PlayBgs();
        }

        /// <summary>
        /// 逃走確率を設定
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void MakeEscapeRatio() {
            _escapeRatio = 0.5 * DataManager.Self().GetGameParty().Agility() /
                           DataManager.Self().GetGameTroop().Agility();
        }
#else
        public static async Task MakeEscapeRatio() {
            _escapeRatio = 0.5 * await DataManager.Self().GetGameParty().Agility() /
                           await DataManager.Self().GetGameTroop().Agility();
        }
#endif

        /// <summary>
        /// Update処理
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void UpdateBattleProcess() {
#else
        public static async Task UpdateBattleProcess() {
#endif
            if (!IsBusy()) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                if (!UpdateEvent())
#else
                if (!await UpdateEvent())
#endif
                {
                    switch (_phase)
                    {
                        case "start":
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                            StartInput();
#else
                            await StartInput();
#endif
                            break;
                        case "turn":
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                            UpdateTurn();
#else
                            await UpdateTurn();
#endif
                            break;
                        case "action":
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                            UpdateAction();
#else
                            await UpdateAction();
#endif
                            break;
                        case "turnEnd":
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                            UpdateTurnEnd();
#else
                            await UpdateTurnEnd();
#endif
                            break;
                        case "battleEnd":
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                            UpdateBattleEnd();
#else
                            await UpdateBattleEnd();
#endif
                            break;
                    }
                }
                else
                {
                    if (!_actionForced)
                    {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        _logWindow.Hide();
                        _statusWindow.Hide();
#else
                        await _logWindow.Hide();
                        await _statusWindow.Hide();
#endif
                    }
                    else
                    {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        _statusWindow.Hide();
#else
                        await _statusWindow.Hide();
#endif
                    }
                }
            }
        }

        /// <summary>
        /// イベントのアップデートを行い、何か実行されたか返す
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static bool UpdateEvent() {
#else
        public static async Task<bool> UpdateEvent() {
#endif
            //イベントを実行中であればtrue
            if (GameStateHandler.CurrentGameState() == GameStateHandler.GameState.BATTLE_EVENT)
                return true;

            switch (_phase)
            {
                case "start":
                case "turn":
                case "turnEnd":
                    if (IsActionForced())
                    {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        return ProcessForcedAction();
#else
                        return await ProcessForcedAction();
#endif
                    }
                    else
                    {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        return UpdateEventMain();
#else
                        return await UpdateEventMain();
#endif
                    }
            }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            return CheckAbort();
#else
            return await CheckAbort();
#endif
        }

        /// <summary>
        /// イベント主要部分のアップデートを行い、何か実行されたか返す
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static bool UpdateEventMain() {
#else
        public static async Task<bool> UpdateEventMain() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataManager.Self().GetGameParty().RequestMotionRefresh();
#else
            await DataManager.Self().GetGameParty().RequestMotionRefresh();
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            if (DataManager.Self().GetGameTroop().IsEventRunning() || CheckBattleEnd())
#else
            if (DataManager.Self().GetGameTroop().IsEventRunning() || await CheckBattleEnd())
#endif
            {
                return true;
            }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataManager.Self().GetGameTroop().SetupBattleEvent();
#else
            await DataManager.Self().GetGameTroop().SetupBattleEvent();
#endif
            if (DataManager.Self().GetGameTroop().IsEventRunning())
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// メッセージ表示などの処理中か
        /// </summary>
        /// <returns></returns>
        public static bool IsBusy() {
            var ret = DataManager.Self().GetGameMessage().IsBusy() || (_logWindow?.IsBusy() ?? false) || (_spriteset?.IsBusy() ?? false);
            return ret;
        }

        /// <summary>
        /// 入力中か
        /// </summary>
        /// <returns></returns>
        public static bool IsInputting() {
            return _phase == "input";
        }

        /// <summary>
        /// ターンの最中か
        /// </summary>
        /// <returns></returns>
        public static bool IsInTurn() {
            return _phase == "turn";
        }

        /// <summary>
        /// ターンの終了状態か
        /// </summary>
        /// <returns></returns>
        public static bool IsTurnEnd() {
            return _phase == "turnEnd";
        }

        /// <summary>
        /// 中断処理中か
        /// </summary>
        /// <returns></returns>
        public static bool IsAborting() {
            return _phase == "aborting";
        }

        /// <summary>
        /// 戦闘終了状態(敵か味方が全滅)か
        /// </summary>
        /// <returns></returns>
        public static bool IsBattleEnd() {
            return _phase == "battleEnd";
        }

        /// <summary>
        /// [逃走可]か
        /// </summary>
        /// <returns></returns>
        public static bool CanEscape() {
            return _canEscape;
        }

        /// <summary>
        /// [敗北可]か
        /// </summary>
        /// <returns></returns>
        public static bool CanLose() {
            return _canLose;
        }

        /// <summary>
        /// 逃走完了したか
        /// </summary>
        /// <returns></returns>
        public static bool IsEscaped() {
            return _escaped;
        }

        /// <summary>
        /// アクターを返す
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static GameActor Actor() {
#else
        public static async Task<GameActor> Actor() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            return _actorIndex >= 0 && _actorIndex < DataManager.Self().GetGameParty().Members().Count
                ? (GameActor) DataManager.Self().GetGameParty().Members()[_actorIndex]
#else
            var members = await DataManager.Self().GetGameParty().Members();
            return _actorIndex >= 0 && _actorIndex < members.Count
                ? (GameActor) members[_actorIndex]
#endif
                : null;
        }

        /// <summary>
        /// アクターの順番を初期位置に戻す
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void ClearActor() {
            ChangeActor(-1, GameBattler.ActionStateEnum.Null);
        }
#else
        public static async Task ClearActor() {
            await ChangeActor(-1, GameBattler.ActionStateEnum.Null);
        }
#endif

        /// <summary>
        /// アクターの変更
        /// </summary>
        /// <param name="newActorIndex"></param>
        /// <param name="lastActorActionState"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void ChangeActor(int newActorIndex, GameBattler.ActionStateEnum lastActorActionState) {
#else
        public static async Task ChangeActor(int newActorIndex, GameBattler.ActionStateEnum lastActorActionState) {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var lastActor = Actor();
#else
            var lastActor = await Actor();
#endif
            lastActor?.SetActionState(lastActorActionState);

            _actorIndex = newActorIndex;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var newActor = Actor();
#else
            var newActor = await Actor();
#endif
            newActor?.SetActionState(GameBattler.ActionStateEnum.Inputting);
        }

        /// <summary>
        /// 戦闘開始
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void StartBattle() {
#else
        public static async Task StartBattle() {
#endif
            _phase = "start";
            //戦闘回数をインクリメントする
            DataManager.Self().GetRuntimeSaveDataModel().runtimeSystemConfig.battleCount++;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataManager.Self().GetGameParty().OnBattleStart();
            DataManager.Self().GetGameTroop().OnBattleStart();
            DisplayStartMessages();
#else
            await DataManager.Self().GetGameParty().OnBattleStart();
            await DataManager.Self().GetGameTroop().OnBattleStart();
            await DisplayStartMessages();
#endif

            //開始時点では、ログWindowは非表示
            _logWindow.gameObject.SetActive(false);
        }

        /// <summary>
        /// [出現]メッセージを表示
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void DisplayStartMessages() {
#else
        public static async Task DisplayStartMessages() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataManager.Self().GetGameTroop().EnemyNames().ForEach(name =>
#else
            (await DataManager.Self().GetGameTroop().EnemyNames()).ForEach(name =>
#endif
            {
                DataManager.Self().GetGameMessage().Add(TextManager.Format(TextManager.emerge, name));
            });
            if (_preemptive)
                DataManager.Self().GetGameMessage()
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    .Add(TextManager.Format(TextManager.preemptive, DataManager.Self().GetGameParty().Name()));
#else
                    .Add(TextManager.Format(TextManager.preemptive, await DataManager.Self().GetGameParty().Name()));
#endif
            else if (_surprise)
                DataManager.Self().GetGameMessage()
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    .Add(TextManager.Format(TextManager.surprise, DataManager.Self().GetGameParty().Name()));
#else
                    .Add(TextManager.Format(TextManager.surprise, await DataManager.Self().GetGameParty().Name()));
#endif
        }

        /// <summary>
        /// 入力開始
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void StartInput() {
#else
        public static async Task StartInput() {
#endif
            _phase = "input";
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataManager.Self().GetGameTroop().AppearEnemy();
            DataManager.Self().GetGameParty().MakeActions();
            DataManager.Self().GetGameTroop().MakeActions();
            ClearActor();
            if (_surprise || !DataManager.Self().GetGameParty().CanInput()) StartTurn();
#else
            await DataManager.Self().GetGameTroop().AppearEnemy();
            await DataManager.Self().GetGameParty().MakeActions();
            await DataManager.Self().GetGameTroop().MakeActions();
            await ClearActor();
            if (_surprise || !await DataManager.Self().GetGameParty().CanInput()) await StartTurn();
#endif
        }

        /// <summary>
        /// 入力中のアクターのアクションを返す
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static GameAction InputtingAction() {
#else
        public static async Task<GameAction> InputtingAction() {
#endif
            if (_phase != "input") return null;
            try
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                return Actor()?.InputtingAction();
#else
                return (await Actor())?.InputtingAction();
#endif
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// ひとつ先のコマンドを選択
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void SelectNextCommand() {
#else
        public static async Task SelectNextCommand() {
#endif
            if (_phase != "input") return;
            do
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                if (Actor() == null || !Actor().SelectNextCommand())
#else
                var actor = await Actor();
                if (actor == null || !actor.SelectNextCommand())
#endif
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    ChangeActor(_actorIndex + 1, GameBattler.ActionStateEnum.Waiting);
                    if (_actorIndex >= DataManager.Self().GetGameParty().Size())
#else
                    await ChangeActor(_actorIndex + 1, GameBattler.ActionStateEnum.Waiting);
                    if (_actorIndex >= await DataManager.Self().GetGameParty().Size())
#endif
                    {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        StartTurn();
#else
                        await StartTurn();
#endif
                        break;
                    }
                }
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            } while (Actor().CanInput() == false);
#else
            } while ((await Actor()).CanInput() == false);
#endif
        }

        /// <summary>
        /// ひとつ前のコマンドを選択
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void SelectPreviousCommand() {
#else
        public static async Task SelectPreviousCommand() {
#endif
            do
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                if (Actor() == null || !Actor().SelectPreviousCommand())
#else
                var actor = await Actor();
                if (actor == null || !actor.SelectPreviousCommand())
#endif
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    ChangeActor(_actorIndex - 1, GameBattler.ActionStateEnum.Undecided);
#else
                    await ChangeActor(_actorIndex - 1, GameBattler.ActionStateEnum.Undecided);
#endif
                    if (_actorIndex < 0) return;
                }
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            } while (!Actor().CanInput());
#else
            } while (!(await Actor()).CanInput());
#endif
        }

        /// <summary>
        /// [ステータス]表示を再描画
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void RefreshStatus() {
            _statusWindow.Refresh();
        }
#else
        public static async Task RefreshStatus() {
            await _statusWindow.Refresh();
        }
#endif

        /// <summary>
        /// ターン開始
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void StartTurn() {
#else
        public static async Task StartTurn() {
#endif
            _phase = "turn";
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            ClearActor();
#else
            await ClearActor();
#endif
            DataManager.Self().GetGameTroop().IncreaseTurn();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            MakeActionOrders();
            DataManager.Self().GetGameParty().RequestMotionRefresh();
#else
            await MakeActionOrders();
            await DataManager.Self().GetGameParty().RequestMotionRefresh();
#endif
            _logWindow.Open();
            _logWindow.StartTurn();

            //ターン継続中は、ログWindowを表示し、ステータスWindowを非表示にする
            _statusWindow.gameObject.SetActive(false);
            _logWindow.gameObject.SetActive(true);
        }

        /// <summary>
        /// ターンのアップデート
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void UpdateTurn() {
#else
        public static async Task UpdateTurn() {
#endif
            //ターン継続中は、ログWindowを表示
            _logWindow.gameObject.SetActive(true);

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataManager.Self().GetGameParty().RequestMotionRefresh();
#else
            await DataManager.Self().GetGameParty().RequestMotionRefresh();
#endif
            if (_subject == null)
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _subject = GetNextSubject();
#else
                _subject = await GetNextSubject();
#endif

            if (_subject != null)
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                ProcessTurn();
#else
                await ProcessTurn();
#endif
            else
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                EndTurn();
#else
                await EndTurn();
#endif
        }

        /// <summary>
        /// ターン継続処理
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void ProcessTurn() {
#else
        public static async Task ProcessTurn() {
#endif
            var subject = _subject;
            var action = subject.CurrentAction();
            if (action != null)
            {
                action.Prepare();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                if (action.IsValid()) StartAction();
#else
                if (await action.IsValid()) await StartAction();
#endif
                subject.RemoveCurrentAction();
            }
            else
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                subject.OnAllActionsEnd();
                RefreshStatus();
                _logWindow.Show();
#else
                await subject.OnAllActionsEnd();
                await RefreshStatus();
                await _logWindow.Show();
#endif
                _logWindow.DisplayAutoAffectedStatus(subject);
                _logWindow.DisplayCurrentState(subject);
                _logWindow.DisplayRegeneration(subject);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _subject = GetNextSubject();
#else
                _subject = await GetNextSubject();
#endif
            }
        }

        /// <summary>
        /// ターン終了処理
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void EndTurn() {
#else
        public static async Task EndTurn() {
#endif
            _phase = "turnEnd";
            _preemptive = false;
            _surprise = false;
            bool turnForced = _turnForced;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            AllBattleMembers().ForEach(battler =>
#else
            foreach (var battler in await AllBattleMembers())
#endif
            {
                if (!turnForced)
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    battler.OnTurnEnd();
                    RefreshStatus();
#else
                    await battler.OnTurnEnd();
                    await RefreshStatus();
#endif
                    _logWindow.DisplayAutoAffectedStatus(battler);
                    _logWindow.DisplayRegeneration(battler);
                }
                else
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    RefreshStatus();
#else
                    await RefreshStatus();
#endif
                }
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            });
#else
            }
#endif
            if (IsForcedTurn())
            {
                _actionForced = false;
                _turnForced = false;
                BattleEventCommandChainLauncher.ResumeEvent();
            }

            _logWindow.Close();

            //ターン終了後は、ログWindowを非表示にし、ステータスWindowを表示する
            //バトルイベントが継続していた場合には、ステータスWindowは表示せず、イベント終了後に表示とする
            _logWindow.gameObject.SetActive(false);
            if (GameStateHandler.CurrentGameState() != GameStateHandler.GameState.BATTLE_EVENT)
                _statusWindow.gameObject.SetActive(true);
        }

        /// <summary>
        /// 強制されたターンか
        /// </summary>
        /// <returns></returns>
        public static bool IsForcedTurn() {
            return _turnForced;
        }

        /// <summary>
        /// ターン終了のアップテート
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void UpdateTurnEnd() {
            StartInput();
        }
#else
        public static async Task UpdateTurnEnd() {
            await StartInput();
        }
#endif

        /// <summary>
        /// 次の対象バトラーを返す
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static GameBattler GetNextSubject() {
#else
        public static async Task<GameBattler> GetNextSubject() {
#endif
            for (;;)
            {
                if (_actionBattlers.Count <= 0) return null;

                var battler = _actionBattlers[0];
                _actionBattlers.RemoveAt(0);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                if (battler.IsBattleMember() && battler.IsAlive()) return battler;
#else
                if (await battler.IsBattleMember() && battler.IsAlive()) return battler;
#endif
            }
        }

        /// <summary>
        /// 戦闘に参加している全バトラーを返す
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static List<GameBattler> AllBattleMembers() {
#else
        public static async Task<List<GameBattler>> AllBattleMembers() {
#endif
            var members = new List<GameBattler>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataManager.Self().GetGameParty().Members().Aggregate(members, (m, a) =>
#else
            (await DataManager.Self().GetGameParty().Members()).Aggregate(members, (m, a) =>
#endif
            {
                m.Add(a);
                return m;
            });
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataManager.Self().GetGameTroop().Members().Aggregate(members, (m, e) =>
#else
            (await DataManager.Self().GetGameTroop().Members()).Aggregate(members, (m, e) =>
#endif
            {
                m.Add(e);
                return m;
            });
            return members;
        }

        /// <summary>
        /// アクションの順番を設定
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void MakeActionOrders() {
#else
        public static async Task MakeActionOrders() {
#endif
            var battlers = new List<GameBattler>();

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            if (!_surprise) DataManager.Self().GetGameParty().Members().ForEach(actor => battlers.Add(actor));

            if (!_preemptive) DataManager.Self().GetGameTroop().Members().ForEach(enemy => battlers.Add(enemy));
#else
            if (!_surprise) (await DataManager.Self().GetGameParty().Members()).ForEach(actor => battlers.Add(actor));

            if (!_preemptive) (await DataManager.Self().GetGameTroop().Members()).ForEach(enemy => battlers.Add(enemy));
#endif

            battlers.ForEach(battler => { battler.MakeSpeed(); });
            battlers.Sort((a, b) => (int) b.Speed - (int) a.Speed);
            _actionBattlers = battlers;
        }

        /// <summary>
        /// アクション開始
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void StartAction() {
#else
        public static async Task StartAction() {
#endif
            var subject = _subject;
            var action = subject.CurrentAction();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var targets = action.MakeTargets();
#else
            var targets = await action.MakeTargets();
#endif
            _phase = "action";
            _action = action;
            _targets = targets;
            _targetMyself = action.MakeTargetMyself();
            _isTargetMyself = _targetMyself != null ? true : false;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            subject.UseItem(action.Item);
#else
            await subject.UseItem(action.Item);
#endif
            _action.ApplyGlobal();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            RefreshStatus();
#else
            await RefreshStatus();
#endif
            //行動ターゲットとして、使用者を追加
            _logWindow.StartAction(subject, action, targets, _targetMyself);
        }

        /// <summary>
        /// アクションのアップデート
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void UpdateAction() {
#else
        public static async Task UpdateAction() {
#endif
            if (_targets.Count > 0)
            {
                var target = _targets[0];
                _targets.RemoveAt(0);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                InvokeAction(_subject, target);
#else
                await InvokeAction(_subject, target);
#endif
            }
            else if (_targetMyself != null)
            {
                var targetMyself = _targetMyself;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                InvokeActionMyself(_subject, _subject);
#else
                await InvokeActionMyself(_subject, _subject);
#endif
                _targetMyself = null;
            }
            else
            {
                //コモンイベントが存在する場合はキューに貯める
                bool isCommonEvent = false;
                bool isCommonEventForUser = false;

                //コモンイベントが設定されているか
                foreach (var effect in _action.Item.Effects)
                {
                    if (isCommonEvent) break;
                    isCommonEvent = _action.IsEffectCommonEvent(effect);
                }

                //使用者への影響にチェックが入っている場合、使用者への影響側のコモンイベントも確認
                if (_action.IsForUser())
                    foreach (var effect in _action.Item.EffectsMyself)
                    {
                        if (isCommonEventForUser) break;
                        isCommonEventForUser = _action.IsEffectCommonEvent(effect);
                    }

                //いずれかのコモンイベントが存在した場合は、Inspector上最後に設定されているコモンイベントを実行
                if (isCommonEvent || isCommonEventForUser)
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _action.SetCommonEvent(isCommonEventForUser);
#else
                    await _action.SetCommonEvent(isCommonEventForUser);
#endif
                }

                _isTargetMyself = false;

                //Actionは終了
                EndAction();
            }
        }

        /// <summary>
        /// 行動終了処理
        /// </summary>
        public static void EndAction() {
            _logWindow.EndAction(_subject);
            _phase = "turn";
        }

        /// <summary>
        /// 指定対象が指定目標に対してのアクションを起動する
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="target"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void InvokeAction(GameBattler subject, GameBattler target) {
#else
        public static async Task InvokeAction(GameBattler subject, GameBattler target) {
#endif
            _logWindow.Push(_logWindow.PushBaseLine);
            if (TforuUtility.MathRandom() < _action.ItemCnt(target))
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                InvokeCounterAttack(subject, target);
#else
                await InvokeCounterAttack(subject, target);
#endif
            else if (TforuUtility.MathRandom() < _action.ItemMrf(target))
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                InvokeMagicReflection(subject, target);
#else
                await InvokeMagicReflection(subject, target);
#endif
            else
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                InvokeNormalAction(subject, target);
#else
                await InvokeNormalAction(subject, target);
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            subject.SetLastTarget(target);
#else
            await subject.SetLastTarget(target);
#endif
            _logWindow.Push(_logWindow.PopBaseLine);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            RefreshStatus();
#else
            await RefreshStatus();
#endif
        }

        /// <summary>
        /// 指定対象が指定目標に対しての通常アクションを起動する
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="target"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void InvokeNormalAction(GameBattler subject, GameBattler target) {
#else
        public static async Task InvokeNormalAction(GameBattler subject, GameBattler target) {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var realTarget = ApplySubstitute(target);
            _action.Apply(realTarget);
#else
            var realTarget = await ApplySubstitute(target);
            await _action.Apply(realTarget);
#endif
            _logWindow.DisplayActionResults(subject, realTarget);
        }

        /// <summary>
        /// 指定対象が指定目標に対してのアクションを起動する（使用者への影響）
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="target"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void InvokeActionMyself(GameBattler subject, GameBattler target) {
#else
        public static async Task InvokeActionMyself(GameBattler subject, GameBattler target) {
#endif
            _logWindow.Push(_logWindow.PushBaseLine);

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var realTarget = ApplySubstitute(target);
            _action.ApplyMyself(realTarget);
#else
            var realTarget = await ApplySubstitute(target);
            await _action.ApplyMyself(realTarget);
#endif
            _logWindow.DisplayActionResults(subject, realTarget);

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            subject.SetLastTarget(target);
#else
            await subject.SetLastTarget(target);
#endif
            _logWindow.Push(_logWindow.PopBaseLine);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            RefreshStatus();
#else
            await RefreshStatus();
#endif
        }

        /// <summary>
        /// 指定対象が指定目標に対しての反撃アクションを起動する
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="target"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void InvokeCounterAttack(GameBattler subject, GameBattler target) {
#else
        public static async Task InvokeCounterAttack(GameBattler subject, GameBattler target) {
#endif
            var action = new GameAction(target);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            await action.InitForConstructor(target);
#endif
            action.SetAttack();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            action.Apply(subject);
#else
            await action.Apply(subject);
#endif
            _logWindow.DisplayCounter(target);
            _logWindow.DisplayActionResults(target, subject);
        }

        /// <summary>
        /// 指定対象が指定目標に対しての魔法反射アクションを起動する
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="target"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void InvokeMagicReflection(GameBattler subject, GameBattler target) {
#else
        public static async Task InvokeMagicReflection(GameBattler subject, GameBattler target) {
#endif
            _action._reflectionTarget = target;
            _logWindow.DisplayReflection(target);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _action.Apply(subject);
#else
            await _action.Apply(subject);
#endif
            _logWindow.DisplayActionResults(target, subject);
        }

        /// <summary>
        /// 対象が死んでいるなどしたら、代わりを選んで返す。 問題なければ、対象をそのまま返す
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static GameBattler ApplySubstitute(GameBattler target) {
#else
        public static async Task<GameBattler> ApplySubstitute(GameBattler target) {
#endif
            if (CheckSubstitute(target))
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var substitute = target.FriendsUnit().SubstituteBattler();
#else
                var substitute = await target.FriendsUnit().SubstituteBattler();
#endif
                if (substitute != null && target != substitute)
                {
                    _logWindow.DisplaySubstitute(substitute, target);
                    return substitute;
                }
            }

            return target;
        }

        /// <summary>
        /// 対象が死んでいるなどして代わりが必要か返す
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static bool CheckSubstitute(GameBattler target) {
            return target.IsDying() && !_action.IsCertainHit();
        }

        /// <summary>
        /// 強制行動中か
        /// </summary>
        /// <returns></returns>
        public static bool IsActionForced() {
            return _actionForced;
        }

        /// <summary>
        /// 強制行動
        /// </summary>
        /// <param name="battler"></param>
        public static void ForceAction(GameBattler battler) {
            _actionForced = true;
            _actionForcedBattler = battler;
            var index = _actionBattlers.IndexOf(battler);
            if (index >= 0) _actionBattlers.RemoveAt(index);
        }

        /// <summary>
        /// 強制アクションの処理
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static bool ProcessForcedAction() {
#else
        public static async Task<bool> ProcessForcedAction() {
#endif
            if (_actionForcedBattler != null)
            {
                //強制行動中は、ログWindowを表示し、ステータスWindowを非表示にする
                _logWindow.Open();
                _logWindow.StartTurn();
                _statusWindow.gameObject.SetActive(false);
                _logWindow.gameObject.SetActive(true);

                _turnForced = true;
                _subject = _actionForcedBattler;
                _actionForcedBattler = null;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                StartAction();
#else
                await StartAction();
#endif
                _subject.RemoveCurrentAction();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 中止
        /// </summary>
        public static void Abort() {
            _phase = "aborting";
        }

        /// <summary>
        /// 味方か敵が全滅しているなど戦闘終了状態なら終了し、終了を実行したか返す
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static bool CheckBattleEnd() {
#else
        public static async Task<bool> CheckBattleEnd() {
#endif
            if (_phase != "")
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                if (CheckAbort())
#else
                if (await CheckAbort())
#endif
                {
                    //バトルが終了している場合、ログWindowは非表示
                    _logWindow.gameObject.SetActive(false);
                    return true;
                }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                if (DataManager.Self().GetGameParty().IsAllDead())
#else
                if (await DataManager.Self().GetGameParty().IsAllDead())
#endif
                {
                    //バトルが終了している場合、ログWindowは非表示
                    _logWindow.gameObject.SetActive(false);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    ProcessDefeat();
#else
                    await ProcessDefeat();
#endif
                    return true;
                }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                if (DataManager.Self().GetGameTroop().IsAllDead())
#else
                if (await DataManager.Self().GetGameTroop().IsAllDead())
#endif
                {
                    //バトルが終了している場合、ログWindowは非表示
                    _logWindow.gameObject.SetActive(false);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    ProcessVictory();
#else
                    await ProcessVictory();
#endif
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// パーティがいないなど中止する状態なら中止し、中止を実行したか返す
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static bool CheckAbort() {
#else
        public static async Task<bool> CheckAbort() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            if (DataManager.Self().GetGameParty().IsEmpty() || IsAborting())
#else
            if (await DataManager.Self().GetGameParty().IsEmpty() || IsAborting())
#endif
            {
                SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_SE, DataManager.Self().GetSystemDataModel().soundSetting.escape);
                SoundManager.Self().PlaySe();
                _escaped = true;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                ProcessAbort();
#else
                await ProcessAbort();
#endif
            }

            return false;
        }

        /// <summary>
        /// 勝利処理
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void ProcessVictory() {
#else
        public static async Task ProcessVictory() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataManager.Self().GetGameParty().RemoveBattleStates();
#else
            await DataManager.Self().GetGameParty().RemoveBattleStates();
#endif
            //戦闘中に変更となったステート情報をマップに引き継ぐ
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataManager.Self().GetGameParty().AllMembers().ForEach(actor => { actor.SetBattleEndStates(); });
#else
            foreach (var actor in await DataManager.Self().GetGameParty().AllMembers())
            {
                await actor.SetBattleEndStates();
            }
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataManager.Self().GetGameParty().PerformVictory();
#else
            await DataManager.Self().GetGameParty().PerformVictory();
#endif
            PlayVictoryMe();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            MakeRewards();
            DisplayVictoryMessage();
#else
            await MakeRewards();
            await DisplayVictoryMessage();
#endif
            DisplayRewards();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            GainRewards();
#else
            await GainRewards();
#endif
            EndBattle(0);
        }

        /// <summary>
        /// 逃走処理を行い、逃走が成功したか返す
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static bool ProcessEscape() {
#else
        public static async Task<bool> ProcessEscape() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataManager.Self().GetGameParty().PerformEscape();
#else
            await DataManager.Self().GetGameParty().PerformEscape();
#endif
            var success = _preemptive ? true : TforuUtility.MathRandom() < _escapeRatio;
            if (success)
            {
                //逃走成功
                SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_SE,
                    DataManager.Self().GetSystemDataModel().soundSetting.escape);
                SoundManager.Self().PlaySe();

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                DataManager.Self().GetGameParty().RemoveBattleStates();
#else
                await DataManager.Self().GetGameParty().RemoveBattleStates();
#endif
                //戦闘中に変更となったステート情報をマップに引き継ぐ
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                DataManager.Self().GetGameParty().AllMembers().ForEach(actor => { actor.SetBattleEndStates(); });
#else
                foreach (var actor in await DataManager.Self().GetGameParty().AllMembers())
                {
                    await actor.SetBattleEndStates();
                }
#endif

                //逃走メッセージの表示を実施
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                DisplayEscapeSuccessMessage();
#else
                await DisplayEscapeSuccessMessage();
#endif
                _escaped = true;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                ProcessAbort();
#else
                await ProcessAbort();
#endif
            }
            else
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                DisplayEscapeFailureMessage();
#else
                await DisplayEscapeFailureMessage();
#endif
                _escapeRatio += 0.1;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                DataManager.Self().GetGameParty().ClearActions();
                StartTurn();
#else
                await DataManager.Self().GetGameParty().ClearActions();
                await StartTurn();
#endif
            }

            //逃走メッセージを表示する際には、一時的にログWindowを消去
            _logWindow.gameObject.SetActive(false);
            return success;
        }

        /// <summary>
        /// 中止処理
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void ProcessAbort() {
#else
        public static async Task ProcessAbort() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataManager.Self().GetGameParty().RemoveBattleStates();
#else
            await DataManager.Self().GetGameParty().RemoveBattleStates();
#endif
            //戦闘中に変更となったステート情報をマップに引き継ぐ
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataManager.Self().GetGameParty().AllMembers().ForEach(actor => { actor.SetBattleEndStates(); });
#else
            foreach (var actor in await DataManager.Self().GetGameParty().AllMembers())
            {
                await actor.SetBattleEndStates();
            }
#endif
            EndBattle(1);
        }

        /// <summary>
        /// 敗北処理
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void ProcessDefeat() {
#else
        public static async Task ProcessDefeat() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DisplayDefeatMessage();
#else
            await DisplayDefeatMessage();
#endif
            PlayDefeatMe();

            //敗北不可の場合はGAMEOVER時の音声を、別のところで鳴動する
            //敗北可の場合はマップに戻る際に、マップのBGMを再生しなおすため、ここでは処理しない
            if (!_canLose)
                SoundManager.Self().StopBgm();

            EndBattle(2);
        }

        /// <summary>
        /// 戦闘終了処理
        /// </summary>
        /// <param name="result"></param>
        public static void EndBattle(int result) {
            DataManager.Self().BattleResult = result;
            _phase = "battleEnd";

            //勝利回数と逃走回数をインクリメントする
            if (result == 0)
            {
                DataManager.Self().GetRuntimeSaveDataModel().runtimeSystemConfig.winCount++;
            }
            else if (_escaped)
            {
                DataManager.Self().GetRuntimeSaveDataModel().runtimeSystemConfig.escapeCount++;
            }
        }

        /// <summary>
        /// 戦闘終了のアップデート
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void UpdateBattleEnd() {
#else
        public static async Task UpdateBattleEnd() {
#endif
            //遷移先をMAPで初期化
            SceneBattle.NextScene = GameStateHandler.GameState.MAP;

            if (IsBattleTest())
            {
                SoundManager.Self().StopBgm();
            }
            //逃げていなくて全員死んだときの処理
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            else if (!_escaped && DataManager.Self().GetGameParty().IsAllDead())
#else
            else if (!_escaped && await DataManager.Self().GetGameParty().IsAllDead())
#endif
            {
                //負けられるかの判定
                if (_canLose)
                {
                    //負けられる時の処理
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    DataManager.Self().GetGameParty().ReviveBattleMembers();
#else
                    await DataManager.Self().GetGameParty().ReviveBattleMembers();
#endif
                }
                else
                {
                    //負けられない時の処理
                    //SceneBattle.GameOver();
                    //遷移先をGAMEOVERとする
                    SceneBattle.NextScene = GameStateHandler.GameState.GAME_OVER;
                }
            }

            //バトル終了処理
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            SceneBattle.Stop();
#else
            await SceneBattle.Stop();
#endif
            _phase = null;
        }

        /// <summary>
        /// 報酬を設定
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void MakeRewards() {
#else
        public static async Task MakeRewards() {
#endif
            _rewards = new BattleRewards
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                gold = DataManager.Self().GetGameTroop().GoldTotal(),
                exp = DataManager.Self().GetGameTroop().ExpTotal(),
                items = DataManager.Self().GetGameTroop().MakeDropItems()
#else
                gold = await DataManager.Self().GetGameTroop().GoldTotal(),
                exp = await DataManager.Self().GetGameTroop().ExpTotal(),
                items = await DataManager.Self().GetGameTroop().MakeDropItems()
#endif
            };
        }

        /// <summary>
        /// [勝利]メッセージを表示
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void DisplayVictoryMessage() {
            DataManager.Self().GetGameMessage().Add((TextManager.Format(TextManager.victory, DataManager.Self().GetGameParty().Name())));
        }
#else
        public static async Task DisplayVictoryMessage() {
            DataManager.Self().GetGameMessage().Add((TextManager.Format(TextManager.victory, await DataManager.Self().GetGameParty().Name())));
        }
#endif

        /// <summary>
        /// [敗北]メッセージを表示
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void DisplayDefeatMessage() {
            DataManager.Self().GetGameMessage().Add((TextManager.Format(TextManager.defeat, DataManager.Self().GetGameParty().Name())));
        }
#else
        public static async Task DisplayDefeatMessage() {
            DataManager.Self().GetGameMessage().Add((TextManager.Format(TextManager.defeat, await DataManager.Self().GetGameParty().Name())));
        }
#endif

        /// <summary>
        /// [逃走成功]メッセージを表示
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void DisplayEscapeSuccessMessage() {
            DataManager.Self().GetGameMessage().Add((TextManager.Format(TextManager.escapeStart, DataManager.Self().GetGameParty().Name())));
        }
#else
        public static async Task DisplayEscapeSuccessMessage() {
            DataManager.Self().GetGameMessage().Add((TextManager.Format(TextManager.escapeStart, await DataManager.Self().GetGameParty().Name())));
        }
#endif

        /// <summary>
        /// [逃走失敗]メッセージを表示
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void DisplayEscapeFailureMessage() {
            DataManager.Self().GetGameMessage().Add(TextManager.Format(TextManager.escapeStart, DataManager.Self().GetGameParty().Name()));
            DataManager.Self().GetGameMessage().Add(TextManager.Format("\\." + TextManager.escapeFailure));
        }
#else
        public static async Task DisplayEscapeFailureMessage() {
            DataManager.Self().GetGameMessage().Add(TextManager.Format(TextManager.escapeStart, await DataManager.Self().GetGameParty().Name()));
            DataManager.Self().GetGameMessage().Add(TextManager.Format("\\." + TextManager.escapeFailure));
        }
#endif

        /// <summary>
        /// 報酬(経験値・お金・アイテム)メッセージを表示
        /// </summary>
        public static void DisplayRewards() {
            DisplayExp();
            DisplayGold();
            DisplayDropItems();
        }

        /// <summary>
        /// [経験値獲得]メッセージを表示
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void DisplayExp() {
#else
        public static async void DisplayExp() {
#endif
            var exp = _rewards.exp;
            if (exp > 0)
            {
                //Uniteでは経験値を人数で割る
                //逃げたメンバーはカウントしない
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                int count = DataManager.Self().GetGameParty().AllMembers().Count;
                var members = DataManager.Self().GetGameParty().AllMembers();
#else
                var members = await DataManager.Self().GetGameParty().AllMembers();
                int count = members.Count;
#endif
                for (int i = 0; i < members.Count; i++)
                {
                    if (members[i].IsEscaped) count--;
                }
                exp = Mathf.FloorToInt(exp / count);

                var text = TextManager.Format(TextManager.obtainExp, exp.ToString(), TextManager.exp);
                DataManager.Self().GetGameMessage().Add("\\." + text);
            }
        }

        /// <summary>
        /// [お金獲得]メッセージを表示
        /// </summary>
        public static void DisplayGold() {
            var gold = _rewards.gold;
            if (gold > 0)
            {
                var text = TextManager.Format(TextManager.obtainGold, gold.ToString());
                DataManager.Self().GetGameMessage().Add("\\." + text);
            }
        }

        /// <summary>
        /// [アイテム獲得]メッセージを表示
        /// </summary>
        public static void DisplayDropItems() {
            var items = _rewards.items;
            if (items.Count > 0)
            {
                DataManager.Self().GetGameMessage().NewPage();
                items.ForEach(item =>
                {
                    var text = TextManager.Format(TextManager.obtainItem, item.Name);
                    DataManager.Self().GetGameMessage().Add(text);
                });
            }
        }

        /// <summary>
        /// 報酬(経験値・お金・アイテム)を返す
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void GainRewards() {
#else
        public static async Task GainRewards() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            GainExp();
#else
            await GainExp();
#endif
            GainGold();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            GainDropItems();
#else
            await GainDropItems();
#endif
        }

        /// <summary>
        /// [経験値]を返す
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void GainExp() {
#else
        public static async Task GainExp() {
#endif
            var exp = _rewards.exp;

            //Uniteでは経験値を人数で割る
            //逃げたメンバーはカウントしない
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            int count = DataManager.Self().GetGameParty().AllMembers().Count;
            var members = DataManager.Self().GetGameParty().AllMembers();
#else
            var members = await DataManager.Self().GetGameParty().AllMembers();
            int count = members.Count;
#endif
            for (int i = 0; i < members.Count; i++)
            {
                if (members[i].IsEscaped) count--;
            }
            exp = Mathf.FloorToInt(exp / count);

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataManager.Self().GetGameParty().AllMembers().ForEach(actor => { 
#else
            foreach (var actor in members) {
#endif
                if (!actor.IsEscaped)
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    actor.GainExp(exp);
#else
                    await actor.GainExp(exp);
#endif
                }
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            });
#else
            }
#endif
        }

        /// <summary>
        /// [お金]を返す
        /// </summary>
        public static void GainGold() {
            DataManager.Self().GetGameParty().GainGold(_rewards.gold);
        }

        /// <summary>
        /// [ドロップアイテム]を返す
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void GainDropItems() {
#else
        public static async Task GainDropItems() {
#endif
            var items = _rewards.items;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            items.ForEach(item => { DataManager.Self().GetGameParty().GainItem(item, 1); });
#else
            foreach (var item in items)
            {
                await DataManager.Self().GetGameParty().GainItem(item, 1);
            }
#endif
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod()]
        static void Init() {
            IsBattle = false;
        }
#endif
    }

    /// <summary>
    /// 報酬
    /// </summary>
    public class BattleRewards
    {
        public int            exp;
        public int            gold;
        public List<GameItem> items;
    }
}