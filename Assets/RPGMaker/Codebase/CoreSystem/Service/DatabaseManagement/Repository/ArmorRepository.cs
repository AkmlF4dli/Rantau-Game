using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Armor;
using RPGMaker.Codebase.CoreSystem.Lib.RepositoryCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement.Repository
{
    public class ArmorRepository : AbstractDatabaseRepository<ArmorDataModel>
    {
        protected override string JsonPath => "Assets/RPGMaker/Storage/Initializations/JSON/armor.json";
#if !UNITY_EDITOR || UNITE_WEBGL_TEST
#if !UNITY_WEBGL
        public new List<ArmorDataModel> Load() {
#else
        public new async Task<List<ArmorDataModel>> Load() {
#endif
            if (DataModels != null)
            {
                // キャッシュがあればそれを返す
                return DataModels;
            }
#if !UNITY_WEBGL
            DataModels = ScriptableObjectOperator.GetClass<ArmorDataModel>(JsonPath) as List<ArmorDataModel>;
#else
            DataModels = (await ScriptableObjectOperator.GetClass<ArmorDataModel>(JsonPath)) as List<ArmorDataModel>;
#endif
            SetSerialNumbers();
            return DataModels;
        }
#endif
    }
}