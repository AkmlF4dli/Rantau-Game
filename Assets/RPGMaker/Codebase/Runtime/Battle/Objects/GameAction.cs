using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Common;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Enemy;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.EventBattle;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.EventCommon;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.EventMap;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Item;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.SkillCustom;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.SystemSetting;
using RPGMaker.Codebase.CoreSystem.Knowledge.Enum;
using RPGMaker.Codebase.CoreSystem.Service.EventManagement;
using RPGMaker.Codebase.Runtime.Battle.Wrapper;
using RPGMaker.Codebase.Runtime.Common;
using RPGMaker.Codebase.Runtime.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using static RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.SystemSetting.SystemSettingDataModel;
using Random = UnityEngine.Random;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Battle.Objects
{
    /// <summary>
    /// 攻撃や防御、スキル・アイテムの使用など、戦闘の行動を記述したクラス
    /// Game_Battler の _actions プロパティが持っていて、逆にこちらからは subject() メソッドで Game_Battler を取得できる
    /// </summary>
    public class GameAction
    {
        public enum TypeEnum
        {
            Attack,
            Skill,
            Item,
            Guard
        }

        public enum HealType
        {
            Hp,
            Mp,
            Tp
        }

        /// <summary>
        /// 定数 HP回復
        /// </summary>
        public const     int  EffectRecoverHp     = 11;
        /// <summary>
        /// 定数 MP回復
        /// </summary>
        public const     int  EffectRecoverMp     = 12;
        /// <summary>
        /// 定数 TP回復
        /// </summary>
        public const     int  EffectGainTp        = 13;
        /// <summary>
        /// 定数 状態異常付与
        /// </summary>
        public const     int  EffectAddState      = 21;
        /// <summary>
        /// 定数 状態異常解除
        /// </summary>
        public const     int  EffectRemoveState   = 22;
        /// <summary>
        /// 定数 バフ付与
        /// </summary>
        public const     int  EffectAddBuff       = 31;
        /// <summary>
        /// 定数 デバフ付与
        /// </summary>
        public const     int  EffectAddDebuff     = 32;
        /// <summary>
        /// 定数 バフ解除
        /// </summary>
        public const     int  EffectRemoveBuff    = 33;
        /// <summary>
        /// 定数 デバフ解除
        /// </summary>
        public const     int  EffectRemoveDebuff  = 34;
        /// <summary>
        /// 定数 逃走時のエフェクト
        /// </summary>
        public const     int  EffectSpecial       = 41;
        /// <summary>
        /// 定数 パラメータ追加
        /// </summary>
        public const     int  EffectGrow          = 42;
        /// <summary>
        /// 定数 スキル習得
        /// </summary>
        public const     int  EffectLearnSkill    = 43;
        /// <summary>
        /// 定数 コモンイベント実行
        /// </summary>
        public const     int  EffectCommonEvent   = 44;
        /// <summary>
        /// エフェクトID 逃走時エフェクト=0
        /// </summary>
        public const     int  SpecialEffectEscape = 0;
        /// <summary>
        /// 攻撃タイプ 必中=0
        /// </summary>
        public const     int  HittypeCertain      = 0;
        /// <summary>
        /// 攻撃タイプ 物理攻撃
        /// </summary>
        public const     int  HittypePhysical     = 1;
        /// <summary>
        /// 攻撃タイプ 魔法攻撃
        /// </summary>
        public const     int  HittypeMagical      = 2;
        private readonly bool _forcing;

        public  GameBattler _reflectionTarget;
        private GameBattler _subject;
        private string      _subjectActorId;
        private int         _subjectEnemyIndex;
        private int         _targetIndex;
        private TraitCommonDataModel _effect;

        private string _skillId = GameBattlerBase.AttackSkillId;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="subject">行動主体（行動しているバトラー）</param>
        /// <param name="forcing">強制行動かどうか</param>
        public GameAction(GameBattler subject, bool forcing = false) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            _forcing = forcing;
        }
        public async Task InitForConstructor(GameBattler subject, bool forcing = false) {
#endif
            _subjectActorId = "";
            _subjectEnemyIndex = -1;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _setSubject(subject);
#else
            await _setSubject(subject);
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _forcing = forcing;
#else
#endif
            Clear();
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        public void Clear() {
            Item = null;
            _targetIndex = -1;
        }

        /// <summary>
        /// 行動主体に関するデータ設定
        /// </summary>
        /// <param name="subject">行動主体</param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void _setSubject(GameBattler subject) {
#else
        private async Task _setSubject(GameBattler subject) {
#endif
            if (subject.IsActor())
            {
                _subjectActorId = ((GameActor) subject).ActorId;
                _subjectEnemyIndex = -1;
            }
            else
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _subjectEnemyIndex = subject.Index();
#else
                _subjectEnemyIndex = await subject.Index();
#endif
                _subjectActorId = "";
            }

            _subject = subject;
        }

        /// <summary>
        /// 行動主体取得
        /// </summary>
        public GameBattler Subject
        {
            get
            {
                return _subject;
            }
        }

        /// <summary>
        /// 行動主体から見たときの、味方のグループを返却する
        /// </summary>
        /// <returns></returns>
        public GameUnit FriendsUnit() {
            return Subject.FriendsUnit();
        }

        /// <summary>
        /// 行動主体から見たときの、敵対グループを返却する
        /// </summary>
        /// <returns></returns>
        public GameUnit OpponentsUnit() {
            return Subject.OpponentsUnit();
        }

        /// <summary>
        /// 敵に指定[行動パターン]を設定
        /// </summary>
        /// <param name="enemyAction"></param>
        public void SetEnemyAction(EnemyDataModel.EnemyAction enemyAction) {
            if (enemyAction != null)
            {
                SetSkill(enemyAction.skillId);
            }
            else
            {
                Clear();
            }
        }

        /// <summary>
        /// 行動に[攻撃]を設定
        /// </summary>
        public void SetAttack() {
            SetSkill(_skillId, TypeEnum.Attack);
        }

        public void SetAttackSkill(string skillId) {
            _skillId = skillId;
        }

        /// <summary>
        /// 行動に[防御]を設定
        /// </summary>
        public void SetGuard() {
            SetSkill(GameBattlerBase.GuardSkillId, TypeEnum.Guard);
        }

        /// <summary>
        /// 行動に[スキル]を設定
        /// </summary>
        /// <param name="skillId"></param>
        public void SetSkill(string skillId, TypeEnum typeEnum = TypeEnum.Skill) {
            Item = new GameItem(skillId, GameItem.DataClassEnum.Skill);
        }

        /// <summary>
        /// 行動に[アイテム][スキル]を設定
        /// </summary>
        /// <param name="itemId"></param>
        public void SetItem(string itemId) {
            Item = new GameItem(itemId, GameItem.DataClassEnum.Item);
        }

        /// <summary>
        /// 指定番号で行動対象を設定
        /// </summary>
        /// <param name="targetIndex"></param>
        public void SetTarget(int targetIndex) {
            _targetIndex = targetIndex;
        }

        /// <summary>
        /// 行動の情報を記述したオブジェクトを返す
        /// 道具というより項目ぐらいの意味で、攻撃・スキルなどにもこれが使われる
        /// </summary>
        public GameItem Item { get; private set; }

        /// <summary>
        /// [スキル]か
        /// </summary>
        /// <returns></returns>
        public bool IsSkill() {
            return Item?.IsSkill() ?? false;
        }

        /// <summary>
        /// [アイテム]か
        /// </summary>
        /// <returns></returns>
        public bool IsItem() {
            return Item?.IsItem() ?? false;
        }

        /// <summary>
        /// 繰り返し行動回数を返す
        /// </summary>
        /// <returns></returns>
        public int NumRepeats() {
            var repeats = Item?.Repeats ?? 1;
            if (IsAttack()) repeats += Subject.AttackTimesAdd();

            return (int) Math.Floor(repeats);
        }

        /// <summary>
        /// _item プロパティの[範囲]と同じものが、指定したの配列の中にあるか
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private bool CheckItemScope(ICollection<int> list) {
            if (Item == null) return false;
            return list.Contains(Item.Scope);
        }

        /// <summary>
        /// [範囲]が敵単体(複数回も含む)か
        /// </summary>
        /// <returns></returns>
        public bool IsForOpponent() {
            return CheckItemScope(new List<int>
            {
                1, 2, 3, 4, 5, 6
            });
        }

        /// <summary>
        /// [範囲]が味方(自身も含む)か 生存,戦闘不能を問わず
        /// </summary>
        /// <returns></returns>
        public bool IsForFriend() {
            return CheckItemScope(new List<int>
            {
                7, 8, 9, 10, 11, 12, 13
            });
        }

        /// <summary>
        /// [範囲]が戦闘不能の味方か
        /// </summary>
        /// <returns></returns>
        public bool IsForDeadFriend() {
            return CheckItemScope(new List<int>
            {
                9, 10
            });
        }

        /// <summary>
        /// [範囲]が生存,戦闘不能問わず味方か
        /// </summary>
        /// <returns></returns>
        public bool IsForAllFriend() {
            return CheckItemScope(new List<int>
            {
                12, 13
            });
        }

        /// <summary>
        /// [範囲]が自分自身か
        /// </summary>
        /// <returns></returns>
        public bool IsForUser() {
            return CheckItemScopeUse();
        }

        /// <summary>
        /// [範囲]が敵味方問わず単体(複数回含まず)か
        /// </summary>
        /// <returns></returns>
        public bool IsForOne() {
            return CheckItemScope(new List<int>
            {
                1, 3, 7, 9, 11, 12
            });
        }

        /// <summary>
        /// [範囲]がランダムな敵か
        /// </summary>
        /// <returns></returns>
        public bool IsForRandom() {
            return CheckItemScope(new List<int>
            {
                3, 4, 5, 6
            });
        }

        /// <summary>
        /// [範囲]が敵味方・戦闘不能問わず全体か
        /// </summary>
        /// <returns></returns>
        public bool IsForAll() {
            return CheckItemScope(new List<int>
            {
                2, 8, 10, 13, 50
            });
        }

        /// <summary>
        /// [範囲]が敵味方全体か
        /// </summary>
        /// <returns></returns>
        public bool IsForOpponentAndFriend() {
            return CheckItemScope(new List<int>
            {
                50
            });
        }


        /// <summary>
        /// [範囲]が単体で対象の選択が必要か 生存,戦闘不能を問わず
        /// </summary>
        /// <returns></returns>
        public bool NeedsSelection() {
            return CheckItemScope(new List<int>
            {
                1, 7, 9, 12
            });
        }

        /// <summary>
        /// 単体攻撃対象の数を返す
        /// </summary>
        /// <returns></returns>
        public int NumTargets() {
            return IsForRandom() ? Item.Scope - 2 : 0;
        }

        /// <summary>
        /// _item プロパティのダメージタイプと同じものが、指定したの配列の中にあるか
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private bool CheckDamageType(ICollection<int> list) {
            if (Item == null) return false;
            return list.Contains(Item.DamageType);
        }

        /// <summary>
        /// ダメージの[タイプ]がHPに関するものか
        /// </summary>
        /// <returns></returns>
        public bool IsHpEffect() {
            return CheckDamageType(new List<int>
            {
                1, 3, 5
            });
        }

        /// <summary>
        /// ダメージの[タイプ]がMPに関するものか
        /// </summary>
        /// <returns></returns>
        public bool IsMpEffect() {
            return CheckDamageType(new List<int>
            {
                2, 4, 6
            });
        }

        /// <summary>
        /// ダメージの[タイプ]が[HP吸収]か[MP吸収]か
        /// </summary>
        /// <returns></returns>
        public bool IsDrain() {
            return CheckDamageType(new List<int>
            {
                5, 6
            });
        }

        /// <summary>
        /// ダメージの[タイプ]が[HP回復]か
        /// </summary>
        /// <returns></returns>
        public bool IsHpRecover() {
            return Item.RecoverHp == 1;
        }

        /// <summary>
        /// ダメージの[タイプ]が[HP回復]か（使用者への影響）
        /// </summary>
        /// <returns></returns>
        public bool IsHpRecoverMyself() {
            return Item.RecoverHpMyself == 1;
        }

        /// <summary>
        /// ダメージの[タイプ]が[MP回復]か
        /// </summary>
        /// <returns></returns>
        public bool IsMpRecover() {
            return Item.RecoverMp == 1;
        }

        /// <summary>
        /// ダメージの[タイプ]が[MP回復]か（使用者への影響）
        /// </summary>
        /// <returns></returns>
        public bool IsMpRecoverMyself() {
            return Item.RecoverMpMyself == 1;
        }

        /// <summary>
        /// ダメージの[タイプ]が[TP回復]か
        /// </summary>
        /// <returns></returns>
        public bool IsTpRecover() {
            return Item.RecoverTp == 1;
        }

        /// <summary>
        /// [命中タイプ]が[必中]か
        /// </summary>
        /// <returns></returns>
        public bool IsCertainHit() {
            return Item?.HitType == HittypeCertain;
        }

        /// <summary>
        /// [命中タイプ]が[物理攻撃]か
        /// </summary>
        /// <returns></returns>
        public bool IsPhysical() {
            return Item?.HitType == HittypePhysical;
        }

        /// <summary>
        /// [命中タイプ]が[魔法攻撃]か
        /// </summary>
        /// <returns></returns>
        public bool IsMagical() {
            return Item?.HitType == HittypeMagical;
        }

        /// <summary>
        /// 行動が[攻撃]か
        /// </summary>
        /// <returns></returns>
        public bool IsAttack() {
            return Item?.ItemId == GameBattlerBase.AttackSkillId;
        }

        /// <summary>
        /// 行動が[防御]か
        /// </summary>
        /// <returns></returns>
        public bool IsGuard() {
            return Item?.ItemId == GameBattlerBase.GuardSkillId;
        }

        /// <summary>
        /// [魔法]スキルか
        /// </summary>
        /// <returns></returns>
        public bool IsMagicSkill() {
            if (IsSkill())
            {
                return Item?.STypeId == 1;
            }
            return false;
        }

        /// <summary>
        /// [範囲]に沿って、対象をランダムに決定
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void DecideRandomTarget() {
#else
        public async Task DecideRandomTarget() {
#endif
            GameBattler target = null;
            if (IsForDeadFriend())
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                target = FriendsUnit().RandomDeadTarget();
#else
                target = await FriendsUnit().RandomDeadTarget();
#endif
            else if (IsForAllFriend())
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                target = FriendsUnit().RandomAllTarget();
#else
                target = await FriendsUnit().RandomAllTarget();
#endif
            else if (IsForFriend())
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                target = FriendsUnit().RandomTarget();
#else
                target = await FriendsUnit().RandomTarget();
#endif
            else
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                target = OpponentsUnit().RandomTarget();
#else
                target = await OpponentsUnit().RandomTarget();
#endif

            if (target != null)
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _targetIndex = target.Index();
#else
                _targetIndex = await target.Index();
#endif
            else
                Clear();
        }

        /// <summary>
        /// 行動に[混乱]を設定
        /// </summary>
        public void SetConfusion() {
            SetAttack();
        }

        /// <summary>
        /// 準備(標準では[混乱]の設定しかしていない)
        /// </summary>
        public void Prepare() {
            if (Subject.IsConfused() && !_forcing) SetConfusion();
        }

        /// <summary>
        /// 行動可能か
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public bool IsValid() {
            return _forcing && Item != null || Subject.CanUse(Item);
        }
#else
        public async Task<bool> IsValid() {
            return _forcing && Item != null || await Subject.CanUse(Item);
        }
#endif

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public float Speed() {
            var agi = Subject.Agi;

            float speed = agi + Random.Range(0, (int) Math.Floor((decimal) 5 + agi / 4));
            if (Item != null) speed += Item.Speed;
            if (IsAttack()) speed += Subject.AttackSpeed();
            return speed;
        }

        /// <summary>
        /// 対象となり得るバトラーを配列で返す
        /// 使用者への影響については、対象とは別で処理されるべきもののため、ここでは含めない
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<GameBattler> MakeTargets() {
#else
        public async Task<List<GameBattler>> MakeTargets() {
#endif
            var targets = new List<GameBattler>();

            //混乱中
            if (!_forcing && Subject.IsConfused())
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                targets.Add(ConfusionTarget());
#else
                targets.Add(await ConfusionTarget());
#endif
            }
            //[範囲]が敵単体(複数回も含む)
            else if (IsForOpponent())
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                targets = TargetsForOpponents();
#else
                targets = await TargetsForOpponents();
#endif
            }
            //[範囲]が味方(自身も含む)
            else if (IsForFriend())
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                targets = TargetsForFriends();
#else
                targets = await TargetsForFriends();
#endif
            }
            //[範囲]が敵味方全体
            else if (IsForOpponentAndFriend())
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                foreach (var enemy in TargetsForOpponents()) targets.Add(enemy);
                foreach (var actor in TargetsForFriends()) targets.Add(actor);
#else
                foreach (var enemy in await TargetsForOpponents()) targets.Add(enemy);
                foreach (var actor in await TargetsForFriends()) targets.Add(actor);
#endif
            }

            return RepeatTargets(targets);
        }

        /// <summary>
        /// 対象となり得るバトラーを返す（使用者への影響）
        /// </summary>
        /// <returns></returns>
        public GameBattler MakeTargetMyself() {
            //使用者への影響
            if (IsForUser()) return Subject;
            return null;
        }

        /// <summary>
        /// 繰り返し行動の対象を配列で返す
        /// </summary>
        /// <param name="targets"></param>
        /// <returns></returns>
        public List<GameBattler> RepeatTargets(List<GameBattler> targets) {
            var repeatedTargets = new List<GameBattler>();
            var repeats = NumRepeats();
            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (target != null)
                    for (var j = 0; j < repeats; j++)
                        repeatedTargets.Add(target);
            }

            return repeatedTargets;
        }

        /// <summary>
        /// 混乱している場合の、対象バトラーを選んで返す
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public GameBattler ConfusionTarget() {
#else
        public async Task<GameBattler> ConfusionTarget() {
#endif
            switch (Subject.ConfusionLevel())
            {
                case 1:
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    return OpponentsUnit().RandomTarget();
#else
                    return await OpponentsUnit().RandomTarget();
#endif
                case 2:
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    if (Random.Range(0, 2) == 0) return OpponentsUnit().RandomTarget();
                    return FriendsUnit().RandomTarget();
#else
                    if (Random.Range(0, 2) == 0) return await OpponentsUnit().RandomTarget();
                    return await FriendsUnit().RandomTarget();
#endif
                default:
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    return FriendsUnit().RandomTarget();
#else
                    return await FriendsUnit().RandomTarget();
#endif
            }
        }

        /// <summary>
        /// 敵側のバトラーの配列を返す
        /// </summary>
        /// <returns></returns>

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<GameBattler> TargetsForOpponents() {
#else
        public async Task<List<GameBattler>> TargetsForOpponents() {
#endif
            var targets = new List<GameBattler>();
            var unit = OpponentsUnit();
            if (unit != null)
            {
                if (IsForRandom())
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    for (var i = 0; i < NumTargets(); i++) targets.Add(unit.RandomTarget());
#else
                    for (var i = 0; i < NumTargets(); i++) targets.Add(await unit.RandomTarget());
#endif
                }
                else if (IsForOne())
                {
                    if (_targetIndex < 0)
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        targets.Add(unit.RandomTarget());
#else
                        targets.Add(await unit.RandomTarget());
#endif
                    else
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        targets.Add(unit.SmoothTarget(_targetIndex));
#else
                        targets.Add(await unit.SmoothTarget(_targetIndex));
#endif
                }
                else
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    targets = unit.AliveMembers();
#else
                    targets = await unit.AliveMembers();
#endif
                }
            }

            return targets;
        }

        /// <summary>
        /// 味方側のバトラーの配列を返す
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<GameBattler> TargetsForFriends() {
#else
        public async Task<List<GameBattler>> TargetsForFriends() {
#endif
            var targets = new List<GameBattler>();

            var unit = FriendsUnit();

            if (IsForDeadFriend())
            {
                if (IsForOne())
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    targets.Add(unit.SmoothDeadTarget(_targetIndex));
#else
                    targets.Add(await unit.SmoothDeadTarget(_targetIndex));
#endif
                else
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    targets = unit.DeadMembers();
#else
                    targets = await unit.DeadMembers();
#endif
            }
            else if (IsForAllFriend())
            {
                if (IsForOne())
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    targets.Add(unit.SmoothAllTarget(_targetIndex));
#else
                    targets.Add(await unit.SmoothAllTarget(_targetIndex));
#endif
                else
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    targets = unit.Members();
#else
                    targets = await unit.Members();
#endif
            }
            else if (IsForOne())
            {
                if (_targetIndex < 0)
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    targets.Add(unit.RandomTarget());
#else
                    targets.Add(await unit.RandomTarget());
#endif
                else
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    targets.Add(unit.SmoothTarget(_targetIndex));
#else
                    targets.Add(await unit.SmoothTarget(_targetIndex));
#endif
            }
            else
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                targets = unit.AliveMembers();
#else
                targets = await unit.AliveMembers();
#endif
            }

            return targets;
        }

        /// <summary>
        /// 全ての対象に効果を適用して、総ダメージ量を返す
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public double Evaluate() {
#else
        public async Task<double> Evaluate() {
#endif
            double value = 0;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            ItemTargetCandidates().ForEach(target =>
#else
            (await ItemTargetCandidates()).ForEach(async target =>
#endif
            {
                var targetValue = EvaluateWithTarget(target);
                if (IsForAll())
                {
                    value += targetValue;
                }
                else if (targetValue > value)
                {
                    value = targetValue;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _targetIndex = ((GameEnemy) target).Index();
#else
                    _targetIndex = await ((GameEnemy) target).Index();
#endif
                }
            });
            value *= NumRepeats();
            if (value > 0) value += TforuUtility.MathRandom();

            return value;
        }

        /// <summary>
        /// 対象となり得るバトラーを配列で返す
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<GameBattler> ItemTargetCandidates() {
#else
        public async Task<List<GameBattler>> ItemTargetCandidates() {
#endif
            var ret = new List<GameBattler>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            if (!IsValid())
#else
            if (!await IsValid())
#endif
            {
            }
            else if (IsForOpponent())
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                ret.AddRange(OpponentsUnit().AliveMembers());
#else
                ret.AddRange(await OpponentsUnit().AliveMembers());
#endif
            }
            else if (IsForDeadFriend())
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                ret.AddRange(FriendsUnit().DeadMembers());
#else
                ret.AddRange(await FriendsUnit().DeadMembers());
#endif
            }
            else if (IsForAllFriend())
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                ret.AddRange(FriendsUnit().Members());
#else
                ret.AddRange(await FriendsUnit().Members());
#endif
            }
            else
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                ret.AddRange(FriendsUnit().AliveMembers());
#else
                ret.AddRange(await FriendsUnit().AliveMembers());
#endif
            }

            return ret;
        }

        /// <summary>
        /// 指定対象に効果を適用して、ダメージ量を返す
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public double EvaluateWithTarget(GameBattler target) {
            if (IsHpEffect())
            {
                var value = MakeDamageValue(target, false, false);
                if (IsForOpponent()) return value / Math.Max(target.Hp, 1);

                var recovery = Math.Min(-value, target.Mhp - target.Hp);
                return recovery / target.Mhp;
            }

            return 0;
        }

        /// <summary>
        /// 対象に行動を試験適用し、その結果は行動可能か
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool TestApply(GameBattler target) {
            return (IsForDeadFriend() == target.IsDead() || IsForAllFriend()) &&
                   (
                       DataManager.Self().GetGameParty().InBattle() ||
                       IsForOpponent() ||
                       IsHpRecover() && target.Hp < target.Mhp ||
                       IsMpRecover() && target.Mp < target.Mmp ||
                       HasItemAnyValidEffects(target)
                   );
        }

        /// <summary>
        /// 対象に行動を試験適用し、その結果は行動可能か（使用者への影響）
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool TestApplyMyself(GameBattler target) {
            return Subject == target && IsForUser() &&
                   (
                       DataManager.Self().GetGameParty().InBattle() ||
                       IsHpRecoverMyself() && target.Hp < target.Mhp ||
                       IsMpRecoverMyself() && target.Mp < target.Mmp ||
                       HasItemAnyValidEffectsMyself(target)
                   );
        }

        /// <summary>
        /// 指定対象が、なんらかの[使用効果]を発生させるか
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool HasItemAnyValidEffects(GameBattler target) {
            return Item.Effects.Any(effect => { return TestItemEffect(target, effect); });
        }

        /// <summary>
        /// 指定対象が、なんらかの[使用効果]を発生させるか（使用者への影響）
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool HasItemAnyValidEffectsMyself(GameBattler target) {
            return Item.EffectsMyself.Any(effect => { return TestItemEffect(target, effect); });
        }

        /// <summary>
        /// 対象に[使用効果]を試験適用し、その結果は実行可能か
        /// </summary>
        /// <param name="target"></param>
        /// <param name="effect"></param>
        /// <returns></returns>
        public bool TestItemEffect(GameBattler target, TraitCommonDataModel effect) {
            switch (effect.categoryId)
            {
                case EffectAddState:
                    return !target.IsStateAffected(effect.traitsId);
                case EffectRemoveState:
                    return target.IsStateAffected(effect.traitsId);
                case EffectAddBuff:
                    return !target.IsMaxBuffAffected(effect.traitsId);
                case EffectAddDebuff:
                    return !target.IsMaxDebuffAffected(effect.traitsId);
                case EffectRemoveBuff:
                    return target.IsBuffAffected(effect.traitsId);
                case EffectRemoveDebuff:
                    return target.IsDebuffAffected(effect.traitsId);
                case EffectLearnSkill:
                    if (!target.IsActor()) return false;
                    var actor = (GameActor) target;
                    return !actor.IsLearnedSkill(effect.traitsId);
                default:
                    return true;
            }
        }

        /// <summary>
        /// 指定対象の[反撃率]を返す
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public double ItemCnt(GameBattler target) {
            if (IsPhysical() && target.CanMove())
                return target.Cnt;
            return 0;
        }

        /// <summary>
        /// 指定対象の[魔法反射率]を返す
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public double ItemMrf(GameBattler target) {
            if (IsMagical())
                return target.Mrf;
            return 0;
        }

        /// <summary>
        /// 指定対象の[命中率]を返す
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public double ItemHit(GameBattler target) {
            if (IsPhysical())
                return Item.SuccessRate * 0.01 * Subject.Hit;
            return Item.SuccessRate * 0.01;
        }

        /// <summary>
        /// 指定対象の[回避率]を返す
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public double ItemEva(GameBattler target) {
            if (IsPhysical())
                return target.Eva;
            if (IsMagical())
                return target.Mev;
            return 0;
        }

        /// <summary>
        /// 指定対象の[会心率]を返す
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public double ItemCri(GameBattler target) {
            var criticalRate = Subject.Cri;
            return Item.DamageData.critical == 1 ? criticalRate * (1 - target.Cev) : 0;
        }

        /// <summary>
        /// 指定対象へ結果( Game_ActionResult )の適用
        /// </summary>
        /// <param name="target"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void Apply(GameBattler target) {
