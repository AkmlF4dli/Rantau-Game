using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Animation;
using RPGMaker.Codebase.CoreSystem.Lib.RepositoryCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement.Repository
{
    public class AnimationRepository : AbstractDatabaseRepository<AnimationDataModel>
    {
        protected override string JsonPath => "Assets/RPGMaker/Storage/Animation/JSON/animation.json";
#if !UNITY_EDITOR || UNITE_WEBGL_TEST
#if !UNITY_WEBGL
        public new List<AnimationDataModel> Load() {
#else
        public new async Task<List<AnimationDataModel>> Load() {
#endif
            if (DataModels != null)
            {
                // キャッシュがあればそれを返す
                return DataModels;
            }
#if !UNITY_WEBGL
            DataModels = ScriptableObjectOperator.GetClass<AnimationDataModel>(JsonPath) as List<AnimationDataModel>;
#else
            DataModels = (await ScriptableObjectOperator.GetClass<AnimationDataModel>(JsonPath)) as List<AnimationDataModel>;
#endif
            SetSerialNumbers();
            return DataModels;
        }
#endif
    }
}