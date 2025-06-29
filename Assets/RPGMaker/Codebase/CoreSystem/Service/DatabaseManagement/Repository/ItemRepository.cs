using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Item;
using RPGMaker.Codebase.CoreSystem.Lib.RepositoryCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement.Repository
{
    public class ItemRepository : AbstractDatabaseRepository<ItemDataModel>
    {
        protected override string JsonPath => "Assets/RPGMaker/Storage/Item/JSON/item.json";
        public void DeleteItem(int itemId) {
            throw new NotImplementedException();
        }

        public void ChangeMaximum(int maximumNum) {
            throw new NotImplementedException();
        }

#if !UNITY_EDITOR || UNITE_WEBGL_TEST
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public new List<ItemDataModel> Load() {
#else
        public new async Task<List<ItemDataModel>> Load() {
#endif
            if (DataModels != null)
            {
                // キャッシュがあればそれを返す
                return DataModels;
            }
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataModels = ScriptableObjectOperator.GetClass<ItemDataModel>(JsonPath) as List<ItemDataModel>;
#else
            DataModels = (await ScriptableObjectOperator.GetClass<ItemDataModel>(JsonPath)) as List<ItemDataModel>;
#endif
            SetSerialNumbers();
            return DataModels;
        }
#endif
    }
}