#else
        public async Task Apply(GameBattler target) {
#endif
            //targetの行動結果を初期化する
            target.Result = new GameActionResult();
            var result = target.Result;

            //targetとsubjectが異なる場合には、subject側の行動結果も初期化する
            if (target != Subject)
                Subject.ClearResult();

            result.Used = TestApply(target);
            result.Missed = result.Used && TforuUtility.MathRandom() >= ItemHit(target);
            result.Evaded = !result.Missed && TforuUtility.MathRandom() < ItemEva(target);
            result.Physical = IsPhysical();
            result.Drain = IsDrain();

            bool isScope = (Item.Scope != 0);

            if (result.IsHit())
            {
                if (Item.DamageType > 0 && GameStateHandler.IsBattle())
                {
                    result.Critical = TforuUtility.MathRandom() < ItemCri(target);
                    var value = MakeDamageValue(target, result.Critical, false);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    ExecuteDamage(target, value);
#else
                    await ExecuteDamage(target, value);
#endif
                }

                //ダメージタイプから、HP回復とMP回復が無くなり、各ダメージタイプと併用設定が可能となった
                //そのため、ダメージ計算とは別で、回復関連の処理を行う
                if (IsHpRecover() && isScope)
                {
                    if (Item.IsSkill())
                    {
                        var skillMasterData = DataManager.Self().GetSkillCustomDataModel(Item.ItemId);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        ItemEffectRecoverHp(target, skillMasterData.targetEffect.heal.hp);
#else
                        await ItemEffectRecoverHp(target, skillMasterData.targetEffect.heal.hp);
#endif
                    }
                    else
                    {
                        var itemMasterData = DataManager.Self().GetItemDataModel(Item.ItemId);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        ItemEffectRecoverHpItem(target, itemMasterData.targetEffect.heal.hp);
#else
                        await ItemEffectRecoverHpItem(target, itemMasterData.targetEffect.heal.hp);
#endif
                    }

                }

                if (IsMpRecover() && isScope)
                {
                    if (Item.IsSkill())
                    {
                        var skillMasterData = DataManager.Self().GetSkillCustomDataModel(Item.ItemId);
                        ItemEffectRecoverMp(target, skillMasterData.targetEffect.heal.mp);
                    }
                    else
                    {
                        var itemMasterData = DataManager.Self().GetItemDataModel(Item.ItemId);
                        ItemEffectRecoverMpItem(target, itemMasterData.targetEffect.heal.mp);
                    }

                }

                if (IsTpRecover() && GameStateHandler.IsBattle() && isScope)
                {
                    if (Item.IsSkill())
                    {
                        var skillMasterData = DataManager.Self().GetSkillCustomDataModel(Item.ItemId);
                        ItemEffectGainTp(target, skillMasterData.targetEffect.heal.tp);
                    }
                    else
                    {
                        var itemMasterData = DataManager.Self().GetItemDataModel(Item.ItemId);
                        ItemEffectGainTpItem(target, itemMasterData.targetEffect.heal.tp);
                    }
                }

                //スキルやアイテムに設定されている使用効果
                foreach (var effect in Item.Effects)
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    ApplyItemEffect(target, effect);
#else
                    await ApplyItemEffect(target, effect);
#endif
                }

                //MVには ステート付与に[通常攻撃]というものが存在したが、Uniteでは省略されている
                //MVではステート付与に[通常攻撃]がある場合に、使用者の特徴にあるステート付与の情報も見ている
                //そのため、攻撃タイプが[通常攻撃]の場合に、使用者に付与されているステート付与も見るように修正
                //HPダメージタイプ時のみ効果を付与する　U312
                if ( (Item.DamageType == 1) && (Item.AttackType == 0) )
                {
                    foreach (var effect in Subject.AllTraits())
                    {
                        if (effect.categoryId == GameBattlerBase.TraitAttackState)
                        {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                            ItemEffectAddAttackState(target, effect);
#else
                            await ItemEffectAddAttackState(target, effect);
#endif
                        }
                    }
                }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                ApplyItemUserEffect(target);
#else
                await ApplyItemUserEffect(target);
#endif
            }

            //直前の行動を保存
            var lastData = DataManager.Self().GetRuntimeSaveDataModel().runtimePartyDataModel.lastData;
            if (Subject.IsActor())
            {
                //アクターの場合
                if (Item.IsSkill())
                {
                    lastData.skillId = Item.SkillData.SerialNumber;
                    lastData.itemId = 0;
                }
                else if (Item.IsItem())
                {
                    lastData.skillId = 0;
                    lastData.itemId = Item.ItemData.SerialNumber;
                }
                else
                {
                    lastData.skillId = 0;
                    lastData.itemId = 0;
                }
                string actorId = ((GameActor) Subject).ActorId;
                lastData.actionActorId = DataManager.Self().GetActorDataModel(actorId).SerialNumber;
                if (target is GameEnemy)
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    lastData.targetEnemyIndex = ((GameEnemy) target).Index();
#else
                    lastData.targetEnemyIndex = await ((GameEnemy) target).Index();
#endif
                }
                else if (target is GameActor)
                {
                    actorId = ((GameActor) target).ActorId;
                    lastData.targetActorId = DataManager.Self().GetActorDataModel(actorId).SerialNumber;
                }
            }
            else
            {
                //敵の場合
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                lastData.actionEnemyIndex = ((GameEnemy) Subject).Index();
#else
                lastData.actionEnemyIndex = await ((GameEnemy) Subject).Index();
#endif
                if (target is GameEnemy)
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    lastData.targetEnemyIndex = ((GameEnemy) target).Index();
#else
                    lastData.targetEnemyIndex = await ((GameEnemy) target).Index();
#endif
                }
                else if (target is GameActor)
                {
                    string actorId = ((GameActor) target).ActorId;
                    lastData.targetActorId = DataManager.Self().GetActorDataModel(actorId).SerialNumber;
                }
            }
        }

        /// <summary>
        /// 指定対象へ結果( Game_ActionResult )の適用（使用者への影響）
        /// </summary>
        /// <param name="target"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ApplyMyself(GameBattler target) {
