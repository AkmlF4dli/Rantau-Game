using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Lib.Auth;
using RPGMaker.Codebase.Editor.Common;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGMaker.Codebase
{
    public class LoginWindow : EditorWindow
    {
        static readonly Vector2 WindowSize = new Vector2(450, 600);

        TaskCompletionSource<bool> mAsycTaskResult = new TaskCompletionSource<bool>();

        [Serializable]
        class GlobalSetting
        {
            [SerializeField] string invoiceID = "";

            public string InvoiceID { get => invoiceID; set => invoiceID = value; }
        }

        public static Task<bool> OpenAsync() {
            var isOpen = EditorWindow.HasOpenInstances<LoginWindow>();
            if (isOpen)
            {
                // 開き済みの場合は多重でオープンしない
                return Task.FromResult(false);
            }
            else
            {
                var win = GetWindowWithRect<LoginWindow>(
                    new Rect(Screen.width / 2, Screen.height / 2, WindowSize.x, WindowSize.y),
                    true);
                win.ShowUtility();
                return win.mAsycTaskResult.Task;
            }
        }

        void OnEnable() {
            minSize = WindowSize;
            maxSize = WindowSize;

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/RPGMaker/Codebase/Editor/Common/Window/ModalWindow/Uxml/LoginWindow_window.uxml");
            if (visualTree == null)
            {
                EditorApplication.delayCall += () => Close();
                return;
            }
            var tree = visualTree.CloneTree();
            rootVisualElement.Clear();
            rootVisualElement.Add(tree);
            tree.style.flexGrow = 1;

            EditorLocalize.LocalizeElements(tree);

            var loadingView = tree.Q<VisualElement>("LoadingView");
            var contentRoot = tree.Q<VisualElement>("ContentRoot");
            var message = tree.Q<TextField>("Message");
            var inputValue = "";

            Action checkAction = async () =>
            {
                contentRoot.SetEnabled(false);
                loadingView.visible = true;

                await Auth.AttemptToAuthenticate(inputValue);

                if (Auth.IsAuthenticated)
                {
                    message.value = EditorLocalize.LocalizeText("WORD_1671"); //"認証に成功しました";
                    mAsycTaskResult.TrySetResult(true);
                    SaveInvoiceID(inputValue);
                    EditorApplication.delayCall += () => Close();
                }
                else
                {
                    message.value = EditorLocalize.LocalizeText("WORD_1672"); //"認証に失敗しました";
                }
                contentRoot.SetEnabled(true);
                loadingView.visible = false;
            };

            var bt = SetButtonClicked("SendPurchaseID", () =>
            {
                checkAction();
            });


            var input = tree.Q<TextField>("PurchaseID");
            input.RegisterValueChangedCallback(evt =>
            {
                inputValue = evt.newValue;
                if (inputValue.StartsWith("IN"))
                {
                    inputValue = inputValue.Substring(2);
                }
                if (ulong.TryParse(inputValue, out _))
                {
                    message.value = "";
                    bt.SetEnabled(true);
                }
                else
                {
                    message.value = EditorLocalize.LocalizeText("WORD_1673"); //"IDは数字のみを入力してください";
                    bt.SetEnabled(false);
                }
            });
            SetButtonClicked("OpenAssetStoreOrdersPage", () =>
            {
                Application.OpenURL("https://assetstore.unity.com/orders");
            });
            SetButtonClicked("OpenOnlineManualPage", () =>
            {
                var manualURL = EditorLocalize.LocalizeText("WORD_1674"); //マニュアルのURL;
                Application.OpenURL(manualURL);
            });

            loadingView.visible = false;
            input.value = LoadInvoiceID();
            if (input.value != "")
            {
                inputValue = input.value;
                checkAction();
            } else
            {
                bt.SetEnabled(false);
            }
        }

        private void OnDestroy() {
            mAsycTaskResult.TrySetResult(false);
        }

        Button SetButtonClicked(string name, Action onClick) {
            var bt = rootVisualElement.Q<Button>(name);
            if (bt != null)
            {
                bt.clicked += onClick;
            }
            return bt;
        }

        string LoadInvoiceID() {
            var filepath = Path.Combine(PathManager.GetRPGMakerPersistentFolderPath(), "GlobalSetting.json");
            // Versionのファイルを更新
            if (File.Exists(filepath))
            {
                using var reader = new StreamReader(filepath, Encoding.UTF8);
                var json = reader.ReadToEnd();
                var globalSetting = JsonUtility.FromJson<GlobalSetting>(json);
                return globalSetting.InvoiceID;
            }
            return "";
        }

        void SaveInvoiceID(string id) {

            var globalSetting = new GlobalSetting();
            globalSetting.InvoiceID = id;
            var filepath = Path.Combine(PathManager.GetRPGMakerPersistentFolderPath(), "GlobalSetting.json");
            // Versionのファイルを更新
            {
                using var writer = new StreamWriter(filepath, false, Encoding.UTF8);
                writer.Write(JsonUtility.ToJson(globalSetting));
            }
        }
    }
}
