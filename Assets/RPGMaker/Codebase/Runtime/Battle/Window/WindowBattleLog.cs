using RPGMaker.Codebase.Runtime.Battle.Objects;
using RPGMaker.Codebase.Runtime.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace RPGMaker.Codebase.Runtime.Battle.Window
{
    /// <summary>
    /// 戦闘ログのウィンドウ
    /// このウィンドウのメソッドの多くは、push() によって _methods プロパティに保存され順次実行される
    /// メッセージの表示だけではなく、サイドビューのアクションなども処理する、マネージャ的な役割も持っている
    /// </summary>
    public class WindowBattleLog : WindowSelectable
    {
        /// <summary>
        /// 行の配列
        /// </summary>
        private List<string> _lines;
        /// <summary>
        /// メソッドの配列
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private List<Action> _methods;
#else
        private List<Func<Task>> _methods;
#endif
        /// <summary>
        /// 待ち時間
        /// </summary>
        private int _waitCount;
        /// <summary>
        /// 待機状態
        /// </summary>
        private string _waitMode;
        /// <summary>
        /// 区切り行数のスタック
        /// </summary>
        private List<int> _baseLineStack;
        /// <summary>
        /// 通常攻撃
        /// </summary>
        private static string AnimationNormalAttack = "111419c2-2152-4b18-901d-4465c6771d4f";
        /// <summary>
        /// 文字を表示する場所（TextMeshProUGUI）
        /// 特殊文字（Ⅲなど）が含まれていると、文字の描画領域（高さ）に変動が出てしまうため、
        /// そのような場合でも表示場所が変わらないように、1行ずつ部品を用意している
        /// 最大で4行分の表示
        /// </summary>
        [SerializeField] protected List<TextMeshProUGUI> textField;

        /// <summary>
        /// 初期化処理
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        override public void Initialize() {
#else
        override public async Task Initialize() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            base.Initialize();
#else
            await base.Initialize();
#endif
            Opacity = 0;
            _lines = new List<string>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _methods = new List<Action>();
#else
            _methods = new List<Func<Task>>();
#endif
            _waitCount = 0;
            _waitMode = "";
            _baseLineStack = new List<int>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            Refresh();
#else
            await Refresh();
#endif
        }

        /// <summary>
        /// メッセージ描画速度
        /// </summary>
        /// <returns></returns>
        public int MessageSpeed() {
            return 64;
        }

        /// <summary>
        /// 動作中か
        /// </summary>
        /// <returns></returns>
        public bool IsBusy() {
            return _waitCount > 0 || _waitMode != "" || _methods.Count > 0;
        }

        /// <summary>
        /// Update処理
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public new void UpdateTimeHandler() {
            if (!UpdateWait()) CallNextMethod();
        }
#else
        public new async Task UpdateTimeHandler() {
            if (!UpdateWait()) await CallNextMethod();
        }
#endif

        /// <summary>
        /// 待ち時間のアップデート
        /// </summary>
        /// <returns></returns>
        public bool UpdateWait() {
            return UpdateWaitCount() || UpdateWaitMode();
        }

        /// <summary>
        /// 待ちカウントのアップデート
        /// </summary>
        /// <returns></returns>
        public bool UpdateWaitCount() {
            if (_waitCount > 0)
            {
                _waitCount -= IsFastForward() ? 3 : 1;
                if (_waitCount < 0) _waitCount = 0;

                if (IsShowMessage())
                {
                    return false;
                }

                //メッセージが出きっており、待ち状態でも無い場合は次に進める
                return true;
            }

            //待ち状態
            return false;
        }

        /// <summary>
        /// 待ち状態のアップデート
        /// </summary>
        /// <returns></returns>
        public bool UpdateWaitMode() {
            var waiting = false;
            switch (_waitMode)
            {
                case "effect":
                    waiting = BattleManager.GetSpriteSet().IsEffecting();
                    break;
                case "movement":
                    waiting = BattleManager.GetSpriteSet().IsAnyoneMoving();
                    break;
                case "waittime":
                    waiting = _waitCount > 0;
                    break;
            }

            if (!waiting) _waitMode = "";
            return waiting;
        }

        /// <summary>
        /// 待機状態を設定
        /// </summary>
        /// <param name="waitMode"></param>
        public void SetWaitMode(string waitMode) {
            _waitMode = waitMode;
        }

        /// <summary>
        /// 次のメソッドを呼ぶ
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void CallNextMethod() {
#else
        public async Task CallNextMethod() {
#endif
            if (_methods != null && _methods.Count > 0)
            {
                var method = _methods[0];
                _methods.RemoveAt(0);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                method.Invoke();
#else
                await method.Invoke();
#endif
            }
        }

        /// <summary>
        /// 早送りか
        /// </summary>
        /// <returns></returns>
        public bool IsFastForward() {
            if (InputHandler.OnPress(Common.Enum.HandleType.Decide) ||
                InputHandler.OnPress(Common.Enum.HandleType.LeftShiftDown))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// ログの挙動を予約する。
        /// 引数の内容は MV.BattleLogMethod と同じ
        /// </summary>
        /// <param name="action"></param>
        public void Push(Action action) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _methods.Add(action);
#else
            Func<Task> func = async () =>
            {
                await UniteTask.Delay(0);
                action.Invoke();
            };
            Push(func);
        }
        public void Push(Func<Task> action) {
            _methods.Add(action); 
#endif
        }

        /// <summary>
        /// 表示を消去。区切り行数の記録も消去
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void Clear() {
#else
        public async Task Clear() {
#endif
            _lines = new List<string>();
            _baseLineStack = new List<int>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            Refresh();
#else
            await Refresh();
#endif
        }

        /// <summary>
        /// 待つ
        /// </summary>
        public void Wait(int time = -1) {
            if (time == -1)
                _waitCount = MessageSpeed();
            else
                _waitCount = time;
        }

        /// <summary>
        /// エフェクトを待つ
        /// </summary>
        public void WaitForEffect() {
            SetWaitMode("effect");
        }

        /// <summary>
        /// 動作を待つ
        /// </summary>
        public void WaitForMovement() {
            SetWaitMode("movement");
        }

        /// <summary>
        /// 待ち時間完了まで待つ
        /// </summary>
        public void WaitFotWaitTime() {
            SetWaitMode("waittime");
        }

        /// <summary>
        /// 行を追加
        /// </summary>
        /// <param name="text"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void AddText(string text) {
#else
        public async Task AddText(string text) {
#endif
            _lines.Add(text);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            Refresh();
#else
            await Refresh();
#endif
            Wait();
        }

        /// <summary>
        /// 区切り行数を記録
        /// </summary>
        public void PushBaseLine() {
            _baseLineStack.Add(_lines.Count);
        }

        /// <summary>
        /// 記録した区切り行数に戻る
        /// </summary>
        public void PopBaseLine() {
            var baseLine = _baseLineStack[_baseLineStack.Count - 1];
            _baseLineStack.RemoveAt(_baseLineStack.Count - 1);
            while (_lines.Count > baseLine) _lines.RemoveAt(_lines.Count - 1);
        }

        /// <summary>
        /// 新たな行を待つ
        /// </summary>
        public void WaitForNewLine() {
            var baseLine = 0;
            if (_baseLineStack.Count > 0) 
                baseLine = _baseLineStack[_baseLineStack.Count - 1];
            if (_lines.Count > baseLine) 
                Wait();
        }

        /// <summary>
        /// ダメージを表示
        /// </summary>
        /// <param name="target"></param>
        public void PopupDamage(GameBattler target) {
            target.StartDamagePopup();
        }

        /// <summary>
        /// 行動の開始を適用
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="action"></param>
        public void PerformActionStart(GameBattler subject, GameAction action) {
            subject.PerformActionStart(action);
        }

        /// <summary>
        /// 行動を適用
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="action"></param>
        public void PerformAction(GameBattler subject, GameAction action) {
            subject.PerformAction(action);
        }

        /// <summary>
        /// 行動の終了を適用
        /// </summary>
        /// <param name="subject"></param>
        public void PerformActionEnd(GameBattler subject) {
            subject.PerformActionEnd();
        }

        /// <summary>
        /// ダメージを適用
        /// </summary>
        /// <param name="target"></param>
        public void PerformDamage(GameBattler target) {
            target.PerformDamage();
        }

        /// <summary>
        /// 攻撃失敗を適用
        /// </summary>
        /// <param name="target"></param>
        public void PerformMiss(GameBattler target) {
            target.PerformMiss();
        }

        /// <summary>
        /// 回復を適用
        /// </summary>
        /// <param name="target"></param>
        public void PerformRecovery(GameBattler target) {
            target.PerformRecovery();
        }

        /// <summary>
        /// 回避を適用
        /// </summary>
        /// <param name="target"></param>
        public void PerformEvasion(GameBattler target) {
            target.PerformEvasion();
        }

        /// <summary>
        /// 魔法反射を適用
        /// </summary>
        /// <param name="target"></param>
        public void PerformMagicEvasion(GameBattler target) {
            target.PerformMagicEvasion();
        }

        /// <summary>
        /// カウンター攻撃を適用
        /// </summary>
        /// <param name="target"></param>
        public void PerformCounter(GameBattler target) {
            target.PerformCounter();
        }

        /// <summary>
        /// 反射を適用
        /// </summary>
        /// <param name="target"></param>
        public void PerformReflection(GameBattler target) {
            target.PerformReflection();
        }

        /// <summary>
        /// [かばう]行動を適用
        /// </summary>
        /// <param name="substitute"></param>
        /// <param name="target"></param>
        public void PerformSubstitute(GameBattler substitute, GameBattler target) {
            substitute.PerformSubstitute(target);
        }

        /// <summary>
        /// 倒れたことを適用
        /// </summary>
        /// <param name="target"></param>
        public void PerformCollapse(GameBattler target) {
            target.PerformCollapse();
        }

        /// <summary>
        /// アニメを表示
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="targets"></param>
        /// <param name="animationId"></param>
        public void ShowAnimation(GameBattler subject, List<GameBattler> targets, GameAction action) {
            if (action.Item.AnimationId == AnimationNormalAttack && subject.IsActor())
            {
                //通常攻撃、かつアクターである場合は武器アニメーションを行う
                ShowNormalAnimation(targets, ((GameActor) subject).AttackAnimationId1());
            }
            else
            {
                //指定されてるアニメーションを行う
                ShowNormalAnimation(targets, action.Item.AnimationId);
            }
        }

        /// <summary>
        /// 攻撃アニメを表示
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="targets"></param>
        public void ShowAttackAnimation(GameBattler subject, List<GameBattler> targets) {
            if (subject.IsActor())
                ShowActorAttackAnimation((GameActor) subject, targets);
            else
                ShowEnemyAttackAnimation((GameEnemy) subject, targets);
        }

        /// <summary>
        /// アクターの攻撃アニメを表示
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="targets"></param>
        public void ShowActorAttackAnimation(GameActor subject, List<GameBattler> targets) {
            ShowNormalAnimation(targets, subject.AttackAnimationId1());
            ShowNormalAnimation(targets, subject.AttackAnimationId2(), true);
        }

        /// <summary>
        /// 敵の攻撃アニメを表示
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="targets"></param>
        public void ShowEnemyAttackAnimation(GameEnemy subject, List<GameBattler> targets) {
            SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_SE, DataManager.Self().GetSystemDataModel().soundSetting.enemyAttack);
            SoundManager.Self().PlaySe();
        }

        /// <summary>
        /// 通常アニメーション開始
        /// </summary>
        /// <param name="targets"></param>
        /// <param name="animationId"></param>
        /// <param name="mirror"></param>
        public void ShowNormalAnimation(List<GameBattler> targets, string animationId, bool mirror = false) {
            var animation = DataManager.Self().GetAnimationDataModel(animationId);
            if (animation != null)
            {
                var delay = AnimationBaseDelay();
                var nextDelay = AnimationNextDelay();
                targets.ForEach(target =>
                {
                    target.StartAnimation(animationId, mirror, delay);
                    delay += nextDelay;
                });
            }
        }

        /// <summary>
        /// アニメーション再生開始までの遅延を返す
        /// </summary>
        private int AnimationBaseDelay() {
            return 1 * 60;
        }

        /// <summary>
        /// 次のアニメーションまでの遅延時間
        /// 対象が複数だった場合などで、アニメーションを全員に表示する際の待ち時間
        /// </summary>
        /// <returns></returns>
        private int AnimationNextDelay() {
            return Mathf.RoundToInt(0.2f * 60);
        }

        /// <summary>
        /// コンテンツの再描画
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public override void Refresh() {
#else
        public override async Task Refresh() {
            await UniteTask.Delay(0);
#endif
            //初期化処理
            for (int i = 0; i < textField.Count; i++)
                textField[i].text = "";
            var text = "";

            //次に表示する文字列を詰める
            for (var i = 0; i < _lines.Count; i++)
            {
                text += _lines[i] + "\n";
                DrawLineText(i);
            }
        }

        /// <summary>
        /// ターン開始
        /// </summary>
        public void StartTurn() {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _methods.Add(() => { Wait(); });
#else
            Push(() => { Wait(); });
#endif
        }

        /// <summary>
        /// 行動の開始
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="action"></param>
        /// <param name="targets"></param>
        public void StartAction(GameBattler subject, GameAction action, List<GameBattler> targets, GameBattler targetMyself) {
            var item = action.Item;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _methods.Add(() => { PerformActionStart(subject, action); });
            _methods.Add(WaitForMovement);
            _methods.Add(() => { PerformAction(subject, action); });
            _methods.Add(() => { ShowAnimation(subject, targets, action); });
#else
            Push(() => { PerformActionStart(subject, action); });
            Push(WaitForMovement);
            Push(() => { PerformAction(subject, action); });
            Push(() => { ShowAnimation(subject, targets, action); });
#endif
            if (targetMyself != null)
            {
                //使用者ターゲット指定時のアニメーションを設定
                List<GameBattler> MyTargets = new List<GameBattler>{ targetMyself };
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _methods.Add(() => { ShowAnimation(subject, MyTargets, action); });
#else
                Push(() => { ShowAnimation(subject, MyTargets, action); });
#endif
            }
            //バトルログ
            DisplayAction(subject, item);
        }

        /// <summary>
        /// アクションの終了
        /// </summary>
        /// <param name="subject"></param>
        public void EndAction(GameBattler subject) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _methods.Add(WaitForNewLine);
            _methods.Add(Clear);
            _methods.Add(() => { PerformActionEnd(subject); });
#else
            Push(WaitForNewLine);
            Push(Clear);
            Push(() => { PerformActionEnd(subject); });
#endif
        }

        /// <summary>
        /// 現在のステートを表示
        /// </summary>
        /// <param name="subject"></param>
        public void DisplayCurrentState(GameBattler subject) {
            var stateText = subject.MostImportantStateText();
            if (stateText != "")
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _methods.Add(() => {
#else
                Push(async () => {
#endif
                    string[] work = { subject.GetName() };
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    AddText(TextManager.Format(stateText, work)); 
#else
                    await AddText(TextManager.Format(stateText, work)); 
#endif
                });
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _methods.Add(() => { Wait(MessageSpeed() * 2); });
                _methods.Add(WaitFotWaitTime);
                _methods.Add(Clear);
#else
                Push(() => { Wait(MessageSpeed() * 2); });
                Push(WaitFotWaitTime);
                Push(Clear);
#endif
            }
        }

        /// <summary>
        /// 再生を表示
        /// </summary>
        /// <param name="subject"></param>
        public void DisplayRegeneration(GameBattler subject) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _methods.Add(() => { PopupDamage(subject); });
#else
            Push(() => { PopupDamage(subject); });
#endif
        }

        /// <summary>
        /// 指定された行動( [スキル][アイテム]の使用 )を表示
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="item"></param>
        public void DisplayAction(GameBattler subject, GameItem item) {
            var numMethods = _methods.Count;
            if (item.IsSkill())
            {
                if (item.Message1 != "")
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _methods.Add(() => { AddText(TextManager.Format(item.Message1, subject.GetName(), item.Name)); });
#else
                    Push(async () => { await AddText(TextManager.Format(item.Message1, subject.GetName(), item.Name)); });
#endif
            }
            else
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _methods.Add(() => { AddText(TextManager.Format(TextManager.useItem, subject.GetName(), item.Name)); });
#else
                Push(async () => { await AddText(TextManager.Format(TextManager.useItem, subject.GetName(), item.Name)); });
#endif
            }

            if (_methods.Count == numMethods)
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _methods.Add(() => { Wait(); });
#else
                Push(() => { Wait(); });
#endif
            }
        }

        /// <summary>
        /// カウンター攻撃を表示
        /// </summary>
        /// <param name="target"></param>
        public void DisplayCounter(GameBattler target) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _methods.Add(() => { PerformCounter(target); });
            _methods.Add(() => { AddText(TextManager.Format(TextManager.counterAttack, target.GetName())); });
#else
            Push(() => { PerformCounter(target); });
            Push(async () => { await AddText(TextManager.Format(TextManager.counterAttack, target.GetName())); });
#endif
        }

        /// <summary>
        /// 反射を表示
        /// </summary>
        /// <param name="target"></param>
        public void DisplayReflection(GameBattler target) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _methods.Add(() => { PerformReflection(target); });
            _methods.Add(() => { AddText(TextManager.Format(TextManager.magicReflection, target.GetName())); });
