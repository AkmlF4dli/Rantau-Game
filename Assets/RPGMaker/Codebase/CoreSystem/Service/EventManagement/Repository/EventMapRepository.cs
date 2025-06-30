using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.EventMap;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.CoreSystem.Service.EventManagement.Repository
{
    public class EventMapRepository
    {
        private const string JsonFile = "Assets/RPGMaker/Storage/Event/JSON/eventMap.json";
        private const string SO_PATH = "Assets/RPGMaker/Storage/Event/SO/eventMap.asset";
        private static List<EventMapDataModel> _eventMapDataModels;

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<EventMapDataModel> Load() {
#else
        public async Task<List<EventMapDataModel>> Load() {
#endif
            if (_eventMapDataModels != null) return _eventMapDataModels;
#if UNITY_EDITOR && !UNITE_WEBGL_TEST
            var jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(JsonFile);
            _eventMapDataModels = JsonHelper.FromJsonArray<EventMapDataModel>(jsonString);
#else
#if !UNITY_WEBGL
            _eventMapDataModels = ScriptableObjectOperator.GetClass<EventMapDataModel>(JsonFile) as List<EventMapDataModel>;
#else
            _eventMapDataModels = (await ScriptableObjectOperator.GetClass<EventMapDataModel>(JsonFile)) as List<EventMapDataModel>;
#endif
#endif

            //ID重複のデータが存在した場合には、後勝ちとする
            var removedList = new List<int>();
            var dic = new Dictionary<KeyValuePair<string, string>, int>();
            for (int i = 0; i < _eventMapDataModels.Count; i++)
            {
                var key = new KeyValuePair<string, string>(_eventMapDataModels[i].mapId, _eventMapDataModels[i].eventId);
                if (dic.ContainsKey(key))
                {
                    removedList.Add(dic[key]);
                    dic.Remove(key);
                }
                dic.Add(key, i);
            }
            for (int i = removedList.Count - 1; i >= 0; i--)
            {
                _eventMapDataModels.RemoveAt(removedList[i]);
            }


            SetSerialNumbers();

            return _eventMapDataModels;
        }

#if UNITY_EDITOR && !UNITE_WEBGL_TEST
        public void Save() {
            File.WriteAllText(JsonFile, JsonHelper.ToJsonArray(_eventMapDataModels));

            SetSerialNumbers();
        }
#endif

#if UNITY_EDITOR && !UNITE_WEBGL_TEST
        public void Save(EventMapDataModel eventMapDataModel) {
            var eventMapLists = Load();

            var edited = false;
            for (var index = 0; index < eventMapLists.Count; index++)
            {
                if (eventMapLists[index].mapId != eventMapDataModel.mapId) continue;
                if (eventMapLists[index].eventId != eventMapDataModel.eventId) continue;

                eventMapLists[index] = eventMapDataModel;
                edited = true;
                break;
            }

            if (!edited) eventMapLists.Add(eventMapDataModel);

            File.WriteAllText(JsonFile, JsonHelper.ToJsonArray(eventMapLists));

            _eventMapDataModels = eventMapLists;

            SetSerialNumbers();
        }
#endif

        /**
         * マップに紐づくイベントを取得する
         */
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<EventMapDataModel> LoadEventMapEntitiesByMapId(string mapId) {
            Load();

            return _eventMapDataModels.FindAll(eventEntity => eventEntity.mapId == mapId);
        }
#else
        public async Task<List<EventMapDataModel>> LoadEventMapEntitiesByMapId(string mapId) {
            await Load();
            return _eventMapDataModels.FindAll(eventEntity => eventEntity.mapId == mapId);
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<EventMapDataModel> LoadEventMapEntitiesByMapIdFromJson(string mapId) {
#else
        public async Task<List<EventMapDataModel>> LoadEventMapEntitiesByMapIdFromJson(string mapId) {
#endif
#if UNITY_EDITOR && !UNITE_WEBGL_TEST
            var jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(JsonFile);
            var eventMapDataModels = JsonHelper.FromJsonArray<EventMapDataModel>(jsonString);
#else
#if !UNITY_WEBGL
            var eventMapDataModels = ScriptableObjectOperator.GetClass<EventMapDataModel>(JsonFile) as List<EventMapDataModel>;
#else
            var eventMapDataModels = (await ScriptableObjectOperator.GetClass<EventMapDataModel>(JsonFile)) as List<EventMapDataModel>;
#endif
#endif

            return eventMapDataModels.FindAll(eventMapDataModel => eventMapDataModel.mapId == mapId);
        }

#if UNITY_EDITOR && !UNITE_WEBGL_TEST
        public void Delete(EventMapDataModel eventMapDataModel) {
            var eventMapLists = Load();

            eventMapLists.RemoveAll(eventMap =>
                eventMap.mapId == eventMapDataModel.mapId && eventMap.eventId == eventMapDataModel.eventId);

            File.WriteAllText(JsonFile, JsonHelper.ToJsonArray(eventMapLists));

            _eventMapDataModels = eventMapLists;

            SetSerialNumbers();
        }
#endif

        private void SetSerialNumbers() 
        {
            //U277 同マップにあるイベントに通し番号を付ける
            List<string> MapIDList = new List<string>();
            //マップID
            foreach (var EventMapData in _eventMapDataModels)
            {
                if (MapIDList.Contains(EventMapData.mapId) == false)
                {
                    MapIDList.Add(EventMapData.mapId);
                }
            }
            //マップ単位で番号を付け直す
            foreach (var MapID in MapIDList)
            {
                int Num = 1;
                foreach (var EventMapData in _eventMapDataModels)
                {
                    if (EventMapData.mapId == MapID)
                    {
                        EventMapData.SerialNumber = Num;
                        Num++;
                    }
                }
            }
        }
    }
}