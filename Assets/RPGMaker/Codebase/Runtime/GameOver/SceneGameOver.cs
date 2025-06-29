using JetBrains.Annotations;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime;
using RPGMaker.Codebase.Runtime.Common;
using RPGMaker.Codebase.Runtime.Map;
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RPGMaker.Codebase.Runtime.GameOver
{
    /// <summary>
    /// GameOverのScene制御
    /// </summary>
    public class SceneGameOver : SceneBase
    {
        private bool _isInput = true;

        protected override void Start() {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            StartAsync();
        }
        private async void StartAsync() { 
#endif
            //状態の更新
            GameStateHandler.SetGameState(GameStateHandler.GameState.GAME_OVER);

            //BGM再生
            PlayMe();

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            HudDistributor.Instance.StaticHudHandler().DisplayInitByScene();
#else
            await HudDistributor.Instance.StaticHudHandler().DisplayInitByScene();
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            HudDistributor.Instance.StaticHudHandler().FadeInFixedBlack(Init,   false, 0.5f, true);
#else
            await Init();
            HudDistributor.Instance.StaticHudHandler().FadeInFixedBlack(() => { }, false, 0.5f, true);
#endif
        }

        protected async void PlayMe() {
            SoundManager.Self().Init();
            SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_ME, DataManager.Self().GetSystemDataModel().bgm.gameOverMe);
            await SoundManager.Self().PlayMe();
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        protected override void Init() {
            base.Init();
        }
#else
        protected override async Task Init() {
            await base.Init();
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public override void Update() {
#else
        public override async Task Update() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            base.Update();
#else
            await base.Update();
#endif
            if (InputHandler.IsAnyPressed() && _isInput)
            {
                var SaveData = DataManager.Self().GetRuntimeSaveDataModel();
                //復活ポイントの設定があるかチェック
                if (SaveData.respawnPointData.mapID != "")
                {
                    //復活ポイントから復活する
                    HudDistributor.Instance.AllDestroyHudHandler();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    HudDistributor.Instance.StaticHudHandler().FadeOut(() =>
#else
                    HudDistributor.Instance.StaticHudHandler().FadeOut(async () =>
#endif
                    {
                        //復活ポイントから開始する
                        DataManager.Self().IsRespawnPoint = true;
                        if (SaveData.respawnPointData.eStatus == RuntimeSaveDataModel.RespawnPointData.EStatus.REVIVAL_ONLY)
                        {
                            //全員復活させる
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                            DataManager.Self().GetGameParty().ReviveBattleMembers();
#else
                            await DataManager.Self().GetGameParty().ReviveBattleMembers();
#endif
                        }
                        else
                        {
                            //全回復で復活する
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                            DataManager.Self().GetGameParty().ReviveAllBattleMembers();
#else
                            await DataManager.Self().GetGameParty().ReviveAllBattleMembers();
#endif
                        }
                        SceneManager.LoadScene("SceneMap");
                    }, UnityEngine.Color.black, 0.5f, true);
                }
                else
                {
                    // HUD系UIハンドリング
                    HudDistributor.Instance.AllDestroyHudHandler();
                    HudDistributor.Instance.StaticHudHandler().FadeOut(() =>
                    {
                        //状態の更新
                        GameStateHandler.SetGameState(GameStateHandler.GameState.TITLE);
                        SceneManager.LoadScene("Title");
                    }, UnityEngine.Color.black, 0.5f, true);
                }
                _isInput = false;
            }
        }

        public void FadeIn(bool isWhite, [CanBeNull] Action callBack = null) {
        }

        private IEnumerator FadeInCoroutine([CanBeNull] Action callBack = null) {
            SetFadeActive(true);
            UpdateFade();
            yield return new WaitForSeconds(0.05f);
            if (fadeDuration > 0)
            {
                StartCoroutine(FadeInCoroutine(callBack));
            }
            else
            {
                SetFadeActive(false);
                callBack?.Invoke();
            }
        }

        public void FadeOut(bool isWhite, [CanBeNull] Action callBack = null) {
        }

        private IEnumerator FadeOutCoroutine([CanBeNull] Action callBack = null) {
            SetFadeActive(true);
            UpdateFade();
            yield return new WaitForSeconds(0.05f);
            if (fadeDuration > 0)
                StartCoroutine(FadeOutCoroutine(callBack));
            else
                callBack?.Invoke();
        }

        //復活時の

        //復活ポイントから復帰する
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void RespawnPointExec(bool bFadeOut) {
#else
        public static async Task RespawnPointExec(bool bFadeOut) {
#endif
            var SaveData = DataManager.Self().GetRuntimeSaveDataModel();

            //復活ポイントの設定があるかチェック
            if (SaveData.respawnPointData.mapID != "")
            {
                //SceneManager.LoadScene("GameOver");
                if (bFadeOut)
                {
                    HudDistributor.Instance.AllDestroyHudHandler();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    HudDistributor.Instance.StaticHudHandler().FadeOut(() =>
#else
                    HudDistributor.Instance.StaticHudHandler().FadeOut(async () =>
#endif
                    {
                        if (SaveData.respawnPointData.gameOverScreenFlg == false)
                        { 
                            //復活ポイントから開始する
                            DataManager.Self().IsRespawnPoint = true;
                            if (SaveData.respawnPointData.eStatus == RuntimeSaveDataModel.RespawnPointData.EStatus.REVIVAL_ONLY)
                            {
                                //全員復活させる
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                                DataManager.Self().GetGameParty().ReviveBattleMembers();
#else
                                await DataManager.Self().GetGameParty().ReviveBattleMembers();
#endif
                            }
                            else
                            {
                                //全回復で復活する
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                                DataManager.Self().GetGameParty().ReviveAllBattleMembers();
#else
                                await DataManager.Self().GetGameParty().ReviveAllBattleMembers();
#endif
                            }
                            GameStateHandler.SetGameState(GameStateHandler.GameState.MAP);
                            SceneManager.LoadScene("SceneMap");
                        }
                        else
                        {
                            GameStateHandler.SetGameState(GameStateHandler.GameState.GAME_OVER);
                            SceneManager.LoadScene("GameOver");
                        }
                    }, UnityEngine.Color.black, 0.5f, true);
                }
                else
                {
                    if (SaveData.respawnPointData.gameOverScreenFlg == false)
                    {
                        //復活ポイントから開始する
                        DataManager.Self().IsRespawnPoint = true;
                        if (SaveData.respawnPointData.eStatus == RuntimeSaveDataModel.RespawnPointData.EStatus.REVIVAL_ONLY)
                        {
                            //全員復活させる
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                            DataManager.Self().GetGameParty().ReviveBattleMembers();
#else
                            await DataManager.Self().GetGameParty().ReviveBattleMembers();
#endif
                        }
                        else
                        {
                            //全回復で復活する
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                            DataManager.Self().GetGameParty().ReviveAllBattleMembers();
#else
                            await DataManager.Self().GetGameParty().ReviveAllBattleMembers();
#endif
                        }
                        GameStateHandler.SetGameState(GameStateHandler.GameState.MAP);
                        SceneManager.LoadScene("SceneMap");
                    }
                    else
                    {
                        GameStateHandler.SetGameState(GameStateHandler.GameState.GAME_OVER);
                        SceneManager.LoadScene("GameOver");
                    }
                }
            }
            else
            {
               SceneManager.LoadScene("GameOver");
            }
        }
    }
}