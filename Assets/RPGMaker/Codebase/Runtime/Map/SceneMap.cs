using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.Runtime.Common;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

namespace RPGMaker.Codebase.Runtime.Map
{
    /// <summary>
    /// マップのScene制御
    /// </summary>
    public class SceneMap : SceneBase
    {
        [SerializeField] public GameObject menuGameObject;
        [SerializeField] public GameObject rootGameObject;
        
        // エフェクト無効カメラ
        private Camera _ignoreEffectCamera;

        protected override void Start() {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            StartCoroutine(Cleanup());
        }
        private System.Collections.IEnumerator Cleanup() {
            AddressableManager.Load.MarkForRelease();
            AddressableManager.Load.IncrementTimestamp();

            yield return Resources.UnloadUnusedAssets();

            System.GC.Collect();
            StartAsync();
        }
        private async void StartAsync() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            HudDistributor.Instance.StaticHudHandler().DisplayInitByScene();
            HudDistributor.Instance.StaticHudHandler().FadeInFixedBlack(Init,   false, 0f, true);
#else
            await HudDistributor.Instance.StaticHudHandler().DisplayInitByScene();
            await Init();
            HudDistributor.Instance.StaticHudHandler().FadeInFixedBlack(() => { }, false, 0f, true);
#endif
            MenuManager.IsEndGameToTitle = false;
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        protected override void Init() {
            base.Init();
#else
        protected override async Task Init() {
            await base.Init();
#endif
            //U388 隊列歩行の有無の設定
            MapManager.ChangePlayerFollowers(DataManager.Self().GetRuntimeSaveDataModel().runtimeSystemConfig.follow == 1);

            if (!Commons.IsURP())
            {
                _ignoreEffectCamera = new GameObject("IgnoreEffectCamera").AddComponent<Camera>();
            }
            
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            MapManager.InitManager(rootGameObject, Camera.main, menuGameObject, _ignoreEffectCamera);
#else
            await MapManager.InitManager(rootGameObject, Camera.main, menuGameObject, _ignoreEffectCamera);
#endif

            // パーティメンバーの隊列歩行の反映
            // follow = -1 の場合は、旧セーブデータで、隊列歩行状態を持っていないもののため、マスタデータの値を反映する
            if (DataManager.Self().GetRuntimeSaveDataModel().runtimeSystemConfig.follow == -1)
            {
                DataManager.Self().GetRuntimeSaveDataModel().runtimeSystemConfig.follow = DataManager.Self().GetSystemDataModel().optionSetting.optFollowers;
            }


            //カメラ設定、画面サイズによって変更する。アスペクト比はベース維持
            var baseWidth = 9.0f;
            var baseHeight = 16.0f;

            var displaySize = DataManager.Self().GetSystemDataModel()
                .DisplaySize[DataManager.Self().GetSystemDataModel().displaySize];
            var scaleWidth = displaySize.y / baseHeight * (baseWidth / displaySize.x);
            var scaleRatio = Mathf.Max(scaleWidth, 1.0f);
            Camera.main.orthographicSize *= scaleRatio;

            if (!Commons.IsURP())
            {
                _ignoreEffectCamera.CopyFrom(Camera.main);
                _ignoreEffectCamera.gameObject.AddComponent<CameraResolutionManager>();
                var layerMask = LayerMask.NameToLayer("IgnorePostEffect");
                Camera.main.cullingMask = ~(1 << layerMask);
                _ignoreEffectCamera.cullingMask = 1 << layerMask;
                _ignoreEffectCamera.clearFlags = CameraClearFlags.Nothing;
            } else
            {
                var cameraData = Camera.main.GetUniversalAdditionalCameraData();
                cameraData.SetRenderer(Commons.UniteRendererDataOffset);
            }

            Camera.main.cullingMask &= ~(1 << LayerMask.NameToLayer("Minimap"));

            //Canvas大きさ設定
            var scales = rootGameObject.transform.GetComponentsInChildren<CanvasScaler>();
            foreach (var scale in scales)
            {
                scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                //scale.referenceResolution = displaySize;
            }

            // ループマップなどで主人公が下に移動し続けると表示されなくなるので、
            // ひとまず表示される範囲で極端に小さな値を設定。
            Camera.main.nearClipPlane = -10000000000f;
            if (!Commons.IsURP())
            {
                _ignoreEffectCamera.nearClipPlane = -10000000000f;
            }

            gameObject.AddComponent<SelectedGameObjectManager>();

            TimeHandler.Instance.AddTimeActionEveryFrame(UpdateTimeHandler);
        }

        private void OnDestroy() {
            TimeHandler.Instance?.RemoveTimeAction(UpdateTimeHandler);
        }

        public void UpdateTimeHandler() {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            _ = UpdateTimeHandlerAsync();
        }
        private async Task UpdateTimeHandlerAsync() {
#endif
            //Map更新処理
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            MapManager.UpdateEventWatch();
#else
            await MapManager.UpdateEventWatch();
#endif

            //GameOverへの遷移中でなければ、キーイベントを受け付ける
            if (!MapManager.IsMovingGameOver)
                InputHandler.Watch();
        }
    }
}