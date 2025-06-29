using RPGMaker.Codebase.Editor.Common;
using RPGMaker.Codebase.Editor.Common.Window;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGMaker.Codebase.Editor.DatabaseEditor.ModalWindow
{
    public class ImagePicker : BaseModalWindow
    {
        protected readonly string Extension = "png";
        protected readonly float MinThumbnailSize = 32.0f;
        protected readonly float MaxThumbnailSize = 256.0f;
        protected readonly float FontSize = 12.0f;
        protected readonly float VerticalItemPadding = 4.0f;
        protected readonly float HorizontalItemPadding = 2.0f;
        protected readonly float PreviewPadding = 8.0f;
        protected readonly float InitialPaneWidth = 400.0f;

        protected string CurrentSelectedName;
        protected int CurrentSelectedIndex = -1;
        protected bool ContainerInitialized;
        protected string[] ItemNames;
        protected string[] Paths;
        protected readonly Dictionary<string, Texture2D> TextureCache = new Dictionary<string, Texture2D>();
        protected float ItemWidth;
        protected float ItemHeight;
        protected bool IsHorizontalLayout;
        protected bool PaneWidthLarge;

        protected ScrollView ScrollView;
        protected Scroller Scroller;
        protected VisualElement ItemContainer;
        protected VisualElement PreviewImage;
        protected Label PreviewName;
        protected Label PreviewImageSize;
        protected VisualElement PreviewContainer;
        protected VisualTreeAsset ItemVisualTree;
        protected Slider ScaleSlider;
        protected Button ButtonOk;
        protected Button ButtonCancel;

        public string Path { get; set; }
        public bool AddNone { get; set; }
        public string NoneText { get; set; }

        protected override string ModalUxml =>
            "Assets/RPGMaker/Codebase/Editor/DatabaseEditor/ModalWindow/Uxml/image_picker.uxml";

        private string ItemUxml =>
            "Assets/RPGMaker/Codebase/Editor/DatabaseEditor/ModalWindow/Uxml/image_picker_item.uxml";

        protected override string ModalUss => "";

        /*
         * Instantiateを使用する
         */
        protected ImagePicker()
        {
        }

        public static ImagePicker Instantiate(
            string path,
            bool addNone = false,
            string noneText = "WORD_0113"
        )
        {
            var ins = CreateInstance<ImagePicker>();
            ins.Path = path;
            ins.AddNone = addNone;
            ins.NoneText = noneText;

            return ins;
        }

        public void ShowWindow(
            string modalTitle,
            CallBackWidow callBack,
            string currentSelectingImageFileName,
            bool paneWidthLarge = false
        )
        {
            PaneWidthLarge = paneWidthLarge;
            CurrentSelectedName = currentSelectingImageFileName;
            ShowWindow(modalTitle, callBack);

            // NOTE: 拡張子付きで名前が渡された際の対応
            if (!string.IsNullOrEmpty(System.IO.Path.GetExtension(CurrentSelectedName)))
            {
                CurrentSelectedName = System.IO.Path.GetFileNameWithoutExtension(CurrentSelectedName);
            }

            _ = SelectImage();
            //モーダルウィンドウとして表示する
            ShowModal();
        }

        protected virtual async Task SelectImage()
        {
            while (!ContainerInitialized)
            {
                await Task.Delay(1);
            }

            if (!string.IsNullOrEmpty(CurrentSelectedName))
            {
                var index = -1;
                for (var i = 0; i < ItemNames.Length; i++)
                {
                    if (ItemNames[i] == CurrentSelectedName)
                    {
                        index = i;
                        break;
                    }
                }

                if (index >= 0)
                {
                    CurrentSelectedIndex = index;
                    Texture2D texture = LoadTexture(index);
                    PreviewImage.style.backgroundImage = texture;
                    PreviewName.text = ItemNames[index];
                    PreviewImageSize.text = $"{texture.width}x{texture.height}";
                    var pickerItem = ItemContainer[index].Q<VisualElement>("Wrapper");
                    pickerItem.EnableInClassList("--selected", true);

                    ToScroll(index);
                }
            }
        }

        public override void ShowWindow(string modalTitle, CallBackWidow callBack)
        {
            var wnd = GetWindow<ImagePicker>();
            if (callBack != null) _callBackWindow = callBack;
            wnd.titleContent = new GUIContent(EditorLocalize.LocalizeText("WORD_1611"));

            wnd.Init();
        }

        public override void Init()
        {
            InitData();
            InitView();
            InitEvent();
            InitItem();

            UpdateContainsItem();
        }

        /*
         * データを初期化
         */
        protected virtual void InitData()
        {
            List<string> tempList = System.IO.Directory.GetFiles(Path)
                .Where(path => path.EndsWith(Extension))
                .Select(System.IO.Path.GetFileNameWithoutExtension)
                .ToList();
            if (AddNone)
            {
                tempList.Insert(0, EditorLocalize.LocalizeText(NoneText));
            }

            ItemNames = tempList.ToArray();

            Paths = ItemNames.Select(value => string.Format(Path + "{0}." + Extension, value))
                .ToArray();
        }

        /*
         * UIを初期化
         */
        protected virtual void InitView()
        {
            var root = rootVisualElement;
            var viewVisualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ModalUxml);
            ItemVisualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ItemUxml);
            VisualElement view = viewVisualTree.CloneTree();
            EditorLocalize.LocalizeElements(view);
            view.style.flexGrow = 1;
            if (EditorPrefs.GetInt("UserSkin") == 1)
            {
                view.AddToClassList("dark");
            }
            root.Add(view);

            ScrollView = root.Q<ScrollView>("ScrollView");
            Scroller = ScrollView.verticalScroller;
            ItemContainer = root.Q<VisualElement>("ItemContainer");
            PreviewImage = root.Q<VisualElement>("PreviewImage");
            PreviewName = root.Q<Label>("PreviewName");
            PreviewImageSize = root.Q<Label>("ImageSize");
            PreviewContainer = root.Q<VisualElement>("RightPane");

            ScaleSlider = root.Q<Slider>("ScaleSlider");
            ButtonOk = root.Q<Button>("Common_Button_Ok");
            ButtonCancel = root.Q<Button>("Common_Button_Cancel");

            ItemContainer.EnableInClassList("--vertical-layout", true);
            ItemContainer.EnableInClassList("--horizontal-layout", false);

            var scale = ScaleSlider.value / 100.0f;
            ItemWidth = ItemHeight = Mathf.Lerp(MinThumbnailSize, MaxThumbnailSize, scale);

            if (PaneWidthLarge)
            {
                root.Q<TwoPaneSplitView>("SplitView").fixedPaneInitialDimension = InitialPaneWidth;
            }
        }

        /*
         * イベントを初期化
         */
        protected virtual void InitEvent()
        {
            // ReSharper disable once UnusedParameter.Local
            ScrollView.RegisterCallback<GeometryChangedEvent>(
                evt => { UpdateContainsItem(); }
            );

            // ReSharper disable once UnusedParameter.Local
            Scroller.slider.RegisterValueChangedCallback(
                evt => { UpdateContainsItem(); }
            );

            // ReSharper disable once UnusedParameter.Local
            ItemContainer.RegisterCallback<GeometryChangedEvent>(
                evt => { UpdateContainsItem(); }
            );

            // ReSharper disable once UnusedParameter.Local
            PreviewContainer.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                var containerWidth = PreviewContainer.layout.width;
                var containerHeight = PreviewContainer.layout.height;
                var squareSize = Mathf.Min(containerWidth, containerHeight) - PreviewPadding * 2.0f;
                PreviewImage.style.width = squareSize;
                PreviewImage.style.height = squareSize;
            });

            ScaleSlider.RegisterValueChangedCallback(UpdateItemScale);

            ButtonOk.clicked += () =>
            {
                if ((AddNone && CurrentSelectedIndex == 0) || CurrentSelectedIndex == -1)
                {
                    _callBackWindow("");
                }
                else
                {
                    _callBackWindow(CurrentSelectedName);
                }

                Close();
            };

            ButtonCancel.clicked += Close;
        }

        /*
         * リストアイテムを初期化
         */
        protected virtual void InitItem()
        {
            for (var i = 0; i < ItemNames.Length; i++)
            {
                var itemName = ItemNames[i];

                var pickerItem = ItemVisualTree.CloneTree();
                pickerItem.style.height = ItemWidth;
                pickerItem.style.width = ItemHeight;
                SetPadding(pickerItem, VerticalItemPadding);
                pickerItem.Q<Label>("Name").text = ItemNameShaving(itemName, ItemWidth);
                var wrapper = pickerItem.Q<VisualElement>("Wrapper");
                wrapper.EnableInClassList("--selected", false);

                var index = i;
                // ReSharper disable once UnusedParameter.Local
                pickerItem.RegisterCallback<ClickEvent>(evt =>
                {
                    CurrentSelectedIndex = index;
                    UpdateSelectedItem();
                });

                pickerItem.RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.clickCount == 2)
                    {
                        _callBackWindow(CurrentSelectedName);
                        Close();
                    }
                });

                ItemContainer.Add(pickerItem);
            }
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                if (CurrentSelectedIndex == -1) return;

                var viewportRect = GetViewportRect();
                int numColumns = (int)(viewportRect.width / ItemWidth);

                switch (Event.current.keyCode)
                {
                    case KeyCode.RightArrow:
                        if (CurrentSelectedIndex < ItemContainer.childCount - 1)
                        {
                            CurrentSelectedIndex++;
                        }

                        Event.current.Use();
                        break;

                    case KeyCode.LeftArrow:
                        if (CurrentSelectedIndex > 0)
                        {
                            CurrentSelectedIndex--;
                        }

                        Event.current.Use();
                        break;

                    case KeyCode.DownArrow:
                        if (CurrentSelectedIndex + numColumns < ItemContainer.childCount)
                        {
                            CurrentSelectedIndex += numColumns;
                        }

                        Event.current.Use();
                        break;

                    case KeyCode.UpArrow:
                        if (CurrentSelectedIndex - numColumns >= 0)
                        {
                            CurrentSelectedIndex -= numColumns;
                        }

                        Event.current.Use();
                        break;

                    case KeyCode.Escape:
                        Event.current.Use();
                        Close();
                        break;
                }

                UpdateSelectedItem();
                ToScroll(CurrentSelectedIndex);
            }
        }

        /*
         * 表示可能なアイテムを更新する
         */
        private void UpdateContainsItem()
        {
            var scrollViewHeight = ScrollView.layout.height;
            var containerWidth = ItemContainer.layout.width;
            var containerHeight = ItemContainer.layout.height;

            if (float.IsNaN(containerWidth) || float.IsNaN(scrollViewHeight)) return;
            if (ItemContainer.childCount == ItemNames.Length) ContainerInitialized = true;
            if (containerWidth == 0 || containerHeight == 0) return;

            var viewportRect = GetViewportRect();

            for (var i = 0; i < ItemContainer.childCount; i++)
            {
                var item = ItemContainer[i];
                if (viewportRect.Overlaps(item.localBound))
                {
                    if (!TextureCache.ContainsKey(Paths[i]))
                    {
                        _ = LoadTexture(i);
                    }
                }
            }
        }

        /**
         * テクスチャをロード
         * @param index ロードするテクスチャのインデックス
         */
        protected Texture2D LoadTexture(int index)
        {
            if (index < 0 || index >= Paths.Length) return null;
            if (AddNone && index == 0) return null;

            var path = Paths[index];
            if (!TextureCache.ContainsKey(path))
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

                // NOTE: データが異常な場合の対応（Door1_001_01.png 等）
                if (texture == null)
                {
                    Debug.LogWarning($"Texture not found: {path}");
                }

                TextureCache[path] = texture;
            }

            ItemContainer[index].Q<VisualElement>("Thumbnail").style.backgroundImage = TextureCache[path];

            return TextureCache[path];
        }

        /*
         * アイテムのスケールを更新する
         * @param evt スライダーの値変更イベント
         */
        private void UpdateItemScale(ChangeEvent<float> evt)
        {
            var scale = evt.newValue / 100.0f;
            var thumbnailSize = Mathf.Lerp(MinThumbnailSize, MaxThumbnailSize, scale);

            for (var i = 0; i < ItemContainer.childCount; i++)
            {
                var pickerItem = ItemContainer[i];
                var thumbnail = pickerItem.Q<VisualElement>("Thumbnail");
                var itemName = pickerItem.Q<Label>("Name");
                IsHorizontalLayout = Mathf.Approximately(thumbnailSize, MinThumbnailSize);

                if (IsHorizontalLayout)
                {
                    pickerItem.style.width = Length.Percent(100.0f);
                    pickerItem.style.height = FontSize + HorizontalItemPadding * 2.0f;
                    ItemWidth = GetViewportRect().width;
                    ItemHeight = FontSize + HorizontalItemPadding * 2.0f;
                    SetPadding(pickerItem, HorizontalItemPadding);
                    thumbnail.style.width = FontSize;
                    itemName.text = ItemNameShaving(ItemNames[i], ItemContainer.layout.width);
                    ItemContainer.EnableInClassList("--vertical-layout", false);
                    ItemContainer.EnableInClassList("--horizontal-layout", true);
                }
                else
                {
                    pickerItem.style.width = thumbnailSize;
                    pickerItem.style.height = thumbnailSize;
                    ItemWidth = thumbnailSize;
                    ItemHeight = thumbnailSize;
                    SetPadding(pickerItem, VerticalItemPadding);

                    // NOTE: USSの設定が反映されない為直接設定
                    thumbnail.style.width = Length.Percent(100.0f);

                    itemName.text = ItemNameShaving(ItemNames[i], thumbnailSize);
                    ItemContainer.EnableInClassList("--vertical-layout", true);
                    ItemContainer.EnableInClassList("--horizontal-layout", false);
                }
            }

            UpdateContainsItem();
        }

        /*
         * 選択中のアイテムで更新する
         */
        protected virtual void UpdateSelectedItem()
        {
            if (AddNone && CurrentSelectedIndex == 0)
            {
                PreviewImage.style.backgroundImage = null;
                PreviewImageSize.text = "";
            }

            if (CurrentSelectedIndex >= 0 && CurrentSelectedIndex < ItemContainer.childCount)
            {
                if (TextureCache.TryGetValue(Paths[CurrentSelectedIndex], out var texture))
                {
                    PreviewImage.style.backgroundImage = texture;
                    PreviewImageSize.text = texture == null ? "" : $"{texture.width}x{texture.height}";
                }

                CurrentSelectedName = PreviewName.text = ItemNames[CurrentSelectedIndex];

                for (var i = 0; i < ItemContainer.childCount; i++)
                {
                    ItemContainer[i].Q<VisualElement>("Wrapper").EnableInClassList(
                        "--selected",
                        false
                    );
                }

                ItemContainer[CurrentSelectedIndex].Q<VisualElement>("Wrapper").EnableInClassList(
                    "--selected",
                    true
                );
            }
        }

        /*
         * パディングを設定する
         * @param item アイテム
         * @param padding パディング
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetPadding(VisualElement item, float padding)
        {
            item.style.paddingBottom = padding;
            item.style.paddingRight = padding;
            item.style.paddingLeft = padding;
            item.style.paddingTop = padding;
        }

        /*
         * 長いアイテム名を省略する
         * @param itemName アイテム名
         * @param width ラベルの幅
         * NOTE: プロポーショナルフォントの為、全角の「■」幅で判断
         */
        private string ItemNameShaving(string itemName, float width)
        {
            var maxLength = width / FontSize;
            if (itemName.Length >= maxLength)
            {
                return itemName.Substring(0, (int)maxLength - 1) + "...";
            }

            return itemName;
        }

        /**
         * スクロールビューポートの矩形を取得
         * @return スクロールビューポートの矩形
         */
        private Rect GetViewportRect()
        {
            var scrollViewHeight = ScrollView.layout.height;
            var containerWidth = ItemContainer.layout.width;
            var containerHeight = ItemContainer.layout.height;
            var scrollOffset = ScrollView.verticalScroller.slider.value;

            // NOTE: Scroller.slider.visibleは更新されていない
            var hasScrollbar = containerHeight > scrollViewHeight;

            if (hasScrollbar) containerWidth -= Scroller.slider.style.width.value.value;

            return new Rect(0.0f, scrollOffset, containerWidth, scrollViewHeight);
        }

        /**
         * アイテムへスクロールする
         * @param index 対象のインデックス
         */
        protected void ToScroll(int index)
        {
            var viewportRect = GetViewportRect();
            var itemRect = ItemContainer[index].localBound;

            if (viewportRect.Contains(new Vector2(itemRect.x, itemRect.y)) &&
                viewportRect.Contains(new Vector2(itemRect.xMax, itemRect.yMax))) return;

            ScrollView.scrollOffset = new Vector2(0, itemRect.y);
        }
    }
}