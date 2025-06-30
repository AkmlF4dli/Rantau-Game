using RPGMaker.Codebase.CoreSystem.Helper;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace RPGMaker.Codebase.Runtime.Common
{
    public class SceneBase : Stage
    {
        public enum INPUT_KEY_TYPE
        {
            NONE,
            UP,
            DOWN,
            LEFT,
            RIGHT,
            Z,
            X,
            ESCAPE,
            LEFTCLICK
        }

        private Color _fadeColor  = Color.black;
        private Color _flashColor = Color.black;


        private Color _tintColor = Color.black;

        protected bool  active;
        protected float alpha = 1.0f;
        protected int   fadeDuration;
        protected int   fadeSign;
        protected Image fadeSprite;
        protected float flashAlpha = 1.0f;
        protected int   flashDuration;
        protected int   flashSign;
        protected Image flashSprite;


        protected ParticleSystem rainParticle;
        protected ParticleSystem snowParticle;
        protected float          tintAlpha = 1.0f;
        protected int            tintDuration;
        protected int            tintSign;
        protected Image          tintSprite;

        public SoundManager soundManager { get; } = null;

        protected bool IsActive() {
            return active;
        }

        protected virtual void Start() {
            if (rainParticle == null)
                try
                {
                    rainParticle = GameObject.FindWithTag("Rain").GetComponent<ParticleSystem>();
                    rainParticle.gameObject.SetActive(false);
                }
                catch (Exception)
                {
                }

            if (snowParticle == null)
                try
                {
                    snowParticle = GameObject.FindWithTag("Snow").GetComponent<ParticleSystem>();
                    snowParticle.gameObject.SetActive(false);
                }
                catch (Exception)
                {
                }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            Init();
#else
            _ = Init();
#endif
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        protected virtual void Init() {
#else
        protected virtual async Task Init() {
            await UniteTask.Delay(0);
#endif
            active = true;
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public virtual void Update() {
        }
#else
        public virtual async Task Update() {
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public virtual void Stop() {
#else
        public virtual async Task Stop() {
            await UniteTask.Delay(0);
#endif
            active = false;
        }

        protected bool IsBusy() {
            return fadeDuration > 0;
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        protected void StartFadeIn(int duration, bool white) {
#else
        protected async Task StartFadeIn(int duration, bool white) {
#endif
            alpha = 1.0f;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            CreateFadeSprite(white);
#else
            await CreateFadeSprite(white);
#endif
            fadeSign = 1;
            fadeDuration = duration;
            fadeSprite.color = new Color(_fadeColor.r, _fadeColor.g, _fadeColor.b, 1.0f);
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        protected void StartFadeOut(int duration, bool white) {
#else
        protected async Task StartFadeOut(int duration, bool white) {
#endif
            alpha = 0.0f;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            CreateFadeSprite(white);
#else
            await CreateFadeSprite(white);
#endif
            fadeSign = -1;
            fadeDuration = duration;
            fadeSprite.color = new Color(_fadeColor.r, _fadeColor.g, _fadeColor.b, 0.0f);
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        protected void CreateFadeSprite(bool white) {
#else
        protected async Task CreateFadeSprite(bool white) {
#endif
            if (!fadeSprite)
            {
                var canvas = GameObject.FindWithTag("Canvas");
                var gameObject = new GameObject();
                gameObject.name = "FadeObject";
                gameObject.transform.SetParent(canvas.transform);
                fadeSprite = gameObject.AddComponent<Image>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var sp = UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(
#else
                var sp = await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(
#endif
                    "Assets/RPGMaker/Storage/Images/System/fadebackground" + ".png");
                fadeSprite.sprite = sp;
                gameObject.transform.localScale = new Vector3(1000f, 1000f, 1f);
                gameObject.transform.localPosition = new Vector3(gameObject.transform.localPosition.x,
                    gameObject.transform.localPosition.y, -10f);
            }

            if (white)
                _fadeColor = Color.white;
            else
                _fadeColor = Color.black;
        }

        protected void UpdateFade() {
            if (fadeDuration > 0)
            {
                var d = fadeDuration;
                if (fadeSign > 0)
                {
                    alpha -= fadeSprite.color.a / d;
                    fadeSprite.color = new Color(_fadeColor.r, _fadeColor.g, _fadeColor.b, alpha);
                }
                else
                {
                    alpha += (1.0f + fadeSprite.color.a) / d;
                    fadeSprite.color = new Color(_fadeColor.r, _fadeColor.g, _fadeColor.b, alpha);
                }

                fadeDuration--;
            }
        }

        protected void SetFadeActive(bool isActive) {
            fadeSprite.enabled = isActive;
        }

        protected int FadeSpeed() {
            return 24;
        }

        protected int SlowFadeSpeed() {
            return FadeSpeed() * 2;
        }

        public void ExecEvent() {
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void CreateTintColor() {
#else
        public async Task CreateTintColor() {
#endif
            if (!tintSprite)
            {
                var canvas = GameObject.FindWithTag("Canvas");
                var gameObject = new GameObject();
                gameObject.name = "TintObject";
                gameObject.transform.SetParent(canvas.transform);
                tintSprite = gameObject.AddComponent<Image>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var sp = UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(
#else
                var sp = await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(
#endif
                    "Assets/Tkool/Editor/Images/System/fadebackground" + ".png");
                tintSprite.sprite = sp;
                gameObject.transform.localScale = new Vector3(1000f, 1000f, 1f);
                gameObject.transform.localPosition = new Vector3(gameObject.transform.localPosition.x,
                    gameObject.transform.localPosition.y, -8f);
            }
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        protected void StartTintOut(int duration, Color tint) {
#else
        protected async Task StartTintOut(int duration, Color tint) {
#endif
            tintAlpha = 0.0f;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            CreateTintColor();
#else
            await CreateTintColor();
#endif
            tintSign = -1;
            tintDuration = duration;
            _tintColor = tint;
            tintSprite.color = new Color(_tintColor.r, _tintColor.g, _tintColor.b, 0.3f);
        }

        protected void UpdateTint() {
            if (tintDuration > 0)
            {
                var d = tintDuration;
                if (tintSign > 0)
                {
                    tintAlpha -= tintSprite.color.a / d;
                    tintSprite.color = new Color(_tintColor.r, _tintColor.g, _tintColor.b, tintAlpha);
                }
                else
                {
                    tintAlpha += tintSprite.color.a / d;
                    tintSprite.color = new Color(_tintColor.r, _tintColor.g, _tintColor.b, tintAlpha);
                }

                tintDuration--;
            }
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void CreateFlashColor() {
#else
        public async Task CreateFlashColor() {
#endif
            if (!flashSprite)
            {
                var canvas = GameObject.FindWithTag("Canvas");
                var gameObject = new GameObject();
                gameObject.name = "FlashObject";
                gameObject.transform.SetParent(canvas.transform);
                flashSprite = gameObject.AddComponent<Image>();
                flashSprite.material = new Material(Shader.Find("UI/Default"));
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var sp = UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(
#else
                var sp = await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(
#endif
                    "Assets/Tkool/Editor/Images/System/fadebackground" + ".png");
                flashSprite.sprite = sp;
                gameObject.transform.localScale = new Vector3(1000f, 1000f, 1f);
                gameObject.transform.localPosition = new Vector3(gameObject.transform.localPosition.x,
                    gameObject.transform.localPosition.y, -9f);
            }
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        protected void StartFlash(int duration, Color flash, int a) {
#else
        protected async Task StartFlash(int duration, Color flash, int a) {
#endif
            flashAlpha = a / 255f;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            CreateFlashColor();
#else
            await CreateFlashColor();
#endif
            flashSign = 1;
            flashDuration = duration;
            _flashColor = flash;
            flashSprite.color = new Color(_flashColor.r, _flashColor.g, _flashColor.b, flashAlpha);
        }

        protected void UpdateFlash() {
            if (flashDuration > 0)
            {
                var d = flashDuration;
                if (flashSign > 0)
                {
                    flashAlpha -= flashSprite.color.a / d;

                    flashSprite.color = new Color(_flashColor.r, _flashColor.g, _flashColor.b, flashAlpha);
                }
                else
                {
                    flashAlpha += (1.0f + flashSprite.color.a) / d;
                    flashSprite.color = new Color(_flashColor.r, _flashColor.g, _flashColor.b, flashAlpha);
                }

                flashDuration--;
            }
        }


        protected void StartWeather(string type, int power, int frame) {
            if (type == "Rain")
            {
                rainParticle.gameObject.SetActive(true);
                var main = rainParticle.main;
                main.gravityModifier = 2f + power;
            }
            else if (type == "Storm")
            {
                rainParticle.gameObject.SetActive(true);
                var main = rainParticle.main;
                var emissionModule = rainParticle.emission;
                main.gravityModifier = 4f + power;
                emissionModule.rateOverTime = 20f + 10 * power;
            }
            else if (type == "Snow")
            {
                snowParticle.gameObject.SetActive(true);
                var main = snowParticle.main;
                main.gravityModifier = 2f + power;
            }
            else
            {
                rainParticle.gameObject.SetActive(false);
                snowParticle.gameObject.SetActive(false);
            }
        }
    }
}