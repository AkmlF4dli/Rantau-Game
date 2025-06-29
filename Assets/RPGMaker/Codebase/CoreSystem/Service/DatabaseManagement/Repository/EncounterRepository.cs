using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Encounter;
using RPGMaker.Codebase.CoreSystem.Lib.RepositoryCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement.Repository
{
    public class EncounterRepository : AbstractDatabaseRepository<EncounterDataModel>
    {
        protected override string JsonPath => "Assets/RPGMaker/Storage/Encounter/JSON/encounter.json";
        
#if !UNITY_EDITOR || UNITE_WEBGL_TEST
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public new List<EncounterDataModel> Load() {
#else
        public new async Task<List<EncounterDataModel>> Load() {
#endif
            if (DataModels != null)
            {
                // キャッシュがあればそれを返す
                return DataModels;
            }
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataModels = ScriptableObjectOperator.GetClass<EncounterDataModel>(JsonPath) as List<EncounterDataModel>;
#else
            DataModels = (await ScriptableObjectOperator.GetClass<EncounterDataModel>(JsonPath)) as List<EncounterDataModel>;
#endif
            SetSerialNumbers();
            return DataModels;
        }
#endif
    }
}