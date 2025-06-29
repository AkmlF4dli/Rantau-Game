using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.EventBattle;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.EventMap;
using RPGMaker.Codebase.CoreSystem.Knowledge.Enum;
using RPGMaker.Codebase.CoreSystem.Service.EventManagement;
using RPGMaker.Codebase.Runtime.Battle.Objects;
using RPGMaker.Codebase.Runtime.Common;
using RPGMaker.Codebase.Runtime.Event;
using RPGMaker.Codebase.Runtime.Map;
using System;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Battle
{
    public class BattleEventCommandChainLauncher : AbstractEventCommandChainLauncher
    {
        /// <summary>
        ///     EventBattleDataModel
        /// </summary>
        private EventBattleDataModel _eventBattleData;

        /// <summary>
        ///     EventDataModel
        /// </summary>
        private EventDataModel _eventDataModelEntity;

        /// <summary>
        ///     GameTroop
        /// </summary>
        public GameTroop GameTroop;

        /// <summary>
        /// イベントを一時的に中断しているかどうか
        /// バトルでは同時に1イベントしか実行されないため、static で持つ
        /// </summary>
        private static bool _isPause = false;

        /// <summary>
        /// イベントを一時的に中断する場合に、再開する際のCB
        /// バトルでは同時に1イベントしか実行されないため、static で持つ
        /// </summary>
        private static Action _resumeAction = null;

        /// <summary>
        ///     コンストラクタ.
        /// </summary>
        public BattleEventCommandChainLauncher() {
            _eventManagementService = new EventManagementService();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
        }
        public async Task InitForConstructor() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _eventCommonDataModels = _eventManagementService.LoadEventCommon();
#else
            _eventCommonDataModels = await _eventManagementService.LoadEventCommon();
#endif
            Init();
        }

        /// <summary>
        ///     バトルイベントに利用するデータ設定
        /// </summary>
        /// <param name="eventDataModelData">EventDataModel</param>
        /// <param name="eventBattleData">EventBattleDataModel</param>
        public void SetEventData(
            EventDataModel eventDataModelData,
            EventBattleDataModel eventBattleData
        ) {
            _eventDataModelEntity = eventDataModelData.Clone();
            _eventBattleData = eventBattleData;

            //インデントを振りなおしておく
            _eventManagementService.SetEventIndent(_eventDataModelEntity);
        }

        /// <summary>
        /// 現在実行中のイベントを一時中断
        /// メニュー表示やバトル表示など、別の画面へ遷移する際に利用
        /// </summary>
        public static void PauseEvent(Action callback) {
            _isPause = true;
            _resumeAction = callback;
            GameStateHandler.SetGameState(GameStateHandler.GameState.BATTLE);
        }

        /// <summary>
        /// 実行を中断していたイベントの再開
        /// </summary>
        public static void ResumeEvent() {
            //イベント再開時に状態を復帰する
            if (_isPause)
            {
                //先にフラグを落とす
                _isPause = false;
                if (_resumeAction != null)
                {
                    //復帰するので、GameStateをEVENTに変更する
                    GameStateHandler.SetGameState(GameStateHandler.GameState.BATTLE_EVENT);

                    //先にメンバ変数を解放する
                    Action resumeAction = _resumeAction;
                    _resumeAction = null;

                    //実行する
                    resumeAction();
                }
            }
        }

        /// <summary>
        ///     初期化処理.
        /// </summary>
        public void Init() {
            _running = false;
            _isPause = false;
            _resumeAction = null;
            _eventDataModelEntity = null;
            _eventBattleData = null;
        }

        /// <summary>
        ///     イベント実行開始
        /// </summary>
        /// <param name="callBackEvent">イベント終了後に実行するコールバック</param>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public bool Starting(Action<EventMapDataModel, EventDataModel> callBackEvent) {
#else
        public async Task<bool> Starting(Action<EventMapDataModel, EventDataModel> callBackEvent) {
#endif
            if (_running) return false;

            if (_eventDataModelEntity == null || _eventDataModelEntity == null ||
                _eventDataModelEntity.eventCommands.Count == 0)
                return false;

            var eventDataModelData = _eventDataModelEntity.Clone();
            _eventDataModelEntity = eventDataModelData;
            _commandsInQueue = _eventDataModelEntity.eventCommands.DataClone();
            _currentCommandIndex = -1;

            _running = true;
            _callback = callBackEvent;

            //状態の更新
            GameStateHandler.SetGameState(GameStateHandler.GameState.BATTLE_EVENT);

            //イベント実行
            _commandEventID = _eventDataModelEntity.id;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            ProcessCommand(_eventDataModelEntity.id);
#else
            await ProcessCommandAsync(_eventDataModelEntity.id);
#endif
            return true;
        }


        /// <summary>
        ///     イベントコマンドを実行する.
        /// </summary>
        /// <param name="eventId"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public override void ProcessCommand(string eventId) {
#else
        public override async Task ProcessCommandAsync(string eventId) {
#endif
            if (_commandEventID != eventId) return;

            _currentCommandIndex++;

            if (_currentCommandIndex >= _commandsInQueue.Count ||
                _running == false)
            {
                // イベントコマンドチェーンの終了
                EndDelayCommandChain();
                return;
            }

            var targetCommand = _commandsInQueue[_currentCommandIndex];
            var code = (EventEnum) Enum.ToObject(typeof(EventEnum), targetCommand.code);
            _eventCode = (int) code;

            var nowIndent = _commandsInQueue[_currentCommandIndex].indent;

            //バトルで、実行しないイベントがある
            if (!EventCodeList.CheckEventCodeExecute(_eventCode, EventCodeList.EventType.Battle, true))
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                ProcessCommand(_commandEventID);
#else
                await ProcessCommandAsync(_commandEventID);
#endif
                return;
            }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            if (FlowControl(code, _eventBattleData.eventId) == false)
#else
            if (await FlowControl(code, _eventBattleData.eventId) == false)
#endif
            {
                // 選択肢
                if (code == EventEnum.EVENT_CODE_MESSAGE_INPUT_SELECT)
                {
                    _commandProcessors[code]
                        .Invoke(this, _commandEventID, targetCommand, ProcessCommand, _commandsInQueue);
                }
                // 選択肢関係
                else if (code == EventEnum.EVENT_CODE_MESSAGE_INPUT_SELECT_CANCELED ||
                         code == EventEnum.EVENT_CODE_MESSAGE_INPUT_SELECT_END ||
                         code == EventEnum.EVENT_CODE_MESSAGE_INPUT_SELECT_SELECTED)
                {
                    // 選択済であれば終了地点まで飛ばす
                    for (var i = _currentCommandIndex; i < _commandsInQueue.Count; i++)
                        if (_commandsInQueue[i].code == (int) EventEnum.EVENT_CODE_MESSAGE_INPUT_SELECT_END &&
                            _commandsInQueue[i].indent == nowIndent)
                        {
                            _currentCommandIndex = i;
                            break;
                        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    ProcessCommand(_commandEventID);
#else
                    await ProcessCommandAsync(_commandEventID);
#endif
                }
                // 一旦ショップのアイテムリストは飛ばす
                else if (code == EventEnum.EVENT_CODE_SCENE_SET_SHOP_CONFIG_LINE ||
                        //U382 MP orTP　CHANGE系は処理を次へ
                        (code == EventEnum.EVENT_CODE_BATTLE_CHANGE_MP || 
                        code == EventEnum.EVENT_CODE_BATTLE_CHANGE_TP)
                        )
               {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        ProcessCommand(_commandEventID);
#else
                    await ProcessCommandAsync(_commandEventID);
#endif
                }

                else
                {
                    try
                    {
                        _commandProcessors[code].Invoke(this, _commandEventID, targetCommand, ProcessCommand);
                    }
                    catch (Exception)
                    {
                        //イベントコマンドチェーンの終了
                        EndDelayCommandChain();
                    }
                }
            }
        }

        /// <summary>
        ///     イベント実行終了
        /// </summary>
        private async void EndDelayCommandChain() {
            //ここで少し待ち、イベント終了契機がキーなどの入力だった場合に、後続のイベント等が発動しないようにする
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            await Task.Delay(100);
#else
            await UniteTask.Delay(100);
#endif
            _callback?.Invoke(null, null);
            //ここで少し待ち、イベント終了直後に別のイベントが発動することを抑制する
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            await Task.Delay(100);
#else
            await UniteTask.Delay(100);
#endif
            //状態の更新
            //すでにバトル終了しているケースがあるため、現在の状態を確認して、設定しなおす
            if (GameStateHandler.IsBattle())
            {
                GameStateHandler.SetGameState(GameStateHandler.GameState.BATTLE);
            }
            else if (GameStateHandler.IsMap())
            {
                if (MapEventExecutionController.Instance.IsPauseEvent())
                {
                    GameStateHandler.SetGameState(GameStateHandler.IsMap() ? GameStateHandler.GameState.EVENT : GameStateHandler.GameState.BATTLE_EVENT);
                }
                else
                {
                    GameStateHandler.SetGameState(GameStateHandler.GameState.MAP);
                }
            }
            _running = false;
        }

        /// <summary>
        ///     GameTroopの登録
        /// </summary>
        /// <param name="gameTroop">GameTroop</param>
        public void SetTroop(GameTroop gameTroop) {
            GameTroop = gameTroop;
        }
    }
}