#else
        public async Task ApplyMyself(GameBattler target) {
#endif
            //targetの行動結果を初期化する
            target.Result = new GameActionResult();
            var result = target.Result;

            //targetとsubjectが異なる場合には、subject側の行動結果も初期化する
            if (target != Subject)
                Subject.ClearResult();

            result.Used = TestApplyMyself(target);
            //使用者への影響には命中率等が存在しないため、必中扱いで通す
            result.Missed = false;
            result.Evaded = false;
            result.Physical = IsPhysical();
            result.Drain = false;

            if (Item.DamageTypeMyself > 0)
            {
                result.Critical = TforuUtility.MathRandom() < ItemCri(target);
                var value = MakeDamageValue(target, result.Critical, true);
                //使用者への影響の場合、HPダメージの有効無効を切り替えることが出来ない
                //計算した結果、ダメージが無い場合には、処理しないようにする
                if (value != 0)
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    ExecuteHpDamage(target, (int) value);
#else
                    await ExecuteHpDamage(target, (int) value);
#endif
            }

            //ダメージタイプから、HP回復とMP回復が無くなり、各ダメージタイプと併用設定が可能となった
            //そのため、ダメージ計算とは別で、回復関連の処理を行う
            if (IsHpRecoverMyself())
            {
                if (Item.IsSkill())
                {
                    var skillMasterData = DataManager.Self().GetSkillCustomDataModel(Item.ItemId);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    ItemEffectRecoverHp(target, skillMasterData.userEffect.heal.hp);
#else
                    await ItemEffectRecoverHp(target, skillMasterData.userEffect.heal.hp);
#endif
                }
                else
                {
                    var itemMasterData = DataManager.Self().GetItemDataModel(Item.ItemId);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    ItemEffectRecoverHpItem(target, itemMasterData.userEffect.heal.hp);
#else
                    await ItemEffectRecoverHpItem(target, itemMasterData.userEffect.heal.hp);
#endif
                }
            }

            if (IsMpRecoverMyself())
            {
                if (Item.IsSkill())
                {
                    var skillMasterData = DataManager.Self().GetSkillCustomDataModel(Item.ItemId);
                    ItemEffectRecoverMp(target, skillMasterData.userEffect.heal.mp);
                }
                else
                {
                    var itemMasterData = DataManager.Self().GetItemDataModel(Item.ItemId);
                    ItemEffectRecoverMpItem(target, itemMasterData.userEffect.heal.mp);
                }
            }

            foreach (var effect in Item.EffectsMyself)
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                ApplyItemEffect(target, effect);
#else
                await ApplyItemEffect(target, effect);
#endif
            }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            int gainTp = ApplyItemUserEffect(target);
