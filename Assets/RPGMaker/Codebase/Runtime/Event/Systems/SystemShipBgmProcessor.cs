using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement;
using RPGMaker.Codebase.Runtime.Common;
using System.Collections.Generic;
using static RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime.RuntimeSystemConfigDataModel;
using UnityEngine;

namespace RPGMaker.Codebase.Runtime.Event.Systems
{
    /// <summary>
    /// [システム]-[乗り物BGM変更]
    /// </summary>
    public class SystemShipBgmProcessor : AbstractEventCommandProcessor
    {
        protected override void Process(string eventID, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventID, command);
        }

        private async void ProcessAsync(string eventID, EventDataModel.EventCommand command) {
#endif
            var databaseManagementService = new DatabaseManagementService();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var vehiclesDataModels = databaseManagementService.LoadCharacterVehicles();
#else
            var vehiclesDataModels = await databaseManagementService.LoadCharacterVehicles();
#endif

            //IDから乗り物を探す
            VehicleSound sound = new VehicleSound();
            for (var i = 0; i < vehiclesDataModels.Count; i++)
                if (vehiclesDataModels[i].id == command.parameters[4])
                {
                    //IDの一致した乗り物のデータの変更
                    var runtimeSystemConfig = DataManager.Self().GetRuntimeSaveDataModel().runtimeSystemConfig;
                    if (runtimeSystemConfig.vehicleSound == null)
                        runtimeSystemConfig.vehicleSound = new List<VehicleSound>();

                    bool flg = false;
                    for (int j = 0; j < runtimeSystemConfig.vehicleSound.Count; j++)
                    {
                        if (runtimeSystemConfig.vehicleSound[j].id == vehiclesDataModels[i].id)
                        {
                            //U339 vehicleSound[i] を j へ
                            runtimeSystemConfig.vehicleSound[j].sound.name = command.parameters[0];
                            runtimeSystemConfig.vehicleSound[j].sound.volume = int.Parse(command.parameters[1]);
                            runtimeSystemConfig.vehicleSound[j].sound.pitch = int.Parse(command.parameters[2]);
                            runtimeSystemConfig.vehicleSound[j].sound.pan = int.Parse(command.parameters[3]);
                            sound = runtimeSystemConfig.vehicleSound[j];
                            flg = true;
                            break;
                        }
                    }
                    if (!flg)
                    {
                        sound.id = vehiclesDataModels[i].id;
                        sound.sound = new Sound(command.parameters[0], int.Parse(command.parameters[3]), int.Parse(command.parameters[2]), int.Parse(command.parameters[1]));
                        runtimeSystemConfig.vehicleSound.Add(sound);
                    }
                    break;
                }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            //データの保存
            databaseManagementService.SaveCharacterVehicles(vehiclesDataModels);
#endif
            //次へ
            ProcessEndAction();
        }

        private void ProcessEndAction() {
            SendBackToLauncher.Invoke();
        }
    }
}