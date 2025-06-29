using UnityEngine;

namespace RPGMaker.Codebase.Runtime.PostEffectOnRender
{
    [ExecuteInEditMode, ImageEffectAllowedInSceneView]
    public class Fill : PostEffectBase
    {
        private static readonly int Color1 = Shader.PropertyToID("_Color1");
        private static readonly int Color2 = Shader.PropertyToID("_Color2");
        private static readonly int Horizontal = Shader.PropertyToID("_Horizontal");
        private static readonly int Vertical = Shader.PropertyToID("_Vertical");
        private static readonly int Inverse = Shader.PropertyToID("_Inverse");
        private static readonly int Blend = Shader.PropertyToID("_Blend");
        private static readonly int MixRatio = Shader.PropertyToID("_MixRatio");

        private bool _isGradient;
        private Vector4 _color1;
        private Vector4 _color2;
        private float _blend;
        private int _dir;
        private float _currentMixRatio;

        protected override void Awake()
        {
            ShaderName = "Hidden/Fill";
            base.Awake();
        }

        protected override void SetInitialParams()
        {
            _isGradient = Param[0] == 1;

            if (!_isGradient)
            {
                _color1 = new Vector4(
                    Param[1] / 255.0f,
                    Param[2] / 255.0f,
                    Param[3] / 255.0f,
                    Param[4] / 255.0f
                );
            }
            else
            {
                _color1 = new Vector4(
                    Param[6] / 255.0f,
                    Param[7] / 255.0f,
                    Param[8] / 255.0f,
                    Param[9] / 255.0f
                );
            }

            _color2 = new Vector4(
                Param[10] / 255.0f,
                Param[11] / 255.0f,
                Param[12] / 255.0f,
                Param[13] / 255.0f
            );

            _dir = Param[5];
            _blend = Param[14];
            _currentMixRatio = 0.0f;
        }

        public override void UpdateParams(float ratio)
        {
            _currentMixRatio = ratio;
        }

        public override void ApplyParams()
        {
            if (Material == null) return;
            if (Param == null) return;

            if (!_isGradient)
            {
                Material.SetVector(Color1, _color1);
                Material.SetVector(Color2, _color1);
            }
            else
            {
                Material.SetVector(Color1, _color1);
                Material.SetVector(Color2, _color2);
            }


            switch (_dir)
            {
                case 0:
                    Material.SetFloat(Horizontal, 0);
                    Material.SetFloat(Vertical, 1);
                    Material.SetFloat(Inverse, 0);
                    break;
                case 1:
                    Material.SetFloat(Horizontal, 1);
                    Material.SetFloat(Vertical, 0);
                    Material.SetFloat(Inverse, 0);
                    break;
                case 2:
                    Material.SetFloat(Horizontal, 1);
                    Material.SetFloat(Vertical, 1);
                    Material.SetFloat(Inverse, 0);
                    break;
                case 3:
                    Material.SetFloat(Horizontal, 1);
                    Material.SetFloat(Vertical, 1);
                    Material.SetFloat(Inverse, 1);
                    break;
                default:
                    Material.SetFloat(Horizontal, 0);
                    Material.SetFloat(Vertical, 1);
                    Material.SetFloat(Inverse, 0);
                    break;
            }

            Material.SetFloat(Blend, _blend);
            Material.SetFloat(MixRatio, _currentMixRatio);
        }
    }
}