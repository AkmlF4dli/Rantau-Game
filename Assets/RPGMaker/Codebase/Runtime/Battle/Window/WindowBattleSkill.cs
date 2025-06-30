using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Battle.Window
{
    /// <summary>
    /// 戦闘シーンでのスキル選択ウィンドウ
    /// UniteではWindow...系は戦闘シーンでのみ利用のため、ほぼWrapper
    /// </summary>
    public class WindowBattleSkill : WindowSkillList
    {
        /// <summary>
        /// 初期化
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