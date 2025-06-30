using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Map;
using RPGMaker.Codebase.CoreSystem.Knowledge.Enum;
using RPGMaker.Codebase.Runtime.Map.Component.Character;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Threading.Tasks;
using static RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Map.MapDataModel;

namespace RPGMaker.Codebase.Runtime.Map.Component.Map
{
    public class MapLoop : MonoBehaviour
    {
        private const int RANGE_WIDTH  = 28;
        private const int RANGE_HEIGHT = 18;

        private List<Tilemap>      _tilemaps = new List<Tilemap>();
        private MapDataModel       _mapDataModel;
        private List<EventOnMap>   _eventOnMaps   = new List<EventOnMap>();
        private List<VehicleOnMap> _vehiclesOnMap = new List<VehicleOnMap>();

        private int _xPosMin;
        private int _xPosMax;
        private int _yPosMin;
        private int _yPosMax;

        private bool _isHLoop;
        private bool _isRLoop;

        private int  _centerPosX;
        private int  _centerPosY;
        private bool _isLoad = false;

        public string vehicle = "";


        private Vector3 GizmoMapPos;
        private Vector3 GizmoMapSize;
        private List<Vector3> GizmoMapTilePosList = new List<Vector3>();
        private List<Vector3> GizmoMapTileSrcPosList = new List<Vector3>();
        private List<Vector3> GizmoMapTileSrcPosListShadow = new List<Vector3>();
        private List<Vector3> GizmoMapTilePosListShadow = new List<Vector3>();
        private List<Vector3> GizmoMapTileOrgPosList = new List<Vector3>();
        private List<Vector3> GizmoMapTileOrgPosListShadow = new List<Vector3>();

