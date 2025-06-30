using SimpleJSON;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace RPGMaker.Codebase.Editor.Common
{
    public class EditorNotify 
    {
        #region Notification
        private static string latestPostUri = "https://notice.rpgmakerofficial.com/wp-json/myplugin/v1/latest_post_by_product_and_language/?product=unite&language=";

        /// <summary>
        /// Notify new information.
        /// </summary>
        public static async void NoticeAsync() {
            var uri = $"{latestPostUri}{GetNotifLanguageCode()}";
            using (var webRequest = UnityWebRequest.Get(uri))
            {
                var asyncOp = webRequest.SendWebRequest();
                for (int i = 0; i < 100; i++)
                {
                    if (asyncOp.isDone)
                    {
                        break;
                    }
                    await Task.Delay(100);
                }
                if (!asyncOp.isDone)
                {
                    Debug.LogError("Timeout");
                    return;
                }
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    var jsonNode = JSON.Parse(webRequest.downloadHandler.text);
                    if (!jsonNode.IsObject) return;
                    var jsonObject = jsonNode.AsObject;
                    if (!jsonObject.HasKey("date")) return;
                    if (jsonObject.HasKey("toppage"))
                    {
                        RMUPreferences.instance.noticeToppageUrl = jsonObject["toppage"].Value;
                        RMUPreferences.instance.Save();
                    }
                    if (RMUPreferences.instance.displayNotifs)
                    {
                        var newDate = jsonObject["date"].Value;
                        if (newDate.CompareTo(RMUPreferences.instance.lastNoticeDate) > 0)
                        {
                            RMUPreferences.instance.lastNoticeDate = newDate;
                            RMUPreferences.instance.Save();
                            uri = $"{RMUPreferences.instance.noticeToppageUrl}?product=unite&theme=unite&language={GetNotifLanguageCode()}";
                            Application.OpenURL(uri);
                        }
                    }
                    return;
                }
                // Debug.LogError("Error: " + webRequest.error);
            }
            return;
        }

        private static string GetNotifLanguageCode() {
            var lang = EditorLocalize.GetNowLanguage();
            var code = "en";
            if (lang == SystemLanguage.Japanese)
            {
                code = "ja";
            }
            else if (lang == SystemLanguage.Chinese)
            {
                code = "zh";
            }
            return code;
        }

        public class PreferencesProvider : SettingsProvider
        {
            private const string SettingPath = "Preferences/RPG Maker Unite";
            //private UnityEditor.Editor _editor;

            [SettingsProvider]
            public static SettingsProvider CreateSettingProvider() {
                return new PreferencesProvider(SettingPath, SettingsScope.User, null);
            }

            public PreferencesProvider(string path, SettingsScope scopes, IEnumerable<string> keywords) : base(path, scopes, keywords) {
            }

            public override void OnGUI(string searchContext) {
                EditorGUILayout.Space(5);
                GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
                labelStyle.fontSize = 20;
                EditorGUILayout.LabelField(EditorLocalize.LocalizeText("WORD_2950"), labelStyle);
                var newValue = EditorGUI.ToggleLeft(new Rect(0, 30, 200, 20), EditorLocalize.LocalizeText("WORD_2951"), RMUPreferences.instance.displayNotifs);
                if (newValue != RMUPreferences.instance.displayNotifs)
                {
                    RMUPreferences.instance.displayNotifs = newValue;
                    RMUPreferences.instance.Save();
                }
                if (EditorGUI.LinkButton(new Rect(20, 50, 106, 20), EditorLocalize.LocalizeText("WORD_2952")))
                {
                    var uri = $"{RMUPreferences.instance.noticeToppageUrl}?product=unite&theme=unite&language={GetNotifLanguageCode()}";
                    Application.OpenURL(uri);
                }
            }
        }
        #endregion
    }
}