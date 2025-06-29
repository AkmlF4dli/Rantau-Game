using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.Editor.Common.Window.ModalWindow;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;


namespace RPGMaker.Codebase.Editor.Common
{
    public class AnalyticsManager : ScriptableSingleton<AnalyticsManager>
    {
        const string measurement_id = "G-NLG9BE4Y1H";
        const string api_secret = "CJSM_1EKQ3WaBGQ6fKNYAw";
        private const string RMU_UserHashKey = "RMU_GAUserHashID";
        private const string RMU_CrientHashKey = "RMU_GACrientHashID";
        private const string RMU_LastPlayTimeTick = "RMU_GALastPlayTimeTick";
        private const string RMU_FirstTimeTracked = "RMU_GAFirstTimeTracked";

        // イベント名。
        public enum EventName
        {
            None,

            page_view,
            project,
            action,
            test_event_name
        }

        // イベントパラメータ。
        // C#の予約後と同じになるものは定義できないので、それについては末尾に'_'を追加しているが、
        // uxmlファイルへの記述では'_'は不要。
        public enum EventParameter
        {
            None,

            title,
            ui_setting,
            word,
            option,
            character,
            vehicle,
            job,
            battle_scene,
            enemy,
            troop,
            sound,
            se,
            skill_common,
            skill_basic,
            skill_custom,
            state_basic,
            state_custom,
            equipment_weapon,
            equipment_armor,
            equipment_item,
            type_element,
            type_skill,
            type_weapon,
            type_armor,
            type_equipment,
            battle_effect,
            common_event,
            resource_character,
            resource_ballon_icon,
            resource_sv_character,
            resource_battle_effect,
            environment,
            switch_,
            variable,
            map_tile,
            map_tilegroup,
            map_edit,
            map_battle_edit,
            map_event,
            event_search,
            outline,

            initialize,
            new_,
            open,
            close,
            save,
            deploy,
            testplay,
            help,

            test_event_parameter
        }

        // VisualElementのクラス名として埋め込まれたアナリティクス用タグに一致する正規表現。
        private readonly Regex analyticsTagRegex;
        private bool isInitialized;
        private DateTime startDateTime = DateTime.Now;
        private readonly HashSet<(EventName, EventParameter)> maybeReplaceToOutlineEvents;

        // シングルトンなのでprivateなコンストラクタ。
        private AnalyticsManager() {
            var eventNames = string.Join("|", ((EventName[]) System.Enum.GetValues(typeof(EventName))).Select(enumName => enumName.ToString()));
            var eventParameters = string.Join("|", ((EventParameter[]) System.Enum.GetValues(typeof(EventParameter))).Select(enumParameter => enumParameter.ToString()));
            analyticsTagRegex = new Regex($"^AnalyticsTag__(?<NAME>({eventNames}))__(?<PARAMETER>({eventParameters}))$", RegexOptions.IgnoreCase);
            maybeReplaceToOutlineEvents = new HashSet<(EventName, EventParameter)>
            {
                (EventName.page_view, EventParameter.map_edit),
                (EventName.page_view, EventParameter.map_battle_edit),
                (EventName.page_view, EventParameter.map_event)
            };
        }

        // "AnalyticsTag__{イベント名}__{イベントパラメータ}"という名のクラスが設定してあるVisualElementをHierarchyの
        // 親方向に探していき、最初に見つけたものの『イベント名』と『イベントパラメータ』を送信する。
        public static void PostEventFromHierarchy(VisualElement ve) {
            instance.PostEventFromHierarchyBody(ve);
        }

        public static void PostEvent(EventName eventName, EventParameter eventParameter) {
            instance.PostEventBody(eventName, eventParameter);
        }

        private void PostEventFromHierarchyBody(VisualElement ve) {

            (EventName, EventParameter) RecursiveSearch(VisualElement ve) {
                foreach (var className in ve.GetClasses())
                {
                    var match = analyticsTagRegex.Match(className);
                    if (match.Success)
                    {
                        if (CSharpUtil.TryParse(match.Groups["NAME"].Value, out EventName eventName) &&
                            CSharpUtil.TryParse(match.Groups["PARAMETER"].Value, out EventParameter eventParameter))
                        {
                            // 差し替えの可能性のあるイベントの場合、更に親方向に検索して差し替えを試行。
                            if (maybeReplaceToOutlineEvents.Contains((eventName, eventParameter)) && ve.parent != null)
                            {
                                var eventToReplace = (EventName.page_view, EventParameter.outline);
                                if (RecursiveSearch(ve.parent) == eventToReplace) return eventToReplace;
                            }
                            return (eventName, eventParameter);
                        }
                    }
                }

                if (ve.parent != null)
                {
                    return RecursiveSearch(ve.parent);
                }
                return (EventName.None, EventParameter.None);
            }

            var (eventName, eventParameter) = RecursiveSearch(ve);
            if (eventName != EventName.None && eventParameter != EventParameter.None)
            {
                PostEvent(eventName, eventParameter);
            }
        }

