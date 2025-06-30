using RPGMaker.Codebase.Runtime.Common;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Map.Component.Character
{
    public class ActorOnMap : CharacterOnMap
    {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ExecUpdateTimeHandler() {
            UpdateTimeHandler();
        }
#else
        public async Task ExecUpdateTimeHandler() {
            await UpdateTimeHandlerAsync();
        }
#endif
    }
}