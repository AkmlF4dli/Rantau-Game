using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.UiSetting;
using System;
using System.IO;
using UnityEngine;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement.Repository
{
    public class UiSettingRepository
    {
        private const string JsonPath = "Assets/RPGMaker/Storage/Ui/JSON/ui.json";

        private static UiSettingDataModel _uiSettingDataModel;

        public void Save(UiSettingDataModel uiSettingDataModel) {
            if (uiSettingDataModel == null)
                throw new Exception("Tried to save null data model.");

            File.WriteAllText(JsonPath, JsonUtility.ToJson(uiSettingDataModel));

            // キャッシュを更新
            _uiSettingDataModel = uiSettingDataModel;
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public UiSettingDataModel Load() {
#else
        public async Task<UiSettingDataModel> Load() {
#endif
            if (_uiSettingDataModel != null)
                // キャッシュがあればをそれを返す
                return _uiSettingDataModel;
#if UNITY_EDITOR && !UNITE_WEBGL_TEST
            var jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(JsonPath);
            _uiSettingDataModel = JsonHelper.FromJson<UiSettingDataModel>(jsonString);
#else
#if !UNITY_WEBGL
            _uiSettingDataModel = ScriptableObjectOperator.GetClass<UiSettingDataModel>(JsonPath) as UiSettingDataModel;
#else
            _uiSettingDataModel = (await ScriptableObjectOperator.GetClass<UiSettingDataModel>(JsonPath)) as UiSettingDataModel;
#endif
#endif
            return _uiSettingDataModel;
        }
    }
}