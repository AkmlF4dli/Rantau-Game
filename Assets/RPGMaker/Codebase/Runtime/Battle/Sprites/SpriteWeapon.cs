using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.AssetManage;
using RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement;
using RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement.Repository;
using RPGMaker.Codebase.Runtime.Common;
using UnityEngine;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Battle.Sprites
{
    /// <summary>
    /// 武器(img/system/WeaponsX.png)のスプライト
    /// </summary>
    public class SpriteWeapon : SpriteBase
    {
        /// <summary>
        /// 画像のフレーム数
        /// </summary>
        private int _frameCount;
        /// <summary>
        /// 現在表示しているフレーム番号
        /// </summary>
        private int _imageFrame;
        /// <summary>
        /// 画像のTexture
        /// </summary>
        private Texture2D _texture;
        /// <summary>
        /// 画像名
        /// </summary>
        private string _weaponImageId = "";
        /// <summary>
        /// AssetManageDataModel
        /// </summary>
        private AssetManageDataModel assetData;


        /// <summary>
        /// 初期化
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public override void Initialize() {
#else
        public override async Task Initialize() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            base.Initialize();
#else
            await base.Initialize();
#endif
            InitMembers();
            _initialized = true;
            TimeHandler.Instance.AddTimeActionEveryFrame(UpdateTimeHandler);
        }

        /// <summary>
        /// 破棄処理
        /// </summary>
        private void OnDestroy() {
            TimeHandler.Instance?.RemoveTimeAction(UpdateTimeHandler);
        }

        /// <summary>
        /// メンバ変数を初期化
        /// </summary>
        public void InitMembers() {
            _weaponImageId = "";
            anchor.x = 0.5;
            anchor.y = 1;
            X = -32;
        }

        /// <summary>
        /// 準備
        /// </summary>
        /// <param name="weaponImageId"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void Setup(string weaponImageId) {
#else
        public async Task Setup(string weaponImageId) {
#endif
            if (assetData == null)
            {
                _weaponImageId = weaponImageId;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                LoadBitmap();
#else
                await LoadBitmap();
#endif
            }
        }

        /// <summary>
        /// Update処理
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public override void UpdateTimeHandler() {
#else
        public override async Task UpdateTimeHandlerAsync() {
#endif
            if (!_initialized) return;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            base.UpdateTimeHandler();
#else
            await base.UpdateTimeHandlerAsync();
#endif
        }

        /// <summary>
        /// 画像の読み込み
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void LoadBitmap() {
#else
        public async Task LoadBitmap() {
#endif
            if (!string.IsNullOrEmpty(_weaponImageId))
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                InitData();
#else
                await InitData();
#endif

            if (assetData != null)
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _texture = UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Texture2D>(
#else
                _texture = await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Texture2D>(
#endif
                    "Assets/RPGMaker/Storage/Images/System/Weapon/" + assetData.imageSettings[0].path);
                _frameCount = assetData.imageSettings[0].animationFrame;
            }
        }

        /// <summary>
        /// 画像のAssetData読込
        /// </summary>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void InitData() {
#else
        private async Task InitData() {
#endif
#if UNITY_EDITOR && !UNITE_WEBGL_TEST
            var orderData = AssetManageRepository.OrderManager.Load();
            var databaseManagementService = new DatabaseManagementService();
            var manageData = databaseManagementService.LoadAssetManage();
            assetData = null;
            for (int i = 0; i < manageData.Count; i++)
                if (manageData[i].id == _weaponImageId)
                {
                    assetData = manageData[i];
                    break;
                }
#else
#if !UNITY_WEBGL
            assetData = ScriptableObjectOperator.GetClass<AssetManageDataModel>("SO/" + _weaponImageId + ".asset") as AssetManageDataModel;
#else
            assetData = (await ScriptableObjectOperator.GetClass<AssetManageDataModel>("SO/" + _weaponImageId + ".asset")) as AssetManageDataModel;
#endif
#endif
        }

        /// <summary>
        /// モーション開始
        /// </summary>
        /// <param name="frameTime"></param>
        public void StartMotion() {
            if (assetData != null && _texture != null)
            {
                //アニメーション速度の定義は、アニメーションが1巡するまでのフレーム数(60fps)で定義されている
                //1コマを切り替える時間は、1巡するまでの速度 / コマ数 / 60f
                int motionMax = assetData.imageSettings[0].animationFrame;
                float frameTime = 1.0f * assetData.imageSettings[0].animationSpeed / motionMax / 20.0f;
                TimeHandler.Instance.AddTimeAction(frameTime, MotionAnimation, true);
            }
        }

        /// <summary>
        /// アニメーション処理
        /// </summary>
        private void MotionAnimation() {
            if (assetData != null && _texture != null)
            {
                //Sprite設定
                SetSprite(_texture);
                //TextureUV設定
                SetTextureUV(new Vector2(1.0f / _frameCount * _imageFrame, 0),
                    new Vector2(1.0f / _frameCount * (_imageFrame + 1), 1));
                //サイズ設定
                SetSize(new Vector2(_texture.width/ (_frameCount + 1), _texture.height));
                //コマ数を更新
                _imageFrame++;

                //最終のコマ迄再生していた場合
                if (_imageFrame == assetData.imageSettings[0].animationFrame)
                {
                    _imageFrame = 0;

                    SetSprite(_texture);
                    SetTextureUV(new Vector2(1.0f / _frameCount * _imageFrame, 0), new Vector2(1.0f / _frameCount * (_imageFrame + 1), 1));
                    SetSize(new Vector2(_texture.width/ (_frameCount + 1), _texture.height));

                    //アニメーション終了で非表示にする
                    _weaponImageId = "";
                    gameObject.SetActive(false);
                    TimeHandler.Instance.RemoveTimeAction(MotionAnimation);
                }

                return;
            }

            //素材の読み込みが行えなかった場合は処理を終了する
            _weaponImageId = "";
            gameObject.SetActive(false);
            TimeHandler.Instance.RemoveTimeAction(MotionAnimation);
        }
    }
}