#else
            int gainTp = await ApplyItemUserEffect(target);
#endif
            if (gainTp > 0)
            {
                MakeSuccess(target);
            }
        }

        /// <summary>
        /// コモンイベントが設定されている場合に、コモンイベントをキューに溜める
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void SetCommonEvent(bool isForUser) {
#else
        public async Task SetCommonEvent(bool isForUser) {
#endif
            //スキルやアイテムに設定されている使用効果
            if (!isForUser)
            {
                foreach (var effect in Item.Effects)
                {
                    if (EffectCommonEvent == ConvertUniteData.SetEffectCode(effect))
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        ApplyItemEffect(null, effect);
#else
                        await ApplyItemEffect(null, effect);
#endif
                }
            }
            else
            {
                foreach (var effect in Item.EffectsMyself)
                {
                    if (EffectCommonEvent == ConvertUniteData.SetEffectCode(effect))
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        ApplyItemEffect(null, effect);
#else
                        await ApplyItemEffect(null, effect);
#endif
                }
            }

            //使用者に対する使用効果
            //コモンイベントの実行
            if (_effect != null)
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                ItemEffectCommonEvent(null, _effect);
#else
                await ItemEffectCommonEvent(null, _effect);
#endif
                _effect = null;
            }
        }


        /// <summary>
        /// 指定対象へのダメージ量を計算して返す
        /// </summary>
        /// <param name="target"></param>
        /// <param name="critical"></param>
        /// <returns></returns>
        public double MakeDamageValue(GameBattler target, bool critical, bool isMyself) {
            //ダメージの計算式を適用し、ダメージ量を取得する
            var baseValue = EvalDamageFormula(target, isMyself);
            //指定対象への[属性]の効果率を乗算する
            var value = (double) baseValue * CalcElementRate(target);
            //var value = (double) baseValue;
            //命中タイプが物理攻撃の場合には、物理ダメージ率を乗算する
            if (IsPhysical()) value *= target.Pdr;
            //命中タイプが魔法攻撃の場合には、魔法ダメージ率を乗算する
            if (IsMagical()) value *= target.Mdr;
            //ダメージがマイナス=回復の場合には、回復率を乗算する
            if (baseValue < 0) value *= target.Rec;
            //クリティカルが発生している場合には、会心分、ダメージを乗算する
            if (critical) value = ApplyCritical(value);

            //ダメージを分散する
            if (!isMyself)
                value = ApplyVariance(value, Item.DamageData.variance);
            else
                value = ApplyVariance(value, Item.DamageDataMyself.variance);

            //ダメージの最小値、最大値を適用する
            if (!isMyself)
            {
                double min = Item.DamageData.valueMin != -1 ? Item.DamageData.valueMin : -1;
                double max = Item.DamageData.valueMax != -1 ? Item.DamageData.valueMax : -1;
                if (min != -1 && value < min) value = min;
                if (max != -1 && value > max) value = max;
            }

            //防御している場合にダメージを減らす
            value = ApplyGuard(value, target);
            //最終ダメージを四捨五入して決定する
            value = (int) Math.Round(value);
            return value;
        }

        /// <summary>
        /// [ダメージ]の[計算式]を適用し、ダメージ量を返す
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private int EvalDamageFormula(GameBattler target, bool isMyself) {
            //使用者
            var a = Subject;
            //対象
            var b = target;
            //ダメージタイプがHP回復、MP回復の場合は-1、その他の場合は1
            var list = new List<int>(2) { 3, 4 };
            var sign = list.Contains(Item.DamageType) ? -1 : 1;
            //ダメージ計算式を用いてダメージ計算する
            if (!isMyself)
            {
                var value = (int) Math.Max(Eval(Item.DamageData.formula, a, b), 0) * sign;
                return value;
            }
            else
            {
                var value = (int) Math.Max(Eval(Item.DamageDataMyself.formula, a, b), 0) * sign;
                return value;
            }
        }

        /// <summary>
        /// 指定ダメージで[会心]攻撃
        /// </summary>
        /// <param name="damage"></param>
        /// <returns></returns>
        private static double ApplyCritical(double damage) {
            return damage * 3;
        }

        /// <summary>
        /// 指定ダメージに対して[分散度]を適用したダメージを返す
        /// </summary>
        /// <param name="damage"></param>
        /// <param name="variance"></param>
        /// <returns></returns>
        private double ApplyVariance(double damage, int variance) {
            if (variance == 0) return damage;
            var amp = Math.Floor(Math.Max(Math.Abs(damage) * variance / 100, 0));
            var v = TforuUtility.MathRandom() * (amp + 1) + TforuUtility.MathRandom() * (amp + 1) - amp;
            return damage >= 0 ? damage + v : damage - v;
        }

        /// <summary>
        /// 指定ダメージを対象バトラーが防御し、防御分を減らしたダメージを返す
        /// </summary>
        /// <param name="damage"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private double ApplyGuard(double damage, GameBattler target) {
            var guard = target.Grd;
            return damage / (damage > 0 && target.IsGuard() ? 2 * guard : 1);
        }

        /// <summary>
        /// 指定対象にダメージを与える
        /// </summary>
        /// <param name="target"></param>
        /// <param name="damage"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ExecuteDamage(GameBattler target, double damage) {
