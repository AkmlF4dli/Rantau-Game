#define USE_PARTIAL_LOOP
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RPGMaker.Codebase.CoreSystem.Helper.SO;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Animation;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Armor;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.AssetManage;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.CharacterActor;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Class;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Encounter;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Enemy;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.EventBattle;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.EventCommon;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.EventMap;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Flag;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Item;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Map;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.SkillCommon;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.SkillCustom;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.State;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.SystemSetting;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Troop;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.UiSetting;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Vehicle;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Weapon;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.WordDefinition;
using RPGMaker.Codebase.CoreSystem.Service.MapManagement.Repository;
using UnityEngine;
#if USE_PARTIAL_LOOP
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Sound;
#endif
using System.Threading.Tasks;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RPGMaker.Codebase.CoreSystem.Helper
{
    public static class ScriptableObjectOperator
    {
        //====================================================================================================
        // 読込パス
        //====================================================================================================
        private const string JSON_PATH_ANIMATION = "Assets/RPGMaker/Storage/Animation/JSON/animation.json";
        private const string SO_PATH_ANIMATION = "Assets/RPGMaker/Storage/Animation/SO/animation.asset";
        private const string JSON_PATH_ARMOR = "Assets/RPGMaker/Storage/Initializations/JSON/armor.json";
        private const string SO_PATH_ARMOR = "Assets/RPGMaker/Storage/Initializations/SO/armor.asset";
        private const string JSON_PATH_ASSETMANAGE = "Assets/RPGMaker/Storage/AssetManage/JSON";
        private const string SO_PATH_ASSETMANAGE = "Assets/RPGMaker/Storage/AssetManage/SO";
        private const string JSON_PATH_ASSETMANAGE_DATA = "Assets/RPGMaker/Storage/AssetManage/JSON/assetsData.json";
        private const string SO_PATH_ASSETMANAGE_DATA = "Assets/RPGMaker/Storage/AssetManage/SO/assetsData.asset";
        private const string JSON_PATH_AUTOGUID = "Assets/RPGMaker/Storage/Initializations/JSON/autoGuide.json";
        private const string SO_PATH_AUTOGUID = "Assets/RPGMaker/Storage/Initializations/SO/autoGuide.asset";
        private const string JSON_PATH_CHARACTERACTOR = "Assets/RPGMaker/Storage/Character/JSON/characterActor.json";
        private const string SO_PATH_CHARACTERACTOR = "Assets/RPGMaker/Storage/Character/SO/characterActor.asset";
        private const string JSON_PATH_CLASS = "Assets/RPGMaker/Storage/Character/JSON/class.json";
        private const string SO_PATH_CLASS = "Assets/RPGMaker/Storage/Character/SO/class.asset";
        private const string JSON_PATH_ENCOUNTER = "Assets/RPGMaker/Storage/Encounter/JSON/encounter.json";
        private const string SO_PATH_ENCOUNTER = "Assets/RPGMaker/Storage/Encounter/SO/encounter.asset";
        private const string JSON_PATH_ENEMY = "Assets/RPGMaker/Storage/Character/JSON/enemy.json";
        private const string SO_PATH_ENEMY = "Assets/RPGMaker/Storage/Character/SO/enemy.asset";
        private const string JSON_PATH_FLAGS = "Assets/RPGMaker/Storage/Flags/JSON/flags.json";
        private const string SO_PATH_FLAGS = "Assets/RPGMaker/Storage/Flags/SO/flags.asset";
        private const string JSON_PATH_ITEM = "Assets/RPGMaker/Storage/Item/JSON/item.json";
        private const string SO_PATH_ITEM = "Assets/RPGMaker/Storage/Item/SO/item.asset";
        private const string JSON_PATH_SKILL = "Assets/RPGMaker/Storage/Initializations/JSON/skill.json";
        private const string SO_PATH_SKILL = "Assets/RPGMaker/Storage/Initializations/SO/skill.asset";
        private const string JSON_PATH_SKILLCUSTOM = "Assets/RPGMaker/Storage/Initializations/JSON/skillCustom.json";
        private const string SO_PATH_SKILLCUSTOM = "Assets/RPGMaker/Storage/Initializations/SO/skillCustom.asset";
        private const string JSON_PATH_STATE = "Assets/RPGMaker/Storage/Initializations/JSON/state.json";
        private const string SO_PATH_STATE = "Assets/RPGMaker/Storage/Initializations/SO/state.asset";
        private const string JSON_PATH_SYSTEM = "Assets/RPGMaker/Storage/Initializations/JSON/system.json";
        private const string SO_PATH_SYSTEM = "Assets/RPGMaker/Storage/Initializations/SO/system.asset";
        private const string JSON_PATH_TITLE = "Assets/RPGMaker/Storage/Initializations/JSON/title.json";
        private const string SO_PATH_TITLE = "Assets/RPGMaker/Storage/Initializations/SO/title.asset";
        private const string JSON_PATH_TROOP = "Assets/RPGMaker/Storage/Character/JSON/troop.json";
        private const string SO_PATH_TROOP = "Assets/RPGMaker/Storage/Character/SO/troop.asset";
        private const string JSON_PATH_UI = "Assets/RPGMaker/Storage/Ui/JSON/ui.json";
        private const string SO_PATH_UI = "Assets/RPGMaker/Storage/Ui/SO/ui.asset";
        private const string JSON_PATH_VEHICLES = "Assets/RPGMaker/Storage/Character/JSON/vehicles.json";
        private const string SO_PATH_VEHICLES = "Assets/RPGMaker/Storage/Character/SO/vehicles.asset";
        private const string JSON_PATH_WEAPON = "Assets/RPGMaker/Storage/Initializations/JSON/weapon.json";
        private const string SO_PATH_WEAPON = "Assets/RPGMaker/Storage/Initializations/SO/weapon.asset";
        private const string JSON_PATH_WORDS = "Assets/RPGMaker/Storage/Word/JSON/words.json";
        private const string SO_PATH_WORDS = "Assets/RPGMaker/Storage/Word/SO/words.asset";

        private const string JSON_PATH_MAPBASE = "Assets/RPGMaker/Storage/Map/JSON/mapbase.json";
        private const string SO_PATH_MAPBASE = "Assets/RPGMaker/Storage/Map/SO/mapbase.asset";

        private const string JSON_PATH_EVENT = "Assets/RPGMaker/Storage/Event/JSON/Event";
        private const string SO_PATH_EVENT = "Assets/RPGMaker/Storage/Event/SO/Event";
        private const string JSON_PATH_EVENTMAP = "Assets/RPGMaker/Storage/Event/JSON/eventMap.json";
        private const string SO_PATH_EVENTMAP = "Assets/RPGMaker/Storage/Event/SO/eventMap.asset";
        private const string JSON_PATH_EVENTCOMMON = "Assets/RPGMaker/Storage/Event/JSON/eventCommon.json";
        private const string SO_PATH_EVENTCOMMON = "Assets/RPGMaker/Storage/Event/SO/eventCommon.asset";
        private const string JSON_PATH_EVENTBATTLE = "Assets/RPGMaker/Storage/Event/JSON/eventBattle.json";
        private const string SO_PATH_EVENTBATTLE = "Assets/RPGMaker/Storage/Event/SO/eventBattle.asset";

        private const string JSON_PATH_MAP = "Assets/RPGMaker/Storage/Map/JSON/Map";
        private const string SO_PATH_MAP = "Assets/RPGMaker/Storage/Map/SO/Map";
        private const string SO_PATH_MAP_FOLDER = "Assets/RPGMaker/Storage/Map/SO";
        private const string ASSET_PATH_TILE = "Assets/RPGMaker/Storage/Map/TileAssets";
        private const string SO_PATH_TILE = "Assets/RPGMaker/Storage/Map/SO/TileAssets.asset";
        private const string ASSET_PATH_TILE_TABLE = "Assets/RPGMaker/Storage/Map/JSON/tileTable.json";
        private const string SO_PATH_TILE_TABLE = "Assets/RPGMaker/Storage/Map/SO/tileTable.asset";

#if USE_PARTIAL_LOOP
        private const string JSON_PATH_BGM_LOOP = "Assets/RPGMaker/Storage/Sounds/bgmLoopInfo.json";
        private const string SO_PATH_BGM_LOOP = "Assets/RPGMaker/Storage/Sounds/bgmLoopInfo.asset";
        private const string JSON_PATH_BGS_LOOP = "Assets/RPGMaker/Storage/Sounds/bgsLoopInfo.json";
        private const string SO_PATH_BGS_LOOP = "Assets/RPGMaker/Storage/Sounds/bgsLoopInfo.asset";
#endif

        //====================================================================================================
        // action
        //====================================================================================================
        private static List<System.Action> _firstActionList;
        private static List<System.Action> _secondActionList;

#if UNITY_EDITOR
        //====================================================================================================
        // JSON→SO変換処理
        //====================================================================================================
        public static void CreateSO() {
            _firstActionList = null;
            _secondActionList = null;
            _firstActionList = new List<System.Action>();
            _secondActionList = new List<System.Action>();

            // アニメーション
#if !UNITY_WEBGL
            var jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(JSON_PATH_ANIMATION);
#else
            var jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonStringSync(JSON_PATH_ANIMATION);
#endif
            var anim = JsonHelper.FromJsonArray<AnimationDataModel>(jsonString);
            var animSo = ScriptableObject.CreateInstance<AnimationSO>();
            animSo.dataModels = anim;
            CreateAsset(animSo, SO_PATH_ANIMATION);

            // アーマー
#if !UNITY_WEBGL
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(JSON_PATH_ARMOR);
#else
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonStringSync(JSON_PATH_ARMOR);
#endif
            var armor = JsonHelper.FromJsonArray<ArmorDataModel>(jsonString);
            var armorSo = ScriptableObject.CreateInstance<ArmorSO>();
            armorSo.dataModels = armor;
            CreateAsset(armorSo, SO_PATH_ARMOR);

            // 素材管理
            // 管理用ファイル
#if !UNITY_WEBGL
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(JSON_PATH_ASSETMANAGE_DATA);
#else
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonStringSync(JSON_PATH_ASSETMANAGE_DATA);
#endif
            var assetData = JsonHelper.FromJsonArray<AssetManageDataModel>(jsonString);
            var assetDataSo = ScriptableObject.CreateInstance<AssetManagesSO>();
            assetDataSo.dataModels = assetData;
            CreateAsset(assetDataSo, SO_PATH_ASSETMANAGE_DATA);

            // 個々のファイル
            // ディレクトリ内のファイル全取得
            var dataPath =
                Directory.GetFiles(JSON_PATH_ASSETMANAGE + "/Assets/", "*.json", SearchOption.AllDirectories);
            for (var i = 0; i < dataPath.Length; i++)
            {
                dataPath[i] = dataPath[i].Replace("\\", "/");

                // 取得したJSONデータを読み込む
#if !UNITY_WEBGL
                jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(dataPath[i]);
#else
                jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonStringSync(dataPath[i]);
#endif
                var asset = JsonHelper.FromJson<AssetManageDataModel>(jsonString);
                var assetSo = ScriptableObject.CreateInstance<AssetManageSO>();
                assetSo.dataModel = asset;
                CreateAsset(assetSo, SO_PATH_ASSETMANAGE + "/" + asset.id + ".asset");
            }

            // オートガイド
#if !UNITY_WEBGL
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(JSON_PATH_AUTOGUID);
#else
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonStringSync(JSON_PATH_AUTOGUID);
#endif
            var guide = JsonHelper.FromJsonArray<AutoGuideDataModel>(jsonString);
            var guideSo = ScriptableObject.CreateInstance<AutoGuideSO>();
            guideSo.dataModels = guide;
            CreateAsset(guideSo, SO_PATH_AUTOGUID);

            // アクター
#if !UNITY_WEBGL
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(JSON_PATH_CHARACTERACTOR);
#else
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonStringSync(JSON_PATH_CHARACTERACTOR);
#endif
            var actor = JsonHelper.FromJsonArray<CharacterActorDataModel>(jsonString);
            var actorSo = ScriptableObject.CreateInstance<CharacterActorSO>();
            actorSo.dataModels = actor;
            CreateAsset(actorSo, SO_PATH_CHARACTERACTOR);

            // クラス
#if !UNITY_WEBGL
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(JSON_PATH_CLASS);
#else
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonStringSync(JSON_PATH_CLASS);
#endif
            var classdata = JsonHelper.FromJsonArray<ClassDataModel>(jsonString);
            var classSo = ScriptableObject.CreateInstance<ClassSO>();
            classSo.dataModels = classdata;
            CreateAsset(classSo, SO_PATH_CLASS);

            // エンカウント
#if !UNITY_WEBGL
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(JSON_PATH_ENCOUNTER);
#else
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonStringSync(JSON_PATH_ENCOUNTER);
#endif
            var encount = JsonHelper.FromJsonArray<EncounterDataModel>(jsonString);
            var encountSo = ScriptableObject.CreateInstance<EncounterSO>();
            encountSo.dataModels = encount;
            CreateAsset(encountSo, SO_PATH_ENCOUNTER);

            // エネミー
#if !UNITY_WEBGL
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(JSON_PATH_ENEMY);
#else
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonStringSync(JSON_PATH_ENEMY);
#endif
            var enemy = JsonHelper.FromJsonArray<EnemyDataModel>(jsonString);
            var enemySo = ScriptableObject.CreateInstance<EnemySO>();
            enemySo.dataModels = enemy;
            CreateAsset(enemySo, SO_PATH_ENEMY);

            // フラグ
#if !UNITY_WEBGL
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(JSON_PATH_FLAGS);
#else
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonStringSync(JSON_PATH_FLAGS);
#endif
            var flags = JsonHelper.FromJson<FlagDataModel>(jsonString);
            var flagsSo = ScriptableObject.CreateInstance<FlagsSO>();
            flagsSo.dataModel = flags;
            CreateAsset(flagsSo, SO_PATH_FLAGS);

            // アイテム
#if !UNITY_WEBGL
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(JSON_PATH_ITEM);
#else
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonStringSync(JSON_PATH_ITEM);
#endif
            var item = JsonHelper.FromJsonArray<ItemDataModel>(jsonString);
            var itemSo = ScriptableObject.CreateInstance<ItemSO>();
            itemSo.dataModels = item;
            CreateAsset(itemSo, SO_PATH_ITEM);

            // スキルカスタム
#if !UNITY_WEBGL
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(JSON_PATH_SKILLCUSTOM);
#else
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonStringSync(JSON_PATH_SKILLCUSTOM);
#endif
            var skillCustom = JsonHelper.FromJsonArray<SkillCustomDataModel>(jsonString);
            var skillCustomSo = ScriptableObject.CreateInstance<SkillCustomSO>();
            skillCustomSo.dataModels = skillCustom;
            CreateAsset(skillCustomSo, SO_PATH_SKILLCUSTOM);

            // スキルコモン
#if !UNITY_WEBGL
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(JSON_PATH_SKILL);
#else
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonStringSync(JSON_PATH_SKILL);
#endif
            var skill = JsonHelper.FromJsonArray<SkillCommonDataModel>(jsonString);
            var skillSo = ScriptableObject.CreateInstance<SkillCommonSO>();
            skillSo.dataModels = skill;
            CreateAsset(skillSo, SO_PATH_SKILL);

            // ステート
#if !UNITY_WEBGL
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(JSON_PATH_STATE);
#else
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonStringSync(JSON_PATH_STATE);
#endif
            var state = JsonHelper.FromJsonArray<StateDataModel>(jsonString);
            var stateSo = ScriptableObject.CreateInstance<StateSO>();
            stateSo.dataModels = state;
            CreateAsset(stateSo, SO_PATH_STATE);

            // システム
#if !UNITY_WEBGL
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(JSON_PATH_SYSTEM);
#else
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonStringSync(JSON_PATH_SYSTEM);
#endif
            var system = JsonHelper.FromJson<SystemSettingDataModel>(jsonString);
            var systemSo = ScriptableObject.CreateInstance<SystemSO>();
            systemSo.dataModels = system;
            CreateAsset(systemSo, SO_PATH_SYSTEM);

            // タイトル
#if !UNITY_WEBGL
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(JSON_PATH_TITLE);
#else
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonStringSync(JSON_PATH_TITLE);
#endif
            var title = JsonHelper.FromJson<RuntimeTitleDataModel>(jsonString);
            var titleSo = ScriptableObject.CreateInstance<TitleSO>();
            titleSo.dataModel = title;
            CreateAsset(titleSo, SO_PATH_TITLE);

            // グループ
#if !UNITY_WEBGL
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(JSON_PATH_TROOP);
#else
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonStringSync(JSON_PATH_TROOP);
#endif
            var troop = JsonHelper.FromJsonArray<TroopDataModel>(jsonString);
            var troopSo = ScriptableObject.CreateInstance<TroopSO>();
            troopSo.dataModels = troop;
            CreateAsset(troopSo, SO_PATH_TROOP);

            // UI
#if !UNITY_WEBGL
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(JSON_PATH_UI);
#else
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonStringSync(JSON_PATH_UI);
#endif
            var ui = JsonHelper.FromJson<UiSettingDataModel>(jsonString);
            var uiSo = ScriptableObject.CreateInstance<UiSettingSO>();
            uiSo.dataModel = ui;
            CreateAsset(uiSo, SO_PATH_UI);

            // 乗り物
#if !UNITY_WEBGL
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(JSON_PATH_VEHICLES);
#else
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonStringSync(JSON_PATH_VEHICLES);
#endif
            var vehicle = JsonHelper.FromJsonArray<VehiclesDataModel>(jsonString);
            var vehicleSo = ScriptableObject.CreateInstance<VehicleSO>();
            vehicleSo.dataModels = vehicle;
            CreateAsset(vehicleSo, SO_PATH_VEHICLES);

            // 武器
#if !UNITY_WEBGL
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(JSON_PATH_WEAPON);
#else
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonStringSync(JSON_PATH_WEAPON);
#endif
            var weapon = JsonHelper.FromJsonArray<WeaponDataModel>(jsonString);
            var weaponSo = ScriptableObject.CreateInstance<WeaponSO>();
            weaponSo.dataModels = weapon;
            CreateAsset(weaponSo, SO_PATH_WEAPON);

            // 文章
#if !UNITY_WEBGL
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(JSON_PATH_WORDS);
#else
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonStringSync(JSON_PATH_WORDS);
#endif
            var words = JsonHelper.FromJson<WordDefinitionDataModel>(jsonString);
            var wordsSo = ScriptableObject.CreateInstance<WordSO>();
            wordsSo.dataModel = words;
            CreateAsset(wordsSo, SO_PATH_WORDS);

            // イベント系
            //--------------------------------------------------------------------------------------
            // マップ
#if !UNITY_WEBGL
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(JSON_PATH_EVENTMAP);
#else
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonStringSync(JSON_PATH_EVENTMAP);
#endif
            var eventMap = JsonHelper.FromJsonArray<EventMapDataModel>(jsonString);
            var eventMapSo = ScriptableObject.CreateInstance<EventMapSO>();
            eventMapSo.dataModels = eventMap;
            CreateAsset(eventMapSo, SO_PATH_EVENTMAP);

            // コモン
#if !UNITY_WEBGL
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(JSON_PATH_EVENTCOMMON);
#else
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonStringSync(JSON_PATH_EVENTCOMMON);
#endif
            var eventCommon = JsonHelper.FromJsonArray<EventCommonDataModel>(jsonString);
            var eventCommonSo = ScriptableObject.CreateInstance<EventCommonSO>();
            eventCommonSo.dataModels = eventCommon;
            CreateAsset(eventCommonSo, SO_PATH_EVENTCOMMON);

            // バトル
#if !UNITY_WEBGL
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(JSON_PATH_EVENTBATTLE);
#else
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonStringSync(JSON_PATH_EVENTBATTLE);
#endif
            var eventBattle = JsonHelper.FromJsonArray<EventBattleDataModel>(jsonString);
            var eventBattleSo = ScriptableObject.CreateInstance<EventBattleSO>();
            eventBattleSo.dataModels = eventBattle;
            CreateAsset(eventBattleSo, SO_PATH_EVENTBATTLE);

            // イベント
            // ディレクトリ内のファイル全取得
            dataPath = Directory.GetFiles(JSON_PATH_EVENT, "*.json", SearchOption.TopDirectoryOnly);
            for (var i = 0; i < dataPath.Length; i++)
            {
                dataPath[i] = dataPath[i].Replace("\\", "/");

#if !UNITY_WEBGL
                jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(dataPath[i]);
#else
                jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonStringSync(dataPath[i]);
#endif
                var eventData = JsonHelper.FromJson<EventDataModel>(jsonString);
                var eventDataSo = ScriptableObject.CreateInstance<EventSO>();
                eventDataSo.dataModel = eventData;
                if (eventData.id != "")
                {
                    CreateAsset(eventDataSo, SO_PATH_EVENT + "/" + eventData.id + "-" + eventData.page + ".asset");
#if UNITY_EDITOR
                    AddressableManager.Path.SetAddressToAsset(
                        SO_PATH_EVENT + "/" + eventData.id + "-" + eventData.page + ".asset");
#endif
                }
            }

            // マップ系
            //--------------------------------------------------------------------------------------
            // マップ
            // ディレクトリ内のファイル全取得
            var mapBaseDataSo = ScriptableObject.CreateInstance<MapBaseSO>();
            mapBaseDataSo.dataModel = new List<MapBaseDataModel>();

            //SerialNumberがずれないように、同一の読み込み方法を用いる
#if !UNITY_WEBGL
            List<MapDataModel> mapDataModels = new MapRepository().LoadMapDataModels();
#else
            List<MapDataModel> mapDataModels = new MapRepository().LoadMapDataModelsSync();
#endif
            for (int i = 0; i < mapDataModels.Count; i++)
            {
                var mapDataSo = ScriptableObject.CreateInstance<MapSO>();
                mapDataSo.dataModel = mapDataModels[i];
                if (mapDataModels[i].id != "")
                {
                    CreateAsset(mapDataSo, SO_PATH_MAP + "/" + mapDataModels[i].id + ".asset");
#if UNITY_EDITOR
                    AddressableManager.Path.SetAddressToAsset(SO_PATH_MAP + "/" + mapDataModels[i].id + ".asset");
#endif
                }

                MapBaseDataModel work = new MapBaseDataModel(mapDataModels[i].id, mapDataModels[i].name, mapDataModels[i].SerialNumber);
                mapBaseDataSo.dataModel.Add(work);
            }

            CreateAsset(mapBaseDataSo, SO_PATH_MAPBASE);
#if UNITY_EDITOR
            AddressableManager.Path.SetAddressToAsset(SO_PATH_MAPBASE);
#endif

            // タイル(アセットを直接探してSOに変換)
#if !UNITY_WEBGL
            var tileDataModels = Directory.GetFiles(ASSET_PATH_TILE)
                .Select(assetPath => UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<TileDataModel>(assetPath))
                .Where(tileAsset => tileAsset != null)
                .ToList();
#else
            var tileDataModels = new List<TileDataModel>();
            foreach (var assetPath in Directory.GetFiles(ASSET_PATH_TILE)){
                var tileAsset = AssetDatabase.LoadAssetAtPath<TileDataModel>(assetPath);
                if (tileAsset == null) continue;
                tileDataModels.Add(tileAsset);
            }
#endif
            var tileSo = ScriptableObject.CreateInstance<TileSO>();
            tileSo.dataModels = tileDataModels;
            CreateAsset(tileSo, SO_PATH_TILE);

            // タイルテーブル
#if !UNITY_WEBGL
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(ASSET_PATH_TILE_TABLE);
#else
            jsonString = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonStringSync(ASSET_PATH_TILE_TABLE);
#endif
            var tileTable = JsonHelper.FromJsonArray<TileDataModelInfo>(jsonString);
            var tileTableSo = ScriptableObject.CreateInstance<TileTableSO>();
            tileTableSo.dataModels = tileTable;
            CreateAsset(tileTableSo, SO_PATH_TILE_TABLE);

#if USE_PARTIAL_LOOP
            //　BGM/BGSループ情報
            for (int i = 0; i < 2; i++)
            {
                var jsonFilename = (i == 0) ? JSON_PATH_BGM_LOOP : JSON_PATH_BGS_LOOP;
                var soFilename = (i == 0) ? SO_PATH_BGM_LOOP : SO_PATH_BGS_LOOP;
#if !UNITY_WEBGL
                var jsonStr = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(jsonFilename);
#else
                var jsonStr = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonStringSync(jsonFilename);
#endif
                var loopInfos = JsonHelper.FromJsonArray<LoopInfoModel>(jsonStr);
                var loopInfoSo = ScriptableObject.CreateInstance<LoopInfoSO>();
                loopInfoSo.dataModels = loopInfos;
                CreateAsset(loopInfoSo, soFilename);
            }
#endif

#if UNITY_EDITOR
            // フォルダ作成
            AssetDatabase.StartAssetEditing();
            for (int i = 0; i < _firstActionList.Count; i++)
                _firstActionList[i].Invoke();
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
            
            // SO作成
            AssetDatabase.StartAssetEditing();
            for (int i = 0; i < _secondActionList.Count; i++)
                _secondActionList[i].Invoke();
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();

            _firstActionList.Clear();
            _secondActionList.Clear();
#endif
        }
#endif

        //====================================================================================================
        // 対応したクラス名を返す
        //====================================================================================================
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static object GetClass<T>(string path) {
            switch (typeof(T).ToString().Split('.')[typeof(T).ToString().Split('.').Length - 1])
            {
                case "AnimationDataModel":
                    return AddressableManager.Load.LoadAssetSync<AnimationSO>(path)?.dataModels;
                case "ArmorDataModel":
                    return AddressableManager.Load.LoadAssetSync<ArmorSO>(path)?.dataModels;
                case "AssetManageDataModel":
                    return AddressableManager.Load.LoadAssetSync<AssetManageSO>(path)?.dataModel;
                case "AutoGuideDataModel":
                    return AddressableManager.Load.LoadAssetSync<AutoGuideSO>(path)?.dataModels;
                case "CharacterActorDataModel":
                    return AddressableManager.Load.LoadAssetSync<CharacterActorSO>(path)?.dataModels;
                case "ClassDataModel":
                    return AddressableManager.Load.LoadAssetSync<ClassSO>(path)?.dataModels;
                case "EncounterDataModel":
                    return AddressableManager.Load.LoadAssetSync<EncounterSO>(path)?.dataModels;
                case "EnemyDataModel":
                    return AddressableManager.Load.LoadAssetSync<EnemySO>(path)?.dataModels;
                case "FlagDataModel":
                    return AddressableManager.Load.LoadAssetSync<FlagsSO>(path)?.dataModel;
                case "ItemDataModel":
                    return AddressableManager.Load.LoadAssetSync<ItemSO>(path)?.dataModels;
                case "SkillCustomDataModel":
                    return AddressableManager.Load.LoadAssetSync<SkillCustomSO>(path)?.dataModels;
                case "SkillCommonDataModel":
                    return AddressableManager.Load.LoadAssetSync<SkillCommonSO>(path)?.dataModels;
                case "StateDataModel":
                    return AddressableManager.Load.LoadAssetSync<StateSO>(path)?.dataModels;
                case "SystemSettingDataModel":
                    return AddressableManager.Load.LoadAssetSync<SystemSO>(path)?.dataModels;
                case "RuntimeTitleDataModel":
                    return AddressableManager.Load.LoadAssetSync<TitleSO>(path)?.dataModel;
                case "TroopDataModel":
                    return AddressableManager.Load.LoadAssetSync<TroopSO>(path)?.dataModels;
                case "UiSettingDataModel":
                    return AddressableManager.Load.LoadAssetSync<UiSettingSO>(path)?.dataModel;
                case "VehiclesDataModel":
                    return AddressableManager.Load.LoadAssetSync<VehicleSO>(path)?.dataModels;
                case "WeaponDataModel":
                    return AddressableManager.Load.LoadAssetSync<WeaponSO>(path)?.dataModels;
                case "WordDefinitionDataModel":
                    return AddressableManager.Load.LoadAssetSync<WordSO>(path)?.dataModel;

                // イベント系
                case "EventDataModel":
                    return AddressableManager.Load.LoadAssetSync<EventSO>(path)?.dataModel;
                case "EventMapDataModel":
                    return AddressableManager.Load.LoadAssetSync<EventMapSO>(path)?.dataModels;
                case "EventCommonDataModel":
                    return AddressableManager.Load.LoadAssetSync<EventCommonSO>(path)?.dataModels;
                case "EventBattleDataModel":
                    return AddressableManager.Load.LoadAssetSync<EventBattleSO>(path)?.dataModels;

                // マップ系
                case "MapBaseDataModel":
                    return AddressableManager.Load.LoadAssetSync<MapBaseSO>(path)?.dataModel;
                case "MapDataModel":
                    return AddressableManager.Load.LoadAssetSync<MapSO>(path)?.dataModel;
                case "TileDataModel":
                    return AddressableManager.Load.LoadAssetSync<TileSO>(path)?.dataModels;
                case "TileDataModelInfo":
                    return AddressableManager.Load.LoadAssetSync<TileTableSO>(path)?.dataModels;

                default:
                    return "";
            }
        }
#else
        public static async Task<object> GetClass<T>(string path, bool mapAsset = false) {
            switch (typeof(T).ToString().Split('.')[typeof(T).ToString().Split('.').Length - 1])
            {
                case "AnimationDataModel":
                    return (await AddressableManager.Load.LoadAssetSync<AnimationSO>(path))?.dataModels;
                case "ArmorDataModel":
                    return (await AddressableManager.Load.LoadAssetSync<ArmorSO>(path))?.dataModels;
                case "AssetManageDataModel":
                    return (await AddressableManager.Load.LoadAssetSync<AssetManageSO>(path))?.dataModel;
                case "AutoGuideDataModel":
                    return (await AddressableManager.Load.LoadAssetSync<AutoGuideSO>(path))?.dataModels;
                case "CharacterActorDataModel":
                    return (await AddressableManager.Load.LoadAssetSync<CharacterActorSO>(path))?.dataModels;
                case "ClassDataModel":
                    return (await AddressableManager.Load.LoadAssetSync<ClassSO>(path))?.dataModels;
                case "EncounterDataModel":
                    return (await AddressableManager.Load.LoadAssetSync<EncounterSO>(path))?.dataModels;
                case "EnemyDataModel":
                    return (await AddressableManager.Load.LoadAssetSync<EnemySO>(path))?.dataModels;
                case "FlagDataModel":
                    return (await AddressableManager.Load.LoadAssetSync<FlagsSO>(path))?.dataModel;
                case "ItemDataModel":
                    return (await AddressableManager.Load.LoadAssetSync<ItemSO>(path))?.dataModels;
                case "SkillCustomDataModel":
                    return (await AddressableManager.Load.LoadAssetSync<SkillCustomSO>(path))?.dataModels;
                case "SkillCommonDataModel":
                    return (await AddressableManager.Load.LoadAssetSync<SkillCommonSO>(path))?.dataModels;
                case "StateDataModel":
                    return (await AddressableManager.Load.LoadAssetSync<StateSO>(path))?.dataModels;
                case "SystemSettingDataModel":
                    return (await AddressableManager.Load.LoadAssetSync<SystemSO>(path))?.dataModels;
                case "RuntimeTitleDataModel":
                    return (await AddressableManager.Load.LoadAssetSync<TitleSO>(path))?.dataModel;
                case "TroopDataModel":
                    return (await AddressableManager.Load.LoadAssetSync<TroopSO>(path))?.dataModels;
                case "UiSettingDataModel":
                    return (await AddressableManager.Load.LoadAssetSync<UiSettingSO>(path))?.dataModel;
                case "VehiclesDataModel":
                    return (await AddressableManager.Load.LoadAssetSync<VehicleSO>(path))?.dataModels;
                case "WeaponDataModel":
                    return (await AddressableManager.Load.LoadAssetSync<WeaponSO>(path))?.dataModels;
                case "WordDefinitionDataModel":
                    return (await AddressableManager.Load.LoadAssetSync<WordSO>(path))?.dataModel;

                // イベント系
                case "EventDataModel":
                    return (await AddressableManager.Load.LoadAssetSync<EventSO>(path, mapAsset))?.dataModel;
                case "EventMapDataModel":
                    return (await AddressableManager.Load.LoadAssetSync<EventMapSO>(path))?.dataModels;
                case "EventCommonDataModel":
                    return (await AddressableManager.Load.LoadAssetSync<EventCommonSO>(path))?.dataModels;
                case "EventBattleDataModel":
                    return (await AddressableManager.Load.LoadAssetSync<EventBattleSO>(path))?.dataModels;

                // マップ系
                case "MapBaseDataModel":
                    return (await AddressableManager.Load.LoadAssetSync<MapBaseSO>(path))?.dataModel;
                case "MapDataModel":
                    return (await AddressableManager.Load.LoadAssetSync<MapSO>(path, mapAsset))?.dataModel;
                case "TileDataModel":
                    return (await AddressableManager.Load.LoadAssetSync<TileSO>(path))?.dataModels;
                case "TileDataModelInfo":
                    return (await AddressableManager.Load.LoadAssetSync<TileTableSO>(path))?.dataModels;

                default:
                    return "";
            }
        }
#endif
#if UNITY_EDITOR
        //====================================================================================================
        // SO出力
        //====================================================================================================
        private static void CreateAsset(Object so, string path) {
            // 拡張子を除いたパスを取得する
            var extension = Path.GetExtension(path);
            var folderPath = path;
            if (!string.IsNullOrEmpty(extension))
            {
                folderPath = path.Replace(extension, string.Empty);
                folderPath = folderPath.Replace("/" + folderPath.Split('/')[folderPath.Split('/').Length - 1],
                    string.Empty);
            }

            // 既にSOがあり変更がなければ処理しない
            if (File.Exists(path))
            {
                var currentSO = UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath(path, so.GetType());

                // 共通関数の呼び出し
                System.Type type = currentSO.GetType();
                var method = type.GetMethod("isEquals");
                object[] arg = new object[] { so };
                if (method != null)
                    if ((bool) method.Invoke(currentSO, arg))
                        return;
            }

            // フォルダがなければ作成
            if (Directory.Exists(folderPath) == false)
            {
                _firstActionList.Add(() =>
                {
                    Directory.CreateDirectory(folderPath);
                    UnityEditorWrapper.AssetDatabaseWrapper.Refresh();
                });
            }

            _secondActionList.Add(() =>
            {
                UnityEditorWrapper.AssetDatabaseWrapper.CreateAsset(so, path);
            });
        }
#endif
    }
}