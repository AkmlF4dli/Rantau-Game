using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Troop;
using RPGMaker.Codebase.CoreSystem.Lib.RepositoryCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement.Repository
{
    public class TroopRepository : AbstractDatabaseRepository<TroopDataModel>
    {
        protected override string JsonPath => "Assets/RPGMaker/Storage/Character/JSON/troop.json";
#if !UNITY_EDITOR || UNITE_WEBGL_TEST
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public new List<TroopDataModel> Load() {
#else
        public new async Task<List<TroopDataModel>> Load() {
#endif
            if (DataModels != null)
            {
                // キャッシュがあればそれを返す
                return DataModels;
            }
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataModels = ScriptableObjectOperator.GetClass<TroopDataModel>(JsonPath) as List<TroopDataModel>;
#else
            DataModels = (await ScriptableObjectOperator.GetClass<TroopDataModel>(JsonPath)) as List<TroopDataModel>;
#endif
            SetSerialNumbers();
            return DataModels;
        }
#endif
    }
}