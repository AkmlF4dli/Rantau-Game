using JetBrains.Annotations;
using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.EventMap;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Map;
using RPGMaker.Codebase.CoreSystem.Knowledge.Enum;
using RPGMaker.Codebase.CoreSystem.Service.EventManagement;
using RPGMaker.Codebase.Editor.Common;
using RPGMaker.Codebase.Editor.Common.AddonUIUtil;
using RPGMaker.Codebase.Editor.Hierarchy.Common;
using RPGMaker.Codebase.Editor.MapEditor.Component.Canvas.QuickEvent.ModalWindow;
using RPGMaker.Codebase.Editor.MapEditor.Window.EventEdit;
using RPGMaker.Codebase.Runtime.Addon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

namespace RPGMaker.Codebase.Editor.MapEditor.Component.Canvas
{

    /// <summary>
    /// マップ上のイベントを編集する機能を持つキャンバスコンポーネント
    /// </summary>
    public class EventEditCanvas : MapCanvas
    {
        // const
        private const string SelectedMapMassImg = "Assets/RPGMaker/Storage/System/Map/SelectedMapMass.asset";
        private const string RouteMassImg       = "Assets/RPGMaker/SystemResource/MapRoute/route.png";

        //簡単イベント
        private Dictionary<QuickEventContentsEnum, string> QuickEventContentsDictionary =
            new Dictionary<QuickEventContentsEnum, string>
            {
                {QuickEventContentsEnum.door, EditorLocalize.LocalizeText("WORD_1573")},
                {QuickEventContentsEnum.movement, EditorLocalize.LocalizeText("WORD_1574")},
                {QuickEventContentsEnum.savePoint, EditorLocalize.LocalizeText("WORD_1575")},
                {QuickEventContentsEnum.treasureChest, EditorLocalize.LocalizeText("WORD_1576")},
                {QuickEventContentsEnum.cannibalChest, EditorLocalize.LocalizeText("WORD_1577")},
                {QuickEventContentsEnum.toolShop, EditorLocalize.LocalizeText("WORD_1578")},
                {QuickEventContentsEnum.inn, EditorLocalize.LocalizeText("WORD_1580")}
            };

        public enum QuickEventContentsEnum
        {
            door = 0,
            movement,
            savePoint,
            treasureChest,
            cannibalChest,
            toolShop,
            inn
        }

        // データプロパティ
        private EventMapDataModel              _eventMapDataModelEntity;
        private EventMapDataModel.EventMapPage _targetEventMapPage;
        private List<Vector3Int>               _movePosList  = new List<Vector3Int>();
        private List<EventMoveEnum>            _moveCodeList = new List<EventMoveEnum>();
        private List<int>                      _moveCountList = new List<int>();
        private EventEditWindow                _eventEditWindow;
        private string                         _mapId;
        private object                         _deliveryAddressee;
        private Action<List<EventDataModel.EventCommandMoveRoute>> _actionCodeList = null;

        // 状態プロパティ
        private DrawMode      _drawMode;
        private int           _eventIndex;
        private int           _eventCommandIndex;
        private string        _parameterIndex;
        private int           _isPointDelivery;
        private PointDelivery _pointDelivery;
        private string        _coordinateId;
        private bool          _eventMove;

        // UI要素プロパティ
        private GameObject _routeTileObj;

        //private TileBase _selectedTile = null;
        private Vector3Int _selectedPos    = Vector3Int.back;
        private int        _selectedZindex = -6;

        private int  routeCount = 0;

        private Action<int, int> _cursorPosChangeAction = null;

        private EventMapDataModel _draggedEventMapDataModelEntity;

        public void SetEventEditWindow(EventEditWindow eventEditWindow) {
            _eventEditWindow = eventEditWindow;
        }

        // enum
        private enum DrawMode
        {
            None,
            Event,
            Route,
            EventRoute,
            Destination,

            Coordinate,
        }

        //座標の受け渡し先
        public enum PointDelivery
        {
            Movement,
            Door,
        }

        private QuickEventEdit            _quickEventEdit;

        class RoutePointInfo
        {
            public List<Vector3Int> pointList = new List<Vector3Int>();
            public int dragIndex = -1;
            public int intrIndex = -1;
        }

        RoutePointInfo _routePointInfo = null;