#else
        public async Task ExecuteDamage(GameBattler target, double damage) {
#endif
            if (damage == 0)
            {
                target.Result.Critical = false;
            }
            if (IsHpEffect())
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                ExecuteHpDamage(target, (int) damage);
#else
                await ExecuteHpDamage(target, (int) damage);
#endif
            }
            if (IsMpEffect())
            {
                ExecuteMpDamage(target, (int) damage);
            }
        }

        /// <summary>
        /// 指定対象にHPダメージを与える
        /// </summary>
        /// <param name="target"></param>
        /// <param name="value"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void ExecuteHpDamage(GameBattler target, int value) {
#else
        private async Task ExecuteHpDamage(GameBattler target, int value) {
#endif
            if (IsDrain()) value = Math.Min(target.Hp, value);
            MakeSuccess(target);
            target.GainHp(-value);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            if (value > 0) target.OnDamage(value);
#else
            if (value > 0) await target.OnDamage(value);
#endif

            GainDrainedHp(value);
        }

        /// <summary>
        /// 指定対象にMPダメージを与える
        /// </summary>
        /// <param name="target"></param>
        /// <param name="value"></param>
        private void ExecuteMpDamage(GameBattler target, int value) {
            if (value != 0) MakeSuccess(target);

            target.GainMp(-value);
            GainDrainedMp(value);
        }

        /// <summary>
        /// 敵から吸収したHPを返す
        /// </summary>
        /// <param name="value"></param>
        private void GainDrainedHp(int value) {
            if (!IsDrain()) return;

            var gainTarget = Subject;
            if (_reflectionTarget != null) gainTarget = _reflectionTarget;

            gainTarget.GainHp(value);
        }

        /// <summary>
        /// 敵から吸収したMPを返す
        /// </summary>
        /// <param name="value"></param>
        private void GainDrainedMp(int value) {
            if (!IsDrain()) return;

            var gainTarget = Subject;
            if (_reflectionTarget != null) gainTarget = _reflectionTarget;

            gainTarget.GainMp(value);
        }

        /// <summary>
        /// 指定対象にエフェクトを適用
        /// </summary>
        /// <param name="target"></param>
        /// <param name="effect"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ApplyItemEffect(GameBattler target, TraitCommonDataModel effect) {
#else
        public async Task ApplyItemEffect(GameBattler target, TraitCommonDataModel effect) {
#endif
            var code = ConvertUniteData.SetEffectCode(effect);

            switch (code)
            {
                case EffectAddState:
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    ItemEffectAddState(target, effect);
#else
                    await ItemEffectAddState(target, effect);
#endif
                    break;
                case EffectRemoveState:
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    ItemEffectRemoveState(target, effect);
#else
                    await ItemEffectRemoveState(target, effect);
#endif
                    break;
                case EffectAddBuff:
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    ItemEffectAddBuff(target, effect);
#else
                    await ItemEffectAddBuff(target, effect);
#endif
                    break;
                case EffectAddDebuff:
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    ItemEffectAddDebuff(target, effect);
#else
                    await ItemEffectAddDebuff(target, effect);
#endif
                    break;
                case EffectRemoveBuff:
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    ItemEffectRemoveBuff(target, effect);
#else
                    await ItemEffectRemoveBuff(target, effect);
#endif
                    break;
                case EffectRemoveDebuff:
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    ItemEffectRemoveDebuff(target, effect);
#else
                    await ItemEffectRemoveDebuff(target, effect);
#endif
                    break;
                case EffectSpecial:
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    ItemEffectSpecial(target, effect);
#else
                    await ItemEffectSpecial(target, effect);
#endif
                    break;
                case EffectGrow:
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    ItemEffectGrow(target, effect);
#else
                    await ItemEffectGrow(target, effect);
#endif
                    break;
                case EffectLearnSkill:
                    ItemEffectLearnSkill(target, effect);
                    break;
                case EffectCommonEvent:
                    if (target == null)
                        _effect = effect;
                    break;
            }
        }

        /// <summary>
        /// MVでは指定対象に[HP回復]の[使用効果]を加えるメソッドであったが
        /// Uniteでは対象者への効果にHP回復が設定されていた場合の処理を記載する
        /// </summary>
        /// <param name="target"></param>
        /// <param name="effect"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ItemEffectRecoverHp(GameBattler target, SkillCustomDataModel.ItemEffectHealParam effect) {
#else
        public async Task ItemEffectRecoverHp(GameBattler target, SkillCustomDataModel.ItemEffectHealParam effect) {
#endif
            var value = CalculationSkill(target, effect, HealType.Hp);
            var floored = (int) Math.Floor(value);
            if (floored != 0)
            {
                target.GainHp(floored);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                target.Refresh();
#else
                await target.Refresh();
#endif
                MakeSuccess(target);
            }
        }

        /// <summary>
        /// MVでは指定対象に[MP回復]の[使用効果]を加えるメソッドであったが
        /// Uniteでは対象者への効果にMP回復が設定されていた場合の処理を記載する
        /// </summary>
        /// <param name="target"></param>
        /// <param name="effect"></param>
        public void ItemEffectRecoverMp(GameBattler target, SkillCustomDataModel.ItemEffectHealParam effect) {
            var value = CalculationSkill(target, effect, HealType.Mp);

            var floored = (int) Math.Floor(value);
            if (floored != 0)
            {
                target.GainMp(floored);
                MakeSuccess(target);
            }
        }

        /// <summary>
        /// MVでは指定対象に[TP付与]の[使用効果]を加えるメソッドであったが
        /// Uniteでは対象者への効果にTP回復が設定されていた場合の処理を記載する
        /// </summary>
        /// <param name="target"></param>
        /// <param name="effect"></param>
        public void ItemEffectGainTp(GameBattler target, SkillCustomDataModel.ItemEffectHealParam effect) {
            var value = CalculationSkill(target, effect, HealType.Tp);
            var floored = (int) Math.Floor(value);
            if (floored != 0)
            {
                target.GainTp(floored);
                MakeSuccess(target);
            }
        }

        /// <summary>
        /// 指定対象に[使用効果]を加える
        /// </summary>
        /// <param name="target"></param>
        /// <param name="effect"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ItemEffectAddState(GameBattler target, TraitCommonDataModel effect) {
            ItemEffectAddNormalState(target, effect);
        }
#else
        public async Task ItemEffectAddState(GameBattler target, TraitCommonDataModel effect) {
            await ItemEffectAddNormalState(target, effect);
        }
#endif

        /// <summary>
        /// 指定対象に攻撃の[使用効果]を加える
        /// </summary>
        /// <param name="target"></param>
        /// <param name="effect"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ItemEffectAddAttackState(GameBattler target, TraitCommonDataModel effect) {
