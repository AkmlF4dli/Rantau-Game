using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime;
using RPGMaker.Codebase.Runtime.Battle.Wrapper;
using RPGMaker.Codebase.Runtime.Common;
using RPGMaker.Codebase.Runtime.Common.Component.Hud.Character;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Battle.Objects
{
    /// <summary>
    /// 戦闘シーンでのアイコンやアニメーションを含む、バトラーの動作を制御する
    /// </summary>
    public class GameBattler : GameBattlerBase
    {
        /// <summary>
        /// アクションの状態定義
        /// </summary>
        public enum ActionStateEnum
        {
            Undecided,      //行動未決定
            Inputting,      //入力中
            Waiting,        //待ち状態
            Acting,         //行動中
            Done,
            Null
        }
        public string Id { get; set; }
        /// <summary>
        /// 行動の配列
        /// </summary>
        public List<GameAction> Actions { get; set; }
        /// <summary>
        /// 速度(行動順を決定する)
        /// </summary>
        public float Speed { get; set; }
        GameActionResult _result;
        /// <summary>
        /// 行動結果
        /// </summary>
        public GameActionResult Result {
            get
            {
                return _result;
            }
            set
            {
                _result = value;
            }
        }
        /// <summary>
        /// アクション状態
        /// </summary>
        private ActionStateEnum _actionState;
        /// <summary>
        /// 最後の対象番号（直接指定用）
        /// </summary>
        public virtual int LastTargetIndex { get; set; }
        /// <summary>
        /// アニメーションの配列
        /// MVと異なり、パーティクルを再生可能なアニメーションの型としている
        /// </summary>
        private List<CharacterAnimationActor> _animations = new List<CharacterAnimationActor>();
        /// <summary>
        /// ダメージポップアップするか
        /// </summary>
        private bool _damagePopup;
        /// <summary>
        /// エフェクトタイプ
        /// </summary>
        private string _effectType;
        /// <summary>
        /// モーションタイプ
        /// </summary>
        private string _motionType;
        /// <summary>
        /// 武器画像ID
        /// </summary>
        private string _weaponImageId;
        /// <summary>
        /// モーションを更新するか
        /// </summary>
        private bool _motionRefresh;
        /// <summary>
        /// 選択されているか
        /// </summary>
        private bool _selected;
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public GameBattler() {
            this.InitMembers();
        }

        /// <summary>
        /// メンバ変数を初期化
        /// </summary>
        override public void InitMembers() {
            base.InitMembers();
            Actions = new List<GameAction>();
            Speed = 0;
            Result = new GameActionResult();
            _actionState = ActionStateEnum.Null;
            LastTargetIndex = 0;
            _animations = new List<CharacterAnimationActor>();
            _damagePopup = false;
            _effectType = null;
            _motionType = null;
            _weaponImageId = null;
            _motionRefresh = false;
            _selected = false;
        }

        /// <summary>
        /// アニメーションを消去
        /// </summary>
        public void ClearAnimations() {
            _animations.Clear();
        }

        /// <summary>
        /// ダメージポップアップを消去
        /// </summary>
        public void ClearDamagePopup() {
            _damagePopup = false;
        }

        /// <summary>
        /// 武器アニメーションを消去
        /// </summary>
        public void ClearWeaponAnimation() {
            _weaponImageId = null;
        }

        /// <summary>
        /// エフェクトを消去
        /// </summary>
        public void ClearEffect() {
            _effectType = null;
        }

        /// <summary>
        /// モーションを消去
        /// </summary>
        public void ClearMotion() {
            _motionType = null;
            _motionRefresh = false;
        }

        /// <summary>
        /// 指定エフェクトを要求
        /// </summary>
        /// <param name="effectType"></param>
        public void RequestEffect(string effectType) {
            _effectType = effectType;
        }

        /// <summary>
        /// 指定モーションを要求
        /// </summary>
        /// <param name="motionType"></param>
        public void RequestMotion(string motionType) {
            _motionType = motionType;
        }

        /// <summary>
        /// モーションの初期化を要求
        /// </summary>
        public void RequestMotionRefresh() {
            _motionRefresh = true;
        }

        /// <summary>
        /// バトラーの選択
        /// </summary>
        public void Select() {
            _selected = true;
        }

        /// <summary>
        /// 選択を外す
        /// </summary>
        public void Deselect() {
            _selected = false;
        }

        /// <summary>
        /// アニメーションが要求されているか
        /// </summary>
        /// <returns></returns>
        public bool IsAnimationRequested() {
            return _animations.Count > 0;
        }

        /// <summary>
        /// ダメージポップアップが要求されているか
        /// </summary>
        /// <returns></returns>
        public bool IsDamagePopupRequested() {
            return _damagePopup;
        }

        /// <summary>
        /// エフェクトが要求されているか
        /// </summary>
        /// <returns></returns>
        public bool IsEffectRequested() {
            return !string.IsNullOrEmpty(_effectType);
        }

        /// <summary>
        /// モーションが要求されているか
        /// </summary>
        /// <returns></returns>
        public bool IsMotionRequested() {
            return !string.IsNullOrEmpty(_motionType);
        }

        /// <summary>
        /// 武器アニメーションが要求されているか
        /// </summary>
        /// <returns></returns>
        public bool IsWeaponAnimationRequested() {
            return !string.IsNullOrEmpty(_weaponImageId);
        }

        /// <summary>
        /// モーションの初期化が要求されているか
        /// </summary>
        /// <returns></returns>
        public bool IsMotionRefreshRequested() {
            return _motionRefresh;
        }

        /// <summary>
        /// 選択されているか
        /// </summary>
        /// <returns></returns>
        public bool IsSelected() {
            return _selected;
        }

        /// <summary>
        /// エフェクトタイプを返す
        /// </summary>
        /// <returns></returns>
        public string EffectType() {
            return _effectType;
        }

        /// <summary>
        /// 行動タイプを返す
        /// </summary>
        /// <returns></returns>
        public string MotionType() {
            return _motionType;
        }

        /// <summary>
        /// 武器画像IDを返す
        /// </summary>
        /// <returns></returns>
        public string WeaponImageId() {
            return _weaponImageId;
        }

        /// <summary>
        /// 次のアニメーションを返す
        /// </summary>
        /// <returns></returns>
        public CharacterAnimationActor ShiftAnimation() {
            var ret = _animations.First();
            _animations.Remove(ret);
            return ret;
        }

        /// <summary>
        /// 指定アニメーション開始(追加)
        /// </summary>
        /// <param name="animationId"></param>
        /// <param name="mirror"></param>
        /// <param name="delay"></param>
        public virtual void StartAnimation(string animationId, bool mirror, float delay) {
            var data = new CharacterAnimationActor
            {
                animationId = animationId,
                mirror = mirror,
                delay = delay
            };
            _animations.Add(data);
        }

        /// <summary>
        /// ダメージポップアップ開始
        /// </summary>
        public virtual void StartDamagePopup() {
            _damagePopup = true;
        }

        /// <summary>
        /// 指定武器のアニメーション開始
        /// </summary>
        /// <param name="weaponImageId"></param>
        public virtual void StartWeaponAnimation(string weaponImageId) {
            _weaponImageId = weaponImageId;
        }

        /// <summary>
        /// 指定番号のアクションを返す
        /// </summary>
        /// <param name="index">バトラー番号</param>
        /// <returns></returns>
        public GameAction Action(int index) {
            return Actions[index];
        }

        /// <summary>
        /// 指定番号のバトラーにアクションを設定
        /// </summary>
        /// <param name="index">バトラー番号</param>
        /// <param name="action">アクション</param>
        public void SetAction(int index, GameAction action) {
            Actions[index] = action;
        }

        /// <summary>
        /// 行動番号を返す
        /// </summary>
        /// <returns></returns>
        public int NumActions() {
            return Actions.Count;
        }

        /// <summary>
        /// アクションを消去
        /// </summary>
        public virtual void ClearActions() {
            Actions.Clear();
        }

        /// <summary>
        /// 結果を初期化する
        /// </summary>
        public void ClearResult() {
            Result = new GameActionResult();
        }

        /// <summary>
        /// 能力値やステートを規定値内に収める処理
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public override void Refresh() {
            base.Refresh();
            if (Hp <= 0)
                AddState(DeathStateId);
            else
                RemoveState(DeathStateId);
        }
#else
        public override async Task Refresh() {
            await base.Refresh();
            if (Hp <= 0)
                await AddState(DeathStateId);
            else
                await RemoveState(DeathStateId);
        }
#endif

        /// <summary>
        /// 指定ステートを追加
        /// </summary>
        /// <param name="stateId"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public virtual bool AddState(string stateId) {
#else
        public virtual async Task<bool> AddState(string stateId) {
#endif
            if (IsStateAddable(stateId))
            {
                if (!IsStateAffected(stateId))
                {
                    var state = DataManager.Self().GetStateDataModel(stateId);

                    if (state.stateOn == 1)
                        AddNewMapState(stateId);
                    else
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        AddNewState(stateId);
#else
                        await AddNewState(stateId);
#endif

                    // バトル以外の特徴設定
                    var actorDataModels = DataManager.Self().GetRuntimeSaveDataModel()?.runtimeActorDataModels;
                    for (int i = 0; i < actorDataModels?.Count; i++)
                        if (Id == actorDataModels[i].actorId)
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                            ItemManager.SetTraits(state.traits, actorDataModels[i]);
#else
                            await ItemManager.SetTraits(state.traits, actorDataModels[i]);
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    Refresh();
#else
                    await Refresh();
#endif
                }

                ResetStateCounts(stateId);
                Result.PushAddedState(stateId);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 指定マップ用ステートを追加
        /// </summary>
        /// <param name="stateId"></param>
        private void AddNewMapState(string stateId) {
            // 付与ステートデータ作成
            RuntimeActorDataModel.State item = new RuntimeActorDataModel.State();
            item.id = DataManager.Self().GetStateDataModel(stateId).id;
            item.walkingCount = 0;

            var actorDataModels = DataManager.Self().GetRuntimeSaveDataModel().runtimeActorDataModels;
            for (int i = 0; i < actorDataModels.Count; i++)
            {
                bool isFind = false;
                if (Id == actorDataModels[i].actorId)
                {
                    isFind = true;

                    // 追加済みか
                    bool isAdded = false;
                    for (int i2 = 0; i2 < actorDataModels[i].states.Count; i2++)
                        if (actorDataModels[i].states[i2].id == item.id)
                        {
                            isAdded = true;
                            break;
                        }

                    if (!isAdded)
                    {
                        actorDataModels[i].states.Add(item);
                        States.Add(DataManager.Self().GetStateDataModel(stateId));
                    }
                }

                if (isFind) break;
            }
        }

        /// <summary>
        /// 指定ステートが付加可能か
        /// </summary>
        /// <param name="stateId"></param>
        /// <returns></returns>
        public bool IsStateAddable(string stateId) {
            return IsAlive() && DataManager.Self().GetStateDataModel(stateId) != null &&
                    !IsStateResist(ConvertUniteData.StateUuidToSerialNo(stateId)) &&
                    !Result.IsStateRemoved(stateId) &&
                    !IsStateRestrict(stateId) &&
                    IsStateTiming(stateId);
        }

        /// <summary>
        /// 指定ステートが[行動制約によって解除]かつ、現在行動制約中か
        /// </summary>
        /// <param name="stateId"></param>
        /// <returns></returns>
        public bool IsStateRestrict(string stateId) {
            return (DataManager.Self().GetStateDataModel(stateId).stateOn == 2 &&
                    DataManager.Self().GetStateDataModel(stateId).removeByRestriction == 1 ||
                    DataManager.Self().GetStateDataModel(stateId).stateOn == 0 &&
                    DataManager.Self().GetStateDataModel(stateId).inBattleRemoveRestriction == 1) &&
                   IsRestricted();
        }

        /// <summary>
        /// 指定ステートが付与可能なタイミングか（バトル、マップ、常時）
        /// マップステートはバトル中も付与可能とする
        /// </summary>
        /// <param name="stateId"></param>
        /// <returns></returns>
        public bool IsStateTiming(string stateId) {
            return DataManager.Self().GetStateDataModel(stateId).stateOn == 0 &&GameStateHandler.IsBattle()||
                   DataManager.Self().GetStateDataModel(stateId).stateOn == 1 ||
                   DataManager.Self().GetStateDataModel(stateId).stateOn == 2;
        }

        /// <summary>
        /// 行動制約された時に呼ばれるハンドラ
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public override void OnRestrict() {
#else
        public override async Task OnRestrict() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            base.OnRestrict();
#else
            await base.OnRestrict();
#endif
            ClearActions();
            for (int i = 0; i < States.Count; i++)
            {
                if (States[i].stateOn == 2 && States[i].removeByRestriction == 1 ||
                    States[i].stateOn == 0 && States[i].inBattleRemoveRestriction == 1)
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    RemoveState(States[i].id);
#else
                    await RemoveState(States[i].id);
#endif
                    i--;
                }
            }
        }

        /// <summary>
        /// 指定ステートを解除
        /// </summary>
        /// <param name="stateId"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public virtual bool RemoveState(string stateId) {
#else
        public virtual async Task<bool> RemoveState(string stateId) {
#endif
            if (IsStateAffected(stateId))
            {
                if (stateId == DeathStateId) Revive();

                EraseState(stateId);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                Refresh();
#else
                await Refresh();
#endif
                Result.PushRemovedState(stateId);

                var actorDataModels = DataManager.Self().GetRuntimeSaveDataModel()?.runtimeActorDataModels;
                for (int i = 0; i < actorDataModels?.Count; i++)
                    if (Id == actorDataModels[i].actorId)
                    {
                        var state = DataManager.Self().GetStateDataModel(stateId);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        ItemManager.SetTraits(state.traits, actorDataModels[i], remove : true);
#else
                        await ItemManager.SetTraits(state.traits, actorDataModels[i], remove : true);
#endif
                        actorDataModels[i].states.RemoveAll(s => s.id == stateId);
                    }

                return true;
            }

            return false;
        }

        /// <summary>
        /// 戦闘から逃げる
        /// </summary>
        public void Escape() {
            if (DataManager.Self().GetGameParty().InBattle())
            {
                IsEscaped = true;
                Hide();
            }

            ClearActions();

            SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_SE,
                DataManager.Self().GetSystemDataModel().soundSetting.escape);
            SoundManager.Self().PlaySe();
        }

        /// <summary>
        /// 指定通常能力に指定ターン数の[強化]を追加
        /// </summary>
        /// <param name="paramId"></param>
        /// <param name="turns"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void AddBuff(int paramId, int turns) {
#else
        public async Task AddBuff(int paramId, int turns) {
#endif
            if (IsAlive())
            {
                IncreaseBuff(paramId);
                if (IsBuffAffected(paramId)) OverwriteBuffTurns(paramId, turns);

                Result.PushAddedBuff(paramId);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                Refresh();
#else
                await Refresh();
#endif
            }
        }

        /// <summary>
        /// 指定通常能力に指定ターン数の[弱体]を追加
        /// </summary>
        /// <param name="paramId"></param>
        /// <param name="turns"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void AddDeBuff(int paramId, int turns) {
#else
        public async Task AddDeBuff(int paramId, int turns) {
#endif
            if (IsAlive())
            {
                DecreaseBuff(paramId);
                if (IsDebuffAffected(paramId)) OverwriteBuffTurns(paramId, turns);

                Result.PushAddedDebuff(paramId);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                Refresh();
#else
                await Refresh();
#endif
            }
        }

        /// <summary>
        /// 指定通常能力の[強化]を解除
        /// </summary>
        /// <param name="paramId"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void RemoveBuff(int paramId) {
#else
        public async Task RemoveBuff(int paramId) {
#endif
            if (IsAlive() && IsBuffOrDebuffAffected(paramId))
            {
                EraseBuff(paramId);
                Result.PushRemovedBuff(paramId);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                Refresh();
#else
                await Refresh();
#endif
            }
        }

        /// <summary>
        /// ステートを解除
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void RemoveBattleStates() {
#else
        public async Task RemoveBattleStates() {
#endif
            var toRemoveIds = new List<string>();
            States.ForEach(state =>
            {
                //ステートがバトル中のみ有効、またはバトル終了時に解除かどうか
                if (state.stateOn == 0 || state.removeAtBattleEnd == 1) toRemoveIds.Add(state.id);
            });

            for (int i = 0; i < toRemoveIds.Count; i++)
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                RemoveState(toRemoveIds[i]);
#else
                await RemoveState(toRemoveIds[i]);
#endif
            }
        }

        /// <summary>
        /// 全能力の[強化]を解除
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void RemoveAllBuffs() {
            for (var i = 0; i < BuffLength(); i++) RemoveBuff(i);
        }
#else
        public async Task RemoveAllBuffs() {
            for (var i = 0; i < BuffLength(); i++) await RemoveBuff(i);
        }
#endif

        /// <summary>
        /// 状態異常を自動解除する
        /// </summary>
        /// <param name="timing"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void RemoveStatesAuto(int timing) {
#else
        public async Task RemoveStatesAuto(int timing) {
#endif
            var toRemoveIds = new List<string>();
            States.ForEach(state =>
            {
                if (IsStateExpired(state.id) && state.removeAtBattling == 1 && state.autoRemovalTiming == timing) toRemoveIds.Add(state.id);
            });

            for (int i = 0; i < toRemoveIds.Count; i++)
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                RemoveState(toRemoveIds[i]);
#else
                await RemoveState(toRemoveIds[i]);
#endif
            }
        }

        /// <summary>
        /// ターン終了した能力[強化][弱体]を解除
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void RemoveBuffsAuto() {
#else
        public async Task RemoveBuffsAuto() {
#endif
            for (var i = 0; i < BuffLength(); i++)
                if (IsBuffExpired(i))
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    RemoveBuff(i);
#else
                    await RemoveBuff(i);
#endif
        }

        /// <summary>
        /// [ダメージで解除]のステートを解除
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void RemoveStatesByDamage() {
#else
        public async Task RemoveStatesByDamage() {
#endif
            var toRemoveIds = new List<string>();
            States.ForEach(state =>
            {
                if (state.stateOn == 2 && state.removeByDamage == 1 && TforuUtility.MathRandom() * 100 < state.removeProbability)
                    toRemoveIds.Add(state.id);
                else if (state.stateOn == 0 && state.inBattleRemoveDamage == 1 && TforuUtility.MathRandom() * 100 < state.inBattleRemoveProbability)
                    toRemoveIds.Add(state.id);
            });

            for (int i = 0; i < toRemoveIds.Count; i++)
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                RemoveState(toRemoveIds[i]);
#else
                await RemoveState(toRemoveIds[i]);
#endif
            }
        }

        /// <summary>
        /// 行動回数を設定して返す
        /// </summary>
        /// <returns></returns>
        public int MakeActionTimes() {
            return ActionPlusSet().Aggregate(1, (r, p) => { return TforuUtility.MathRandom() < p ? r + 1 : r; });
        }

        /// <summary>
        /// アニメーションを生成
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public virtual void MakeActions() {
#else
        public virtual async Task MakeActions() {
#endif
            ClearActions();
            if (CanMove())
            {
                var actionTimes = MakeActionTimes();
                Actions = new List<GameAction>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                for (var i = 0; i < actionTimes; i++) Actions.Add(new GameAction(this));
#else
                for (var i = 0; i < actionTimes; i++)
                {
                    var action = new GameAction(this);
                    await action.InitForConstructor(this);
                    Actions.Add(action);
                }
#endif
            }
        }

        /// <summary>
        /// 速度(行動順を決定する)を設定
        /// </summary>
        public void MakeSpeed() {
            Speed = Actions.ElementAtOrDefault(0)?.Speed() ?? 0;
        }

        /// <summary>
        /// 現在のアクションを返す
        /// </summary>
        /// <returns></returns>
        public GameAction CurrentAction() {
            return Actions.Any() ? Actions[0] : null;
        }

        /// <summary>
        /// 現在の行動を解除
        /// </summary>
        public void RemoveCurrentAction() {
            Actions.RemoveAt(0);
        }

        /// <summary>
        /// 目標バトラーを設定
        /// </summary>
        /// <param name="target"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SetLastTarget(GameBattler target) {
#else
        public async Task SetLastTarget(GameBattler target) {
#endif
            if (target != null)
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                LastTargetIndex = target.Index();
#else
                LastTargetIndex = await target.Index();
#endif
            else
                LastTargetIndex = 0;
        }

        /// <summary>
        /// 指定したスキルを強制する
        /// </summary>
        /// <param name="skillId"></param>
        /// <param name="targetIndex"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ForceAction(string skillId, int targetIndex) {
#else
        public async Task ForceAction(string skillId, int targetIndex) {
#endif
            ClearActions();
            var action = new GameAction(this, true);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            await action.InitForConstructor(this, true);
#endif
            action.SetSkill(skillId);

            if (targetIndex == -2)
                action.SetTarget(LastTargetIndex);
            else if (targetIndex == -1)
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                action.DecideRandomTarget();
#else
                await action.DecideRandomTarget();
#endif
            else
                action.SetTarget(targetIndex);

            Actions.Add(action);
        }

        /// <summary>
        /// 指定アイテムを使用
        /// </summary>
        /// <param name="item"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void UseItem(GameItem item) {
#else
        public async Task UseItem(GameItem item) {
#endif
            if (item.IsSkill())
                PaySkillCost(item);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            else if (item.IsItem()) ConsumeItem(item);
#else
            else if (item.IsItem()) await ConsumeItem(item);
#endif
        }

        /// <summary>
        /// 指定アイテムを消費
        /// </summary>
        /// <param name="item"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ConsumeItem(GameItem item) {
            DataManager.Self().GetGameParty().ConsumeItem(item);
        }
#else
        public async Task ConsumeItem(GameItem item) {
            await DataManager.Self().GetGameParty().ConsumeItem(item);
        }
#endif

        /// <summary>
        /// 指定量のHPを回復
        /// </summary>
        /// <param name="value"></param>
        public void GainHp(int value) {
            if (Result == null) ClearResult();
            Result.HpDamage = -value;
            Result.HpAffected = true;
            SetHp(Hp + value);
        }

        /// <summary>
        /// 指定量のMPを回復
        /// </summary>
        /// <param name="value"></param>
        public void GainMp(int value) {
            Result.MpDamage = -value;
            SetMp(Mp + value);
        }

        /// <summary>
        /// 指定量のTPを回復
        /// </summary>
        /// <param name="value"></param>
        public void GainTp(int value) {
            Result.TpDamage = -value;
            SetTp(Tp + value);
        }

        /// <summary>
        /// 指定量のTPを非表示で回復
        /// </summary>
        /// <param name="value"></param>
        public void GainSilentTp(int value) {
            SetTp(Tp + value);
        }

        /// <summary>
        /// TPの量を25までのランダムな値に初期化
        /// </summary>
        public void InitTp() {
            SetTp(new System.Random().Next(0, 25));
        }

        /// <summary>
        /// TPを0に
        /// </summary>
        public void ClearTp() {
            SetTp(0);
        }

        /// <summary>
        /// ダメージ率にしたがって、TPを増やす
        /// </summary>
        /// <param name="damageRate"></param>
        public void ChargeTpByDamage(double damageRate) {
            var value = (int) Math.Floor(50 * damageRate * Tcr);
            GainSilentTp(value);
        }

        /// <summary>
        /// 自動回復・ダメージを適用
        /// </summary>
        public void RegenerateHp() {
            var value = (int) Math.Floor(Mhp * Hrg);
            value = Math.Max(value, -MaxSlipDamage());
            if (value != 0) GainHp(value);
        }

        /// <summary>
        /// 速度(行動順を決定する)を設定
        /// </summary>
        /// <returns></returns>
        public int MaxSlipDamage() {
            return DataManager.Self().GetSystemDataModel().optionSetting.optSlipDeath == 1 ? Hp : Math.Max(Hp - 1, 0);
        }

        /// <summary>
        /// MP自動回復を適用
        /// </summary>
        public void RegenerateMp() {
            var value = (int) Math.Floor(Mmp * Mrg);
            if (value != 0) GainMp(value);
        }

        /// <summary>
        /// TP自動回復を適用
        /// </summary>
        public void RegenerateTp() {
            var value = (int) Math.Floor(100 * Trg);
            GainSilentTp(value);
        }

        /// <summary>
        /// 自動回復・ダメージを適用
        /// </summary>
        public void RegenerateAll() {
            if (IsAlive())
            {
                RegenerateHp();
                RegenerateMp();
                RegenerateTp();
            }
        }

        /// <summary>
        /// 戦闘開始ハンドラ
        /// </summary>
        public void OnBattleStart() {
            SetActionState(ActionStateEnum.Undecided);
            ClearMotion();
            if (!IsPreserveTp()) InitTp();
        }

        /// <summary>
        /// 全行動終了ハンドラ
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void OnAllActionsEnd() {
            ClearResult();
            RemoveStatesAuto(1);
            RemoveBuffsAuto();
        }
#else
        public async Task OnAllActionsEnd() {
            ClearResult();
            await RemoveStatesAuto(1);
            await RemoveBuffsAuto();
        }
#endif

        /// <summary>
        /// ターン終了ハンドラ
        /// </summary>
        /// <param name="isForcedTurn"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void OnTurnEnd(bool isForcedTurn = false) {
#else
        public async Task OnTurnEnd(bool isForcedTurn = false) {
#endif
            ClearResult();
            RegenerateAll();
            if (!isForcedTurn)
            {
                UpdateStateTurns();
                UpdateBuffTurns();
            }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            RemoveStatesAuto(2);
#else
            await RemoveStatesAuto(2);
#endif
        }

        /// <summary>
        /// 戦闘終了ハンドラ
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void OnBattleEnd() {
#else
        public async Task OnBattleEnd() {
#endif
            ClearResult();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            RemoveBattleStates();
            RemoveAllBuffs();
#else
            await RemoveBattleStates();
            await RemoveAllBuffs();
#endif
            ClearActions();
            if (!IsPreserveTp()) ClearTp();

            Appear();
        }

        /// <summary>
        /// 被ダメージハンドラ
        /// </summary>
        /// <param name="value"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void OnDamage(int value) {
#else
        public async Task OnDamage(int value) {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            RemoveStatesByDamage();
#else
            await RemoveStatesByDamage();
#endif
            ChargeTpByDamage(1.0 * value / Mhp);

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            Refresh();
#else
            await Refresh();
#endif
        }

        /// <summary>
        /// 指定アクション状態を設定
        /// </summary>
        /// <param name="actionState"></param>
        public void SetActionState(ActionStateEnum actionState) {
            _actionState = actionState;
            RequestMotionRefresh();
        }

        /// <summary>
        /// 行動が未選択か
        /// </summary>
        /// <returns></returns>
        public bool IsUndecided() {
            return _actionState == ActionStateEnum.Undecided;
        }

        /// <summary>
        /// 戦闘コマンド入力中か
        /// </summary>
        /// <returns></returns>
        public bool IsInputting() {
            return _actionState == ActionStateEnum.Inputting;
        }

        /// <summary>
        /// 待機中か
        /// </summary>
        /// <returns></returns>
        public bool IsWaiting() {
            return _actionState == ActionStateEnum.Waiting;
        }

        /// <summary>
        /// アクション実行中か
        /// </summary>
        /// <returns></returns>
        public bool IsActing() {
            return _actionState == ActionStateEnum.Acting;
        }

        /// <summary>
        /// 魔法詠唱中か
        /// </summary>
        /// <returns></returns>
        public bool IsChanting() {
            if (IsWaiting()) return Actions.Any(action => action?.IsMagicSkill() ?? false);

            return false;
        }

        /// <summary>
        /// [防御]して待機中か
        /// </summary>
        /// <returns></returns>
        public bool IsGuardWaiting() {
            return Actions.Any(action => action?.IsGuard() ?? false);
        }

        /// <summary>
        /// 指定アクションの開始動作を実行
        /// </summary>
        /// <param name="action"></param>
        public virtual void PerformActionStart(GameAction action) {
            if (!action.IsGuard()) SetActionState(ActionStateEnum.Acting);
        }

        /// <summary>
        /// 指定アクションを実行
        /// </summary>
        /// <param name="action"></param>
        public virtual void PerformAction(GameAction action) {
        }

        /// <summary>
        /// 行動終了を実行
        /// </summary>
        public virtual void PerformActionEnd() {
            SetActionState(ActionStateEnum.Done);
        }

        /// <summary>
        /// 被ダメージ動作を実行
        /// </summary>
        public virtual void PerformDamage() {
        }

        /// <summary>
        /// 失敗動作を実行
        /// </summary>
        public virtual void PerformMiss() {
            SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_SE,
                DataManager.Self().GetSystemDataModel().soundSetting.miss);
            SoundManager.Self().PlaySe();
        }

        /// <summary>
        /// 回復動作を実行
        /// </summary>
        public virtual void PerformRecovery() {
            SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_SE,
                DataManager.Self().GetSystemDataModel().soundSetting.recovery);
            SoundManager.Self().PlaySe();
        }

        /// <summary>
        /// 回避動作を実行
        /// </summary>
        public virtual void PerformEvasion() {
            SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_SE,
                DataManager.Self().GetSystemDataModel().soundSetting.evasion);
            SoundManager.Self().PlaySe();
        }

        /// <summary>
        /// 魔法回避動作を実行
        /// </summary>
        public virtual void PerformMagicEvasion() {
            SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_SE,
                DataManager.Self().GetSystemDataModel().soundSetting.magicEvasion);
            SoundManager.Self().PlaySe();
        }

        /// <summary>
        /// カウンター動作を実行
        /// </summary>
        public virtual void PerformCounter() {
            SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_SE,
                DataManager.Self().GetSystemDataModel().soundSetting.evasion);
            SoundManager.Self().PlaySe();
        }

        /// <summary>
        /// 魔法反射動作を実行
        /// </summary>
        public virtual void PerformReflection() {
            SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_SE,
                DataManager.Self().GetSystemDataModel().soundSetting.magicReflection);
            SoundManager.Self().PlaySe();
        }

        /// <summary>
        /// 身代わり動作を実行
        /// </summary>
        /// <param name="target"></param>
        public virtual void PerformSubstitute(GameBattler target) {
        }

        /// <summary>
        /// 倒れる動作を実行
        /// </summary>
        public virtual void PerformCollapse() {
        }

        //================================================================================
        // 以下はUniteで追加されたメソッド
        //================================================================================

        /// <summary>
        /// アクションの実行者の名前を返却
        /// </summary>
        /// <returns></returns>
        public string GetName() {
            //以下については、本来は try catch の必要が無いが、
            //不具合調査時に埋め込むログに、名前を出力する際に、通常では呼ばれないタイミングで、本メソッドを
            //実行してしまうことがある
            //その場合でもエラーにならないように、未だ初期化が行われていない場合には、空文字列を返却するようにしている
            if (this is GameActor actor)
            {
                try
                {
                    return actor.Name;
                } catch (Exception) { }
            }

            if (this is GameEnemy enemy)
            {
                try
                {
                    return enemy.Name();
                }
                catch (Exception) { }
            }
            return "";
        }
        
        /// <summary>
        ///制御文字が含まれていても読み込まない用
        /// </summary>
        /// <returns></returns>
        public string GetNameNoColChar() {
            if (this is GameActor actor)
            {
                try
                {
                    var name = actor.Name;
                    name = name.Replace("\\", "\\\\");

                    return name;
                } catch (Exception) { }
            }

            if (this is GameEnemy enemy)
            {
                try
                {
                    return enemy.Name();
                }
                catch (Exception) { }
            }
            return "";
        }


        /// <summary>
        /// アクターかどうか
        /// </summary>
        /// <returns></returns>
        public override bool IsActor() {
            return this is GameActor;
        }

        /// <summary>
        /// 敵かどうか
        /// </summary>
        /// <returns></returns>
        public override bool IsEnemy() {
            return this is GameEnemy;
        }

        /// <summary>
        /// 自分から見て味方キャラクターを返却
        /// </summary>
        /// <returns></returns>
        public virtual GameUnit FriendsUnit() {
            switch (this)
            {
                case GameActor _:
                    return DataManager.Self().GetGameParty();
                case GameEnemy _:
                    return DataManager.Self().GetGameTroop();
                default:
                    throw new Exception();
            }
        }

        /// <summary>
        /// 自分から見て敵キャラクターを返却
        /// </summary>
        /// <returns></returns>
        public virtual GameUnit OpponentsUnit() {
            switch (this)
            {
                case GameActor _:
                    return DataManager.Self().GetGameTroop();
                case GameEnemy _:
                    return DataManager.Self().GetGameParty();
                default:
                    throw new Exception();
            }
        }

        /// <summary>
        /// バトルメンバーかどうか
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public virtual bool IsBattleMember() {
#else
        public virtual async Task<bool> IsBattleMember() {
            await UniteTask.Delay(0);
#endif
            return true;
        }

        /// <summary>
        /// Index
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public virtual int Index() {
#else
        public virtual async Task<int> Index() {
            await UniteTask.Delay(0);
#endif
            return -1;
        }

        /// <summary>
        /// Sprite表示中かどうか
        /// </summary>
        /// <returns></returns>
        public virtual bool IsSpriteVisible() {
            return true;
        }
    }
}