#else
            Push(() => { PerformReflection(target); });
            Push(async () => { await AddText(TextManager.Format(TextManager.magicReflection, target.GetName())); });
#endif
        }

        /// <summary>
        /// [かばう]行動を表示
        /// </summary>
        /// <param name="substitute"></param>
        /// <param name="target"></param>
        public void DisplaySubstitute(GameBattler substitute, GameBattler target) {
            var substName = substitute.GetName();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _methods.Add(() => { PerformSubstitute(substitute, target); });
            _methods.Add(() => { AddText(TextManager.Format(TextManager.substitute, substName, target.GetName())); });
#else
            Push(() => { PerformSubstitute(substitute, target); });
            Push(async () => { await AddText(TextManager.Format(TextManager.substitute, substName, target.GetName())); });
#endif
        }

        /// <summary>
        /// 行動結果を表示
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="target"></param>
        public void DisplayActionResults(GameBattler subject, GameBattler target) {
            if (target.Result.Used)
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _methods.Add(() => { PushBaseLine(); });
#else
                Push(PushBaseLine);
#endif
                DisplayCritical(target);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _methods.Add(() => { PopupDamage(target); });
                _methods.Add(() => { PopupDamage(subject); });
#else
                Push(() => { PopupDamage(target); });
                Push(() => { PopupDamage(subject); });
#endif
                DisplayDamage(target);
                DisplayAffectedStatus(target);
                DisplayFailure(target);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _methods.Add(() => { WaitForNewLine(); });
                _methods.Add(() => { PopBaseLine(); });
#else
                Push(WaitForNewLine);
                Push(PopBaseLine);
#endif
            }
        }

        /// <summary>
        /// 行動失敗を表示
        /// </summary>
        /// <param name="target"></param>
        public void DisplayFailure(GameBattler target) {
            if (target.Result.IsHit() && !target.Result.Success)
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _methods.Add(() => { AddText(TextManager.Format(TextManager.actionFailure, target.GetName())); });
#else
                Push(async () => { await AddText(TextManager.Format(TextManager.actionFailure, target.GetName())); });
#endif
        }

        /// <summary>
        /// クリティカル攻撃を表示
        /// </summary>
        /// <param name="target"></param>
        public void DisplayCritical(GameBattler target) {
            if (target.Result.Critical)
            {
                if (target.IsActor())
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _methods.Add(() => { AddText(TextManager.criticalToActor); });
#else
                    Push(async () => { await AddText(TextManager.criticalToActor); });
#endif
                else
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _methods.Add(() => { AddText(TextManager.criticalToEnemy); });
#else
                    Push(async () => { await AddText(TextManager.criticalToEnemy); });
#endif
            }
        }

        /// <summary>
        /// ダメージ表示
        /// </summary>
        /// <param name="target"></param>
        public void DisplayDamage(GameBattler target) {
            if (target.Result.Missed)
            {
                DisplayMiss(target);
            }
            else if (target.Result.Evaded)
            {
                DisplayEvasion(target);
            }
            else
            {
                DisplayHpDamage(target);
                DisplayMpDamage(target);
                DisplayTpDamage(target);
            }
        }

        /// <summary>
        /// 攻撃の失敗を表示
        /// </summary>
        /// <param name="target"></param>
        public void DisplayMiss(GameBattler target) {
            var fmt = "";
            if (target.Result.Physical)
            {
                fmt = target.IsActor() ? TextManager.actorNoHit : TextManager.enemyNoHit;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _methods.Add(() => { PerformMiss(target); });
#else
                Push(() => { PerformMiss(target); });
#endif
            }
            else
            {
                fmt = TextManager.actionFailure;
            }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _methods.Add(() => { AddText(TextManager.Format(fmt, target.GetName())); });
#else
            Push(async () => { await AddText(TextManager.Format(fmt, target.GetName())); });
#endif
        }

        /// <summary>
        /// 回避を表示
        /// </summary>
        /// <param name="target"></param>
        public void DisplayEvasion(GameBattler target) {
            var fmt = TextManager.evasion;
            if (target.Result.Physical)
            {
                fmt = TextManager.evasion;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _methods.Add(() => { PerformEvasion(target); });
#else
                Push(() => { PerformEvasion(target); });
#endif
            }
            else
            {
                fmt = TextManager.magicEvasion;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _methods.Add(() => { PerformMagicEvasion(target); });
#else
                Push(() => { PerformMagicEvasion(target); });
#endif
            }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _methods.Add(() => { AddText(TextManager.Format(fmt, target.GetName())); });
#else
            Push(async () => { await AddText(TextManager.Format(fmt, target.GetName())); });
#endif
        }

        /// <summary>
        /// HPへのダメージを表示
        /// </summary>
        /// <param name="target"></param>
        public void DisplayHpDamage(GameBattler target) {
            if (target.Result.HpAffected)
            {
                if (target.Result.HpDamage > 0 && !target.Result.Drain)
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _methods.Add(() => { PerformDamage(target); });
#else
                    Push(() => { PerformDamage(target); });
#endif
                }
                if (target.Result.HpDamage < 0)
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _methods.Add(() => { PerformRecovery(target); });
#else
                    Push(() => { PerformRecovery(target); });
#endif
                }
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _methods.Add(() => { AddText(MakeHpDamageText(target)); });
#else
                Push(async () => { await AddText(MakeHpDamageText(target)); });
