using RPGMaker.Codebase.Runtime.Battle.Sprites;
using UnityEngine;

namespace RPGMaker.Codebase.Runtime.Battle
{
    public class BattleScenePreviewTest : MonoBehaviour
    {
        private void Start() {
            var spritesetBattle = gameObject.transform.Find("SpriteSetBattle").GetComponent<SpritesetBattle>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            spritesetBattle.RenderBattleScenePreview("0a452403-4c97-4083-9fd0-e1c088890abc");
#else
            _ = spritesetBattle.RenderBattleScenePreview("0a452403-4c97-4083-9fd0-e1c088890abc");
#endif
        }
    }
}