#else
        public async Task ItemEffectAddAttackState(GameBattler target, TraitCommonDataModel effect) {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            Subject.AttackStates().ForEach(stateId =>
#else
            foreach (var stateId in Subject.AttackStates())
#endif
            {
                double chance = effect.value / 100f;
                chance *= target.StateRate(stateId);
                chance *= Subject.AttackStatesRate(stateId);
                chance *= LukEffectRate(target);
                if (TforuUtility.MathRandom() < chance)
                {
                    var id = target.GetStateIdByNumber(stateId);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    target.AddState(id);
#else
                    await target.AddState(id);
#endif
                    MakeSuccess(target);
                }
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            });
#else
            }
#endif
        }

        /// <summary>
        /// 指定対象に通常の[使用効果]を加える
        /// </summary>
        /// <param name="target"></param>
        /// <param name="effect"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ItemEffectAddNormalState(GameBattler target, TraitCommonDataModel effect) {
#else
        public async Task ItemEffectAddNormalState(GameBattler target, TraitCommonDataModel effect) {
#endif
            double chance = effect.value / 100.0f;
            if (!IsCertainHit())
            {
                //以下はステートの配列番号
                chance *= target.StateRate(effect.effectId);
                chance *= LukEffectRate(target);
            }

            if (TforuUtility.MathRandom() < chance)
            {
                var id = target.GetStateIdByNumber(effect.effectId);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                target.AddState(id);
#else
                await target.AddState(id);
#endif
                MakeSuccess(target);
            }
        }

        /// <summary>
        /// 指定対象に[ステート解除]の[使用効果]を加える
        /// </summary>
        /// <param name="target"></param>
        /// <param name="effect"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ItemEffectRemoveState(GameBattler target, TraitCommonDataModel effect) {
#else
        public async Task ItemEffectRemoveState(GameBattler target, TraitCommonDataModel effect) {
#endif
            var chance = effect.value / 100.0f;
            if (TforuUtility.MathRandom() < chance)
            {
                var id = target.GetStateIdByNumber(effect.effectId);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                target.RemoveState(id);
#else
                await target.RemoveState(id);
#endif
                MakeSuccess(target);
            }
        }

        /// <summary>
        /// 指定対象に[強化]の[使用効果]を加える
        /// </summary>
        /// <param name="target"></param>
        /// <param name="effect"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ItemEffectAddBuff(GameBattler target, TraitCommonDataModel effect) {
            target.AddBuff(effect.effectId, effect.value);
            MakeSuccess(target);
        }
#else
        public async Task ItemEffectAddBuff(GameBattler target, TraitCommonDataModel effect) {
            await target.AddBuff(effect.effectId, effect.value);
            MakeSuccess(target);
        }
#endif

        /// <summary>
        /// 指定対象に[弱体]の[使用効果]を加える
        /// </summary>
        /// <param name="target"></param>
        /// <param name="effect"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ItemEffectAddDebuff(GameBattler target, TraitCommonDataModel effect) {
#else
        public async Task ItemEffectAddDebuff(GameBattler target, TraitCommonDataModel effect) {
#endif
            var chance = target.DebuffRate(effect.effectId) * LukEffectRate(target);
            if (TforuUtility.MathRandom() < chance)
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                target.AddDeBuff(effect.effectId, effect.value);
#else
                await target.AddDeBuff(effect.effectId, effect.value);
#endif
                MakeSuccess(target);
            }
        }

        /// <summary>
        /// 指定対象に[強化の解除]の[使用効果]を加える
        /// </summary>
        /// <param name="target"></param>
        /// <param name="effect"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ItemEffectRemoveBuff(GameBattler target, TraitCommonDataModel effect) {
#else
        public async Task ItemEffectRemoveBuff(GameBattler target, TraitCommonDataModel effect) {
#endif
            if (target.IsBuffAffected(effect.effectId))
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                target.RemoveBuff(effect.effectId);
#else
                await target.RemoveBuff(effect.effectId);
#endif
                MakeSuccess(target);
            }
        }

        /// <summary>
        /// 指定対象に[弱体の解除]の[使用効果]を加える
        /// </summary>
        /// <param name="target"></param>
        /// <param name="effect"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ItemEffectRemoveDebuff(GameBattler target, TraitCommonDataModel effect) {
#else
        public async Task ItemEffectRemoveDebuff(GameBattler target, TraitCommonDataModel effect) {
#endif
            if (target.IsDebuffAffected(effect.effectId))
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                target.RemoveBuff(effect.effectId);
#else
                await target.RemoveBuff(effect.effectId);
#endif
                MakeSuccess(target);
            }
        }

        /// <summary>
        /// 指定対象に[特殊効果]の[使用効果]を加える
        /// </summary>
        /// <param name="target"></param>
        /// <param name="effect"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ItemEffectSpecial(GameBattler target, TraitCommonDataModel effect) {
#else
        public async Task ItemEffectSpecial(GameBattler target, TraitCommonDataModel effect) {
#endif
            if (effect.effectId == SpecialEffectEscape)
            {
                target.Escape();
                MakeSuccess(target);

                //Actorが逃走した場合には、再描画を行う
                if (target is GameActor)
                {
                    //BattleManager.GetSpriteSet().CreateActors();
                    if(GameStateHandler.IsBattle())
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        BattleManager.GetSpriteSet().UpdateActorsPosition();
#else
                        await BattleManager.GetSpriteSet().UpdateActorsPosition();
#endif
                }
            }
        }

        /// <summary>
        /// 指定対象に[成長]の[使用効果]を加える
        /// </summary>
        /// <param name="target"></param>
        /// <param name="effect"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ItemEffectGrow(GameBattler target, TraitCommonDataModel effect) {
            target.AddParam(effect.effectId, effect.value);
            MakeSuccess(target);
        }
#else
        public async Task ItemEffectGrow(GameBattler target, TraitCommonDataModel effect) {
            await target.AddParam(effect.effectId, effect.value);
            MakeSuccess(target);
        }
#endif

        /// <summary>
        /// 指定対象に[スキル習得]の[使用効果]を加える
        /// </summary>
        /// <param name="target"></param>
        /// <param name="effect"></param>
        public void ItemEffectLearnSkill(GameBattler target, TraitCommonDataModel effect) {
            if (target.IsActor())
            {
                var actor = (GameActor) target;
                //U261 UIDでスキルをコール
                actor.LearnSkill(effect.effectUID);
                MakeSuccess(target);
            }
        }

        /// <summary>
        /// 指定対象に[コモンイベント]の[使用効果]を加える
        /// </summary>
        /// <param name="target"></param>
        /// <param name="effect"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ItemEffectCommonEvent(GameBattler target, TraitCommonDataModel effect) {
#else
        public async Task ItemEffectCommonEvent(GameBattler target, TraitCommonDataModel effect) {
#endif
#if true
            var eventManagementService = new EventManagementService();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var commonEvent = eventManagementService.LoadEventCommon();
#else
            var commonEvent = await eventManagementService.LoadEventCommon();
#endif

            EventCommonDataModel commonEventData = null;

            //U261 UIDでデータを検索する
            commonEventData = commonEvent.Find(e=> e.eventId == effect.effectUID);
            if (commonEventData == null) return;

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            EventDataModel eventData = eventManagementService.LoadEventById(commonEventData.eventId);
#else
            EventDataModel eventData = await eventManagementService.LoadEventById(commonEventData.eventId);
#endif
            if (eventData == null) return;

            if (GameStateHandler.IsBattle())
            {
                var battleEventData = new EventBattleDataModel(commonEventData.eventId,
                    new List<EventBattleDataModel.EventBattlePage>());
                var gameTroop = DataManager.Self().GetGameTroop();

                if (!gameTroop.IsRunningCommon)
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    gameTroop.EventManager.Add(new BattleEventCommandChainLauncher());
#else
                    var battleEventCommandChainLauncher = new BattleEventCommandChainLauncher();
                    await battleEventCommandChainLauncher.InitForConstructor();
                    gameTroop.EventManager.Add(battleEventCommandChainLauncher);
#endif
                    gameTroop.EventManager[^1].Init();
                    gameTroop.EventManager[^1].SetEventData(eventData, battleEventData);
                }
                gameTroop.IsRunningCommon = true;
            }
            else if (GameStateHandler.IsMap())
            {
                var eventDataModel = EventDataModel.CreateDefault();
                var eventMapDataModel = new EventMapDataModel();
                eventDataModel.eventCommands = new List<EventDataModel.EventCommand>();
                var eventCommand = new EventDataModel.EventCommand((int) EventEnum.EVENT_CODE_FLOW_JUMP_COMMON,
                    new List<string>() { commonEventData.eventId }, new List<EventDataModel.EventCommandMoveRoute>(), 0);
                eventDataModel.eventCommands.Add(eventCommand);
                eventMapDataModel.eventId = eventDataModel.id;
                eventMapDataModel.pages = new List<EventMapDataModel.EventMapPage>();
                var page = EventMapDataModel.EventMapPage.CreateDefault();

                page.condition.image.enabled = 2;
                eventMapDataModel.pages.Add(page);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                MapEventExecutionController.Instance.TryToTraitsToCommonEvent(eventMapDataModel, eventDataModel);
#else
                await MapEventExecutionController.Instance.TryToTraitsToCommonEvent(eventMapDataModel, eventDataModel);
#endif
            }
#endif
        }

        /// <summary>
        /// 行動結果用に指定対象の行動に成功したフラグを立てる
        /// </summary>
        /// <param name="target"></param>
        private static void MakeSuccess(GameBattler target) {
            target.Result.Success = true;
        }

        /// <summary>
        /// 指定対象にアイテムの効果を適用
        /// </summary>
        /// <param name="target"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public int ApplyItemUserEffect(GameBattler target) {
