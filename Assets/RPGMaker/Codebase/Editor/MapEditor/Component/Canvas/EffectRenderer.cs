using System.Collections;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGMaker.Codebase.Editor.MapEditor.Component.Canvas
{
    public class EffectRenderer
    {
        public enum BlitType
        {
            Normal,
            Blur,
            Bloom
        }

        private const int ReleaseWait = 5;

        private readonly Image _image;
        private RenderTexture _source;
        private RenderTexture _destination;
        private bool _dirty;
        private bool _isRunning;
        private EditorCoroutine _coroutine;
        private Material _material;
        private BlitType _type;
        private static readonly int Temp = Shader.PropertyToID("_Temp");
        private int _releaseCounter;

        public void SetDirty(bool dirty = true, BlitType blitType = BlitType.Normal)
        {
            _dirty = dirty;
            _type = blitType;
        }

        public RenderTexture RenderTexture
        {
            get => _source;
            set
            {
                if (_source != value)
                {
                    _source = value;
                    ResetDestination(_source);
                }
            }
        }

        public void SetEffectMaterial(string shaderName)
        {
            if (string.IsNullOrWhiteSpace(shaderName)) _material = null;
            else _material = new Material(Shader.Find(shaderName));
        }

        public void ApplyParams(int id, float value)
        {
            if (_material) _material.SetFloat(id, value);
        }

        public void ApplyParams(int id, float[] value)
        {
            if (_material) _material.SetFloatArray(id, value);
        }

        public void ApplyParams(int id, Vector4 value)
        {
            if (_material) _material.SetVector(id, value);
        }

        public void ApplyParams(int id, int value)
        {
            if (_material) _material.SetInt(id, value);
        }

        public EffectRenderer(Image image)
        {
            _image = image;
        }

        public void Dispose()
        {
            _source = null;
            ResetDestination();
            _isRunning = false;
            EditorCoroutineUtility.StopCoroutine(_coroutine);
        }

        public void Start()
        {
            _isRunning = true;
            _coroutine = EditorCoroutineUtility.StartCoroutineOwnerless(Loop());
        }

        private IEnumerator Loop()
        {
            while (_isRunning)
            {
                if (_dirty)
                {
                    RenderTexture current = RenderTexture.active;
                    RenderTexture.active = _source;

                    if (_material != null)
                    {
                        switch (_type)
                        {
                            case BlitType.Blur:
                                RenderTexture rt1 = RenderTexture.GetTemporary(
                                    _source.width,
                                    _source.height,
                                    _source.depth,
                                    _source.format
                                );
                                RenderTexture rt2 = RenderTexture.GetTemporary(
                                    _source.width,
                                    _source.height,
                                    _source.depth,
                                    _source.format
                                );

                                Graphics.Blit(_source, rt1);
                                Graphics.Blit(rt1, rt2, _material, 0);
                                Graphics.Blit(rt2, rt1, _material, 0);
                                Graphics.Blit(rt1, rt2, _material, 1);
                                Graphics.Blit(rt2, _destination);

                                RenderTexture.ReleaseTemporary(rt1);
                                RenderTexture.ReleaseTemporary(rt2);
                                break;

                            case BlitType.Bloom:
                                RenderTexture rt3 = RenderTexture.GetTemporary(
                                    _source.width / 2,
                                    _source.height / 2,
                                    _source.depth,
                                    _source.format
                                );
                                _material.SetTexture(Temp, rt3);
                                Graphics.Blit(_source, rt3, _material, 0);
                                Graphics.Blit(_source, _destination, _material, 1);

                                RenderTexture.ReleaseTemporary(rt3);
                                break;

                            default:
                                Graphics.Blit(_source, _destination, _material);
                                break;
                        }
                    }
                    else
                    {
                        Graphics.Blit(_source, _destination);
                    }

                    RenderTexture.active = current;
                    _image.image = _destination;
                    _image.MarkDirtyRepaint();

                    // NOTE: シーンプレビューが遅れてレンダリングされる為の対策
                    _releaseCounter++;
                    if (_releaseCounter >= ReleaseWait)
                    {
                        _releaseCounter = 0;
                        _dirty = false;
                        _type = BlitType.Normal;
                    }
                }

                yield return null;
            }
        }

        private void ResetDestination(RenderTexture source = null)
        {
            if (_destination != null)
            {
                _destination.Release();
                Object.DestroyImmediate(_destination);
            }

            if (source == null) return;

            _destination = new RenderTexture(source.width, source.height, source.depth, source.format);
        }
    }
}