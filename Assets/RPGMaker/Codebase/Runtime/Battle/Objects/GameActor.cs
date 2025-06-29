using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Class;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Common;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.SkillCustom;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.State;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.SystemSetting;
using RPGMaker.Codebase.CoreSystem.Knowledge.Enum;
using RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement;
using RPGMaker.Codebase.Runtime.Battle.Wrapper;
using RPGMaker.Codebase.Runtime.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using static RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime.RuntimeActorDataModel;
using static RPGMaker.Codebase.Runtime.Battle.Objects.GameItem;

namespace RPGMaker.Codebase.Runtime.Battle.Objects
{
    /// <summary>
    /// アクターのパラメータの取得、画像の設定、戦闘の処理とサイドビュー時の画像処理を行うクラス
    /// </summary>
    public class GameActor : GameBattler
    {
        /// <summary>
        /// [レベル]
        /// </summary>
        public int Level => Actor.level;
        /// <summary>
        /// アクターID
        /// </summary>
        public string ActorId => Actor.actorId;
        /// <summary>
        /// アクターのデータモデル
        /// </summary>
        public RuntimeActorDataModel Actor { get; set; }
        /// <summary>
        /// [名前]
        /// </summary>
        public string Name
        {
            get => Actor.name;
            set => Actor.name = value;
        }
        /// <summary>
        /// [二つ名]
        /// </summary>
        public string Nickname => Actor.nickname;
        /// <summary>
        /// [プロフィール]
        /// </summary>
        public string Profile => Actor.profile;
        /// <summary>
        /// 職業データ
        /// </summary>
        private ClassDataModel _classDataModel;
        /// <summary>
        /// [顔]画像ファイル名(拡張子なし)
        /// </summary>
        public string FaceName => Actor.faceImage;
        /// <summary>
        /// [[SV]戦闘キャラ]画像ファイル名(拡張子なし)
        /// </summary>
        public string BattlerName => Actor.battlerImage;
        /// <summary>
        /// 経験値テーブル（累計）
        /// </summary>
        private List<int> _exp;
        /// <summary>
        /// [スキル]の配列
        /// </summary>
        /// <returns></returns>
        public List<SkillCustomDataModel> Skills() {
            var list = new List<SkillCustomDataModel>();

            foreach (var id in Actor.skills)
            {
                var skillData = DataManager.Self().GetSkillCustomDataModel(id);
                if (!list.Contains(skillData) && skillData != null) list.Add(skillData);
            }

            var traits = GetTraits();
            var data = DataManager.Self().GetSkillCustomDataModels();
            for (int i = 0; i < traits.Count; i++)
            {
                // スキル
                if (traits[i].categoryId == (int) TraitSkillAdd)
                {
                    for (int i2 = 0; i2 < data.Count; i2++)
                    {
                        if (data[i2].SerialNumber == traits[i].effectId + 1)
                        {
                            if (!list.Contains(data[i2])) list.Add(data[i2]);
                        }
                    }
                }
            }

            list.Sort((a, b) =>
            {
                return a.SerialNumber - b.SerialNumber;
            });

            return list;
        }
        /// <summary>
        /// [装備]の配列
        /// </summary>
        public List<GameItem> _equips = new List<GameItem>();
        
        /// <summary>
        /// 行動の番号
        /// </summary>
        public int            _actionInputIndex;
        /// <summary>
        /// 最後のメニュースキル
        /// </summary>
        public GameItem _lastMenuSkill;
        /// <summary>
        /// 最後の戦闘スキル
        /// </summary>
        //public GameItem _lastBattleSkill;

        /// <summary>
        /// 最後のコマンド
        /// </summary>
        public string LastCommandSymbol {
            get
            {
                return Actor.lastCommandSymbol;
            }
            set
            {
                Actor.lastCommandSymbol = value;
            }
        }
        /// <summary>
        /// 最後のコマンドのスキルタイプ
        /// </summary>
        //private string    _lastCommandSymbol;
        public int LastCommandSymbolStypeId
        {
            get
            {
                return Actor.lastCommandSymbolStypeId;
            }
            set
            {
                Actor.lastCommandSymbolStypeId = value;
            }
        }
        /// <summary>
        /// ステートのステップ数
        /// </summary>
        private Dictionary<string, int> _stateSteps;

        public List<string> LevelupMessage { get; set; }
        
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="actor"></param>
        public GameActor(RuntimeActorDataModel actor) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
        }
        public async Task InitForConstructor(RuntimeActorDataModel actor)
        {
#endif
            //Runtime実行中のデータを用いて初期化する
            InitMembers();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            SetUp(actor);
#else
            await SetUp(actor);
#endif
        }

        /// <summary>
        /// メンバ変数を初期化
        /// </summary>
        public override void InitMembers() {
            base.InitMembers();
        }

        /// <summary>
        /// 指定アクターで Gama_Actor を設定
        /// </summary>
        /// <param name="actor"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SetUp(RuntimeActorDataModel actor) {
#else
        public async Task SetUp(RuntimeActorDataModel actor) {
#endif
            //Runtime実行中のデータを用いて初期化する
            Actor = actor;
            
            // アクターのClassを取得
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _classDataModel = new DatabaseManagementService().LoadClassCommonByClassId(Actor.classId);
#else
            _classDataModel = await new DatabaseManagementService().LoadClassCommonByClassId(Actor.classId);
#endif

            //各種初期化処理
            InitExp();
            InitSkills();

            //Uniteでは本クラスはバトルで初期化されるため、ここで追加パラメータ分の反映を行う
            ClearParamPlus();
            SetParamPlus(actor.paramPlus);

            //装備の反映
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            InitEquips(actor.equips);
#else
            await InitEquips(actor.equips);
#endif

            //マップから引き継いだステートを反映する
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            SetInitializeStates();
#else
            await SetInitializeStates();
#endif
        }

