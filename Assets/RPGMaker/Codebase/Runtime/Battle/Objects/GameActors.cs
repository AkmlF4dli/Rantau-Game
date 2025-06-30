using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime;
using RPGMaker.Codebase.Runtime.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Battle.Objects
{
    /// <summary>
    /// Game_Actor をまとめて扱えるようにしたクラス。ほぼ、$dataActorsと同じ
    /// </summary>
    public class GameActors
    {
        /// <summary>
        /// アクターの配列
        /// </summary>
        private readonly Dictionary<string, GameActor> _data = new Dictionary<string, GameActor>();

        /// <summary>
        /// 指定IDのアクターを返す
        /// </summary>
        /// <param name="actor"></param>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public GameActor Actor(RuntimeActorDataModel actor) {
#else
        public async Task<GameActor> Actor(RuntimeActorDataModel actor) {
#endif
            if (DataManager.Self().GetActorDataModel(actor.actorId) == null) return null;

            //既に存在するGameActorから探す
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var actors = DataManager.Self().GetGameParty().Members();
#else
            var actors = await DataManager.Self().GetGameParty().Members();
#endif
            for (var i = 0; i < actors.Count; i++)
                if (actor.actorId == actors[i].Id)
                    return (GameActor) actors[i];

            //U417 バトル中に読み込み直しは、バトル中フラグが消えるのでなしで…
            if (DataManager.Self().GetGameParty().InBattle()) return null;

            //万が一ここに到達したら、データが正常に読み込めていないので、読み込みなおす
            DataManager.Self().ReloadGameParty();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            actors = DataManager.Self().GetGameParty().Members();
#else
            actors = await DataManager.Self().GetGameParty().Members();
#endif
            for (var i = 0; i < actors.Count; i++)
                if (actor.actorId == actors[i].Id)
                    return (GameActor) actors[i];

            //これでもNGであれば、nullを返却
            return null;
        }
    }
}