#define USE_ALTERNATIVE_DOCK_TO

using RPGMaker.Codebase.CoreSystem.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace RPGMaker.Codebase.Editor.Common
{
    /// <summary>
    ///     ウィンドウ同士をドッキング（タブ重なりではなく、上下左右結合）するための処理を扱うクラス.
    ///     based on: https://gist.github.com/Jayatubi/f6cafb4d5a5fcb54b537e79be77aa714
    /// </summary>
    public static class Docker
    {
        public enum DockPosition
        {
            Left,
            Top,
            Right,
            Bottom
        }

        /// <summary>
        ///     Docks the second window to the first window at the given position
        /// </summary>
        /// <param name="childWeight">(0, 1)の範囲の値が指定された時、親(windowIdToDock)と子(windowIdToOpen)のサイズの合計に対する子のサイズの比率がchildWeightになるようサイズ調整される。</param>
        public static void DockTo(EditorWindow parentWindow, EditorWindow childWindow, DockPosition position, float childWeight) {
#if USE_ALTERNATIVE_DOCK_TO
            AlternativeDockTo(childWindow, parentWindow, position, childWeight);
            return;
#else
            var mousePosition = GetFakeMousePosition(parentWindow, position, 30);

            // Translated from Editor/Mono/GUI/DockArea.cs:537
            var assembly = typeof(EditorWindow).Assembly;
            var containerWindow = assembly.GetType("UnityEditor.ContainerWindow");
            var dockArea = assembly.GetType("UnityEditor.DockArea");
            var iDropArea = assembly.GetType("UnityEditor.IDropArea");

            object dropInfo = null;
            object targetView = null;

            var windows = containerWindow
                .GetProperty("windows", BindingFlags.Static | BindingFlags.Public)
                .GetValue(null, null) as object[];

            if (windows != null)
                foreach (var window in windows)
                {
                    var rootSplitView = window.GetType()
                        .GetProperty("rootSplitView", BindingFlags.Instance | BindingFlags.Public)
                        .GetValue(window, null);
                    if (rootSplitView != null)
                    {
                        var method = rootSplitView.GetType()
                            .GetMethod("DragOverRootView", BindingFlags.Instance | BindingFlags.Public);
                        dropInfo = method.Invoke(rootSplitView, new object[] {mousePosition});
                        targetView = rootSplitView;
                    }

                    if (dropInfo == null)
                    {
                        var rootView = window.GetType()
                            .GetProperty("rootView", BindingFlags.Instance | BindingFlags.Public)
                            .GetValue(window, null);
                        var allChildren =
                            rootView.GetType().GetProperty("allChildren", BindingFlags.Instance | BindingFlags.Public)
                                .GetValue(rootView, null) as object[];
                        foreach (var view in allChildren)
                            if (iDropArea.IsAssignableFrom(view.GetType()))
                            {
                                var method = view.GetType().GetMethod("DragOver",
                                    BindingFlags.Instance | BindingFlags.Public);
                                dropInfo = method.Invoke(view, new object[] {parentWindow, mousePosition});
                                if (dropInfo != null)
                                {
                                    targetView = view;
                                    break;
                                }
                            }
                    }

                    if (dropInfo != null) break;
                }

            if (dropInfo != null && targetView != null)
            {
                var otherParent = childWindow.GetType()
                    .GetField("m_Parent", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(childWindow);
                dockArea.GetField("s_OriginalDragSource", BindingFlags.Static | BindingFlags.NonPublic)
                    .SetValue(null, otherParent);
                var method = targetView.GetType().GetMethod("PerformDrop", BindingFlags.Instance | BindingFlags.Public);
                method?.Invoke(targetView, new[] {childWindow, dropInfo, mousePosition});
            }
#endif
        }

#if !USE_ALTERNATIVE_DOCK_TO
        private static Vector2 GetFakeMousePosition(EditorWindow targetWindow, DockPosition position, float padding) {
            // 基本的にはターゲットウィンドウの絶対座標が取得できるのだが、
            // 他とのドッキング状況によってはそのドッキングウィンドウからの相対座標になってしまう場合があり、
            // そうなるとドッキング先がズレてしまう
            var mousePosition = Vector2.zero;

            switch (position)
            {
                case DockPosition.Left:
                    mousePosition = new Vector2(padding, targetWindow.position.size.y / 2);
                    break;
                case DockPosition.Top:
                    mousePosition = new Vector2(targetWindow.position.size.x / 2, padding);
                    break;
                case DockPosition.Right:
                    mousePosition = new Vector2(targetWindow.position.size.x - padding,
                        targetWindow.position.size.y / 2);
                    break;
                case DockPosition.Bottom:
                    mousePosition = new Vector2(targetWindow.position.size.x / 2,
                        targetWindow.position.size.y - padding);
                    break;
            }

            return new Vector2(targetWindow.position.x + mousePosition.x, targetWindow.position.y + mousePosition.y);
        }
#endif

        /// <summary>
        /// ウィンドウを他のウィンドウの指定位置にドッキングさせる。
        /// </summary>
        /// <param name="dropWindow">ドロップしてドッキングさせるウィンドウ。</param>
        /// <param name="dockWindow">ドッキング先ウィンドウ。</param>
        /// <param name="dockPosition">ドッキング先ウィンドウのドッキング位置。</param>
		/// <param name="childWeight">(0, 1)の範囲の値が指定された時、親(windowIdToDock)と子(windowIdToOpen)のサイズの合計に対する子のサイズの比率がchildWeightになるようサイズ調整される。</param>
        /// <remarks>
        /// DockTo()メソッドは多分ドロップ座標の算出が不正確で状況によっては正しくドッキングされないので、
        /// 代替として本メソッドを用意しました。
        /// より正確なドロップ座標の算出は、Unityのソースコードを元にしたGetDropPosition()で実現しています。
        /// 
        /// Unityでは本メソッド用の機能は公開されていないため、以下で実現しています。
        /// ・多くの部分で、C#のリフレクションの機能を使用して、Unity内部のプログラムを強引に実行。
        /// ・GitHubのUnityCsReferenceで公開されているUnityのソースコード(2021.3.9)を元に用意したプログラムを実行。
        /// 上記により、本メソッドはUnityのバージョンが変わった場合、正常に動作しない可能性があります。
        /// 本メソッドの動作を確認したUnityのバージョンは 2021.3.9.f1 となります。
        /// </remarks>
        public static void AlternativeDockTo(
            EditorWindow dropWindow, EditorWindow dockWindow, DockPosition dockPosition, float childWeight)
        {
            //DebugUtil.Log($"##▼▼▼▼ AlternativeDockTo({ToString(dropWindow)}, {ToString(dockWindow)}, {dockPosition})");

            List<object> views = GetViewsWithIgnoreWindowNames( new string[] {"EventSearcher_Main", "RPGMaker.Codebase.Editor.Additional.HierarchyHistoryWindow" });
            object dockWindowView = GetDockWindowView(views, dockWindow);
            if (dockWindowView == null)
            {
                DebugUtil.LogWarning(
                    $"ドッキング先ウィンドウ ({dockWindow.GetType().Name}) のViewが存在しないので、" +
                    $"ドッキングできませんでした。");
                return;
            }

            var dockWindowViewScreenPosition = (Rect)GetProperty(dockWindowView, "screenPosition", BindingFlags.Public);
            Vector2 dropPosition = GetDropPosition(dockWindowViewScreenPosition, dockPosition);

            // ウィンドウView列から、指定の画面座標にドロップ可能なものを見つけて、ドロップする。
            foreach (object view in views)
            {
                //Type type = view.GetType();
                //string name = (string)GetProperty(view, "name", BindingFlags.Public);
                //Rect screenPosition = (Rect)GetProperty(view, "screenPosition", BindingFlags.Public);
                //EditorWindow window = dockAreaType.IsAssignableFrom(view.GetType()) ?
                //    (EditorWindow)GetProperty(view, "actualView", BindingFlags.NonPublic) : null;
                //DebugUtil.Log(
                //    $"##＿＿■■ View : " +
                //    $"type={type}, " +
                //    $"name=\"{name}\", " +
                //    $"position=({ToString(screenPosition)}, " +
                //    $"window={window})");

                if (!iDropAreaType.IsAssignableFrom(view.GetType()))
                {
                    continue;
                }

                object dropInfo = InvokeMethod(
                    view, "DragOver", BindingFlags.Public, new object[] { dockWindow, dropPosition });
                DebugUtil.Log($"##＿＿＿■ view.DragOver(\"{ToString(dockWindow)}\", {dropPosition}) -> {dropInfo}");
                if (dropInfo == null)
                {
                    continue;
                }

                object dropArea = GetField(dropInfo, "dropArea", BindingFlags.Public);
                DebugUtil.Log($"##＿＿＿■ dropInfo.dropArea -> {dropArea}");
                if (dropArea == null)
                {
                    continue;
                }

                PerformDrop(dropWindow, view, dropPosition, dropInfo);
                if (childWeight > 0) {
                    //ウィンドウの高さを調整する。
                    views = GetViews();
                    var dropWindowView = GetDockWindowView(views, dropWindow);
                    dockWindowView = GetDockWindowView(views, dockWindow);
                    var dropPos = (Rect) GetProperty(dropWindowView, "position", BindingFlags.Public);
                    var dockPos = (Rect) GetProperty(dockWindowView, "position", BindingFlags.Public);
                    if (dockPosition == DockPosition.Bottom || dockPosition == DockPosition.Top)
                    {
                        var totalHeight = dockPos.height + dropPos.height;
                        var dropHeight = Mathf.Ceil(totalHeight * childWeight);
                        var dockHeight = totalHeight - dropHeight;
                        dropPos.height = dropHeight;
                        dockPos.height = dockHeight;
                        if (dockPosition == DockPosition.Bottom) {
                            dropPos.y = dockPos.y + dockPos.height;
                        } else {
                            dockPos.y = dropPos.y + dropPos.height;
                        }
                    }
                    else if (dockPosition == DockPosition.Right || dockPosition == DockPosition.Left)
                    {
                        var totalWidth = dockPos.width + dropPos.width;
                        var dropWidth = Mathf.Ceil(totalWidth * childWeight);
                        var dockWidth = totalWidth - dropWidth;
                        dropPos.width = dropWidth;
                        dockPos.width = dockWidth;
                        if (dockPosition == DockPosition.Right)
                        {
                            dropPos.x = dockPos.x + dockPos.width;
                        }
                        else
                        {
                            dockPos.x = dropPos.x + dropPos.width;
                        }
                    }
                    SetProperty(dropWindowView, "position", (object) dropPos, BindingFlags.Public);
                    SetProperty(dockWindowView, "position", (object) dockPos, BindingFlags.Public);
                }
                return;
            }

            DebugUtil.LogWarning($"ドッキングできませんでした ({dropWindow}, {dockWindow}, {dockPosition})。");

        }
        // ドロップを実行する。
        static void PerformDrop(EditorWindow dropWindow, object dockView, Vector2 dropPosition, object dropInfo) {
            object otherParent = GetField(dropWindow, "m_Parent", BindingFlags.NonPublic);
            SetStaticField(dockAreaType, "s_OriginalDragSource", BindingFlags.NonPublic, otherParent);

            InvokeMethod(
                dockView,
                "PerformDrop",
                BindingFlags.Public, new[] { dropWindow, dropInfo, dropPosition });
            DebugUtil.Log(
                $"##▲▲▲▲ view.PerformDrop({ToString(dropWindow)}, {dropInfo}, {dropPosition})");
        }

        /// <summary>
        /// 排除指定を追加したView列挙処理
        /// </summary>
        /// <param name="ignoreWindowNames"></param>
        /// <returns></returns>
        static List<object> GetViewsWithIgnoreWindowNames(string[] ignoreWindowNames) {
            List<object> views = new();
            var windows = GetStaticProperty(containerWindowType, "windows", BindingFlags.Public) as object[];
            foreach (object containerWindow in windows)
            {
                object windowID = GetProperty(containerWindow, "windowID", BindingFlags.NonPublic);
                if (ignoreWindowNames.Contains(windowID))
                {
                    continue;
                }
                object rootView = GetProperty(containerWindow, "rootView", BindingFlags.Public);
                object[] allChildren = (object[]) GetProperty(rootView, "allChildren", BindingFlags.Public);
                //Debug.Log($"containerWindow: {containerWindow}, rootView: {rootView}, {string.Join("/", new List<object>(allChildren).Select(x => $"{x}"))}");
                foreach (object view in allChildren)
                {
                    views.Add(view);
                }
            }
            return views;
        }

        // 全Viewを取得する。
        public static List<object> GetViews() {
            List<object> views = new();
            var windows = GetStaticProperty(containerWindowType, "windows", BindingFlags.Public) as object[];
            foreach (object containerWindow in windows) 
            {
                object rootView = GetProperty(containerWindow, "rootView", BindingFlags.Public);
                object[] allChildren = (object[]) GetProperty(rootView, "allChildren", BindingFlags.Public);
                //Debug.Log($"containerWindow: {containerWindow}, rootView: {rootView}, {string.Join("/", new List<object>(allChildren).Select(x => $"{x}"))}");
                foreach (object view in allChildren)
                {
                    views.Add(view);
                }
            }

            return views;
        }

        // ドッキング先ウィンドウのViewを取得する。
        static object GetDockWindowView(List<object> views, EditorWindow dockWindow) {
            return views.
                Where(view => dockAreaType.IsAssignableFrom(view.GetType())).
                ForceSingleOrDefault(view =>
                    (EditorWindow) GetProperty(view, "actualView", BindingFlags.NonPublic) == dockWindow);
        }

        private static readonly Assembly editorWindowAssembly = typeof(EditorWindow).Assembly;
        private static readonly Type containerWindowType = editorWindowAssembly.GetType("UnityEditor.ContainerWindow");
        private static readonly Type dockAreaType = editorWindowAssembly.GetType("UnityEditor.DockArea");
        private static readonly Type iDropAreaType = editorWindowAssembly.GetType("UnityEditor.IDropArea");

        private static object GetProperty(object instance, string name, BindingFlags bindingFlags)
        {
            return instance.GetType().GetProperty(name, BindingFlags.Instance | bindingFlags).GetValue(instance, null);
        }

        private static void SetProperty(object instance, string name, object value, BindingFlags bindingFlags) {
            instance.GetType().GetProperty(name, BindingFlags.Instance | bindingFlags).SetValue(instance, value);
        }

        private static object GetStaticProperty(Type type, string name, BindingFlags bindingFlags)
        {
            return type.GetProperty(name, BindingFlags.Static | bindingFlags).GetValue(null, null);
        }

        private static object GetField(object instance, string name, BindingFlags bindingFlags)
        {
            return instance.GetType().GetField(name, BindingFlags.Instance | bindingFlags).GetValue(instance);
        }

        private static void SetStaticField(Type type, string name, BindingFlags bindingFlags, object value)
        {
            type.GetField(name, BindingFlags.Static | bindingFlags).SetValue(null, value);
        }

        private static object InvokeMethod(object instance, string name, BindingFlags bindingFlags, object[] parameters)
        {
            MethodInfo methodInfo = instance.GetType().GetMethod(name, BindingFlags.Instance | bindingFlags);
            return methodInfo.Invoke(instance, parameters);
        }

        private static string ToString(EditorWindow editorWindow)
        {
            return $"\"【{editorWindow.titleContent}\" <{editorWindow.GetType().Name}> {ToString(editorWindow.position)}】";
        }

        private static string ToString(Rect rect)
        {
            return $"({(int)rect.xMin}, {(int)rect.yMin})-({(int)rect.xMax}, {(int)rect.yMax})";
        }

        /// <summary>
        /// 以下 UnityCsReference 2021.3 を元に用意したもの。
        /// </summary>
        /// <seealso href="SplitView.cs">
        /// https://github.com/Unity-Technologies/UnityCsReference/blob/2021.3/Editor/Mono/GUI/SplitView.cs
        /// </seealso>

        [Flags] internal enum ViewEdge
        {
            None = 0,
            Left = 1 << 0,
            Bottom = 1 << 1,
            Top = 1 << 2,
            Right = 1 << 3,
            BottomLeft = Bottom | Left,
            BottomRight = Bottom | Right,
            TopLeft = Top | Left,
            TopRight = Top | Right,
            FitsVertical = Top | Bottom,
            FitsHorizontal = Left | Right,
            Before = Top | Left, // "Before" in SplitView children
            After = Bottom | Right // "After" in SplitView children
        }

        private static Rect RectFromEdge(Rect rect, ViewEdge edge, float thickness, float offset)
        {
            switch (edge)
            {
                case ViewEdge.Left:
                    return new Rect(rect.x - offset, rect.y, thickness, rect.height);
                case ViewEdge.Right:
                    return new Rect(rect.xMax - thickness + offset, rect.y, thickness, rect.height);
                case ViewEdge.Top:
                    return new Rect(rect.x, rect.y - offset, rect.width, thickness);
                case ViewEdge.Bottom:
                    return new Rect(rect.x, rect.yMax - thickness + offset, rect.width, thickness);
                default:
                    throw new ArgumentException("Specify exactly one edge");
            }
        }

        private static Vector2 GetDropPosition(Rect viewScreenPosition, DockPosition dockPosition)
        {
            const float kDockHeight = 39;                   // DockArea.kDockHeight
            const float kMaxViewDropZoneThickness = 300f;   // SplitView.kMaxViewDropZoneThickness

            // Collect flags of which edge zones the mouse is inside
            var childRect = viewScreenPosition;
            var childRectWithoutDock = RectFromEdge(childRect, ViewEdge.Bottom, childRect.height - kDockHeight, 0f);

            var borderWidth = Mathf.Min(Mathf.Round(childRectWithoutDock.width / 3), kMaxViewDropZoneThickness);
            var borderHeight = Mathf.Min(Mathf.Round(childRectWithoutDock.height / 3), kMaxViewDropZoneThickness);

            Rect dropRect = dockPosition switch
            {
                DockPosition.Left   => RectFromEdge(childRectWithoutDock, ViewEdge.Left, borderWidth, 0f),
                DockPosition.Right  => RectFromEdge(childRectWithoutDock, ViewEdge.Right, borderWidth, 0f),
                DockPosition.Bottom => RectFromEdge(childRectWithoutDock, ViewEdge.Bottom, borderHeight, 0f),
                DockPosition.Top    => RectFromEdge(childRectWithoutDock, ViewEdge.Top, borderHeight, 0f),
                _ => throw new InvalidOperationException()
            };
            
            return dropRect.center;
        }

        //<summary>
        //windowsがドッキングして順に縦に並んでいるときに、ウィンドウの高さのリストを返す。
        //<summary>
        public static List<float> GetDockedWindowHeights(List<EditorWindow> editorWindows) {
            var views = GetViews();
            var heights = editorWindows.Select(x => -1.0f).ToList();
            float lastBottom = 0;
            for (var i = 0; i < editorWindows.Count; i++){
                var editorWindow = editorWindows[i];
                var editorWindowView = GetDockWindowView(views, editorWindow);
                if (editorWindowView == null)
                {
                    continue;
                }
                var position = (Rect) GetProperty(editorWindowView, "screenPosition", BindingFlags.Public);
                if (i > 0 && Mathf.Abs(position.y - lastBottom) >= 0.01f)
                {
                    continue;
                }
                lastBottom = position.y + position.height;
                heights[i] = position.height;
            }
            return heights;
        }

        //<summary>
        //parentWindowとchildWindowがドッキングしているとき、parentWindow+childWindowに対するchildWindowの割合を返す。
        //parentWindowとchildWindowがドッキングしていない場合は、負の値を返す。
        //</summary>
        public static float GetChildWeight(EditorWindow parentWindow, EditorWindow childWindow) {
            float childWeight = -1;
            var views = GetViews();
            var parentWindowView = GetDockWindowView(views, parentWindow);
            if (parentWindowView == null)
            {
                Debug.LogError($"parentWindow({parentWindow})が見つかりません。");
                return childWeight;
            }
            var childWindowView = GetDockWindowView(views, childWindow);
            if (childWindowView == null)
            {
                Debug.LogError($"childWindow({childWindow})が見つかりません。");
                return childWeight;
            }

            var parentWindowViewScreenPosition = (Rect) GetProperty(parentWindowView, "screenPosition", BindingFlags.Public);
            var childWindowViewScreenPosition = (Rect) GetProperty(childWindowView, "screenPosition", BindingFlags.Public);
            if (parentWindowViewScreenPosition.x == childWindowViewScreenPosition.x)
            {
                if (Mathf.Abs(parentWindowViewScreenPosition.y + parentWindowViewScreenPosition.height - childWindowViewScreenPosition.y) < 0.01f)
                {
                    // Bottom docked
                    childWeight = childWindowViewScreenPosition.height / (parentWindowViewScreenPosition.height + childWindowViewScreenPosition.height);
                } else if (Mathf.Abs(parentWindowViewScreenPosition.y - (childWindowViewScreenPosition.y + childWindowViewScreenPosition.height)) < 0.01f)
                {
                    // Top docked
                    childWeight = childWindowViewScreenPosition.height / (parentWindowViewScreenPosition.height + childWindowViewScreenPosition.height);
                }
            } else if (parentWindowViewScreenPosition.y == childWindowViewScreenPosition.y)
            {
                if (Mathf.Abs(parentWindowViewScreenPosition.x + parentWindowViewScreenPosition.width - childWindowViewScreenPosition.x) < 0.01f)
                {
                    // Right docked
                    childWeight = childWindowViewScreenPosition.width / (parentWindowViewScreenPosition.width + childWindowViewScreenPosition.width);
                }
                else if (Mathf.Abs(parentWindowViewScreenPosition.x - (childWindowViewScreenPosition.x + childWindowViewScreenPosition.width)) < 0.01f)
                {
                    // Left docked
                    childWeight = childWindowViewScreenPosition.width / (parentWindowViewScreenPosition.width + childWindowViewScreenPosition.width);
                }
            }
            return childWeight;
        }

        //<summary>
        //dockWindowの下にdropWindowがドッキングされている時、dockWindowの高さがdockHeightになるよう設定する。
        //</summary>
        public static void SetDockWindowHeight(EditorWindow dockWindow, EditorWindow dropWindow, float dockHeight) {
            var views = GetViews();
            var dropWindowView = GetDockWindowView(views, dropWindow);
            var dockWindowView = GetDockWindowView(views, dockWindow);
            var dropPos = (Rect) GetProperty(dropWindowView, "position", BindingFlags.Public);
            var dockPos = (Rect) GetProperty(dockWindowView, "position", BindingFlags.Public);
            var totalHeight = dockPos.height + dropPos.height;
            var dropHeight = totalHeight - dockHeight;
            dropPos.height = dropHeight;
            dockPos.height = dockHeight;
            dropPos.y = dockPos.y + dockPos.height;
            SetProperty(dropWindowView, "position", (object) dropPos, BindingFlags.Public);
            SetProperty(dockWindowView, "position", (object) dockPos, BindingFlags.Public);
        }

        //<summary>
        //指定EditorWindowがOpenされているかを返す。
        //</summary>
        public static bool IsEditorWindowOpen<T>() {
            var views = GetViews();
            foreach (var view in views)
            {
                //Debug.Log($"{view.GetType()}, {(view is T)}");
                //if (view is T) return true;
                if (!dockAreaType.IsAssignableFrom(view.GetType())) continue;
                var o = GetProperty(view, "actualView", BindingFlags.NonPublic);
                if (o is T)
                {
                    return true;
                }
            }
            return false;
        }
    }
}