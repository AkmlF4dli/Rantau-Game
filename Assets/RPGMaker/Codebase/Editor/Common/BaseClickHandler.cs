using System;
using UnityEngine.UIElements;

namespace RPGMaker.Codebase.Editor.Common
{
    public static class BaseClickHandler
    {
        public static void ClickEvent(VisualElement element, Action<int> callBack) {
            element.RegisterCallback<MouseUpEvent>(evt =>
            {
                evt.StopPropagation();
                if (evt.button != (int) MouseButton.RightMouse)
                {
                    callBack((int) MouseButton.LeftMouse);
                    return;
                }

                callBack((int) MouseButton.RightMouse);
            });

            element.RegisterCallback<MouseDownEvent>(evt =>
            {
                evt.StopPropagation();
                if (evt.button != (int) MouseButton.RightMouse)
                {
                    callBack((int) MouseButton.LeftMouse);
                    return;
                }

                callBack((int) MouseButton.RightMouse + 1);
            });
        }

        public static void ClickEvent2(VisualElement element, Action<int> callBack) {
            element.RegisterCallback<MouseUpEvent>(evt =>
            {
                evt.StopPropagation();
                callBack(evt.button);
            });
            element.RegisterCallback<MouseDownEvent>(evt =>
            {
                evt.StopPropagation();
                callBack(evt.button);
            });
        }

    }
}