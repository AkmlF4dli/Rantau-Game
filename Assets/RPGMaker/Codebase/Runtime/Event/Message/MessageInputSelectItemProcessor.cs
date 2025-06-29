using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement;
using RPGMaker.Codebase.Runtime.Common;
using RPGMaker.Codebase.Runtime.Common.Component.Hud;
using UnityEngine;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Event.Message
{
    /// <summary>
    /// [メッセージ]-[アイテム選択の処理]
    /// </summary>
    public class MessageInputSelectItemProcessor : AbstractEventCommandProcessor
    {
        //アイテム選択オブジェクトの保持
        private GameObject _itemSelectChanvas;

        //アイテム選択プレハブのPath
        private readonly string PrefabPath =
            "Assets/RPGMaker/Codebase/Runtime/Map/Asset/Prefab/MessageInputSelectItem.prefab";

        private EventDataModel.EventCommand _command;

        protected override void Process(string eventID, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventID, command);
        }

        private async void ProcessAsync(string eventID, EventDataModel.EventCommand command) {
#endif
            //アイテム選択の表示
            _command = command;

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var loadPrefab = UnityEditorWrapper.PrefabUtilityWrapper.LoadPrefabContents(PrefabPath);
#else
            var loadPrefab = await UnityEditorWrapper.PrefabUtilityWrapper.LoadPrefabContents(PrefabPath);
#endif
            //既にショップが存在する場合新たに作成を行わない
            _itemSelectChanvas =
                Object.Instantiate(loadPrefab);
            UnityEditorWrapper.PrefabUtilityWrapper.UnloadPrefabContents(loadPrefab);

            //シーンから「MessageInputSelectItem」を取得して「ItemWindow」を取得して保持する
            var itemSelect = _itemSelectChanvas.GetComponent<ItemWindow>();

            //アイテム選択終了時処理
            itemSelect.CloseItemWindow(ProcessEndAction);

            //アイテム選択Windowの表示
            itemSelect.SetItemType(int.Parse(command.parameters[1]) + 1);
        }

        private void ProcessEndAction() {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessEndActionAsync();
        }
        private async void ProcessEndActionAsync() {
#endif
            //選択したアイテムのシリアルNoを取得
            var itemSelect = _itemSelectChanvas.GetComponent<ItemWindow>();
            int serialNumber = itemSelect.GetSelectedItem();

            // index取得
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var vari = new DatabaseManagementService().LoadFlags().variables;
#else
            var vari = (await new DatabaseManagementService().LoadFlags()).variables;
#endif

            var index1 = 0;
            for (var i = 0; i < vari.Count; i++)
                if (vari[i].id == _command.parameters[0])
                {
                    index1 = i;
                    break;
                }

            DataManager.Self().GetRuntimeSaveDataModel().variables.data[index1] = serialNumber.ToString();

            //不要となったオブジェクトを破棄
            Object.Destroy(_itemSelectChanvas);

            //イベント処理継続
            SendBackToLauncher.Invoke();
        }
    }
}