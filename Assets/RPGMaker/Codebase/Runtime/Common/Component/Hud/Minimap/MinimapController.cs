using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.Enum;
using RPGMaker.Codebase.Runtime.Map;
using RPGMaker.Codebase.Runtime.Map.Component.Map;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Map;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using static RPGMaker.Codebase.Runtime.Common.Commons;
using static RPGMaker.Codebase.Runtime.Event.Map.MapChangeMinimapProcessor;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Common.Component.Hud.Minimap
{
    public class MinimapController : MonoBehaviour
    {
        private const int TileIndexPlayer = 6;
        private const int TileIndexVehicle = 3;
        public const int TileIndexWhite = 7;
        private readonly List<CharacterMoveDirectionEnum> AllCharacterMoveDirections = new List<CharacterMoveDirectionEnum>()
        {
            CharacterMoveDirectionEnum.Up,
            CharacterMoveDirectionEnum.Down,
            CharacterMoveDirectionEnum.Left,
            CharacterMoveDirectionEnum.Right,
        };
        private const float BlinkingTime = 0.5f;
        private const int TileSize = 96;
        private const float MinimumScale = 1.0f / TileSize;

        [SerializeField] private Camera _camera;
        [SerializeField] private RawImage _rawImage;
        [SerializeField] private Transform _gridTransform;
        [SerializeField] private Tilemap _tilemap;

        private RenderTexture _renderTexture;
        private TileBase[] _tiles;
        private TilesOnThePosition _tilesOnThePosition;
        private Transform _canvasTransform;
        private string _vehicleId;
        private Dictionary<string, Vector2Int> _replacedPosDic = new Dictionary<string, Vector2Int>();
        private TileBase _passableTile;
        private Color _passableColor;
        private TileBase _unpassableTile;
        private Color _unpassableColor;
        private TileBase _eventTile;
        private Color _eventColor;
        private float _eventBlinkingTime;
        private string _actorId;
        private TileBase _actorTile;
        private Color _actorColor;
        private float _actorBlinkingTime = BlinkingTime;
        private Image _actorImage;
        private int _loopStartX;
        private int _loopEndX;
        private int _loopStartY;
        private int _loopEndY;

        class VehicleTileInfo
        {
            public TileBase tile;
            public Color color;
            public float blinkingTime = BlinkingTime;
        }
        class EventTileInfo
        {
            public TileBase tile;
            public Color color;
            public float blinkingTime = BlinkingTime;
        }
        private Dictionary<string, VehicleTileInfo> _vehicleTileInfoDic = new Dictionary<string, VehicleTileInfo>();
        private Dictionary<string, EventTileInfo> _eventTileInfoDic = new Dictionary<string, EventTileInfo>();

        private void Start() {
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void Init()
#else
        public async Task Init()
#endif
        {
            _canvasTransform = transform.Find("Canvas");
            _canvasTransform.SetParent(Camera.main.transform);
            _canvasTransform.gameObject.SetActive(false);
            //_camera.transform.SetParent(Camera.main.transform);
            _canvasTransform.localPosition = Vector3.zero;
            //_camera.transform.localPosition = Vector3.zero;
            var canvas = _canvasTransform.GetComponent<Canvas>();
            canvas.sortingLayerID = UnityUtil.SortingLayerManager.GetId(UnityUtil.SortingLayerManager.SortingLayerIndex.Runtime_Weather);

            _tilesOnThePosition = gameObject.AddComponent<TilesOnThePosition>();
            var minimap = DataManager.Self().GetRuntimeSaveDataModel().runtimeScreenDataModel.minimap;

            //_gridTransform.localScale = Vector3.one * minimap.scale;
            var scale = Mathf.Max(MinimumScale, minimap.scale);
            _camera.orthographicSize = (float)minimap.height / TileSize / 2 / scale;

            var format = _camera.allowHDR ? GraphicsFormat.R16G16B16A16_SFloat : GraphicsFormat.R8G8B8A8_UNorm;
            _renderTexture = new RenderTexture(minimap.width, minimap.height, 32, format);
            _camera.targetTexture = _renderTexture;
            _rawImage.texture = _renderTexture;
            _rawImage.color = new Color(1, 1, 1, minimap.opacity / 255.0f);
            _rawImage.rectTransform.anchoredPosition = new Vector2(minimap.width / 2.0f + minimap.x, -minimap.height / 2.0f - minimap.y);
            _rawImage.rectTransform.sizeDelta = new Vector2(minimap.width, minimap.height);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            LoadFrame(minimap);
#else
            await LoadFrame(minimap);
#endif
            var actorObj = new GameObject();
            actorObj.name = "ActorObj";
            actorObj.transform.SetParent(_rawImage.transform);
            actorObj.AddComponent<RectTransform>();
            var rectTransform = actorObj.GetComponent<RectTransform>();
            rectTransform.anchoredPosition3D = Vector3.zero;
            _actorImage = actorObj.AddComponent<Image>();

            _vehicleId = GetCurrentVehicleId();

            var mapDataModel = MapManager.CurrentMapDataModel;
            if (mapDataModel.scrollType == MapDataModel.MapScrollType.LoopHorizontal || mapDataModel.scrollType == MapDataModel.MapScrollType.LoopBoth)
            {
                var sw = Mathf.CeilToInt((float) ScreenWidth / TileSize);
                //Debug.Log($"sw: {sw}, {(float) ScreenWidth / TileSize}");
                _loopStartX = -Mathf.CeilToInt((float)sw / mapDataModel.width) * mapDataModel.width;
                //Debug.Log($"_loopStartX: {_loopStartX}, {(float)sw / mapDataModel.width}, mapDataModel.width: {mapDataModel.width}");
                _loopEndX = Mathf.Max(sw, Mathf.CeilToInt((float)minimap.width / TileSize / scale)) + sw;
                //Debug.Log($"_loopEndX: {_loopEndX}, minimap.width / scale: {minimap.width / scale}");
            }
            else
            {
                _loopStartX = 0;
                _loopEndX = 1;
            }
            if (mapDataModel.scrollType == MapDataModel.MapScrollType.LoopVertical || mapDataModel.scrollType == MapDataModel.MapScrollType.LoopBoth)
            {
                var sh = Mathf.CeilToInt((float) ScreenHeight / TileSize);
                _loopStartY = Mathf.CeilToInt((float)sh / mapDataModel.height) * mapDataModel.height;
                _loopEndY = -Mathf.Max(sh, Mathf.CeilToInt((float) minimap.height / TileSize / scale)) - sh;
            }
            else
            {
                _loopStartY = 0;
                _loopEndY = -1;
            }
            //Debug.Log($"_loop: {_loopStartX}, {_loopEndX}, y:{_loopStartY}, {_loopEndY}");

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            LoadTiles();
#else
            await LoadTiles();
#endif

            (_passableTile, _passableColor) = GetTileColor(minimap.passableColor, TileIndexPassable);
            (_unpassableTile, _unpassableColor) = GetTileColor(minimap.unpassableColor, TileIndexUnpassable);
            var match = Regex.Match(minimap.eventColor, @"^([^,]*)(,([^,]*))?$");
            if (match.Success){
                var colorStr = match.Groups[1].Value;
                if (colorStr.Length == 0)
                {
                    _eventTile = null;
                    _eventColor = Color.white;
                }
                else
                {
                    (_eventTile, _eventColor) = GetTileColor(colorStr, TileIndexEvent);
                }
                var timeStr = match.Groups[3].Value;
                if (timeStr.Length == 0)
                {
                    _eventBlinkingTime = 0;
                }
                else if (float.TryParse(timeStr, out var time))
                {
                    _eventBlinkingTime = Mathf.Max(0, time);
                }
                else
                {
                    _eventBlinkingTime = BlinkingTime;
                }
            } else
            {
                _eventTile = _tiles[TileIndexEvent];
                _eventColor = Color.white;
                _eventBlinkingTime = BlinkingTime;
            }
            //Debug.Log($"minimap.eventColor: {minimap.eventColor}, {match.Success}, {_eventTile}, {_eventColor}, {_eventBlinkingTime}");

            UpdateTilemapForGround(minimap);
            //MapManager.LoopInstance.LoopInit(new List<Tilemap>() { _tilemap });
            //MapManager.LoopInstance.AddTilemap(_tilemap);

            Update();
            _canvasTransform.gameObject.SetActive(true);
        }

        private (TileBase, Color) GetTileColor(string colorText, int defaultColor) {
            if (string.IsNullOrEmpty(colorText)) return (null, Color.white);
            var match = Regex.Match(colorText, @"^(\d+)$");
            if (match.Success)
            {
                var index = int.Parse(match.Groups[1].Value);
                if (index < _tiles.Length)
                {
                    return (_tiles[index], Color.white);
                } else
                {
                    Debug.LogError($"color index out of range: {index}");
                }
            }
            else
            {
                match = Regex.Match(colorText, @"^#?([0-9a-fA-F]{6})$");
                if (match.Success) {
                    var val = int.Parse(match.Groups[1].Value, NumberStyles.AllowHexSpecifier);
                    return (_tiles[TileIndexWhite], new Color((val >> 16) / 255.0f, ((val >> 8) & 0xff) / 255.0f, (val & 0xff) / 255.0f));
                }
            }
            return (_tiles[defaultColor], Color.white);

        }

        private string GetCurrentVehicleId() {
            string vehicleId = null;
            if (MapManager.OperatingCharacter == MapManager.GetRideVehicle())
            {
                vehicleId = MapManager.GetRideVehicle().CharacterId;
            }
            return vehicleId;
        }

        /// <summary>
        /// 指定位置(x, y)を地面の通れる・通れないのタイルに更新する。
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="posAdjustX">プレイヤーの位置による補正値</param>
        /// <param name="posAdjustY">プレイヤーの位置による補正値</param>
        private void UpdateTileForGround(int x, int y, int posAdjustX, int posAdjustY) {
            //Debug.Log($"UpdateTileForGround({x}, {y})");
            var mapDataModel = MapManager.CurrentMapDataModel;
            _tilesOnThePosition.InitForRuntime(mapDataModel, new Vector2(x - posAdjustX, y - posAdjustY));
            var passable = false;
            if (_vehicleId != null)
            {
                if (_tilesOnThePosition.CanEnterThisTiles(CharacterMoveDirectionEnum.Up, _vehicleId))
                {
                    passable = true;
                }
            }
            else
            {
                foreach (var dir in AllCharacterMoveDirections)
                {
                    if (_tilesOnThePosition.CanEnterThisTiles(dir))
                    {
                        passable = true;
                        break;
                    }
                }
            }
            var tile = passable ? _passableTile : _unpassableTile;
            if (tile != null)
            {
                var color = passable ? _passableColor : _unpassableColor;
                if (tile is Tile) (tile as Tile).color = color;
                for (int i = _loopStartX; i < _loopEndX; i += mapDataModel.width)
                {
                    for (int j = _loopStartY; j >= _loopEndY; j -= mapDataModel.height)
                    {
                        _tilemap.SetTile(new Vector3Int(x + i, y + j, 0), tile);
                    }
                }
            }
        }

        /// <summary>
        /// 指定位置を指定のタイルに更新する。
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="tile"></param>
        private void UpdateTile(int x, int y, TileBase tile) {
            var mapDataModel = MapManager.CurrentMapDataModel;
            for (int i = _loopStartX; i < _loopEndX; i += mapDataModel.width)
            {
                for (int j = _loopStartY; j >= _loopEndY; j -= mapDataModel.height)
                {
                    _tilemap.SetTile(new Vector3Int(x + i, y + j, 0), tile);
                }
            }
        }

        /// <summary>
        /// ミニマップのタイルマップを最新の内容に更新する
        /// </summary>
        private void UpdateTilemapForGround(RuntimeScreenDataModel.Minimap minimap) {
            _tilemap.ClearAllTiles();
            var mapDataModel = MapManager.CurrentMapDataModel;
            //Debug.Log($"{mapDataModel}, {mapDataModel.width}x{mapDataModel.height}, {_vehicleId}");
            //ループでない場合用に、マップの外側に通れない場所扱いでタイルを配置する。
            var hw = Mathf.CeilToInt((minimap.width / minimap.scale / TileSize - 1) / 2);
            var hh = Mathf.CeilToInt((minimap.height / minimap.scale / TileSize - 1) / 2);
            var xmin = -hw;
            var xmax = mapDataModel.width - 1 + hw;
            var ymin = -hh;
            var ymax = mapDataModel.height - 1 + hh;
            if (_unpassableTile != null)
            {
                if (_unpassableTile is Tile) (_unpassableTile as Tile).color = _unpassableColor;
                for (int j = ymin; j <= ymax; j++)
                {
                    for (int i = xmin; i <= xmax; i++)
                    {
                        if (i >= 0 && i < mapDataModel.width && j >= 0 && j < mapDataModel.height)
                        {
                        }
                        else
                        {
                            _tilemap.SetTile(new Vector3Int(i, -j, 0), _unpassableTile);
                        }
                    }
                }
            }
            var posAdjust = GetPosAdjust();
            for (int j = 0; j < mapDataModel.height; j++)
            {
                for (int i = 0; i < mapDataModel.width; i++)
                {
                    UpdateTileForGround(i, -j, posAdjust.x, posAdjust.y);
                }
            }
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void LoadTiles() {
#else
        private async Task LoadTiles() {
#endif
            var tiles = new List<TileBase>();
            for (int i = 0; i < 1000; i++)
            {
                var path = $"Assets/RPGMaker/Codebase/Runtime/Map/Minimap/{i}.asset";
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#if !UNITY_EDITOR
                if (!AddressableManager.Load.CheckResourceExistenceSync(path)) break;
#endif
                var tile = UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<TileBase>(path);
#else
                if (!await AddressableManager.Load.CheckResourceExistence(path)) break;
                var tile = await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<TileBase>(path);
#endif
                if (tile == null) break;
                tiles.Add(tile);
            }
            _tiles = tiles.ToArray();
            //Debug.Log($"_tiles: {string.Join(", ", _tiles.Select(x => $"{x}"))}");
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void LoadFrame(RuntimeScreenDataModel.Minimap minimap) {
#else
        private async Task LoadFrame(RuntimeScreenDataModel.Minimap minimap) {
#endif
            var parentTransform = _rawImage.transform.parent;
            if (!string.IsNullOrEmpty(minimap.frameName))
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var sprite = UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>($"Assets/RPGMaker/Codebase/Runtime/Map/Minimap/Frame/{minimap.frameName}.png");
#else
                var sprite = await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>($"Assets/RPGMaker/Codebase/Runtime/Map/Minimap/Frame/{minimap.frameName}.png");
#endif
                if (sprite != null)
                {
                    var obj = new GameObject();
                    obj.transform.SetParent(parentTransform);
                    obj.name = "ImageObj";
                    obj.AddComponent<RectTransform>();
                    var objRectTransform = obj.GetComponent<RectTransform>();
                    var rawImageRectTransform = _rawImage.GetComponent<RectTransform>();

                    objRectTransform.anchorMin = rawImageRectTransform.anchorMin;
                    objRectTransform.anchorMax = rawImageRectTransform.anchorMax;
                    objRectTransform.anchoredPosition3D = rawImageRectTransform.anchoredPosition3D;
                    objRectTransform.localScale = new Vector3(sprite.bounds.size.x, sprite.bounds.size.y, 1);

                    var image = obj.AddComponent<Image>();
                    image.sprite = sprite;
                }
            }
            if (!string.IsNullOrEmpty(minimap.maskName))
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var sprite = UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>($"Assets/RPGMaker/Codebase/Runtime/Map/Minimap/Frame/{minimap.maskName}.png");
#else
                var sprite = await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>($"Assets/RPGMaker/Codebase/Runtime/Map/Minimap/Frame/{minimap.maskName}.png");
#endif
                if (sprite != null)
                {
                    var obj = new GameObject();
                    obj.transform.SetParent(parentTransform);
                    obj.transform.SetSiblingIndex(0);
                    obj.name = "MaskObj";
                    obj.AddComponent<RectTransform>();
                    var objRectTransform = obj.GetComponent<RectTransform>();
                    var rawImageRectTransform = _rawImage.GetComponent<RectTransform>();

                    objRectTransform.anchorMin = rawImageRectTransform.anchorMin;
                    objRectTransform.anchorMax = rawImageRectTransform.anchorMax;
                    objRectTransform.anchoredPosition3D = rawImageRectTransform.anchoredPosition3D;
                    objRectTransform.localScale = new Vector3(sprite.bounds.size.x, sprite.bounds.size.y, 1);

                    obj.AddComponent<Mask>();
                    var image = obj.AddComponent<Image>();
                    image.sprite = sprite;

                    _rawImage.transform.SetParent(obj.transform);
                }
            }
        }

        /// <summary>
        /// 指定のタイル座標を非ループ領域に丸め込んだ座標を返す。
        /// </summary>
        private Vector2Int GetNonLoopPos(MapDataModel mapDataModel, Vector2Int pos) {
            if (mapDataModel.scrollType == MapDataModel.MapScrollType.LoopHorizontal || mapDataModel.scrollType == MapDataModel.MapScrollType.LoopBoth)
            {
                if (pos.x < 0)
                {
                    pos.x += ((-pos.x + mapDataModel.width - 1) / mapDataModel.width) * mapDataModel.width;
                }
                else if (pos.x >= mapDataModel.width)
                {
                    pos.x += -(pos.x / mapDataModel.width) * mapDataModel.width;
                }
            }
            if (mapDataModel.scrollType == MapDataModel.MapScrollType.LoopVertical || mapDataModel.scrollType == MapDataModel.MapScrollType.LoopBoth)
            {
                if (pos.y > 0)
                {
                    pos.y += -((pos.y + mapDataModel.height - 1) / mapDataModel.height) * mapDataModel.height;
                }
                else if (pos.y <= -mapDataModel.height)
                {
                    pos.y += (-pos.y / mapDataModel.height) * mapDataModel.height;
                }
            }
            return pos;
        }

        /// <summary>
        /// 指定のタイル座標を非ループ領域に丸め込んだ座標を返す。(浮動小数点版)
        /// </summary>
        private Vector2 GetNonLoopPos(MapDataModel mapDataModel, Vector2 pos) {
            if (mapDataModel.scrollType == MapDataModel.MapScrollType.LoopHorizontal || mapDataModel.scrollType == MapDataModel.MapScrollType.LoopBoth)
            {
                if (pos.x < 0)
                {
                    pos.x += Mathf.Ceil(-pos.x / mapDataModel.width) * mapDataModel.width;
                }
                else if (pos.x >= mapDataModel.width)
                {
                    pos.x += -Mathf.Floor(pos.x / mapDataModel.width) * mapDataModel.width;
                }
            }
            if (mapDataModel.scrollType == MapDataModel.MapScrollType.LoopVertical || mapDataModel.scrollType == MapDataModel.MapScrollType.LoopBoth)
            {
                if (pos.y > 0)
                {
                    pos.y += -Mathf.Ceil(pos.y / mapDataModel.height) * mapDataModel.height;
                }
                else if (pos.y <= -mapDataModel.height)
                {
                    pos.y += Mathf.Floor(-pos.y / mapDataModel.height) * mapDataModel.height;
                }
            }
            return pos;
        }

        private Vector3Int GetPosAdjust() {
            var mapDataModel = MapManager.CurrentMapDataModel;

            var targetCharacter = new TargetCharacter(TargetType.Player, null);
            var targetGo = targetCharacter.GetGameObject();
            var targetPos = targetGo.transform.localPosition;
            var posAdjust = Vector3Int.zero;   //targetPosを非ループ時の範囲に丸め込むための調整値。(タイル単位)
            if (mapDataModel.scrollType == MapDataModel.MapScrollType.LoopHorizontal || mapDataModel.scrollType == MapDataModel.MapScrollType.LoopBoth)
            {
                if (targetPos.x < 0)
                {
                    posAdjust.x = Mathf.CeilToInt(-targetPos.x / mapDataModel.width) * mapDataModel.width;
                }
                else if (targetPos.x >= mapDataModel.width)
                {
                    posAdjust.x = -Mathf.FloorToInt(targetPos.x / mapDataModel.width) * mapDataModel.width;
                }
            }
            if (mapDataModel.scrollType == MapDataModel.MapScrollType.LoopVertical || mapDataModel.scrollType == MapDataModel.MapScrollType.LoopBoth)
            {
                if (targetPos.y > 0)
                {
                    posAdjust.y = -Mathf.CeilToInt(targetPos.y / mapDataModel.height) * mapDataModel.height;
                }
                else if (targetPos.y <= -mapDataModel.height)
                {
                    posAdjust.y = Mathf.FloorToInt(-targetPos.y / mapDataModel.height) * mapDataModel.height;
                }
            }
            return posAdjust;
        }

        private void Update()
        {
            if (_tiles == null) return;
            var minimap = DataManager.Self().GetRuntimeSaveDataModel().runtimeScreenDataModel.minimap;
            {
                var vehicleId = GetCurrentVehicleId();
                if (vehicleId != _vehicleId)
                {
                    _vehicleId = vehicleId;
                    UpdateTilemapForGround(minimap);
                    _replacedPosDic.Clear();
                }
            }
            var mapDataModel = MapManager.CurrentMapDataModel;

            //プレイヤーキャラを中心とするようカメラを移動させる。
            var targetCharacter = new TargetCharacter(TargetType.Player, null);
            var targetGo = targetCharacter.GetGameObject();
            var targetPos = targetGo.transform.localPosition;
            var posAdjust = GetPosAdjust();
            //Debug.Log($"targetPos.x: {targetPos.x}, posAdjust.x: {posAdjust.x}, new targetPos.x: {targetPos.x + posAdjust.x}");
            targetPos += posAdjust;

            _camera.transform.position = new Vector3(0.5f + targetPos.x * _gridTransform.localScale.x, 0.5f + targetPos.y * _gridTransform.localScale.y, _camera.transform.position.z);

            //直前のプレイヤーキャラ、乗り物、イベントの表示を消す。
            foreach (var tilePos in _replacedPosDic.Values)
            {
                UpdateTileForGround(tilePos.x, tilePos.y, posAdjust.x, posAdjust.y);
            }
            _replacedPosDic.Clear();

            //イベントの表示点滅
            foreach (var eventOnMap in MapEventExecutionController.Instance.EventsOnMap)
            {
                var eventId = eventOnMap.MapDataModelEvent.eventId;
                EventTileInfo eventTileInfo = null;
                if (!_eventTileInfoDic.ContainsKey(eventId))
                {
                    eventTileInfo = new EventTileInfo();
                    var eventMapDataModel = eventOnMap.MapDataModelEvent;
                    //メモ欄参照
                    var match = Regex.Match($"{eventMapDataModel.note}", @"<minimap:([^,>]*)(,([^,>]*))?>");
                    eventTileInfo.tile = _tiles[TileIndexEvent];
                    eventTileInfo.color = Color.white;
                    if (match.Success)
                    {
                        var colorStr = match.Groups[1].Value;
                        if (colorStr.Length == 0)
                        {
                            eventTileInfo.tile = null;
                            eventTileInfo.color = Color.white;
                        }
                        else
                        {
                            (eventTileInfo.tile, eventTileInfo.color) = GetTileColor(colorStr, TileIndexEvent);
                        }
                        var timeStr = match.Groups[3].Value;
                        if (timeStr.Length == 0)
                        {
                            eventTileInfo.blinkingTime = 0;
                        }
                        else if (float.TryParse(timeStr, out var time))
                        {
                            eventTileInfo.blinkingTime = Mathf.Max(0, time);
                        }
                        else
                        {
                            eventTileInfo.blinkingTime = BlinkingTime;
                        }
                    }
                    else
                    {
                        eventTileInfo.tile = _eventTile;
                        eventTileInfo.color = _eventColor;
                        eventTileInfo.blinkingTime = _eventBlinkingTime;
                    }
                    _eventTileInfoDic.Add(eventId, eventTileInfo);
                }
                else
                {
                    eventTileInfo = _eventTileInfoDic[eventId];
                }
                if (eventTileInfo.blinkingTime == 0 || (Time.time % (eventTileInfo.blinkingTime * 2)) < eventTileInfo.blinkingTime)
                {
                    var pos = eventOnMap.GetCurrentPositionOnTile();
                    var tilePos = GetNonLoopPos(mapDataModel, new Vector2Int(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y)));
                    var tile = eventTileInfo.tile;
                    if (tilePos.x < 9999999.9f && tilePos.y < 9999999.9f && tile != null)
                    {
                        if (tile is Tile) (tile as Tile).color = eventTileInfo.color;
                        //_tilemap.SetTile(new Vector3Int(tilePos.x, tilePos.y, 0), tile);
                        UpdateTile(tilePos.x, tilePos.y, tile);
                        _replacedPosDic.Add(eventId, tilePos);
                    }
                }
            }

            var playerCharacterId = MapManager.OperatingCharacter.CharacterId;
            //乗り物の表示点滅
            foreach (var vehicleOnMap in MapManager.GetVehiclesOnMap())
            {
                var vehicleId = vehicleOnMap.CharacterId;
                VehicleTileInfo vehicleTileInfo = null;
                if (!_vehicleTileInfoDic.ContainsKey(vehicleId))
                {
                    vehicleTileInfo = new VehicleTileInfo();
                    var vehicleDataModel = DataManager.Self().GetVehicleDataModel(vehicleId);
                    //メモ欄参照
                    var match = Regex.Match(vehicleDataModel.memo, @"<minimap:([^,>]*)(,([^,>]*))?>");
                    vehicleTileInfo.tile = _tiles[TileIndexVehicle];
                    vehicleTileInfo.color = Color.white;
                    if (match.Success)
                    {
                        var colorStr = match.Groups[1].Value;
                        if (colorStr.Length == 0)
                        {
                            vehicleTileInfo.tile = null;
                            vehicleTileInfo.color = Color.white;
                        }
                        else
                        {
                            (vehicleTileInfo.tile, vehicleTileInfo.color) = GetTileColor(colorStr, TileIndexVehicle);
                        }
                        var timeStr = match.Groups[3].Value;
                        if (timeStr.Length == 0)
                        {
                            vehicleTileInfo.blinkingTime = 0;
                        }
                        else if (float.TryParse(timeStr, out var time))
                        {
                            vehicleTileInfo.blinkingTime = Mathf.Max(0, time);
                        }
                        else
                        {
                            vehicleTileInfo.blinkingTime = BlinkingTime;
                        }
                    }
                    else
                    {
                        vehicleTileInfo.tile = _tiles[TileIndexVehicle];
                        vehicleTileInfo.color = Color.white;
                        vehicleTileInfo.blinkingTime = BlinkingTime;
                    }
                    _vehicleTileInfoDic.Add(vehicleId, vehicleTileInfo);
                }
                else
                {
                    vehicleTileInfo = _vehicleTileInfoDic[vehicleId];
                }
                if (vehicleTileInfo.blinkingTime == 0 || (Time.time % (vehicleTileInfo.blinkingTime * 2)) < vehicleTileInfo.blinkingTime)
                {
                    var pos = vehicleOnMap.GetCurrentPositionOnTile();
                    var tilePos = GetNonLoopPos(mapDataModel, new Vector2Int(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y)));
                    if (vehicleId == playerCharacterId)
                    {
                        _actorImage.enabled = true;
                        if (vehicleTileInfo.tile != null)
                        {
                            if (vehicleTileInfo.tile is Tile tile)
                            {
                                var sprite = tile.sprite;
                                _actorImage.sprite = sprite;
                                var rectTransform = _actorImage.GetComponent<RectTransform>();
                                rectTransform.anchoredPosition = new Vector2(0, 0);
                                rectTransform.localScale = new Vector3(sprite.bounds.size.x * minimap.scale, sprite.bounds.size.y * minimap.scale, 1);
                            }
                        }
                    }
                    else
                    {
                        var tile = vehicleTileInfo.tile;
                        if (tile != null)
                        {
                            if (tile is Tile) (tile as Tile).color = vehicleTileInfo.color;
                            //_tilemap.SetTile(new Vector3Int(tilePos.x, tilePos.y, 0), tile);
                            UpdateTile(tilePos.x, tilePos.y, tile);
                            _replacedPosDic.Add(vehicleId, tilePos);
                        }
                    }
                }
                else
                {
                    if (vehicleId == playerCharacterId)
                    {
                        _actorImage.enabled = false;
                    }
                }

            }

            //プレイヤーキャラの表示点滅
            var actorId = MapManager.OperatingCharacter.CharacterId;
            if (actorId != _actorId || _actorBlinkingTime == 0 || (Time.time % (_actorBlinkingTime * 2)) < _actorBlinkingTime)
            {
                if (actorId != _actorId)
                {
                    _actorId = actorId;
                    var actorDataModel = DataManager.Self().GetActorDataModel(_actorId);
                    _actorTile = _tiles[TileIndexPlayer];
                    _actorColor = Color.white;
                    _actorBlinkingTime = BlinkingTime;
                    if (actorDataModel != null) //乗り物に乗るとnullになる
                    {
                        //メモ欄参照
                        var match = Regex.Match(actorDataModel?.basic?.memo, @"<minimap:([^,>]*)(,([^,>]*))?>");
                        if (match.Success)
                        {
                            var colorStr = match.Groups[1].Value;
                            if (colorStr.Length == 0)
                            {
                                _actorTile = null;
                                _actorColor = Color.white;
                            }
                            else
                            {
                                (_actorTile, _actorColor) = GetTileColor(colorStr, TileIndexPlayer);
                            }
                            var timeStr = match.Groups[3].Value;
                            if (timeStr.Length == 0)
                            {
                                _actorBlinkingTime = 0;
                            }
                            else if (float.TryParse(timeStr, out var time))
                            {
                                _actorBlinkingTime = Mathf.Max(0, time);
                            }
                            //Debug.Log($"{match.Groups[1].Value} => {_actorTile}, {_actorColor}");
                            //Debug.Log($"{match.Groups[3].Value} => {_actorBlinkingTime}");
                        }
                    }
                    else
                    {
                        _actorTile = null;
                    }
                }
                var tilePos = GetNonLoopPos(mapDataModel, targetCharacter.GetTilePositionOnTile());

                _actorImage.enabled = _actorTile != null;
                if (_actorTile != null)
                {
                    if (_actorTile is Tile tile)
                    {
                        var sprite = tile.sprite;
                        _actorImage.sprite = sprite;
                        var rectTransform = _actorImage.GetComponent<RectTransform>();
                        var pos = MapManager.OperatingCharacter.GetCurrentPositionOnTile();
                        rectTransform.anchoredPosition = new Vector2(0, 0);
                        rectTransform.localScale = new Vector3(sprite.bounds.size.x * minimap.scale, sprite.bounds.size.y * minimap.scale, 1);
                    }
                }
            }
            else
            {
                if (DataManager.Self().GetActorDataModel(actorId) != null)
                {
                    _actorImage.enabled = false;
                }
            }
        }

        private void OnDestroy()
        {
            UnityEngine.Object.Destroy(_canvasTransform.gameObject);
            //UnityEngine.Object.Destroy(_camera.gameObject);
            //MapManager.LoopInstance.RemoveTilemap(_tilemap);
        }

#if false
        [MenuItem("Tools/CreateMinimapTiles")]
        private static void CreateMinimapTiles() {
            Debug.Log("CreateMinimapTiles executed!");
            var sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/RPGMaker/Codebase/Runtime/Map/Minimap/MinimapTile.png").Select(x => x as Sprite).ToList();
            sprites.RemoveAt(0);
            //Debug.Log($"sprites.Count: {sprites.Count}");
            //Debug.Log(string.Join(", ", sprites.Select(x => $"{x}")));
            for (int j = 0; j < 3; j++)
            {
                for (int i = 0; i < 8; i++)
                {
                    var index = i + j * 8;
                    var tile = ScriptableObject.CreateInstance<Tile>();
                    tile.sprite = sprites[index];
                    AssetDatabase.CreateAsset(tile, $"Assets/RPGMaker/Codebase/Runtime/Map/Minimap/{index}.asset");
                }
            }
        }
#endif
    }
}