using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.SkillCommon;
using RPGMaker.Codebase.CoreSystem.Lib.RepositoryCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement.Repository
{
    public class SkillCommonRepository : AbstractDatabaseRepository<SkillCommonDataModel>
    {
        protected override string JsonPath => "Assets/RPGMaker/Storage/Initializations/JSON/skill.json";
        
#if !UNITY_EDITOR || UNITE_WEBGL_TEST
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public new List<SkillCommonDataModel> Load() {
#else
        public new async Task<List<SkillCommonDataModel>> Load() {
#endif
            if (DataModels != null)
            {
                // キャッシュがあればそれを返す
                return DataModels;
            }
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataModels = ScriptableObjectOperator.GetClass<SkillCommonDataModel>(JsonPath) as List<SkillCommonDataModel>;
#else
            DataModels = (await ScriptableObjectOperator.GetClass<SkillCommonDataModel>(JsonPath)) as List<SkillCommonDataModel>;
#endif
            SetSerialNumbers();
            return DataModels;
        }
#endif
    }
}