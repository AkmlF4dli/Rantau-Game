using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.Runtime.Battle.Objects;
using RPGMaker.Codebase.Runtime.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TextMP = TMPro.TextMeshProUGUI;

namespace RPGMaker.Codebase.Runtime.Battle.Window
{
    /// <summary>
    /// [アイテム]の選択ウィンドウ
    /// </summary>
    public class WindowItemList : WindowSelectable
    {
        /// <summary>
        /// アイテム総数（Refreshで初期化されるため保持する）
        /// </summary>
        private int _allItemCount;
        /// <summary>
        /// アイテムのGameObjectのオリジナル
        /// </summary>
        private GameObject _battleItemSelector;
        /// <summary>
        /// アイテムカテゴリ
        /// </summary>
        private string _category;
        /// <summary>
        /// アイテムの配列
        /// </summary>
        private List<GameItem> _data;
        /// <summary>
        /// アイテム総数
        /// </summary>
        private int _itemCount;

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
            //現状固定値
            _category = "item";
            _data = new List<GameItem>();

            _battleItemSelector = gameObject.transform.Find("WindowArea/Scroll View/Viewport/List/ItemSelector1").gameObject;
            _itemCount = 1; // 総数（複製元の分で初期値は1）
            _allItemCount = 2;
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
            return _data?.Count ?? 1;
        }

        /// <summary>
        /// 選択中のアイテムを返す
        /// </summary>
        /// <returns></returns>
        public GameItem Item() {
            return _data != null && Index() >= 0 ? _data.ElementAtOrDefault(Index()) : null;
        }