        public bool bHLoop
        {
            get
            {
                return _isHLoop;
            }
        }
        public bool bRLoop
        {
            get
            {
                return _isRLoop;
            }
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SetUp(
#else
        public async Task SetUp(
#endif
            GameObject mapPrefab,
            MapDataModel mapDataModel,
            List<VehicleOnMap> vehicleOnMaps,
            List<EventOnMap> eventOnMaps
        ) {
            _vehiclesOnMap = vehicleOnMaps;
            _eventOnMaps = eventOnMaps;
            var runtimeTilemaps = mapPrefab.GetComponentsInChildren<Tilemap>().ToList();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _tilemaps = mapDataModel.LayersForRuntime.ConvertAll(layer => layer.tilemap);
#else
            _tilemaps = (await mapDataModel.LayersForRuntime()).ConvertAll(layer => layer.tilemap);
#endif
            for (int i = 0; i < runtimeTilemaps.Count; i++)
            {
                if (runtimeTilemaps[i].name == "Layer A_Upper" ||
                    runtimeTilemaps[i].name == "Layer B_Upper" ||
                    runtimeTilemaps[i].name == "Layer C_Upper" ||
                    runtimeTilemaps[i].name == "Layer D_Upper")
                    _tilemaps.Add(runtimeTilemaps[i]);
            }
            _mapDataModel = mapDataModel;

            _centerPosX = _mapDataModel.width / 2;
            _centerPosY = -_mapDataModel.height / 2;

            if (_mapDataModel.scrollType == MapDataModel.MapScrollType.LoopBoth)
            {
                _isHLoop = true;
                _isRLoop = true;
            }
            else if (_mapDataModel.scrollType == MapDataModel.MapScrollType.LoopHorizontal)
            {
                _isHLoop = false;
                _isRLoop = true;
            }
            else if (_mapDataModel.scrollType == MapDataModel.MapScrollType.LoopVertical)
            {
                _isHLoop = true;
                _isRLoop = false;
            }
            else
            {
                _isHLoop = false;
                _isRLoop = false;
            }

            if (_isLoad)
            {
                LoopInit();
            }
        }

        private void Start() {
            if (_isHLoop == true || _isRLoop == true)
            {
                LoopInit();
            }
            _isLoad = true;
        }

        /// <summary>
        /// 初回のループ処理
        /// </summary>
        private async void LoopInit() {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            MapLoopTileSet();
#else
            await MapLoopTileSet();
#endif
            return;
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public Vector2 SetInitializePosition(Vector2 partyPosition) {
#else
        public async Task<Vector2> SetInitializePosition(Vector2 partyPosition) {
#endif
            //パーティメンバーの移動
            //パーティメンバーの初期表示位置は、マップサイズ内に収める
            if (_isRLoop)
            {
                if (partyPosition.x >= 0)
                {
                    partyPosition.x = partyPosition.x % _mapDataModel.width;
                }
                else
                {
                    partyPosition.x = _mapDataModel.width - (-1 * partyPosition.x % _mapDataModel.width);
                }
            }
            if (_isHLoop)
            {
                if (partyPosition.y <= 0)
                {
                    partyPosition.y = -1 * (-1 * partyPosition.y % _mapDataModel.height);
                }
                else
                {
                    partyPosition.y = -1 * (partyPosition.y % _mapDataModel.height + _mapDataModel.height);
                }
            }

            //イベント座標の移動
            var wmin = partyPosition.x - _centerPosX;
            var wmax = wmin + _mapDataModel.width;
            var hmin = partyPosition.y + _centerPosY;
            var hmax = hmin + (_mapDataModel.height - 1);

            for (int i = 0; i < _eventOnMaps.Count; i++)
            {
                if (MapEventExecutionController.Instance.GetCarryEventOnMap() == _eventOnMaps[i].gameObject)
                    continue;

                if (_isRLoop)
                {
                    if (_eventOnMaps[i].x_now < wmin)
                    {
                        while (_eventOnMaps[i].x_now < wmin)
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                            _eventOnMaps[i].SetToPositionOnTileLoop(new Vector2(_eventOnMaps[i].x_now + _mapDataModel.width, _eventOnMaps[i].y_now));
#else
                            await _eventOnMaps[i].SetToPositionOnTileLoop(new Vector2(_eventOnMaps[i].x_now + _mapDataModel.width, _eventOnMaps[i].y_now));
#endif
                    }
                    if (_eventOnMaps[i].x_now < 99999999 && _eventOnMaps[i].x_now > wmax)
                    {
                        while (_eventOnMaps[i].x_now > wmax)
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                            _eventOnMaps[i].SetToPositionOnTileLoop(new Vector2(_eventOnMaps[i].x_now - _mapDataModel.width, _eventOnMaps[i].y_now));
#else
                            await _eventOnMaps[i].SetToPositionOnTileLoop(new Vector2(_eventOnMaps[i].x_now - _mapDataModel.width, _eventOnMaps[i].y_now));
#endif
                    }
                }
                if (_isHLoop)
                {
                    if (_eventOnMaps[i].y_now < hmin)
                    {
                        while (_eventOnMaps[i].y_now < hmin)
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                            _eventOnMaps[i].SetToPositionOnTileLoop(new Vector2(_eventOnMaps[i].x_now, _eventOnMaps[i].y_now + _mapDataModel.height));
#else
                            await _eventOnMaps[i].SetToPositionOnTileLoop(new Vector2(_eventOnMaps[i].x_now, _eventOnMaps[i].y_now + _mapDataModel.height));
#endif
                    }
                    if (_eventOnMaps[i].y_now < 99999999 && _eventOnMaps[i].y_now > hmax)
                    {
                        while (_eventOnMaps[i].y_now > hmax)
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                            _eventOnMaps[i].SetToPositionOnTileLoop(new Vector2(_eventOnMaps[i].x_now, _eventOnMaps[i].y_now - _mapDataModel.height));
#else
                            await _eventOnMaps[i].SetToPositionOnTileLoop(new Vector2(_eventOnMaps[i].x_now, _eventOnMaps[i].y_now - _mapDataModel.height));
#endif
                    }
                }
            }

            return partyPosition;
        }

        //マップのループ処理
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void MapLoopDirection(CharacterOnMap _actorOnMap, CharacterMoveDirectionEnum direction) {
#else
        public async Task MapLoopDirection(CharacterOnMap _actorOnMap, CharacterMoveDirectionEnum direction) {
#endif
            //マップタイルをループ位置に配置する
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            MapLoopTileSet();
#else
            await MapLoopTileSet();
#endif
            switch (direction)
            {
                case CharacterMoveDirectionEnum.Up:
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    MapLoopUp(_actorOnMap);
#else
                    await MapLoopUp(_actorOnMap);
#endif
                    break;
                case CharacterMoveDirectionEnum.Down:
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    MapLoopDown(_actorOnMap);
#else
                    await MapLoopDown(_actorOnMap);
#endif
                    break;
                case CharacterMoveDirectionEnum.Left:
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    MapLoopLeft(_actorOnMap);
#else
                    await MapLoopLeft(_actorOnMap);
#endif
                    break;
                case CharacterMoveDirectionEnum.Right:
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    MapLoopRight(_actorOnMap);
#else
                    await MapLoopRight(_actorOnMap);
#endif
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        //マップのループ処理
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void MapLoopDirectionReset(CharacterOnMap _actorOnMap, CharacterMoveDirectionEnum direction) {
#else
        public async Task MapLoopDirectionReset(CharacterOnMap _actorOnMap, CharacterMoveDirectionEnum direction) {
#endif
            var pos = MapManager.OperatingCharacter.GetComponent<CharacterOnMap>().GetCurrentPositionOnLoopMapTile();
            // 画面に表示される範囲の設定
            int rangeWidth = RANGE_WIDTH;
            int rangeHeight = RANGE_HEIGHT;

            switch (direction)
            {
                case CharacterMoveDirectionEnum.Up:
                    _yPosMin = (int) pos.y + rangeHeight / 2;
                    _yPosMax = (int) pos.y - rangeHeight / 2;
                    break;
                case CharacterMoveDirectionEnum.Down:
                    _yPosMin = (int) pos.y + rangeHeight / 2;
                    _yPosMax = (int) pos.y - rangeHeight / 2;
                    break;
                case CharacterMoveDirectionEnum.Left:
                    _xPosMin = (int) pos.x - rangeWidth / 2;
                    _xPosMax = (int) pos.x + rangeWidth / 2;
                    break;
                case CharacterMoveDirectionEnum.Right:
                    _xPosMin = (int) pos.x - rangeWidth / 2;
                    _xPosMax = (int) pos.x + rangeWidth / 2;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
            // イベント位置を戻す
            //マップループ時のイベント位置の移動
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            MapLoopEventPos(_actorOnMap);
#else
            await MapLoopEventPos(_actorOnMap);
#endif
        }

        //マップのループ処理
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void MapLoopTileSet() {
#else
        public async Task MapLoopTileSet() {
#endif
            //キャラ位置
            var pos = MapManager.OperatingCharacter.GetComponent<CharacterOnMap>().GetCurrentPositionOnLoopMapTile();
            Vector2Int CharaPos = Vector2Int.FloorToInt(pos);

            // 画面に表示される範囲の設定
            Vector2Int ScreenSize = new Vector2Int(RANGE_WIDTH, RANGE_HEIGHT);
            Vector2Int MapSize = GetMapSize();

            //現在位置からマップの描画範囲計算
            Vector2Int StartPos = CharaPos - ScreenSize/2;
            Vector2Int EndPos   = CharaPos + ScreenSize/2;

            //ループなし
            if ((_isRLoop == false) && (_isHLoop == false))
            {
                return;
            }

            GizmoMapPos = pos;
            GizmoMapSize = new Vector3Int(ScreenSize.x, ScreenSize.y);
#if DEGBU_DRAW
            GizmoMapTilePosList.Clear();
            GizmoMapTileSrcPosList.Clear();

            GizmoMapTilePosListShadow.Clear();
            GizmoMapTileSrcPosListShadow.Clear();

            GizmoMapTileOrgPosList.Clear();
            GizmoMapTileOrgPosListShadow.Clear();
#endif
            for (int i = 0; i < _tilemaps.Count; i++)
            {
                if (_tilemaps[i] == null) continue;
                var tilemap = _tilemaps[i];

                if (i == (int) MapDataModel.Layer.LayerType.Shadow)
                {
                    Vector2Int StartShadowPos = (CharaPos*2) - ScreenSize;
                    Vector2Int EndShadowPos   = (CharaPos*2) + ScreenSize;

                    //影タイルは、通常用タイルの半部のグリットサイズ
                    for (var y = StartShadowPos.y; y < EndShadowPos.y; y++)
                    {
                        for (var x = StartShadowPos.x; x < EndShadowPos.x; x++)
                        {

                            if (_isRLoop == false)
                            {
                                //マップサイズ範囲外
                                if (x >= (MapSize.x*2) || x < 0) continue;
                            }
                            if (_isHLoop == false)
                            {
                                //マップサイズ範囲外
                                if (y <= -(MapSize.y*2) || y > 1) continue;
                            }

                            //ループ位置のチップを取得する
                            var MapMapTilePos = PositionOnTileToPositionOnLoopShadow(x, y);

                            var SrcTilePos = new Vector3Int(MapMapTilePos.x, MapMapTilePos.y);
                            //マップチップがあるか判定
                            //マップチップが元のところにあるか、マップタイルがループ先に配置済みの場合は無視する
                            var TileData = tilemap.GetTile(new Vector3Int(MapMapTilePos.x, MapMapTilePos.y, 0));

#if DEGBU_DRAW
                            if (TileData != null)
                            {
                                Vector3 SrcPos = new Vector3((float) SrcTilePos.x / 2.0f, (float)SrcTilePos.y / 2.0f);
                                SrcPos += new Vector3(0.25f, 0.25f, 0.0f);
                                if (GizmoMapTileSrcPosListShadow.Contains(SrcPos) == false)
                                {
                                    GizmoMapTileSrcPosListShadow.Add(SrcPos);
                                }
                            }
#endif
                            if (TileData == null ||
                               tilemap.GetTile(new Vector3Int(x, y, 0)) != null
                                )
                            {
                                continue;
                            }

#if DEGBU_DRAW
                            {
                                var TilePos = new Vector3Int(x, y);
                                Vector3 SrcPos2 = new Vector3((float) TilePos.x / 2.0f, (float) TilePos.y / 2.0f);
                                SrcPos2 += new Vector3(0.25f, 0.25f, 0.0f);
                                //SrcPos += new Vector3(0.5f, 0.5f, 0.0f);
                                if (GizmoMapTilePosListShadow.Contains(SrcPos2) == false)
                                {
                                    GizmoMapTilePosListShadow.Add(SrcPos2);

                                    GizmoMapTileOrgPosList.Add(new Vector3(x,y,0));
                                    GizmoMapTileOrgPosListShadow.Add(new Vector3(MapMapTilePos.x, MapMapTilePos.y,0));
                                }
                            }
#endif
                            if (TileData != null)
                            {
                                //ループ先のマップチップを配置する
                                tilemap.SetTile(new Vector3Int(x, y, 0), TileData);
                            }
                        }
                    }
                }
                else
                {
                    for (var y = StartPos.y; y <= EndPos.y; y++)
                    {
                        for (var x = StartPos.x; x <= EndPos.x; ++x)
                        {
                            if (_isRLoop == false)
                            {
                                //マップサイズ範囲外
                                if (x >= (MapSize.x) || x < 0) continue;
                            }
                            if (_isHLoop == false)
                            {
                                //マップサイズ範囲外
                                if (y <= -(MapSize.y) || y > 0) continue;
                            }

                            //ループ位置のチップを取得する
                            var MapMapTilePos = PositionOnTileToPositionOnLoop(x, y);

                            var SrcTilePos = new Vector3Int(MapMapTilePos.x, MapMapTilePos.y);
                            //マップチップがあるか判定
                            //マップチップが元のところにあるか、マップタイルがループ先に配置済みの場合は無視する
                            var TileData = tilemap.GetTile(new Vector3Int(MapMapTilePos.x, MapMapTilePos.y, 0));
                            if (TileData == null ||
                                 tilemap.GetTile(new Vector3Int(x, y, 0)) != null
                                )
                            {
                                continue;
                            }
                            if (TileData != null)
                            {
                                //ループ先のマップチップを配置する
                                tilemap.SetTile(new Vector3Int(x, y, 0), TileData);
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// </summary>
        void OnDrawGizmos()
        {
#if DEGBU_DRAW
            Gizmos.color = Color.blue;
            Vector3Int TileSize = new Vector3Int(1,1);
            foreach (var TilePos in GizmoMapTilePosList)
            {
                Gizmos.DrawWireCube(TilePos, TileSize);
            }

            Gizmos.color = Color.yellow;
            foreach (var TilePos in GizmoMapTileSrcPosList)
            {
                Gizmos.DrawWireCube(TilePos, TileSize);
            }
            //影系
            Gizmos.color = Color.white;
            Vector3 STileSize = new Vector3(0.5f, 0.5f);
            foreach (var TilePos in GizmoMapTileSrcPosListShadow)
            {
                Gizmos.DrawWireCube(TilePos, STileSize);
            }
            Gizmos.color = Color.green;
            foreach (var TilePos in GizmoMapTilePosListShadow)
            {
                Gizmos.DrawWireCube(TilePos, STileSize);
            }

            //
            Gizmos.color = Color.red;
            Vector3 Pos = Vector3.zero;
            Vector3 Size = Vector3.zero;

            Gizmos.DrawWireCube(GizmoMapPos, GizmoMapSize);


            //2.移動ルート算出
            //2.1.マウス座標取得
            Vector3 mouseWorldPos = MapManager.GetCamera().ScreenToWorldPoint(Input.mousePosition);
            //2.2.タイルの座標に変換
            Vector2 clickPoint = GetTilePositionByWorldPositionForRuntime(new Vector2(mouseWorldPos.x, mouseWorldPos.y));
            Gizmos.color = Color.red;
            Vector3 MousePoint = new Vector3(clickPoint.x /2  + 0.25f, clickPoint.y/2 + 0.25f, 0);
            //Vector3 MousePoint = new Vector3(clickPoint.x /2 , clickPoint.y/2);
            Gizmos.DrawWireCube(MousePoint, STileSize);

            Vector2Int MapSize = Vector2Int.zero;//GetMapSize();
            var playerPos = MapManager.OperatingCharacter.GetComponent<CharacterOnMap>().GetCurrentPositionOnLoopMapTile();
            // 画面に表示される範囲の設定
            Vector2Int ScreenSize = new Vector2Int(18, 10);//(RANGE_WIDTH, RANGE_HEIGHT);
            //現在位置からマップの描画範囲計算
            Vector2 StartPos = new Vector2(-(ScreenSize.x / 2), (ScreenSize.y / 2));
            Vector2 EndPos = new Vector2(MapSize.x + ScreenSize.x / 2, -(MapSize.y + ScreenSize.y / 2));

            Rect rect = new Rect();
            rect.min = new Vector2(-(ScreenSize.x / 2), -(MapSize.y + ScreenSize.y / 2));
            rect.max = new Vector2(MapSize.x + ScreenSize.x / 2, (ScreenSize.y / 2)); ;
            rect.position += playerPos;

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(playerPos, rect.size);
#endif
        }

#if DEGBU_DRAW
        /// <summary>
        /// ワールド座標をタイル座標に変換
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public Vector2 GetTilePositionByWorldPositionForRuntime(Vector2 worldPosition) {
             var tilePosition = _tilemaps[7].WorldToCell(worldPosition);
            return new Vector2(tilePosition.x, tilePosition.y);
        }
#endif

        void OnGUI() {
#if DEGBU_DRAW
            var pos = MapManager.OperatingCharacter.GetComponent<CharacterOnMap>().GetCurrentPositionOnLoopMapTile();
            Vector2Int CharaPos = Vector2Int.FloorToInt(pos);

            // 画面に表示される範囲の設定
            Vector2Int ScreenSize = new Vector2Int(RANGE_WIDTH, RANGE_HEIGHT);
            Vector2Int MapSize = GetMapSize();
            Vector2Int StartShadowPos = (CharaPos * 2) - ScreenSize;
            Vector2Int EndShadowPos = (CharaPos * 2) + ScreenSize;

            //2.移動ルート算出
            //2.1.マウス座標取得
            Vector3 mouseWorldPos = MapManager.GetCamera().ScreenToWorldPoint(Input.mousePosition);
            //2.2.タイルの座標に変換
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            Vector2 clickPoint = GetTilePositionByWorldPositionForRuntime(new Vector2(mouseWorldPos.x, mouseWorldPos.y));
#else
            Vector2 clickPoint = await MapManager.GetTilePositionByWorldPositionForRuntime(new Vector2(mouseWorldPos.x, mouseWorldPos.y));
#endif
            Vector3 MousePoint = new Vector3(clickPoint.x , clickPoint.y );

            var PosTile = PositionOnTileToPositionOnLoopShadow((int)MousePoint.x, (int) MousePoint.y);

            // プレイヤーの位置を左上に表示
            GUI.Box(new Rect(10, 10, 100, 23), "{" + StartShadowPos.x.ToString() + " , "+ StartShadowPos.y.ToString() + "}");
            GUI.Box(new Rect(10, 33, 100, 23), "{" + EndShadowPos.x.ToString() + " , " + EndShadowPos.y.ToString() + "}");
            GUI.Box(new Rect(10, 53, 100, 23), "{ Mouse " + MousePoint.x.ToString() + " , " + MousePoint.y.ToString() + "}");
            GUI.Box(new Rect(10, 73, 100, 23), "{ Tile  " + PosTile.x.ToString() + " , " + PosTile.y.ToString() + "}");
#endif
        }

        /// <summary>
        /// ループ時のマップチップの位置を取得する(標準チップ用)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private Vector2Int PositionOnTileToPositionOnLoop(int x, int y) 
        {
            Vector2Int MapSize = GetMapSize();
            int mapX = (x % MapSize.x + MapSize.x) % MapSize.x; // 横方向ループ
            int mapY = (y % -MapSize.y+ -MapSize.y) % -MapSize.y; // 縦方向ループ
            return new Vector2Int(mapX, mapY);
        }

        /// <summary>
        /// ループ時のマップチップの位置を取得する(影チップ用)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private Vector2Int PositionOnTileToPositionOnLoopShadow(int x, int y) 
        {
            Vector2Int MapSize = GetMapSize() * 2;
            int mapX = (x % MapSize.x + MapSize.x) % MapSize.x; // 横方向ループ
            int mapY = 0;

            if (y <= 1 && y > -(MapSize.y-1))
            {
                //マップ範囲内ならそのまま
                mapY = y;
            }else
            if (y <= -(MapSize.y-1))
            {
                //下ループ
                int SizeY = MapSize.y;
                if (y % (MapSize.y - 1) == 0)
                {
                    mapY = 1;
                }
                else
                {
                    mapY = (y % SizeY - SizeY) % SizeY;
                }
            }
            else
            if (y > 1 && y <= (MapSize.y -1))
            {
                //上ループ
                mapY = ((y % MapSize.y - MapSize.y) % MapSize.y); // 縦方向ループ
            }
            return new Vector2Int(mapX, mapY);
        }

        /// <summary>
        /// マップループ時のイベント位置の移動
        /// </summary>
        /// <param name="actorMap"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        void MapLoopEventPos(CharacterOnMap actorMap) 
#else
        private async Task MapLoopEventPos(CharacterOnMap actorMap)
#endif
        {
            if (_isHLoop == false  && _isRLoop == false) return;

            var Direction = actorMap.GetCurrentDirection();

            int Height = 0;
            int Width = 0;

            switch (Direction)
            {
                case CharacterMoveDirectionEnum.Up:
                     Height = -_mapDataModel.height;
                     break;
                case CharacterMoveDirectionEnum.Down:
                    Height = _mapDataModel.height;
                    break;
                case CharacterMoveDirectionEnum.Right:
                    Width = -_mapDataModel.width;
                    break;
                case CharacterMoveDirectionEnum.Left:
                    Width = _mapDataModel.width;
                    break;
            }

            for (int i = 0; i < _eventOnMaps.Count; i++)
            {
                int next = _eventOnMaps[i].y_next - _eventOnMaps[i].y_now;
                var Pos = new Vector2(_eventOnMaps[i].x_now +Width, _eventOnMaps[i].y_now + Height);
                _eventOnMaps[i].SetMoveToPositionOnTileLoop2(Pos);
                _eventOnMaps[i].y_next += next;
            }

            for (int i = 0; i < _vehiclesOnMap.Count; i++)
            {
                int next = _vehiclesOnMap[i].y_next - _vehiclesOnMap[i].y_now;
                var Pos = new Vector2(_vehiclesOnMap[i].x_now + Width, _vehiclesOnMap[i].y_now + Height);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _vehiclesOnMap[i].SetToPositionOnTile(Pos);
#else
                await _vehiclesOnMap[i].SetToPositionOnTile(Pos);
#endif
                _vehiclesOnMap[i].x_next += next;
            }
        }


        /// <summary>
        ///     上に移動。
        /// </summary>
        /// <param name="actorMap">アクター</param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void MapLoopUp(CharacterOnMap actorMap) {
#else
        public async Task MapLoopUp(CharacterOnMap actorMap) {
#endif
            if (!_isHLoop) return;

            var actorPosY = actorMap.transform.localPosition.y;

            var hmin = actorPosY + _centerPosY;
            var hmax = hmin + (_mapDataModel.height - 1);

            _yPosMax++;
            _yPosMin++;

            for (int i = 0; i < _eventOnMaps.Count; i++)
                if (_eventOnMaps[i].y_next < hmin)
                {
                    int next = _eventOnMaps[i].y_next - _eventOnMaps[i].y_now;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _eventOnMaps[i].SetToPositionOnTileLoop(new Vector2(_eventOnMaps[i].x_now, _eventOnMaps[i].y_now + _mapDataModel.height));
#else
                    await _eventOnMaps[i].SetToPositionOnTileLoop(new Vector2(_eventOnMaps[i].x_now, _eventOnMaps[i].y_now + _mapDataModel.height));
#endif
                    _eventOnMaps[i].y_next += next;
                }

            for (int i = 0; i < _vehiclesOnMap.Count; i++)
                if (_vehiclesOnMap[i].y_next < hmin)
                {
                    int next = _vehiclesOnMap[i].y_next - _vehiclesOnMap[i].y_now;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _vehiclesOnMap[i].SetToPositionOnTile(new Vector2(_vehiclesOnMap[i].x_now, _vehiclesOnMap[i].y_now + _mapDataModel.height));
#else
                    await _vehiclesOnMap[i].SetToPositionOnTile(new Vector2(_vehiclesOnMap[i].x_now, _vehiclesOnMap[i].y_now + _mapDataModel.height));
#endif
                    _vehiclesOnMap[i].x_next += next;
                }
        }

        /// <summary>
        ///     下に移動。
        /// </summary>
        /// <param name="actorMap">アクター</param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void MapLoopDown(CharacterOnMap actorMap) {
#else
        public async Task MapLoopDown(CharacterOnMap actorMap) {
#endif
            if (!_isHLoop) return;

            var actorPosY = actorMap.transform.localPosition.y;

            var hmin = actorPosY + _centerPosY;
            var hmax = hmin + (_mapDataModel.height - 1);

            _yPosMax--;
            _yPosMin--;
            
            for (int i = 0; i < _eventOnMaps.Count; i++)
                if (_eventOnMaps[i].y_next > hmax)
                {
                    int next = _eventOnMaps[i].y_next - _eventOnMaps[i].y_now;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _eventOnMaps[i].SetToPositionOnTileLoop(new Vector2(_eventOnMaps[i].x_now, _eventOnMaps[i].y_now - _mapDataModel.height));
#else
                    await _eventOnMaps[i].SetToPositionOnTileLoop(new Vector2(_eventOnMaps[i].x_now, _eventOnMaps[i].y_now - _mapDataModel.height));
#endif
                    _eventOnMaps[i].y_next += next;
                }

            for (int i = 0; i < _vehiclesOnMap.Count; i++)
                if (_vehiclesOnMap[i].y_next > hmax)
                {
                    int next = _vehiclesOnMap[i].y_next - _vehiclesOnMap[i].y_now;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _vehiclesOnMap[i].SetToPositionOnTile(new Vector2(_vehiclesOnMap[i].x_now, _vehiclesOnMap[i].y_now - _mapDataModel.height));
#else
                    await _vehiclesOnMap[i].SetToPositionOnTile(new Vector2(_vehiclesOnMap[i].x_now, _vehiclesOnMap[i].y_now - _mapDataModel.height));
#endif
                    _vehiclesOnMap[i].x_next += next;
                }
        }

        /// <summary>
        ///     右に移動。
        /// </summary>
        /// <param name="actorMap">アクター</param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void MapLoopRight(CharacterOnMap actorMap) {
#else
        public async Task MapLoopRight(CharacterOnMap actorMap) {
#endif
            if (!_isRLoop) return;

            var actorPosX = actorMap.transform.localPosition.x;

            var wmin = actorPosX - _centerPosX;
            var wmax = wmin + _mapDataModel.width;

            _xPosMax++;
            _xPosMin++;

            for(int i = 0; i < _eventOnMaps.Count; i++)
                if (_eventOnMaps[i].x_next < wmin)
                {
                    int next = _eventOnMaps[i].x_next - _eventOnMaps[i].x_now;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _eventOnMaps[i].SetToPositionOnTileLoop(new Vector2(_eventOnMaps[i].x_now + _mapDataModel.width, _eventOnMaps[i].y_now));
#else
                    await _eventOnMaps[i].SetToPositionOnTileLoop(new Vector2(_eventOnMaps[i].x_now + _mapDataModel.width, _eventOnMaps[i].y_now));
#endif
                    _eventOnMaps[i].x_next += next;
                }

            for (int i = 0; i < _vehiclesOnMap.Count; i++)
                if (_vehiclesOnMap[i].x_next < wmin)
                {
                    int next = _vehiclesOnMap[i].x_next - _vehiclesOnMap[i].x_now;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _vehiclesOnMap[i].SetToPositionOnTile(new Vector2(_vehiclesOnMap[i].x_now + _mapDataModel.width, _vehiclesOnMap[i].y_now));
#else
                    await _vehiclesOnMap[i].SetToPositionOnTile(new Vector2(_vehiclesOnMap[i].x_now + _mapDataModel.width, _vehiclesOnMap[i].y_now));
#endif
                    _vehiclesOnMap[i].x_next += next;
                }
        }

        /// <summary>
        ///     左に移動。
        /// </summary>
        /// <param name="actorMap">アクター</param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void MapLoopLeft(CharacterOnMap actorMap) {
#else
        public async Task MapLoopLeft(CharacterOnMap actorMap) {
#endif
            if (!_isRLoop) return;

            var actorPosX = actorMap.transform.localPosition.x;

            var wmin = actorPosX - _centerPosX;
            var wmax = wmin + _mapDataModel.width;

            _xPosMax--;
            _xPosMin--;
            
            for (int i = 0; i < _eventOnMaps.Count; i++)
                if (_eventOnMaps[i].x_next > wmax)
                {
                    int next = _eventOnMaps[i].x_next - _eventOnMaps[i].x_now;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _eventOnMaps[i].SetToPositionOnTileLoop(new Vector2(_eventOnMaps[i].x_now - _mapDataModel.width, _eventOnMaps[i].y_now));
#else
                    await _eventOnMaps[i].SetToPositionOnTileLoop(new Vector2(_eventOnMaps[i].x_now - _mapDataModel.width, _eventOnMaps[i].y_now));
#endif
                    _eventOnMaps[i].x_next += next;
                }

            for (int i = 0; i < _vehiclesOnMap.Count; i++)
                if (_vehiclesOnMap[i].x_next > wmax)
                {
                    int next = _vehiclesOnMap[i].x_next - _vehiclesOnMap[i].x_now;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _vehiclesOnMap[i].SetToPositionOnTile(new Vector2(_vehiclesOnMap[i].x_now - _mapDataModel.width, _vehiclesOnMap[i].y_now));
#else
                    await _vehiclesOnMap[i].SetToPositionOnTile(new Vector2(_vehiclesOnMap[i].x_now - _mapDataModel.width, _vehiclesOnMap[i].y_now));
#endif
                    _vehiclesOnMap[i].x_next += next;
                }
        }

        private void MoveTile(Tilemap tilemap, Vector3Int srcPos, Vector3Int dstPos) {
            var tile = tilemap.GetTile<TileBase>(srcPos);
            if (tile == null) return;

            tilemap.SetTile(dstPos, tile);
            tilemap.SetTile(srcPos, null);
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void MovePointLoopCharacter(Vector2 movePos, CharacterOnMap actorMap) {
#else
        public async Task MovePointLoopCharacter(Vector2 movePos, CharacterOnMap actorMap) {
#endif
            //現在のマップのループ状況に応じた座標変換
            Vector2 pos = MovePointLoop(movePos);

            //そこまで移動する
            if (_isRLoop)
            {
                if (pos.x < actorMap.x_next)
                {
                    while (pos.x < actorMap.x_next)
                    {
                        actorMap.x_now--;
                        actorMap.x_next--;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        MapManager.OperatingActor.SetToPositionOnTile(new Vector2(actorMap.x_next, actorMap.y_next));
                        MapLoopLeft(actorMap);
#else
                        await MapManager.OperatingActor.SetToPositionOnTile(new Vector2(actorMap.x_next, actorMap.y_next));
                        await MapLoopLeft(actorMap);
#endif
                    }
                }
                else if (pos.x > actorMap.x_next)
                {
                    while (pos.x > actorMap.x_next)
                    {
                        actorMap.x_now++;
                        actorMap.x_next++;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        MapManager.OperatingActor.SetToPositionOnTile(new Vector2(actorMap.x_next, actorMap.y_next));
                        MapLoopRight(actorMap);
#else
                        await MapManager.OperatingActor.SetToPositionOnTile(new Vector2(actorMap.x_next, actorMap.y_next));
                        await MapLoopRight(actorMap);
#endif
                    }
                }
            }
            if (_isHLoop)
            {
                if (pos.y < actorMap.y_next)
                {
                    while (pos.y < actorMap.y_next)
                    {
                        actorMap.y_now--;
                        actorMap.y_next--;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        MapManager.OperatingActor.SetToPositionOnTile(new Vector2(actorMap.x_next, actorMap.y_next));
                        MapLoopDown(actorMap);
#else
                        await MapManager.OperatingActor.SetToPositionOnTile(new Vector2(actorMap.x_next, actorMap.y_next));
                        await MapLoopDown(actorMap);
#endif
                    }
                }
                else if (pos.y > actorMap.y_next)
                {
                    while (pos.y > actorMap.y_next)
                    {
                        actorMap.y_now++;
                        actorMap.y_next++;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        MapManager.OperatingActor.SetToPositionOnTile(new Vector2(actorMap.x_next, actorMap.y_next));
                        MapLoopUp(actorMap);
#else
                        await MapManager.OperatingActor.SetToPositionOnTile(new Vector2(actorMap.x_next, actorMap.y_next));
                        await MapLoopUp(actorMap);
#endif
                    }
                }
            }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            MapManager.OperatingActor.SetToPositionOnTile(pos);
            MapManager.OperatingActor.GetComponent<CharacterOnMap>().ResetBush(false, false);
#else
            await MapManager.OperatingActor.SetToPositionOnTile(pos);
            await MapManager.OperatingActor.GetComponent<CharacterOnMap>().ResetBush(false, false);
#endif
            //パーティメンバーも強制的に同じ座標に移動
            if (MapManager.PartyOnMap != null)
            {
                for (int i = 0; i < MapManager.PartyOnMap.Count; i++)
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    MapManager.PartyOnMap[i].SetToPositionOnTile(pos);
                    MapManager.PartyOnMap[i].GetComponent<CharacterOnMap>().ResetBush(false, false);
#else
                    await MapManager.PartyOnMap[i].SetToPositionOnTile(pos);
                    await MapManager.PartyOnMap[i].GetComponent<CharacterOnMap>().ResetBush(false, false);
#endif
                }
            }
            //乗り物に搭乗中であれば、乗り物の座標も移動する
            if (MapManager.GetRideVehicle() != null)
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                MapManager.GetRideVehicle().SetToPositionOnTile(pos);
                MapManager.GetRideVehicle().GetComponent<CharacterOnMap>().ResetBush(false, false);
#else
                await MapManager.GetRideVehicle().SetToPositionOnTile(pos);
                await MapManager.GetRideVehicle().GetComponent<CharacterOnMap>().ResetBush(false, false);
#endif
            }
        }

        /// <summary>
        /// ループ時のイベント移動用
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public Vector2 MovePointLoopEvent(Vector2 pos) 
        {
            pos = MovePointLoop(pos);

            var actorPosX = MapManager.OperatingActor.x_next;
            var actorPosY = MapManager.OperatingActor.y_next;

            var wmin = actorPosX - _centerPosX;
            var wmax = wmin + _mapDataModel.width;

            var hmin = actorPosY + _centerPosY;
            var hmax = hmin + (_mapDataModel.height - 1);

            if (_isRLoop)
            {
                if (pos.x > wmax)
                {
                    pos.x -= _mapDataModel.width;
                }
                else if (pos.x < wmin)
                {
                    pos.x += _mapDataModel.width;
                }
            }
            if (_isHLoop)
            {
                if (pos.y > hmax)
                {
                    pos.y -= _mapDataModel.height;
                }
                else if (pos.y < hmin)
                {
                    pos.y += _mapDataModel.height;
                }
            }

            return pos;
        }

        public Vector2 MovePointLoop(Vector2 pos) {
            //指定された座標を、現在のループ中の座標に変換
            if (_isRLoop)
            {
                if (pos.x < _xPosMin)
                {
                    while (pos.x < _xPosMin)
                        pos.x += _mapDataModel.width;
                }
                else if (pos.x > _xPosMax)
                {
                    while (pos.x > _xPosMax)
                        pos.x -= _mapDataModel.width;
                }
            }
            if (_isHLoop)
            {
                if (pos.y > _yPosMin)
                {
                    while (pos.y > _yPosMin)
                        pos.y -= _mapDataModel.height;
                }
                else if (pos.y < _yPosMax)
                {
                    while (pos.y < _yPosMax)
                        pos.y += _mapDataModel.height;
                }
            }
            return pos;
        }

        //イベントの位置を初期位置に戻す
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void EventPositionInitial(CharacterOnMap actorMap)
#else
        public async Task EventPositionInitial(CharacterOnMap actorMap)
#endif
        {
            var actorPosX = actorMap.transform.localPosition.x;

            var wmin = actorPosX - _centerPosX;
            var wmax = _mapDataModel.width;
            var actorPosY = actorMap.transform.localPosition.y;

            var hmin = actorPosY + _centerPosY;
            var hmax = _mapDataModel.height;


            for (int i = 0; i < _eventOnMaps.Count; i++)
            {
                if (_eventOnMaps[i].x_now > _mapDataModel.width)
                {
                    _eventOnMaps[i].x_now -= (int) wmax;
                }
                else
                if (_eventOnMaps[i].x_now <= -_mapDataModel.width)
                {
                    _eventOnMaps[i].x_now += (int) wmax;
                }

                if (_eventOnMaps[i].y_now < -_mapDataModel.height)
                {
                    _eventOnMaps[i].y_now += (int) hmax;
                }
                else
                if(_eventOnMaps[i].y_now > 0)
                {
                    _eventOnMaps[i].y_now -= (int) hmax;
                }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _eventOnMaps[i].SetToPositionOnTileLoop(new Vector2(_eventOnMaps[i].x_now, _eventOnMaps[i].y_now));
#else
                await _eventOnMaps[i].SetToPositionOnTileLoop(new Vector2(_eventOnMaps[i].x_now , _eventOnMaps[i].y_now));
#endif
                _eventOnMaps[i].x_next = _eventOnMaps[i].x_now;
                _eventOnMaps[i].y_next = _eventOnMaps[i].y_now;
            }

            for (int i = 0; i < _vehiclesOnMap.Count; i++)
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _vehiclesOnMap[i].SetToPositionOnTile(new Vector2(_vehiclesOnMap[i].x_now, _vehiclesOnMap[i].y_now));
#else
                    await _vehiclesOnMap[i].SetToPositionOnTile(new Vector2(_vehiclesOnMap[i].x_now, _vehiclesOnMap[i].y_now));
#endif
                _vehiclesOnMap[i].x_next = _vehiclesOnMap[i].x_now;
                _vehiclesOnMap[i].y_next = _vehiclesOnMap[i].y_now;
            }
        }

        /// <summary>
        /// マップサイズを取得する
        /// </summary>
        /// <returns></returns>
        public Vector2Int GetMapSize() 
        {
            return new Vector2Int(_mapDataModel.width, _mapDataModel.height);        
        }

        /// <summary>
        /// マップの描画範囲外かを判定する(U343)
        /// </summary>
        /// <param name="Pos"></param>
        /// <returns></returns>
        public bool CheckMapRangeLoop(Vector2 Pos) 
        {
            bool bCheckLoop = false;
            //Player位置
            var playerPos = MapManager.OperatingCharacter.GetComponent<CharacterOnMap>().GetCurrentPositionOnLoopMapTile();
            // 画面に表示される範囲の設定
            Vector2Int ScreenSize = new Vector2Int(18,10)/2;//(RANGE_WIDTH, RANGE_HEIGHT);
            //現在位置からマップの描画範囲計算
            Rect rect = new Rect();
            rect.min = new Vector2(-(ScreenSize.x), -ScreenSize.y);
            rect.max = new Vector2(ScreenSize.x, (ScreenSize.y)); ;
            rect.position += playerPos;
            if (rect.Contains(Pos) == false)
            {
                bCheckLoop = true;
            }
            return bCheckLoop;
        }

        /// <summary>
        /// イベントキャラ等の移動ループ座標の取得用(U343)
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public Vector2 GetMovePointLoopEvent2(Vector2 pos) 
        {
            //Player位置
            var playerPos = MapManager.OperatingCharacter.GetComponent<CharacterOnMap>().GetCurrentPositionOnLoopMapTile();
            Vector2Int ScreenSize = new Vector2Int(18, 10)/2;//(RANGE_WIDTH, RANGE_HEIGHT);
            Vector2 LoopMax = new Vector2(ScreenSize.x, ScreenSize.y);
            Vector2 LoopMin = new Vector2(-ScreenSize.x, -ScreenSize.y);
            LoopMax += playerPos;
            LoopMin += playerPos;

            //指定された座標を、現在のループ中の座標に変換
            if (_isRLoop)
            {
                if (pos.x < LoopMin.x)
                {
                    pos.x += _mapDataModel.width;
                }
                else if (pos.x > LoopMax.x)
                {
                    pos.x -= _mapDataModel.width;
                }
            }
            if (_isHLoop)
            {
                if (pos.y < LoopMin.y)
                {
                   pos.y += _mapDataModel.height;
                }
                else if (pos.y > LoopMax.y)
                {
                   pos.y -= _mapDataModel.height;
                }
            }
            return pos;
        }

    }
}
