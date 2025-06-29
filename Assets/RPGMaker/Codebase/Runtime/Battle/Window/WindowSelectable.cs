using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.Runtime.Battle.Objects;
using RPGMaker.Codebase.Runtime.Common;
using RPGMaker.Codebase.Runtime.Common.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace RPGMaker.Codebase.Runtime.Battle.Window
{
    /// <summary>
    /// コマンドカーソルの移動やスクロールを行うウィンドウ
    /// </summary>
    public class WindowSelectable : WindowBattle
    {
        /// <summary>
        /// 入力ハンドラ
        /// </summary>
        protected Dictionary<string, Action> _handlers;
        /// <summary>
        /// ヘルプウィンドウ
        /// </summary>
        private WindowHelp _helpWindow;
        /// <summary>
        /// 選択されている項目の番号
        /// </summary>
        protected int _index;
        /// <summary>
        /// アイコン画像
        /// </summary>
        protected Image _iconImage = null;
        /// <summary>
        /// Selector
        /// </summary>
        [SerializeField] protected List<Selector> selectors;

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
            _index = -1;
            _helpWindow = null;
            _handlers = new Dictionary<string, Action>();
            Deactivate();
        }

        /// <summary>
        /// 選択中の項目の番号を返す
        /// </summary>
        /// <returns></returns>
        public int Index() {
            return _index;
        }

        /// <summary>
        /// ウィンドウが持つ最大項目数を返す
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public virtual int MaxItems() {
#else
        public virtual async Task<int> MaxItems() {
            await UniteTask.Delay(0);
#endif
            return 4;
        }

        /// <summary>
        /// ウィンドウをアクティブにする
        /// </summary>
        public new void Activate() {
            base.Activate();
            Reselect();
        }

        /// <summary>
        /// 非アクティブにする
        /// </summary>
        public new void Deactivate() {
            base.Deactivate();
            Reselect();
        }

        /// <summary>
        /// 指定した番号の項目を選択
        /// </summary>
        /// <param name="index"></param>
        public void Select(int index) {
            _index = index;
            CallUpdateHelp();
        }

        /// <summary>
        /// 全項目を非選択
        /// </summary>
        public void Deselect() {
            Select(-1);
        }

        /// <summary>
        /// 項目の再選択
        /// </summary>
        public void Reselect() {
            Select(_index);
        }

        /// <summary>
        /// ヘルプウィンドウを設定
        /// </summary>
        /// <param name="helpWindow"></param>
        public void SetHelpWindow(WindowHelp helpWindow) {
            _helpWindow = helpWindow;
            CallUpdateHelp();
        }

        /// <summary>
        /// ヘルプウィンドウを表示
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ShowHelpWindow() {
            if (_helpWindow) _helpWindow.Show();
        }
#else
        public async Task ShowHelpWindow() {
            if (_helpWindow) await _helpWindow.Show();
        }
#endif

        /// <summary>
        /// ヘルプウィンドウを非表示
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void HideHelpWindow() {
            if (_helpWindow) _helpWindow.Hide();
        }
#else
        public async Task HideHelpWindow() {
            if (_helpWindow) await _helpWindow.Hide();
        }
#endif

        /// <summary>
        /// ハンドラを設定
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="method"></param>
        public void SetHandler(string symbol, Action method) {
            _handlers[symbol] = method;
        }

        /// <summary>
        /// 指定されたハンドラが利用可能か
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public bool IsHandled(string symbol) {
            return _handlers.ContainsKey(symbol);
        }

        /// <summary>
        /// 指定したハンドラを呼ぶ
        /// </summary>
        /// <param name="symbol"></param>
        public void CallHandler(string symbol) {
            if (IsHandled(symbol)) _handlers[symbol].Invoke();
        }

        /// <summary>
        /// Update処理
        /// </summary>
        public new void Update() {
            base.Update();
            if (InputHandler.OnUp(HandleType.RightClick) || InputHandler.OnUp(HandleType.Back) || SceneBattle.BackButton)
            {
                if (Active)
                {
                    if (IsCancelEnabled())
                    {
                        if (SceneBattle.IsUpdateSuppress) return;
                        SceneBattle.IsUpdateSuppress = true;
                        ProcessCancel();
                    }
                }
            }
        }

        /// <summary>
        /// キャンセルが可能か
        /// </summary>
        /// <returns></returns>
        public bool IsCancelEnabled() {
            return IsHandled("cancel");
        }

        /// <summary>
        /// OKの処理
        /// </summary>
        public virtual void ProcessOk() {
            Deactivate();
            CallOkHandler();
        }

        /// <summary>
        /// OKのハンドラを呼ぶ
        /// </summary>
        public virtual void CallOkHandler() {
            CallHandler("ok");
        }

        /// <summary>
        /// キャンセルを処理
        /// </summary>
        public void ProcessCancel() {
            SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_SE,
                DataManager.Self().GetSystemDataModel().soundSetting.cancel);
            SoundManager.Self().PlaySe();
            Deactivate();
            CallCancelHandler();
        }

        /// <summary>
        /// キャンセルのハンドラを呼ぶ
        /// </summary>
        public void CallCancelHandler() {
            CallHandler("cancel");
        }

        /// <summary>
        /// ヘルプのアップデートを呼ぶ
        /// </summary>
        public void CallUpdateHelp() {
            if (Active && _helpWindow) UpdateHelp();
        }

        /// <summary>
        /// ヘルプウィンドウをアップデート
        /// </summary>
        public virtual void UpdateHelp() {
            _helpWindow.Clear();
        }

        /// <summary>
        /// 指定項目をヘルプウィンドウに表示
        /// </summary>
        /// <param name="item"></param>
        public void SetHelpWindowItem(GameItem item) {
            if (_helpWindow) _helpWindow.SetItem(item);
        }

        /// <summary>
        /// 全項目を描画
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public virtual void DrawAllItems() {
#else
        public virtual async Task DrawAllItems() {
#endif
            if (selectors != null && selectors.Any()) selectors.ForEach(selector => selector?.canvas?.SetActive(false));

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            if (MaxItems() == 0) ClearItem();
#else
            if (await MaxItems() == 0) ClearItem();
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            for (var i = 0; i < MaxItems(); i++)
            {
                DrawItem(i);
            }
#else
            for (var i = 0; i < await MaxItems(); i++)
            {
                await DrawItem(i);
            }
#endif
        }

        /// <summary>
        /// 指定番号の項目を描画
        /// overrideして利用する
        /// </summary>
        /// <param name="index"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public virtual void DrawItem(int index) {
        }
#else
        public virtual async Task DrawItem(int index) {
            await UniteTask.Delay(0);
        }
#endif

        /// <summary>
        /// 指定した番号の項目を削除
        /// </summary>
        public virtual void ClearItem() {
            if (selectors != null)
                selectors.ForEach(selector => selector?.gameObject.SetActive(false));
        }

        /// <summary>
        /// コンテンツの再描画
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public virtual void Refresh() {
            DrawAllItems();
        }
#else
        public virtual async Task Refresh() {
            await DrawAllItems();
        }
#endif

        /// <summary>
        /// ボタン選択時処理
        /// </summary>
        /// <param name="index"></param>
        public void OnClickSelection(int index) {
            if (_index == index)
            {
                ProcessOk();
                return;
            }

            Select(index);
        }
        
        /// <summary>
        /// アイコン設定
        /// </summary>
        /// <param name="iconName"></param>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        protected Sprite GetItemImage(string iconName) {
#else
        protected async Task<Sprite> GetItemImage(string iconName) {
#endif
            var iconSetTexture =
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Texture2D>(
#else
                await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Texture2D>(
#endif
                    "Assets/RPGMaker/Storage/Images/System/IconSet/" + iconName + ".png");

            var iconTexture = iconSetTexture;
            if (iconTexture == null)
            {
                return null;
            }
            
            var sprite = ImageUtility.SpriteCreate(
                iconTexture,
                new Rect(0, 0, iconTexture.width, iconTexture.height),
                new Vector2(0.5f, 0.5f)
            );
            
            var aspect = ImageManager.FixAspect( new Vector2(66f,66f), new Vector2(iconTexture.width, iconTexture.height));
            var aspectRatio = _iconImage.GetComponent<AspectRatioFitter>();
            if (aspectRatio == null)
            {
                aspectRatio = _iconImage.gameObject.AddComponent<AspectRatioFitter>();
                aspectRatio.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            }
            aspectRatio.aspectRatio = aspect;
            
            return sprite;
        }
    }
}