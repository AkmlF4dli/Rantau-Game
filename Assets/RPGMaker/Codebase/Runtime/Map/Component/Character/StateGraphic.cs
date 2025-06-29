using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.AssetManage;
using RPGMaker.Codebase.CoreSystem.Knowledge.Enum;
using System;
using UnityEngine;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Map.Component.Character
{
    public class StateGraphic : CharacterGraphic
    {
        
        private SpriteDataModel _sdSprite;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public override void ChangeAsset(string assetId) {
#else
        public override async Task ChangeAsset(string assetId) {
#endif
            // 画像が設定されていなければ初期化
            if (_image == null)
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                Init(assetId);
#else
                await Init(assetId);
#endif
            }

            if (_image != null && _image.enabled == false)
                _image.enabled = true;

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            SetUpSprites(assetId);
#else
            await SetUpSprites(assetId);
#endif
            _currentSprite = (int)_directionEnum switch
            {
                0 => null,
                1 => _sdSprite,
                _ => throw new ArgumentOutOfRangeException()
            };
            _currentSpriteIndex = 0;
            ChangeTextureSize();
            Render();
        }


        public override void ChangeDirection(CharacterMoveDirectionEnum directionEnum) {
            if (directionEnum == _directionEnum) return;

            _directionEnum = directionEnum;
            _currentSprite = (int) _directionEnum switch
            {
                0 => null,
                1 => _sdSprite,
                _ => throw new ArgumentOutOfRangeException()
            };
            _currentSpriteIndex = 0;
            ChangeTextureSize();
            Render();
        }
        
        protected override void ChangeTextureSize() {
            var spriteData = (int) _directionEnum switch
            {
                0 => null,
                1 => _sdSprite,
                _ => throw new ArgumentOutOfRangeException()
            };

            if (spriteData?.sprite?.texture != null)
                SetSize(new Vector2(
                    spriteData.sprite.texture.width / spriteData.animationFrame / TILE_SIZE,
                    spriteData.sprite.texture.height / TILE_SIZE));
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        protected override void SetUpSprites(string assetId) {
#else
        protected override async Task SetUpSprites(string assetId) {
#endif
            if (assetId != "")
            {
                // idとファイル名は同名の為そのままロード
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var inputString =
                    UnityEditorWrapper.AssetDatabaseWrapper
                        .LoadAssetAtPath<TextAsset>(
                            "Assets/RPGMaker/Storage/AssetManage/JSON/Assets/" + assetId + ".json");
                var assetManageData = JsonHelper.FromJson<AssetManageDataModel>(inputString.text);
#else
                var assetManageData =
#if !UNITY_WEBGL
					ScriptableObjectOperator.GetClass<AssetManageDataModel>("SO/" + assetId + ".asset") as AssetManageDataModel;
#else
					(await ScriptableObjectOperator.GetClass<AssetManageDataModel>("SO/" + assetId + ".asset")) as AssetManageDataModel;
#endif
#endif
                //各方向のキャラクター画像を読み込む。画像がnullだった場合はオブジェクト用の画像から探す
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var sprite = LoadCharacterSprite(assetManageData.imageSettings[0].path);
#else
                var sprite = await LoadCharacterSprite(assetManageData.imageSettings[0].path);
#endif

                //コマ数入ってくる
                var spriteIndex = assetManageData.imageSettings[0].animationFrame == 0 ? 1 : assetManageData.imageSettings[0].animationFrame;
                // アニメーション速度
                var spriteSpeed = assetManageData.imageSettings[0].animationSpeed == 0 ? 1 : assetManageData.imageSettings[0].animationSpeed * assetManageData.imageSettings[0].animationFrame;

                _sdSprite = new SpriteDataModel(sprite, spriteIndex, spriteSpeed);
            }
            //画像のIDが入ってこなかったら画像を消す
            else
            {
                _sdSprite = null;
                if (_currentSprite != null)
                    _currentSprite.sprite = null;
                if (_image != null)
                    _image.enabled = false;
            }
        }
        
        /// <summary>
        ///     キャラクター用画像の読込
        /// </summary>
        /// <param name="fileName">画像のファイル名</param>
        /// <returns>読み込まれた画像のSprite</returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        protected override Sprite LoadCharacterSprite(string fileName) {
#else
        protected override async Task<Sprite> LoadCharacterSprite(string fileName) {
#endif
            if (string.IsNullOrEmpty(fileName)) return null;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            return UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(
#else
            return await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(
#endif
                "Assets/RPGMaker/Storage/Images/System/Status/" + fileName);
        }
        
        // enum / interface / local class
        //--------------------------------------------------------------------------------------------------------------
    }
}