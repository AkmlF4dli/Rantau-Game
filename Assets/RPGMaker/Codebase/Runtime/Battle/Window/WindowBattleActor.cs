using RPGMaker.Codebase.Runtime.Battle.Objects;
using RPGMaker.Codebase.Runtime.Common;
using System.Collections.Generic;
using RPGMaker.Codebase.CoreSystem.Helper;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Battle.Window
{
    /// <summary>
    /// 戦闘中にアクターを選択するウィンドウ
    /// </summary>
    public class WindowBattleActor : WindowBattleStatus
    {
        /// <summary>
        /// 初期化
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
            Openness = 255;

            //共通UIの適応を開始
            Init();

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            Hide();
#else
            await Hide();
#endif
        }

        /// <summary>
        /// ウィンドウを表示
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        override public void Show() {
#else
        override public async Task Show() {
#endif
            for (int i = 0; i < selectors.Count; i++)
            {
                selectors[i].GetComponent<WindowButtonBase>().SetEnabled(true);
            }
            SetNavigation();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            Select(0);
            base.Show();
#else
            await Select(0);
            await base.Show();
#endif
        }

        /// <summary>
        /// ウィンドウを非表示(閉じるわけではない)
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        override public void Hide() {
#else
        override public async Task Hide() {
#endif
            for (int i = 0; i < selectors.Count; i++)
            {
                selectors[i].GetComponent<WindowButtonBase>().SetEnabled(false);
            }
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            base.Hide();
            DataManager.Self().GetGameParty().Select(null);
#else
            await base.Hide();
            await DataManager.Self().GetGameParty().Select(null);
#endif
        }

        /// <summary>
        /// 指定した番号の項目を選択
        /// </summary>
        /// <param name="index"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private new void Select(int index) {
            base.Select(index);
            DataManager.Self().GetGameParty().Select(Actor());
        }
#else
        private new async Task Select(int index) {
            base.Select(index);
            await DataManager.Self().GetGameParty().Select(await Actor());
        }
#endif

        /// <summary>
        /// アクターデータを取得
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private GameActor Actor() {
            return (GameActor) DataManager.Self().GetGameParty().Members()[Index()];
        }
#else
        private async Task<GameActor> Actor() {
            return (GameActor) (await DataManager.Self().GetGameParty().Members())[Index()];
        }
#endif
        
        /// <summary>
        /// ナビゲーション設定
        /// </summary>
        public void SetNavigation() {
            var selects = gameObject.transform.Find("List").gameObject
                .GetComponentsInChildren<Selectable>();
            var buttons = new List<GameObject>();

            List<Selectable> work = new List<Selectable>();
            for (var i = 0; i < selects.Length; i++)
            {
                if (selects[i].transform.Find("Highlight") == null)
                {
                    continue;
                }
                work.Add(selects[i]);
            }

            for (var i = 0; i < work.Count; i++)
            {
                if(work[i].GetComponent<WindowButtonBase>() != null)
                    work[i].GetComponent<WindowButtonBase>().SetRaycastTarget(true);

                buttons.Add(work[i].gameObject);
                work[i].targetGraphic = work[i].transform.Find("Highlight").GetComponent<Image>();
                var nav = work[i].navigation;
                nav.mode = Navigation.Mode.Explicit;

                nav.selectOnRight = work[i < work.Count - 1 ? i + 1 : 0];
                nav.selectOnLeft = work[i == 0 ? work.Count - 1 : i - 1];
                work[i].navigation = nav;
            }
            buttons[0].GetComponent<Button>().Select();
        }
    }
}