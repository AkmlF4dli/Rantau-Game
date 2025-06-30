using System;
using System.Linq;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime;
using RPGMaker.Codebase.Runtime.Common.Component.Hud.PostEffect.Tweener;
using UnityEngine;

namespace RPGMaker.Codebase.Runtime.Common.Component.Hud.PostEffect
{
    public class PostEffect : MonoBehaviour
    {
        // TODO: もっとエレガントにできないか
        // TODO: Test削除
        private readonly string[] _effectTypes =
        {
            "RPGMaker.Codebase.Runtime.PostEffectOnRender.Test",
            "RPGMaker.Codebase.Runtime.PostEffectOnRender.Glitch",
            "RPGMaker.Codebase.Runtime.PostEffectOnRender.Blur",
            "RPGMaker.Codebase.Runtime.PostEffectOnRender.Bloom",
            "RPGMaker.Codebase.Runtime.PostEffectOnRender.Fill",
            "RPGMaker.Codebase.Runtime.PostEffectOnRender.Pixelate",

            // トランジション
            "RPGMaker.Codebase.Runtime.PostEffectOnRender.WipeCircle",
            "RPGMaker.Codebase.Runtime.PostEffectOnRender.WipeX",
            "RPGMaker.Codebase.Runtime.PostEffectOnRender.WipeY",
            "RPGMaker.Codebase.Runtime.PostEffectOnRender.WipeCircle",
            "RPGMaker.Codebase.Runtime.PostEffectOnRender.WipeCircle",
        };

        private const int NumEffect = 6;

        private PostEffectTweener _tweener = new PostEffectTweener();

        /*
         * エフェクトを適用
         * @param callback エフェクト適用後のコールバック
         * @param effect エフェクトの種類
         * @param persistent 永続的なエフェクト
         * @param frame エフェクトのフレーム数
         * @param param エフェクトのパラメータ
         * @param wait エフェクト適用後の待機
         * @param restore セーブデータからのエフェクト追加
         * @return エフェクトのハンドル
         */
        public int Apply(
            Action callback,
            int effect,
            bool persistent,
            float frame = 0,
            int[] param = null,
            bool wait = false,
            bool restore = false,
            bool isTransition = false
        )
        {
            var effectType = _effectTypes[effect];
            var duration = persistent ? -1 : frame / 60.0f;
            var handle =
                _tweener.Add(Type.GetType(effectType), duration, param, isTransition);

            // セーブデータからの復帰の場合、重複セーブを抑制
            if (!restore && persistent)
            {
                DataManager.Self().GetRuntimeSaveDataModel().runtimeScreenDataModel.postEffect.postEffectData.Add(
                    new RuntimeScreenDataModel.PostEffectData
                    {
                        type = effect,
                        param = param!.ToList(),
                    });
            }

            if (persistent || !wait || frame == 0)
            {
                callback?.Invoke();
            }
            else
            {
                TimeHandler.Instance.AddTimeAction(duration, callback, false);
            }

            return handle;
        }

        /*
         * トランジションを適用
         * @param frame トランジションのフレーム数
         * @return エフェクトのハンドル
         */
        public int ApplyTransition(int transition, float frame)
        {
            return Apply(
                null,
                transition + NumEffect,
                false,
                frame,
                null,
                false,
                false,
                true
            );
        }

        /**
         * エフェクトを削除
         */
        public bool Remove(int handle)
        {
            return _tweener.Remove(handle);
        }

        /**
         * エフェクトをすべて削除
         * @param callback 削除後のコールバック
         */
        public bool RemoveAll(Action callback = null)
        {
            callback?.Invoke();
            DataManager.Self().GetRuntimeSaveDataModel().runtimeScreenDataModel.postEffect.postEffectData.Clear();
            return _tweener.RemoveAll();
        }
        
        /**
         * エフェクトを停止
         */
        public bool Stop(int handle)
        {
            return _tweener.Stop(handle);
        }

        /**
         * エフェクトをすべて停止
         */
        public bool StopAll()
        {
            return _tweener.StopAll();
        }

        /**
         * エフェクトを再開
         */
        public bool Resume(int handle)
        {
            return _tweener.Resume(handle);
        }

        /**
         * エフェクトをすべて再開
         */
        public bool ResumeAll()
        {
            return _tweener.ResumeAll();
        }

        private void Update()
        {
            _tweener.Update();
        }

        private void OnDestroy()
        {
            _tweener.Dispose();
            _tweener = null;
        }
    }
}