        private static TileBase GetRouteTile(int routeCount)
        {
            routeCount = ((routeCount - 1) % 50) + 1;
            var name = $"route_{routeCount.ToString("000")}";
            var tile = ScriptableObject.CreateInstance<Tile>();
            var sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(RouteMassImg).Select(x => x as Sprite);
            foreach (var sprite in sprites)
            {
                if (sprite.name == name)
                {
                    tile.sprite = sprite;
                    return tile;
                }
            }
            tile.sprite = sprites.First();
            return tile;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="mapDataModel"></param>
        /// <param name="eventMapDataModel"></param>
        /// <param name="eventMapPage"></param>
        /// <param name="repaintMethod"></param>
        public EventEditCanvas(
            MapDataModel mapDataModel,
            EventMapDataModel eventMapDataModel,
            EventMapDataModel.EventMapPage eventMapPage,
            Action repaintMethod)
            : base(mapDataModel, repaintMethod)
        {
            DebugUtil.LogMethod();

            _eventMapDataModelEntity = eventMapDataModel;
            _targetEventMapPage = eventMapPage;

            _eventManagementService = new EventManagementService();
            _databaseManagementService = Editor.Hierarchy.Hierarchy.databaseManagementService;
            _quickEventEdit = new QuickEventEdit();

            this.RegisterCallback<DetachFromPanelEvent>(DetachFromPanelEventCallback);  //親から切り離されたときにイベントリストウィンドウを閉じたい。
        }

        /// <summary>
        /// UI初期化
        /// 【注意】
        /// 本メソッドは基底クラスMapCanvasのコンストラクタから呼ばれるので、
        /// 本メソッドの内の処理は、本クラスのコンストラクタ内の処理より先に行われる。
        /// </summary>
        protected override void InitUI(bool isSampleMap = false) {
            DebugUtil.LogMethod();

            base.InitUI();
            LayersProcessForEditor();

            void LayersProcessForEditor()
            {
                foreach (var layer in MapDataModel.MapPrefabManagerForEditor.layers)
                {
                    if (layer.type == MapDataModel.Layer.LayerType.ForRoute)
                    {
                        _routeTileMap = layer.tilemap;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// データおよび表示更新
        /// </summary>
        /// <param name="mapDataModel"></param>
        /// <param name="movePosList"></param>
        public virtual void Refresh(
            [CanBeNull] MapDataModel mapDataModel = null,
            [CanBeNull] List<Vector3Int> movePosList = null
        ) {
            DebugUtil.LogMethod();

            if (mapDataModel != null) MapDataModel = mapDataModel;
            if (movePosList != null) _movePosList = movePosList;
            //ウィンドウリサイズで移動ルートの再描画が行われると、ルートポイントの末尾が開始位置まで伸びてしまう不具合が発生するのを防止。
            if (_drawMode != DrawMode.EventRoute && _drawMode != DrawMode.Route)
            {
                _movePosList?.ForEach(Brush);
            }

            // レイヤーを初期化してサイズ調整
            InitTilemapLayers();

            SetAllEventTiles(_targetEventMapPage);
            SetEarlyPosition();

            Render();
        }

        /// <summary>
        /// マップに触ったらイベントが作成されてしまうので、それを回避する為のモード
        /// </summary>
        public void LaunchNoneDrawingMode() {
            DebugUtil.LogMethod();

            _drawMode = DrawMode.None;
        }

        const double CtrlDuration = 0.5;
        private double _ctrlUntil;
        private bool _ctrlPressed = false;

        protected override void OnKeyDown(KeyDownEvent e) {
            if (_drawMode == DrawMode.EventRoute || _drawMode == DrawMode.Route)
            {
                //ルート設定時は、Ctrlキーを押している間、補間タイルを薄く表示する。
                if (e.ctrlKey)
                {
                    _ctrlUntil = EditorApplication.timeSinceStartup + CtrlDuration;
                    if (!_ctrlPressed)
                    {
                        _ctrlPressed = true;
                        RedrawRoute();
                        //Ctrlキー入力に対してキーリピートが行われるため、単純にKeyDownEvent/KeyUpEventでCtrlキーが離されたかを検知できない。
                        //一定時間後にCtrlキーが押されていないかをチェックして、離されていると判断できれば、補間タイルを通常表示に戻す。
                        _ = RedrawRouteByCtrlReleaseAsync();
                    }
                }
            }
            base.OnKeyDown(e);
        }

        async Task RedrawRouteByCtrlReleaseAsync() {
            while (true) {
                var ms = (int) ((_ctrlUntil - EditorApplication.timeSinceStartup) * 1000);
                if (ms <= 0)
                {
                    _ctrlPressed = false;
                    RedrawRoute();
                    break;
                }
                await Task.Delay(ms);
            }
        }

        private void RedrawRoute() {
            var roInfo = GetRoutePointInfo(Vector3Int.zero);
            RedrawMovementRoute(roInfo);
        }

        public void LaunchNoneDrawingModeByRoute() {
            DebugUtil.LogMethod();

            _drawMode = DrawMode.None;
            _routeTileMap.ClearAllTiles();
            Render();
            var command = new List<EventDataModel.EventCommandMoveRoute>();
            int length = _moveCodeList.Count;
            for (int i = 0; i < length; i++)
            {
                command.Add(new EventDataModel.EventCommandMoveRoute((int) _moveCodeList[i], new List<string>(),
                    _moveCountList[i]));
            }

            _actionCodeList.Invoke(command);
        }

        /// <summary>
        /// イベント描画モード起動
        /// </summary>
        /// <param name="mapId"></param>
        public void LaunchEventDrawingMode(string mapId) {
            DebugUtil.LogMethod();

            _drawMode = DrawMode.Event;
            _mapId = mapId;
            _eventMapDataModelEntity = null;
            _targetEventMapPage = null;
            _movePosList = null;
        }

        /// <summary>
        /// ルート描画モード起動
        /// </summary>
        /// <param name="initializePos"></param>
        public void LaunchRouteDrawingMode(Vector3Int initializePos) {
            DebugUtil.LogMethod();

            _drawMode = DrawMode.Route;
            //ルート設定時には、キーボードフォーカスを得られるようにする。
            var editorWindow = WindowLayoutManager.GetOrOpenWindow(WindowLayoutManager.WindowLayoutId.MapEventEditWindow);
            if (editorWindow != null)
            {
                editorWindow.Focus();
                this.Focus();
            }
            
            int index = -1;

            Vector3Int movePos;
            if (_movePosList != null && _movePosList.Count > 0) {
                movePos = _movePosList[0];
            }
            else
            {
                if (_movePosList == null)
                    _movePosList = new List<Vector3Int>();
                _movePosList.Add(initializePos);
                movePos = _movePosList[0];
            }
            startPos = new Vector2Int(_movePosList[0].x, _movePosList[0].y);

            while (index++ < (_moveCodeList.Count - 1))
            {
                this.routeCount = _moveCountList[index];
                var tile = GetRouteTile(this.routeCount);
                switch (_moveCodeList[index])
                {
                    case EventMoveEnum.MOVEMENT_MOVE_UP:
                        movePos = movePos + Vector3Int.up;
                        _routeTileMap.SetTile(new Vector3Int(movePos.x, movePos.y,0), tile);
                        break;
                    case EventMoveEnum.MOVEMENT_MOVE_DOWN:
                        movePos = movePos + Vector3Int.down;
                        _routeTileMap.SetTile(new Vector3Int(movePos.x, movePos.y,0), tile);
                        break;
                    case EventMoveEnum.MOVEMENT_MOVE_LEFT:
                        movePos = movePos + Vector3Int.left;
                        _routeTileMap.SetTile(new Vector3Int(movePos.x, movePos.y,0), tile);
                        break;
                    case EventMoveEnum.MOVEMENT_MOVE_RIGHT:
                        movePos = movePos + Vector3Int.right;
                        _routeTileMap.SetTile(new Vector3Int(movePos.x, movePos.y,0), tile);
                        break;
                }
            }

            startPos = new Vector2Int(movePos.x, movePos.y);

            Render();
        }

        /// <summary>
        /// 目的地描画モード起動
        /// </summary>
        /// <param name="eventMapDataModelEntity"></param>
        /// <param name="eventIndex"></param>
        /// <param name="eventCommandIndex"></param>
        public void LaunchDestinationDrawingMode(
            EventMapDataModel eventMapDataModelEntity,
            int eventIndex,
            int eventCommandIndex
        ) {
            DebugUtil.LogMethod();

            _drawMode = DrawMode.Destination;
            _eventMapDataModelEntity = eventMapDataModelEntity;
            _eventIndex = eventIndex;
            _eventCommandIndex = eventCommandIndex;
            _targetEventMapPage = _eventMapDataModelEntity?.pages[0];
            _movePosList = null;
        }

        // 描画処理
        //--------------------------------------------------------------------------------------
        private void Brush(Vector3Int cellPos) {
            DebugUtil.LogMethod();

            switch (_drawMode)
            {
                case DrawMode.None:
                    SetCursorPos(cellPos);
                    break;
                case DrawMode.Route:
                case DrawMode.EventRoute:
                    RouteBrush(cellPos);
                    break;
                case DrawMode.Destination:
                    DestinationBrush(cellPos);
                    break;
                case DrawMode.Event:
                    if (_IsEventPos(cellPos)) {
                        MapEditor.LaunchEventEditMode(MapDataModel, GetMassToEvent(cellPos), 0);
                    } else {
                        CreateEvent(cellPos);
                    }

                    // イベントの新規作成をした場合はマップなどの表示を更新済みで、
                    // 下方のRender()のインスタンスは破棄済みなので、ここで戻る。
                    return;
                case DrawMode.Coordinate:
                    SetPosition(cellPos);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _eventEditWindow?.SetPointLabel(cellPos.x, cellPos.y);

            Render();
        }

        private Vector2Int startPos = Vector2Int.zero;

        private void RouteBrush(Vector3Int cellPos) {
            
            int click_x = (int) cellPos.x;
            int click_y = (int) cellPos.y;

            if (routeCount == 1)
            {
                startPos = new Vector2Int(_movePosList[0].x, _movePosList[0].y);
            }

            int move_x = click_x - startPos.x;
            int move_y = click_y - startPos.y;
            if (move_x == 0 && move_y == 0)
            {
                //開始位置とクリックした位置が一緒のため、なにもしない
                return;
            }
            
            //以下、移動ルート計算
            //計算用の係数
            float coefficient = 0.0f;
            //歩いた歩数を記録する用の一時変数
            int calc_x = 0;
            int calc_y = 0;
            
            if (Mathf.Abs(move_x) <= Mathf.Abs(move_y))
            {
                //3.3.係数算出
                coefficient = Mathf.Abs(1.0f * move_x / move_y);
                //3.4.ルートを出す
                while (!(calc_x == move_x && calc_y == move_y))
                {
                    //y座標に1歩進む
                    int bef_y = calc_y;
                    if (move_y < 0)
                    {
                        calc_y--;
                        _moveCodeList.Add(EventMoveEnum.MOVEMENT_MOVE_DOWN);
                        _moveCountList.Add(routeCount);
                        _routeTileMap.SetTile(new Vector3Int(startPos.x + calc_x, startPos.y + calc_y,0), GetRouteTile(routeCount));
                    }
                    else
                    {
                        calc_y++;
                        _moveCodeList.Add(EventMoveEnum.MOVEMENT_MOVE_UP);
                        _moveCountList.Add(routeCount);
                        _routeTileMap.SetTile(new Vector3Int(startPos.x + calc_x, startPos.y + calc_y,0), GetRouteTile(routeCount));
                    }
                    if (_ctrlPressed && (startPos.x + calc_x != click_x || startPos.y + calc_y != click_y))
                    {
                        _routeTileMap.SetColor(new Vector3Int(startPos.x + calc_x, startPos.y + calc_y, 0), new Color(1f, 1f, 1f, 0.5f));
                    }
                    //次にx座標を動かす必要があるかどうかの確認
                    if (Math.Round(Mathf.Abs(bef_y * coefficient)) < Math.Round(Mathf.Abs(calc_y * coefficient)) || calc_y == move_y)
                    {
                        if (calc_x == move_x)
                        {
                            continue;
                        }
                        //一歩ずらす
                        if (move_x < 0)
                        {
                            calc_x--;
                            _moveCodeList.Add(EventMoveEnum.MOVEMENT_MOVE_LEFT);
                            _moveCountList.Add(routeCount);
                            _routeTileMap.SetTile(new Vector3Int(startPos.x  + calc_x, startPos.y + calc_y,0), GetRouteTile(routeCount));
                        }
                        else
                        {
                            calc_x++;
                            _moveCodeList.Add(EventMoveEnum.MOVEMENT_MOVE_RIGHT);
                            _moveCountList.Add(routeCount);
                            _routeTileMap.SetTile(new Vector3Int(startPos.x + calc_x, startPos.y + calc_y,0), GetRouteTile(routeCount));
                        }
                        if (_ctrlPressed && (startPos.x + calc_x != click_x || startPos.y + calc_y != click_y))
                        {
                            _routeTileMap.SetColor(new Vector3Int(startPos.x + calc_x, startPos.y + calc_y, 0), new Color(1f, 1f, 1f, 0.5f));
                        }
                        //フェールセーフ
                        if (calc_y == move_y) break;
                    }
                }
            }
            else
            {
                //3.3.係数算出
                coefficient = Mathf.Abs(1.0f * move_y / move_x);
                //3.4.ルートを出す
                while (!(calc_x == move_x && calc_y == move_y))
                {
                    //x座標に1歩進む
                    int bef_x = calc_x;
                    if (move_x < 0)
                    {
                        calc_x--;
                        _moveCodeList.Add(EventMoveEnum.MOVEMENT_MOVE_LEFT);
                        _moveCountList.Add(routeCount);
                        _routeTileMap.SetTile(new Vector3Int(startPos.x + calc_x, startPos.y + calc_y,0), GetRouteTile(routeCount));
                    }
                    else
                    {
                        calc_x++;
                        _moveCodeList.Add(EventMoveEnum.MOVEMENT_MOVE_RIGHT);
                        _moveCountList.Add(routeCount);
                        _routeTileMap.SetTile(new Vector3Int(startPos.x + calc_x, startPos.y + calc_y,0), GetRouteTile(routeCount));
                    }
                    if (_ctrlPressed && (startPos.x + calc_x != click_x || startPos.y + calc_y != click_y))
                    {
                        _routeTileMap.SetColor(new Vector3Int(startPos.x + calc_x, startPos.y + calc_y, 0), new Color(1f, 1f, 1f, 0.5f));
                    }
                    //次にx座標を動かす必要があるかどうかの確認
                    if (Math.Round(Mathf.Abs(bef_x * coefficient)) < Math.Round(Mathf.Abs(calc_x * coefficient)) || calc_x == move_x)
                    {
                        if (calc_y == move_y)
                        {
                            continue;
                        }
                        //一歩ずらす
                        if (move_y < 0)
                        {
                            calc_y--;
                            _moveCodeList.Add(EventMoveEnum.MOVEMENT_MOVE_DOWN);
                            _moveCountList.Add(routeCount);
                            _routeTileMap.SetTile(new Vector3Int(startPos.x + calc_x, startPos.y + calc_y,0), GetRouteTile(routeCount));
                        }
                        else
                        {
                            calc_y++;
                            _moveCodeList.Add(EventMoveEnum.MOVEMENT_MOVE_UP);
                            _moveCountList.Add(routeCount);
                            _routeTileMap.SetTile(new Vector3Int(startPos.x + calc_x, startPos.y + calc_y,0), GetRouteTile(routeCount));

                        }
                        if (_ctrlPressed && (startPos.x + calc_x != click_x || startPos.y + calc_y != click_y))
                        {
                            _routeTileMap.SetColor(new Vector3Int(startPos.x + calc_x, startPos.y + calc_y, 0), new Color(1f, 1f, 1f, 0.5f));
                        }
                        //フェールセーフ
                        if (calc_x == move_x) break;
                    }
                }
            }
            
            startPos = new Vector2Int(cellPos.x, cellPos.y) ;
        }

        public void InitRouteTileRender(
            EventMapDataModel eventMapDataModelEntity,
            List<Vector3Int> movePosList,
            List<EventDataModel.EventCommandMoveRoute> codeList,
            Action<List<EventDataModel.EventCommandMoveRoute>> setAndSaveMoveRouteAction,
            int eventIndex,
            int eventCommandIndex
        ) {
            routeCount = 0;
            _eventMapDataModelEntity = eventMapDataModelEntity;
            _eventIndex = eventIndex;
            _eventCommandIndex = eventCommandIndex;
            _targetEventMapPage = _eventMapDataModelEntity?.pages[0];
            _movePosList = movePosList;
            _moveCodeList = new List<EventMoveEnum>();
            _moveCountList = new List<int>();
            foreach (var route in codeList)
            {
                _moveCodeList.Add((EventMoveEnum)route.code);
                _moveCountList.Add(route.codeIndex);
            }

            _actionCodeList = setAndSaveMoveRouteAction;

            if (_routeTileObj == null)
            {
                
                _routeTileObj = new GameObject();
                _routeTileMap = _routeTileObj.AddComponent<Tilemap>();
                _routeTileObj.AddComponent<Grid>();
                var tilemapRenderer = _routeTileObj.AddComponent<TilemapRenderer>();
                tilemapRenderer.sortingLayerName = "Editor_Event";
                _routeTileObj.transform.localPosition = new Vector3(0f, 0f, -3f);

            }

            MoveGameObjectToPreviewScene(_routeTileObj);

            if (_drawMode == DrawMode.Route)
            {
                _routeTileMap.ClearAllTiles();
                LaunchRouteDrawingMode(new Vector3Int(_eventMapDataModelEntity.x, _eventMapDataModelEntity.y));
            }
            
            Render();
        }

        private void DestinationBrush(Vector3Int cellPos) {
            var mapName = "マップ";
            UnityEditorWrapper.AssetDatabaseWrapper.Refresh();
            var eventDataModels = _eventManagementService.LoadEvent();

            switch (eventDataModels[_eventIndex].eventCommands[_eventCommandIndex].code)
            {
                case (int) EventEnum.EVENT_CODE_MOVE_PLACE:
                    _parameterIndex = "-1_3_4";
                    break;
                case (int) EventEnum.MOVEMENT_JUMP:
                    _parameterIndex = "9_10_11";
                    break;
                case (int) EventEnum.EVENT_CODE_MOVE_PLACE_SHIP:
                    _parameterIndex = "-1_3_4";
                    break;
                case (int) EventEnum.EVENT_CODE_MAP_GET_POINT:
                    _parameterIndex = "-1_4_5";
                    break;
                case (int) EventEnum.EVENT_CODE_MOVE_SET_EVENT_POINT:
                    _parameterIndex = "-1_2_3";
                    break;
            }

            SetMoveEventViewTile(new Vector2Int(cellPos.x, cellPos.y));

            string[] split = _parameterIndex.Split('_');
            if (int.Parse(split[0]) != -1)
                eventDataModels[_eventIndex].eventCommands[_eventCommandIndex]
                    .parameters[int.Parse(split[0])] = mapName;
            eventDataModels[_eventIndex].eventCommands[_eventCommandIndex]
                .parameters[int.Parse(split[1])] = cellPos.x.ToString(); //x
            eventDataModels[_eventIndex].eventCommands[_eventCommandIndex]
                .parameters[int.Parse(split[2])] = cellPos.y.ToString(); //y
            _eventManagementService.SaveEvent(eventDataModels[_eventIndex]);
            if (split.Length == 4 && split[3] == "end")
                _drawMode = default;
            UnityEditorWrapper.AssetDatabaseWrapper.Refresh();
            MapEditor.ProcessTextRefresh();
        }

        private void CreateEvent(Vector3Int cellPos)
        {
            var firstView = Editor.Hierarchy.Hierarchy.GetCurrentFirstView();
            var eventMapDataModel = MapEditor.CreateEvent(MapDataModel, cellPos.x, cellPos.y);
            MapEditor.ReloadMaps();

            // イベントの新規作成なので0固定。
            int pageNum = 0;
            
            // ヒエラルキーの該当イベントページを選択状態にする。
            Editor.Hierarchy.Hierarchy.SelectButton(CommonMapHierarchyView.GetEventPageButtonName(eventMapDataModel.eventId, pageNum), firstView);
        }
        public void ClearRouteTileDisplay() {
            try
            {
                if (_routeTileMap?.GetUsedTilesCount() != 0)
                {
                    _movePosList?.Clear();
                    _moveCodeList?.Clear();
                    _routeTileMap?.ClearAllTiles();
                    Render();
                }
            }
            catch (Exception)
            {
                Render();
            }
        }

        private Action<Vector3Int> _callBack = null;

        public void SetCoordinate(Action<Vector3Int> callBack = null, string id = "", bool eventMove = false) {
            _drawMode = DrawMode.Coordinate;
            _callBack = callBack;
            _coordinateId = id;
            _eventMove = eventMove;
        }

        public void SetPosition(Vector3Int cellPos)
        {
            // マップ範囲外は何もしない
            if (IsOuterMap(cellPos)) return;

            // セルの位置が同じだったら何もしない
            if (_selectedPos == cellPos) return;

            // 移動時は重複チェックを行う（既にイベントが置かれているか・初期配置のイベントが無いか）
            if (_eventMove == true && _IsEventPos(cellPos) == false && CanPutEvent(cellPos) == false) return;

            InitTilemapLayers();

            SetCursorPos(cellPos);

            _callBack(cellPos);
        }

        // イベントハンドラ
        //--------------------------------------------------------------------------------------
        protected override void OnMouseDown(IMouseEvent e) {
            IsMouseDown = true;

            if (e.altKey || (MouseButton)e.button == MouseButton.MiddleMouse)
            {
                return;
            }

            //座標受け渡し
            if ((MouseButton)e.button == MouseButton.LeftMouse && _isPointDelivery == 1)
            {
                switch (_pointDelivery)
                {
                    //簡単イベントのポップアップに触れる
                    case PointDelivery.Movement:
                        QuickeventMovementModalwindow quickeventMovementModalwindow =
                            (QuickeventMovementModalwindow) _deliveryAddressee;
                        quickeventMovementModalwindow.ChangePoint(GetTilePosOnMousePosForEditor(e.mousePosition));
                        // ウィンドウをアクティブにする
                        EditorWindow.FocusWindowIfItsOpen<QuickeventMovementModalwindow>();
                        break;

                    // Doorイベント
                    case PointDelivery.Door:
                        QuickeventDoorModalwindow quickeventDoorModalwindow =
                            (QuickeventDoorModalwindow) _deliveryAddressee;
                        quickeventDoorModalwindow.ChangePoint(GetTilePosOnMousePosForEditor(e.mousePosition));
                        // ウィンドウをアクティブにする
                        EditorWindow.FocusWindowIfItsOpen<QuickeventDoorModalwindow>();
                        break;
                }
            }

            if ((MouseButton)e.button == MouseButton.RightMouse)
            {
                // 座標設定時は右ボタンクリックは無効。
                if (_drawMode == DrawMode.Coordinate)
                {
                    return;
                }
                if (_drawMode == DrawMode.EventRoute || _drawMode == DrawMode.Route)
                {
                    var cellPos = GetTilePosOnMousePosForEditor(e.mousePosition);
                    if (_routeTileMap.GetTile(cellPos) == null)
                    {
                        //移動ルート上でないタイルを右クリックしたら、最後のルートポイントを削除。
                        var roInfo = GetRoutePointInfo(Vector3Int.zero);
                        if (roInfo.pointList.Count > 0)
                        {
                            roInfo.pointList.RemoveAt(roInfo.pointList.Count - 1);
                            RedrawMovementRoute(roInfo);
                        }
                    } else
                    {
                        //移動ルート上を右クリックしたら、ポップアップメニューを表示。
                        var menu = new GenericMenu();
                        var roInfo = GetRoutePointInfo(cellPos);
                        if (roInfo.dragIndex >= 0 && (roInfo.dragIndex == roInfo.pointList.Count - 1 || (roInfo.dragIndex > 0 ? roInfo.pointList[roInfo.dragIndex - 1] : _movePosList[0]) != roInfo.pointList[roInfo.dragIndex + 1]))
                        {
                            //右クリック位置にルートポイントがあり、かつ、そのルートポイントを削除したときに前後のルートポイントが重ならないなら。
                            // 削除
                            //
                            menu.AddItem(new GUIContent(EditorLocalize.LocalizeText("WORD_5033")), false, () => {
                                roInfo.pointList.RemoveAt(roInfo.dragIndex);
                                RedrawMovementRoute(roInfo);
                            });
                        }
                        if (roInfo.pointList[roInfo.intrIndex] != cellPos)
                        {
                            //ルートポイント上での挿入にならないなら。
                            // 挿入
                            menu.AddItem(new GUIContent(EditorLocalize.LocalizeText("WORD_5032")), false, () =>
                            {
                                roInfo.pointList.Insert(roInfo.intrIndex, cellPos);
                                RedrawMovementRoute(roInfo);
                            });
                        }
                        menu.ShowAsContext();
                    }
                    return;
                }
                if (_draggedEventMapDataModelEntity != null)
                {
                    //イベント移動中に右クリックしたら移動をキャンセル。
                    _draggedEventMapDataModelEntity = null;
                }

                IsMouseDown = false;
                IsRightClick = true;
                SetCursorPos(GetTilePosOnMousePosForEditor(e.mousePosition));
                Render();
                return;
            }

            if (_drawMode == DrawMode.EventRoute || _drawMode == DrawMode.Route)
            {
                _routePointInfo = null;
                if (e.ctrlKey)
                {
                    var cellPos = GetTilePosOnMousePosForEditor(e.mousePosition);
                    if (_routeTileMap.GetTile(cellPos) != null)
                    {
                        //Ctrlキーを押しながら左クリックしたタイルが移動ルート上なら。
                        _routePointInfo = GetRoutePointInfo(cellPos);
                        if (_routePointInfo.dragIndex < 0)
                        {
                            _routePointInfo = null;
                        }
                    }
                    return;
                }
                routeCount++;
                Brush(GetTilePosOnMousePosForEditor(e.mousePosition));
            }
            else
            {
                Brush(GetTilePosOnMousePosForEditor(
                    e.mousePosition, dontCompensateLeftAndUpValues: _drawMode == DrawMode.Coordinate));
                if (_drawMode == DrawMode.None)
                {
                    var eventMapDataModel = GetMassToEvent(GetTilePosOnMousePosForEditor(e.mousePosition));
                    if (eventMapDataModel != null)
                    {
                        _draggedEventMapDataModelEntity = eventMapDataModel;
                    }
                }
            }
        }

        RoutePointInfo GetRoutePointInfo(Vector3Int cellPos) {
            var roInfo = new RoutePointInfo();

            int index = 0;
            int count = 1;
            var movePos = _movePosList[0];
            while (index < _moveCodeList.Count)
            {
                this.routeCount = _moveCountList[index];
                switch (_moveCodeList[index])
                {
                    case EventMoveEnum.MOVEMENT_MOVE_UP:
                        movePos = movePos + Vector3Int.up;
                        break;
                    case EventMoveEnum.MOVEMENT_MOVE_DOWN:
                        movePos = movePos + Vector3Int.down;
                        break;
                    case EventMoveEnum.MOVEMENT_MOVE_LEFT:
                        movePos = movePos + Vector3Int.left;
                        break;
                    case EventMoveEnum.MOVEMENT_MOVE_RIGHT:
                        movePos = movePos + Vector3Int.right;
                        break;
                }
                if (movePos == cellPos)
                {
                    roInfo.intrIndex = roInfo.pointList.Count;
                }
                index++;
                if (index >= _moveCodeList.Count || _moveCountList[index] > count)
                {
                    if (movePos == cellPos)
                    {
                        roInfo.dragIndex = roInfo.pointList.Count;
                    }
                    roInfo.pointList.Add(movePos);
                    count++;
                }
            }

            return roInfo;
        }

        protected override void OnMouseUp(IMouseEvent e) {
            base.OnMouseUp(e);

            //移動ルートポイント情報のクリア
            _routePointInfo = null;

            var cellPos = GetTilePosOnMousePosForEditor(e.mousePosition, dontCompensateLeftAndUpValues: true);
            //イベントの移動先がマップ範囲内のときだけ、処理させる。
            if (IsOuterMap(cellPos)) return;
            if ((MouseButton)e.button == MouseButton.RightMouse)
            {
                // 座標設定時、移動ルート設定時は右ボタンクリックは無効。
                if (_drawMode == DrawMode.Coordinate || _drawMode == DrawMode.EventRoute || _drawMode == DrawMode.Route)
                {
                    return;
                }
                OnRightClickEvent(cellPos);
                return;
            }

            // イベントの移動
            if (_draggedEventMapDataModelEntity != null)
            {                
                //イベントの移動先がマップ範囲内のときだけ、処理させる。
                if (IsOuterMap(cellPos) == false) {
                    // ドラッグしているイベントが設置可能かチェック
                    if (!_IsEventPos(cellPos) && CanPutEvent(cellPos) && CanPutArea(cellPos))
                    {
                        var refresh = false;    //選択中のイベントを移動させたときだけRefresh()を行わせる。
                        var selectEvent = (_eventMapDataModelEntity != _draggedEventMapDataModelEntity) ? _draggedEventMapDataModelEntity : null;   //ヒエラルキーで選択状態にしたいイベント
                        if (_draggedEventMapDataModelEntity.x != cellPos.x || _draggedEventMapDataModelEntity.y != cellPos.y)
                        {
                            var eventMapDataModel = GetMassToEvent(cellPos);
                            if (eventMapDataModel == null)
                            {
                                _draggedEventMapDataModelEntity.x = cellPos.x;
                                _draggedEventMapDataModelEntity.y = cellPos.y;
                                MapEditor.SaveEventMap(_draggedEventMapDataModelEntity);
                                refresh = true;
                            }
                            else
                            {
                                selectEvent = eventMapDataModel;
                            }
                        }
                        if (selectEvent != null)
                        {
                            Editor.Hierarchy.Hierarchy.SelectButton(
                                CommonMapHierarchyView.GetEventPageButtonName(
                                    selectEvent.eventId,
                                    EventMapDataModel.EventMapPage.DefaultPage));
                            refresh = false;
                        }
                        if (refresh)
                        {
                            _eventEditWindow.Refresh();
                        }
                    }
                }
                _draggedEventMapDataModelEntity = null;
            }

            // 左クリック
            if ((MouseButton) e.button == MouseButton.LeftMouse && (_drawMode == DrawMode.None || _drawMode == DrawMode.Event))
            {
                var eventMapDataModel = GetMassToEvent(cellPos);

                var executionContentsWindow = WindowLayoutManager.GetWindowFromResources<ExecutionContentsWindow>();
                var drawNoneNotMapEventFocus = (_drawMode == DrawMode.None && Editor.Hierarchy.Hierarchy.GetCurrentFirstView() != Editor.Hierarchy.Hierarchy.FirstView.MapEventListView);
                // イベント配置モードか、異なるイベントか、イベント実行内容ウィンドウが開いてないか、DrawMode未設定でマップ・イベントがリスト上でフォーカスされていなければ、クリックしたイベントを選択状態にする
                if (eventMapDataModel != null && (_drawMode == DrawMode.Event || _eventMapDataModelEntity != eventMapDataModel || executionContentsWindow == null || drawNoneNotMapEventFocus))
                {
                    Editor.Hierarchy.Hierarchy.SelectButton(
                                        CommonMapHierarchyView.GetEventPageButtonName(
                                            eventMapDataModel.eventId,
                                            EventMapDataModel.EventMapPage.DefaultPage));
                    Focus();
                }
            }
        }

        protected override void OnMouseDrag(MouseMoveEvent e) {
            // 中央ボタン押下中ならベースに処理させる。
            if (e.pressedButtons == MousePressedButtons.Middle)
            {
                base.OnMouseDrag(e);
                return;
            }
            // 左ボタンのみ押下中でなければ戻る。
            if (e.pressedButtons != MousePressedButtons.Left)
            {
                return;
            }

            if (e.altKey)
            {
                base.OnMouseDrag(e);
            }
            else
            {
                // シフトキーが押されていない場合はブラシ処理
                if (_drawMode == DrawMode.EventRoute || _drawMode == DrawMode.Route)
                {
                    if (_routePointInfo != null)
                    {
                        //ルートポイントをドラッグ移動。
                        var cellPos = GetTilePosOnMousePosForEditor(e.mousePosition);
                        if (cellPos != _routePointInfo.pointList[_routePointInfo.dragIndex]
                            && (cellPos != (_routePointInfo.dragIndex > 0 ? _routePointInfo.pointList[_routePointInfo.dragIndex - 1] : _movePosList[0]))
                            && (_routePointInfo.dragIndex == _routePointInfo.pointList.Count - 1 || cellPos != _routePointInfo.pointList[_routePointInfo.dragIndex + 1])
                            )
                        {
                            //移動先が、前後のルートポイントと一致しないなら。
                            _routePointInfo.pointList[_routePointInfo.dragIndex] = cellPos;
                            RedrawMovementRoute(_routePointInfo);
                        }
                    }
                    return;
                }
                else
                {
                    // 座標設定時はドラッグは無効。
                    if (_drawMode == DrawMode.Coordinate)
                    {
                        return;
                    }

                    Brush(GetTilePosOnMousePosForEditor(e.mousePosition));
                }
            }
        }

        private void RedrawMovementRoute(RoutePointInfo routePointInfo) {
            _routeTileMap?.ClearAllTiles();
            _moveCodeList.Clear();
            _moveCountList.Clear();
            startPos = new Vector2Int(_movePosList[0].x, _movePosList[0].y);
            routeCount = 0;
            foreach (var cellPos in routePointInfo.pointList)
            {
                routeCount++;
                RouteBrush(cellPos);
            }
            Render();
        }

        /// <summary>
        /// 移動系イベントで表示する用
        /// </summary>
        public void SetMoveEventViewTile(Vector2Int pos) {
            _routeTileMap?.ClearAllTiles();
            var tile = AssetDatabase.LoadAssetAtPath<TileBase>(EventMassImg);
            _routeTileMap?.SetTile(new Vector3Int(pos.x, pos.y, 6), tile);
            SetCursorPos(new Vector3Int(pos.x, pos.y, 0));
            Render();
        }

        public void DeleteMoveEventViewTile() {
            if (_routeTileMap != null && _routeTileMap?.GetUsedTilesCount() != 0)
            {
                _routeTileMap?.ClearAllTiles();
                Render();
            }
        }

        /// <summary>
        /// すでにイベントが設置されているか
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private bool _IsEventPos(Vector3Int pos) {
            foreach (var map in _eventMapDataModels)
            {
                if (map.x == pos.x && map.y == pos.y)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 配置可能範囲か
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private bool CanPutArea(Vector3Int pos) {
            // 範囲外
            if (MapDataModel.width - 1 < pos.x || MapDataModel.height - 1 < pos.y * -1 || 0 > pos.x || 0 < pos.y)
                return false;

            return true;
        }

        /// <summary>
        /// 配置できるか
        /// （すでにプレイヤーか乗り物が設置されていないか）
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private bool CanPutEvent(Vector3Int pos) {
            var vehicle = _databaseManagementService.LoadCharacterVehicles();

            foreach (var v in vehicle)
                if (v.mapId == MapDataModel.id && v.initialPos[0] == pos.x && v.initialPos[1] == pos.y)
                    return false;

            if (IsPutPlayerStart(pos))
                return false;

            return true;
        }

        /// <summary>
        /// 初期配置 - プレイヤーのイベントが指定したマップ位置に配置されているか
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private bool IsPutPlayerStart(Vector3Int pos) {
            var startMap = _databaseManagementService.LoadSystem().initialParty.startMap;
            return startMap.mapId == MapDataModel.id && startMap.position[0] == pos.x && startMap.position[1] == pos.y;
        }

        /// <summary>
        /// マス目からイベント取得
        /// </summary>
        /// <param name="cellPos"></param>
        private EventMapDataModel GetMassToEvent(Vector3Int cellPos) {
            EventMapDataModel eventMapDataModelEntity = null;
            foreach (var eventMap in _eventMapDataModels)
            {
                if (eventMap.x == cellPos.x && eventMap.y == cellPos.y)
                {
                    eventMapDataModelEntity = eventMap;
                    break;
                }
            }
            return eventMapDataModelEntity;
        }

        private void OnRightClickEvent(Vector3Int cellPos) {
            // マップ外で右クリックを押下した場合は右クリックメニューを表示しない
            if (!CanPutArea(cellPos)) return;

            var menu = new GenericMenu();

            // 既にイベントが置かれている
            if (_IsEventPos(cellPos)) {
                // コピー
                menu.AddItem(new GUIContent(EditorLocalize.LocalizeText("WORD_0015")), false, () => CopyLogic(cellPos));
                // 削除
                menu.AddItem(new GUIContent(EditorLocalize.LocalizeText("WORD_0886")), false, () => {

                    if (EditorUtility.DisplayDialog(
                                EditorLocalize.LocalizeText("WORD_5021"),
                                EditorLocalize.LocalizeText("WORD_5022"),
                                EditorLocalize.LocalizeText("WORD_5023"),
                                EditorLocalize.LocalizeText("WORD_5024")) == true)
                    {
                        CommonMapHierarchyView.DeleteEventFromMap(GetMassToEvent(cellPos), MapDataModel);
                    }
                });
            } 
            // イベントが置かれていない
            else {
                // 初期配置のイベントが置かれていない
                if (CanPutEvent(cellPos)) {
                    // 新規作成
                    menu.AddItem(new GUIContent(EditorLocalize.LocalizeText("WORD_1520")), false, () => CreateEvent(cellPos));
                    // 貼り付けはコピー/切り取り中のイベントが無い場合は非活性にする
                    var copyEventExist = false;

                    if (!String.IsNullOrEmpty(CopyEventId) && _eventManagementService.LoadEventMap().Any(ev => ev.eventId == CopyEventId)) {
                        copyEventExist = true;
                    }
                    menu.AddItem(new GUIContent(EditorLocalize.LocalizeText("WORD_0011")), false, copyEventExist ? () => PasteAction(cellPos) : null);

                    // 簡単イベント
                    for (int i = 0; i < QuickEventContentsDictionary.Count; i++)
                    {
                        var QuickEvent = QuickEventContentsDictionary[(QuickEventContentsEnum) i];
                        var Content = (QuickEventContentsEnum) i;
                        menu.AddItem(new GUIContent(EditorLocalize.LocalizeText("WORD_1572") + "/" + QuickEvent), false, () =>
                        {
                            _quickEventEdit.Init(
                                cellPos,
                                this,
                                _eventEditWindow,
                                Content,
                                _eventMapDataModels,
                                MapDataModel
                            );
                            MapEditor.ReloadMaps();
                        });
                    }

                    // 初期配置 - プレイヤー
                    menu.AddItem(new GUIContent(EditorLocalize.LocalizeText("WORD_1597") + "/" + EditorLocalize.LocalizeText("WORD_1598")), false, () =>
                    {
                        var systemSettingDataModel = _databaseManagementService.LoadSystem();
                        systemSettingDataModel.initialParty.startMap.mapId = MapDataModel.id;
                        systemSettingDataModel.initialParty.startMap.position[0] = cellPos[0];
                        systemSettingDataModel.initialParty.startMap.position[1] = cellPos[1];
                        _databaseManagementService.SaveSystem(systemSettingDataModel);
                        RpgMakerEditor.DataCheckMenuButton(true);
                        IsRightClick = false;
                        Refresh();
                    });

                    //初期配置の乗り物部分
                    var vehiclesDataModels =
                        _databaseManagementService.LoadCharacterVehicles();
                    for (int i = 0; i < vehiclesDataModels.Count; i++)
                    {
                        var vehicle = vehiclesDataModels[i];
                        menu.AddItem(new GUIContent(EditorLocalize.LocalizeText("WORD_1597") + "/" + vehicle.name), false, () =>
                        {
                            vehicle.mapId = MapDataModel.id;
                            vehicle.initialPos =
                                new List<int>() { cellPos.x, cellPos.y, cellPos.z };
                            for (int j = 0; j < vehiclesDataModels.Count; j++)
                            {
                                if (vehiclesDataModels[j].id == vehicle.id)
                                {
                                    vehiclesDataModels[j].initialPos = vehicle.initialPos;
                                    break;
                                }
                            }

                            _databaseManagementService.SaveCharacterVehicles(vehiclesDataModels);
                            IsRightClick = false;
                            Refresh();
                        });
                    }
                }
                // 初期配置のイベントが置かれている
                else {
                    // 初期配置イベント削除
                    menu.AddItem(new GUIContent(EditorLocalize.LocalizeText("WORD_5030")), false, () => DeleteInitialEvent(cellPos));
                }
            }
            menu.ShowAsContext();
        }

        public static string CopyEventId = "";

        /// <summary>
        /// 現在のマウス位置がマップ外かチェックする
        /// </summary>
        /// <param name="cellPostision"></param>
        /// <returns></returns>
        private bool IsOuterMap(Vector3 cellPostision) {
            return cellPostision.x < 0 || cellPostision.x > MapDataModel.width ||
                -cellPostision.y < 0 || -cellPostision.y > MapDataModel.height;
        }

        /// <summary>
        /// イベントコピーの実処理
        /// </summary>
        /// <param name="cellPos"></param>
        /// <returns></returns>
        private void CopyLogic(Vector3Int cellPos) {
            var eventDataModel = GetMassToEvent(cellPos);
            CopyEventId = eventDataModel.eventId;
        }

        /// <summary>
        /// イベント貼り付けの実処理
        /// </summary>
        /// <param name="cellPos"></param>
        /// <returns></returns>
        private void PasteAction(Vector3Int cellPos) {
            // Json経由でクローン作成。
            var eventMapDataModel = JsonHelper.Clone(
                _eventManagementService.LoadEventMap().Single(em => em.eventId == CopyEventId));
            eventMapDataModel.x = cellPos.x;
            eventMapDataModel.y = cellPos.y;

            // イベントidを新規生成したものに変更。
            eventMapDataModel.eventId = System.Guid.NewGuid().ToString();
            // マップidを貼り付け位置のものに変更。
            eventMapDataModel.mapId = MapDataModel.id;
            eventMapDataModel.name = eventMapDataModel.name + " " + EditorLocalize.LocalizeText("WORD_1462"); ;
            // セーブ。
            _eventManagementService.SaveEventMap(eventMapDataModel);

            // EventDataModelも複製
            List<EventDataModel> copyList = new List<EventDataModel>();
            foreach (var source in _eventManagementService.LoadEvent().Where(e => e?.id == CopyEventId))
            {
                var eventDataModelCopy = JsonHelper.Clone(source);
                eventDataModelCopy.id = eventMapDataModel.eventId;
                copyList.Add(eventDataModelCopy);
            }
            copyList.ForEach(v => _eventManagementService.SaveEvent(v));

            // Hierarchy更新。
            MapEditor.ReloadMaps();
            Editor.Hierarchy.Hierarchy.SelectButton(CommonMapHierarchyView.GetEventPageButtonName(eventMapDataModel.eventId, 0));
        }

        /// <summary>
        /// 初期配置イベント
        /// </summary>
        /// <param name="cellPos"></param>
        private void DeleteInitialEvent(Vector3Int cellPos) {
            // プレイヤーの初期位置
            if (IsPutPlayerStart(cellPos)) {
                var systemSettingDataModel = _databaseManagementService.LoadSystem();
                systemSettingDataModel.initialParty.startMap.mapId = "";
                systemSettingDataModel.initialParty.startMap.position[0] = 0;
                systemSettingDataModel.initialParty.startMap.position[1] = 0;
                _databaseManagementService.SaveSystem(systemSettingDataModel);
                Refresh();
                RpgMakerEditor.DataCheckMenuButton(false);
                return;
            }

            // 乗り物の初期位置
            var vehicles = _databaseManagementService.LoadCharacterVehicles();
            var updateVehicles = vehicles.Select(vehicle =>{
                if (vehicle.mapId == MapDataModel.id && vehicle.initialPos[0] == cellPos.x && vehicle.initialPos[1] == cellPos.y) {
                    vehicle.mapId = "";
                    vehicle.initialPos[0] = 0;
                    vehicle.initialPos[1] = 0;
                }
                return vehicle;
            }).ToList();

            _databaseManagementService.SaveCharacterVehicles(updateVehicles);
            Refresh();

        }

        public static void CopyMapEvents(string srcMapId, string destMapId, string destMapName) {
            var eventManagementService = Editor.Hierarchy.Hierarchy.eventManagementService;
            var srcList = eventManagementService.LoadEventMap().Where(em => em.mapId == srcMapId).ToList();
            var eventMapping = new Dictionary<string, string>();
            foreach (var srcEventMapDataModel in srcList) {
                // Json経由でクローン作成。
                var eventMapDataModel = JsonHelper.Clone(srcEventMapDataModel);
                eventMapDataModel.x = srcEventMapDataModel.x;
                eventMapDataModel.y = srcEventMapDataModel.y;

                // イベントidを新規生成したものに変更。
                eventMapDataModel.eventId = System.Guid.NewGuid().ToString();
                eventMapping.Add(srcEventMapDataModel.eventId, eventMapDataModel.eventId);
                // マップidを貼り付け位置のものに変更。
                eventMapDataModel.mapId = destMapId;
                // セーブ。
                eventManagementService.SaveEventMap(eventMapDataModel);

                // EventDataModelも複製
                List<EventDataModel> copyList = new List<EventDataModel>();
                foreach (var source in eventManagementService.LoadEvent().Where(e => e?.id == srcEventMapDataModel.eventId))
                {
                    var eventDataModelCopy = JsonHelper.Clone(source);
                    eventDataModelCopy.id = eventMapDataModel.eventId;
                    copyList.Add(eventDataModelCopy);
                }
                copyList.ForEach(v => eventManagementService.SaveEvent(v));
            }

            ReplaceEventReference(srcMapId, destMapId, destMapName, eventMapping, eventManagementService.LoadEvent());
        }

        static Dictionary<int, object> _eventReferneceIndexDic = new Dictionary<int, object>() {
            { (int)EventEnum.EVENT_CODE_CHARACTER_SHOW_ANIMATION, 0 },
            { (int)EventEnum.EVENT_CODE_CHARACTER_SHOW_ICON, 0 },
            { (int)EventEnum.MOVEMENT_TURN_DOWN, 0 },
            { (int)EventEnum.MOVEMENT_TURN_LEFT, 0 },
            { (int)EventEnum.MOVEMENT_TURN_RIGHT, 0 },
            { (int)EventEnum.MOVEMENT_TURN_UP, 0 },
            { (int)EventEnum.MOVEMENT_TURN_90_RIGHT, 0 },
            { (int)EventEnum.MOVEMENT_TURN_90_LEFT, 0 },
            { (int)EventEnum.MOVEMENT_TURN_180, 0 },
            { (int)EventEnum.MOVEMENT_TURN_90_RIGHT_OR_LEFT, 0 },
            { (int)EventEnum.MOVEMENT_TURN_AT_RANDOM, 0 },
            { (int)EventEnum.MOVEMENT_TURN_TOWARD_PLAYER, 0 },
            { (int)EventEnum.MOVEMENT_TURN_AWAY_FROM_PLAYER, 0 },
            { (int)EventEnum.MOVEMENT_WALKING_ANIMATION_ON, 0 },
            { (int)EventEnum.MOVEMENT_WALKING_ANIMATION_OFF, 0 },
            { (int)EventEnum.MOVEMENT_STEPPING_ANIMATION_ON, 0 },
            { (int)EventEnum.MOVEMENT_STEPPING_ANIMATION_OFF, 0 },
            { (int)EventEnum.MOVEMENT_CHANGE_IMAGE, 0 },
            { (int)EventEnum.EVENT_CODE_MOVE_SET_MOVE_POINT, 0 },
            { (int)EventEnum.MOVEMENT_MOVE_AT_RANDOM, 0 },
            { (int)EventEnum.MOVEMENT_MOVE_TOWARD_PLAYER, 0 },
            { (int)EventEnum.MOVEMENT_MOVE_AWAY_FROM_PLAYER, 0 },
            { (int)EventEnum.EVENT_CODE_FLOW_CUSTOM_MOVE, 1 },
            { (int)EventEnum.MOVEMENT_JUMP, 0 },
            { (int)EventEnum.EVENT_CODE_MAP_GET_POINT, 6 },
            { (int)EventEnum.EVENT_CODE_FLOW_IF, 33 },
            { (int)EventEnum.EVENT_CODE_MOVE_SET_EVENT_POINT, new List<int>(){ 0, 5 } },
        };
        static void ReplaceEventReference(string srcMapId, string destMapId, string destMapName, Dictionary<string, string> eventMapping, List<EventDataModel> eventDataModelList) {
            var set = new HashSet<string>();
            foreach (var eventId in eventMapping.Values)
            {
                set.Add(eventId);
            }
            var addonInfos = AddonManager.Instance.GetAddonInfos();
            var modifiedEventDataModels = new HashSet<EventDataModel>();
            foreach (var eventDataModel in eventDataModelList.Where(e => set.Contains(e?.id)))
            {
                foreach (var eventCommand in eventDataModel.eventCommands)
                {
                    var indices = new List<int>();
                    if (_eventReferneceIndexDic.ContainsKey(eventCommand.code))
                    {
                        var idx = _eventReferneceIndexDic[eventCommand.code];
                        if (idx is List<int>)
                        {
                            indices.AddRange((idx as List<int>));
                        } else
                        {
                            indices.Add((int)idx);
                        }
                    } else
                        switch (eventCommand.code)
                        {
                            case (int) EventEnum.EVENT_CODE_GAME_VAL:
                                if (int.Parse(eventCommand.parameters[3]) == 3 && eventCommand.parameters[4] == "5")
                                {
                                    indices.Add(5);
                                }
                                break;
                            case (int) EventEnum.EVENT_CODE_ADDON_COMMAND:
                                {
                                    var addonInfo = addonInfos.GetAddonInfo(eventCommand.parameters[0]);
                                    var commandInfo = addonInfo != null
                                        ? addonInfo.commandInfos.GetCommandInfo(eventCommand.parameters[1])
                                        : null;
                                    var parameters =
                                        new AddonParameterContainer(JsonHelper.FromJsonArray<AddonParameter>(eventCommand.parameters[2]));
                                    if (commandInfo != null)
                                    {
                                        foreach (var paramInfo in commandInfo.args)
                                        {
                                            var text = paramInfo.GetInfo("text")?.value;
                                            var value = parameters.GetParameterValue(paramInfo.name);
                                            if (value == null)
                                            {
                                                continue;
                                            }
                                            value = AddonUIUtil.GetEventReplacedText(addonInfo, paramInfo, null, value, srcMapId, destMapId, eventMapping);
                                            parameters.SetParameterValue(paramInfo.name, value);
                                        }
                                        eventCommand.parameters[2] = JsonHelper.ToJsonArray<AddonParameter>(parameters);
                                    }
                                    break;
                                }
                        }
                    if (indices.Count > 0)
                    {
                        foreach (var index in indices)
                        {
                            if (eventMapping.ContainsKey(eventCommand.parameters[index]))
                            {
                                eventCommand.parameters[index] = eventMapping[eventCommand.parameters[index]];
                            }
                        }
                        modifiedEventDataModels.Add(eventDataModel);
                    }

                    //「場所移動」「乗り物位置設定」
                    if (eventCommand.code == (int) EventEnum.EVENT_CODE_MOVE_PLACE || eventCommand.code == (int) EventEnum.EVENT_CODE_MOVE_PLACE_SHIP)
                    {
                        if (eventCommand.parameters[2] == srcMapId){
                            eventCommand.parameters[2] = destMapId;
                            if (eventCommand.code == (int) EventEnum.EVENT_CODE_MOVE_PLACE)
                            {
                                eventCommand.parameters[1] = destMapName;
                                eventCommand.parameters[7] = destMapId;
                            }
                            modifiedEventDataModels.Add(eventDataModel);
                        }
                    }
                }
            }
            var eventManagementService = Editor.Hierarchy.Hierarchy.eventManagementService;
            foreach (var eventDataModel in modifiedEventDataModels)
            {
                eventManagementService.SaveEvent(eventDataModel);
            }
        }

        /// <summary>
        ///場所移動の座標指定を起動する
        ///座標の受け渡し先Enumが入る
        /// </summary>
        /// <param name="pointDelivery"></param>
        /// <param name="script"></param>
        public void StartMapPointMode(
            PointDelivery pointDelivery,
            object script = null
        ) {
            //Mapに関する干渉の停止
            _drawMode = DrawMode.None;
            //座標の受け渡しへ移行
            _isPointDelivery = 1;
            //受け渡し先の指定
            _pointDelivery = pointDelivery;

            //入ってきた受け渡し先の保持
            _deliveryAddressee = script;
        }

        public void StopMapPointMode() {
            //座標の受け渡しを停止
            _isPointDelivery = 0;
        }

        /// <summary>
        /// カーソル位置を設定する
        /// </summary>
        public void SetCursorPos(Vector3Int cellPos, Action<int, int> action = null) {
            if (_selectedPos == cellPos)
            {
                return;
            }

            if (_tilemapLayers == null)
            {
                return;
            }

            var tilemap = _tilemapLayers[(int)TilemapLayer.Type.Cursor].tilemap;

            if (tilemap != null)
            {
                tilemap.SetTile(_selectedPos, null);
                _selectedPos = new Vector3Int(cellPos.x, cellPos.y, _selectedZindex);
                tilemap.SetTile(_selectedPos, AssetDatabase.LoadAssetAtPath<TileBase>(SelectedMapMassImg));
            }

            MoveGameObjectToPreviewScene(_gridGameObject);
            Render();

            if (action != null)
            {
                _cursorPosChangeAction = action;
            }

            if (_cursorPosChangeAction != null)
            {
                _cursorPosChangeAction(cellPos.x, cellPos.y);
            }
        }

        /// <summary>
        /// EventEditCanvasが親から切り離されたときのコールバック。イベント編集でなくなったので、イベントリストウィンドウを閉じる。
        /// </summary>
        /// <param name="e"></param>
        private void DetachFromPanelEventCallback(DetachFromPanelEvent e) {
            Editor.Hierarchy.Hierarchy.CloseEventListWindow();
        }

        public void UnregisterDetachFromPanelEvent() {
            this.UnregisterCallback<DetachFromPanelEvent>(DetachFromPanelEventCallback);
        }

    }
}