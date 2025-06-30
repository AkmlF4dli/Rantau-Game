using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Animation;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Armor;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.AssetManage;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.CharacterActor;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Class;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Encounter;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Enemy;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Flag;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Item;
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
using RPGMaker.Codebase.CoreSystem.Knowledge.Enum;
using RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement.Repository;
using RPGMaker.Codebase.Runtime.Common;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement
{
    public class DatabaseManagementService
    {
        private readonly AnimationRepository      _animationRepository;
        private readonly ArmorRepository          _armorRepository;
        private readonly AssetManageRepository    _assetManageRepository;
        private readonly AutoGuideRepository      _autoGuideRepository;
        private readonly CharacterActorRepository _characterActorRepository;
        private readonly ClassRepository          _classRepository;
        private readonly EncounterRepository      _encounterRepository;
        private readonly EnemyRepository          _enemyRepository;
        private readonly FlagsRepository          _flagsRepository;
        private readonly ItemRepository           _itemRepository;
        private readonly SkillCommonRepository    _skillCommonRepository;
        private readonly SkillCustomRepository    _skillCustomRepository;
        private readonly StateRepository          _stateRepository;
        private readonly SystemRepository         _systemRepository;
        private readonly TitleRepository          _titleRepository;
        private readonly TroopRepository          _troopRepository;
        private readonly UiSettingRepository      _uiSettingRepository;
        private readonly VehicleRepository        _vehicleRepository;
        private readonly WeaponRepository         _weaponRepository;
        private readonly WordDefinitionRepository _wordDefinitionRepository;

        public DatabaseManagementService() {
            _animationRepository = new AnimationRepository();
            _armorRepository = new ArmorRepository();
            _assetManageRepository = new AssetManageRepository();
            _autoGuideRepository = new AutoGuideRepository();
            _classRepository = new ClassRepository();
            _characterActorRepository = new CharacterActorRepository();
            _encounterRepository = new EncounterRepository();
            _enemyRepository = new EnemyRepository();
            _flagsRepository = new FlagsRepository();
            _itemRepository = new ItemRepository();
            _skillCustomRepository = new SkillCustomRepository();
            _skillCommonRepository = new SkillCommonRepository();
            _stateRepository = new StateRepository();
            _systemRepository = new SystemRepository();
            _titleRepository = new TitleRepository();
            _troopRepository = new TroopRepository();
            _uiSettingRepository = new UiSettingRepository();
            _vehicleRepository = new VehicleRepository();
            _weaponRepository = new WeaponRepository();
            _wordDefinitionRepository = new WordDefinitionRepository();
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<AnimationDataModel> LoadAnimation() {
            return _animationRepository.Load();
        }
#else
        public async Task<List<AnimationDataModel>> LoadAnimation() {
            return await _animationRepository.Load();
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SaveAnimation(List<AnimationDataModel> animationDataModels) {
            _animationRepository.Save(animationDataModels);
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<ArmorDataModel> LoadArmor() {
            return _armorRepository.Load();
        }
#else
        public async Task<List<ArmorDataModel>> LoadArmor() {
            return await _armorRepository.Load();
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SaveArmor(List<ArmorDataModel> armorDataModels) {
            _armorRepository.Save(armorDataModels);
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<AssetManageDataModel> LoadAssetManage() {
            return _assetManageRepository.Load();
        }
#else
        public async Task<List<AssetManageDataModel>> LoadAssetManage() {
            return await _assetManageRepository.Load();
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<AssetManageDataModel> LoadWalkingCharacterAssets() {
            return _assetManageRepository.Load()
                .FindAll(item => item.assetTypeId == (int) AssetCategoryEnum.MOVE_CHARACTER);
        }
#else
        public async Task<List<AssetManageDataModel>> LoadWalkingCharacterAssets() {
            return (await _assetManageRepository.Load())
                .FindAll(item => item.assetTypeId == (int) AssetCategoryEnum.MOVE_CHARACTER);
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<AssetManageDataModel> LoadObjectAssets() {
            return _assetManageRepository.Load().FindAll(item => item.assetTypeId == (int) AssetCategoryEnum.OBJECT);
        }
#else
        public async Task<List<AssetManageDataModel>> LoadObjectAssets() {
            return (await _assetManageRepository.Load()).FindAll(item => item.assetTypeId == (int) AssetCategoryEnum.OBJECT);
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<AssetManageDataModel> LoadPopupIconAssets() {
            return _assetManageRepository.Load().FindAll(item => item.assetTypeId == (int) AssetCategoryEnum.POPUP);
        }
#else
        public async Task<List<AssetManageDataModel>> LoadPopupIconAssets() {
            return (await _assetManageRepository.Load()).FindAll(item => item.assetTypeId == (int) AssetCategoryEnum.POPUP);
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<AssetManageDataModel> LoadActorAssets() {
            return _assetManageRepository.Load()
                .FindAll(item => item.assetTypeId == (int) AssetCategoryEnum.SV_BATTLE_CHARACTER);
        }
#else
        public async Task<List<AssetManageDataModel>> LoadActorAssets() {
            return (await _assetManageRepository.Load())
                .FindAll(item => item.assetTypeId == (int) AssetCategoryEnum.SV_BATTLE_CHARACTER);
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<AssetManageDataModel> LoadWeaponAssets() {
            return _assetManageRepository.Load().FindAll(item => item.assetTypeId == (int) AssetCategoryEnum.SV_WEAPON);
        }
#else
        public async Task<List<AssetManageDataModel>> LoadWeaponAssets() {
            return (await _assetManageRepository.Load()).FindAll(item => item.assetTypeId == (int) AssetCategoryEnum.SV_WEAPON);
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<AssetManageDataModel> LoadStateOverlapAssets() {
            return _assetManageRepository.Load()
                .FindAll(item => item.assetTypeId == (int) AssetCategoryEnum.SUPERPOSITION);
        }
#else
        public async Task<List<AssetManageDataModel>> LoadStateOverlapAssets() {
            return (await _assetManageRepository.Load())
                .FindAll(item => item.assetTypeId == (int) AssetCategoryEnum.SUPERPOSITION);
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<AssetManageDataModel> LoadBattleEffectAssets() {
            return _assetManageRepository.Load()
                .FindAll(item => item.assetTypeId == (int) AssetCategoryEnum.BATTLE_EFFECT);
        }
#else
        public async Task<List<AssetManageDataModel>> LoadBattleEffectAssets() {
            return (await _assetManageRepository.Load())
                .FindAll(item => item.assetTypeId == (int) AssetCategoryEnum.BATTLE_EFFECT);
        }
#endif

#if UNITY_EDITOR
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SaveAssetManage(AssetManageDataModel dataModel, bool del = false) {
#else
        public async Task SaveAssetManage(AssetManageDataModel dataModel, bool del = false) {
#endif
            //該当のIDが既に存在する場合は置き換え
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var assetManagerDataModels = LoadAssetManage();
#else
            var assetManagerDataModels = await LoadAssetManage();
#endif
            if (!del)
            {
                var saveState = false;
                for (var i = 0; i < assetManagerDataModels.Count; i++)
                    if (assetManagerDataModels[i].id == dataModel.id)
                    {
                        assetManagerDataModels[i] = dataModel;
                        saveState = true;
                        break;
                    }

                //該当のIDが存在しない場合追加する
                if (!saveState) assetManagerDataModels.Add(dataModel);
                _assetManageRepository.Save(dataModel);
            }
            else
            {
                assetManagerDataModels.Remove(dataModel);
                //ファイルの削除の実行
                if (File.Exists("Assets/RPGMaker/Storage/AssetManage/JSON/Assets/" + dataModel.id + ".json"))
                    File.Delete("Assets/RPGMaker/Storage/AssetManage/JSON/Assets/" + dataModel.id + ".json");
            }
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<AutoGuideDataModel> LoadAutoGuide() {
            return _autoGuideRepository.Load();
        }
#else
        public async Task<List<AutoGuideDataModel>> LoadAutoGuide() {
            return await _autoGuideRepository.Load();
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SaveAutoGuid(List<AutoGuideDataModel> autoGuideDataModels) {
            _autoGuideRepository.Save(autoGuideDataModels);
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<ClassDataModel> LoadClassCommon() {
            return _classRepository.Load();
        }
#else
        public async Task<List<ClassDataModel>> LoadClassCommon() {
            return await _classRepository.Load();
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public ClassDataModel LoadClassCommonByClassId(string classId) {
#else
        public async Task<ClassDataModel> LoadClassCommonByClassId(string classId) {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var classes = _classRepository.Load();
#else
            var classes = await _classRepository.Load();
#endif
            ClassDataModel data = null;
            for (int i = 0; i < classes.Count; i++)
                if (classes[i].id == classId)
                {
                    data = classes[i];
                    break;
                }
            return data;
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SaveClassCommon(List<ClassDataModel> classDataModels) {
            _classRepository.Save(classDataModels);
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<EncounterDataModel> LoadEncounter() {
            return _encounterRepository.Load();
        }
#else
        public async Task<List<EncounterDataModel>> LoadEncounter() {
            return await _encounterRepository.Load();
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SaveEncounter(List<EncounterDataModel> encounterDataModels) {
            _encounterRepository.Save(encounterDataModels);
        }
#endif

        public void ResetEncounter() {
            _encounterRepository.Reset();
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public FlagDataModel LoadFlags() {
            return _flagsRepository.Load();
        }
#else
        public async Task<FlagDataModel> LoadFlags() {
            return await _flagsRepository.Load();
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SaveFlags(FlagDataModel flagDataModel) {
            _flagsRepository.Save(flagDataModel);
        }
#endif

#if UNITY_EDITOR
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SaveVariable(FlagDataModel.Variable variable) {
            _flagsRepository.SaveVariable(variable);
        }
#else
        public async Task SaveVariable(FlagDataModel.Variable variable) {
            await _flagsRepository.SaveVariable(variable);
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SaveSwitch(FlagDataModel.Switch sw) {
            _flagsRepository.SaveSwitch(sw);
        }
#else
        public async Task SaveSwitch(FlagDataModel.Switch sw) {
            await _flagsRepository.SaveSwitch(sw);
        }
#endif
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<ItemDataModel> LoadItem() {
            return _itemRepository.Load();
        }
#else
        public async Task<List<ItemDataModel>> LoadItem() {
            return await _itemRepository.Load();
        }
#endif

#if UNITY_EDITOR
        public void SaveItem(List<ItemDataModel> itemDataModels) {
            _itemRepository.Save(itemDataModels);
        }
#endif

        public void DeleteItem(int itemId) {
            _itemRepository.DeleteItem(itemId);
        }

        public void ChangeMaximum(int maximumNum) {
            _itemRepository.ChangeMaximum(maximumNum);
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<CharacterActorDataModel> LoadCharacterActor() {
            return _characterActorRepository.Load();
        }
#else
        public async Task<List<CharacterActorDataModel>> LoadCharacterActor() {
            return await _characterActorRepository.Load();
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SaveCharacterActor(List<CharacterActorDataModel> characterActorDataModels) {
            _characterActorRepository.Save(characterActorDataModels);
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<ClassDataModel> LoadCharacterActorClass() {
            return _classRepository.Load();
        }
#else
        public async Task<List<ClassDataModel>> LoadCharacterActorClass() {
            return await _classRepository.Load();
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SaveCharacterActorClass(List<ClassDataModel> classDataModels) {
            _classRepository.Save(classDataModels);
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<VehiclesDataModel> LoadCharacterVehicles() {
            return _vehicleRepository.Load();
        }
#else
        public async Task<List<VehiclesDataModel>> LoadCharacterVehicles() {
            return await _vehicleRepository.Load();
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SaveCharacterVehicles(List<VehiclesDataModel> vehiclesDataModels) {
            _vehicleRepository.Save(vehiclesDataModels);
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<EnemyDataModel> LoadEnemy() {
            return _enemyRepository.Load();
        }
#else
        public async Task<List<EnemyDataModel>> LoadEnemy() {
            return await _enemyRepository.Load();
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SaveEnemy(List<EnemyDataModel> enemyDataModels) {
            _enemyRepository.Save(enemyDataModels);
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<SkillCommonDataModel> LoadSkillCommon() {
            return _skillCommonRepository.Load();
        }
#else
        public async Task<List<SkillCommonDataModel>> LoadSkillCommon() {
            return await _skillCommonRepository.Load();
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SaveSkillCommon(List<SkillCommonDataModel> skillCommonDataModels) {
            _skillCommonRepository.Save(skillCommonDataModels);
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<SkillCustomDataModel> LoadSkillCustom() {
            return _skillCustomRepository.Load();
        }
#else
        public async Task<List<SkillCustomDataModel>> LoadSkillCustom() {
            return await _skillCustomRepository.Load();
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SaveSkillCustom(List<SkillCustomDataModel> skillCustomDataModels) {
            _skillCustomRepository.Save(skillCustomDataModels);
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<StateDataModel> LoadStateEdit() {
            return _stateRepository.Load();
        }
#else
        public async Task<List<StateDataModel>> LoadStateEdit() {
            return await _stateRepository.Load();
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SaveStateEdit(List<StateDataModel> stateDataModels) {
            _stateRepository.Save(stateDataModels);
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public SystemSettingDataModel LoadSystem() {
            return _systemRepository.Load();
        }
#else
        public async Task<SystemSettingDataModel> LoadSystem() {
            return await _systemRepository.Load();
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SaveSystem(SystemSettingDataModel systemSettingDataModel) {
            _systemRepository.Save(systemSettingDataModel);
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public RuntimeTitleDataModel LoadTitle() {
            return _titleRepository.Load();
        }
#else
        public async Task<RuntimeTitleDataModel> LoadTitle() {
            return await _titleRepository.Load();
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SaveTitle(RuntimeTitleDataModel runtimeTitleDataModel) {
            _titleRepository.Save(runtimeTitleDataModel);
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<TroopDataModel> LoadTroop() {
            return _troopRepository.Load();
        }
#else
        public async Task<List<TroopDataModel>> LoadTroop() {
            return await _troopRepository.Load();
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SaveTroop(List<TroopDataModel> troopDataModels) {
            _troopRepository.Save(troopDataModels);
        }
#endif

        public void ResetTroop() {
            _troopRepository.Reset();
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public UiSettingDataModel LoadUiSettingDataModel() {
            return _uiSettingRepository.Load();
        }
#else
        public async Task<UiSettingDataModel> LoadUiSettingDataModel() {
            return await _uiSettingRepository.Load();
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SaveUiSettingDataModel(UiSettingDataModel uiSettingDataModel) {
            _uiSettingRepository.Save(uiSettingDataModel);
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<WeaponDataModel> LoadWeapon() {
            return _weaponRepository.Load();
        }
#else
        public async Task<List<WeaponDataModel>> LoadWeapon() {
            return await _weaponRepository.Load();
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SaveWeapon(List<WeaponDataModel> weaponDataModels) {
            _weaponRepository.Save(weaponDataModels);
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public WordDefinitionDataModel LoadWordDefinition() {
            return _wordDefinitionRepository.Load();
        }
#else
        public async Task<WordDefinitionDataModel> LoadWordDefinition() {
            return await _wordDefinitionRepository.Load();
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SaveWord(WordDefinitionDataModel wordDefinitionDataModel) {
            _wordDefinitionRepository.Save(wordDefinitionDataModel);
        }
#endif

        /// <summary>
        /// 敵キャラのリストから新しく敵グループを作成
        /// </summary>
        /// <param name="troopId"></param>
        /// <param name="viewType"></param>
        /// <param name="encounterDataModel"></param>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public TroopDataModel CreateEnemyToTroopDataModel(
#else
        public async Task<TroopDataModel> CreateEnemyToTroopDataModel(
#endif
            string troopId,
            int viewType,
            EncounterDataModel encounterDataModel,
            bool isRandom = true
        ) {
            TroopDataModel troop = new TroopDataModel
            {
                id = troopId,
                name = troopId,
                battleEventId = "",
                frontViewMembers = new List<TroopDataModel.FrontViewMember>(),
                sideViewMembers = new List<TroopDataModel.SideViewMember>(),
                backImage1 = encounterDataModel.backImage1 != "" ? encounterDataModel.backImage1 : "",
                backImage2 = encounterDataModel.backImage2 != "" ? encounterDataModel.backImage2 : ""
            };

            //troopの中身を新規に作成する
            //AUTOMATCH を敵グループから削除する(不要なデータが残る場合があったため)
            DataManager.Self().GetTroopDataModels().RemoveAll(value => value.id == "AUTOMATCH");

            //フロントビューかサイドビューかに応じて処理が変わる
            if (viewType == 0)
            {
                //フロントビュー
                //体数決定（1～8）
                //本来はデータベースで正しい値が設定されるべきだが、万が一範囲外だった場合は丸め込む
                int enemyCountWork = encounterDataModel.enemyMax;
                if (enemyCountWork < 1)
                {
                    enemyCountWork = 1;
                }
                else if (enemyCountWork > 8)
                {
                    enemyCountWork = 8;
                }

                int enemyCount = !isRandom ? enemyCountWork : UnityEngine.Random.Range(1, enemyCountWork + 1);

                //抽選用に、各敵のサイズを収集しておく
                List<string> enemies = new List<string>();
                List<int> enemiesCount = new List<int>();

                for (int i = 0; i < encounterDataModel.enemyList.Count; i++)
                {
                    //敵情報取得
                    EnemyDataModel enemyData = null;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    var enemy = LoadEnemy();
#else
                    var enemy = await LoadEnemy();
#endif
                    for (int i2 = 0; i2 < enemy.Count; i2++)
                        if (enemy[i2].id == encounterDataModel.enemyList[i].enemyId)
                        {
                            enemyData = enemy[i2];
                            break;
                        }
                    enemies.Add(enemyData.id);
                    enemiesCount.Add(encounterDataModel.enemyList[i].maxAppearances);
                }

                //配置場所をランダムにする
                List<int> positions = new List<int>();
                positions.Add(0);
                positions.Add(1);
                positions.Add(2);
                positions.Add(3);
                positions.Add(4);
                positions.Add(5);
                positions.Add(6);
                positions.Add(7);

                //表示する敵の数分回す
                for (int i = 0; i < enemyCount; i++)
                {
                    //抽選
                    int enemyIndex = !isRandom ? 0 : UnityEngine.Random.Range(0, enemies.Count);
                    int enemyPosition = !isRandom ? 0 : UnityEngine.Random.Range(0, positions.Count);
                    TroopDataModel.FrontViewMember enemyData =
                        new TroopDataModel.FrontViewMember(enemies[enemyIndex], positions[enemyPosition], 0, 0);
                    troop.frontViewMembers.Add(enemyData);

                    //抽選した配置場所を削除
                    positions.RemoveAt(enemyPosition);

                    //抽選した敵の同時出現数を減らす
                    enemiesCount[enemyIndex]--;

                    //抽選しきった場合は配列から除外
                    if (enemiesCount[enemyIndex] <= 0)
                    {
                        enemies.RemoveAt(enemyIndex);
                        enemiesCount.RemoveAt(enemyIndex);
                        //配列内に敵がいなくなったら処理終了
                        if (enemies.Count <= 0) break;
                    }
                }
            }
            else
            {
                //サイドビュー
                //体数決定（1～6）
                if (!isRandom)
                {
                    for (int i = 0; i < encounterDataModel.enemyList.Count; i++)
                    {
                        int position1 = -1;
                        int position2 = -1;
                        for (var t = 0; t < 2; t++)
                        {
                            for (var j = 0; j < 3; j++)
                            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                                if (!Used(t, j))
#else
                                if (!await Used(t, j))
#endif
                                {
                                    position1 = t;
                                    position2 = j;
                                    break;
                                }
                            }

                            if (position1 != -1 && position2 != -1)
                                break;
                        }

                        if (position1 == -1 && position2 == -1)
                            continue;


                        TroopDataModel.SideViewMember sideViewMember = new TroopDataModel.SideViewMember(
                            encounterDataModel.enemyList[i].enemyId, position1, position2, 0, 0);
                        troop.sideViewMembers.Add(sideViewMember);
                    }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    bool Used(int num1, int num2) {
#else
                    async Task<bool> Used(int num1, int num2) {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        var enemyData = _enemyRepository.Load();
#else
                        var enemyData = await _enemyRepository.Load();
#endif
                        for (var i = 0; i < troop.sideViewMembers.Count; i++)
                        {
                            //使われていた
                            if (troop.sideViewMembers[i].position1 == num1 &&
                                troop.sideViewMembers[i].position2 == num2)
                            {
                                return true;
                            }

                            //このEnemyが大型の場合、配置場所が上または下なら、中も利用していることとする
                            EnemyDataModel enemyDataWork = null;
                            for (int i2 = 0; i2 < enemyData.Count; i2++)
                                if (enemyData[i2].id == troop.sideViewMembers[i].enemyId)
                                {
                                    enemyDataWork = enemyData[i2];
                                    break;
                                }
                            if (enemyDataWork.images.autofitPattern == 1)
                            {
                                if (num2 == 1 && troop.sideViewMembers[i].position1 == num1)
                                {
                                    return true;
                                }
                            }

                            //このEnemyがボス型の場合、num1にもnum2にも置けない
                            if (enemyDataWork.images.autofitPattern == 2)
                            {
                                return true;
                            }
                        }

                        //使われていなかった
                        return false;
                    }

                    return troop;
                }

                //本来はデータベースで正しい値が設定されるべきだが、万が一範囲外だった場合は丸め込む
                int enemyCountWork = encounterDataModel.enemyMax;
                if (enemyCountWork < 1)
                {
                    enemyCountWork = 1;
                }
                else if (enemyCountWork > 6)
                {
                    enemyCountWork = 6;
                }

                int enemyCount = UnityEngine.Random.Range(1, enemyCountWork + 1);

                //体数に応じて抽選方法を変更する
                //基本的に、自動調節する敵しか抽選しない予定だが、現状のデータではほぼすべての敵が自動調節しない設定のため、
                //自動調節しない設定の敵は、全部小型扱いで抽選する

                //抽選用に、各敵のサイズを収集しておく
                List<string> smallEnemy = new List<string>();
                List<int> smallEnemyCount = new List<int>();
                List<string> largeEnemy = new List<string>();
                List<int> largeEnemyCount = new List<int>();
                List<string> bossEnemy = new List<string>();
                List<int> bossEnemyCount = new List<int>();

                for (int i = 0; i < encounterDataModel.enemyList.Count; i++)
                {
                    //敵情報取得
                    EnemyDataModel enemyData = null;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    var enemy = LoadEnemy();
#else
                    var enemy = await LoadEnemy();
#endif
                    for (int i2 = 0; i2 < enemy.Count; i2++)
                        if (enemy[i2].id == encounterDataModel.enemyList[i].enemyId)
                        {
                            enemyData = enemy[i2];
                            break;
                        }
                    //U406 大型判定
                    if (enemyData.images.autofitPattern == 1)
                    {
                        //大型
                        largeEnemy.Add(enemyData.id);
                        //同時出現する最大数の設定
                        largeEnemyCount.Add(encounterDataModel.enemyList[i].maxAppearances);
                    }
                    else //U406 ボス判定
                    if (enemyData.images.autofitPattern == 2)
                    {
                        //ボス型
                        bossEnemy.Add(enemyData.id);
                        //同時出現する最大数の設定
                        bossEnemyCount.Add(encounterDataModel.enemyList[i].maxAppearances);
                    }
                    else
                    {
                        //小型
                        smallEnemy.Add(enemyData.id);
                        //同時出現する最大数の設定
                        smallEnemyCount.Add(encounterDataModel.enemyList[i].maxAppearances);
                    }
                }

                //抽選結果が5以上で、小型が不在の場合は、この段階で出現数を減らしておく
                if (enemyCount >= 5 && smallEnemy.Count <= 0)
                {
                    enemyCount = 4;
                }

                //抽選結果が3以上で、小型も大型も不在の場合は、この段階で出現数を減らしておく
                if (enemyCount >= 3 && smallEnemy.Count <= 0 && bossEnemy.Count <= 0)
                {
                    enemyCount = 2;
                }

                if (enemyCount == 6)
                {
                    //小型から6体抽選する
                    List<int> positions = new List<int>();
                    positions.Add(0);
                    positions.Add(1);
                    positions.Add(2);
                    positions.Add(3);
                    positions.Add(4);
                    positions.Add(5);
                    for (int i = 0; i < enemyCount; i++)
                    {
                        //抽選
                        int enemyIndex = UnityEngine.Random.Range(0, smallEnemy.Count);
                        int enemyPosition = UnityEngine.Random.Range(0, positions.Count);
                        TroopDataModel.SideViewMember enemyData = new TroopDataModel.SideViewMember(
                            smallEnemy[enemyIndex], positions[enemyPosition] / 3, positions[enemyPosition] % 3, 0, 0);
                        troop.sideViewMembers.Add(enemyData);

                        //抽選した配置場所を削除
                        positions.RemoveAt(enemyPosition);

                        //抽選した敵の同時出現数を減らす
                        smallEnemyCount[enemyIndex]--;

                        //抽選しきった場合は配列から除外
                        if (smallEnemyCount[enemyIndex] <= 0)
                        {
                            smallEnemy.RemoveAt(enemyIndex);
                            smallEnemyCount.RemoveAt(enemyIndex);
                            //配列内に敵がいなくなったら処理終了
                            if (smallEnemy.Count <= 0) break;
                        }
                    }
                }
                else if (enemyCount == 5)
                {
                    //大型から最大で2、抽選する
                    //5の場合、大型が上下で並ぶ必要があるため、先に敵を抽選する
                    int largeEnemyAppearCount = 0;
                    int smallEnemyAppearCount = 0;
                    List<string> smallEnemyDatas = new List<string>();
                    List<string> largeEnemyDatas = new List<string>();
                    for (int i = 0; i < enemyCount; i++)
                    {
                        //U406 型で配置数が決まっているので、オーバーしない様に判定追加
                        //大型4
                        //大型1  小型3 配置オーバーチェック
                        if (largeEnemyAppearCount == 1 && smallEnemyAppearCount == 3)
                        {
                            break;
                        }
                        //大型2　小型2 配置オーバーチェック
                        if (largeEnemyAppearCount == 2 && smallEnemyAppearCount == 2)
                        {
                            break;
                        }

                        //抽選
                        int enemyIndex = UnityEngine.Random.Range(0, smallEnemy.Count + largeEnemy.Count);
                        if (enemyIndex < smallEnemy.Count)
                        {
                            //小型敵のデータを保持
                            smallEnemyDatas.Add(smallEnemy[enemyIndex]);
                            //小型敵の抽選数を増やす
                            smallEnemyAppearCount++;
                            //抽選した敵の同時出現数を減らす
                            smallEnemyCount[enemyIndex]--;

                            //抽選しきった場合は配列から除外
                            if (smallEnemyCount[enemyIndex] <= 0)
                            {
                                smallEnemy.RemoveAt(enemyIndex);
                                smallEnemyCount.RemoveAt(enemyIndex);
                            }
                        }
                        else
                        {
                            enemyIndex = enemyIndex - smallEnemy.Count;

                            //大型敵のデータを保持
                            largeEnemyDatas.Add(largeEnemy[enemyIndex]);
                            //大型敵の抽選数を増やす
                            largeEnemyAppearCount++;
                            //抽選した敵の同時出現数を減らす
                            largeEnemyCount[enemyIndex]--;

                            //抽選しきった場合は配列から除外
                            if (largeEnemyCount[enemyIndex] <= 0)
                            {
                                largeEnemy.RemoveAt(enemyIndex);
                                largeEnemyCount.RemoveAt(enemyIndex);
                            }

                            //大型の敵の抽選回数が2回になった場合は、大型の敵のデータを無くし、以降の抽選不可能とする
                            if (largeEnemyAppearCount == 2)
                            {
                                largeEnemy.Clear();
                                largeEnemyCount.Clear();
                            }
                        }

                        //配列内に敵がいなくなったら処理終了
                        if (smallEnemy.Count <= 0 && largeEnemy.Count <= 0) break;
                    }

                    //大型の敵の出現数が2の場合
                    if (largeEnemyDatas.Count == 2)
                    {
                        int largeEnemyPosition = UnityEngine.Random.Range(0, 2);
                        int smallEnemyPosition = (largeEnemyPosition + 1) % 2;

                        for (int i = 0; i < smallEnemyDatas.Count; i++)
                        {
                            TroopDataModel.SideViewMember enemyData =
                                new TroopDataModel.SideViewMember(smallEnemyDatas[i], smallEnemyPosition, i, 0, 0);
                            troop.sideViewMembers.Add(enemyData);
                        }

                        for (int i = 0; i < largeEnemyDatas.Count; i++)
                        {
                            TroopDataModel.SideViewMember enemyData =
                                new TroopDataModel.SideViewMember(largeEnemyDatas[i], largeEnemyPosition, (i * 2), 0,
                                    0);
                            troop.sideViewMembers.Add(enemyData);
                        }
                    }
                    //大型の敵の出現数が1の場合
                    else if (largeEnemyDatas.Count == 1)
                    {
                        //大型のポジションの方に、小型を1体配置する
                        int largeEnemyPosition = UnityEngine.Random.Range(0, 2);
                        int smallEnemyPosition = (largeEnemyPosition + 1) % 2;

                        int rand = UnityEngine.Random.Range(0, 2);

                        for (int i = 0; i < smallEnemyDatas.Count; i++)
                        {
                            if (i == 3)
                            {
                                TroopDataModel.SideViewMember enemyData =
                                    new TroopDataModel.SideViewMember(smallEnemyDatas[i], largeEnemyPosition,
                                        (rand * 2), 0, 0);
                                troop.sideViewMembers.Add(enemyData);
                            }
                            else
                            {
                                TroopDataModel.SideViewMember enemyData =
                                    new TroopDataModel.SideViewMember(smallEnemyDatas[i], smallEnemyPosition, i, 0, 0);
                                troop.sideViewMembers.Add(enemyData);
                            }
                        }

                        for (int i = 0; i < largeEnemyDatas.Count; i++)
                        {
                            TroopDataModel.SideViewMember enemyData =
                                new TroopDataModel.SideViewMember(largeEnemyDatas[i], largeEnemyPosition, (rand * 2), 0,
                                    0);
                            troop.sideViewMembers.Add(enemyData);
                        }
                    }
                    //大型が抽選した結果、居なかった場合
                    else
                    {
                        //小型から6体抽選する
                        List<int> positions = new List<int>();
                        positions.Add(0);
                        positions.Add(1);
                        positions.Add(2);
                        positions.Add(3);
                        positions.Add(4);
                        positions.Add(5);
                        for (int i = 0; i < smallEnemyDatas.Count; i++)
                        {
                            //抽選
                            int enemyPosition = UnityEngine.Random.Range(0, positions.Count);
                            TroopDataModel.SideViewMember enemyData =
                                new TroopDataModel.SideViewMember(smallEnemyDatas[i], positions[enemyPosition] / 3,
                                    positions[enemyPosition] % 3, 0, 0);
                            troop.sideViewMembers.Add(enemyData);

                            //抽選した配置場所を削除
                            positions.RemoveAt(enemyPosition);
                        }
                    }
                }
                else
                {
                    //4以下の場合は、前列と後列に最大2体配置する
                    //ただし、ボス型が抽選された場合は、前列と後列をボスがしめる
                    List<int> positions = new List<int>();
                    positions.Add(0);
                    positions.Add(1);
                    positions.Add(2);
                    positions.Add(3);

                    //U402 数確認の為
                    int largeEnemyAppearCount = 0;
                    int smallEnemyAppearCount = 0;

                    for (int i = 0; i < enemyCount; i++)
                    {
                        //抽選
                        int enemyIndex =
                            UnityEngine.Random.Range(0, smallEnemy.Count + largeEnemy.Count + bossEnemy.Count);
                        int enemyPosition = UnityEngine.Random.Range(0, positions.Count);

                        if (enemyIndex < smallEnemy.Count)
                        {
                            TroopDataModel.SideViewMember enemyData =
                                new TroopDataModel.SideViewMember(smallEnemy[enemyIndex], positions[enemyPosition] / 2,
                                    (positions[enemyPosition] % 2 * 2), 0, 0);
                            troop.sideViewMembers.Add(enemyData);

                            //抽選した敵の同時出現数を減らす
                            smallEnemyCount[enemyIndex]--;

                            //抽選しきった場合は配列から除外
                            if (smallEnemyCount[enemyIndex] <= 0)
                            {
                                smallEnemy.RemoveAt(enemyIndex);
                                smallEnemyCount.RemoveAt(enemyIndex);
                            }
                            largeEnemyAppearCount++;
                            //抽選した配置場所を削除
                            positions.RemoveAt(enemyPosition);
                        }
                        else if (enemyIndex < smallEnemy.Count + largeEnemy.Count)
                        {
                            enemyIndex = enemyIndex - smallEnemy.Count;
                            TroopDataModel.SideViewMember enemyData =
                                new TroopDataModel.SideViewMember(largeEnemy[enemyIndex], positions[enemyPosition] / 2,
                                    (positions[enemyPosition] % 2 * 2), 0, 0);
                            troop.sideViewMembers.Add(enemyData);

                            //抽選した敵の同時出現数を減らす
                            largeEnemyCount[enemyIndex]--;

                            //抽選しきった場合は配列から除外
                            if (largeEnemyCount[enemyIndex] <= 0)
                            {
                                largeEnemy.RemoveAt(enemyIndex);
                                largeEnemyCount.RemoveAt(enemyIndex);
                            }

                            largeEnemyAppearCount++;

                            //抽選した配置場所を削除
                            positions.RemoveAt(enemyPosition);
                        }
                        else
                        {
                            //U402 他の敵が出ているか確認する(ボスは1体のみ出現)
                            if (largeEnemyAppearCount != 0 || smallEnemyAppearCount != 0) continue;

                            //ボス型を抽選した
                            //既に前列または後列を埋めている場合は飛ばす
                            int posWork = -1;
                            for (int j = 0; j < positions.Count; j++)
                            {
                                if (j == enemyPosition) continue;
                                if (positions[enemyPosition] / 2 == positions[j] / 2)
                                {
                                    posWork = j;
                                    break;
                                }
                            }

                            if (posWork == -1)
                            {
                                //もうボスを配置できない
                                i--;
                                continue;
                            }

                            //ボスを2マス分使って配置する
                            enemyIndex = enemyIndex - smallEnemy.Count - largeEnemy.Count;
                            TroopDataModel.SideViewMember enemyData =
                                new TroopDataModel.SideViewMember(bossEnemy[enemyIndex], positions[enemyPosition] / 2,
                                    1, 0, 0);
                            troop.sideViewMembers.Add(enemyData);

                            //抽選した敵の同時出現数を減らす
                            bossEnemyCount[enemyIndex]--;

                            //抽選しきった場合は配列から除外
                            if (bossEnemyCount[enemyIndex] <= 0)
                            {
                                bossEnemy.RemoveAt(enemyIndex);
                                bossEnemyCount.RemoveAt(enemyIndex);
                            }

                            //抽選した配置場所を削除
                            if (enemyPosition < posWork)
                            {
                                positions.RemoveAt(posWork);
                                positions.RemoveAt(enemyPosition);
                            }
                            else
                            {
                                positions.RemoveAt(enemyPosition);
                                positions.RemoveAt(posWork);
                            }
                            //U406 ボス配置したら終わり
                            break;
                        }

                        //配列内に敵がいなくなったら処理終了
                        if (smallEnemy.Count <= 0 && largeEnemy.Count <= 0 && bossEnemy.Count <= 0) break;
                    }
                }
            }
            return troop;
        }
    }
}