        private static string GetGAUserHashID() {
            if (EditorPrefs.HasKey(RMU_UserHashKey) == false)
            {
                EditorPrefs.SetString(RMU_UserHashKey, Guid.NewGuid().ToString());
            }
            return EditorPrefs.GetString(RMU_UserHashKey);
        }

        private static string GetGAClientID() {
            if (EditorPrefs.HasKey(RMU_CrientHashKey) == false)
            {
                EditorPrefs.SetString(RMU_CrientHashKey, $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{UnityEngine.Random.Range(100000000, 999999999)}");
            }
            return EditorPrefs.GetString(RMU_CrientHashKey);
        }
        private IEnumerator PostEventCoroutine(string json) {
            var jsonBytes = Encoding.UTF8.GetBytes(json);
            var uri = $"https://www.google-analytics.com/mp/collect?measurement_id={measurement_id}&api_secret={api_secret}";
            using var webRequest = new UnityWebRequest(uri, "POST");
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("User-Agent", $"Unity {Application.unityVersion}");
            webRequest.uploadHandler = new UploadHandlerRaw(jsonBytes);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            yield return webRequest.SendWebRequest();
            //Debug.Log($"target:{uri}\njson:{json}\n Result: {webRequest.result}, StatusCode: {webRequest.responseCode}, Body: {webRequest.downloadHandler.text}");
        }

        private void PostEventBody(EventName eventName, EventParameter eventParameter) {


            if (eventParameter == EventParameter.initialize)
            {
                if (isInitialized == false)
                {
                    isInitialized = true;
                    var country = new RegionInfo(CultureInfo.CurrentCulture.LCID);
                    var json = @$"{{""user_id"":""{GetGAUserHashID()}"",""client_id"":""{GetGAClientID()}"",""events"":[";

                    if (EditorPrefs.HasKey(RMU_FirstTimeTracked) == false)
                    {
                        // PC単位での初回起動
                        json += @$"{{""name"":""first_time_user"",""params"":{{""country"":""{country}""}}}},";
                        EditorPrefs.SetString(RMU_FirstTimeTracked, "tracked");
                    }
                    if (EditorPrefs.HasKey(RMU_LastPlayTimeTick))
                    {
                        // 前回の使用時間
                        json += @$"{{""name"":""exit_unite"",""params"":{{""play_total_minutes"":{EditorPrefs.GetString(RMU_LastPlayTimeTick)}}}}},";
                        EditorPrefs.DeleteKey(RMU_LastPlayTimeTick);
                    }
                    // 通常起動
                    var currentUniteVersion = UniteVersionInfo.GetVersionString();
                    json += @$"{{""name"":""initialize"",""params"":{{""language"":""{country}"",""country"":""{country}"",""platform"":""assetstore"",""version"":""{currentUniteVersion}"",""engagement_time_msec"":30}}}}]}}";
                    EditorCoroutineUtility.StartCoroutine(PostEventCoroutine(json), this);
                }
                // initializeは初回のみでActionとして送信しない
                return;
            }

            if (isInitialized)
            {
                var json = @$"{{""user_id"":""{GetGAUserHashID()}"",""client_id"":""{GetGAClientID()}"",""events"":[{{""name"":""{eventName.ToString()}"",""params"":{{""parameter"":""{eventParameter.ToString()}""}}}}]}}";
                EditorCoroutineUtility.StartCoroutine(PostEventCoroutine(json), this);
            }
        }

        public void OnEditorQuit() {
            if (isInitialized == true)
            {
                var span = DateTime.Now - startDateTime;
                EditorPrefs.SetString(RMU_LastPlayTimeTick, $"{Math.Ceiling(span.TotalMinutes)}");
            }
        }
    }

    [InitializeOnLoad]
    public static class OnEditorQuitHandler
    {
        static OnEditorQuitHandler() {
            EditorApplication.quitting += OnEditorQuit;
        }

        private static void OnEditorQuit() {
            AnalyticsManager.instance.OnEditorQuit();
        }
    }
}