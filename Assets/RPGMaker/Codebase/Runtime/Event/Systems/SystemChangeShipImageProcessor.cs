using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Common;
using RPGMaker.Codebase.Runtime.Map;
using RPGMaker.Codebase.Runtime.Map.Component.Character;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Event.Systems
{
    /// <summary>
    /// [システム]-[乗り物の画像変更]
    /// </summary>
    public class SystemChangeShipImageProcessor : AbstractEventCommandProcessor
    {
        protected override void Process(string eventID, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventID, command);
        }

        private async void ProcessAsync(string eventID, EventDataModel.EventCommand command) {
#endif
            if (string.IsNullOrEmpty(command.parameters[0]))
            {
                ProcessEndAction();
                return;
            }

            //セーブデータへの反映
            var runtimeVehicleDataModels = DataManager.Self().GetRuntimeSaveDataModel().runtimePlayerDataModel.map.vehicles;
            for (int i = 0; i < runtimeVehicleDataModels.Count; i++)
            {
                if (runtimeVehicleDataModels[i].id == command.parameters[0])
                {
                    runtimeVehicleDataModels[i].assetId = command.parameters[1];
                    break;
                }
            }

            //画像の更新更新
            var vehicleObject = MapManager.GetVehicleGameObject(command.parameters[0]);
            if (vehicleObject != null)
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                vehicleObject.GetComponent<VehicleOnMap>().ChangeAsset(command.parameters[1]);
#else
                await vehicleObject.GetComponent<VehicleOnMap>().ChangeAsset(command.parameters[1]);
#endif

            //次へ
            ProcessEndAction();
        }

        private void ProcessEndAction() {
            SendBackToLauncher.Invoke();
        }
    }
}