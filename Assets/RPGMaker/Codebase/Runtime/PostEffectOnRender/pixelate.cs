using UnityEngine;

namespace RPGMaker.Codebase.Runtime.PostEffectOnRender
{
    [ExecuteInEditMode, ImageEffectAllowedInSceneView]
    public class Pixelate : PostEffectBase
    {
        private static readonly int NumPixelX = Shader.PropertyToID("_NumPixelX");
        private static readonly int NumPixelY = Shader.PropertyToID("_NumPixelY");

        private int _initialPixelNumX;
        private int _initialPixelNumY;
        private int _maxPixelNumX;
        private int _maxPixelNumY;
        private int _currentPixelNumX;
        private int _currentPixelNumY;

        protected override void Awake()
        {
            ShaderName = "Hidden/Pixelate";
            base.Awake();
        }

        protected override void SetInitialParams()
        {
            _initialPixelNumX = Screen.width;
            _initialPixelNumY = Screen.height;
            var intensity = Param[0];
            intensity = intensity == 0 ? 1 : intensity + 1;
            _maxPixelNumX = Screen.width / intensity;
            _maxPixelNumY = Screen.height / intensity;
        }

        public override void UpdateParams(float ratio)
        {
            _currentPixelNumX = (int)Mathf.Lerp(_initialPixelNumX, _maxPixelNumX, ratio);
            _currentPixelNumY = (int)Mathf.Lerp(_initialPixelNumY, _maxPixelNumY, ratio);
        }

        public override void ApplyParams()
        {
            if (Material == null) return;
            if (Param == null) return;

            Material.SetInt(NumPixelX, _currentPixelNumX);
            Material.SetInt(NumPixelY, _currentPixelNumY);
        }
    }
}