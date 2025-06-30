using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.SystemSetting;
using RPGMaker.Codebase.Runtime.Common;
using UnityEngine;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Battle.Window
{
    /// <summary>
    /// Unite用のバトルで利用するPrefab生成クラス
    /// UIパターンに応じてPrefabをロードする
    /// </summary>
    public class WindowInitialize
    {
        private SystemSettingDataModel _systemSettingDataModel;

        /// <summary>
        /// バトル用のPrefabをロードする
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public GameObject Create(GameObject obj) {
#else
        public async Task<GameObject> Create(GameObject obj) {
#endif
            // ウィンドウの作成のみ行う
            _systemSettingDataModel = DataManager.Self().GetSystemDataModel();

            var pattern = int.Parse(_systemSettingDataModel.uiPatternId) + 1;
            if (pattern < 1 || pattern > 6)
                pattern = 1;

            var window =
                Object.Instantiate(
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<GameObject>(
#else
                    await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<GameObject>(
#endif
                        "Assets/RPGMaker/Codebase/Runtime/Battle/Windows0" + pattern + ".prefab"));
            window.transform.SetParent(obj.transform, false);
            return window;
        }
    }
}