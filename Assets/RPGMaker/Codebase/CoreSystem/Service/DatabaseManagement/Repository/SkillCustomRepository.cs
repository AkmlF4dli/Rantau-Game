using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.SkillCustom;
using RPGMaker.Codebase.CoreSystem.Lib.RepositoryCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement.Repository
{
    public class SkillCustomRepository : AbstractDatabaseRepository<SkillCustomDataModel>
    {
        protected override string JsonPath => "Assets/RPGMaker/Storage/Initializations/JSON/skillCustom.json";
#if !UNITY_EDITOR || UNITE_WEBGL_TEST
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public new List<SkillCustomDataModel> Load() {
#else
        public new async Task<List<SkillCustomDataModel>> Load() {
#endif
            if (DataModels != null)
            {
                // キャッシュがあればそれを返す
                return DataModels;
            }
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataModels = ScriptableObjectOperator.GetClass<SkillCustomDataModel>(JsonPath) as List<SkillCustomDataModel>;
#else
            DataModels = (await ScriptableObjectOperator.GetClass<SkillCustomDataModel>(JsonPath)) as List<SkillCustomDataModel>;
#endif
            SetSerialNumbers();
            return DataModels;
        }
#endif
    }
}