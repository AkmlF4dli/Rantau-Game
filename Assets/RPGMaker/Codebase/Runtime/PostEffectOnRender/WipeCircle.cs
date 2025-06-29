using UnityEngine;

namespace RPGMaker.Codebase.Runtime.PostEffectOnRender
{
    [ExecuteInEditMode, ImageEffectAllowedInSceneView]
    public class WipeCircle : PostEffectBase
    {
        private static readonly int Transition = Shader.PropertyToID("_Transition");

        private float _currentWipeSize;

        protected override void Awake()
        {
            ShaderName = "Hidden/WipeCircle";
            base.Awake();
        }

        protected override void SetInitialParams()
        {
            _currentWipeSize = 0.0f;
        }

        public override void UpdateParams(float ratio)
        {
            _currentWipeSize = ratio;
        }

        public override void ApplyParams()
        {
            if (Material == null) return;

            Material.SetFloat(Transition, _currentWipeSize);
        }
    }
}