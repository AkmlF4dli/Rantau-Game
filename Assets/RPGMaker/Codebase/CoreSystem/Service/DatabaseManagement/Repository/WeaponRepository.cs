using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Weapon;
using RPGMaker.Codebase.CoreSystem.Lib.RepositoryCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement.Repository
{
    public class WeaponRepository : AbstractDatabaseRepository<WeaponDataModel>
    {
        protected override string JsonPath => "Assets/RPGMaker/Storage/Initializations/JSON/weapon.json";
#if !UNITY_EDITOR || UNITE_WEBGL_TEST
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public new List<WeaponDataModel> Load() {
#else
        public new async Task<List<WeaponDataModel>> Load() {
#endif
            if (DataModels != null)
            {
                // キャッシュがあればそれを返す
                return DataModels;
            }
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataModels = ScriptableObjectOperator.GetClass<WeaponDataModel>(JsonPath) as List<WeaponDataModel>;
#else
            DataModels = (await ScriptableObjectOperator.GetClass<WeaponDataModel>(JsonPath)) as List<WeaponDataModel>;
#endif
            SetSerialNumbers();
            return DataModels;
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SetWeaponEquipType() {
            
            var system = new SystemRepository().Load();
            Load();

            for (int i = 0; i < DataModels.Count; i++)
            {
                if (DataModels[i].basic.equipmentTypeId == "")
                {
                    DataModels[i].basic.equipmentTypeId = system.equipTypes[0].id;
                }
            }

            Save(DataModels);
        }

#endif
    }
}