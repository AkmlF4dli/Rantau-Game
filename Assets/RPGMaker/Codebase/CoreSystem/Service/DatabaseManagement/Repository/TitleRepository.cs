using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime;
using System;
using System.IO;
using UnityEngine;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement.Repository
{
    public class TitleRepository
    {
        private const string JsonPath = "Assets/RPGMaker/Storage/Initializations/JSON/title.json";

        private static RuntimeTitleDataModel _runtimeTitleDataModel;

        public void Save(RuntimeTitleDataModel runtimeTitleDataModel) {
            if (runtimeTitleDataModel == null)
                throw new Exception("Tried to save null data model.");

            File.WriteAllText(JsonPath, JsonUtility.ToJson(runtimeTitleDataModel));

            // キャッシュを更新
            _runtimeTitleDataModel = runtimeTitleDataModel;
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public RuntimeTitleDataModel Load() {
#else
        public async Task<RuntimeTitleDataModel> Load() {
#endif
            if (_runtimeTitleDataModel != null)
            {
                // キャッシュがあればそれを返す
                return _runtimeTitleDataModel;
            }
#if UNITY_EDITOR && !UNITE_WEBGL_TEST
            var jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(JsonPath);
            _runtimeTitleDataModel = JsonHelper.FromJson<RuntimeTitleDataModel>(jsonString);
#else
            _runtimeTitleDataModel =
#if !UNITY_WEBGL
 ScriptableObjectOperator.GetClass<RuntimeTitleDataModel>(JsonPath) as RuntimeTitleDataModel;
#else
 (await ScriptableObjectOperator.GetClass<RuntimeTitleDataModel>(JsonPath)) as RuntimeTitleDataModel;
#endif
#endif
            return _runtimeTitleDataModel;
        }
    }
}