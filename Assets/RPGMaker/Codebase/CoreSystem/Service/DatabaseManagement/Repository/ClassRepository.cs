using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Class;
using RPGMaker.Codebase.CoreSystem.Lib.RepositoryCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement.Repository
{
    public class ClassRepository : AbstractDatabaseRepository<ClassDataModel>
    {
        protected override string JsonPath => "Assets/RPGMaker/Storage/Character/JSON/class.json";
#if !UNITY_EDITOR || UNITE_WEBGL_TEST
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public new List<ClassDataModel> Load() {
#else
        public new async Task<List<ClassDataModel>> Load() {
#endif
            if (DataModels != null)
            {
                // キャッシュがあればそれを返す
                return DataModels;
            }
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataModels = ScriptableObjectOperator.GetClass<ClassDataModel>(JsonPath) as List<ClassDataModel>;
#else
            DataModels = (await ScriptableObjectOperator.GetClass<ClassDataModel>(JsonPath)) as List<ClassDataModel>;
#endif
            SetSerialNumbers();
            return DataModels;
        }
#endif
    }
}