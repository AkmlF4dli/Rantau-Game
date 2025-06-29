using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.EventCommon;
using RPGMaker.Codebase.CoreSystem.Lib.RepositoryCore;
using System.Collections.Generic;
using System.IO;
using JsonHelper = RPGMaker.Codebase.CoreSystem.Helper.JsonHelper;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.CoreSystem.Service.EventManagement.Repository
{
    public class EventCommonRepository
    {
        private const string JsonPath = "Assets/RPGMaker/Storage/Event/JSON/eventCommon.json";
        private static List<EventCommonDataModel> _eventCommonDataModels;

#if UNITY_EDITOR && !UNITE_WEBGL_TEST
        public void Save(EventCommonDataModel eventCommonDataModel) {
            if (_eventCommonDataModels == null) Load();

            var edited = false;
            for (var index = 0; index < _eventCommonDataModels.Count; index++)
            {
                if (_eventCommonDataModels[index].eventId != eventCommonDataModel.eventId) continue;

                _eventCommonDataModels[index] = eventCommonDataModel;
                edited = true;
                break;
            }

            if (!edited) _eventCommonDataModels.Add(eventCommonDataModel);

            File.WriteAllText(JsonPath, JsonHelper.ToJsonArray(_eventCommonDataModels));

            SetSerialNumbers();
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<EventCommonDataModel> Load() {
#else
        public async Task<List<EventCommonDataModel>> Load() {
#endif
            if (_eventCommonDataModels != null) return _eventCommonDataModels;
#if UNITY_EDITOR && !UNITE_WEBGL_TEST
            var jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(JsonPath);
            _eventCommonDataModels = JsonHelper.FromJsonArray<EventCommonDataModel>(jsonString);
#else
#if !UNITY_WEBGL
            _eventCommonDataModels = ScriptableObjectOperator.GetClass<EventCommonDataModel>(JsonPath) as List<EventCommonDataModel>;
#else
            _eventCommonDataModels = (await ScriptableObjectOperator.GetClass<EventCommonDataModel>(JsonPath)) as List<EventCommonDataModel>;
#endif
#endif

            SetSerialNumbers();

            return _eventCommonDataModels;
        }

#if UNITY_EDITOR && !UNITE_WEBGL_TEST
        public void Delete(EventCommonDataModel eventCommonDataModel) {
            var jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(JsonPath);
            var eventLists = JsonHelper.FromJsonArray<EventCommonDataModel>(jsonString);

            for (var index = 0; index < eventLists.Count; index++)
            {
                if (eventLists[index].eventId != eventCommonDataModel.eventId) continue;

                eventLists.RemoveAt(index);
                break;
            }

            File.WriteAllText(JsonPath, JsonHelper.ToJsonArray(eventLists));

            _eventCommonDataModels = eventLists;

            SetSerialNumbers();
        }
#endif

        private void SetSerialNumbers() {
            // serial numberフィールドがあるモデルには連番を設定する
            for (var i = 0; i < _eventCommonDataModels.Count; i++)
                if (_eventCommonDataModels[i] is WithSerialNumberDataModel serialNumberDataModel)
                    serialNumberDataModel.SerialNumber = i + 1;
        }
    }
}