using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Map;
using RPGMaker.Codebase.Runtime.Shop;
using System.Threading.Tasks;
using UnityEngine;

namespace RPGMaker.Codebase.Runtime.Event.Screen
{
    /// <summary>
    /// [シーン制御]-[ショップの処理]
    /// </summary>
    public class OpenShopProcessor : AbstractEventCommandProcessor
    {
        //ショップオブジェクトの保持
        private GameObject _itemShopChanvas;

        //ショッププレハブのPath
        private readonly string PrefabPath =
            "Assets/RPGMaker/Codebase/Runtime/Map/Shop/Asset/Prefab/ItemShopCanvas.prefab";

        protected override void Process(string eventID, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventID, command);
        }

        private async void ProcessAsync(string eventID, EventDataModel.EventCommand command) {
#endif
            //ショップの仕入れ
            MapEventExecutionController.Instance.GetEventOnMap(eventID).ShopEvent();

            //ショップの表示
            //既にショップが存在する場合新たに作成を行わない
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var loadPrefab = UnityEditorWrapper.PrefabUtilityWrapper.LoadPrefabContents(PrefabPath);
#else
            var loadPrefab = await UnityEditorWrapper.PrefabUtilityWrapper.LoadPrefabContents(PrefabPath);
#endif
            _itemShopChanvas = Object.Instantiate(loadPrefab);
            UnityEditorWrapper.PrefabUtilityWrapper.UnloadPrefabContents(loadPrefab);

            //シーンから「ItemShopCanvas」を取得して「ItemShop」を取得して保持する
            var itemShop = _itemShopChanvas.GetComponent<ItemShop>();

            //ショップへ売るアイテムの受け渡し
            itemShop.ShopBuyItem = MapEventExecutionController.Instance.GetEventOnMap(eventID).ShopItemList();

            //ショップイベントの閉店処理の設定
            itemShop.BackOperationButton.onClick.AddListener(ProcessEndAction);
            //ボタンからショップの終了
            itemShop.CloseShop(ProcessEndAction);
        }

        private void ProcessEndAction() {
            var itemShop = _itemShopChanvas.GetComponent<ItemShop>();
            itemShop.DestroyObject();
            itemShop.BackOperationButton.onClick.RemoveListener(ProcessEndAction);

            _itemShopChanvas.SetActive(false);
            Object.Destroy(_itemShopChanvas);
            SendBackToLauncher.Invoke();
        }
    }
}