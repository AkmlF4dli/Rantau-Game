using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement;
using RPGMaker.Codebase.Runtime.Common.Component.Hud;
using RPGMaker.Codebase.Runtime.Common.Component.Hud.Display;
using RPGMaker.Codebase.Runtime.Common.Component.Hud.GameProgress;
using RPGMaker.Codebase.Runtime.Common.Component.Hud.Message;
using RPGMaker.Codebase.Runtime.Common.Component.Hud.PostEffect;
using RPGMaker.Codebase.Runtime.Common.Component.Hud.Minimap;
using RPGMaker.Codebase.Runtime.Common.Enum;
using System;
using System.Collections.Generic;
using UnityEngine;
using Display = RPGMaker.Codebase.Runtime.Common.Component.Hud.Display.Display;
using Object = UnityEngine.Object;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Common.Component
{
    /**
     * HUD（ヘッドアップディスプレイ）系のUIを制御するクラス
     * たとえばメッセージウィンドウの処理などをこのクラスで行う
     */
    public class HudHandler
    {
        // データプロパティ
        //--------------------------------------------------------------------------------------------------------------
        DatabaseManagementService _databaseManagementService = new DatabaseManagementService();

        // 表示要素プロパティ
        //--------------------------------------------------------------------------------------------------------------
        private GameObject            _rootGameObject;
        private MessageWindow         _messageWindow;
        private int                   _messageWindowPosition;
        private MessageInputNumber    _inputNumWindow;
        private ItemWindow            _itemWindow;
        private MessageInputSelect    _inputSelectWindow;
        private MessageTextScroll     _messageTextScroll;
        private MapChangeName         _mapChangeName;
        
        private Picture               _picture;
        private Movie                 _movie;
        private Display               _display;
        
        private Display               _sceneDisplay;

        private MinimapController     _minimapController;

        private GameTimer _gameTimer;
        private GameObject _timerObject;

        // methods
        //--------------------------------------------------------------------------------------------------------------
        /**
         * コンストラクタ
         */
        public HudHandler(GameObject rootGameObject) {
            _rootGameObject = rootGameObject;
        }
        
        //全てに対し削除の実施
        //rootとmessageを除く
        public void AllDestroy() {
            if(_inputNumWindow != null)
                Object.Destroy(_inputNumWindow.gameObject);
            if(_itemWindow != null)
                Object.Destroy(_itemWindow.gameObject);
            if (_inputSelectWindow != null)
            {
                Object.Destroy(_inputSelectWindow.gameObject);
                _inputSelectWindow = null;
            }
            if(_messageTextScroll != null)
                Object.Destroy(_messageTextScroll.gameObject);
            if(_mapChangeName != null)
                Object.Destroy(_mapChangeName.gameObject);
            if(_picture != null)
                Object.Destroy(_picture.gameObject);
            if(_movie != null)
                Object.Destroy(_movie.gameObject);
            if(_display != null)
                Object.Destroy(_display.gameObject);
        }

        // メッセージウィンドウ制御
        //--------------------------------------------------------------------------------------------------------------
        public bool IsMessageWindowActive() {
            return _messageWindow != null;
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void OpenMessageWindow() {
#else
        public async Task OpenMessageWindow() {
#endif
            if (IsMessageWindowActive())
            {
                CloseMessageWindow();
            }
            _messageWindow = (new GameObject()).AddComponent<MessageWindow>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _messageWindow.Init();
#else
            await _messageWindow.Init();
#endif
            _messageWindow.transform.position = new Vector3(0, 0, -9);
            _messageWindow.gameObject.transform.SetParent(_rootGameObject.transform);
        }

        public void CloseMessageWindow() {
            if (_messageWindow == null) return;
            _messageWindow.Destroy();
            Object.Destroy(_messageWindow.gameObject);
            _messageWindow = null;
        }
        
        public void ShowFaceIcon(string iconPath) {
            _messageWindow?.ShowFaceIcon(iconPath);
        }

        public void ShowName(string actorName) {
            _messageWindow?.ShowName(actorName);
        }

        public void ShowPicture(string pictureName) {
            _messageWindow?.ShowPicture(pictureName);
        }

        public void SetShowMessage(string message) {
            _messageWindow?.ShowMessage(message);
        }

        public void SetMessageWindowColor(int kind) {
            _messageWindow?.SetWindowColor(kind);
        }

        public void SetMessageWindowPos(int kind) {
            _messageWindowPosition = kind;
            _messageWindow?.SetWindowPos(kind);
        }

        public int GetMessageWindowPos() {
            return _messageWindowPosition;
        }

        public void Next() {
            _messageWindow.Next();
        }

        public void NextMessage(Action action) {
            if (!IsMessageWindowActive())
                return;

            if ( _messageWindow.NextMessage())
            {
                action.Invoke();
                CloseMessageWindow();
            }
        }

        public bool IsInputWait() {
            return _messageWindow?.IsWait() ?? false;
        }

        public bool IsInputEnd() {
            return _messageWindow?.IsEnd() ?? false;
        }

        public bool IsNotWaitInput() {
            return _messageWindow?.IsNotWaitInput() ?? false;
        }

        public void SetIsNotWaitInput(bool flg) {
            _messageWindow?.SetIsNotWaitInput(flg);
        }

        // 桁数入力ウィンドウ制御
        //--------------------------------------------------------------------------------------------------------------
        public bool IsInputNumWindowActive() {
            return _inputNumWindow != null;
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public MessageInputNumber OpenInputNumWindow(string numDigits) {
#else
        public async Task<MessageInputNumber> OpenInputNumWindow(string numDigits) {
#endif
            if (IsInputNumWindowActive())
            {
                CloseInputNumWindow();
            }

            _inputNumWindow = (new GameObject()).AddComponent<MessageInputNumber>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _inputNumWindow.Init(int.Parse(numDigits));
#else
            await _inputNumWindow.Init(int.Parse(numDigits));
#endif
            _inputNumWindow.transform.position = new Vector3(0, 0, -9);
            _inputNumWindow.gameObject.transform.SetParent(_rootGameObject.transform);
            return _inputNumWindow;
        }

        public void CloseInputNumWindow() {
            if (_inputNumWindow == null) return;

            Object.Destroy(_inputNumWindow.gameObject);
            _inputNumWindow = null;
        }

        public int InputNumWindowOperation(HandleType type) {
            return _inputNumWindow.Process(type);
        }
        
        //数値入力の今の値
        public int InputNumNumber() {
            return _inputNumWindow.GetNowNumber();
        }

        // 所持アイテムウィンドウ制御
        //--------------------------------------------------------------------------------------------------------------
        public bool IsItemWindowActive() {
            return _itemWindow != null;
        }

        public void OpenItemWindow() {
            if (IsItemWindowActive())
            {
                CloseItemWindow();
            }

            _itemWindow = (new GameObject()).AddComponent<ItemWindow>();
            _itemWindow.Init();
            _itemWindow.transform.position = new Vector3(0, 0, -9);
            _itemWindow.gameObject.transform.SetParent(_rootGameObject.transform);
        }

        public void CloseItemWindow() {
            if (_itemWindow == null) return;

            Object.Destroy(_itemWindow.gameObject);
            _itemWindow = null;
        }

        // 所持アイテムウィンドウ制御
        //--------------------------------------------------------------------------------------------------------------
        public bool IsInputSelectWindowActive() {
            return _inputSelectWindow != null;
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public MessageInputSelect OpenInputSelectWindow() {
#else
        public async Task<MessageInputSelect> OpenInputSelectWindow() {
#endif
            if (IsInputSelectWindowActive())
            {
                CloseInputSelectWindow();
            }

            _inputSelectWindow = (new GameObject()).AddComponent<MessageInputSelect>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _inputSelectWindow.Init();
#else
            await _inputSelectWindow.Init();
#endif
            _inputSelectWindow.transform.position = new Vector3(0, 0, -9);
            _inputSelectWindow.gameObject.transform.SetParent(_rootGameObject.transform);
            return _inputSelectWindow;
        }

        public void CloseInputSelectWindow() {
            if (_inputSelectWindow == null) return;

            Object.Destroy(_inputSelectWindow.gameObject);
            _inputSelectWindow = null;
        }

        public void SetInputSelectWindowColor(int kind) {
            _inputSelectWindow.SetWindowColor(kind);
        }

        public void ActiveSelectFrame(string text) {
            _inputSelectWindow.ActiveSelectFrame(text);
        }

        public int GetSelectNum() {
            return _inputSelectWindow.GetSelectNum();
        }

        public void SetInputSelectWindowPos(int kind) {
            RectTransform Frame = _inputSelectWindow.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<RectTransform>();
            switch (kind)
            {
                //左
                case 0:
                    Frame.anchorMax = new Vector2(0.0f,0.5f);
                    Frame.anchorMin = new Vector2(0.0f,0.5f); 
                    Frame.pivot = new Vector2(0.0f, 1.0f);
                    break;
                //中
                case 1:
                    Frame.anchorMax = new Vector2(0.5f,0.5f);
                    Frame.anchorMin = new Vector2(0.5f,0.5f);
                    Frame.pivot = new Vector2(0.5f, 1.0f);
                    break;
                //右、その他
                case 2:
                default:
                    Frame.anchorMax = new Vector2(1.0f,0.5f);
                    Frame.anchorMin = new Vector2(1.0f,0.5f);
                    Frame.pivot = new Vector2(1.0f, 1.0f);
                    break;
            }

            int y = 195;
            // 文章表示位置によってY座標を設定
            if (HudDistributor.Instance.NowHudHandler().IsMessageWindowActive())
                if (HudDistributor.Instance.NowHudHandler().GetMessageWindowPos() == 0)
                    y = 195;
                else if (HudDistributor.Instance.NowHudHandler().GetMessageWindowPos() == 1)
                    y = -145;
                else
                {
                    y = -185;
                    Frame.pivot = new Vector2(Frame.pivot.x, 0.0f);
                }

            //U86 左表示時に名前表示された場合
            if (kind == 0)
            {
                if (_messageWindow)
                {   
                    if (_messageWindow.IsNameShow())
                    {
                        y = -10;
                    }
                }
            }
            //positionが残るため更新
            _inputSelectWindow.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<RectTransform>().anchoredPosition = 
                new Vector3(
                    0f,
                    y
                );

            //以下はちらつき防止処理
            _inputSelectWindow.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<RectTransform>().localScale = new Vector3(0.0f, 0.0f, 0.0f);
            TimeHandler.Instance.AddTimeAction(0.1f, SetInputSelectWindowPosAft, false);
        }

        private void SetInputSelectWindowPosAft() {
            if (_inputSelectWindow != null)
                _inputSelectWindow.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);
        }

        public void MoveCorsor(HandleType type) {
            _inputSelectWindow?.Process(type);
        }

        // メッセージスクロール
        //--------------------------------------------------------------------------------------------------------------
        public bool IsMessageScrollWindowActive() {
            return _messageTextScroll != null;
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void OpenMessageScrollWindow() {
#else
        public async Task OpenMessageScrollWindow() {
#endif
            if (IsMessageScrollWindowActive())
            {
                CloseMessageScrollWindow();
            }

            _messageTextScroll = (new GameObject()).AddComponent<MessageTextScroll>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _messageTextScroll.Init();
#else
            await _messageTextScroll.Init();
#endif
            _messageTextScroll.transform.position = new Vector3(0, 0, -9);
            _messageTextScroll.gameObject.transform.SetParent(_rootGameObject.transform);
        }

        public void CloseMessageScrollWindow() {
            if (_messageTextScroll == null) return;

            Object.Destroy(_messageTextScroll.gameObject);
            _messageTextScroll = null;
        }

        public void SetScrollSpeed(int speed) {
            _messageTextScroll?.SetSpeed(speed);
        }

        public void StartScroll(Action action) {
            _messageTextScroll.StartScroll(action);
        }

        public void SetScrollText(string text) {
            _messageTextScroll.SetScrollText(text);
        }
        public void SetScrollNoFast(bool flg) {
            _messageTextScroll.SetScrollNoFast(flg);
        }

        // ピクチャー関係の処理
        //--------------------------------------------------------------------------------------------------------------
        public bool IsPictureActive() {
            return _picture != null;
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void PictureInit() {
#else
        public async Task PictureInit() {
#endif
            if (!IsPictureActive())
            {
                _picture = (new GameObject()).AddComponent<Picture>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _picture.Init();
#else
                await _picture.Init();
#endif
            }
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void AddPicture(int pictureNumber, string pictureName) {
            _picture.AddPicture(pictureNumber, pictureName);
        }
#else
        public async Task AddPicture(int pictureNumber, string pictureName) {
            await _picture.AddPicture(pictureNumber, pictureName);
        }
#endif

        public void AddPictureParameter(int pictureNumber, List<string> parameters) {
            _picture.AddPictureParameter(pictureNumber, parameters);
        }

        public void SetExecuteCommonEvent(int pictureNumber)
        {
            _picture.SetExecuteCommonEvent(pictureNumber);
        }

        public UnityEngine.UI.Image GetPicture(int pictureNumber) {
            return _picture?.GetPicture(pictureNumber);
        }

        public void SetPivot(int pictureNumber, int pivot) {
            _picture.SetPivot(pictureNumber, pivot);
        }
        
        public void SetAnchor(int pictureNumber, int anchor) {
            _picture.SetAnchor(pictureNumber, anchor);
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SetPosition(int pictureNumber, int type, string x, string y) {
            _picture.SetPosition(pictureNumber, type, x, y);
        }
#else
        public async Task SetPosition(int pictureNumber, int type, string x, string y) {
            await _picture.SetPosition(pictureNumber, type, x, y);
        }
#endif

        public void SetPictureSize(int pictureNumber, int widthDiameter, int heightDiameter) {
            _picture.SetPictureSize(pictureNumber, widthDiameter, heightDiameter);
        }

        public void PlayPictureSize(int pictureNumber, int frame, int widthDiameter, int heightDiameter) {
            _picture.StartChangeSize(pictureNumber, frame, widthDiameter, heightDiameter);
        }

        public void SetPictureOpacity(int pictureNumber, int opacity) {
            _picture.SetPictureOpacity(pictureNumber, opacity);
        }

        public void SetProcessingType(int pictureNumber, int processingType) {
            _picture.SetProcessingType(pictureNumber, processingType);
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void PlayMove(Action action, int pictureNumber, int moveType, int type, string x, string y, int flame, bool toggle) {
            _picture.StartMove(action, pictureNumber, moveType, type, x, y, flame, toggle);
        }
#else
        public async Task PlayMove(Action action, int pictureNumber, int moveType, int type, string x, string y, int flame, bool toggle) {
            await _picture.StartMove(action, pictureNumber, moveType, type, x, y, flame, toggle);
        }
#endif

        public void PlayRotation(int pictureNumber, int rotation) {
            _picture.StartRotation(pictureNumber, rotation);
        }

        public void PlayChangeColor(Action action, Color color, int pictureNumber, float gray, int flame, bool toggle) {
            _picture.StartChangeColor(action, color, pictureNumber, gray, flame, toggle);
        }

        public void DeletePicture(int pictureNumber) {
            _picture.DeletePicture(pictureNumber);
        }

        public void SavePicture() {
            if (IsPictureActive())
            {
                _picture.SavePicture();
            }
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void LoadPicture() {
            PictureInit();
            _picture.LoadPicture();
        }
#else
        public async Task LoadPicture() {
            await PictureInit();
            await _picture.LoadPicture();
        }
#endif

        // 画面関係の処理
        //--------------------------------------------------------------------------------------------------------------
        public bool IsDisplayActive() {
            return _display != null;
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void DisplayInit() {
#else
        public async Task DisplayInit() {
#endif
            if (!IsDisplayActive())
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _display = Display.CreateDisplay();
#else
                _display = await Display.CreateDisplay();
#endif
                _display.Init();
            }
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void DisplayInitByScene() {
#else
        public async Task DisplayInitByScene() {
#endif
            if (_sceneDisplay == null)
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _sceneDisplay = Display.CreateDisplayByScene();
#else
                _sceneDisplay = await Display.CreateDisplayByScene();
#endif
                _sceneDisplay.Init();
                UnityEngine.Object.DontDestroyOnLoad(_sceneDisplay);
            }
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public GameTimer CreateGameTimer() {
#else
        public async Task<GameTimer> CreateGameTimer() {
#endif
            if (_gameTimer == null)
            {
                _gameTimer = new GameObject().AddComponent<GameTimer>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _gameTimer.Init();
#else
                await _gameTimer.Init();
#endif
            }
            return _gameTimer;
        }

        public GameTimer GetGameTimer() {
            return _gameTimer;
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public GameObject TimerInitObject() {
#else
        public async Task<GameObject> TimerInitObject() {
#endif
            if (_timerObject == null)
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _timerObject = Display.CreateTimerObject();
#else
                _timerObject = await Display.CreateTimerObject();
#endif
                _timerObject.name = "Timer";
                UnityEngine.Object.DontDestroyOnLoad(_timerObject);
            }
            return _timerObject;
        }
        
        public void HideFadeImage() {
            _display.HideFadeImage();
        }
        public void FadeOut(Action action, Color fadeColor, float time = 0.5f, bool isScene = false) {
            if (!isScene)
            {
                _display.StartFadeOut(action, fadeColor, time);
            }
            else
            {
                _sceneDisplay.StartFadeOut(action, fadeColor, time);
            }
        }
        public void FadeIn(Action action, bool isInitialize = false, float time = 0.5f, bool isScene = false) {
            if (!isScene)
            {
                _display.StartFadeIn(action, isInitialize, time);
            }
            else
            {
                _sceneDisplay.StartFadeIn(action, isInitialize, time);
            }
        }
        
        /// <summary>
        /// 主にシーン遷移専用。かならず画像を塗り潰してからフェードインする
        /// </summary>
        /// <param name="action"></param>
        /// <param name="isInitialize"></param>
        /// <param name="time"></param>
        /// <param name="isScene"></param>
        public void FadeInFixedBlack(Action action, bool isInitialize = false, float time = 0.5f, bool isScene = false) {
            if (!isScene)
            {
                _display.StartFadeIn(action, isInitialize, time);
            }
            else
            {
                _sceneDisplay.SetFadeImageColor(UnityEngine.Color.black);
                _sceneDisplay.StartFadeIn(action, isInitialize, time);
            }
        }
        
        
        public void ChangeColor(Action action, Color color, float gray, float flame, bool wait) {
            _display.DisplayChangeColor(action, color, gray, flame, wait);
        }
        public void Flash(Action action, Color color, int gray, int flame, bool wait, string evetId) {
            _display.DisplayFlash(action, color, gray, flame, wait, evetId);
        }
        public void Shake(Action action, int intensity, int speed, int flame, bool wait) {
            _display.DisplayShake(action, intensity, speed, flame, wait);
        }
        public void ChangeWeather(Action action, int type, int value, float flame, bool wait) {
            _display.DisplayWeather(action, type, value, flame, wait);
        }

        /**
         * ポストエフェクトを適用
         * @param action コールバック
         * @param type エフェクトの種類
         * @param persistent 永続的なエフェクトかどうか
         * @param frame エフェクトのフレーム数
         * @param param エフェクトのパラメータ
         * @param wait エフェクトの終了を待つかどうか
         */
        public void ApplyPostEffect(Action action, int type, bool persistent, float frame, int[] param, bool wait)
        {
            _rootGameObject.GetComponent<PostEffect>().Apply(action, type, persistent, frame, param, wait);
        }

        /**
         * セーブデータからポストエフェクトを適用
         * @param type エフェクトの種類
         * @param param エフェクトのパラメータ
         */
        public void RestorePostEffect(int type, int[] param)
        {
            _rootGameObject.GetComponent<PostEffect>().Apply(
                null,
                type,
                true,
                0,
                param,
                false,
                true
            );
        }
        
        /**
         * ポストエフェクトをすべて削除
         * @param action コールバック
         * @return 削除できたらtrue
         */
        public bool RemovePostEffect(Action action = null)
        {
            return _rootGameObject.GetComponent<PostEffect>().RemoveAll(action);
        }

        //ミニマップ表示/非表示
        //---------------------------------------------------------------------------
        /// <summary>
        /// セーブデータのミニマップ設定に合わせてミニマップの表示・非表示を行う。
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SetupMinimap() {
#else
        public async Task SetupMinimap() {
#endif
            HideMinimap();

            var minimap = DataManager.Self().GetRuntimeSaveDataModel().runtimeScreenDataModel.minimap;
            if (minimap.show)
            {
                var go = GameObject.Instantiate(
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<GameObject>(
#else
                    await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<GameObject>(
#endif
                        "Assets/RPGMaker/Codebase/Runtime/Map/Asset/Prefab/Minimap.prefab"),
                    new Vector3(0.0f, 0.0f, 0.0f),
                    Quaternion.identity
                );
                go.transform.SetParent(_rootGameObject.transform);
                _minimapController = go.GetComponent<MinimapController>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _minimapController.Init();
#else
                await _minimapController.Init();
#endif
            }
        }


        /// <summary>
        /// ミニマップを非表示にする。
        /// </summary>
        public void HideMinimap() {
            if (_minimapController != null)
            {
                Object.Destroy(_minimapController.gameObject);
                _minimapController = null;
            }
        }

        //マップ名表示/非表示
        //---------------------------------------------------------------------------
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void PlayChangeMapName() {
#else
        public async Task PlayChangeMapName() {
#endif
            //表示前に前のものが残っていたら削除
            ClosePlayChangeMapName();
            
            _mapChangeName = new GameObject().AddComponent<MapChangeName>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _mapChangeName.GetComponent<MapChangeName>().Init();
#else
            await _mapChangeName.GetComponent<MapChangeName>().Init();
#endif
        }
        
        public void ClosePlayChangeMapName() {
            if (_mapChangeName == null) return;

            Object.Destroy(_mapChangeName.gameObject);
            _mapChangeName = null;
        }

        // ムービー関係の処理
        //--------------------------------------------------------------------------------------------------------------
        public bool IsMovieActive() {
            return _movie != null;
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void MovieInit() {
#else
        public async Task MovieInit() {
#endif
            if (!IsMovieActive())
            {
                _movie = (new GameObject()).AddComponent<Movie>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _movie.Init();
#else
                await _movie.Init();
#endif
            }
        }

        public void AddMovie(string movieName, Action callBack) {
            _movie.AddMovie(movieName, callBack);
        }
    }
}