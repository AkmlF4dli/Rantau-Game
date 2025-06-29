using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.Runtime.Battle.Objects;
using RPGMaker.Codebase.Runtime.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RPGMaker.Codebase.Runtime.Battle.Window
{
    /// <summary>
    /// 敵の選択用のウィンドウ
    /// </summary>
    public class WindowBattleEnemy : WindowSelectable
    {
        /// <summary>
        /// 敵一覧
        /// </summary>
        private List<GameEnemy> _enemies;

        /// <summary>
        /// 初期化
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        override public void Initialize() {
#else
        override public async Task Initialize() {
#endif
            _enemies = new List<GameEnemy>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            base.Initialize();
            Refresh();
#else
            await base.Initialize();
            await Refresh();
#endif

            //共通UIの適応を開始
            Init();

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            Hide();
#else
            await Hide();
#endif

            SetUpNav();
        }

        /// <summary>
        /// ウィンドウが持つ最大項目数を返す
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public override int MaxItems() {
#else
        public override async Task<int> MaxItems() {
            await UniteTask.Delay(0);
#endif
            return _enemies.Count;
        }

        /// <summary>
        /// 選択中の[敵キャラ]を返す
        /// </summary>
        /// <returns></returns>
        public GameEnemy Enemy() {
            return _enemies[Index()];
        }

        /// <summary>
        /// 選択中の[敵キャラ]の番号を返す
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public int EnemyIndex() {
            var enemy = Enemy();
            return enemy?.Index() ?? -1;
        }
#else
        public async Task<int> EnemyIndex() {
            var enemy = Enemy();
            return enemy != null ? await enemy.Index() : -1;
        }
#endif

        /// <summary>
        /// 指定番号の項目を描画
        /// </summary>
        /// <param name="index"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public override void DrawItem(int index) {
#else
        public override async Task DrawItem(int index) {
            await UniteTask.Delay(0);
#endif
            if (_enemies.Count < index + 1) return;

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            selectors[index].SetUp(index, _enemies[index].Name(), (int idx) =>
#else
            selectors[index].SetUp(index, _enemies[index].Name(), async (int idx) =>
#endif
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                Select(index);
#else
                await Select(index);
#endif
                BattleManager.GetSpriteSet().SelectEnemy(_enemies[index]);
            }, OnClickSelection);
        }

        /// <summary>
        /// ウィンドウを表示
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public override void Show() {
#else
        public override async Task Show() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            Refresh();
#else
            await Refresh();
#endif
            var selects = gameObject.transform.Find("WindowArea/List").gameObject.GetComponentsInChildren<Selectable>();
            //ボタンの有効状態切り替え
            bool flg = false;
            for (int i = 0; selects != null && i < selects.Length; i++)
            {
                selects[i].GetComponent<WindowButtonBase>().SetEnabled(true);
                //元々フォーカスが当たっていた場所に、フォーカスしなおし
                if (selects[i].GetComponent<WindowButtonBase>().IsHighlight())
                {
                    flg = true;
                    selects[i].GetComponent<Button>().Select();
                }
            }
            if (!flg && selects != null)
            {
                selects[0].GetComponent<Button>().Select();
            }
            EventSystem.current.SetSelectedGameObject(selects[0].gameObject);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            Select(0);
#else
            await Select(0);
#endif

            //ナビゲーション設定しなおし
            SetUpNav();

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            base.Show();
#else
            await base.Show();
#endif
        }

        /// <summary>
        /// ウィンドウを非表示(閉じるわけではない)
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public override void Hide() {
#else
        public override async Task Hide() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            base.Hide();
#else
            await base.Hide();
#endif
            var selects = gameObject.transform.Find("WindowArea/List").gameObject.GetComponentsInChildren<Selectable>();
            for (int i = 0; selects != null && i < selects.Length; i++)
            {
                selects[i].GetComponent<WindowButtonBase>().SetEnabled(false);
            }
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataManager.Self().GetGameTroop().Select(null);
#else
            await DataManager.Self().GetGameTroop().Select(null);
#endif
            if (BattleManager.GetSpriteSet() != null) 
                BattleManager.GetSpriteSet().SelectEnemy(null);
        }

        /// <summary>
        /// コンテンツの再描画
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public new void Refresh() {
#else
        public new async Task Refresh() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _enemies = DataManager.Self().GetGameTroop().AliveMembers().Aggregate(new List<GameEnemy>(), (l, e) =>
#else
            _enemies = (await DataManager.Self().GetGameTroop().AliveMembers()).Aggregate(new List<GameEnemy>(), (l, e) =>
#endif
            {
                l.Add((GameEnemy) e);
                return l;
            });
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            base.Refresh();
#else
            await base.Refresh();
#endif
        }

        /// <summary>
        /// 指定した番号の項目を選択
        /// </summary>
        /// <param name="index"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public new void Select(int index) {
#else
        public new async Task Select(int index) {
#endif
            base.Select(index);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataManager.Self().GetGameTroop().Select(Enemy());
#else
            await DataManager.Self().GetGameTroop().Select(Enemy());
#endif
        }

        /// <summary>
        /// ナビゲーション設定
        /// </summary>
        private void SetUpNav() {
            // 選択UIのナビゲーションを設定する
            var selects = gameObject.transform.Find("WindowArea/List").gameObject.GetComponentsInChildren<Selectable>();
            var half = (selects.Length + 1) / 2; // 項目の半分の値（切り上げ）
            for (var i = 0; i < selects.Length; i++)
            {
                var nav = selects[i].navigation;
                nav.mode = Navigation.Mode.Explicit;

                // 前半部分
                if (half - 1 >= i)
                {
                    nav.selectOnRight = selects[selects.Length % 2 == 1 && i == selects.Length - half ? i : i + half];
                    nav.selectOnLeft = selects[i == 0 ? i : i + half - 1]; // 先頭は自身を設定
                    nav.selectOnDown = selects[i == half - 1 ? i : i + 1]; // 前半の末尾は自身を設定
                    nav.selectOnUp = selects[i == 0 ? i : i - 1]; // 先頭は自身を設定
                }
                // 後半部分
                else
                {
                    nav.selectOnRight =
                        selects[selects.Length % 2 == 0 && i == selects.Length - 1 ? i : i - half + 1]; // 末尾は自身を設定
                    nav.selectOnLeft = selects[i - half];
                    nav.selectOnDown = selects[i == selects.Length - 1 ? i : i + 1]; // 末尾は自身を設定
                    nav.selectOnUp = selects[i == half ? i : i - 1]; // 後半の先頭は自身を設定
                }

                selects[i].navigation = nav;
            }
        }
    }
}