        /// <summary>
        /// パラメータの再設定を行う
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ResetActorData(bool isStartBattle = false) {
#else
        public async Task ResetActorData(bool isStartBattle = false) {
#endif
            // アクターのClassを取得
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _classDataModel = new DatabaseManagementService().LoadClassCommonByClassId(Actor.classId);
#else
            _classDataModel = await new DatabaseManagementService().LoadClassCommonByClassId(Actor.classId);
#endif

            //各種初期化処理
            InitExp();
            InitSkills();

            //Uniteでは本クラスはバトルで初期化されるため、ここで追加パラメータ分の反映を行う
            ClearParamPlus();
            SetParamPlus(Actor.paramPlus);

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            InitEquips(Actor.equips);
#else
            await InitEquips(Actor.equips);
#endif

            //マップから引き継いだステートを反映する
            if (isStartBattle)
            {
                ClearStates();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                SetInitializeStates();
#else
                await SetInitializeStates();
#endif
            }
        }

        /// <summary>
        /// HP
        /// </summary>
        public override int Hp
        {
            get => Actor.hp;
            set
            {
                if (Actor != null) Actor.hp = value;
            }
        }

        /// <summary>
        /// MP
        /// </summary>
        public override int Mp
        {
            get => Actor.mp;
            set
            {
                if (Actor != null) Actor.mp = value;
            }
        }

        /// <summary>
        /// TP
        /// </summary>
        public override int Tp
        {
            get => Actor.tp;
            set
            {
                if (Actor != null) Actor.tp = value;
            }
        }

        /// <summary>
        /// 最終ターゲットIndex
        /// </summary>
        public override int LastTargetIndex
        {
            get { return Actor.lastTargetIndex; }
            set { if (Actor != null ) Actor.lastTargetIndex = value; }
        }

        /// <summary>
        /// ステート変化を戻す
        /// </summary>
        public override void ClearStates() {
            base.ClearStates();
            _stateSteps = new Dictionary<string, int>();
        }

        public override void ClearActions() {
            base.ClearActions();
            _actionInputIndex = 0;
        }

        /// <summary>
        /// ステート変化をマップから引き継ぐ
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        protected void SetInitializeStates() {
#else
        protected async Task  SetInitializeStates() {
#endif
            //マップでのみ有効なステートは反映しない
            for (int i = 0; i < Actor.states.Count; i++)
            {
                StateDataModel state = DataManager.Self().GetStateDataModel(Actor.states[i].id);
                if (state.stateOn != 1)
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    AddNewState(Actor.states[i].id);
#else
                    await AddNewState(Actor.states[i].id);
#endif
                    var variance = 1 + Math.Max(state.maxTurns - state.minTurns, 0);
                    StateTurns[Actor.states[i].id] = state.minTurns + new System.Random().Next(0, variance);
                }
            }
        }

        /// <summary>
        /// ステート変化をマップに戻す
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SetBattleEndStates() {
#else
        public async Task SetBattleEndStates() {
#endif
            //バトル中のみ有効なステートを解除
            ClearStatesEndBattle();

            //マップで有効なステートを反映
            for (int i = 0; i < States.Count; i++)
            {
                bool flg = false;
                for (int j = 0; j < Actor.states.Count; j++)
                {
                    if (States[i].id == Actor.states[j].id)
                    {
                        flg = true;
                        break;
                    }
                }
                if (!flg)
                {
                    //バトル中に新規に登録されたステート
                    State item = new State();
                    item.id = States[i].id;
                    item.walkingCount = 0;
                    Actor.states.Add(item);
                }
            }
            for (int i = 0; i < Actor.states.Count; i++)
            {
                bool flg = false;
                for (int j = 0; j < States.Count; j++)
                {
                    if (States[j].id == Actor.states[i].id)
                    {
                        flg = true;
                        break;
                    }
                }
                if (!flg)
                {
                    StateDataModel state = DataManager.Self().GetStateDataModel(Actor.states[i].id);
                    if (state.stateOn != 1)
                    {
                        //バトル中に解除されたステート
                        Actor.states.RemoveAt(i);
                        i--;
                    }
                    else
                    {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        AddNewState(Actor.states[i].id);
#else
                        await AddNewState(Actor.states[i].id);
#endif
                    }
                }
            }
        }

        /// <summary>
        /// 指定ステートを消す
        /// </summary>
        /// <param name="stateId"></param>
        public override void EraseState(string stateId) {
            base.EraseState(stateId);
            _stateSteps.Remove(stateId);
        }

        /// <summary>
        /// 指定ステートの有効ターン数を初期化
        /// </summary>
        /// <param name="stateId"></param>
        public override void ResetStateCounts(string stateId) {
            base.ResetStateCounts(stateId);
            _stateSteps[stateId] = DataManager.Self().GetStateDataModel(stateId).stepsToRemove;
        }

        /// <summary>
        /// [経験値]を初期化
        /// </summary>
        private void InitExp() {
            //経験値曲線を作成
            //アクターから職業データを取得
            _exp = _classDataModel.GetExpTable();
        }

        /// <summary>
        /// 現在の[経験値]を返す
        /// </summary>
        /// <returns></returns>
        public int CurrentExp() {
            return Actor.exp.value;
        }

        /// <summary>
        /// 次のレベルの必要経験値を返す
        /// </summary>
        /// <returns></returns>
        public int NextLevelExp() {
            return _classDataModel.GetExpForLevel(Actor.level + 1);
        }

        /// <summary>
        /// [次のレベルまで]の経験値を返す
        /// </summary>
        /// <returns></returns>
        public int NextRequiredExp() {
            return NextLevelExp() - CurrentExp();
        }

        /// <summary>
        /// 最大レベルを返す
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public int MaxLevel() {
#else
        public async Task<int> MaxLevel() {
#endif
            //最大レベルは、各アクターに設定されている[最大レベル]
            var actor = DataManager.Self().GetActorDataModel(ActorId);

            //ゲーム全体での[最大レベル]以上のレベルが設定されている場合には、ゲーム全体での[最大レベル]の方を優先
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var classDataModel = new DatabaseManagementService().LoadClassCommon()[0];
#else
            var classDataModel = (await new DatabaseManagementService().LoadClassCommon())[0];
#endif
            var maxLevel = classDataModel.maxLevel;
            if (maxLevel > actor.basic.maxLevel)
            {
                maxLevel = actor.basic.maxLevel;
            }

            return maxLevel;
        }

        /// <summary>
        /// スキルの初期化
        /// </summary>
        public void InitSkills() {
            //職業を変更した場合、レベルアップにより習得で来ていたはずのスキルは習得できない
            //その後セーブ＆ロードを行ったとしても同等の挙動である必要があるため、初期化済みかどうかによって
            //スキルの初期化をするかどうかを決定する
            if (Actor.initialized == 0)
            {
                new List<ClassDataModel.SkillType>(CurrentClass().skillTypes).ForEach(learning =>
                {
                    if (learning.level <= Actor.level) LearnSkill(learning.skillId);
                });

                Actor.initialized = 1;
            }
        }

        /// <summary>
        /// 指定スロットを初期化する
        /// </summary>
        /// <param name="equips"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void InitEquips(List<Equip> equips) {
#else
        public async Task InitEquips(List<Equip> equips) {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var slots = EquipSlots();
#else
            var slots = await EquipSlots();
#endif
            var maxSlots = slots.Count;
            _equips = new List<GameItem>();
            for (var i = 0; i < equips.Count; i++)
                if (i < maxSlots)
                {
                    var itemId = equips[i].itemId;
                    var isWeapon = i == 0 || 
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        (ItemManager.CheckEquipTraitDualWield(Actor) && i == 1);
#else
                        (await ItemManager.CheckEquipTraitDualWield(Actor) && i == 1);
#endif
                    GameItem gameItem;
                    if (itemId == "")
                    {
                        gameItem = new GameItem(itemId, GameItem.DataClassEnum.None);
                    }
                    else
                    {
                        gameItem = new GameItem(itemId,
                            isWeapon ? GameItem.DataClassEnum.Weapon : GameItem.DataClassEnum.Armor);
                    }

                    _equips.Add(gameItem);

                }
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            ReleaseUnequippableItems(true);
            Refresh();
#else
            await ReleaseUnequippableItems(true);
            await Refresh();
#endif
        }
        

        /// <summary>
        /// 装備スロットの配列を返す
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<string> EquipSlots() {
#else
        public async Task<List<string>> EquipSlots() {
#endif
            var slots = new List<string>();
            for (var i = 0; i < DataManager.Self().GetSystemDataModel().equipTypes.Count; i++)
                slots.Add(DataManager.Self().GetSystemDataModel().equipTypes[i].id);

            //特徴が二刀流であった場合には、盾の部分を武器に変更する
            if (slots.Count >= 2 &&
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                ItemManager.CheckEquipTraitDualWield(Actor)) slots[1] = "48254f68-178f-448e-98fd-60a0bb616a28";
#else
                await ItemManager.CheckEquipTraitDualWield(Actor)) slots[1] = "48254f68-178f-448e-98fd-60a0bb616a28";
#endif

            return slots;
        }

        /// <summary>
        /// 装備の配列を返す
        /// </summary>
        /// <returns></returns>
        public List<GameItem> Equips() {
            return _equips;
        }

        /// <summary>
        /// 装備している武器を配列で返す
        /// </summary>
        /// <returns></returns>
        public List<GameItem> Weapons() {
            return Equips().FindAll(item => item?.IsWeapon() ?? false);
        }

        /// <summary>
        /// 防具を返す
        /// </summary>
        /// <returns></returns>
        public List<GameItem> Armors() {
            return Equips().FindAll(item => item?.IsArmor() ?? false);
        }

        /// <summary>
        /// 所持アイテムを交換し、交換できたか返す
        /// </summary>
        /// <param name="newItem"></param>
        /// <param name="oldItem"></param>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public bool TradeItemWithParty(GameItem newItem, GameItem oldItem) {
#else
        public async Task<bool> TradeItemWithParty(GameItem newItem, GameItem oldItem) {
#endif
            var party = DataManager.Self().GetGameParty();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            if (newItem != null && !party.HasItem(newItem)) return false;
#else
            if (newItem != null && !await party.HasItem(newItem)) return false;
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            party.GainItem(oldItem, 1);
            party.LoseItem(newItem, 1);
#else
            await party.GainItem(oldItem, 1);
            await party.LoseItem(newItem, 1);
#endif
            return true;
        }

        /// <summary>
        /// 指定アイテムが装備されているか
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool IsEquipped(GameItem item) {
            return Equips().Contains(item);
        }

        /// <summary>
        /// 装備を捨て、所持品に残さない
        /// </summary>
        /// <param name="item"></param>
        public void DiscardEquip(GameItem item) {
            var slotId = Equips().IndexOf(item);
            if (slotId >= 0) _equips[slotId] = null;
        }

        /// <summary>
        /// 装備不可アイテムの装備を外す
        /// </summary>
        /// <param name="forcing"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ReleaseUnequippableItems(bool forcing) {
#else
        public async Task ReleaseUnequippableItems(bool forcing) {
#endif
            try
            {
                var equips = Equips();
                for (var i = 0; i < equips.Count; i++)
                {
                    var item = equips[i];
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    if (item != null && !ItemManager.CanEquip(this, item))
#else
                    if (item != null && !await ItemManager.CanEquip(this, item))
#endif
                    {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        if (!forcing) TradeItemWithParty(null, item);
#else
                        if (!forcing) await TradeItemWithParty(null, item);
#endif
                        var equipTypes = DataManager.Self().GetSystemDataModel().equipTypes;
                        SystemSettingDataModel.EquipType equipType = null;
                        for (int j = 0; j < equipTypes.Count; j++)
                        {
                            if (item.ETypeId == equipTypes[j].id)
                            {
                                equipType = equipTypes[j];
                                break;
                            }
                        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        ItemManager.RemoveEquipment(Actor, equipType, i, false, true);
#else
                        await ItemManager.RemoveEquipment(Actor, equipType, i, false, true);
#endif
                        _equips[i] = null;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 指定スキルの発動条件に合う装備をしているか
        /// </summary>
        /// <param name="skill"></param>
        /// <returns></returns>
        public override bool IsSkillWtypeOk(GameItem skill) {
            // SerialNoからUUIDに変換する
            var wtypeId1 = ConvertUniteData.WeaponTypeSerialNoToUuid(skill.RequiredWTypeId1);
            var wtypeId2 = ConvertUniteData.WeaponTypeSerialNoToUuid(skill.RequiredWTypeId2);

            // スキルの発動条件に武器タイプが無い、又は武器タイプ設定がある場合には、その武器タイプの武器を装備しているかチェック
            if ((wtypeId1 == "" && wtypeId2 == "") ||
                (wtypeId1 != "" && IsWTypeEquipped(wtypeId1)) ||
                (wtypeId2 != "" && IsWTypeEquipped(wtypeId2)))
                return true;

            return false;
        }

        /// <summary>
        /// 指定武器タイプの武器を装備しているか
        /// </summary>
        /// <param name="wtypeId">武器タイプID（UUID）</param>
        /// <returns></returns>
        public bool IsWTypeEquipped(string wtypeId) {
            return Weapons().Any(weapon => { return weapon.WTypeId == wtypeId; });
        }

        /// <summary>
        /// 能力値やステートを規定値内に収める処理
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public override void Refresh() {
            ReleaseUnequippableItems(false);
            base.Refresh();
        }
#else
        public override async Task Refresh() {
            await ReleaseUnequippableItems(false);
            await base.Refresh();
        }
#endif

        /// <summary>
        /// アクターかどうか
        /// </summary>
        /// <returns></returns>
        public override bool IsActor() {
            return true;
        }

        /// <summary>
        /// 味方パーティを返す
        /// </summary>
        /// <returns></returns>
        public override GameUnit FriendsUnit() {
            return DataManager.Self().GetGameParty();
        }

        /// <summary>
        /// 敵グループを返す
        /// </summary>
        /// <returns></returns>
        public override GameUnit OpponentsUnit() {
            return DataManager.Self().GetGameTroop();
        }

        /// <summary>
        /// キャラ番号を返す
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public override int Index() {
            return DataManager.Self().GetGameParty().Members().IndexOf(this);
        }
#else
        public override async Task<int> Index() {
            return (await DataManager.Self().GetGameParty().Members()).IndexOf(this);
        }
#endif

        /// <summary>
        /// 戦闘に参加しているか
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public override bool IsBattleMember() {
            return DataManager.Self().GetGameParty().BattleMembers().Contains(this);
        }
#else
        public override async Task<bool> IsBattleMember() {
            return (await DataManager.Self().GetGameParty().BattleMembers()).Contains(this);
        }
#endif

        /// <summary>
        /// 現在の[クラス]を返す
        /// </summary>
        /// <returns></returns>
        public ClassDataModel CurrentClass() {
            return DataManager.Self().GetClassDataModel(Actor.classId);
        }

        /// <summary>
        /// 使用可能なスキルの配列を返す
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<SkillCustomDataModel> UsableSkills() {
            return Skills().FindAll(skill => CanUse(new GameItem(skill.basic.id, GameItem.DataClassEnum.Skill)));
        }
#else
        public async Task<List<SkillCustomDataModel>> UsableSkills() {
            var r = new List<SkillCustomDataModel>();
            foreach (var skill in Skills()) {
                if (await CanUse(new GameItem(skill.basic.id, GameItem.DataClassEnum.Skill))) {
                    r.Add(skill);
                }
            }
            return r;
        }
#endif

        /// <summary>
        /// 特徴オブジェクトを配列で返す
        /// </summary>
        /// <returns></returns>
        public override List<TraitCommonDataModel> TraitObjects() {
            var states = base.TraitObjects();
            List<TraitCommonDataModel> traitList = new List<TraitCommonDataModel>();

            //アクターに設定されている特徴を付加
            var actorDataModel = DataManager.Self().GetActorDataModel(ActorId);
            if (actorDataModel.traits != null && actorDataModel.traits.Count > 0)
                foreach (var trait in actorDataModel.traits)
                    if (!traitList.Contains(trait))
                        traitList.Add(trait);

            //職業に設定されている特徴を付加
            var classDataModel = DataManager.Self().GetClassDataModel(Actor.classId);
            if (classDataModel.traits != null && classDataModel.traits.Count > 0)
                //本来はforeachでよいが、配列の4番目（狙われ率）の数値に誤りがあったため、
                //既に制作中のゲームのマスタでも動作するように、強制的に変換する処理を入れる
                for (var i = 0; i < classDataModel.traits.Count; i++)
                {
                    var trait = classDataModel.traits[i];
                    if (i == 3)
                        trait.effectId = 0;
                    if (!traitList.Contains(trait))
                        traitList.Add(trait);
                }

            //装備に設定されている特徴を付加
            var equips = Equips();
            for (var i = 0; i < equips.Count; i++)
                if (equips.ElementAtOrDefault(i) != null)
                    if (equips[i].Traits != null)
                        foreach (var trait in equips[i].Traits)
                            if (!traitList.Contains(trait))
                                traitList.Add(trait);
            //ステートに設定されている特徴を追加
            for (var i = 0; i < states.Count; i++)
                if (states.ElementAtOrDefault(i) != null)
                    if (states[i] != null)
                        if (!traitList.Contains(states[i]))
                            traitList.Add(states[i]);

            return traitList;
        }

        /// <summary>
        /// [攻撃時属性]の配列を返す
        /// </summary>
        /// <returns></returns>
        public override List<int> AttackElements() {
            var set = base.AttackElements();
            if (HasNoWeapons() && !set.Contains(BareHandsElementId())) set.Add(BareHandsElementId());
            return set;
        }

        /// <summary>
        /// 武器を持っていな(素手)か
        /// </summary>
        /// <returns></returns>
        public bool HasNoWeapons() {
            return Weapons().Count == 0;
        }

        /// <summary>
        /// 素手攻撃の属性IDを返す
        /// Uniteの場合は、UUIDにする必要があるかもしれない
        /// </summary>
        /// <returns></returns>
        public int BareHandsElementId() {
            return 1;
        }

        /// <summary>
        /// 指定通常能力値の最大値を返す
        /// </summary>
        /// <param name="paramId"></param>
        /// <returns></returns>
        public override int ParamMax(int paramId) {
            if (paramId == 0) return 9999;
            return base.ParamMax(paramId);
        }

        /// <summary>
        /// 指定通常能力値の基本値を返す
        /// </summary>
        /// <param name="paramId"></param>
        /// <returns></returns>
        public override int ParamBase(int paramId) {
            var param = 0;

            switch (paramId)
            {
                case 0:
                    param = CurrentClass().parameter.maxHp[Actor.level];
                    break;
                case 1:
                    param = CurrentClass().parameter.maxMp[Actor.level];
                    break;
                case 2:
                    param = CurrentClass().parameter.attack[Actor.level];
                    break;
                case 3:
                    param = CurrentClass().parameter.defense[Actor.level];
                    break;
                case 4:
                    param = CurrentClass().parameter.magicAttack[Actor.level];
                    break;
                case 5:
                    param = CurrentClass().parameter.magicDefense[Actor.level];
                    break;
                case 6:
                    param = CurrentClass().parameter.speed[Actor.level];
                    break;
                case 7:
                    param = CurrentClass().parameter.luck[Actor.level];
                    break;
            }

            return param;
        }

        /// <summary>
        /// 指定通常能力値に加算される値を返す
        /// </summary>
        /// <param name="paramId"></param>
        /// <returns></returns>
        public override int ParamPlus(int paramId) {
            var value = base.ParamPlus(paramId);
            return value;
        }

        /// <summary>
        /// 1撃目のアニメーションIDを返す
        /// </summary>
        /// <returns></returns>
        public string AttackAnimationId1() {
            if (HasNoWeapons()) return BareHandsAnimationId();
            var weapons = Weapons();
            return weapons[0]?.AnimationId ?? "0";
        }

        /// <summary>
        /// 2撃目のアニメーションIDを返す
        /// </summary>
        /// <returns></returns>
        public string AttackAnimationId2() {
            var weapons = Weapons();
            if (weapons.Count < 2) return "0";
            return weapons[1]?.AnimationId ?? "0";
        }

        /// <summary>
        /// 素手の場合のアニメーションを返却
        /// </summary>
        /// <returns></returns>
        public string BareHandsAnimationId() {
            return "0";
        }

        /// <summary>
        /// 経験値を加え、必要ならレベルアップ処理を行う
        /// </summary>
        /// <param name="exp"></param>
        /// <param name="show"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ChangeExp(int exp, bool show) {
#else
        public async Task ChangeExp(int exp, bool show) {
#endif
            //EXPを単純に加算する
            Actor.exp.value = exp;

            //加算後のレベルがいくつなのかを求める
            var nextLevel = 0;
            for (var i = 0; i < _exp.Count; i++)
                if (_exp[i] > Actor.exp.value)
                {
                    //経験値に満たなかったので、そのレベルには到達できない
                    //i = 0 で Lv1 なので、iを代入すると、経験値獲得後のレベルになる
                    nextLevel = i;

                    //最大レベルを越えている場合には、最大レベルで丸める
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    if (nextLevel > MaxLevel())
                    {
                        nextLevel = MaxLevel();
                    }
#else
                    if (nextLevel > await MaxLevel())
                    {
                        nextLevel = await MaxLevel();
                    }
#endif
                    break;
                }

            //nextLevelがヒットしなかった場合は、最大レベルとする
            if (nextLevel == 0)
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                nextLevel = MaxLevel();
#else
                nextLevel = await MaxLevel();
#endif

            var levelChange = false;
            var lastSkills = Skills();

            //レベルアップしている場合
            if (Actor.level < nextLevel)
            {
                int nowLevel = Actor.level;
                levelChange = true;
                for (var i = 0; i < nextLevel - nowLevel; i++)
                {
                    LevelUp();
                }
            }
            //レベルダウンしている場合
            else if (Actor.level > nextLevel)
            {
                //レベルダウンの場合は、レベル代入のみ
                Actor.level = nextLevel;
            }

            //レベルアップ演出
            if (show && levelChange)
            {
                DisplayLevelUp(FindNewSkills(lastSkills));
            }
            else if (levelChange)
            {
                CreateLevelUpMessage(FindNewSkills(lastSkills));
            }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            Refresh();
#else
            await Refresh();
#endif
        }

        /// <summary>
        /// レベルアップ時処理
        /// </summary>
        public void LevelUp() {
            //アクターのレベルを1上げる
            Actor.level++;
            new List<ClassDataModel.SkillType>(CurrentClass().skillTypes).ForEach(learning =>
            {
                if (learning.level == Actor.level)
                    //スキルを習得する
                    LearnSkill(learning.skillId);
            });
        }

        /// <summary>
        /// レベルダウン時処理
        /// </summary>
        public void LevelDown() {
            //アクターのレベルを1下げる
            if (Actor.level > 1)
            {
                Actor.level--;
                Actor.exp.value = _exp[Actor.level - 1];
            }
        }

        /// <summary>
        /// レベルアップにより新たに習得したスキルを返却
        /// </summary>
        /// <param name="lastSkills"></param>
        /// <returns></returns>
        public List<SkillCustomDataModel> FindNewSkills(List<SkillCustomDataModel> lastSkills) {
            var newSkills = Skills();
            for (var i = 0; i < lastSkills.Count; i++)
            {
                var index = newSkills.IndexOf(lastSkills[i]);
                if (index >= 0) newSkills.RemoveAt(index);
            }

            return newSkills;
        }

        /// <summary>
        /// レベルアップの表示
        /// </summary>
        /// <param name="newSkills"></param>
        public void DisplayLevelUp(List<SkillCustomDataModel> newSkills) {
            var text = LevelUpText();
            DataManager.Self().GetGameMessage().NewPage();
            DataManager.Self().GetGameMessage().Add(text);
            newSkills.ForEach(skill =>
            {
                DataManager.Self().GetGameMessage()
                    .Add(TextManager.Format(TextManager.obtainSkill, skill.basic.name));
            });
        }

        /// <summary>
        /// 経験値取得処理
        /// </summary>
        /// <param name="exp"></param>
        /// <param name="flg"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void GainExp(int exp, bool flg = true) {
#else
        public async Task GainExp(int exp, bool flg = true) {
#endif
            //経験値はint上限値までは増やし続ける
            //int上限を越える場合は、int上限に丸める
            if (int.MaxValue - Actor.exp.value < Mathf.RoundToInt(exp * FinalExpRate()))
            {
                Actor.exp.value = int.MaxValue;
            }
            else if (Actor.exp.value + Mathf.RoundToInt(exp * FinalExpRate()) < 0)
            {
                Actor.exp.value = 0;
            }
            else
            {
                Actor.exp.value += Mathf.RoundToInt(exp * FinalExpRate());
            }
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            ChangeExp(Actor.exp.value, flg);
#else
            await ChangeExp(Actor.exp.value, flg);
#endif
        }

        /// <summary>
        /// 戦闘に出ているか控えかで変わる経験値の比率を返す
        /// </summary>
        public float FinalExpRate() {
            return (float) Exr;
        }

        /// <summary>
        /// 指定レベルに変更する
        /// </summary>
        /// <param name="level"></param>
        /// <param name="show"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ChangeLevel(int level, bool show) {
#else
        public async Task ChangeLevel(int level, bool show) {
#endif
            //level の数分レベルアップした際に、最大レベルを越える場合には、最大レベルまでとする
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            level = Mathf.Min(level, MaxLevel() - Actor.level);
#else
            level = Mathf.Min(level, await MaxLevel() - Actor.level);
#endif

            if (level > 0)
            {
                var lastSkills = Skills();
                for (var i = 0; i < level; i++)
                {
                    //次のレベルまでに必要な経験値を加算することによって、レベルアップする
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    GainExp(NextRequiredExp(), false);
#else
                    await GainExp(NextRequiredExp(), false);
#endif
                }
                //演出が必要な場合は、演出する
                if (show)
                {
                    DisplayLevelUp(FindNewSkills(lastSkills));
                }
            }
            else
            {
                for (var i = 0; i > level; i--)
                    LevelDown();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                Refresh();
#else
                await Refresh();
#endif
            }
        }

        /// <summary>
        /// 指定スキルを習得する
        /// </summary>
        /// <param name="skillId"></param>
        public void LearnSkill(string skillId) {
            if (!IsLearnedSkill(skillId))
            {
                Actor.skills.Add(skillId);
                Actor.skills.Sort((a, b) =>
                {
                    return 0;
                });
            }
        }

        /// <summary>
        /// 指定スキルを習得する（使用効果のスキル取得のみ）
        /// </summary>
        /// <param name="skillId"></param>
        public void LearnSkill(int skillId) {
            if (!IsLearnedSkill(skillId))
            {
                SkillCustomDataModel data = null;
                var skills = DataManager.Self().GetSkillCustomDataModels();
                for (int i = 0; i < skills.Count; i++)
                    if (skills[i].SerialNumber == skillId)
                    {
                        data = skills[i];
                        break;
                    }
                if (data == null) return;

                Actor.skills.Add(data.basic.id);
                Actor.skills.Sort((a, b) =>
                {
                    return 0;
                });
            }
        }

        /// <summary>
        /// 指定スキルを習得しているか
        /// </summary>
        /// <param name="skillId"></param>
        /// <returns></returns>
        public bool IsLearnedSkill(string skillId) {
            return Actor.skills.Contains(skillId);
        }

        /// <summary>
        /// 指定スキルを習得しているか
        /// </summary>
        /// <param name="skillId"></param>
        /// <returns></returns>
        public bool IsLearnedSkill(int skillId) {
            SkillCustomDataModel skill = null;
            var skills = Skills();
            for (int i = 0; i < skills.Count; i++)
                if (skills[i].SerialNumber == skillId)
                {
                    skill = skills[i];
                    break;
                }
            if (skill != null)
                return Actor.skills.Contains(skill.basic.id);
            return false;
        }

        /// <summary>
        /// 指定クラスに変更する
        /// </summary>
        /// <param name="classId"></param>
        /// <param name="keepExp"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ChangeClass(string classId, bool keepExp) {
#else
        public async Task ChangeClass(string classId, bool keepExp) {
#endif
            //職業の変更を行う
            //経験値を据え置きにする場合は、レベルの数値を計算して再設定する
            Actor.classId = classId;
            if (!keepExp)
            {
                Actor.exp.value = 0;
            }
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            SetUp(Actor);
            SetCurrentLevel();
            Refresh();
#else
            await SetUp(Actor);
            await SetCurrentLevel();
            await Refresh();
#endif
        }

        /// <summary>
        /// スプライト(画像)が表示されているか
        /// </summary>
        /// <returns></returns>
        public override bool IsSpriteVisible() {
            return DataManager.Self().GetSystemDataModel().battleScene.viewType == 1;
        }

        /// <summary>
        /// 指定アニメーション開始(追加)
        /// </summary>
        /// <param name="animationId"></param>
        /// <param name="mirror"></param>
        /// <param name="delay"></param>
        public override void StartAnimation(string animationId, bool mirror, float delay) {
            mirror = !mirror;
            base.StartAnimation(animationId, mirror, delay);
        }

        /// <summary>
        /// 指定アクションの開始動作を実行
        /// </summary>
        /// <param name="action"></param>
        public override void PerformActionStart(GameAction action) {
            base.PerformActionStart(action);
        }

        /// <summary>
        /// 指定アクションを実行
        /// </summary>
        /// <param name="action"></param>
        public override void PerformAction(GameAction action) {
            base.PerformAction(action);
            if (action.IsAttack())
                PerformAttack();
            else if (action.IsGuard())
                RequestMotion("defence");
            else if (action.IsMagicSkill())
                RequestMotion("magic");
            else if (action.IsSkill())
                RequestMotion("skill");
            else if (action.IsItem()) 
                RequestMotion("item");
        }

        /// <summary>
        /// 行動終了を実行
        /// </summary>
        public override void PerformActionEnd() {
            base.PerformActionEnd();
        }

        /// <summary>
        /// 攻撃動作を実行
        /// </summary>
        public async void PerformAttack() {
            var weapons = Weapons();
            var wtypeId = weapons.ElementAtOrDefault(0)?.WTypeId ?? "0";
            
            //装備がなければ、「なし（素手）」のタイプを設定する
            if (wtypeId == "0")
            {
                wtypeId = DataManager.Self().GetSystemDataModel().weaponTypes[0].id;
            }

            SystemSettingDataModel.WeaponType attackMotion = null;
            for (var i = 0; i < DataManager.Self().GetSystemDataModel().weaponTypes.Count; i++)
            {
                if (DataManager.Self().GetSystemDataModel().weaponTypes[i].id == wtypeId)
                {
                    attackMotion = DataManager.Self().GetSystemDataModel().weaponTypes[i];
                    break;
                }
            }

            if (attackMotion != null)
            {
                //武器アニメーションの指定
                //一歩前に出る分待つ
                //一歩前に出るのは12fで、フラグのON/OFF分の2Fを加えた分待つ
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                await Task.Delay(Mathf.RoundToInt(14f / 60f * 1000));
#else
                await UniteTask.Delay(Mathf.RoundToInt(14f / 60f * 1000));
#endif

                if (attackMotion.motionId == 0)
                    RequestMotion("trust");
                else if (attackMotion.motionId == 1)
                    RequestMotion("swing");
                else if (attackMotion.motionId == 2) 
                    RequestMotion("projectile");

                StartWeaponAnimation(attackMotion.image);
            }
        }

        /// <summary>
        /// 被ダメージ動作を実行
        /// </summary>
        public override void PerformDamage() {
            base.PerformDamage();
            if (IsSpriteVisible())
            {
                //サイドビューの場合、アクターがダメージを受けたタイミングで、ダメージモーションを再生する
                RequestMotion("damage");
            }

            SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_SE, DataManager.Self().GetSystemDataModel().soundSetting.actorDamage);
            SoundManager.Self().PlaySe();
        }

        /// <summary>
        /// 回避動作を実行
        /// </summary>
        public override void PerformEvasion() {
            base.PerformEvasion();
            RequestMotion("evade");
        }

        /// <summary>
        /// 魔法回避動作を実行
        /// </summary>
        public override void PerformMagicEvasion() {
            base.PerformMagicEvasion();
            RequestMotion("evade");
        }

        /// <summary>
        /// カウンター動作を実行
        /// </summary>
        public override void PerformCounter() {
            base.PerformCounter();
            PerformAttack();
        }

        /// <summary>
        /// 倒れる動作を実行
        /// </summary>
        public override void PerformCollapse() {
            base.PerformCollapse();
            if (DataManager.Self().GetGameParty().InBattle())
            {
                SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_SE, DataManager.Self().GetSystemDataModel().soundSetting.actorDied);
                SoundManager.Self().PlaySe();
            }
        }

        /// <summary>
        /// 勝利動作を実行
        /// </summary>
        public void PerformVictory() {
            if (CanMove()) RequestMotion("win");
        }

        /// <summary>
        /// 逃走動作を実行
        /// </summary>
        public void PerformEscape() {
            if (CanMove()) RequestMotion("escape");
        }

        /// <summary>
        /// 行動の配列を生成して返す
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<GameAction> MakeActionList() {
#else
        public async Task<List<GameAction>> MakeActionList() {
#endif
            var list = new List<GameAction>();
            var action = new GameAction(this);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            await action.InitForConstructor(this);
#endif

            // [通常攻撃] に変更がある可能性があるため、ここで改めて設定しなおし
            var skillId = GameBattlerBase.AttackSkillId;
            var skillData = DataManager.Self().GetSkillCustomDataModels();
            SkillCustomDataModel skill = null;
            for (int i = 0; i < skillData.Count; i++)
            {
                if (skillData[i].SerialNumber == AttackSkill() + 1)
                {
                    skill = skillData[i];
                    skillId = skill.basic.id;
                    break;
                }
            }
            action.SetAttackSkill(skillId);

            // [通常攻撃]を設定
            action.SetAttack();
            list.Add(action);

            // スキルを設定
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            UsableSkills().ForEach(skill =>
#else
            foreach (var sk in await UsableSkills())
#endif
            {
                action = new GameAction(this);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                action.SetSkill(skill.basic.id);
#else
                await action.InitForConstructor(this);
                action.SetSkill(sk.basic.id);
#endif
                list.Add(action);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            });
#else
            }
#endif
            return list;
        }

        /// <summary>
        /// 自動戦闘の行動を生成
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void MakeAutoBattleActions() {
#else
        public async Task MakeAutoBattleActions() {
#endif
            for (var i = 0; i < NumActions(); i++)
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var list = MakeActionList();
#else
                var list = await MakeActionList();
#endif
                double maxValue = 0;
                for (var j = 0; j < list.Count; j++)
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    var value = list[j].Evaluate();
#else
                    var value = await list[j].Evaluate();
#endif
                    if (value > maxValue)
                    {
                        maxValue = value;
                        SetAction(i, list[j]);
                    }
                }
            }

            SetActionState(ActionStateEnum.Waiting);
        }

        /// <summary>
        /// 混乱状態の行動を生成
        /// </summary>
        public void MakeConfusionActions() {
            for (var i = 0; i < NumActions(); i++)
                Actions[i].SetConfusion();

            SetActionState(ActionStateEnum.Waiting);
        }

        /// <summary>
        /// アニメーションを生成
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public override void MakeActions() {
#else
        public override async Task MakeActions() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            base.MakeActions();
#else
            await base.MakeActions();
#endif

            if (NumActions() > 0)
                SetActionState(ActionStateEnum.Undecided);
            else
                SetActionState(ActionStateEnum.Waiting);

            if (IsAutoBattle())
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                MakeAutoBattleActions();
#else
                await MakeAutoBattleActions();
#endif
            else if (IsConfused()) 
                MakeConfusionActions();
        }

        /// <summary>
        /// 入力された行動を返す
        /// </summary>
        /// <returns></returns>
        public GameAction InputtingAction() {
            return Actions[_actionInputIndex];
        }

        /// <summary>
        /// 次のコマンドを選択し、選択できたか返す
        /// </summary>
        /// <returns></returns>
        public bool SelectNextCommand() {
            if (_actionInputIndex < NumActions() - 1)
            {
                _actionInputIndex++;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 前のコマンドを選択し、選択できたか返す
        /// </summary>
        /// <returns></returns>
        public bool SelectPreviousCommand() {
            if (_actionInputIndex > 0)
            {
                _actionInputIndex--;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 最後のメニュースキルを返す
        /// </summary>
        /// <returns></returns>
        public GameItem LastMenuSkill() {
            return _lastMenuSkill;
        }

        /// <summary>
        /// 最後の戦闘スキルを返す
        /// </summary>
        /// <returns></returns>
        public GameItem LastBattleSkill() {
            //アイテムの種類( ‘item’, ‘skill’, ‘weapon’, ‘armor’, ‘’ )
            switch (Actor.lastBattleSkill.dataClass)
            {
                case "item":
                    return new GameItem(Actor.lastBattleSkill.itemId, DataClassEnum.Item);
                case "skill":
                    return new GameItem(Actor.lastBattleSkill.itemId, DataClassEnum.Skill);
                case "weapon":
                    return new GameItem(Actor.lastBattleSkill.itemId, DataClassEnum.Weapon);
                case "armor":
                    return new GameItem(Actor.lastBattleSkill.itemId, DataClassEnum.Armor);
            }
            return new GameItem(Actor.lastBattleSkill.itemId, DataClassEnum.None);
        }

        /// <summary>
        /// 最後の戦闘スキルを設定
        /// </summary>
        /// <param name="skill"></param>
        public void SetLastBattleSkill(GameItem skill) {
            Actor.lastBattleSkill.itemId = skill.ItemId;
            switch (skill.DataClass)
            {
                case DataClassEnum.Item:
                    Actor.lastBattleSkill.dataClass = "item";
                    break;
                case DataClassEnum.Skill:
                    Actor.lastBattleSkill.dataClass = "skill";
                    break;
                case DataClassEnum.Weapon:
                    Actor.lastBattleSkill.dataClass = "weapon";
                    break;
                case DataClassEnum.Armor:
                    Actor.lastBattleSkill.dataClass = "armor";
                    break;
                default:
                    Actor.lastBattleSkill.dataClass = "";
                    break;
            }
        }

        /// <summary>
        /// 指定アイテムが[特殊効果 - 逃げる]を持っているか
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool TestEscape(GameItem item) {
            bool flg = false;
            //対象者側
            if (item.Effects != null)
            {
                flg = item.Effects.Any(effect =>
                    effect != null && ConvertUniteData.SetEffectCode(effect) == GameAction.EffectSpecial);
            }
            //使用者側
            if (item.EffectsMyself != null && !flg)
            {
                flg = item.EffectsMyself.Any(effect =>
                    effect != null && ConvertUniteData.SetEffectCode(effect) == GameAction.EffectSpecial);
            }
            return flg;
        }

        /// <summary>
        /// 指定アイテムが使用可能か
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public override bool MeetsUsableItemConditions(GameItem item) {
            if (DataManager.Self().GetGameParty().InBattle() && !BattleManager.CanEscape() && TestEscape(item))
                return false;

            return base.MeetsUsableItemConditions(item);
        }
        
        /// <summary>
        /// 床ダメージ
        /// </summary>
        /// <param name="damage"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ExecuteFloorDamage(int damage) {
#else
        public async Task ExecuteFloorDamage(int damage) {
#endif
            damage = Math.Min(damage, MaxFloorDamage());
            GainHp(-damage);

            if (Hp == 0)
            {
                //ステートが付与可能なタイミングかどうかのチェック
                var stateDataModels = DataManager.Self().GetStateDataModels();
                if (IsStateTiming(stateDataModels[0].id))
                {
                    //ステート付与
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    AddState(stateDataModels[0].id);
#else
                    await AddState(stateDataModels[0].id);
#endif
                }
                else
                {
                    //戦闘不能を付与できないタイミングだった場合は、Hp=1に戻す
                    GainHp(1);
                }
            }
        }

        /// <summary>
        /// 床ダメージで戦闘不能になるか
        /// </summary>
        /// <returns></returns>
        private int MaxFloorDamage() {
            return DataManager.Self().GetSystemDataModel().optionSetting.optFloorDeath == 1 ? Hp : Math.Max(Hp - 1, 0);
        }

        //================================================================================
        // 以下はUniteで追加されたメソッド
        //================================================================================

        /// <summary>
        /// 現在のレベル設定
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void SetCurrentLevel() {
#else
        private async Task SetCurrentLevel() {
#endif
            //経験値情報から、現在のレベルを設定する
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            for (int i = 1; i < MaxLevel(); i++)
#else
            for (int i = 1; i < await MaxLevel(); i++)
#endif
            {
                if (Actor.exp.value >= _classDataModel.GetExpForLevel(i) &&
                    Actor.exp.value < _classDataModel.GetExpForLevel(i + 1))
                {
                    Actor.level = i;
                    return;
                }
            }
            //ここまで来てしまった場合は、最大レベルを設定
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            Actor.level = MaxLevel();
#else
            Actor.level = await MaxLevel();
#endif
        }

        /// <summary>
        /// 全ての特徴を返却
        /// </summary>
        /// <returns></returns>
        private List<TraitCommonDataModel> GetTraits() {
            return base.AllTraits();
        }

        /// <summary>
        /// 属性の特徴を返却
        /// </summary>
        /// <returns></returns>
        public override List<TraitCommonDataModel> AllTraits() {
            var ret = base.AllTraits();
            return AddTraitsElement(ret);
        }


        /// <summary>
        /// 属性設定から特徴を付与する
        /// </summary>
        public List<TraitCommonDataModel> AddTraitsElement(List<TraitCommonDataModel> ret) {
            //属性追加
            //属性はSerialNoで持つ
            var actorDataModel = DataManager.Self().GetActorDataModel(ActorId);
            var classDataModel = DataManager.Self().GetClassDataModel(actorDataModel.basic.classId);
            var system = DataManager.Self().GetSystemDataModel();
            SystemSettingDataModel.Element classElements = null;
            for (int i = 0; i < system.elements.Count; i++)
                if (system.elements[i].id == classDataModel.element)
                {
                    classElements = system.elements[i];
                    break;
                }
            if (classElements == null) classElements = system.elements[0];

            MyElement = new List<int>();

            //無し以外の属性を詰める
            if (actorDataModel.element != 0)
                MyElement.Add(actorDataModel.element);
            //職業とアクターの属性が異なる場合は追加
            if ((classElements.SerialNumber - 1) != 0 && (classElements.SerialNumber - 1) != actorDataModel.element)
                MyElement.Add(classElements.SerialNumber - 1);

            return ret;
        }

        /// <summary>
        /// レベルアップ時のテキスト情報返却
        /// </summary>
        /// <returns></returns>
        public string LevelUpText() {
            var name = Name;
            name = name.Replace("\\", "\\\\");
            return TextManager.Format(TextManager.levelUp, name, TextManager.level, Actor.level.ToString());
        }

        /// <summary>
        /// レベルアップ時のメッセージ作成
        /// </summary>
        /// <param name="newSkills"></param>
        public void CreateLevelUpMessage(List<SkillCustomDataModel> newSkills) {
            //以下の変数は、イベント等の他の個所でレベルアップ表現をする際に、外側から初期化すること
            if (LevelupMessage == null)
            {
                LevelupMessage = new List<string>();
            }
            newSkills.ForEach(skill =>
            {
                LevelupMessage.Add(TextManager.Format(TextManager.obtainSkill, skill.basic.name));
            });
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public override bool AddState(string stateId) {
#else
        public override async Task<bool> AddState(string stateId) {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            bool ret = base.AddState(stateId);
#else
            bool ret = await base.AddState(stateId);
#endif
            if (ret)
            {
                if (GameStateHandler.IsMap())
                {
                    //マップから実行された場合には、実行結果を即クリアする
                    ClearResult();
                    //マップに即引き継ぎ
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    SetBattleEndStates();
#else
                    await SetBattleEndStates();
#endif
                }
            }
            return ret;
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public override bool RemoveState(string stateId) {
#else
        public override async Task<bool> RemoveState(string stateId) {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            bool ret = base.RemoveState(stateId);
#else
            bool ret = await base.RemoveState(stateId);
#endif
            if (ret)
            {
                if (GameStateHandler.IsMap())
                {
                    //マップから実行された場合には、実行結果を即クリアする
                    ClearResult();
                    //マップに即引き継ぎ
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    SetBattleEndStates();
#else
                    await SetBattleEndStates();
#endif
                }
            }
            return ret;
        }

        /// <summary>
        /// 指定能力に指定した値を追加
        /// </summary>
        /// <param name="paramId"></param>
        /// <param name="value"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public override void AddParam(int paramId, int value) {
#else
        public override async Task AddParam(int paramId, int value) {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            base.AddParam(paramId, value);
#else
            await base.AddParam(paramId, value);
#endif

            //RuntimeActorDataModel の方にも保存する
            //パラメータの内容をMVに合わせる
            //0:最大HP
            //1:最大MP
            //2:攻撃力
            //3:防御力
            //4:魔法力
            //5:魔法防御
            //6:俊敏性
            //7:運
            switch (paramId)
            {
                case 0:
                    Actor.paramPlus.maxHp += value;
                    break;
                case 1:
                    Actor.paramPlus.maxMp += value;
                    break;
                case 2:
                    Actor.paramPlus.attack += value;
                    break;
                case 3:
                    Actor.paramPlus.defense += value;
                    break;
                case 4:
                    Actor.paramPlus.magicAttack += value;
                    break;
                case 5:
                    Actor.paramPlus.magicDefence += value;
                    break;
                case 6:
                    Actor.paramPlus.speed += value;
                    break;
                case 7:
                    Actor.paramPlus.luck += value;
                    break;
            }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            Refresh();
#else
            await Refresh();
#endif
        }
    }
}