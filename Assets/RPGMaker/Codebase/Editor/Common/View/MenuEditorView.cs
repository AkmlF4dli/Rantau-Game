using RPGMaker.Codebase.Editor.Common.Window;
using RPGMaker.Codebase.Editor.Common.Window.ModalWindow;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGMaker.Codebase.Editor.Common.View
{
    public class MenuEditorView : VisualElement
    {
        public const string ImagePath = "Assets/RPGMaker/Storage/Images/System/Menu/";
        public const string Dark = "Dark";
        public const string Light = "Light";
        public const string ImagePathActive = "Active/";
        public const string ImagePathDown = "Down/";
        public const string ImagePathDisable = "Disable/";
        public const string ImagePathLog = "unite_logo_s";

        public const string HelpURL_JP = "https://support.rpgmakerunite.com/hc/ja";
        public const string HelpURL_EN = "https://support.rpgmakerunite.com/hc/en-us";
        public const string HelpURL_CN = "https://support.rpgmakerunite.com/hc/zh-cn";
        public const string ImageIconMenu = "uibl_icon_menu_";
        public const string ImageIconMenuP = "uibl_icon_menu_p_";
        public const string ImageIconMenuD = "uibl_icon_menu_d_";

        class MenuButtonInfo
        {
            public MenuWindow.BtnType ButtonType { get; private set; }
            public string ButtonTypeString { get; private set; }
            public string TipsID { get; private set; }
            public bool IsPlaymodeActive { get; private set; }
            public bool IsEditModeActive { get; private set; }
            public MenuButtonInfo(MenuWindow.BtnType btnType, string tipsID, bool isPlaymodeActive, bool isEditModeActive) {
                ButtonType = btnType;
                TipsID = tipsID;
                ButtonTypeString = ((int) ButtonType).ToString("000");
                IsPlaymodeActive = isPlaymodeActive;
                IsEditModeActive = isEditModeActive;
            }
        }

        List<MenuButtonInfo> _menuButtonInfo = new List<MenuButtonInfo>()
        {
            new( MenuWindow.BtnType.New,   "WORD_1454", true,true),
            new( MenuWindow.BtnType.Open,  "WORD_1455", true,true),
            new( MenuWindow.BtnType.Save,  "WORD_1457", true,true),
            new( MenuWindow.BtnType.Pen,   "WORD_1648", true,true),
            new( MenuWindow.BtnType.Play,  "WORD_1646", false,true),
            new( MenuWindow.BtnType.Addon, "WORD_2500", false,true),
            new( MenuWindow.BtnType.HierarchyHistory,"WORD_5028", false,true),
            new( MenuWindow.BtnType.Search,"WORD_1642", false,true),
            new( MenuWindow.BtnType.Stop,  "WORD_1647", true,false),
            new( MenuWindow.BtnType.Help,  "WORD_1644", true,true),
        };

        private readonly string mainUxml = "Assets/RPGMaker/Codebase/Editor/Common/Asset/Uxml/menu.uxml";
        private readonly MenuWindow _menuWindow;

        private VisualElement _logoArea;
        private VisualElement _menuArea;
        private List<Image> _imagesIcon;

        private Object[] _spriteObjects = null;

        public MenuEditorView(MenuWindow menuWindow) {
            _menuWindow = menuWindow;
            Init();
        }

        private void Init() {
            Clear();

            _spriteObjects = AssetDatabase.LoadAllAssetsAtPath($"Assets/RPGMaker/SystemResource/MenuIcon/{EditerMode()}.png");

            var items = new VisualElement();
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(mainUxml);
            VisualElement labelFromUxml = visualTree.CloneTree();
            EditorLocalize.LocalizeElements(labelFromUxml);
            labelFromUxml.style.flexGrow = 1;
            items.Add(labelFromUxml);
            _menuArea = items.Query<VisualElement>("menu_area");
            _logoArea = items.Query<VisualElement>("logo_area");
            InitUi();
            Add(items);
        }

        private void InitUi() {
            //テストプレイ中はアイコンがStopになる
            var btns = EditorApplication.isPlayingOrWillChangePlaymode ?
                _menuButtonInfo.Where(btInfo => btInfo.IsPlaymodeActive) :
                _menuButtonInfo.Where(btInfo => btInfo.IsEditModeActive);

            _imagesIcon = new List<Image>();
            foreach (var item in btns)
            {
                var sprite = (Sprite) _spriteObjects.FirstOrDefault(obj => obj.name == ImageIconMenu + item.ButtonTypeString);
                var image = new Image();    
                image.name = item.ButtonTypeString;
                image.sprite = sprite;
                image.style.width = sprite.rect.width;
                image.style.height = sprite.rect.height;
                image.style.flexShrink = 0;
                image.RegisterCallback<MouseOverEvent>(MouseOverEvent);
                image.RegisterCallback<MouseUpEvent>(OnMouseUpEvent);
                image.RegisterCallback<MouseDownEvent>(OnMouseDownEvent);
                image.tooltip = EditorLocalize.LocalizeText(item.TipsID);
                _imagesIcon.Add(image);
                _menuArea.Add(image);
            }

            var logo = new Image();
            logo.sprite = (Sprite) _spriteObjects.FirstOrDefault(obj => obj.name == ImagePathLog);
            logo.style.width = logo.sprite.rect.width;
            logo.style.height = logo.sprite.rect.height;
            logo.style.flexShrink = 0;
            _logoArea.Add(logo);
        }

        public static void Help() {
            //現在の言語設定
            var language = EditorLocalize.GetNowLanguage();
            switch (language)
            {
                case SystemLanguage.Japanese:
                    Application.OpenURL(HelpURL_JP);
                    break;
                case SystemLanguage.Chinese:
                    Application.OpenURL(HelpURL_CN);
                    break;
                default:
                    Application.OpenURL(HelpURL_EN);
                    break;
            }
            AnalyticsManager.PostEvent(
                AnalyticsManager.EventName.action,
                AnalyticsManager.EventParameter.help);
        }

        public static void About() {
            var versionCheckWindow = ScriptableObject.CreateInstance<VersionCheckWindow>();
            versionCheckWindow.ShowWindow();
        }

        //ダークモード、ライトモードのPath切り替え用
        public static string EditerMode() {
            return EditorGUIUtility.isProSkin ? Dark : Light;
        }

        private void MouseOverEvent(MouseEventBase<MouseOverEvent> evt) {
            var image = (Image) evt.target;
            var iconName = image.name;
            image.sprite = (Sprite) _spriteObjects.FirstOrDefault(obj => obj.name == ImageIconMenu + image.name);
        }

        private void OnMouseUpEvent(MouseEventBase<MouseUpEvent> evt) {
            var image = (Image) evt.target;
            var iconName = image.name;
            image.sprite = (Sprite) _spriteObjects.FirstOrDefault(obj => obj.name == ImageIconMenu + image.name);
            _menuWindow.Select(int.Parse(image.name));
        }

        private void OnMouseDownEvent(MouseEventBase<MouseDownEvent> evt) {
            var image = (Image) evt.target;
            var iconName = image.name;
            image.sprite = (Sprite) _spriteObjects.FirstOrDefault(obj => obj.name == ImageIconMenuP + image.name);
        }

        public Image GetIconImage(int index) {
            Image image = null;
            for (int i = 0; i < _imagesIcon.Count; i++)
            {
                if (int.Parse(_imagesIcon[i].name) == index)
                {
                    image = _imagesIcon[i];
                    break;
                }
            }

            return image;
        }
    }
}