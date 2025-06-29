using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Vehicle;
using RPGMaker.Codebase.CoreSystem.Lib.RepositoryCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement.Repository
{
    public class VehicleRepository : AbstractDatabaseRepository<VehiclesDataModel>
    {
        protected override string JsonPath => "Assets/RPGMaker/Storage/Character/JSON/vehicles.json";
#if !UNITY_EDITOR || UNITE_WEBGL_TEST
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public new List<VehiclesDataModel> Load() {
#else
        public new async Task<List<VehiclesDataModel>> Load() {
#endif
            if (DataModels != null)
            {
                // キャッシュがあればそれを返す
                return DataModels;
            }
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataModels = ScriptableObjectOperator.GetClass<VehiclesDataModel>(JsonPath) as List<VehiclesDataModel>;
#else
            DataModels = (await ScriptableObjectOperator.GetClass<VehiclesDataModel>(JsonPath)) as List<VehiclesDataModel>;
#endif
            SetSerialNumbers();
            return DataModels;
        }
#endif
    }
}