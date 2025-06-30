using RPGMaker.Codebase.Editor.Common;
using System.Xml;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGMaker.Codebase.Editor.Hierarchy.Region.Base.View
{
    public class EventListWindow : BaseWindow
    {
        private Action LostFocusCallBack;

        /// <summary>
        /// フォーカスロスト時のコールバックを登録する
        /// </summary>
        /// <param name="Callback"></param>
        public void RegisterLostFocusCallback(Action Callback) {
            LostFocusCallBack = Callback;
        }

        protected void Awake() 
        {
            titleContent = new GUIContent(EditorLocalize.LocalizeText("WORD_0014"));
            rootVisualElement.focusable = true;
            rootVisualElement.RegisterCallback<KeyDownEvent>(e =>
            {
                e.StopPropagation();
                switch (e.keyCode)
                {
                    case KeyCode.UpArrow:
                        Hierarchy.SelectPrevItem();
                        break;
                    case KeyCode.DownArrow:
                        Hierarchy.SelectNextItem();
                        break;
                }
            });

        }

        private void CreateGUI()
        {
            rootVisualElement.style.flexGrow = 1;   //EventListViewをウィンドウいっぱいに表示させる。
        }

        private void OnFocus() {
            rootVisualElement.Focus();
        }

        /// <summary>
        /// フォーカスが外れた際にコールされる
        /// </summary>
        private void OnLostFocus()
        {
            if (LostFocusCallBack != null)
            {
                //フォーカスOut時のコールバックコール
                LostFocusCallBack.Invoke();
            }
        }
    }
}