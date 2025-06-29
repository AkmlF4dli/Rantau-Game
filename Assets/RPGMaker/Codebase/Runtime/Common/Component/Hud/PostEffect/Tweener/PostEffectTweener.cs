using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using RPGMaker.Codebase.Runtime.PostEffectOnRender;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime.RuntimeScreenDataModel;

namespace RPGMaker.Codebase.Runtime.Common.Component.Hud.PostEffect.Tweener
{
    /**
     * エフェクトの時間制御を管理
     * NOTE:
     *  RPGMaker.Codebase.Runtime.Common.TimeHandlerの代わり
     */
    public class PostEffectTweener: IDisposable
    {
        /*
         * 同時に再生できるエフェクトの最大数
         */
        public static int MaxTweens = 10;

        private List<PostEffectTween> _tweens;

        public PostEffectTweener()
        {
            _tweens = new List<PostEffectTween>();
            for (var i = 0; i < MaxTweens; i++)
            {
                _tweens.Add(null);
            }
        }

        ~PostEffectTweener()
        {
        }

        public void Dispose() {
            for (var i = 0; i < _tweens.Count; i++)
            {
                if (_tweens[i] != null) _tweens[i].Dispose();
                _tweens[i] = null;
            }

            _tweens.Clear();
            _tweens = null;
        }

        /*
         * エフェクトを更新
         */
        public void Update()
        {
            for (var i = 0; i < _tweens.Count; i++)
            {
                if (_tweens[i] == null) continue;
                if (!_tweens[i].IsPlaying && !_tweens[i].IsCompleted) continue;
                if (_tweens[i].IsPlaying) _tweens[i].Update();
                if (_tweens[i].IsCompleted) Remove(_tweens[i].Handle);
            }
        }

        /*
         * エフェクトを追加
         * @param type エフェクトの種類
         * @param duration エフェクトの時間
         * @param param エフェクトのパラメータ
         * @return エフェクトのハンドル
         */
        public int Add(Type type, float duration, int[] param, bool isTransition = false)
        {
            int handle = GetEmptyHandle();
            if (handle != -1)
            {
                _tweens[handle] = new PostEffectTween(type, duration, param, isTransition)
                {
                    Handle = handle
                };
            }

            return handle;
        }

        /*
         * エフェクトを削除する
         * @param tween 削除するエフェクトのハンドル
         * @return 削除できたらtrue
         */
        public bool Remove(int handle)
        {
            PostEffectTween tween = _tweens[handle];
            if (tween == null) return false;
            tween.Dispose();
            _tweens[handle] = null;
            return true;
        }

        /*
         * 全てのエフェクトを削除する
         */
        public bool RemoveAll()
        {
            bool returnValue = false;
            for (var i = 0; i < _tweens.Count; i++)
            {
                returnValue |= Remove(i);
            }

            return returnValue;
        }

        /**
         * エフェクトを停止する
         */
        public bool Stop(int handle)
        {
            PostEffectTween tween = _tweens[handle];
            if (tween == null) return false;
            tween.Stop();
            return true;
        }

        /**
         * エフェクトを全て停止する
         */
        public bool StopAll()
        {
            bool returnValue = false;
            for (var i = 0; i < _tweens.Count; i++)
            {
                returnValue |= Stop(i);
            }
            
            return returnValue;
        }
        
        /**
         * エフェクトを再開する
         */
        public bool Resume(int handle)
        {
            PostEffectTween tween = _tweens[handle];
            if (tween == null) return false;
            tween.Resume();
            return true;
        }

        /**
         * エフェクトを全て再開する
         */
        public bool ResumeAll()
        {
            bool returnValue = false;
            for (var i = 0; i < _tweens.Count; i++)
            {
                returnValue |= Resume(i);
            }
            
            return returnValue;
        }

        /*
         * 空いているハンドルを返す
         */
        public int GetEmptyHandle()
        {
            for (var i = 0; i < _tweens.Count; i++)
            {
                if (_tweens[i] == null) return i;
            }

            return -1;
        }
    }

    /**
     * エフェクト時間制御
     */
    public class PostEffectTween
    {
        private enum Status
        {
            Rise,
            Keep,
            Drop
        }

        static readonly float MaxEasingDuration = 0.5f;
        static readonly float EaseDisableDuration = 0.5f;

        private int _handle;
        private float _duration;
        private float _easingDuration;
        private float _elapsedTime;
        private float _dropTime;
        private PostEffectBase _postEffect;
        private float _currentRatio = 0.0f;
        private Status _currentStatus;
        private bool _isTransition;
        private PostEffectRendererFeature _postEffectRendererFeature;

        private bool _isPlaying;

        // TODO: 要らないかも
        private bool _isCompleted;

        /*
         * エフェクトが再生中ならtrue
         */
        public bool IsPlaying => _isPlaying;

        /*
         * エフェクトが終了していたらtrue
         */
        public bool IsCompleted => _isCompleted;

        /*
         * エフェクトのハンドル
         */
        public int Handle
        {
            get => _handle;
            set => _handle = value;
        }