#else
        public async Task<int> ApplyItemUserEffect(GameBattler target) {
#endif
            var value = (int) Math.Floor(Item.TpGain * Subject.Tcr);
            Subject.GainSilentTp(value);

            //アクターを対象に効果を発揮した場合には、装備が変更になっている可能性があるため更新する
            if (target is GameActor)
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                ResetEquipment((GameActor) target);
#else
                await ResetEquipment((GameActor) target);
#endif

            return value;
        }

        /// <summary>
        /// 装備状況の更新
        /// </summary>
        /// <param name="actor"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void ResetEquipment(GameActor actor) {
#else
        private async Task ResetEquipment(GameActor actor) {
#endif
            //アクターが装備するものを頭から順にチェックしなおし
            SystemSettingDataModel systemSettingDataModel = DataManager.Self().GetSystemDataModel();

            for (var j = 0; j < actor.Actor.equips.Count; j++)
            {
                //装備種別を取得
                SystemSettingDataModel.EquipType equipType = null;
                for (int j2 = 0; j2 < systemSettingDataModel.equipTypes.Count; j2++)
                    if (systemSettingDataModel.equipTypes[j2].id == actor.Actor.equips[j].equipType)
                    {
                        equipType = systemSettingDataModel.equipTypes[j2];
                        break;
                    }

                //装備が封印されているかどうか
                bool ret = actor.IsEquipTypeSealed(j);
                if (ret)
                {
                    //装備を外す
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    ItemManager.RemoveEquipment(actor.Actor, equipType, j);
#else
                    await ItemManager.RemoveEquipment(actor.Actor, equipType, j);
#endif
                }
            }
            //GameActorへ反映
            //装備封印以外の要因で、装備を外すことになった場合は、以下の処理内で外れる
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            actor.ResetActorData();
#else
            await actor.ResetActorData();
#endif
        }

        /// <summary>
        /// 指定対象の[幸運]の適用率を返す
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public double LukEffectRate(GameBattler target) {
            return Math.Max(1.0 + (Subject.Luk - target.Luk) * 0.001, 0.0);
        }

        /// <summary>
        /// [使用効果]に含まれる[コモンイベント]を抽出して $GameTemp(Game_Temp) に保持
        /// </summary>
        public void ApplyGlobal() {
        }


        //==========================================================
        // 以下はUnite固有処理
        //==========================================================

        /// <summary>
        /// メニューから実行する
        /// 使用効果にCommonEventが含まれているかどうかを返却する
        /// </summary>
        /// <param name="effect"></param>
        /// <returns></returns>
        public bool IsEffectCommonEvent(TraitCommonDataModel effect) {
            var code = ConvertUniteData.SetEffectCode(effect);
            if (code == EffectCommonEvent)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// HP回復、MP回復、TP回復用処理
        /// </summary>
        /// <param name="target"></param>
        /// <param name="effect"></param>
        /// <returns></returns>

        public double CalculationSkill(GameBattler target, SkillCustomDataModel.ItemEffectHealParam effect, HealType healType) {
            //回復の計算式を適用
            double baseValue = 0;

            if (effect.calc.enabled == 1)
                //計算式	計算式入力（排他）
                // U367 対象者とターゲットを入替 (a.Mak) a : 使用者　b : 対象者
                baseValue = Eval(effect.calc.value, Subject , target);
            else if (effect.perMax.enabled == 1)
            {
                //最大値に対する割合 チェックボックス／数値入力（排他）
                if (healType == HealType.Hp)
                    baseValue = target.Mhp * (effect.perMax.value / 100f);
                else if (healType == HealType.Mp)
                    baseValue = target.Mmp * (effect.perMax.value / 100f);
                else if (healType == HealType.Tp)
                    baseValue = 100 * (effect.perMax.value / 100f);
            }
            else
                //回復量の固定値 チェックボックス／数値入力（排他）
                baseValue = effect.fix.value;

            var value = (double) baseValue;

            //回復効果率を乗算する
            value *= target.Rec;

            //分散
            int variance = effect.perMax.distributeEnabled == 1 ? effect.perMax.distribute : 0;
            value = ApplyVariance(value, variance);

            //最終回復量を四捨五入して決定する
            value = (int) Math.Round(value);

            //回復量の最大値で丸める
            double max = effect.perMax.maxEnabled == 1 ? effect.perMax.max : -1;
            if (max != -1 && value > max) value = max;

            return value;
        }

        /// <summary>
        /// MVでは指定対象に[HP回復]の[使用効果]を加えるメソッドであったが
        /// Uniteでは対象者への効果にHP回復が設定されていた場合の処理を記載する
        /// </summary>
        /// <param name="target"></param>
        /// <param name="effect"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ItemEffectRecoverHpItem(GameBattler target, ItemDataModel.ItemEffectHealParam effect) {
#else
        public async Task ItemEffectRecoverHpItem(GameBattler target, ItemDataModel.ItemEffectHealParam effect) {
#endif
            var value = CalculationItem(target, effect, HealType.Hp);
            value *= Subject.Pha;

            var floored = (int) Math.Floor(value);
            if (floored != 0)
            {
                target.GainHp(floored);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                target.Refresh();
#else
                await target.Refresh();
#endif
                MakeSuccess(target);
            }
        }

        /// <summary>
        /// MVでは指定対象に[MP回復]の[使用効果]を加えるメソッドであったが
        /// Uniteでは対象者への効果にHP回復が設定されていた場合の処理を記載する
        /// </summary>
        /// <param name="target"></param>
        /// <param name="effect"></param>
        public void ItemEffectRecoverMpItem(GameBattler target, ItemDataModel.ItemEffectHealParam effect) {
            var value = CalculationItem(target, effect, HealType.Mp);
            value *= Subject.Pha;

            var floored = (int) Math.Floor(value);
            if (floored != 0)
            {
                target.GainMp(floored);
                MakeSuccess(target);
            }
        }

        /// <summary>
        /// MVでは指定対象に[TP回復]の[使用効果]を加えるメソッドであったが
        /// Uniteでは対象者への効果にHP回復が設定されていた場合の処理を記載する
        /// </summary>
        /// <param name="target"></param>
        /// <param name="effect"></param>
        public void ItemEffectGainTpItem(GameBattler target, ItemDataModel.ItemEffectHealParam effect) {
            var value = CalculationItem(target, effect, HealType.Tp);
            value *= Subject.Pha;

            var floored = (int) Math.Floor(value);
            if (floored != 0)
            {
                target.GainTp(floored);
                MakeSuccess(target);
            }
        }

        /// <summary>
        /// HP回復、MP回復、TP回復用処理
        /// </summary>
        /// <param name="target"></param>
        /// <param name="effect"></param>
        /// <returns></returns>
        public double CalculationItem(GameBattler target, ItemDataModel.ItemEffectHealParam effect, HealType healType) {
            //回復の計算式を適用
            double baseValue = 0;

            if (effect.calc.enabled == 1)
                //計算式	計算式入力（排他）
                baseValue = Eval(effect.calc.value, target, Subject);
            else if (effect.perMax.enabled == 1)
            {
                //最大値に対する割合 チェックボックス／数値入力（排他）
                if (healType == HealType.Hp)
                    baseValue = target.Mhp * (effect.perMax.value / 100f);
                else if (healType == HealType.Mp)
                    baseValue = target.Mmp * (effect.perMax.value / 100f);
                else if (healType == HealType.Tp)
                    baseValue = 100 * (effect.perMax.value / 100f);
            }
            else
                //回復量の固定値 チェックボックス／数値入力（排他）
                baseValue = effect.fix.value;

            var value = (double) baseValue;

            //回復効果率を乗算する
            value *= target.Rec;

            //分散
            int variance = effect.perMax.distributeEnabled == 1 ? effect.perMax.distribute : 0;
            value = ApplyVariance(value, variance);

            //最終回復量を四捨五入して決定する
            value = (int) Math.Round(value);

            //回復量の最大値で丸める
            double max = effect.perMax.maxEnabled == 1 ? effect.perMax.max : -1;
            if (max != -1 && value > max) value = max;

            return value;
        }

        /// <summary>
        /// 使用者への影響があるかどうか
        /// </summary>
        /// <returns></returns>
        private bool CheckItemScopeUse() {
            return Item.UserScope == 1;
        }


        //========================================================
        // 以下属性計算
        //========================================================

        private List<float> _elementList;

        private float CalcElementRate(GameBattler target) {
            var system = DataManager.Self().GetSystemDataModel();

            //攻撃属性
            List<int> elements;
            if (Item.DamageElementId == 1)
                elements = Subject.AttackElements();
            else
                elements = new List<int>() { Item.DamageElementId };

            //最終ダメージを属性ごとに計算
            List<float> elementRate = new List<float>();

            for (int i = 0; i < elements.Count; i++)
            {
                float rate = 1.0f;

                //攻撃側の属性->被攻撃側の属性
                //攻撃側の属性
                //属性にはSerialNo - 1の値が設定されているため、補完して取得する
                Element attackElement = null;
                for (int i2 = 0; i2 < system.elements.Count; i2++)
                    if (system.elements[i2].SerialNumber == elements[i] + 1)
                    {
                        attackElement = system.elements[i2];
                        break;
                    }

                if (attackElement != null)
                {
                    //被攻撃側の属性を検索して適用する
                    //優位属性
                    foreach (var advantage in attackElement.advantageous)
                    {
                        foreach (var targetElement in target.MyElement)
                        {
                            if (advantage.element == targetElement)
                            {
                                rate = rate * (1.0f + (1.0f - advantage.magnification / 1000f));
                            }
                        }
                    }

                    //劣位属性
                    foreach (var disadvantage in attackElement.disadvantage)
                        foreach (var targetElement in target.MyElement)
                            if (disadvantage.element == targetElement)
                                rate = rate * (1.0f + (1.0f - disadvantage.magnification / 1000f));
                }

                //被攻撃側の属性
                foreach (var defenceElementNo in target.MyElement)
                {
                    //属性にはSerialNo - 1の値が設定されているため、補完して取得する
                    Element defenceElement = null;
                    for (int i2 = 0; i2 < system.elements.Count; i2++)
                        if (system.elements[i2].SerialNumber == defenceElementNo + 1)
                        {
                            defenceElement = system.elements[i2];
                            break;
                        }

                    //攻撃属性を検索して適用する
                    //優位属性
                    foreach (var advantage in defenceElement.advantageous)
                        if (advantage.element == elements[i])
                            rate = rate * advantage.magnification / 1000f;

                    //劣位属性
                    foreach (var disadvantage in defenceElement.disadvantage)
                        if (disadvantage.element == elements[i])
                            rate = rate * disadvantage.magnification / 1000f;
                }
                //被攻撃側に設定されている特徴[属性有効度]の適用
                rate = rate * target.ElementRate(elements[i]);

                //最終の計算結果を保持
                elementRate.Add(rate);
            }

            //最終ダメージの最大値を返却
            bool flg = false;
            float rateMax = -1f;
            foreach (var rate in elementRate)
                if (rateMax < rate)
                {
                    flg = true;
                    rateMax = rate;
                }

            //フラグがfalseなら
            if (!flg)
            {
                rateMax = 1f;
            }
            
            return rateMax;
        }

        //======================================================================
        // 以下はEvalに関連する処理
        //======================================================================

        /// <summary>
        /// Unite用のEval処理
        /// </summary>
        /// <param name="formula"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static double Eval(string formula, GameBattler a, GameBattler b) {
            //変換テーブル
            var prms = new Dictionary<string, double>
            {
                {"a.hp", a.Hp},   {"a.mp", a.Mp},   {"a.tp", a.Tp},   {"a.mhp", a.Mhp}, {"a.mmp", a.Mmp}, {"a.atk", a.Atk},
                {"a.def", a.Def}, {"a.mat", a.Mat}, {"a.mdf", a.Mdf}, {"a.agi", a.Agi}, {"a.luk", a.Luk}, {"a.hit", a.Hit},
                {"a.eva", a.Eva}, {"a.cri", a.Cri}, {"a.cev", a.Cev}, {"a.mev", a.Mev}, {"a.mrf", a.Mrf}, {"a.cnt", a.Cnt},
                {"a.hrg", a.Hrg}, {"a.mrg", a.Mrg}, {"a.trg", a.Trg}, {"a.tgr", a.Tgr}, {"a.grd", a.Grd}, {"a.rec", a.Rec},
                {"a.pha", a.Pha}, {"a.mcr", a.Mcr}, {"a.tcr", a.Tcr}, {"a.pdr", a.Pdr}, {"a.mdr", a.Mdr}, {"a.fdr", a.Fdr}, 
                {"a.exr", a.Exr}, 
                {"b.hp", b.Hp},   {"b.mp", b.Mp},   {"b.tp", b.Tp},   {"b.mhp", b.Mhp}, {"b.mmp", b.Mmp}, {"b.atk", b.Atk},
                {"b.def", b.Def}, {"b.mat", b.Mat}, {"b.mdf", b.Mdf}, {"b.agi", b.Agi}, {"b.luk", b.Luk}, {"b.hit", b.Hit},
                {"b.eva", b.Eva}, {"b.cri", b.Cri}, {"b.cev", b.Cev}, {"b.mev", b.Mev}, {"b.mrf", b.Mrf}, {"b.cnt", b.Cnt},
                {"b.hrg", b.Hrg}, {"b.mrg", b.Mrg}, {"b.trg", b.Trg}, {"b.tgr", b.Tgr}, {"b.grd", b.Grd}, {"b.rec", b.Rec},
                {"b.pha", b.Pha}, {"b.mcr", b.Mcr}, {"b.tcr", b.Tcr}, {"b.pdr", b.Pdr}, {"b.mdr", b.Mdr}, {"b.fdr", b.Fdr}, 
                {"b.exr", b.Exr}
            };

            //変換テーブルに含まれている文字列を、実際のパラメータに置換する
            foreach (var kv in prms) formula = formula.ToLower().Replace(kv.Key, kv.Value.ToString());

            // 変数変換
            formula = VariableConvert(formula);

            //1文字ずつのArrayList配列に変換する
            var list = new List<char>(formula);
            list.RemoveAll(x => x == ' ');
            //char[]の型に変換する
            var c = list.ToArray();
            //Node型に変換する
            var node = Parse(c);
            //evalする
            double value = Eval2(node);
            return Double.IsNaN(value) ? 0 : value;
        }

        /// <summary>
        /// 変数変換処理
        /// </summary>
        private static string VariableConvert(string formula) {
            var variables = DataManager.Self().GetRuntimeSaveDataModel().variables;

            while (true)
            {
                // 変数記号を検索
                var last = formula.IndexOf("]");
                if (last == -1) break;
                var first = formula.LastIndexOf("v[", last);
                if (first == -1) break;

                // 変数ブロックを取り出す
                var variableStr = formula.Substring(first, last - first + 1);
                var math = variableStr.Replace("v[", "").Replace("]", "");
                double num = Eval2(Parse(math.ToArray()));

                // 不正な値は0とする
                if (double.IsNaN(num) || num - 1 < 0 || variables.data.Count <= num - 1)
                    formula = formula.Replace(variableStr, "0");
                else if (variables.data.Count > num)
                    formula = formula.Replace(variableStr, variables.data[(int) num - 1]);
            }

            return formula;
        }

        /// <summary>
        /// eval処理
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static double Eval2(Node node) {
            List<string> ns;
            List<char> os;

            LexicalAnalysis(node.formula, out ns, out os);

            var numbers = new List<double>();

            var child = 0;
            for (var i = 0; i < ns.Count; i++)
            {
                var num = 0.0;


                switch (ns[i])
                {
                    case "#":
                        num = Eval2(node.childs[child++]);
                        break;
                    default:
                        double.TryParse(ns[i], out num);
                        break;
                }

                numbers.Add(num);
            }


            for (var i = 0; i < os.Count;)
            {
                if (numbers.Count <= i + 1) break;
                switch (os[i])
                {
                    case '*':
                    {
                        var left = numbers[i];
                        var right = numbers[i + 1];
                        numbers[i] = left * right;
                        numbers.RemoveAt(i + 1);
                        os.RemoveAt(i);
                    }
                        break;
                    case '/':
                    {
                        var left = numbers[i];
                        var right = numbers[i + 1];
                        numbers[i] = left / right;
                        numbers.RemoveAt(i + 1);
                        os.RemoveAt(i);
                    }
                        break;
                    default:
                        i++;
                        break;
                }
            }

            if (numbers.Count >= 1)
            {
                var total = numbers[0];
                {
                    for (var i = 0; i < os.Count; i++)
                        switch (os[i])
                        {
                            case '+':
                                total += numbers[i + 1];
                                break;
                            case '-':
                                total -= numbers[i + 1];
                                break;
                        }
                }

                return total;
            }

            return 0;
        }


        /// <summary>
        /// Nodeへのパース処理
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private static Node Parse(char[] c) {
            var root = new Node();
            var target = root;

            for (var i = 0; i < c.Length; i++)
                switch (c[i])
                {
                    case '(':
                    {
                        target.formula += "#";


                        var node = new Node();
                        target.Add(node);
                        target = node;
                    }
                        break;
                    case ')':
                    {
                        target = target.parent;
                    }
                        break;
                    default:
                        target.formula += c[i];
                        break;
                }


            return root;
        }

        /// <summary>
        /// 字句解析処理
        /// </summary>
        /// <param name="str"></param>
        /// <param name="ns"></param>
        /// <param name="os"></param>
        private static void LexicalAnalysis(string str, out List<string> ns, out List<char> os) {
            ns = new List<string>();
            os = new List<char>();

            var text = "";
            for (var i = 0; i < str.Length; i++)
                switch (str[i])
                {
                    case '+':
                    case '-':
                    case '*':
                    case '/':
                        ns.Add(text);
                        os.Add(str[i]);
                        text = "";
                        break;
                    default:
                        if (IsNumber(str[i]) || str[i] == '.')
                        {
                            text += str[i];
                            if (i == str.Length - 1)
                            {
                                ns.Add(text);
                                text = "";
                            }
                        }

                        break;
                }
        }

        /// <summary>
        /// 数値かどうかの判定
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private static bool IsNumber(char c) {
            return char.IsDigit(c) || c == 'x' || c == 'X' || c == '#';
        }

        public void UseItemPlaySe(int type) {
            if (IsHpRecover() || IsMpRecover() || IsTpRecover() || IsHpRecoverMyself() || IsMpRecoverMyself())
            {
                SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_SE, DataManager.Self().GetSystemDataModel().soundSetting.recovery);
            }
            else
            {
                if(type == 0)
                    SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_SE, DataManager.Self().GetSystemDataModel().soundSetting.useItem);
                else
                    SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_SE, DataManager.Self().GetSystemDataModel().soundSetting.useSkill);
            }
            
            SoundManager.Self().PlaySe();

        }
    }

    /// <summary>
    /// Node
    /// eval処理用
    /// </summary>
    public class Node
    {
        public List<Node> childs  = new List<Node>();
        public string     formula = "";


        public Node parent { get; private set; }

        public void Add(Node node) {
            node.parent = this;
            childs.Add(node);
        }
    }
}