using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.EventMap;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Map;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime;
using RPGMaker.Codebase.CoreSystem.Knowledge.Enum;
using RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement;
using RPGMaker.Codebase.CoreSystem.Service.EventManagement;
using RPGMaker.Codebase.Runtime.Common;
using RPGMaker.Codebase.Runtime.Common.Component.Hud.Character;
using RPGMaker.Codebase.Runtime.Map.Component.Map;
using RPGMaker.Codebase.Runtime.Scene.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.EventMap.EventMapDataModel.EventMapPageConditionImage;
using static RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime.RuntimeOnMapDataModel;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Map.Component.Character
{
    public class EventOnMap : CharacterOnMap
    {
        /// <summary>
        /// MapDataModel
        /// </summary>
        private       MapDataModel     _mapDataModel;
        /// <summary>
        /// 移動イベント
        /// </summary>
        private       MoveSetMovePoint _moveEvent;
        /// <summary>
        /// タイル上でのイベント位置
        /// </summary>
        private TilesOnThePosition _tilesOnThePosition;
        /// <summary>
        /// 現在移動中かどうかのフラグ
        /// </summary>
        private bool isMove;
        /// <summary>
        /// 現在イベントがマップ上で有効かどうかのフラグ
        /// </summary>
        public bool isValid;
        /// <summary>
        /// イベントのChainLauncher
        /// </summary>
        private EventCommandChainLauncher _eventCommandChainLauncher;
        /// <summary>
        /// データベース
        /// </summary>
        private DatabaseManagementService _databaseManagementService;
        /// <summary>
        /// イベントが並列処理実行中かどうか
        /// </summary>
        public bool IsParallel { get; private set; }
        /// <summary>
        /// イベント内で実行中のコモンイベント
        /// </summary>
        private CommonEventManager _commonEventManager;
        /// <summary>
        /// イベントページ
        /// </summary>
        public int page;
        /// <summary>
        /// マップ移動によって、前のマップから引き継いだイベントを、イベント終了後に二度と起動しないようにするためのフラグ
        /// </summary>
        private bool _canExecute = true;

        // データプロパティ
        //--------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// このイベントのEventMapDataModel
        /// </summary>
        public EventMapDataModel MapDataModelEvent { get; private set; }
        /// <summary>
        /// このイベントのEventDataModel
        /// </summary>
        public EventDataModel EventDataModel { get; private set; }

        // initialize methods
        //--------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 初期化処理
        /// </summary>
        /// <param name="positionOnTile">タイル上の座標位置</param>
        /// <param name="mapDataModelEvent">EventMapDataModel</param>
        /// <param name="evt">EventDataModel</param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void Init(Vector2 positionOnTile, EventMapDataModel mapDataModelEvent, EventDataModel eventDataModel) {
#else
        public async Task Init(Vector2 positionOnTile, EventMapDataModel mapDataModelEvent, EventDataModel eventDataModel) {
#endif
            _databaseManagementService = new DatabaseManagementService();
            MapDataModelEvent = mapDataModelEvent;
            EventDataModel = eventDataModel;

            //このイベントを無効状態で初期化
            isValid = false;

            //一時消去フラグのリセット
            mapDataModelEvent.temporaryErase = 0;

            // 画像設定
            if (mapDataModelEvent.pages[page].condition.image.enabled == (int) EnabledType.Character)
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var actor = _databaseManagementService.LoadCharacterActor();
#else
                var actor = await _databaseManagementService.LoadCharacterActor();
#endif
                for (int i = 0; i < actor.Count; i++)
                    if (actor[i].uuId == mapDataModelEvent.pages[page].condition.image.imageName)
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        base.Init(positionOnTile,
#else
                        await base.Init(positionOnTile,
#endif
                            actor[i].image.character,
                            "");
            }
            else if (mapDataModelEvent.pages[page].condition.image.enabled == (int) EnabledType.SelectedImage)
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                base.Init(positionOnTile, mapDataModelEvent.pages[page].image.sdName, "");
#else
                await base.Init(positionOnTile, mapDataModelEvent.pages[page].image.sdName, "");
#endif
            else if (mapDataModelEvent.pages[page].condition.image.enabled == (int) EnabledType.Tile)
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                base.Init(positionOnTile, mapDataModelEvent.pages[page].image.tileName, "");
#else
                await base.Init(positionOnTile, mapDataModelEvent.pages[page].image.tileName, "");
#endif
            else
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                base.Init(positionOnTile, "", "");
#else
                await base.Init(positionOnTile, "", "");
#endif

            _tilesOnThePosition = gameObject.AddComponent<TilesOnThePosition>();

            _moveEvent = gameObject.AddComponent<MoveSetMovePoint>();
            _moveEvent.SetData(EventDataModel, MapDataModelEvent, this);

            SetSortingLayer(isFlying: false);

            // アニメーション設定
            var isSteppingAnimation = !(MapDataModelEvent.pages[page].walk.stepping == 1);
            var isAnimation = !(MapDataModelEvent.pages[page].walk.walking == 1);
            SetAnimation(isAnimation, isSteppingAnimation);

            // 向き設定
            InitChangeDirection();

            // ※MapDataModelEvent.pages[page].image.direction　が未使用
            // 初期は下向き
            var direction = CharacterMoveDirectionEnum.Down;
            switch (MapDataModelEvent.pages[page].walk.direction)
            {
                case 0:
                case 1:
                    break;
                case 2:
                    direction = CharacterMoveDirectionEnum.Right;
                    break;
                case 3:
                    direction = CharacterMoveDirectionEnum.Left;
                    break;
                case 4:
                    direction = CharacterMoveDirectionEnum.Down;
                    break;
                case 5:
                    direction = CharacterMoveDirectionEnum.Up;
                    break;
                case 6:
                    direction = CharacterMoveDirectionEnum.Damage;
                    break;
            }

            ChangeCharacterDirection(direction);
            _isLockDirection = MapDataModelEvent.pages[page].walk.directionFix == 1;

            //このイベントの実行状態の初期化
            _eventCommandChainLauncher = new EventCommandChainLauncher(CheckValidForce);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            await _eventCommandChainLauncher.InitForConstructor();
#endif

            //配置直後は該当のマップに存在するためtrueで初期化
            _canExecute = true;

            //すり抜け初期状態設定
            _isThrough = (MapDataModelEvent.pages[page].walk.through == 0);

            //フレーム単位での処理
            TimeHandler.Instance.AddTimeActionEveryFrame(UpdateTimeHandler);

            //GameObject用の表示名設定 U274
            //            name = "EVENT_" + MapDataModelEvent.SerialNumber;
            name = "EVENT_" + MapDataModelEvent.eventId;
        }

        /// <summary>
        /// 旧 directionFix データの変換処理
        /// </summary>
        private void InitChangeDirection() {
            // 元々 directionFix に、移動方向、プレイヤー、右固定、左固定、下固定、上固定 というデータが入っている
            // 最新では directionFix には 固定かどうか しか設定しないため、2以上のデータの場合には、変換処理を行う
            if (MapDataModelEvent.pages[page].walk.directionFix >= 2)
            {
                // 指定されていた向きを direction に設定
                MapDataModelEvent.pages[page].walk.direction = MapDataModelEvent.pages[page].walk.directionFix;
                // 固定
                MapDataModelEvent.pages[page].walk.directionFix = 1;
            }
            // direction が -1 の場合は、初期化されていない = 古いデータとみなし、変換処理を行う
            if (MapDataModelEvent.pages[page].walk.direction == -1)
            {
                // 指定されていた向きを direction に設定
                MapDataModelEvent.pages[page].walk.direction = MapDataModelEvent.pages[page].walk.directionFix;
                // ここに来るケースは、向きが 移動方向又はプレイヤーの場合で、向きは固定にできないため、0を指定
                MapDataModelEvent.pages[page].walk.directionFix = 0;
            }
        }

        private void OnDestroy() {
            //フレーム単位での処理
            TimeHandler.Instance?.RemoveTimeAction(UpdateTimeHandler);
        }

        public bool CanEventExecute() {
            return _canExecute;
        }

        /// <summary>
        /// マップ移動後のため実行不可とする
        /// </summary>
        public void SetNotEventExecute() {
            _canExecute = false;
        }

        /**
        * 更新処理
        */
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public override void UpdateTimeHandler() {
            base.UpdateTimeHandler();
#else
        public override async Task UpdateTimeHandlerAsync() {
            await base.UpdateTimeHandlerAsync();
#endif

            // 発動判定のみ更新
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            bool ret = SetMovementParameter();
#else
            bool ret = await SetMovementParameter();
#endif
            if (isValid && ret)
            {
                //フレーム単位での処理
                TimeHandler.Instance.AddTimeActionEveryFrame(WaitStartMove);
            }
        }

        /// <summary>
        /// 向き更新処理
        /// </summary>
        public void LookToPlayerDirection() {
            // 向き固定ではない場合、プレイヤーを向く
            if (MapDataModelEvent.pages[page].walk.directionFix == 0)
            {
                var angleVector =
                    MapManager.OperatingCharacter.transform.localPosition - gameObject.transform.localPosition;
                if (angleVector.x == 0f && angleVector.y == 0f) return;

                var angle4 = ((int) (Mathf.Atan2(angleVector.x, angleVector.y) * Mathf.Rad2Deg) + 360 + 45) % 360 / 90;
                var direction = angle4 switch
                {
                    0 => CharacterMoveDirectionEnum.Up,
                    1 => CharacterMoveDirectionEnum.Right,
                    2 => CharacterMoveDirectionEnum.Down,
                    3 => CharacterMoveDirectionEnum.Left,
                    _ => throw new NotImplementedException()
                };

                CharacterMoveDirectionEnum tempDir = CharacterMoveDirectionEnum.None;
                if (transform.GetChild(0) != null)
                    tempDir = transform.GetChild(0).GetComponent<CharacterGraphic>().GetTempDirection();
                gameObject.GetComponent<CharacterOnMap>().ChangeCharacterDirection(direction);
                if (transform.GetChild(0) != null)
                    transform.GetChild(0)?.GetComponent<CharacterGraphic>().SetTempDirection(tempDir);
            }
        }

        /// <summary>
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SetUp(MapDataModel mapDataModel) {
#else
        public async Task SetUp(MapDataModel mapDataModel) {
#endif
            _mapDataModel = mapDataModel;

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            SetMovementParameter();
#else
            await SetMovementParameter();
#endif

            //フレーム単位での処理
            TimeHandler.Instance.AddTimeActionEveryFrame(WaitStartMove);
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void RestartMove() {
#else
        public async Task RestartMove() {
#endif
            var data = GetComponent<MoveSetMovePoint>();
            if (data != null)
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                data.RestartMove();
#else
                await data.RestartMove();
#endif
            }
        }

        private void WaitStartMove() {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            _ = WaitStartMoveAsync();
        }
        private async Task WaitStartMoveAsync() {
#endif
            if (isMove || MapManager.OperatingCharacter == null) return;

            TimeHandler.Instance.RemoveTimeAction(WaitStartMove);

            // イベント中であれば移動は行わず終了
            if (_moveEvent.IsEventCommandCall == true) return;

            isMove = true;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            StartMove();
#else
            await StartMove();
#endif

        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void StartMove() {
#else
        private async Task StartMove() {
#endif
            //U355  NULLチェックを強化(破棄した時に参照する危険性がある為)
            if (_moveEvent == null) return;

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _moveEvent.SetNpcMove(MapDataModelEvent.pages[page], MoveEnd, 
            () => {
                _moveEvent.UpdateMove();
#else
            await _moveEvent.SetNpcMove(MapDataModelEvent.pages[page], MoveEnd, 
            async () => {
                await _moveEvent.UpdateMove();
#endif
                isMove = false;
            }, 
            (CharacterMoveDirectionEnum direction) => {
                //接触イベントの実行判定を行う
                if (isValid && MapDataModelEvent.pages[page].eventTrigger == (int) EventTriggerEnum.ContactFromTheEvent)
                {
                    var characterOnMap = MapManager.OperatingCharacter.GetComponent<CharacterOnMap>();

                    //異なるプライオリティで、同一の座標に存在する場合
                    if (characterOnMap.IsSameCheck(x_now, y_now) && MapDataModelEvent.pages[page].priority != 1)
                    {
                        if (this.ExecuteEvent(MapEventExecutionController.Instance.EndTriggerEvent, false))
                        {
                            LookToPlayerDirection();
                        }
                    }
                    //同一のプライオリティ
                    else if (MapDataModelEvent.pages[page].priority == 1 && direction == CharacterMoveDirectionEnum.Max)
                    {
                        //全方位チェック
                        if (characterOnMap.IsAroundCheck(x_now, y_now) && MapDataModelEvent.pages[page].priority == 1)
                        {
                            if (this.ExecuteEvent(MapEventExecutionController.Instance.EndTriggerEvent, false))
                            {
                                LookToPlayerDirection();
                            }
                        }
                    }
                    else if (MapDataModelEvent.pages[page].priority == 1) 
                    {
                        Vector2 directionVec = direction switch
                        {
                            CharacterMoveDirectionEnum.Left => Vector2.left,
                            CharacterMoveDirectionEnum.Right => Vector2.right,
                            CharacterMoveDirectionEnum.Up => Vector2.up,
                            CharacterMoveDirectionEnum.Down => Vector2.down,
                            _ => throw new ArgumentOutOfRangeException()
                        };

                        //移動方向チェック
                        if (characterOnMap.GetDestinationPositionOnTile() == GetDestinationPositionOnTile() + directionVec)
                        {
                            if (this.ExecuteEvent(MapEventExecutionController.Instance.EndTriggerEvent, false))
                            {
                                LookToPlayerDirection();
                            }
                        }
                    }

                    //現在イベント実行中の場合は、MapEventExecutionController への登録も行う
                    if (_eventCommandChainLauncher.IsRunning())
                    {
                        MapEventExecutionController.Instance.SetExecuteEventsOnMap(this);
                        MapEventExecutionController.Instance.UpdateGameState();
                    }
                }
                isMove = false;
            },
            MapDataModelEvent.eventId, _mapDataModel);
            isMove = false;
        }

        /// <summary>
        ///     有効化を明示的に確認
        ///     （Updateの順番等で1F遅れる事があるため）
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public bool CheckValid(bool isForce = false) {
#else
        public async Task<bool> CheckValid(bool isForce = false) {
#endif
            try
            {
                //現在、自分自身が破棄済みであれば処理しない
                if (gameObject == null || transform == null) return false;
            } catch (Exception) { }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            bool ret = SetMovementParameter(isForce);
#else
            bool ret = await SetMovementParameter(isForce);
#endif
            if (isValid && ret)
            {
                //フレーム単位での処理
                TimeHandler.Instance.AddTimeActionEveryFrame(WaitStartMove);
            }
            return isValid;
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private bool SetMovementParameter(bool isForce = false) {
#else
        private async Task<bool> SetMovementParameter(bool isForce = false) {
#endif
            bool changePage = false;

            try
            {
                //現在、自分自身が破棄済みであれば処理しない
                if (gameObject == null || transform == null) return false;
                //現在、自分自身のイベントが実行中であれば、更新処理を行わない
                if (!isForce && _eventCommandChainLauncher.IsRunning()) return false;
                //現在、イベントが一時消去状態かつ、無効状態であれば、更新処理を行わない
                if (!isValid && MapDataModelEvent.temporaryErase == 1) return false;
            }
            catch (Exception)
            {
                //マップ移動時に、このオブジェクト自体が破棄された場合、タイミングによってはここに入ってくる可能性がある
                //nullチェック等での判定が出来ないため、ここで破棄済みの場合の処理を実施する
                return false;
            }

            var saveDataWork = DataManager.Self().GetRuntimeSaveDataModel();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var sw = _databaseManagementService.LoadFlags().switches;
            var vari = _databaseManagementService.LoadFlags().variables;
#else
            var sw = (await _databaseManagementService.LoadFlags()).switches;
            var vari = (await _databaseManagementService.LoadFlags()).variables;
#endif
            //U346 
            bool isValidOld = isValid;

            for (var i = MapDataModelEvent.pages.Count - 1; i >= 0; i--)
            {
                // スイッチ1の判定
                if (MapDataModelEvent.pages[i].condition.switchOne.enabled == 1)
                {
                    var num = 0;
                    for (var swCnt = 0; swCnt < sw.Count; swCnt++)
                        if (sw[swCnt].id == MapDataModelEvent.pages[i].condition.switchOne.switchId)
                        {
                            num = swCnt;
                            break;
                        }

                    if (saveDataWork.switches.data[num] == false)
                    {
                        isValid = false;
                        continue;
                    }
                }

                // スイッチ2の判定
                if (MapDataModelEvent.pages[i].condition.switchTwo.enabled == 1)
                {
                    var num = 0;
                    for (var swCnt = 0; swCnt < sw.Count; swCnt++)
                        if (sw[swCnt].id == MapDataModelEvent.pages[i].condition.switchTwo.switchId)
                        {
                            num = swCnt;
                            break;
                        }

                    if (saveDataWork.switches.data[num] == false)
                    {
                        isValid = false;
                        continue;
                    }
                }

                // 変数
                if (MapDataModelEvent.pages[i].condition.variables.enabled == 1)
                {
                    // 変数の値取得
                    int GetVariableValue(string id) {
                        for (var i2 = 0; i2 < vari.Count; i2++)
                            if (vari[i2].id == id)
                                return int.Parse(saveDataWork.variables.data[i2]);
                        return 0;
                    }

                    var baseValue = GetVariableValue(MapDataModelEvent.pages[i].condition.variables.variableId);
                    var compareValue = MapDataModelEvent.pages[i].condition.variables.value;

                    // 計算
                    if (baseValue >= compareValue)
                    {
                        // 一致時は何もしない
                    }
                    else
                    {
                        isValid = false;
                        continue;
                    }
                }

                // セルフスイッチ
                if (MapDataModelEvent.pages[i].condition.selfSwitch.enabled == 1)
                {
                    // データを検索
                    RuntimeSaveDataModel.SaveDataSelfSwitchesData swData = null;
                    for (int i2 = 0; i2 < saveDataWork.selfSwitches.Count; i2++)
                        if (saveDataWork.selfSwitches[i2].id == MapDataModelEvent.eventId)
                            swData = saveDataWork.selfSwitches[i2];

                    // データがなければ追加
                    if (swData == null)
                    {
                        saveDataWork.selfSwitches.Add(new RuntimeSaveDataModel.SaveDataSelfSwitchesData());
                        saveDataWork.selfSwitches[saveDataWork.selfSwitches.Count - 1].id = MapDataModelEvent.eventId;
                        saveDataWork.selfSwitches[saveDataWork.selfSwitches.Count - 1].data =
                            new List<bool> { false, false, false, false };
                        swData = saveDataWork.selfSwitches[saveDataWork.selfSwitches.Count - 1];
                    }

                    var num = -1;

                    // 空であればAとする
                    if (MapDataModelEvent.pages[i].condition.selfSwitch.selfSwitch == "")
                        MapDataModelEvent.pages[i].condition.selfSwitch.selfSwitch = "A";

                    switch (MapDataModelEvent.pages[i].condition.selfSwitch.selfSwitch)
                    {
                        case "A":
                            num = 0;
                            break;
                        case "B":
                            num = 1;
                            break;
                        case "C":
                            num = 2;
                            break;
                        case "D":
                            num = 3;
                            break;
                    }

                    if (num != -1 && swData.data[num])
                    {
                        // 一致時は何もしない
                    }
                    else
                    {
                        isValid = false;
                        continue;
                    }
                }

                if (MapDataModelEvent.pages[i].condition.item.enabled == 1)
                {
                    if (saveDataWork.HasItem(MapDataModelEvent.pages[i].condition.item.itemId))
                    {
                        // 一致時は何もしない
                    }
                    else
                    {
                        isValid = false;
                        continue;
                    }
                }

                if (MapDataModelEvent.pages[i].condition.actor.enabled == 1)
                {
                    if (saveDataWork.ActorInParty(MapDataModelEvent.pages[i].condition.actor.actorId))
                    {
                        // 一致時は何もしない
                    }
                    else
                    {
                        isValid = false;
                        continue;
                    }
                }

                if (MapDataModelEvent.pages[i].condition.switchItem.enabled == 1)
                {
                    if (saveDataWork.HasSwitchItem(MapDataModelEvent.pages[i].condition.switchItem.switchItemId))
                    {
                        // 一致時は何もしない
                    }
                    else
                    {
                        isValid = false;
                        continue;
                    }
                }

                // ページ番号が更新
                if (page != i)
                {
                    changePage = true;
                    page = i;

                    // 画像差し替え
                    switch (MapDataModelEvent.pages[page].condition.image.enabled)
                    {
                        case (int) EnabledType.Character:
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                            var actor = _databaseManagementService.LoadCharacterActor()
#else
                            var actor = (await _databaseManagementService.LoadCharacterActor())
#endif
                                .FirstOrDefault(actor => actor.uuId == MapDataModelEvent.pages[page].condition.image.imageName);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                            if (actor != null) ChangeAsset(actor.image.character);
#else
                            if (actor != null) await ChangeAsset(actor.image.character);
#endif
                            break;
                        case (int) EnabledType.SelectedImage:
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                            ChangeAsset(MapDataModelEvent.pages[page].image.sdName);
#else
                            await ChangeAsset(MapDataModelEvent.pages[page].image.sdName);
#endif
                            break;
                        case (int) EnabledType.Tile:
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                            ChangeAsset(MapDataModelEvent.pages[page].image.tileName);
#else
                            await ChangeAsset(MapDataModelEvent.pages[page].image.tileName);
#endif
                            break;
                        default:
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                            ChangeAsset(String.Empty);
#else
                            await ChangeAsset(String.Empty);
#endif
                            break;
                    }

                    // 向き差し替え
                    // 初期はなし
                    var direction = CharacterMoveDirectionEnum.None;
                    switch (MapDataModelEvent.pages[page].walk.direction)
                    {
                        case 0:
                        case 1:
                            break;
                        case 2:
                            direction = CharacterMoveDirectionEnum.Right;
                            break;
                        case 3:
                            direction = CharacterMoveDirectionEnum.Left;
                            break;
                        case 4:
                            direction = CharacterMoveDirectionEnum.Down;
                            break;
                        case 5:
                            direction = CharacterMoveDirectionEnum.Up;
                            break;
                        case 6:
                            direction = CharacterMoveDirectionEnum.Damage;
                            break;
                    }

                    //向きを変更する必要がある場合に、変更処理を行う
                    if (direction != CharacterMoveDirectionEnum.None)
                    {
                        _isLockDirection = false;
                        ChangeCharacterDirection(direction);
                    }

                    _isLockDirection = MapDataModelEvent.pages[page].walk.directionFix == 1;

                    // アニメーション設定
                    SetAnimation(!Convert.ToBoolean(MapDataModelEvent.pages[page].walk.walking),
                        !Convert.ToBoolean(MapDataModelEvent.pages[page].walk.stepping));

                    // すり抜けを再設定
                    _isThrough = (MapDataModelEvent.pages[page].walk.through == 0);

                    // イベント更新
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    EventDataModel = new EventManagementService().LoadEventById(EventDataModel.id, page);
#else
                    EventDataModel = await new EventManagementService().LoadEventById(EventDataModel.id, page);
#endif
                }

                //一時消去中はイベントが消える
                isValid = MapDataModelEvent.temporaryErase == 0;
                break;
            }

            //U346　先頭ページイベントのみ非表示から表示された場合に、疑似的にページ変更フラグを設定する
            if (changePage == false && isValidOld != isValid && page == 0)
            {
                if(isValid == true)
                {
                    changePage = true;
                }
            }

            if (transform.GetChild(0)?.GetComponent<CharacterGraphic>() != null)
                transform.GetChild(0).GetComponent<CharacterGraphic>().SetImageEnable(isValid);

            return changePage;
        }

        new public void MoveEnd() {
            try
            {
                _isMoving = false;

                //移動元を移動先にする
                x_now = x_next;
                y_now = y_next;
            }
            catch (Exception)
            {
            }
            isMove = false;
        }

        /// <summary>
        /// イベントキャラのプライオリティは通常？
        /// </summary>
        /// <returns>通常フラグ</returns>
        public bool IsPriorityNormal() {
            return GetPriority() == EventMapDataModel.EventMapPage.PriorityType.Normal;
        }

        /// <summary>
        ///     イベントキャラのプライオリティ
        /// </summary>
        /// <returns></returns>
        public override EventMapDataModel.EventMapPage.PriorityType GetPriority() {
            if (MapDataModelEvent.pages.Count == 0) return EventMapDataModel.EventMapPage.PriorityType.Normal;
            return (EventMapDataModel.EventMapPage.PriorityType)MapDataModelEvent.pages[page].priority;
        }

        /// <summary>
        ///     イベントキャラのすり抜け状態
        /// </summary>
        /// <returns></returns>
        public bool GetTrough() {
            return _isThrough;
        }

        /// <summary>
        /// イベント実行処理
        /// </summary>
        public bool ExecuteEvent(Action<EventMapDataModel, EventDataModel> callback, bool autoEvent) {
            if (!isValid) return false;
            return _eventCommandChainLauncher.LaunchCommandChain(MapDataModelEvent, EventDataModel, callback, autoEvent, ExecuteCommonEvent);
        }
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
        public bool ExecuteEvent(Func<EventMapDataModel, EventDataModel, Task> callback, bool autoEvent) {
            if (!isValid) return false;
            return _eventCommandChainLauncher.LaunchCommandChain(MapDataModelEvent, EventDataModel, (eventMapDataModel, eventDataModel) =>
            {
                _ = callback(eventMapDataModel, eventDataModel);
            }, autoEvent, ExecuteCommonEvent);
        }
#endif

        /// <summary>
        /// イベント実行処理
        /// </summary>
        public bool ExecuteCommonEvent(Action<EventMapDataModel, EventDataModel> callback, bool autoEvent) {
            return _eventCommandChainLauncher.LaunchCommandChain(MapDataModelEvent, EventDataModel, callback, autoEvent, ExecuteCommonEvent);
        }
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
        public bool ExecuteCommonEvent(Func<EventMapDataModel, EventDataModel, Task> callback, bool autoEvent) {
            return _eventCommandChainLauncher.LaunchCommandChain(MapDataModelEvent, EventDataModel, (eventMapDataModel, eventDataModel) =>
            {
                _ = callback(eventMapDataModel, eventDataModel);
            }, autoEvent, ExecuteCommonEvent);
        }
#endif

        /// <summary>
        /// イベントを実行中に、コモンイベントを実行する場合の処理
        /// </summary>
        /// <param name="param"></param>
        private void ExecuteCommonEvent(string param) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            _ = ExecuteCommonEventAsync(param);
        }
        private async Task ExecuteCommonEventAsync(string param) {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            if (_commonEventManager == null) _commonEventManager = new CommonEventManager();
            _commonEventManager.LaunchCommandChain(param);
#else
            if (_commonEventManager == null)
            {
                _commonEventManager = new CommonEventManager();
                await _commonEventManager.InitForConstructor();
            }
            await _commonEventManager.LaunchCommandChain(param);
#endif
        }

        /// <summary>
        /// ショップイベントの商品更新
        /// </summary>
        public void ShopEvent() {
            _eventCommandChainLauncher.ShopEvent();
        }

        /// <summary>
        /// ショップイベントのデータ取得
        /// </summary>
        /// <returns></returns>
        public List<EventDataModel.EventCommand> ShopItemList() {
            return _eventCommandChainLauncher.ShopItemList();
        }


        /// <summary>
        /// コモンイベントのアップデート処理
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void UpdateCommonEvents() {
#else
        public async Task UpdateCommonEvents() {
#endif
            // コモンイベント
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            if (_commonEventManager == null) _commonEventManager = new CommonEventManager();
#else
            if (_commonEventManager == null)
            {
                _commonEventManager = new CommonEventManager();
                await _commonEventManager.InitForConstructor();
            }
#endif
            _commonEventManager.UpdateCommonEvent();
        }

        /// <summary>
        /// マップから破棄される際に、各種データを破棄する
        /// </summary>
        public void DestroyEvent() {
            // 実行中イベントがあれば停止
            if (_eventCommandChainLauncher != null)
            {
                _eventCommandChainLauncher.ExitCommandChain();
                _eventCommandChainLauncher = null;
            }

            // コモンイベントの停止
            if (_commonEventManager != null)
            {
                _commonEventManager.DestroyEvent();
            }

            // 並列イベント処理中の状態を初期化
            IsParallel = false;
        }

        /// <summary>
        /// アニメーション設定
        /// </summary>
        /// <param name="animation"></param>
        /// <param name="step"></param>
        public override void SetAnimationSettings(bool animation, bool step) {
            base.SetAnimation(animation, step);

            if (_moveEvent == null) return;

            int animationWork = animation == true ? 0 : 1;
            int stepAnimation = step == true ? 0 : 1;

            _moveEvent.SetAnimationSettings(animationWork, stepAnimation);
        }

        public void InitializeCount() {
            _eventCommandChainLauncher.InitializeCount();
        }

        /// <summary>
        /// イベント終了時のタイミングで、イベントが有効かどうかのチェックを行う
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public bool CheckValidForce() {
#else
        public async Task<bool> CheckValidForce() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            CheckValid(true);
#else
            await CheckValid(true);
#endif
            if (!isValid)
            {
                _eventCommandChainLauncher.ExitCommandChain();
            }
            return isValid;
        }

        /// <summary>
        /// イベントを現在実行中かどうか
        /// </summary>
        /// <returns></returns>
        public bool IsEventRunning() {
            return _eventCommandChainLauncher.IsRunning();
        }

        /// <summary>
        /// 指定したタイルの位置を設定
        /// </summary>
        /// <param name="destinationPositionOnTile"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SetToPositionOnTileLoop(Vector2 destinationPositionOnTile) {
#else
        public async Task SetToPositionOnTileLoop(Vector2 destinationPositionOnTile) {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            base.SetToPositionOnTile(destinationPositionOnTile);
#else
            await base.SetToPositionOnTile(destinationPositionOnTile);
#endif
            if (!_eventCommandChainLauncher.IsRunning())
            {
                isMove = false;
                TimeHandler.Instance.AddTimeActionEveryFrame(WaitStartMove);
            }
        }

        /// <summary>
        /// セーブデータ用のイベントデータ返却
        /// </summary>
        /// <returns></returns>
        public RuntimeOnMapEventDataModel GetMapEventData() {
            RuntimeOnMapEventDataModel data = new RuntimeOnMapEventDataModel();

            data.page = page;
            data.isExecute = _eventCommandChainLauncher.IsRunning() ? 1 : 0;
            if (data.isExecute == 1)
            {
                data.runningType = MapEventExecutionController.Instance.GetEventType(this);
                data.index = _eventCommandChainLauncher.GetIndex();
            }

            return data;
        }

        /// <summary>
        /// コンティニュー用のイベントデータ設定
        /// </summary>
        /// <param name="data"></param>
        public void SetMapEventData(RuntimeOnMapEventDataModel data) {
            page = data.page;

            if (data.isExecute == 1)
            {
                if (data.runningType == 1)
                {
                    MapEventExecutionController.Instance.SetExecuteEventsOnMap(this);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _eventCommandChainLauncher.ResumeCommandChain(MapDataModelEvent, EventDataModel, MapEventExecutionController.Instance.EndTriggerEvent, false, data.index);
#else
                    _eventCommandChainLauncher.ResumeCommandChain(MapDataModelEvent, EventDataModel, (eventMapDataModel, eventDataModel) => { _ = MapEventExecutionController.Instance.EndTriggerEvent(eventMapDataModel, eventDataModel); }, false, data.index);
#endif
                }
                else if (data.runningType == 2)
                {
                    MapEventExecutionController.Instance.SetAutoExecuteEventsOnMap(this);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _eventCommandChainLauncher.ResumeCommandChain(MapDataModelEvent, EventDataModel, MapEventExecutionController.Instance.EndAutoEvent, true, data.index);
#else
                    _eventCommandChainLauncher.ResumeCommandChain(MapDataModelEvent, EventDataModel, (eventMapDataModel, eventDataModel) => { _ = MapEventExecutionController.Instance.EndAutoEvent(eventMapDataModel, eventDataModel); }, true, data.index);
#endif
                }
                else if (data.runningType == 3)
                {
                    MapEventExecutionController.Instance.SetParallelEventsOnMap(this);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _eventCommandChainLauncher.ResumeCommandChain(MapDataModelEvent, EventDataModel, MapEventExecutionController.Instance.EndParallelEvent, true, data.index);
#else
                    _eventCommandChainLauncher.ResumeCommandChain(MapDataModelEvent, EventDataModel, (eventMapDataModel, eventDataModel) => { _ = MapEventExecutionController.Instance.EndParallelEvent(eventMapDataModel, eventDataModel); }, true, data.index);
#endif
                }
            }
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SetMapMoveData(RuntimeOnMapMoveDataModel data) {
#else
        public async Task SetMapMoveData(RuntimeOnMapMoveDataModel data) {
#endif
            var move = gameObject.GetComponent<MoveSetMovePoint>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            move.SetMapMoveData(data, this, gameObject);
#else
            await move.SetMapMoveData(data, this, gameObject);
#endif
        }
    }
}