        public PostEffectTween(Type type, float duration, int[] param, bool isTransition)
        {
            _duration = duration;
            _elapsedTime = 0;
            _isTransition = isTransition;

            if (_duration <= 0)
            {
                // 永続的（最大値まではイージング）
                _easingDuration = MaxEasingDuration;
                _dropTime = float.MaxValue;
                _currentStatus = Status.Rise;
            }
            else if (_duration >= MaxEasingDuration * 2)
            {
                // イージング時間が最長のイージング時間 * 2を含める場合（通常）
                _easingDuration = MaxEasingDuration;
                _dropTime = _duration - _easingDuration;
                _currentStatus = Status.Rise;
            }
            else if (_duration > EaseDisableDuration)
            {
                // 最長のイージング時間が得られない場合はイージング時間はエフェクト時間の半分
                _easingDuration = _duration / 2;
                _dropTime = _duration - _easingDuration;
                _currentStatus = Status.Rise;
            }
            else
            {
                // イージング時間が最長のイージング時間以下の場合はイージングを無効にする
                _easingDuration = 0;
                _dropTime = _duration;
                _currentStatus = Status.Keep;
            }

            if (Camera.main != null)
            {
                _postEffect = Camera.main.gameObject.AddComponent(type) as PostEffectBase;
                if (_postEffect != null)
                {
                    _postEffect.enabled = true;
                    _postEffect.Param = param;
                    _isPlaying = true;
                    if (Commons.IsURP())
                    {
                        _postEffectRendererFeature = ScriptableObject.CreateInstance<PostEffectRendererFeature>();
                        _postEffectRendererFeature.SetParameters(_postEffect);
                        AddFeature(_postEffectRendererFeature);
                    }
                }
            }
        }

        ~PostEffectTween()
        {
            // Debug.Log("PostEffectTween is destroyed");
        }

        public void Dispose() {
            if (_postEffect != null)
            {
                _postEffect.Dispose();
                UnityEngine.Object.Destroy(_postEffect);
                _postEffect = null;
            }
            if (_postEffectRendererFeature != null)
            {
                DeleteFeature(_postEffectRendererFeature);
                _postEffectRendererFeature.Dispose();
                UnityEngine.Object.Destroy(_postEffectRendererFeature);
                _postEffectRendererFeature = null;
            }
        }
        
        /**
         * エフェクトを停止
         */
        public void Stop()
        {
            if (!_isPlaying || _isCompleted) return;
            _isPlaying = false;
            if (_postEffect != null) _postEffect.enabled = false;
            
            if (Commons.IsURP())
            {
                _postEffectRendererFeature.SetActive(false);
            }
        }
        
        /**
         * エフェクトを再開
         */
        public void Resume()
        {
            if (_isPlaying || _isCompleted) return;
            _isPlaying = true;
            if (_postEffect != null) _postEffect.enabled = true;
            
            if (Commons.IsURP())
            {
                _postEffectRendererFeature.SetActive(true);
            }
        }

        protected void AddFeature(ScriptableRendererFeature feature) {
            (var rendererData, var rendererFeatures) = Commons.GetUniteRendererDataFeatues();
            rendererFeatures.Insert(rendererFeatures.Count - Commons.AdditionalFeatureInsertBottomOffset, feature);
            //Debug.Log($"rendererFeatures.Count: {rendererFeatures.Count}: {string.Join(", ", rendererFeatures.Select(x => $"{x}"))}");

            rendererData.SetDirty();
        }

        protected void DeleteFeature(ScriptableRendererFeature feature) {
            (var rendererData, var rendererFeatures) = Commons.GetUniteRendererDataFeatues();
            var index = rendererFeatures.IndexOf(feature);
            if (index >= 0)
            {
                rendererFeatures.RemoveAt(index);
                //Debug.Log($"rendererFeatures.Count: {rendererFeatures.Count}: {string.Join(", ", rendererFeatures.Select(x => $"{x}"))}");
                rendererData.SetDirty();
            }
            else
            {
                Debug.Log($"index is negative");
            }
        }

        /**
         * エフェクトを更新
         * @return エフェクトが終了したかどうか
         */
        public void Update()
        {
            if (_postEffect == null || !_isPlaying) return;

            if (!_isTransition)
            {
                switch (_currentStatus)
                {
                    case Status.Rise:
                        _currentRatio = _elapsedTime / _easingDuration;
                        _postEffect.UpdateParams(EaseOutQuad(_currentRatio));
                        break;

                    case Status.Drop:
                        _currentRatio = (_elapsedTime - _dropTime) / _easingDuration;
                        _postEffect.UpdateParams(EaseOutQuad(1 - _currentRatio));
                        break;

                    default:
                        _postEffect.UpdateParams(1.0f);
                        break;
                }   
            }
            else
            {
                _currentRatio = _elapsedTime / _duration;
                _postEffect.UpdateParams(Linear(1 - _currentRatio));
            }

            _elapsedTime += Time.deltaTime;

            if (_elapsedTime > _easingDuration && _currentStatus == Status.Rise)
            {
                _currentStatus = Status.Keep;
            }

            if (_elapsedTime >= _dropTime && _currentStatus == Status.Keep)
            {
                _currentStatus = Status.Drop;
            }

            if (_duration <= 0) return;

            if (_elapsedTime >= _duration)
            {
                _isPlaying = false;
                _isCompleted = true;

                if (_postEffect != null) _postEffect.enabled = false;
            }
        }

        public static float Linear(float t)
        {
            return t;
        }

        public static float EaseOutQuad(float t)
        {
            return 1 - (1 - t) * (1 - t);
        }
    }
}