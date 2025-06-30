using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Map;

namespace RPGMaker.Codebase.Runtime.Event.Character
{
    /// <summary>
    /// [キャラクター]-[乗り物の乗降]
    /// </summary>
    public class MoveRideShipProcessor : AbstractEventCommandProcessor
    {
        protected override void Process(string eventID, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventID, command);
        }

        private async void ProcessAsync(string eventID, EventDataModel.EventCommand command) {
#endif
            if (MapManager.GetRideVehicle() == null)
                MapManager.TryToRideFromThePlayerToVehicle();
            else
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                MapManager.TryToGetOffFromTheVehicleToPlayer(true);
#else
                await MapManager.TryToGetOffFromTheVehicleToPlayer(true);
#endif
            ProcessEndAction();
        }

        private void ProcessEndAction() {
            SendBackToLauncher.Invoke();
        }
    }
}