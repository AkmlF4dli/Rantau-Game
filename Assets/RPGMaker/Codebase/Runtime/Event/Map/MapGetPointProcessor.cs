using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Map;
using RPGMaker.Codebase.CoreSystem.Service.EventManagement;
using RPGMaker.Codebase.Runtime.Common;
using RPGMaker.Codebase.Runtime.Map;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Event.Map
{
    /// <summary>
    /// [マップ]-[指定位置の情報取得]
    /// </summary>
    public class MapGetPoint : AbstractEventCommandProcessor
    {
        private static readonly Vector2Int invalidPos = new(int.MinValue, int.MinValue);

        protected override void Process(string eventID, EventDataModel.EventCommand command)
        {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventID, command);
        }

        private async void ProcessAsync(string eventID, EventDataModel.EventCommand command) {
#endif
            //0:変数リスト
            //1：[地形タグ][イベントID][タイルID][リージョンID]
            //2：[レイヤー1][レイヤー2][レイヤー3][レイヤー4]
            //3：場所
            //4：x
            //5：y

            string value = null;

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            Vector2Int pos = GetPos(eventID, command.parameters);
#else
            Vector2Int pos = await GetPos(eventID, command.parameters);
#endif
            if (pos != invalidPos)
            {
                switch (command.parameters[1])
                {
                    // 地形タグ
                    case "0":
                    {
                            var layerType = int.Parse(command.parameters[2]) switch
                            {
                                0 => MapDataModel.Layer.LayerType.A,
                                1 => MapDataModel.Layer.LayerType.B,
                                2 => MapDataModel.Layer.LayerType.C,
                                3 => MapDataModel.Layer.LayerType.D,
                                _ => throw new System.ArgumentOutOfRangeException()
                            };

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                            var layerTilemap = MapManager.GetTileMapForRuntime(layerType);
#else
                            var layerTilemap = await MapManager.GetTileMapForRuntime(layerType);
#endif

                            if (pos.y > 0)
                            {
                                pos.y = -pos.y;
                            }

                            var tileDataModel = layerTilemap.GetTile<TileDataModel>(new Vector3Int(pos.x, pos.y, 0));
                            value = tileDataModel?.terrainTagValue.ToString();
                            break;
                    }

                    //イベントID
                    case "1":
                    {
                        var currentMapId = MapManager.CurrentMapDataModel.id;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        var currentMapEventMapDataModels = new EventManagementService().LoadEventMap().
#else
                        var currentMapEventMapDataModels = (await new EventManagementService().LoadEventMap()).
#endif
                            Where(eventMapDataModel => eventMapDataModel.mapId == currentMapId);
                        foreach (var eventMapDataModel in currentMapEventMapDataModels)
                        {
                            if (eventMapDataModel.x == pos.x &&
                                System.Math.Abs(eventMapDataModel.y) == System.Math.Abs(pos.y))
                            {
                                value = eventMapDataModel.SerialNumberString;
                                break;
                            }
                        }

                        break;
                    }

                    //タイルID
                    case "2":
                    {
                        var layerType = int.Parse(command.parameters[2]) switch
                        {
                            0 => MapDataModel.Layer.LayerType.A,
                            1 => MapDataModel.Layer.LayerType.B,
                            2 => MapDataModel.Layer.LayerType.C,
                            3 => MapDataModel.Layer.LayerType.D,
                            _ => throw new System.ArgumentOutOfRangeException()
                        };

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        var layerTilemap = MapManager.GetTileMapForRuntime(layerType);
#else
                        var layerTilemap = await MapManager.GetTileMapForRuntime(layerType);
#endif

                        if (pos.y > 0)
                        {
                            pos.y = -pos.y;
                        }

                        var tileDataModel = layerTilemap.GetTile<TileDataModel>(new Vector3Int(pos.x, pos.y, 0));
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        value = tileDataModel?.SerialNumberString;
#else
                        value = await tileDataModel?.SerialNumberString();
#endif
                        break;
                    }

                    //リージョンID
                    case "3":
                    {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        var regionLayer = RegionLayerForRuntime;
#else
                        var regionLayer = await RegionLayerForRuntime();
#endif
                        value = regionLayer.GetTileDataModelWithYPosCorrection(pos)?.regionId.ToString();
                        value ??= "0";
                        break;
                    }
                }
            }

            DebugUtil.Log($"value={value}");

            // 値を変数に代入。
            if (value != null)
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var variableIndex = GetVariableIndex(command.parameters[0]);
#else
                var variableIndex = await GetVariableIndex(command.parameters[0]);
#endif
                if (variableIndex >= 0)
                {
                    DataManager.Self().GetRuntimeSaveDataModel().variables.data[variableIndex] = value;
                }
            }

            SendBackToLauncher.Invoke();
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private static MapDataModel.Layer RegionLayerForRuntime =>
            MapManager.CurrentMapDataModel.MapPrefabManagerForRuntime.layers[(int)MapDataModel.Layer.LayerType.Region];
#else
        private static async Task<MapDataModel.Layer> RegionLayerForRuntime() {
            return (await MapManager.CurrentMapDataModel.MapPrefabManagerForRuntime.layers())[(int) MapDataModel.Layer.LayerType.Region];
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private static Vector2Int GetPos(string eventId, List<string> parameters)
#else
        private static async Task<Vector2Int> GetPos(string eventId, List<string> parameters)
#endif
        {
            return parameters[3] switch
            {
                // 直接指定 (即値)。
                "0" => new Vector2Int(int.Parse(parameters[4]), int.Parse(parameters[5])),
                // 変数で指定。
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                "1" => GetPositionByVariables(parameters[4], parameters[5]),
#else
                "1" => await GetPositionByVariables(parameters[4], parameters[5]),
#endif
                // キャラクターで指定。
                "2" => new Commons.TargetCharacter(
                    parameters.Count >= 7 ? (parameters[6] == "-1" ? eventId : parameters[6]) : (parameters[4] == "-1" ? eventId : parameters[4])/*過去の間違った値格納場所*/, eventId).
                        GetTilePositionOnTile(),
                // 他。
                _ => invalidPos
            };
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private static Vector2Int GetPositionByVariables(string xVariableId, string yVariableId)
#else
        private static async Task<Vector2Int> GetPositionByVariables(string xVariableId, string yVariableId)
#endif
        {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            int xVariableIndex = GetVariableIndex(xVariableId);
            int yVariableIndex = GetVariableIndex(yVariableId);
#else
            int xVariableIndex = await GetVariableIndex(xVariableId);
            int yVariableIndex = await GetVariableIndex(yVariableId);
#endif

            if (xVariableIndex < 0 || !int.TryParse(GetVariableValue(xVariableIndex), out int x) ||
                yVariableIndex < 0 || !int.TryParse(GetVariableValue(yVariableIndex), out int y))
            {
                return invalidPos;
            }

            return new Vector2Int(x, y);
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private static int GetVariableIndex(string variableId)
#else
        private static async Task<int> GetVariableIndex(string variableId)
#endif
        {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            return new CoreSystem.Service.DatabaseManagement.DatabaseManagementService().LoadFlags().
#else
            return (await new CoreSystem.Service.DatabaseManagement.DatabaseManagementService().LoadFlags()).
#endif
                variables.FindIndex(v => v.id == variableId);
        }

        private static string GetVariableValue(int variableIndex)
        {
            return DataManager.Self().GetRuntimeSaveDataModel().variables.data[variableIndex];
        }
    }
}