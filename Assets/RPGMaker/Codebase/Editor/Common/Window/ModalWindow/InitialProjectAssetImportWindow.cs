using RPGMaker.Codebase.Editor.Common.View;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGMaker.Codebase.Editor.Common.Window.ModalWindow
{
    public class InitialProjectAssetImportWindow : BaseModalWindow
    {
        // テキスト
        private const string TITLE_TEXT = "WORD_2100";
        private const string DESCRIPTION_TEXT = "WORD_5005"; //"作成するプロジェクトを選択してください";
        private const string DESCRIPTION_TEXT2 = "WORD_5010"; //"指定言語のプロジェクトテンプレートがインストール\nされていません";
        private const string PROJECT_NAME_TEXT = "WORD_5008"; //"プロジェクト名";
        private const string PATH_TEXT = "WORD_5009"; //"保存場所";

        public void ShowWindow() {
            var wnd = GetWindow<InitialProjectAssetImportWindow>();
            // 処理タイトル名適用
            wnd.titleContent = new GUIContent(EditorLocalize.LocalizeText(TITLE_TEXT));
            wnd.Init();
            //サイズ固定用
            var size = new Vector2(600, 480);
            wnd.minSize = size;
            wnd.maxSize = size;
            wnd.maximized = false;
        }

        public override void Init() {

            // Viewの作成
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/RPGMaker/Codebase/Editor/Common/Window/ModalWindow/Uxml/initialize_project_asset_import.uxml").CloneTree();
            EditorLocalize.LocalizeElements(visualTreeAsset);
            rootVisualElement.Add(visualTreeAsset);

            var radioFullButton = visualTreeAsset.Q<RadioButton>("radio_full_asset");
            radioFullButton.value = false;

            var projectPathLabel = visualTreeAsset.Q<Label>("path_label");
            projectPathLabel.text = EditorLocalize.LocalizeText(PATH_TEXT);

            var projectPathTextField = visualTreeAsset.Q<ImTextField>("path");
            projectPathTextField.value = Application.dataPath.Replace("/Assets", "");
            projectPathTextField.isReadOnly = true;

#if UNITY_EDITOR_WIN
            // 2階層上
            var folderSub = "../../";
#else
            // 4階層上
            var folderSub = "../../../../";
#endif
            // テンポラリの最新バージョンを探す
            var temporaryRPGMakerFolderPath = Path.Combine(Application.persistentDataPath, folderSub, ".RPGMaker");
            var currentUniteVersion = "";
            Version highestVersion = null;
            var directories = Directory.GetDirectories(temporaryRPGMakerFolderPath);
            foreach( var dir  in directories)
            {
                if( Version.TryParse( Path.GetFileName(dir), out var version ))
                {
                    if (highestVersion == null || version > highestVersion)
                    {
                        highestVersion = version;
                        currentUniteVersion = Path.GetFileName(dir);
                    }
                }
            }

            // 各zipファイル
            var versionFolderPath = Path.Combine(temporaryRPGMakerFolderPath, currentUniteVersion);
            var installStorageZipNameList = new List<string>();
            var okButton = visualTreeAsset.Q<Button>("OK_button");
            okButton.clicked += () =>
            {
                var ProjectPath = Path.Combine(projectPathTextField.value);
                ZipFile.ExtractToDirectory(Path.Combine(versionFolderPath, $"project_base_v{currentUniteVersion}.zip"), ProjectPath, true);
                // バージョンファイルを追加
                {
                    var versionTextPath = Path.Combine(ProjectPath, "Packages/jp.ggg.rpgmaker.unite/version.txt");
                    using var writer = new StreamWriter(versionTextPath, false, Encoding.UTF8);
                    writer.Write(currentUniteVersion);
                }
                // Storageの展開処理
                var StoragePath = Path.Combine(ProjectPath, "Assets/RPGMaker/Storage");
                if (!Directory.Exists(StoragePath))
                {
                    Directory.CreateDirectory(StoragePath);
                }
                // storageのZIPを展開
                installStorageZipNameList.ForEach(installZip => ZipFile.ExtractToDirectory(Path.Combine(versionFolderPath, $"{installZip}{currentUniteVersion}.zip"), StoragePath, true));
                // 保存
                EditorApplication.ExecuteMenuItem("File/Save");
                // このWindowを閉じる
                Close();
                // 作成したPJを開く
                EditorApplication.OpenProject(ProjectPath);
            };

            // CANCELボタン
            var cancelButton = visualTreeAsset.Q<Button>("CANCEL_button");
            cancelButton.clicked += () => {
                Close();
            };

            var langSelect = visualTreeAsset.Q<VisualElement>("langSelect");
            langSelect.Clear();

            var languageNames = new List<string>(){
                EditorLocalize.LocalizeText("WORD_2102"),
                EditorLocalize.LocalizeText("WORD_2103"),
                EditorLocalize.LocalizeText("WORD_2104")
            };

            // 各zipファイルが存在しているかどうかを確認し、無ければ説明欄の文言を切り替え、OKボタンを押下不可とする
            // 現在の言語設定
            var selectLanguageIndex = 0;
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Japanese:
                    selectLanguageIndex = 0;
                    break;
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.ChineseTraditional:
                    selectLanguageIndex = 2;
                    break;
                default:
                    selectLanguageIndex = 1;
                    break;
            }
            var langSelectPopupField = new PopupFieldBase<string>(languageNames, selectLanguageIndex);
            langSelect.Add(langSelectPopupField);
            langSelectPopupField.RegisterValueChangedCallback(evt =>
            {
                selectLanguageIndex = langSelectPopupField.index;
                UpdateOKButtonEnable();
            });

            // ボタン設定
            {
                var iconPath = EditorGUIUtility.isProSkin ? "Dark" : "Light";
                var objects = AssetDatabase.LoadAllAssetsAtPath($"Assets/RPGMaker/SystemResource/MenuIcon/{iconPath}.png");
                var sprite = (Sprite) objects.FirstOrDefault(obj => obj.name == "uibl_icon_menu_002");

                var projectPathSelectButton = visualTreeAsset.Q<Button>("path_button");
                projectPathSelectButton.style.backgroundImage = new StyleBackground(sprite);
                projectPathSelectButton.style.width = sprite.rect.width;
                projectPathSelectButton.clicked += () =>
                {
                    var result = EditorUtility.OpenFolderPanel("Open Folder", projectPathTextField.value, "");
                    if (!string.IsNullOrEmpty(result))
                    {
                        projectPathTextField.value = result;
                        UpdateOKButtonEnable();
                    }
                };
            }

            // ボタン状態のチェック
            UpdateOKButtonEnable();

            /// <summary>
            /// OKボタンの有効/無効切替
            /// </summary>
            void UpdateOKButtonEnable() {
                var descriptionLabel = visualTreeAsset.Q<Label>("description_text");
                installStorageZipNameList.Clear();
                installStorageZipNameList.Add("masterdata_jp_v");
                if (radioFullButton.value)
                {
                    installStorageZipNameList.Add("defaultgame_jp_v");
                }
                if (selectLanguageIndex == 2)
                {
                    installStorageZipNameList.Add(radioFullButton.value ? "defaultgame_ch_v" : "masterdata_ch_v");
                }
                else if (selectLanguageIndex != 0)
                {
                    installStorageZipNameList.Add(radioFullButton.value ? "defaultgame_en_v" : "masterdata_en_v");
                }

                var notExist = installStorageZipNameList.Any(fname => File.Exists(Path.Combine(versionFolderPath, $"{fname}{currentUniteVersion}.zip")) == false);

                descriptionLabel.text = EditorLocalize.LocalizeText(notExist ? DESCRIPTION_TEXT2 : DESCRIPTION_TEXT);
                if (notExist)
                {
                    okButton.SetEnabled(false);
                }
                else
                {
                    // 選択中のフォルダ内が空かチェック
                    var path = projectPathTextField.value;
                    if (Directory.GetFiles(path).Length == 0 && Directory.GetDirectories(path).Length == 0)
                    {
                        okButton.SetEnabled(true);
                    }
                    else
                    {
                        okButton.SetEnabled(false);
                    }
                }
            }
        }





    }
}