using RPGMaker.Codebase.Runtime.Battle.Objects;
using RPGMaker.Codebase.Runtime.Common;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Battle.Sprites
{
    /// <summary>
    /// 戦闘シーンで表示されるキャラ画像
    /// </summary>
    public class SpriteBattler : SpriteBase
    {
        /// <summary>
        /// バトラー
        /// </summary>
        protected GameBattler _battler;
        /// <summary>
        /// ダメージポップアップ用
        /// </summary>
        protected List<SpriteDamage> _damages = new List<SpriteDamage>();
        /// <summary>
        /// 基点の x座標
        /// </summary>
        protected float _homeX;
        /// <summary>
        /// 基点の y座標
        /// </summary>
        protected float _homeY;
        /// <summary>
        /// xオフセット
        /// </summary>
        protected float _offsetX;
        /// <summary>
        /// yオフセット
        /// </summary>
        protected float _offsetY;
        /// <summary>
        /// 対象の xオフセット
        /// </summary>
        protected float? _targetOffsetX;
        /// <summary>
        /// 対象の yオフセット
        /// </summary>
        protected float? _targetOffsetY;
        /// <summary>
        /// 移動の継続時間
        /// </summary>
        protected float _movementDuration;
        /// <summary>
        /// 選択エフェクトのカウント
        /// </summary>
        protected float _selectionEffectCount;

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="battler"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public virtual void Initialize(GameBattler battler) {
#else
        public virtual async Task Initialize(GameBattler battler) {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            base.Initialize();
            InitMembers();
            SetBattler(battler);
#else
            await base.Initialize();
            await InitMembers();
            await SetBattler(battler);
#endif
            _initialized = true;
        }

        /// <summary>
        /// メンバ変数を初期化
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public virtual void InitMembers() {
#else
        public virtual async Task InitMembers() {
            await UniteTask.Delay(0);
#endif
            anchor.x = 0.5;
            anchor.y = 1;
            _battler = null;
            _damages = new List<SpriteDamage>();
            _homeX = 0;
            _homeY = 0;
            _offsetX = 0;
            _offsetY = 0;
            _targetOffsetX = null;
            _targetOffsetY = null;
            _movementDuration = 0;
            _selectionEffectCount = 0;
        }

        /// <summary>
        /// バトラーを設定
        /// </summary>
        /// <param name="battler"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public virtual void SetBattler(GameBattler battler) {
#else
        public virtual async Task SetBattler(GameBattler battler) {
            await UniteTask.Delay(0);
#endif
            _battler = battler;
            battler.IsEscaped = false;
        }

        /// <summary>
        /// 基点を設定
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public virtual void SetHome(float x, float y) {
            _homeX = x;
            _homeY = y;
            UpdatePosition();
        }

        /// <summary>
        /// Update処理
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public override void UpdateTimeHandler() {
#else
        public override async Task UpdateTimeHandlerAsync() {
#endif
            if (!_initialized) return;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            base.UpdateTimeHandler();
#else
            await base.UpdateTimeHandlerAsync();
#endif

            if (_battler != null)
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                UpdateMain();
#else
                await UpdateMain();
#endif
                UpdateAnimation();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                UpdateDamagePopup();
#else
                await UpdateDamagePopup();
#endif
                UpdateSelectionEffect();
            }
            else
            {
                bitmap = null;
            }
        }

        /// <summary>
        /// バトラーを非表示にする
        /// Unite固有処理
        /// </summary>
        public virtual void UpdateHide() {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 表示・非表示のアップデート
        /// </summary>
        public override void UpdateVisibility() {
            base.UpdateVisibility();
            if (_battler == null || !_battler.IsSpriteVisible()) Visible = false;
        }

        /// <summary>
        /// 主要なアップデート
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public virtual void UpdateMain() {
#else
        public virtual async Task UpdateMain() {
#endif
            if (_battler.IsSpriteVisible())
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                UpdateBitmap();
#else
                await UpdateBitmap();
#endif
                UpdateFrame();
            }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            UpdateMove();
#else
            await UpdateMove();
#endif
            UpdatePosition();
        }

        /// <summary>
        /// 画像のアップデート
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public virtual void UpdateBitmap() {
        }
#else
        public virtual async Task UpdateBitmap() {
            await UniteTask.Delay(0);
        }
#endif

        /// <summary>
        /// フレームのアップデート
        /// </summary>
        public virtual void UpdateFrame() {
        }

        /// <summary>
        /// 移動のアップデート
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public virtual void UpdateMove() {
#else
        public virtual async Task UpdateMove() {
#endif
            if (_movementDuration > 0)
            {
                var d = _movementDuration;
                _offsetX = (_offsetX * (d - 1) + _targetOffsetX ?? 0) / d;
                _offsetY = (_offsetY * (d - 1) + _targetOffsetY ?? 0) / d;
                _movementDuration--;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                if (_movementDuration == 0) OnMoveEnd();
#else
                if (_movementDuration == 0) await OnMoveEnd();
#endif
            }
        }

        public void UpdateMoveFromPreview() {
            _offsetX = 0f;
        }

        /// <summary>
        /// 位置のアップデート
        /// </summary>
        public virtual void UpdatePosition() {
            X = _homeX + _offsetX;
            Y = _homeY + _offsetY;
        }

        /// <summary>
        /// アニメーションのアップデート
        /// </summary>
        public virtual void UpdateAnimation() {
            SetupAnimation();
        }

        /// <summary>
        /// ダメージポップアップのアップデート
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public virtual void UpdateDamagePopup() {
#else
        public virtual async Task UpdateDamagePopup() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            SetupDamagePopup();
#else
            await SetupDamagePopup();
#endif
            if (_damages.Count > 0)
            {
                _damages[0].Update();
                if (!_damages[0].IsPlaying())
                {
                    Destroy(_damages[0]);
                    _damages.RemoveAt(0);

                    //ダメージ表示終了後に初期化処理を行う
                    _battler.ClearDamagePopup();
                    _battler.ClearResult();
                }
            }
        }

        /// <summary>
        /// 選択エフェクトのアップデート
        /// </summary>
        public virtual void UpdateSelectionEffect() {
        }

        /// <summary>
        /// アニメーションの準備
        /// </summary>
        public virtual void SetupAnimation() {
            while (_battler.IsAnimationRequested())
            {
                var data = _battler.ShiftAnimation();
                var animation = DataManager.Self().GetAnimationDataModel(data.animationId);
                if (animation == null) break;
                var mirror = data.mirror;
                var delay = animation.playSpeed == 3 ? 0 : data.delay;
                StartAnimation(animation, mirror, delay, _battler.IsActor());
            }
        }

        /// <summary>
        /// ダメージポップアップの準備
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public virtual void SetupDamagePopup() {
#else
        public virtual async Task SetupDamagePopup() {
#endif
            if (_battler.IsDamagePopupRequested())
            {
                if (_battler.IsSpriteVisible())
                {
                    SpriteDamage damage = new GameObject().AddComponent<SpriteDamage>();
                    if (_battler.IsEnemy())
                    {
                        damage.transform.SetParent(gameObject.transform.parent);
                    }
                    else
                    {
                        damage.transform.SetParent(gameObject.transform);
                    }
                    damage.transform.localScale = new Vector3(1f, 1f, 1f);
                    damage.transform.localPosition = new Vector3(DamageOffsetX(), DamageOffsetY(), 0f);
                    damage.SetIsEnemy(_battler.IsEnemy());
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    damage.Initialize();
#else
                    await damage.Initialize();
#endif
                    damage.Setup(_battler);

                    _damages.Add(damage);
                }

                //ダメージ準備段階では、初期化は行わない
                _battler.ClearDamagePopup();
            }
        }

        /// <summary>
        /// ダメージの xオフセットを返す
        /// </summary>
        /// <returns></returns>
        public virtual float DamageOffsetX() {
            return 0;
        }

        /// <summary>
        /// ダメージの yオフセットを返す
        /// </summary>
        /// <returns></returns>
        public virtual float DamageOffsetY() {
            return 0;
        }

        /// <summary>
        /// 指定座標へ移動開始
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="duration"></param>
        public virtual void StartMove(float x, float y, float duration) {
            if (_targetOffsetX != x || _targetOffsetY != y)
            {
                _targetOffsetX = x;
                _targetOffsetY = y;
                _movementDuration = duration;
                if (duration == 0)
                {
                    _offsetX = x;
                    _offsetY = y;
                }
            }
        }

        /// <summary>
        /// 移動が終わった時に呼ばれるハンドラ
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public virtual void OnMoveEnd() {
        }
#else
        public virtual async Task OnMoveEnd() {
            await UniteTask.Delay(0);
        }
#endif

        /// <summary>
        /// 効果が加わっているか(常にfalse)
        /// </summary>
        /// <returns></returns>
        public virtual bool IsEffecting() {
            return false;
        }

        /// <summary>
        /// 移動中か
        /// </summary>
        /// <returns></returns>
        public virtual bool IsMoving() {
            return _movementDuration > 0;
        }

        /// <summary>
        /// 基点にいるか
        /// </summary>
        /// <returns></returns>
        public virtual bool InHomePosition() {
            return _offsetX == 0 && _offsetY == 0;
        }
    }
}