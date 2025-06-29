using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Map;
using RPGMaker.Codebase.Editor.Common;
using RPGMaker.Codebase.Editor.Common.View;
using RPGMaker.Codebase.Editor.MapEditor.Component.Canvas;
using RPGMaker.Codebase.Editor.MapEditor.Window.MapEdit;
using System;
using System.Collections.Generic;
using System.IO;
using RPGMaker.Codebase.Editor.DatabaseEditor.ModalWindow;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGMaker.Codebase.Editor.Inspector.Map.View
{
    /// <summary>
    /// [マップ設定]-[マップリスト]-[各マップ]-[マップ編集]-[背景] Inspector
    /// </summary>
    public class BackgroundViewInspector : AbstractInspectorElement
    {
        protected override string MainUxml { get { return "Assets/RPGMaker/Codebase/Editor/Inspector/Map/Asset/background_view.uxml"; } }

        private readonly MapDataModel         _mapEntity;
        private readonly Action<MapDataModel> _onChange;

        //背景の画像リストの保持
        private List<string> _backgroundPicture;

        //画像設定のプルダウン
        private PopupFieldBase<string> _backgroundViewDropdown;

        private Dictionary<string, string> _backgroundViewImageDictionary;

        // "エディター表示"トグル。
        private Toggle _displayToggle;

        //更新用
        private MapEditWindow _mapEditWindow =
            WindowLayoutManager.GetOrOpenWindow(WindowLayoutManager.WindowLayoutId.MapEditWindow) as MapEditWindow;

        //画像の拡大の部分
        private          RadioButton _zoom0Toggle;
        private          RadioButton _zoom2Toggle;
        private          RadioButton _zoom4Toggle;

        public BackgroundViewInspector(MapDataModel mapEntity, Action<MapDataModel> onChange) {
            _mapEntity = mapEntity;
            _onChange = onChange;

            Initialize();
        }

        protected override void RefreshContents() {
            base.RefreshContents();
            Initialize();
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        override protected void InitializeContents() {
            base.InitializeContents();

            _displayToggle = RootContainer.Query<Toggle>("display_toggle");

            _zoom0Toggle = RootContainer.Query<RadioButton>("radioButton-mapEdit-display14");
            _zoom2Toggle = RootContainer.Query<RadioButton>("radioButton-mapEdit-display15");
            _zoom4Toggle = RootContainer.Query<RadioButton>("radioButton-mapEdit-display16");

            SetEntityToUI();
        }

        private void SetEntityToUI() {
            var selectButton = RootContainer.Q<Button>("background_view_button");
            if (string.IsNullOrEmpty(_mapEntity.background.imageName))
                selectButton.text = EditorLocalize.LocalizeText("WORD_1594");
            else
            {
                selectButton.text = Path.ChangeExtension(_mapEntity.background.imageName, ".png");
            }
            selectButton.clickable = null;
            selectButton.clicked += () =>
            {
                var targetImageModalWindow = ImagePicker.Instantiate(PathManager.MAP_BACKGROUND, true);
                targetImageModalWindow.ShowWindow(EditorLocalize.LocalizeWindowTitle("Select Background"), data =>
                {
                    var imageFileName = Path.ChangeExtension((string)data, ".png");
                    var imageFilePath = PathManager.MAP_BACKGROUND + imageFileName;
            
                    _mapEntity.background.imageName = Path.GetFileNameWithoutExtension(imageFileName);
                    _onChange?.Invoke(_mapEntity);
            
                    var tex = UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Texture2D>(imageFilePath);
                    var spr = UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(imageFilePath);
            
                    //Mapのリロード
                    _mapEditWindow =
                        WindowLayoutManager.GetOrOpenWindow(WindowLayoutManager.WindowLayoutId.MapEditWindow) as
                            MapEditWindow;
                    _mapEditWindow.UpdateBackgroundView(_mapEntity, spr, tex);
                }, _mapEntity.background.imageName, true);
            };

            // エディター表示トグル。
            var backgroundTransform = _mapEntity.GetLayerTransformForEditor(MapDataModel.Layer.LayerType.Background);
            var backgroundSpriteRenderer = backgroundTransform.GetComponent<SpriteRenderer>();
            backgroundSpriteRenderer.enabled = _displayToggle.value = _mapEntity.background.showInEditor;
            _displayToggle.RegisterValueChangedCallback(evt =>
            {
                backgroundSpriteRenderer.enabled =
                _mapEntity.background.showInEditor = evt.newValue;
                _onChange?.Invoke(_mapEntity);
            });

            if (_mapEntity.background.imageZoomIndex == MapDataModel.ImageZoomIndex.Zoom1)
                _zoom0Toggle.value = true;
            else if (_mapEntity.background.imageZoomIndex == MapDataModel.ImageZoomIndex.Zoom2)
                _zoom2Toggle.value = true;
            else if (_mapEntity.background.imageZoomIndex == MapDataModel.ImageZoomIndex.Zoom4)
                _zoom4Toggle.value = true;

            new CommonToggleSelector().SetRadioSelector(
                new List<RadioButton> {_zoom0Toggle, _zoom2Toggle, _zoom4Toggle},
                (int) _mapEntity.background.imageZoomIndex, new List<Action>
                {
                    // 等倍表示トグル。
                    () =>
                    {
                        _mapEntity.background.imageZoomIndex = MapDataModel.ImageZoomIndex.Zoom1;
                        MapCanvas.UpdateBackgroundDisplayedAndScaleForEditor(_mapEntity);

                        _onChange?.Invoke(_mapEntity);
                    },
                    // 2倍表示トグル。
                    () =>
                    {
                        _mapEntity.background.imageZoomIndex = MapDataModel.ImageZoomIndex.Zoom2;
                        MapCanvas.UpdateBackgroundDisplayedAndScaleForEditor(_mapEntity);

                        _onChange?.Invoke(_mapEntity);
                    },
                    // 4倍表示トグル。
                    () =>
                    {
                        _mapEntity.background.imageZoomIndex = MapDataModel.ImageZoomIndex.Zoom4;
                        MapCanvas.UpdateBackgroundDisplayedAndScaleForEditor(_mapEntity);

                        _onChange?.Invoke(_mapEntity);
                    }
                });
        }
    }
}