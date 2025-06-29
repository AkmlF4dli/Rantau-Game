using System.Collections.Generic;
using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.CharacterActor;
using RPGMaker.Codebase.CoreSystem.Lib.RepositoryCore;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement.Repository
{
    public class CharacterActorRepository : AbstractDatabaseRepository<CharacterActorDataModel>
    {
        protected override string JsonPath => "Assets/RPGMaker/Storage/Character/JSON/characterActor.json";
#if !UNITY_EDITOR || UNITE_WEBGL_TEST
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public new List<CharacterActorDataModel> Load() {
#else
        public new async Task<List<CharacterActorDataModel>> Load() {
#endif
            if (DataModels != null)
            {
                // キャッシュがあればそれを返す
                return DataModels;
            }
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataModels = ScriptableObjectOperator.GetClass<CharacterActorDataModel>(JsonPath) as List<CharacterActorDataModel>;
#else
            DataModels = (await ScriptableObjectOperator.GetClass<CharacterActorDataModel>(JsonPath)) as List<CharacterActorDataModel>;
#endif
            SetSerialNumbers();
            return DataModels;
        }
#endif
    }
}