        /// <summary>
        /// 指定したアイテムが含まれるか
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public virtual bool Includes(GameItem item) {
#else
        public virtual async Task<bool> Includes(GameItem item) {
            await UniteTask.Delay(0);
#endif
            switch (_category)
            {
                case "item":
                    return item.IsItem() && item.ITypeId == 1;
                case "weapon":
                    return item.IsWeapon();
                case "armor":
                    return item.IsArmor();
                case "keyItem":
                    return item.IsItem() && item.ITypeId == 0;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 指定したアイテムが利用可能か
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public bool IsEnabled(GameItem item) {
            return DataManager.Self().GetGameParty().CanUse(item);
        }
#else
        public async Task<bool> IsEnabled(GameItem item) {
            return await DataManager.Self().GetGameParty().CanUse(item);
        }
#endif

        /// <summary>
        /// アイテムの配列( _data )を生成
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void MakeItemList() {
#else
        public async Task MakeItemList() {
#endif
            _data = new List<GameItem>();
            foreach (var item in DataManager.Self().GetGameParty().AllItems())
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                if (Includes(item))
#else
                if (await Includes(item))
#endif
                    _data.Add(item);
        }

        /// <summary>
        /// 前に選択した項目を選択
        /// </summary>
        public void SelectLast() {
        }

        /// <summary>
        /// 指定した[アイテム]の[名前]を指定位置に描画
        /// </summary>
        /// <param name="index"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public override void DrawItem(int index) {
#else
        public override async Task DrawItem(int index) {
            await UniteTask.Delay(0);
#endif
            MakeItem();

            if (selectors.Count <= index)
                return;

            if (_data.ElementAtOrDefault(index) != null)
            {
                var item = _data[index];

                var iconSetTexture =
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Texture2D>(
#else
                    await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Texture2D>(
#endif
                        "Assets/RPGMaker/Storage/Images/System/IconSet/" + item.Icon + ".png");
                _iconImage = selectors[index].gameObject.transform.Find("Icon").gameObject.GetComponent<Image>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var tex = GetItemImage(item.Icon);
#else
                var tex = await GetItemImage(item.Icon);
#endif
                _iconImage.gameObject.SetActive(tex != null);
                _iconImage.sprite = tex;

                //所持数設定
                var number = DataManager.Self().GetGameParty().NumItems(item);
                var itemValue = selectors[index].gameObject.transform.Find("ItemValue").gameObject
                    .GetComponent<TextMP>();
                itemValue.text = number.ToString();
                itemValue.color = selectors[index].label.color;

                selectors[index].SetUp(index, item.Name, Select, OnClickSelection);
            }
        }

        /// <summary>
        /// ヘルプウィンドウをアップデート
        /// </summary>
        public override void UpdateHelp() {
            SetHelpWindowItem(Item());
        }

        /// <summary>
        /// コンテンツの再描画
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public new void Refresh() {
#else
        public new async Task Refresh() {
#endif
            //古いアイテムを全て消す
            //_battleSkillSelector 以外のもの
            for (int i = 0; i < selectors.Count; i++)
            {
                if (selectors[i].gameObject != _battleItemSelector)
                {
                    DestroyImmediate(selectors[i].gameObject);
                    selectors.RemoveAt(i);
                    i--;
                }
                else
                {
                    //一旦グレーを解除
                    selectors[i].GetComponent<WindowButtonBase>().SetGray(false);
                }
                if (selectors.Count == 1)
                    break;
            }
            _itemCount = 1;

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            MakeItemList();
            DrawAllItems();
#else
            await MakeItemList();
            await DrawAllItems();
#endif
            SetActiveWindow();
            //ハイライト初期化
            for (int i = 0; i < selectors.Count; i++)
            {
                selectors[i].GetComponent<WindowButtonBase>().SetHighlight(i == 0);
            }
            //グレー表示への対応
            for (int i = 0; i < _data.Count; i++)
            {
                //_data[Index()]
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                bool flg = IsEnabled(_data[i]);
#else
                bool flg = await IsEnabled(_data[i]);
#endif
                if (!flg)
                {
                    selectors[i].GetComponent<WindowButtonBase>().SetGray(true);
                }
                else
                {
                    selectors[i].GetComponent<WindowButtonBase>().SetGray(false);
                }
            }
            //件数が1件しかない場合の処理
            if (_data == null || _data.Count == 0)
            {
                selectors[0].gameObject.transform.Find("Icon").gameObject.SetActive(false);
                selectors[0].SetUp(0, "", null, null);
                var itemValue = selectors[0].gameObject.transform.Find("ItemValue").gameObject.GetComponent<TextMP>();
                itemValue.text = "";
            }
            // ナビゲーションの設定
            SetNavigation();
            RefreshAft();
        }

        /// <summary>
        /// コンテンツの再描画の後、若干待ってから実行する処理
        /// </summary>
        private async void RefreshAft() {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            await Task.Delay(100);
#else
            await UniteTask.Delay(100);
#endif

            //ボタンの有効状態切り替え
            //最終選択したアイテムを選択状態とする
            GameItem item = DataManager.Self().GetGameParty().LastItem();

            var index = 0;
            if (DataManager.Self().GetRuntimeConfigDataModel()?.commandRemember != null && DataManager.Self().GetRuntimeConfigDataModel()?.commandRemember == 1)
            {
                for (int i = 0; i < _data.Count; i++)
                {
                    if (_data[i].ItemId == item.ItemId)
                    {
                        index = i;
                        break;
                    }
                }
            }

            bool flg = false;
            for (int i = 0; i < selectors.Count; i++)
            {
                selectors[i].GetComponent<WindowButtonBase>().SetEnabled(true);
                //元々フォーカスが当たっていた場所に、フォーカスしなおし
                if (i == index)
                {
                    flg = true;
                    selectors[i].GetComponent<Button>().Select();
                }
                else
                {
                    selectors[i].GetComponent<WindowButtonBase>().SetHighlight(false);
                }
            }

            if (!flg)
            {
                selectors[0].GetComponent<Button>().Select();
            }
        }

        /// <summary>
        /// 指定した番号の項目を削除
        /// </summary>
        public override void ClearItem() {
            base.ClearItem();
            _itemCount = 0;
        }

        /// <summary>
        /// アイテムの配列( _data )を生成
        /// </summary>
        private void MakeItem() {
            // スキル数を取得
            _allItemCount = _data.Count;

            // 現在のアイテム数が超えていれば
            while (_itemCount <= _allItemCount)
            {
                _itemCount++;
                var gameObject = Instantiate(_battleItemSelector, _battleItemSelector.transform.parent, false);
                gameObject.name = "ItemSelector" + _itemCount;
                selectors.Add(gameObject.GetComponent<Selector>());
            }
        }

        /// <summary>
        /// アイテム表示切替
        /// </summary>
        private void SetActiveWindow() {
            for (var i = 0; i < _itemCount; i++)
                if (i < _allItemCount)
                    selectors[i].gameObject.SetActive(true);
                else
                    selectors[i].gameObject.SetActive(false);

            // ボタンを選択状態にする
            if (_battleItemSelector != null)
                EventSystem.current.SetSelectedGameObject(_battleItemSelector);
        }

        /// <summary>
        /// ナビゲーションを設定する
        /// </summary>
        public void SetNavigation() {
            var selects = gameObject.transform.Find("WindowArea/Scroll View/Viewport/List").gameObject
                .GetComponentsInChildren<Selectable>();
            for (var i = 0; i < selects.Length; i++)
            {
                var nav = selects[i].navigation;
                nav.mode = Navigation.Mode.Explicit;

                nav.selectOnRight = selects[i < selects.Length - 1 ? i + 1 : i];
                nav.selectOnLeft = selects[i == 0 ? i : i - 1];
                nav.selectOnDown = selects[i + 2 < selects.Length ? i + 2 : i + 1 < selects.Length ? i + 1 : i];
                nav.selectOnUp = selects[i - 2 <= -1 ? i : i - 2];
                selects[i].navigation = nav;
            }
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
            base.Show();
#else
            await base.Show();
#endif

            //ボタンの有効状態切り替え
            //最終選択したアイテムを選択状態とする
            GameItem item = DataManager.Self().GetGameParty().LastItem();

            var index = 0;
            if (DataManager.Self().GetRuntimeConfigDataModel()?.commandRemember != null && DataManager.Self().GetRuntimeConfigDataModel()?.commandRemember == 1)
            {
                for (int i = 0; i < _data.Count; i++)
                {
                    if (_data[i].ItemId == item.ItemId)
                    {
                        index = i;
                        break;
                    }
                }
            }

            bool flg = false;
            for (int i = 0; i < selectors.Count; i++)
            {
                selectors[i].GetComponent<WindowButtonBase>().SetEnabled(true);
                //元々フォーカスが当たっていた場所に、フォーカスしなおし
                if (i == index)
                {
                    flg = true;
                    selectors[i].GetComponent<Button>().Select();
                }
                else
                {
                    selectors[i].GetComponent<WindowButtonBase>().SetHighlight(false);
                }
            }

            if (!flg)
            {
                selectors[0].GetComponent<Button>().Select();
            }
        }

        /// <summary>
        /// ウィンドウを非表示(閉じるわけではない)
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public override void Hide() {
#else
        public override async Task Hide() {
#endif
            //ボタンの有効状態切り替え
            for (int i = 0; i < selectors.Count; i++)
            {
                selectors[i].GetComponent<WindowButtonBase>().SetEnabled(false);
            }
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            base.Hide();
#else
            await base.Hide();
#endif
        }
    }
}