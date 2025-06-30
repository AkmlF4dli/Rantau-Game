using RPGMaker.Codebase.Runtime.Battle.Objects;
using RPGMaker.Codebase.Runtime.Common;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Battle.Window
{
    /// <summary>
    /// 戦闘シーンでのアイテム選択ウィンドウ
    /// UniteではWindow...系は戦闘シーンでのみ利用のため、ほぼWrapper
    /// </summary>
    public class WindowBattleItem : WindowItemList
    {
        /// <summary>
        /// 初期化処理
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public override void Initialize() {
#else
        public override async Task Initialize() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            base.Initialize();
#else
            await base.Initialize();
#endif

            //共通UIの適応を開始
            Init();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            Hide();
#else
            await Hide();
#endif
        }

        /// <summary>
        /// 指定したアイテムが含まれるか
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public override bool Includes(GameItem item) {
            return DataManager.Self().GetGameParty().CanUse(item);
        }
#else
        public override async Task<bool> Includes(GameItem item) {
            return await DataManager.Self().GetGameParty().CanUse(item);
        }
#endif

        /// <summary>
        /// ウィンドウを表示
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public override void Show() {
#else
        public override async Task Show() {
#endif
            SelectLast();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            ShowHelpWindow();
            base.Show();
#else
            await ShowHelpWindow();
            await base.Show();
#endif
        }

        /// <summary>
        /// ウィンドウを非表示(閉じるわけではない)
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public override void Hide() {
            HideHelpWindow();
            base.Hide();
        }
#else
        public override async Task Hide() {
            await HideHelpWindow();
            await base.Hide();
        }
#endif
    }
}