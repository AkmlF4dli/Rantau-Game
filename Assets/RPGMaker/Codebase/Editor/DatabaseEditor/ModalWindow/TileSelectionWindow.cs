using JetBrains.Annotations;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Map;
using RPGMaker.Codebase.Editor.Common;
using RPGMaker.Codebase.Editor.MapEditor.Component.Inventory;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine;

namespace RPGMaker.Codebase.Editor.DatabaseEditor.ModalWindow
{
    /// <summary>
    /// タイル選択ウィンドウ.
    /// </summary>
    public class TileSelectionWindow : EditorWindow
    {
        private const string Uxml = "Assets/RPGMaker/Codebase/Editor/DatabaseEditor/ModalWindow/Uxml/tile_selection.uxml";
        private const string Uss  = "Assets/RPGMaker/Codebase/Editor/DatabaseEditor/ModalWindow/Uxml/tile_selection.uss";

        // データプロパティ
        private List<TileDataModelInfo>  _tileEntities;

        // UI要素プロパティ
        private TileInventory      _tileInventory;

        private System.Action<TileDataModel> _callback;

        TileDataModel.Type _type = default;
        string _tileId;

        public static TileSelectionWindow CreateInstance() {
            // close previous windows and return a new instance.
            foreach (var w in Resources.FindObjectsOfTypeAll<TileSelectionWindow>())
            {
                w.Close();
            }
            return ScriptableObject.CreateInstance<TileSelectionWindow>();
        }

        /**
         * 初期化
         */
        public void Init(List<TileDataModelInfo> tileEntities, System.Action<TileDataModel> callback, TileDataModel.Type type, string tileId) {
            this.titleContent = new GUIContent(EditorLocalize.LocalizeText("WORD_5034"));
            _tileEntities = tileEntities;
            _callback = callback;
            _type = type;
            _tileId = tileId;
        }

        /**
         * データおよび表示を更新
         */
        public void Refresh([CanBeNull] List<TileDataModelInfo> tileEntities = null) {
            if (tileEntities != null) _tileEntities = tileEntities;
            _tileInventory.Refresh(_tileEntities);
        }

        /**
         * 表示サイズの更新
         * ウィンドウサイズを変更した際に調節する
         */
        public void RefreshWindowSize(float windowWidth) {
            _tileInventory.style.height = windowWidth / 1.5f;
        }

        /**
         * UI初期化
         */
        private void CreateGUI() {
            rootVisualElement.Clear();

            VisualElement uxmlElement = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Uxml).CloneTree();
            
            StyleSheet styleLayout = AssetDatabase.LoadAssetAtPath<StyleSheet>(MapEditor.MapEditor.UssDarkLayout);
            StyleSheet styleTileInventory = AssetDatabase.LoadAssetAtPath<StyleSheet>(MapEditor.MapEditor.UssDarkTileInventory);
            if (!EditorGUIUtility.isProSkin)
            {
                styleLayout = AssetDatabase.LoadAssetAtPath<StyleSheet>(MapEditor.MapEditor.UssLightLayout);
                styleTileInventory = AssetDatabase.LoadAssetAtPath<StyleSheet>(MapEditor.MapEditor.UssLightTileInventory);
            }
            uxmlElement.styleSheets.Add(styleLayout);
            uxmlElement.styleSheets.Add(styleTileInventory);
            uxmlElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(Uss));

            EditorLocalize.LocalizeElements(uxmlElement);
            VisualElement tileListContainer = uxmlElement.Query<VisualElement>("tile_list_container");

            // タイルリスト
            _tileInventory = new TileInventory(
                _tileEntities,
                null,
                null,
                (tile) => {
                    _callback?.Invoke(tile);
                    Close();
                },
                new List<TileDataModel.Type>() { TileDataModel.Type.NormalTile, TileDataModel.Type.LargeParts },
                _type, _tileId
            );
            tileListContainer.Add(_tileInventory);

            // 確定、キャンセルボタン
            //----------------------------------------------------------------------
            var buttonOk = uxmlElement.Query<Button>("Common_Button_Ok").AtIndex(0);
            var buttonCancel = uxmlElement.Query<Button>("Common_Button_Cancel").AtIndex(0);
            buttonOk.style.alignContent = Align.FlexEnd;
            buttonOk.clicked += () =>
            {
                _callback?.Invoke(_tileInventory.CurrentSelectingTile);
                Close();
            };

            buttonCancel.clicked += () =>
            {
                Close();
            };

            // 要素配置
            rootVisualElement.Add(uxmlElement);
        }

    }
}