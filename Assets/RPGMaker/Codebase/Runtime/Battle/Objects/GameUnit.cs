using RPGMaker.Codebase.Runtime.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Battle.Objects
{
    /// <summary>
    /// 戦闘時のグループを扱うクラス
    /// </summary>
    public abstract class GameUnit
    {
        /// <summary>
        /// 戦闘中か
        /// </summary>
        private bool _inBattle = false;

        /// <summary>
        /// 戦闘中か
        /// </summary>
        /// <returns></returns>
        public bool InBattle() {
            return _inBattle;
        }

        /// <summary>
        /// 戦闘中のバトラー生死問わず全て配列で返す
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public virtual List<GameBattler> Members() {
#else
        public virtual async Task<List<GameBattler>> Members() {
            await UniteTask.Delay(0);
#endif
            return new List<GameBattler>();
        }

        /// <summary>
        /// 生存しているバトラーを配列で返す
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public virtual List<GameBattler> AliveMembers() {
            return Members().FindAll(member => member.IsAlive());
        }
#else
        public virtual async Task<List<GameBattler>> AliveMembers() {
            return (await Members()).FindAll(member => member.IsAlive());
        }
#endif

        /// <summary>
        /// 死亡しているバトラーを配列で返す
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public virtual List<GameBattler> DeadMembers() {
            return Members().FindAll(member => { return member.IsDead(); });
        }
#else
        public virtual async Task<List<GameBattler>> DeadMembers() {
            return (await Members()).FindAll(member => { return member.IsDead(); });
        }
#endif

        /// <summary>
        /// 動ける(死亡や麻痺などでない)バトラーを配列で返す
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public List<GameBattler> MovableMembers() {
            return Members().FindAll(member => member.CanMove());
        }
#else
        public async Task<List<GameBattler>> MovableMembers() {
            return (await Members()).FindAll(member => member.CanMove());
        }
#endif

        /// <summary>
        /// アクションを取り消す
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ClearActions() {
            Members().ForEach(member => member.ClearActions());
        }
#else
        public async Task ClearActions() {
            (await Members()).ForEach(member => member.ClearActions());
        }
#endif

        /// <summary>
        /// ユニットの素早さを返す
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public double Agility() {
#else
        public async Task<double> Agility() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var members = Members();
#else
            var members = await Members();
#endif
            if (members.Count == 0) return 1;

            var sum = members.Aggregate(0, (r, member) => { return r + member.Agi; });
            return sum / members.Count;
        }

        /// <summary>
        /// 生きているメンバーの[狙われ率]の合計を返す
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public double TgrSum() {
            return AliveMembers().Aggregate((double) 0, (r, member) => { return r + member.Tgr; });
        }
#else
        public async Task<double> TgrSum() {
            return (await AliveMembers()).Aggregate((double) 0, (r, member) => { return r + member.Tgr; });
        }
#endif

        /// <summary>
        /// 含まれるバトラーからランダムに1体を返す
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public GameBattler RandomTarget() {
#else
        public async Task<GameBattler> RandomTarget() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var tgrRand = TforuUtility.MathRandom() * TgrSum();
#else
            var tgrRand = TforuUtility.MathRandom() * await TgrSum();
#endif
            GameBattler target = null;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            AliveMembers().ForEach(member =>
#else
            (await AliveMembers()).ForEach(member =>
#endif
            {
                tgrRand -= member.Tgr;
                if (tgrRand <= 0 && target == null) target = member;
            });
            return target;
        }

        /// <summary>
        /// 死亡したバトラーからランダムに1体を返す
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public GameBattler RandomDeadTarget() {
#else
        public async Task<GameBattler> RandomDeadTarget() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var members = DeadMembers();
#else
            var members = await DeadMembers();
#endif
            if (members.Count == 0) return null;

            return members[(int) Math.Floor(TforuUtility.MathRandom() * members.Count)];
        }

        /// <summary>
        /// 生存,死亡を問わず、バトラーからランダムに1体を返す
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public GameBattler RandomAllTarget() {
#else
        public async Task<GameBattler> RandomAllTarget() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var tgrRand = TforuUtility.MathRandom() * TgrSum();
#else
            var tgrRand = TforuUtility.MathRandom() * await TgrSum();
#endif
            GameBattler target = null;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            Members().ForEach(member =>
#else
            (await Members()).ForEach(member =>
#endif
            {
                tgrRand -= member.Tgr;
                if (tgrRand <= 0 && target == null) target = member;
            });
            return target;
        }

        /// <summary>
        /// 指定番号のメンバーを優先して生きているメンバーを返す
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public GameBattler SmoothTarget(int index) {
#else
        public async Task<GameBattler> SmoothTarget(int index) {
#endif
            if (index < 0) index = 0;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            if (index >= Members().Count) index = Members().Count - 1;
#else
            var members = await Members();
            if (index >= members.Count) index = members.Count - 1;
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var member = Members()[index];
            return member != null && member.IsAlive() ? member : AliveMembers().ElementAtOrDefault(0);
#else
            var member = members[index];
            return member != null && member.IsAlive() ? member : (await AliveMembers()).ElementAtOrDefault(0);
#endif
        }

        /// <summary>
        /// 指定番号のメンバーを優先して死亡しているメンバーを返す
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public GameBattler SmoothDeadTarget(int index) {
#else
        public async Task<GameBattler> SmoothDeadTarget(int index) {
#endif
            if (index < 0) index = 0;

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var member = Members()[index];
            return member != null && member.IsDead() ? member : DeadMembers().ElementAtOrDefault(0);
#else
            var member = (await Members())[index];
            return member != null && member.IsDead() ? member : (await DeadMembers()).ElementAtOrDefault(0);
#endif
        }

        /// <summary>
        /// 指定番号のメンバーを返却する
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public GameBattler SmoothAllTarget(int index) {
#else
        public async Task<GameBattler> SmoothAllTarget(int index) {
#endif
            if (index < 0) index = 0;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            if (index >= Members().Count) index = Members().Count - 1;
#else
            var members = await Members();
            if (index >= members.Count) index = members.Count - 1;
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var member = Members()[index];
            return member != null ? member : AliveMembers().ElementAtOrDefault(0);
#else
            var member = members[index];
            return member != null ? member : (await AliveMembers()).ElementAtOrDefault(0);
#endif
        }

        /// <summary>
        /// アクションの結果を取り消す
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ClearResults() {
            Members().ForEach(member => member.ClearResult());
        }
#else
        public async Task ClearResults() {
            (await Members()).ForEach(member => member.ClearResult());
        }
#endif

        /// <summary>
        /// 戦闘開始時に呼ばれるハンドラ
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void OnBattleStart() {
            Members().ForEach(member => { member.OnBattleStart(); });
            _inBattle = true;
        }
#else
        public async Task OnBattleStart() {
            (await Members()).ForEach(member => { member.OnBattleStart(); });
            _inBattle = true;
        }
#endif

        /// <summary>
        /// 戦闘終了時に呼ばれるハンドラ
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void OnBattleEnd() {
#else
        public async Task OnBattleEnd() {
#endif
            _inBattle = false;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            Members().ForEach(member => { member.OnBattleEnd(); });
#else
            foreach (var member in await Members())
            {
                await member.OnBattleEnd();
            }
#endif
        }

        /// <summary>
        /// 戦闘行動を作成する
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void MakeActions() {
            Members().ForEach(member => { member.MakeActions(); });
        }
#else
        public async Task MakeActions() {
            foreach (var member in await Members()){
                await member.MakeActions();
            }
        }
#endif

        /// <summary>
        /// 指定されたバトラーを選択する
        /// </summary>
        /// <param name="activeMember"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void Select(GameBattler activeMember) {
#else
        public async Task Select(GameBattler activeMember) {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            Members().ForEach(member =>
#else
            (await Members()).ForEach(member =>
#endif
            {
                if (member == activeMember)
                    member.Select();
                else
                    member.Deselect();
            });
        }

        /// <summary>
        /// 全バトラーが死亡したか
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public bool IsAllDead() {
            return AliveMembers().Count == 0;
        }
#else
        public async Task<bool> IsAllDead() {
            return (await AliveMembers()).Count == 0;
        }
#endif

        /// <summary>
        /// 身代わりのバトラーを返す
        /// </summary>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public GameBattler SubstituteBattler() {
#else
        public async Task<GameBattler> SubstituteBattler() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var members = Members();
#else
            var members = await Members();
#endif
            for (var i = 0; i < members.Count; i++)
                if (members[i].IsSubstitute())
                    return members[i];

            return null;
        }
    }
}