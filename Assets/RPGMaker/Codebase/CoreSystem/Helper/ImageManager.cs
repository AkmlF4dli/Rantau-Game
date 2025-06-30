using System.Collections.Generic;
using System.IO;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.AssetManage;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Map;
using RPGMaker.Codebase.CoreSystem.Knowledge.Enum;
using RPGMaker.Codebase.CoreSystem.Knowledge.Misc;

#if UNITY_EDITOR
using RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement;
using RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement.Repository;
using RPGMaker.Codebase.CoreSystem.Service.MapManagement.Repository;
using UnityEditor;
#endif

using UnityEngine;
using System.Threading.Tasks;

// 読み込み
namespace RPGMaker.Codebase.CoreSystem.Helper
{
    public static class ImageManager {
        public const int OBJECT_WIDTH = 98;
        private static List<string> DefaultBattlebackName = new List<string>
        {
            "battlebacks1_nature_008",
            "battlebacks2_nature_008"
        };

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static Sprite LoadBattleback1(string filename, int? hue = null) {
#else
        public static async Task<Sprite> LoadBattleback1(string filename, int? hue = null) {
#endif
            if (!string.IsNullOrEmpty(filename))
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                return UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(
#else
                return await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(
#endif
                    PathManager.BATTLE_BACKGROUND_1 + filename + ".png");
            }
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            return UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(
#else
            return await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(
#endif
                PathManager.SYSTEM_BATTLE_BACKGROUND_1 + "battlebacks1_nature_008.png");
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static Sprite LoadBattleback2(string filename, int? hue = null) {
#else
        public static async Task<Sprite> LoadBattleback2(string filename, int? hue = null) {
#endif
            if (!string.IsNullOrEmpty(filename))
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                return UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(
#else
                return await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(
#endif
                    PathManager.BATTLE_BACKGROUND_2 + filename + ".png");
            }
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            return UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(
#else
            return await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(
#endif
                PathManager.SYSTEM_BATTLE_BACKGROUND_2 + "battlebacks2_nature_008.png");
        }

        public static string GetBattlebackName(string filename, int backNumber) {
            if (!string.IsNullOrEmpty(filename))
            {
                return filename;
            }
            else
            {
                if (backNumber == 1)
                {
                    return DefaultBattlebackName[0];
                }
                else
                {
                    return DefaultBattlebackName[1];
                }
            }
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static Bitmap LoadEnemy(string filename, int? hue = null) {
            return LoadBitmap(PathManager.IMAGE_ENEMY, filename, hue, true);
        }
#else
        public static async Task<Bitmap> LoadEnemy(string filename, int? hue = null) {
            return await LoadBitmap(PathManager.IMAGE_ENEMY, filename, hue, true);
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static Texture2D LoadEnemyByTexture(string filename, int? hue = null) {
            return UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Texture2D>(
                PathManager.IMAGE_ENEMY + filename + ".png");
        }
#else
        public static async Task<Texture2D> LoadEnemyByTexture(string filename, int? hue = null) {
            return await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Texture2D>(
                PathManager.IMAGE_ENEMY + filename + ".png");
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static Texture2D LoadFace(string filename, int? hue = null) {
            return UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Texture2D>(
                PathManager.IMAGE_FACE + filename + ".png");
        }
#else
        public static async Task<Texture2D> LoadFace(string filename, int? hue = null) {
            return await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Texture2D>(
                PathManager.IMAGE_FACE + filename + ".png");
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static Texture2D LoadPicture(string filename, int? hue = null) {
            return UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Texture2D>(
                PathManager.IMAGE_ADV + filename + ".png");
        }
#else
        public static async Task<Texture2D> LoadPicture(string filename, int? hue = null) {
            return await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Texture2D>(
                PathManager.IMAGE_ADV + filename + ".png");
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static OverrideTexture LoadSvActor(string filename, int? hue = null) {
            return LoadTexture(PathManager.IMAGE_SV_CHARACTER, filename, hue, false);
        }
#else
        public static async Task<OverrideTexture> LoadSvActor(string filename, int? hue = null) {
            return await LoadTexture(PathManager.IMAGE_SV_CHARACTER, filename, hue, false);
        }
#endif
        
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static Bitmap LoadSystem(string filename, int? hue = null) {
            return LoadBitmap(PathManager.IMAGE_SYSTEM, filename, hue, false);
        }
#else
        public static async Task<Bitmap> LoadSystem(string filename, int? hue = null) {
            return await LoadBitmap(PathManager.IMAGE_SYSTEM, filename, hue, false);
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static Sprite LoadDamage(string filename, int? hue = null) {
            return UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(
                PathManager.SYSTEM_DAMAGE + filename + ".png");
        }
#else
        public static async Task<Sprite> LoadDamage(string filename, int? hue = null) {
            return await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(
                PathManager.SYSTEM_DAMAGE + filename + ".png");
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private static Bitmap LoadBitmap(string folder, string filename, int? hue, bool smooth) {
#else
        private static async Task<Bitmap> LoadBitmap(string folder, string filename, int? hue, bool smooth) {
#endif
            if (filename != "")
            {
                var path = folder + filename;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var bitmap = LoadNormalBitmap(path, hue ?? 0);
#else
                var bitmap = await LoadNormalBitmap(path, hue ?? 0);
#endif

                return bitmap;
            }

            return null;
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private static OverrideTexture LoadTexture(string folder, string filename, int? hue, bool smooth) {
#else
        private static async Task<OverrideTexture> LoadTexture(string folder, string filename, int? hue, bool smooth) {
#endif
            if (filename != "")
            {
                var path = folder + filename;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var texture = LoadNormalTexture(path, hue ?? 0);
#else
                var texture = await LoadNormalTexture(path, hue ?? 0);
#endif
                return texture;
            }

            return null;
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private static Bitmap LoadNormalBitmap(string path, int hue) {
            return new Bitmap(UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(path + ".png"));
        }
#else
        private static async Task<Bitmap> LoadNormalBitmap(string path, int hue) {
            return new Bitmap(await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(path + ".png"));
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private static OverrideTexture LoadNormalTexture(string path, int hue) {
#else
        private static async Task<OverrideTexture> LoadNormalTexture(string path, int hue) {
#endif
            return new OverrideTexture(
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                (Texture) UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath(path + ".png", typeof(Texture)));
#else
                (Texture) await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath(path + ".png", typeof(Texture)));
#endif
        }

        public static Texture2D LoadPopIcon(string filename, int frame) {
            var texture2Ds =
                ImageUtility.Instance.SliceImage(PathManager.IMAGE_BALLOON + filename, frame == 0 ? 1 : frame, 1);
            return texture2Ds[texture2Ds.Count - 1];
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static Texture2D LoadSvCharacter(string assetId) {
#else
        public static async Task<Texture2D> LoadSvCharacter(string assetId) {
#endif
            // 渡されたIDが空なら抜ける
            if (string.IsNullOrEmpty(assetId)) return null;

#if UNITY_EDITOR && !UNITE_WEBGL_TEST
            var inputString =
                UnityEditorWrapper.AssetDatabaseWrapper
                    .LoadAssetAtPath<TextAsset>(
                        PathManager.JSON_ASSETS + assetId + ".json");
            if (inputString == null)
                return null;
            var assetManageData = JsonHelper.FromJson<AssetManageDataModel>(inputString.text);
#else
            var assetManageData =
#if !UNITY_WEBGL
 ScriptableObjectOperator.GetClass<AssetManageDataModel>("SO/" + assetId + ".asset") as AssetManageDataModel;
#else
 (await ScriptableObjectOperator.GetClass<AssetManageDataModel>("SO/" + assetId + ".asset")) as AssetManageDataModel;
#endif
#endif
            // アセットタイプによってパス設定
            var path = "";
            switch (assetManageData.assetTypeId)
            {
                case (int) AssetCategoryEnum.MOVE_CHARACTER:
                    path = PathManager.IMAGE_CHARACTER;
                    break;
                case (int) AssetCategoryEnum.SV_BATTLE_CHARACTER:
                    path = PathManager.IMAGE_SV_CHARACTER;
                    break;
                case (int) AssetCategoryEnum.OBJECT:
                    path = PathManager.IMAGE_OBJECT;
                    break;
            }

            if (!File.Exists(path + assetManageData.imageSettings[0].path))
                return null;
            var texture2Ds = ImageUtility.Instance.SliceImage(
                path + assetManageData.imageSettings[0].path,
                assetManageData.imageSettings[0].animationFrame == 0
                    ? 1
                    : assetManageData.imageSettings[0].animationFrame, 1);
            return texture2Ds[0];
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static Sprite LoadTile(string tileName) {
#else
        public static async Task<Sprite> LoadTile(string tileName) {
#endif
#if UNITY_EDITOR && !UNITE_WEBGL_TEST
            var type = TileDataModel.Type.NormalTile;
            string tileId = string.Empty;
            if (tileName.Length >= 2)
            {
                type = (tileName.StartsWith("0,") ? TileDataModel.Type.NormalTile : TileDataModel.Type.LargeParts);
                tileId = tileName.Substring(2);
            }

            var tileDataModelInfo = new TileDataModelInfo();
            tileDataModelInfo.type = type;
            tileDataModelInfo.id = tileId;
            return tileDataModelInfo.TileDataModel?.m_DefaultSprite;
#else
            var tileDataModel =
#if !UNITY_WEBGL
                ScriptableObjectOperator.GetClass<TileDataModel>($"{(tileName.StartsWith("0,") ? "NormalTile" : "LargeParts")}/{tileName.Substring(2)}.asset") as TileDataModel;
#else
                (await ScriptableObjectOperator.GetClass<TileDataModel>($"{(tileName.StartsWith("0,") ? "NormalTile" : "LargeParts")}/{tileName.Substring(2)}.asset")) as TileDataModel;
#endif
            return tileDataModel.m_DefaultSprite;
#endif
        }

        public static float FixAspect(
            Vector2 windowSize,
            Vector2 texSize
        ) {
            float widthRate = windowSize.x / texSize.x;
            float heightRate = windowSize.y / texSize.y;
            
            return widthRate > heightRate ? heightRate : widthRate;
        }
        
#if UNITY_EDITOR && !UNITE_WEBGL_TEST
        /// <summary>
        /// 画像のリスト取得
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static List<string> GetImageNameList(string path) {
            //PathManager.IMAGE_FACE
            var dir = new DirectoryInfo(path);
            var fileInfoList = dir.GetFiles("*.png");
            var fileNames = new List<string>();
            for (int i = 0; i < fileInfoList.Length; i++)
            {
                var name = fileInfoList[i].Name.Replace(".png", "");
                fileNames.Add(name);
            }
            return fileNames;
        }

        /// <summary>
        /// SV関連のリストを取得
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
#if UNITY_EDITOR && !UNITE_WEBGL_TEST
        public static List<AssetManageDataModel> GetSvIdList(AssetCategoryEnum type) {
#else
        public static async Task<List<AssetManageDataModel>> GetSvIdList(AssetCategoryEnum type) {
#endif
            var orderData = AssetManageRepository.OrderManager.Load();
            var assetManageData = new List<AssetManageDataModel>();
            var databaseManagementService = new DatabaseManagementService();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var manageData = databaseManagementService.LoadAssetManage();
#else
            var manageData = await databaseManagementService.LoadAssetManage();
#endif

            for (var i = 0; i < orderData.orderDataList.Length; i++)
            {
                if (orderData.orderDataList[i].idList == null)
                    continue;
                if (type == (AssetCategoryEnum) orderData.orderDataList[i].assetTypeId)
                {
                    for (var i2 = 0; i2 < orderData.orderDataList[i].idList.Count; i2++)
                    {
                        AssetManageDataModel data = null;
                        for (int i3 = 0; i3 < manageData.Count; i3++)
                        {
                            if (manageData[i3].id == orderData.orderDataList[i].idList[i2])
                            {
                                data = manageData[i3];
                                break;
                            }
                        }

                        //【不具合（調査／修正）】フィールドキャラクタをすべて削除すると追加できなくなる
                        //UNITE_DEVELOPMENT-206
                        //null状態で追加されないように…対応する
                        if (data == null) continue;

                        assetManageData.Add(data);
                    }
                }
            }

            return assetManageData;
        }
#endif
    }
}