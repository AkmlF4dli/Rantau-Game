using RPGMaker.Codebase.Runtime.Common.Enum;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RPGMaker.Codebase.Runtime.Common
{
    public class InputSystemState : MonoBehaviour
    {
        private Vector2                      _move;
        private Dictionary<HandleType, bool> _inputSystemState = new Dictionary<HandleType, bool>();

        class InputState
        {
            const int RepeatWait = 24;
            const int RepeatInterval = 6;
            public bool pressed;
            public int pressedTime;
            public bool onPressed;
            public bool onReleased;
            public bool isRepeat { get { return pressed && pressedTime >= RepeatWait && pressedTime % RepeatInterval == 0; } }
            public void Update(bool pressed) {
                onPressed = pressed && !this.pressed;
                onReleased = !pressed && this.pressed;
                pressedTime = !pressed ? 0 : pressedTime + 1;
                this.pressed = pressed;
            }
        }
        /// <summary>
        /// 入力システムからの最新の入力を保持
        /// </summary>
        private Dictionary<HandleType, bool> _latestInputDic = new Dictionary<HandleType, bool>();
        /// <summary>
        /// 通常イベント向けの入力状態を保持
        /// </summary>
        private Dictionary<HandleType, InputState> _inputStateDic = new Dictionary<HandleType, InputState>();
        /// <summary>
        /// 自動実行、並列実行向けにウェイトが入ってフレームがスキップされることがあるイベント向けの入力状態を保持
        /// </summary>
        private Dictionary<HandleType, InputState> _waitInputStateDic = new Dictionary<HandleType, InputState>();
        /// <summary>
        /// イベント処理で参照させる入力状態辞書インスタンス
        /// </summary>
        private Dictionary<HandleType, InputState> _currentInputStateDic = null;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        InputSystemState() {
            //上下左右キー
            _inputSystemState.Add(HandleType.Left, false);
            _inputSystemState.Add(HandleType.Right, false);
            _inputSystemState.Add(HandleType.Up, false);
            _inputSystemState.Add(HandleType.Down, false);

            //決定キー、戻るキー
            _inputSystemState.Add(HandleType.Decide, false);
            _inputSystemState.Add(HandleType.Back, false);

            //上下左右のキーダウン
            _inputSystemState.Add(HandleType.LeftKeyDown, false);
            _inputSystemState.Add(HandleType.RightKeyDown, false);
            _inputSystemState.Add(HandleType.UpKeyDown, false);
            _inputSystemState.Add(HandleType.DownKeyDown, false);

            //シフトキー
            _inputSystemState.Add(HandleType.LeftShiftDown, false);
            _inputSystemState.Add(HandleType.LeftShiftUp, false);

            //メニュー操作
            _inputSystemState.Add(HandleType.RightClick, false);

            //ページ切り替え
            _inputSystemState.Add(HandleType.PageLeft, false);
            _inputSystemState.Add(HandleType.PageRight, false);

            _latestInputDic.Clear();
            _inputStateDic.Clear();
            _waitInputStateDic.Clear();
            foreach (HandleType handleType in System.Enum.GetValues(typeof(HandleType)))
            {
                _latestInputDic.Add(handleType, false);
                _inputStateDic.Add(handleType, new InputState());
                _waitInputStateDic.Add(handleType, new InputState());
            }
            _currentInputStateDic = _inputStateDic;

            //自分をInputHandlerに登録
            InputHandler.SetInputSystemState(this);
        }

        HandleType _postFrameMoveDirection = default;
        HandleType _lastMoveDirection = default;

        /// <summary>
        /// イベントが参照する入力状態インスタンスを切り替える。
        /// </summary>
        /// <param name="normal">trueの場合、通常イベント用に切り替える</param>
        public void SwitchInputStateDic(bool normal) {
            _currentInputStateDic = normal ? _inputStateDic : _waitInputStateDic;
        }

        /// <summary>
        /// このタイミングでのInputSystemからの入力状態を保持して、イベント処理中に入力が変化しないようにする。
        /// </summary>
        public void UpdateInputState() {
            foreach (var item in _inputStateDic)
            {
                item.Value.Update(_latestInputDic[item.Key]);
            }
        }

        /// <summary>
        /// このタイミングでのInputSystemからの入力状態を保持して、自動実行・並列実行イベント処理中に入力が変化しないようにする。
        /// </summary>
        public void UpdateWaitInputState() {
            foreach (var item in _waitInputStateDic)
            {
                item.Value.Update(_latestInputDic[item.Key]);
            }
        }

        public bool GetHandleState(HandleType handleType, InputType inputType) {
            bool state = false;
            switch (inputType)
            {
                case InputType.Down:
                    state = _currentInputStateDic[handleType].onPressed;
                    break;
                case InputType.Up:
                    state = _currentInputStateDic[handleType].onReleased;
                    break;
                case InputType.Press:
                    state = _currentInputStateDic[handleType].pressed;
                    break;
                case InputType.Repeat:
                    state = _currentInputStateDic[handleType].isRepeat;
                    break;
            }
            return state;
        }

        /// <summary>
        /// 十字キー
        /// </summary>
        /// <param name="context"></param>
        public void OnMove(InputAction.CallbackContext context)
        {
            //初期化
            _inputSystemState[HandleType.Left] = false;
            _inputSystemState[HandleType.Right] = false;
            _inputSystemState[HandleType.Up] = false;
            _inputSystemState[HandleType.Down] = false;
            _latestInputDic[HandleType.Left] = false;
            _latestInputDic[HandleType.Right] = false;
            _latestInputDic[HandleType.Up] = false;
            _latestInputDic[HandleType.Down] = false;

            //十字キーの大きさを取得
            _move = context.ReadValue<Vector2>();

            if(Mathf.Abs(_move.x) >= Mathf.Abs(_move.y))
            {
                //左右への移動
                if (_move.x <= -0.5)
                {
                    _inputSystemState[HandleType.Left] = true;
                    _latestInputDic[HandleType.Left] = true;
                    _lastMoveDirection = HandleType.LeftKeyDown;
                }
                else if (_move.x >= 0.5)
                {
                    _inputSystemState[HandleType.Right] = true;
                    _latestInputDic[HandleType.Right] = true;
                    _lastMoveDirection = HandleType.RightKeyDown;
                } else
                {
                    _lastMoveDirection = HandleType.None;
                }
            }
            else
            {
                if (_move.y <= -0.5)
                {
                    _inputSystemState[HandleType.Down] = true;
                    _latestInputDic[HandleType.Down] = true;
                    _lastMoveDirection = HandleType.DownKeyDown;
                }
                else if (_move.y >= 0.5)
                {
                    _inputSystemState[HandleType.Up] = true;
                    _latestInputDic[HandleType.Up] = true;
                    _lastMoveDirection = HandleType.UpKeyDown;
                }
                else
                {
                    _lastMoveDirection = HandleType.None;
                }
            }
        }

        /// <summary>
        /// 決定キー
        /// </summary>
        /// <param name="context"></param>
        public void OnFire(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed)
            {
                _inputSystemState[HandleType.Decide] = true;
                _latestInputDic[HandleType.Decide] = true;
            }
            else if (context.phase == InputActionPhase.Canceled)
            {
                _inputSystemState[HandleType.Decide] = false;
                _latestInputDic[HandleType.Decide] = false;
            }
        }


        /// <summary>
        /// キャンセルキー
        /// </summary>
        /// <param name="context"></param>
        public void OnCancel(InputAction.CallbackContext context) {
            if (context.phase == InputActionPhase.Performed)
            {
                _inputSystemState[HandleType.Back] = true;
                _latestInputDic[HandleType.Back] = true;
            }
            else if (context.phase == InputActionPhase.Canceled)
            {
                _inputSystemState[HandleType.Back] = false;
                _latestInputDic[HandleType.Back] = false;
            }
        }

        /// <summary>
        /// メニュー
        /// </summary>
        /// <param name="context"></param>
        public void OnMenu(InputAction.CallbackContext context) {
            if (context.phase == InputActionPhase.Performed)
            {
                _inputSystemState[HandleType.RightClick] = true;
                _latestInputDic[HandleType.RightClick] = true;
            }
            else if (context.phase == InputActionPhase.Canceled)
            {
                _inputSystemState[HandleType.RightClick] = false;
                _latestInputDic[HandleType.RightClick] = false;
            }
        }

        /// <summary>
        /// ダッシュ
        /// </summary>
        /// <param name="context"></param>
        public void OnDash(InputAction.CallbackContext context) {
            if (context.phase == InputActionPhase.Performed)
            {
                _latestInputDic[HandleType.LeftShiftDown] = true;
            }
            else if (context.phase == InputActionPhase.Canceled)
            {
                _latestInputDic[HandleType.LeftShiftDown] = false;
            }
        }

        public void OnLeftB(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed)
            {
                _inputSystemState[HandleType.PageLeft] = true;
                _latestInputDic[HandleType.PageLeft] = true;
            }
            else if (context.phase == InputActionPhase.Canceled)
            {
                _inputSystemState[HandleType.PageLeft] = false;
                _latestInputDic[HandleType.PageLeft] = false;
            }
        }

        public void OnRightB(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed)
            {
                _inputSystemState[HandleType.PageRight] = true;
                _latestInputDic[HandleType.PageRight] = true;
            }
            else if (context.phase == InputActionPhase.Canceled)
            {
                _inputSystemState[HandleType.PageRight] = false;
                _latestInputDic[HandleType.PageRight] = false;
            }
        }

        
        public void UpdateOnWatch() {
            _inputSystemState[HandleType.RightKeyDown] = false;
            _inputSystemState[HandleType.LeftKeyDown] = false;
            _inputSystemState[HandleType.UpKeyDown] = false;
            _inputSystemState[HandleType.DownKeyDown] = false;

            if (_lastMoveDirection != _postFrameMoveDirection && 
                _lastMoveDirection != HandleType.None)
            {
                _inputSystemState[_lastMoveDirection] = true;
            }

            _postFrameMoveDirection = _lastMoveDirection;
        }

        /// <summary>
        /// 渡されたHandleTypeの、現在の状態を返却する
        /// </summary>
        /// <param name="handleType">HandleType</param>
        /// <returns>押下されている時true</returns>
        public bool CurrentInputSystemState(HandleType handleType) {
            bool value = false;
            switch (handleType) {
                //連続でキー入力を受け付けるもの
                case HandleType.Left:
                case HandleType.Right:
                case HandleType.Up:
                case HandleType.Down:
                    return _inputSystemState[handleType];
                //1回発火したら終了するもの
                case HandleType.Decide:
                case HandleType.Back:
                case HandleType.RightClick:
                case HandleType.LeftKeyDown:
                case HandleType.RightKeyDown:
                case HandleType.UpKeyDown:
                case HandleType.DownKeyDown:
                case HandleType.LeftShiftDown:
                case HandleType.LeftShiftUp:
                case HandleType.PageLeft:
                case HandleType.PageRight:
                    value = _inputSystemState[handleType];
                    _inputSystemState[handleType] = false;
                    return value;
            }

            return false;
        }

        /// <summary>
        /// 特定のキーがこのフレームで押されたかどうか（OnPress）
        /// </summary>
        /// <param name="handleType"></param>
        /// <returns></returns>
        public bool OnDown(HandleType handleType) {
            PlayerInput pInput = GetComponent<PlayerInput>();
            if (handleType == HandleType.Left || handleType == HandleType.Right || handleType == HandleType.Up || handleType == HandleType.Down)
            {
                return _inputSystemState[handleType];
            }

            InputAction action;
            if (handleType == HandleType.Decide) action = pInput.actions.FindAction("Fire");
            else if (handleType == HandleType.Back) action = pInput.actions.FindAction("Cancel");
            else if (handleType == HandleType.RightClick) action = pInput.actions.FindAction("Menu");
            else if (handleType == HandleType.LeftShiftDown) action = pInput.actions.FindAction("Dash");
            else if (handleType == HandleType.PageLeft && _inputSystemState[HandleType.PageLeft])
            {
                _inputSystemState[HandleType.PageLeft] = false;
                return true;
            }
            else if (handleType == HandleType.PageRight && _inputSystemState[HandleType.PageRight])
            {
                _inputSystemState[HandleType.PageRight] = false;
                return true;
            }
            else return false;
            return action.WasPressedThisFrame() && action.IsPressed();
        }

        /// <summary>
        /// 特定のキーがこのフレームで離されたかどうか（OnPress）
        /// </summary>
        /// <param name="handleType"></param>
        /// <returns></returns>
        public bool OnUp(HandleType handleType) {
            PlayerInput pInput = GetComponent<PlayerInput>();
            if (handleType == HandleType.Left || handleType == HandleType.Right || handleType == HandleType.Up || handleType == HandleType.Down)
            {
                return _inputSystemState[handleType];
            }

            InputAction action;
            if (handleType == HandleType.Decide) action = pInput.actions.FindAction("Fire");
            else if (handleType == HandleType.Back) action = pInput.actions.FindAction("Cancel");
            else if (handleType == HandleType.RightClick) action = pInput.actions.FindAction("Menu");
            else if (handleType == HandleType.LeftShiftDown) action = pInput.actions.FindAction("Dash");
            else return false;

            return action.WasReleasedThisFrame();
        }

        /// <summary>
        /// 特定のキーがこのフレームで押され続けているかどうか（OnPress）
        /// </summary>
        /// <param name="handleType"></param>
        /// <returns></returns>
        public bool OnPress(HandleType handleType) {
            PlayerInput pInput = GetComponent<PlayerInput>();
            if (handleType == HandleType.Left || handleType == HandleType.Right || handleType == HandleType.Up || handleType == HandleType.Down)
            {
                return _inputSystemState[handleType];
            }

            InputAction action;
            if (handleType == HandleType.Decide) action = pInput.actions.FindAction("Fire");
            else if (handleType == HandleType.Back) action = pInput.actions.FindAction("Cancel");
            else if (handleType == HandleType.RightClick) action = pInput.actions.FindAction("Menu");
            else if (handleType == HandleType.LeftShiftDown) action = pInput.actions.FindAction("Dash");
            else return false;
            return action.IsPressed();
        }
    }
}
