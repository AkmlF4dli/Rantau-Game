using System.Collections.Generic;
using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Enemy;
using RPGMaker.Codebase.CoreSystem.Lib.RepositoryCore;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement.Repository
{
    public class EnemyRepository : AbstractDatabaseRepository<EnemyDataModel>
    {
        protected override string JsonPath => "Assets/RPGMaker/Storage/Character/JSON/enemy.json";
#if !UNITY_EDITOR || UNITE_WEBGL_TEST
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public new List<EnemyDataModel> Load() {
#else
        public new async Task<List<EnemyDataModel>> Load() {
#endif
            if (DataModels != null)
            {
                // キャッシュがあればそれを返す
                return DataModels;
            }
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataModels = ScriptableObjectOperator.GetClass<EnemyDataModel>(JsonPath) as List<EnemyDataModel>;
#else
            DataModels = (await ScriptableObjectOperator.GetClass<EnemyDataModel>(JsonPath)) as List<EnemyDataModel>;
#endif
            SetSerialNumbers();
            return DataModels;
        }
#endif
#if ENABLE_DEVELOPMENT_FIX
        public void EnemyRatingFix() {
            if (DataModels == null)
                Load();

            for (int i = 0; i < DataModels.Count; i++)
            {
                for (int i2 = 0; i2 < DataModels[i].actions.Count; i2++)
                {
                    if (DataModels[i].actions[i2].rating > 9)
                    {
                        DataModels[i].actions[i2].rating = 9;
                        Save(DataModels);
                    }
                }
            }
        }
#endif
    }
}