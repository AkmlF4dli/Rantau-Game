using RPGMaker.Codebase.CoreSystem.Helper;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Common.Component.Hud
{
    public class Movie : MonoBehaviour
    {
        // const
        //--------------------------------------------------------------------------------------------------------------
        private const string PrefabPath       = "Assets/RPGMaker/Codebase/Runtime/Map/Asset/Prefab/Movie.prefab";
        private const string MoviePath        = "Assets/RPGMaker/Storage/Movies/";
        private const string ParentObjectName = "Canvas";

        // データプロパティ
        //--------------------------------------------------------------------------------------------------------------

        // 状態プロパティ
        //--------------------------------------------------------------------------------------------------------------

        // 表示要素プロパティ
        //--------------------------------------------------------------------------------------------------------------
        private GameObject    _prefab;
        private RawImage      _rawImage;
        private RenderTexture _renderTexture;
        private VideoPlayer   _videoPlayer;
        private Action _callBack;

        // 関数プロパティ
        //--------------------------------------------------------------------------------------------------------------

        // enum / interface / local class
        //--------------------------------------------------------------------------------------------------------------

        // initialize methods
        //--------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 初期化
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void Init() {
#else
        public async Task Init() {
#endif
            //描画用のプレハブが無い場合に生成
            if (_prefab == null)
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var loadPrefab = UnityEditorWrapper.PrefabUtilityWrapper.LoadPrefabContents(PrefabPath);
#else
                var loadPrefab = await UnityEditorWrapper.PrefabUtilityWrapper.LoadPrefabContents(PrefabPath);
#endif
                _prefab = Instantiate(
                    loadPrefab,
                    gameObject.transform,
                    true
                );
                UnityEditorWrapper.PrefabUtilityWrapper.UnloadPrefabContents(loadPrefab);
            }

            _prefab.SetActive(true);
        }

        /// <summary>
        /// ムービーファイル名を渡して、読み込みの実施
        /// </summary>
        /// <param name="pictureName"></param>
        /// <param name="callBack"></param>
        public void AddMovie(string pictureName, Action callBack) {
            
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            //動画ファイルの読み込み
            var acquiredMovie =
                UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<VideoClip>(MoviePath + pictureName + ".mp4");
#endif
            
            //再生用のオブジェクトの生成と設定
            var obj = new GameObject();
            obj.transform.SetParent(_prefab.transform.Find(ParentObjectName).transform);
            obj.AddComponent<VideoPlayer>();
            obj.AddComponent<RectTransform>();
            obj.AddComponent<RawImage>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            obj.GetComponent<RectTransform>().sizeDelta = new Vector2(acquiredMovie.width, acquiredMovie.height);
#endif
            obj.GetComponent<RectTransform>().localPosition = Vector3.zero;
            obj.transform.localScale = new Vector3(1, 1, 1);

            //描画用「RenderTexture」の設定
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _renderTexture = new RenderTexture((int) acquiredMovie.width, (int) acquiredMovie.height, 24);
#endif

            //描画用「RawImage」の設定
            _rawImage = obj.GetComponent<RawImage>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _rawImage.texture = _renderTexture;
#else
            _rawImage.enabled = false;
#endif

            //描画用「VideoPlayer」の設定
            _videoPlayer = obj.GetComponent<VideoPlayer>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _videoPlayer.targetTexture = _renderTexture;
#endif
            _videoPlayer.loopPointReached += EndReached;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _videoPlayer.clip = acquiredMovie;
#else
            var url = $"{Application.streamingAssetsPath}/{pictureName}.mp4";
            _videoPlayer.source = VideoSource.Url;
            _videoPlayer.url = url;
#endif
            _callBack = callBack;
            //実際に再生していく
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _videoPlayer.Prepare();
            TimeHandler.Instance.AddTimeActionEveryFrame(Prepared);
#else
            _videoPlayer.prepareCompleted += Prepared;
            _videoPlayer.errorReceived += ErrorReceived;
            _videoPlayer.Prepare();
#endif
        }

        private void EndReached(VideoPlayer yuhrr) {
            _videoPlayer.frame = 0;
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void Prepared() {
#else
        private void Prepared(VideoPlayer videoPlayer) {
            if (videoPlayer != _videoPlayer) Debug.LogError($"player is different: {videoPlayer}, {_videoPlayer}");
#endif
            if (_videoPlayer.isPrepared)
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                TimeHandler.Instance.RemoveTimeAction(Prepared);
#else
                var width = (int)_videoPlayer.width;
                var height = (int) _videoPlayer.height;

                _videoPlayer.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
                _renderTexture = new RenderTexture(width, height, 24);
                _rawImage.texture = _renderTexture;
                _rawImage.enabled = true;
                _videoPlayer.targetTexture = _renderTexture;
#endif
                //再生の開始
                _videoPlayer.Play();
                TimeHandler.Instance.AddTimeActionEveryFrame(PlayMovie);
            }
        }
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
        private void ErrorReceived(VideoPlayer source, string message) {
            Debug.LogError($"ErrorReceived: {source}, {message}");
            _videoPlayer.gameObject.SetActive(false);
            _callBack?.Invoke();
        }
#endif

        /// <summary>
        /// 実際に再生から停止までを司る部分
        /// </summary>
        private void PlayMovie() {
            if(_videoPlayer.isPlaying) return;

            TimeHandler.Instance.RemoveTimeAction(PlayMovie);
            //再生の停止
            _videoPlayer.Stop();
            _videoPlayer.gameObject.SetActive(false);
            _callBack?.Invoke();
        }
    }
}