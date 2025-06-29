using RPGMaker.Codebase.CoreSystem.Helper;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Shop
{
    /// <summary>
    ///     取引決済時にアイテムの情報を表示する項目
    /// </summary>
    public class TradeItemContent : MonoBehaviour
    {
        [SerializeField] private Image _iconImage = null;
        [SerializeField] private Text  _nameText  = null;
        [SerializeField] private Text  _numText   = null;
        [SerializeField] private Text  _priseText = null;


        /// <summary>
        ///     表示するアイテムに関する情報をすべて設定する
        /// </summary>
        /// <param name="iconId">アイテムのアイコンID</param>
        /// <param name="itemName">アイテム名</param>
        /// <param name="num">アイテムの個数</param>
        /// <param name="price">アイテムの価格</param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SetTradeItemInfo(string iconId, string itemName, int num, int price) {
#else
        public async Task SetTradeItemInfo(string iconId, string itemName, int num, int price) {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _iconImage.sprite = GetItemImage(iconId);
#else
            _iconImage.sprite = await GetItemImage(iconId);
#endif
            _nameText.text = itemName;
            _numText.text = num.ToString();
            _priseText.text = price.ToString();
        }

        /// <summary>
        ///     取引に関連する数量の表示を設定し直す
        /// </summary>
        /// <param name="num">アイテムの個数</param>
        /// <param name="price">アイテムの価格</param>
        public void SetTradeNum(int num, int price) {
            _numText.text = num.ToString();
            _priseText.text = price.ToString();
        }

        /// <summary>
        ///     アイコンの設定
        /// </summary>
        /// <param name="iconId">アイコン用画像のID</param>
        /// <returns>生成されたスプライト</returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public Sprite GetItemImage(string iconId) {
#else
        public async Task<Sprite> GetItemImage(string iconId) {
#endif
            var iconSetTexture =
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Texture2D>(
#else
                await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Texture2D>(
#endif
                    "Assets/RPGMaker/Storage/Images/System/IconSet/" + iconId + ".png");

            var sprite = ImageUtility.SpriteCreate(
                iconSetTexture,
                new Rect(0, 0, iconSetTexture.width, iconSetTexture.height),
                new Vector2(0.5f, 0.5f)
            );

            return sprite;
        }
    }
}