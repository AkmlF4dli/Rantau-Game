using RPGMaker.Codebase.Runtime.Map;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Common.Component.Hud.GameProgress
{
    public class GameTimer : MonoBehaviour
    {
        // const
        //--------------------------------------------------------------------------------------------------------------
        private Text _minute;

        // データプロパティ
        //--------------------------------------------------------------------------------------------------------------
        private GameObject _prefab;
        private Text       _second;
        private float      _timerCount;

        // initialize methods
        //--------------------------------------------------------------------------------------------------------------
        /**
         * 初期化
         */
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void Init() {
#else
        public async Task Init() {
#endif
            if (_prefab == null)
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _prefab = HudDistributor.Instance.NowHudHandler().TimerInitObject();
#else
                _prefab = await HudDistributor.Instance.NowHudHandler().TimerInitObject();
#endif
            }

            _second = _prefab.transform.Find("Canvas/DisplayArea/Timer/Second").GetComponent<Text>();
            _minute = _prefab.transform.Find("Canvas/DisplayArea/Timer/Minute").GetComponent<Text>();
            SceneManager.activeSceneChanged += ActiveSceneChanged;
        }
        
        /// <summary>
        /// シーンが切り替わったときに次のシーンが何かを取得
        /// </summary>
        /// <param name="thisScene"></param>
        /// <param name="nextScene"></param>
        void ActiveSceneChanged (UnityEngine.SceneManagement.Scene thisScene, UnityEngine.SceneManagement.Scene nextScene) {
            //タイトルだったら、タイマーを削除する
            if (nextScene.name == "Title")
            {
                Destroy(_prefab);
            }
            SceneManager.activeSceneChanged -= ActiveSceneChanged;
        }

        public void SetGameTimer(bool toggle, int count) {
            if (toggle)
            {
                _timerCount = count;
                SetDisplayTimerCount();
            }

            _prefab.SetActive(toggle);
        }

        public void SetGameTimer(bool toggle, float count) {
            if (toggle)
            {
                _timerCount = count;
                SetDisplayTimerCount();
            }

            _prefab.SetActive(toggle);
        }

        public float GetGameTimer() {
            if (_prefab.activeSelf)
            {
                return _timerCount;
            }
            return -1;
        }

        void Update() {
            if (_timerCount > 0 && !MenuManager.IsMenuActive && !MenuManager.IsShopActive)
            {
                _timerCount -= Time.deltaTime;
                SetDisplayTimerCount();
            }
        }


        private void SetDisplayTimerCount() {
            _second.text = ((int) _timerCount % 60).ToString("D2");
            _minute.text = ((int) _timerCount / 60).ToString("D2");
        }
    }
}