#endif
            }
        }

        /// <summary>
        /// MPへのダメージを表示
        /// </summary>
        /// <param name="target"></param>
        public void DisplayMpDamage(GameBattler target) {
            if (target.IsAlive() && target.Result.MpDamage != 0)
            {
                if (target.Result.MpDamage < 0) 
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _methods.Add(() => { PerformRecovery(target); });
                _methods.Add(() => { AddText(MakeMpDamageText(target)); });
#else
                    Push(() => { PerformRecovery(target); });
                Push(async () => { await AddText(MakeMpDamageText(target)); });
#endif
            }
        }

        /// <summary>
        /// TPへのダメージを表示
        /// </summary>
        /// <param name="target"></param>
        public void DisplayTpDamage(GameBattler target) {
            if (target.IsAlive() && target.Result.TpDamage != 0)
            {
                if (target.Result.TpDamage < 0) 
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _methods.Add(() => { PerformRecovery(target); });
                _methods.Add(() => { AddText(MakeTpDamageText(target)); });
#else
                    Push(() => { PerformRecovery(target); });
                Push(async () => { await AddText(MakeTpDamageText(target)); });
#endif
            }
        }

        /// <summary>
        /// 能力値変化を表示
        /// </summary>
        /// <param name="target"></param>
        public void DisplayAffectedStatus(GameBattler target) {
            if (target.Result.IsStatusAffected())
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _methods.Add(() => { PushBaseLine(); });
                DisplayChangedStates(target);
                DisplayChangedBuffs(target);
                _methods.Add(() => { WaitForNewLine(); });
                _methods.Add(() => { PopBaseLine(); });
#else
                Push(PushBaseLine);
                DisplayChangedStates(target);
                DisplayChangedBuffs(target);
                Push(WaitForNewLine);
                Push(PopBaseLine);
#endif
            }
        }

        /// <summary>
        /// 自動での能力値変化を表示
        /// </summary>
        /// <param name="target"></param>
        public void DisplayAutoAffectedStatus(GameBattler target) {
            if (target.Result.IsStatusAffected())
            {
                DisplayAffectedStatus(target);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _methods.Add(() => { Clear(); });
#else
                Push(Clear);
#endif
            }
        }

        /// <summary>
        /// ステートの変化を表示
        /// </summary>
        /// <param name="target"></param>
        public void DisplayChangedStates(GameBattler target) {
            DisplayAddedStates(target);
            DisplayRemovedStates(target);
        }

        /// <summary>
        /// 追加されたステートを表示
        /// </summary>
        /// <param name="target"></param>
        public void DisplayAddedStates(GameBattler target) {
            target.Result.AddedStateObjects().ForEach(state =>
            {
                var stateMsg = target.IsActor() ? state.message1 : state.message2;
                if (state.id == GameBattlerBase.DeathStateId) 
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _methods.Add(() => { PerformCollapse(target); });
#else
                    Push(() => { PerformCollapse(target); });
#endif
                if (stateMsg != "")
                {
                    string[] work = { target.GetName() };
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _methods.Add(() => { PopBaseLine(); });
                    _methods.Add(() => { PushBaseLine(); });
                    _methods.Add(() => { AddText(TextManager.Format(stateMsg, work)); });
                    _methods.Add(() => { WaitForEffect(); });
#else
                    Push(PopBaseLine);
                    Push(PushBaseLine);
                    Push(async () => { await AddText(TextManager.Format(stateMsg, work)); });
                    Push(WaitForEffect);
#endif
                }
            });
        }

        /// <summary>
        /// ステートが外れたことを表示
        /// </summary>
        /// <param name="target"></param>
        public void DisplayRemovedStates(GameBattler target) {
            if (target.IsAlive())
            {
                target.Result.RemovedStateObjects().ForEach(state =>
                {
                    if (state.message4 != "")
                    {
                        string[] work = { target.GetName() };
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        _methods.Add(() => { PopBaseLine(); });
                        _methods.Add(() => { PushBaseLine(); });
                        _methods.Add(() => { AddText(TextManager.Format(state.message4, work)); });
#else
                        Push(() => { PopBaseLine(); });
                        Push(() => { PushBaseLine(); });
                        Push(async () => { await AddText(TextManager.Format(state.message4, work)); });
#endif
                    }
                });
            }
        }

        /// <summary>
        /// [強化][弱体]の変化を表示。
        /// </summary>
        /// <param name="target"></param>
        public void DisplayChangedBuffs(GameBattler target) {
            var result = target.Result;
            DisplayBuffs(target, result.AddedBuffs, TextManager.buffAdd);
            DisplayBuffs(target, result.AddedDebuffs, TextManager.debuffAdd);
            DisplayBuffs(target, result.RemovedBuffs, TextManager.buffRemove);
        }

        /// <summary>
        /// バフ表示
        /// </summary>
        /// <param name="target"></param>
        /// <param name="buffs"></param>
        /// <param name="fmt"></param>
        public void DisplayBuffs(GameBattler target, List<int> buffs, string fmt) {
            buffs.ForEach(paramId =>
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _methods.Add(() => { PopBaseLine(); });
                _methods.Add(() => { PushBaseLine(); });
                _methods.Add(() =>
#else
                Push(() => { PopBaseLine(); });
                Push(() => { PushBaseLine(); });
                Push(async () =>
#endif
                {
                    //paramIdから用語を取得し、設定する
                    string paramName = paramId switch {
                        0 => DataManager.Self().GetWordDefinitionDataModel().status.maxHp.value,
                        1 => DataManager.Self().GetWordDefinitionDataModel().status.maxMp.value,
                        2 => DataManager.Self().GetWordDefinitionDataModel().status.attack.value,
                        3 => DataManager.Self().GetWordDefinitionDataModel().status.guard.value,
                        4 => DataManager.Self().GetWordDefinitionDataModel().status.magic.value,
                        5 => DataManager.Self().GetWordDefinitionDataModel().status.magicGuard.value,
                        6 => DataManager.Self().GetWordDefinitionDataModel().status.speed.value,
                        7 => DataManager.Self().GetWordDefinitionDataModel().status.luck.value,
                        8 => DataManager.Self().GetWordDefinitionDataModel().status.hit.value,
                        9 => DataManager.Self().GetWordDefinitionDataModel().status.evasion.value,
                        _ => ""
                    };
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    AddText(TextManager.Format(fmt, target.GetName(), paramName));
#else
                    await AddText(TextManager.Format(fmt, target.GetName(), paramName));
#endif
                });
            });
        }

        /// <summary>
        /// HPへのダメージのメッセージを生成
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public string MakeHpDamageText(GameBattler target) {
            var result = target.Result;
            var damage = result.HpDamage;
            var isActor = target.IsActor();
            var fmt = "";
            if (damage > 0 && result.Drain)
            {
                fmt = isActor ? TextManager.actorDrain : TextManager.enemyDrain;
                return TextManager.Format(fmt, target.GetName(), TextManager.hp, damage.ToString());
            }
            if (damage > 0)
            {
                fmt = isActor ? TextManager.actorDamage : TextManager.enemyDamage;
                return TextManager.Format(fmt, target.GetName(), damage.ToString());
            }
            if (damage < 0)
            {
                fmt = isActor ? TextManager.actorRecovery : TextManager.enemyRecovery;
                return TextManager.Format(fmt, target.GetName(), TextManager.hp, (-damage).ToString());
            }
            fmt = isActor ? TextManager.actorNoDamage : TextManager.enemyNoDamage;
            return TextManager.Format(fmt, target.GetName());
        }

        /// <summary>
        /// MPへのダメージのメッセージを生成
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public string MakeMpDamageText(GameBattler target) {
            var result = target.Result;
            var damage = result.MpDamage;
            var isActor = target.IsActor();
            var fmt = "";
            if (damage > 0 && result.Drain)
            {
                fmt = isActor ? TextManager.actorDrain : TextManager.enemyDrain;
                return TextManager.Format(fmt, target.GetName(), TextManager.mp, damage.ToString());
            }
            if (damage > 0)
            {
                fmt = isActor ? TextManager.actorLoss : TextManager.enemyLoss;
                return TextManager.Format(fmt, target.GetName(), TextManager.mp, damage.ToString());
            }
            if (damage < 0)
            {
                fmt = isActor ? TextManager.actorRecovery : TextManager.enemyRecovery;
                return TextManager.Format(fmt, target.GetName(), TextManager.mp, (-damage).ToString());
            }
            return "";
        }

        /// <summary>
        /// TPへのダメージのメッセージを生成
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public string MakeTpDamageText(GameBattler target) {
            var result = target.Result;
            var damage = result.TpDamage;
            var isActor = target.IsActor();
            var fmt = "";
            if (damage > 0)
            {
                fmt = isActor ? TextManager.actorLoss : TextManager.enemyLoss;
                return TextManager.Format(fmt, target.GetName(), TextManager.tp, damage.ToString());
            }
            if (damage < 0)
            {
                fmt = isActor ? TextManager.actorGain : TextManager.enemyGain;
                return TextManager.Format(fmt, target.GetName(), TextManager.tp, (-damage).ToString());
            }
            return "";
        }

        /// <summary>
        /// 指定行の文字を描画
        /// </summary>
        /// <param name="index"></param>
        public void DrawLineText(int index) {
            textField[index].text += _lines[index];
        }

        //==================================================================================
        // 以下、Uniteで追加した処理
        //==================================================================================

        /// <summary>
        /// メッセージ表示中かどうか
        /// </summary>
        /// <returns></returns>
        public bool IsShowMessage() {
            if (_lines != null && _lines.Count > 0) return true;
            return false;
        }

        /// <summary>
        /// メッセージWindowを開く
        /// </summary>
        public override void Open() {
            base.Open();
            gameObject.SetActive(true);
        }

        /// <summary>
        /// メッセージWindowを閉じる
        /// </summary>
        public override void Close() {
            base.Close();
            gameObject.SetActive(false);
        }
    }
}