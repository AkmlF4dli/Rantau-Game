using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.SystemSetting;
using RPGMaker.Codebase.Runtime.Common;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Map.Menu
{
    public class MenuInitialize : MonoBehaviour
    {
        private const string UI_PATH = "Assets/RPGMaker/Codebase/Runtime/Map/Asset/Prefab/";

        private SystemSettingDataModel _systemSettingDataModel;

        public static bool enableDebugTool =
#if UNITY_EDITOR
            true
#else
            false
#endif
            ;

        // 開始時にウィンドウを追加だけ行う
        private void Start() {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            StartAsync();
        }
        private async void StartAsync() {
#endif
            _systemSettingDataModel = DataManager.Self().GetSystemDataModel();

            var pattern = int.Parse(_systemSettingDataModel.uiPatternId) + 1;
            if (pattern < 1 || pattern > 6)
                pattern = 1;

            // メニューPrefabロード、生成
            var path = UI_PATH + "MenuWindow0" + pattern + ".prefab";
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var menuPrefab = UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<GameObject>(path);
#else
            var menuPrefab = await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<GameObject>(path);
#endif
            menuPrefab.SetActive(true);
            Instantiate(menuPrefab).transform.SetParent(transform);

            if (enableDebugTool)
            {
                // DebugTool Prefabロード、生成
                var debugToolPath = UI_PATH + "DebugToolWindow.prefab";
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var debugToolMenuPrefab = UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<GameObject>(debugToolPath);
#else
                var debugToolMenuPrefab = await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<GameObject>(debugToolPath);
#endif
                debugToolMenuPrefab.SetActive(true);
                Instantiate(debugToolMenuPrefab).transform.SetParent(transform);
            }

            //Canvas大きさ設定
            var displaySize = DataManager.Self().GetSystemDataModel()
                .DisplaySize[DataManager.Self().GetSystemDataModel().displaySize];
            var scales = transform.GetComponentsInChildren<CanvasScaler>();
            foreach (var scale in scales)
            {
                scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                //scale.referenceResolution = displaySize;
            }
        }
    }
}