using RPGMaker.Codebase.Editor.Common.Window;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGMaker.Codebase.Editor.Common.View
{
    public class SideMenuView : VisualElement
    {
        private readonly List<int> _btnTypes = new List<int>
        {
            (int) MenuWindow.BtnType.ZoomIn,
            (int) MenuWindow.BtnType.ZoomOut,
            (int) MenuWindow.BtnType.ActualSize
        };

        private readonly string        mainUxml = "Assets/RPGMaker/Codebase/Editor/Common/Asset/Uxml/sideMenu.uxml";
        private readonly Action        _actualSizeAction;
        private readonly VisualElement _menuArea;

        private readonly Action _zoomInAction;
        private readonly Action _zoomOutAction;

        private UnityEngine.Object[] _spriteObjects = null;

        public SideMenuView(Action zoomin, Action zoomOut, Action actualSize) {
            _zoomInAction = zoomin;
            _zoomOutAction = zoomOut;
            _actualSizeAction = actualSize;

            Clear();

            _spriteObjects = AssetDatabase.LoadAllAssetsAtPath($"Assets/RPGMaker/SystemResource/MenuIcon/{MenuEditorView.EditerMode()}.png");

            var items = new VisualElement();
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(mainUxml);
            VisualElement labelFromUxml = visualTree.CloneTree();
            EditorLocalize.LocalizeElements(labelFromUxml);
            labelFromUxml.style.flexGrow = 1;
            items.Add(labelFromUxml);
            _menuArea = items.Query<VisualElement>("menu_area");
            _menuArea.style.flexDirection = FlexDirection.Row;
            InitUi();
        }

        public VisualElement GetSideMenuElement() {
            return _menuArea;
        }

        private void InitUi() {
            for (var i = 0; i < _btnTypes.Count; i++)
            {
                var buttonName = _btnTypes[i].ToString("000");
                var sprite = (Sprite) _spriteObjects.FirstOrDefault(obj => obj.name == MenuEditorView.ImageIconMenu + buttonName);

                var image = new Image();
                image.name = buttonName;
                image.sprite = sprite;
                //image.image =
                //    AssetDatabase.LoadAssetAtPath<Texture>(
                //        MenuEditorView.ImagePath + MenuEditorView.EditerMode() + MenuEditorView.ImagePathActive +
                //        MenuEditorView.ImageIconMenu +
                //        _btnTypes[i].ToString("000") + ".png"
                //    );
                //image.style.width = image.image.width;
                //image.style.height = image.image.height;
                image.RegisterCallback<MouseOverEvent>(MouseOverEvent);
                image.RegisterCallback<MouseUpEvent>(OnMouseUpEvent);
                image.RegisterCallback<MouseDownEvent>(OnMouseDownEvent);
                _menuArea.Add(image);
            }
        }

        private void MouseOverEvent(MouseEventBase<MouseOverEvent> evt) {
            var image = (Image) evt.target;
            image.sprite = (Sprite) _spriteObjects.FirstOrDefault(obj => obj.name == MenuEditorView.ImageIconMenu + image.name);
        }

        private void OnMouseUpEvent(MouseEventBase<MouseUpEvent> evt) {
            var image = (Image) evt.target;
            image.sprite = (Sprite) _spriteObjects.FirstOrDefault(obj => obj.name == MenuEditorView.ImageIconMenu + image.name);
        }

        private void OnMouseDownEvent(MouseEventBase<MouseDownEvent> evt) {
            var image = (Image) evt.target;
            image.sprite = (Sprite) _spriteObjects.FirstOrDefault(obj => obj.name == MenuEditorView.ImageIconMenuP + image.name);
            SelectAction(int.Parse(image.name));
        }

        private void SelectAction(int selectbutton) {
            switch (selectbutton)
            {
                case (int) MenuWindow.BtnType.ZoomIn:
                    _zoomInAction.Invoke();
                    break;
                case (int) MenuWindow.BtnType.ZoomOut:
                    _zoomOutAction.Invoke();
                    break;
                case (int) MenuWindow.BtnType.ActualSize:
                    _actualSizeAction.Invoke();
                    break;
            }
        }
    }
}