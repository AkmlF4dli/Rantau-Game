//#define USE_TRACE_PRINT

#if USE_TRACE_PRINT
using RPGMaker.Codebase.CoreSystem.Helper;
#endif
using RPGMaker.Codebase.Runtime.Common.Enum;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace RPGMaker.Codebase.Runtime.Common
{
    public static class InputHandler
    {
        // データプロパティ
        //--------------------------------------------------------------------------------------------------------------
        private static readonly Dictionary<HandleType, List<Action>> _inputActions = new();
        private static readonly List<HandleType> _AnyKeys = new List<HandleType>() {
            HandleType.Decide,
            HandleType.Back,
            HandleType.Up,
            HandleType.Down,
            HandleType.Right,
            HandleType.Left,
            HandleType.LeftClick,
            HandleType.RightClick,
            HandleType.LeftShiftDown
        };

        /// <summary>
        /// 現在のInputSystemState 各Sceneで1つだけ存在する
        /// </summary>
        private static InputSystemState _currentInputSystemState;

        /// <summary>
        /// 現在のInputSystemStateの登録
        /// </summary>
        /// <param name="currentInputSystemState">現在のInputSystemState</param>
        public static void SetInputSystemState(InputSystemState currentInputSystemState) {
            _currentInputSystemState = currentInputSystemState;
        }

        public static void UpdateInputSystemState() {
            _currentInputSystemState?.UpdateInputState();
        }

        public static void UpdateWaitInputSystemState() {
            _currentInputSystemState?.UpdateWaitInputState();
        }

        public static void SwitchInputSystemState(bool normal) {
            _currentInputSystemState?.SwitchInputStateDic(normal);
        }

        // methods
        //--------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 入力監視
        /// </summary>
        public static void Watch() {
            // InputSystemからのインプットのハンドリング
            if (_currentInputSystemState == null) return;

            // キーチェック前の事前処理
            _currentInputSystemState.UpdateOnWatch();

            // 連続でキーを受け付けるもの
            if (_currentInputSystemState.CurrentInputSystemState(HandleType.Left))
            {
                Handle(HandleType.Left);
            }
            else if (_currentInputSystemState.CurrentInputSystemState(HandleType.Right))
            {
                Handle(HandleType.Right);
            }
            else if (_currentInputSystemState.CurrentInputSystemState(HandleType.Up))
            {
                Handle(HandleType.Up);
            }
            else if (_currentInputSystemState.CurrentInputSystemState(HandleType.Down))
            {
                Handle(HandleType.Down);
            }
            else if (_currentInputSystemState.CurrentInputSystemState(HandleType.Decide))
            {
                Handle(HandleType.Decide);
            }
            else if (_currentInputSystemState.CurrentInputSystemState(HandleType.Back))
            {
                Handle(HandleType.Back);
            }

            // 1回しかキーを受け付けないもの
            if (_currentInputSystemState.CurrentInputSystemState(HandleType.LeftKeyDown))
            {
                Handle(HandleType.LeftKeyDown);
            }
            else if (_currentInputSystemState.CurrentInputSystemState(HandleType.RightKeyDown))
            {
                Handle(HandleType.RightKeyDown);
            }
            else if (_currentInputSystemState.CurrentInputSystemState(HandleType.UpKeyDown))
            {
                Handle(HandleType.UpKeyDown);
            }
            else if (_currentInputSystemState.CurrentInputSystemState(HandleType.DownKeyDown))
            {
                Handle(HandleType.DownKeyDown);
            }

            // マウスイベント系
            // 左クリックのみ、マップ移動等で利用しているため、Decideとは別途で発火する
            if (Input.GetMouseButtonUp(0))
            {
                Handle(HandleType.LeftClick);
            }
        }

        /// <summary>
        /// 引数の入力状態を返却する
        /// </summary>
        /// <param name="handleType"></param>
        /// <param name="inputType"></param>
        /// <returns></returns>
        public static bool GetHandleState(HandleType handleType, InputType inputType) {
            return _currentInputSystemState.GetHandleState(handleType, inputType);
        }

        /// <summary>
        /// 特定のキーがこのフレームで押下されたかどうか
        /// </summary>
        /// <param name="handleType">HandleType</param>
        /// <returns>押下されている場合にtrue</returns>
        public static bool OnDown(HandleType handleType) {
            if (_currentInputSystemState == null)
            {
                return false;
            }
            if (handleType == HandleType.LeftClick && Input.GetMouseButtonUp(0))
            {
                return true;
            }
            return _currentInputSystemState.OnDown(handleType);
        }

        /// <summary>
        /// 特定のキーがこのフレームで離されたされたかどうか
        /// </summary>
        /// <param name="handleType">HandleType</param>
        /// <returns>押下されている場合にtrue</returns>
        public static bool OnUp(HandleType handleType) {
            if (_currentInputSystemState == null)
            {
                return false;
            }
            return _currentInputSystemState.OnUp(handleType);
        }

        /// <summary>
        /// 特定のキーがこのフレームで押され続けているかどうか
        /// </summary>
        /// <param name="handleType">HandleType</param>
        /// <returns>押下されている場合にtrue</returns>
        public static bool OnPress(HandleType handleType) {
            if (_currentInputSystemState == null)
            {
                return false;
            }
            return _currentInputSystemState.OnPress(handleType);
        }

        /// <summary>
        /// 何かしら入力が行われたかチェックする
        /// </summary>
        /// <returns></returns>
        public static bool IsAnyPressed() {
            return _AnyKeys.Any(key => OnDown(key));
        }

        //---------下復活-----------

        /**
         * inputに対するactionを登録する
         */
        public static void RegisterInputAction(HandleType handleType, Action action) {
            if (!_inputActions.ContainsKey(handleType)) _inputActions[handleType] = new List<Action>();

            //既に登録済みであれば、新規登録を行わない
            for (var i = 0; i < _inputActions[handleType].Count; i++)
                if (_inputActions[handleType][i] == action)
                    return;

            //登録が無ければ、登録する
            _inputActions[handleType].Add(action);
            TracePrint("Add", handleType, action);
        }

        /**
         * 登録したactionを削除する
         */
        public static void DeregisterInputAction(HandleType handleType, Action action) {
            if (!_inputActions.ContainsKey(handleType)) return;

            //既に登録済みであれば、削除する
            for (var i = 0; i < _inputActions[handleType].Count; i++)
                if (_inputActions[handleType][i] == action)
                {
                    _inputActions[handleType].RemoveAt(i);
                    TracePrint("Del", handleType, action);
                    break;
                }
        }

        /**
         * 登録したactionを一括で削除
         */
        public static void AllDeregisterInputAction() {
            _inputActions.Clear();
            TracePrint("Clr");
        }

        /**
         * inputごとに登録されたActionsを実行する
         */
        public static void Handle(HandleType handleType) {
            if (!_inputActions.ContainsKey(handleType) || _inputActions[handleType].Count == 0) return;

            // 登録されているActionsを順繰りにすべて実行する
            // ※ foreach処理中にactionが解除される場合があるとエラーになるのでcloneで実行する
            var actionsClone = _inputActions[handleType].ToList();
            actionsClone.ForEach(action => action.Invoke());
        }

        [Conditional("USE_TRACE_PRINT")]
        private static void TracePrint(string operateName, HandleType operateHandleType, Action operateAction)
        {
#if USE_TRACE_PRINT
            DebugUtil.Log("");
            DebugUtil.Log($"## {operateName} {ToString(operateHandleType, operateAction)}");
            foreach (var handleType in _inputActions.Keys)
            {
                foreach (var action in _inputActions[handleType])
                {
                    DebugUtil.Log($"_inputActions{ToString(handleType, action)}");
                }
            }

            string ToString(HandleType handleType, Action action)
            {
                return $"[{handleType}] = {action.GetTargetClassMethodName()}";
            }
#endif
        }

        [Conditional("USE_TRACE_PRINT")]
        private static void TracePrint(string operateName)
        {
#if USE_TRACE_PRINT
            DebugUtil.Log("");
            DebugUtil.Log($"## {operateName}");
#endif
        }
    }
}