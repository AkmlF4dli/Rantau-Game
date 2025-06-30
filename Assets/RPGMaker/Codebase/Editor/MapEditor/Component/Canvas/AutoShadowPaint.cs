using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Map;
using RPGMaker.Codebase.CoreSystem.Service.MapManagement;
using RPGMaker.Codebase.CoreSystem.Service.MapManagement.Repository;
using RPGMaker.Codebase.Editor.Common.View;
using RPGMaker.Codebase.Editor.Hierarchy.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using static RPGMaker.Codebase.Editor.DatabaseEditor.ModalWindow.AddonParameterEditArray2ModalWindow;

namespace RPGMaker.Codebase.Editor.MapEditor.Component.Canvas
{
    /// <summary>
    /// マップのオート影つけの処理を行う
    /// </summary>
    public class AutoShadowPaint
    {
        private MapDataModel _mapDataModel;
        private MapDataModel.Layer _layer;
        private MapDataModel.Layer _shadowLayer;

        private Dictionary<string, bool> _tileIdAutoShadowDic = new Dictionary<string, bool>();
        private HashSet<Vector3Int> _cellPosList = new HashSet<Vector3Int>();
        private Dictionary<Vector3Int, bool> _tilePosAutoShadowDic = new Dictionary<Vector3Int, bool>();

        public AutoShadowPaint(MapDataModel mapDataModel, MapDataModel.Layer layer, MapDataModel.Layer shadowLayer) {
            _mapDataModel = mapDataModel;
            _layer = layer;
            _shadowLayer = shadowLayer;
            var mapManagementService = Editor.Hierarchy.Hierarchy.mapManagementService;
            var tileGroupDataModels = mapManagementService.LoadTileGroups();
            foreach (var tileGroup in tileGroupDataModels)
            {
                foreach (var tileDataModelInfo in tileGroup.tileDataModels) {
                    var tileId = tileDataModelInfo.id;
                    if (_tileIdAutoShadowDic.ContainsKey(tileId))
                    {
                        if (!_tileIdAutoShadowDic[tileId] && tileGroup.autoShadow)
                        {
                            _tileIdAutoShadowDic[tileId] = true;
                        }
                    } else
                    {
                        _tileIdAutoShadowDic.Add(tileId, tileGroup.autoShadow);
                    }
                }
            }
        }

        /// <summary>
        /// オート影つけ範囲をクリアする。
        /// </summary>
        public void Clear() {
            _cellPosList.Clear();
            _tilePosAutoShadowDic.Clear();
        }

        /// <summary>
        /// オート影つけを行うセルを追加する。
        /// </summary>
        public void AddCell(Vector3Int cellPos) {
            if (cellPos.x >= 0 && cellPos.x < _mapDataModel.width && cellPos.y <= 0 && cellPos.y > -_mapDataModel.height)
            {
                AddCellSub(cellPos);
            }
            for (int x = 0; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    AddCellSub(cellPos + new Vector3Int(x, y, cellPos.z));
                }
            }
        }
        void AddCellSub(Vector3Int cellPos) {
            if (cellPos.x >= 0 && cellPos.x < _mapDataModel.width && cellPos.y <= 0 && cellPos.y > -_mapDataModel.height)
            {
                _cellPosList.Add(cellPos);
            }
        }

        public void UpdateShadow() {
            var shadowTile = _shadowLayer.tilesOnPalette[0].TileDataModel;
            var shadowTilemap = _shadowLayer.tilemap;
            var addList = new List<Vector3Int>() {
                new Vector3Int(0, 0, 0),
                new Vector3Int(1, 0, 0),
                new Vector3Int(0, 1, 0),
                new Vector3Int(1, 1, 0),
            };

            //影をクリアする。
            foreach (var cp in _cellPosList)
            {
                var cp2 = cp * 2;
                foreach (var add in addList){
                    shadowTilemap.SetTile(cp2 + add, null);
                }
            }

            //オートシャドータイルの配置状況を見て、影を付ける。
            var upLeft = new Vector3Int(-1, 1);
            var left = new Vector3Int(-1, 0);
            var up = new Vector3Int(0, 1);
            foreach (var cp in _cellPosList)
            {
                if (IsAutoShadowTile(cp))
                {
                    continue;
                }
                if (!IsAutoShadowTile(cp + upLeft) || !IsAutoShadowTile(cp + left)){
                    continue;
                }
                var cp2 = cp * 2;
                shadowTilemap.SetTile(cp2, shadowTile);
                shadowTilemap.SetTile(cp2 + up, shadowTile);
            }
        }

        bool IsAutoShadowTile(Vector3Int cellPos) {
            if (cellPos.x < 0 || cellPos.x >= _mapDataModel.width || cellPos.y > 0 || cellPos.y <= -_mapDataModel.height)
            {
                return false;
            }
            if (_tilePosAutoShadowDic.ContainsKey(cellPos))
            {
                return _tilePosAutoShadowDic[cellPos];
            }
            var tile = _layer.GetTileDataModelByPosition(new Vector2(cellPos.x, cellPos.y));
            if (tile == null)
            {
                _tilePosAutoShadowDic.Add(cellPos, false);
                return false;
            }
            var isAutoShadow = ((_tileIdAutoShadowDic.ContainsKey(tile.id) && _tileIdAutoShadowDic[tile.id]) && (tile.type == TileDataModel.Type.AutoTileA || tile.type == TileDataModel.Type.AutoTileB));
            _tilePosAutoShadowDic.Add(cellPos, isAutoShadow);
            return isAutoShadow;
        }

    }
}