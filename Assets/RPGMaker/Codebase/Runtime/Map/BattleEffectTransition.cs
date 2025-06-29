using System;
using RPGMaker.Codebase.Runtime.Common;
using UnityEngine;

namespace RPGMaker.Codebase.Runtime.Map
{
    public class BattleEffectTransition
    {
        // NOTE: トランジションエフェクトのフレーム数はエフェクトのフレーム数 - 1
        //  稀に1フレーム先にトランジションエフェクトが終了することがある為
        public static readonly int NumTransitionFrames = 29;

        private int _counter = 0;

        public int EffectHandle { get; set; } = -1;

        public void Update()
        {
            if (GameStateHandler.CurrentGameState() != GameStateHandler.GameState.BEFORE_BATTLE)
            {
                return;
            }

            _counter++;
            if (_counter >= NumTransitionFrames)
            {
                _counter = 0;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                MapManager.EndBattleTransition(EffectHandle);
#else
                _ = MapManager.EndBattleTransition(EffectHandle);
#endif
            }
        }
    }
}