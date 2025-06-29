using System.Collections.Generic;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Battle.Window
{
    /// <summary>
    /// コマンド選択用ウィンドウ
    /// コマンド項目には画面に表示される「表示名」と、内部で識別に使われる「シンボル」が別に存在するので注意
    /// </summary>
    public class WindowCommand : WindowSelectable
    {
        /// <summary>
        /// コマンド項目の配列
        /// </summary>
        private List<Command> _list;
        private int _ext = 0;

        /// <summary>
        /// 初期化
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        override public void Initialize() {
#else
        override public async Task Initialize() {
#endif
            ClearCommandList();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            MakeCommandList();
            base.Initialize();
            Refresh();
#else
            await MakeCommandList();
            await base.Initialize();
            await Refresh();
#endif
            Select(0);
            Activate();
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
            return _list.Count;
        }

        /// <summary>
        /// リストの初期化
        /// </summary>
        public void ClearCommandList() {
            _list = new List<Command>();
            _ext = 0;
        }

        /// <summary>
        /// メニューに全項目を追加。 個々の追加は addCommand で行っている
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public virtual void MakeCommandList() {
#else
        public virtual async Task MakeCommandList() {
            await UniteTask.Delay(0);
#endif
        }

        /// <summary>
        /// コマンド項目を追加
        /// </summary>
        /// <param name="name"></param>
        /// <param name="symbol"></param>
        /// <param name="enabled"></param>
        public void AddCommand(string name, string symbol, bool enabled = false) {
            _list.Add(new Command
            {
                name = name,
                symbol = symbol,
                enabled = enabled
            });
        }

        /// <summary>
        /// 現在のコマンド項目のオブジェクトを返す
        /// </summary>
        /// <returns></returns>
        public Command CurrentData() {
            return Index() >= 0 ? _list[Index()] : null;
        }

        /// <summary>
        /// 現在のコマンド項目のシンボルを返す
        /// </summary>
        /// <returns></returns>
        public string CurrentSymbol() {
            if (CurrentData() != null  && CurrentData().symbol.Contains("skill"))
                return CurrentData() != null ? CurrentData().symbol.Substring(0, 5) : null;

            return CurrentData() != null ? CurrentData().symbol : null;
        }

        /// <summary>
        /// 現在のコマンド項目の追加情報を設定
        /// </summary>
        /// <param name="ext"></param>
        public void SetExt(int ext) {
            _ext = ext;
        }

        /// <summary>
        /// 現在のコマンド項目の追加情報を返す
        /// </summary>
        /// <returns></returns>
        public int CurrentExt() {
            return _ext;
        }

        /// <summary>
        /// 指定されたシンボルをもつコマンド項目の番号を返す
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public int FindSymbol(string symbol) {
            for (var i = 0; i < _list.Count; i++)
                if (_list[i].symbol == symbol)
                    return i;

            return -1;
        }

        /// <summary>
        /// 指定シンボルに従ってコマンド項目を選択
        /// </summary>
        /// <param name="symbol"></param>
        public void SelectSymbol(string symbol) {
            var index = FindSymbol(symbol);
            if (index >= 0)
            {
                if (!_list[index].enabled && _list[index].symbol == "escape")
                    return;
                Select(index);
            }
            else
            {
                Select(0);
            }
        }

        /// <summary>
        /// 指定した追加情報を持ったコマンド項目の番号を返す
        /// </summary>
        /// <param name="ext"></param>
        /// <returns></returns>
        public int FindExt(int ext) {
            for (var i = 0; i < _list.Count; i++)
                if (_ext == ext)
                    return i;

            return -1;
        }

        /// <summary>
        /// 指定した追加情報に従ってコマンド項目を選択
        /// </summary>
        /// <param name="ext"></param>
        public void SelectExt(int ext) {
            var index = FindExt(ext);
            if (index >= 0)
                Select(index);
            else
                Select(0);
        }

        /// <summary>
        /// 指定番号の項目を描画
        /// </summary>
        /// <param name="index"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public override void DrawItem(int index) {
        }
#else
        public override async Task DrawItem(int index) {
            await UniteTask.Delay(0);
        }
#endif

        /// <summary>
        /// OKのハンドラを呼ぶ
        /// </summary>
        public override void CallOkHandler() {
            var symbol = CurrentSymbol();
            if (IsHandled(symbol))
                CallHandler(symbol);
            else if (IsHandled("ok"))
                base.CallOkHandler();
            else
                Activate();
        }

        /// <summary>
        /// コンテンツの再描画
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public override void Refresh() {
#else
        public override async Task Refresh() {
#endif
            ClearCommandList();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            MakeCommandList();
            base.Refresh();
#else
            await MakeCommandList();
            await base.Refresh();
#endif
        }

        /// <summary>
        /// クリック時の処理
        /// </summary>
        /// <param name="symbol"></param>
        public void OnClickCommand(string symbol) {
            CallHandler(symbol);
        }
    }

    public class Command
    {
        public bool   enabled;
        public string name;
        public string symbol;
    }
}