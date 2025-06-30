using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Map;
using System.Linq;
using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.Editor.Common;
using System.Reflection;

public class TileSpriteComverter : EditorWindow
{
    [MenuItem("RPG Maker/V1.1.0 Tool/Convert Old TileTextures to v1.1.0 Format", priority = 804)]
    static void CombineTileDataModelTextures() {

        var result = EditorUtility.DisplayDialog(EditorLocalize.LocalizeText("WORD_1649"), EditorLocalize.LocalizeText("WORD_1650"), "OK", "Cancel");
        if (result != true)
        {
            return;
        }

        // 選択中のオブジェクトからTileDataModelを選ぶ
        var tileDataModelGUIDs = AssetDatabase.FindAssets("t:TileDataModel", new string[] { "Assets/RPGMaker/Storage/Map/TileAssets/AutoTileC" });

        var normalTiles = new List<TileDataModel>();
        var autoTiles = new List<TileDataModel>();

        foreach (var guid in tileDataModelGUIDs)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var tileDataModel = AssetDatabase.LoadAssetAtPath<TileDataModel>(path);
            // ノーマルタイルは結合しない
            // ラージパーツは結合方法が異なる
            switch (tileDataModel.type)
            {
                case TileDataModel.Type.NormalTile:
                    normalTiles.Add(tileDataModel);
                    break;
                case TileDataModel.Type.LargeParts:
                case TileDataModel.Type.Region:
                case TileDataModel.Type.BackgroundCollision:
                    // コンバートしない
                    break;
                default:
                    autoTiles.Add(tileDataModel);
                    break;
            }
        }
        normalTiles.ForEach(tile => NormalTileConvert(tile));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        // 一応
        GC.Collect();
        Resources.UnloadUnusedAssets();

        autoTiles.ForEach(tile => AutoTileConvert(tile));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ラージパーツ変換処理
        CombineLargePartsTileDataModelTextures();
        // ボーダーサイズの補正処理
        ShrinkSpriteBorderSize98to96();
        // 完了ダイアログ
        EditorUtility.DisplayDialog(EditorLocalize.LocalizeText("WORD_1649"), EditorLocalize.LocalizeText("WORD_1651"), "OK");
        // 設定を行った後、再起動を行う
        var restartEditorAndRecompileScripts = typeof(EditorApplication).GetMethod("RestartEditorAndRecompileScripts", BindingFlags.NonPublic | BindingFlags.Static);
        restartEditorAndRecompileScripts.Invoke(null, null);
    }

    /// <summary>
    /// ラージパーツはパーツのピックアップの取りこぼしがあるとダメなので全部コンバートする
    /// </summary>
    static void CombineLargePartsTileDataModelTextures() {
        string TileTableJsonPath = "Assets/RPGMaker/Storage/Map/JSON/tileTable.json";
        var largePartsTileDictionary = new Dictionary<string, List<string>>();

        // LargeParts情報をJSONから読む：Assetをロードしないとタイプがわからないため
        var jsonStringOrg = UnityEditorWrapper.AssetDatabaseWrapper.LoadJsonString(TileTableJsonPath);
        var tileDataModelInfoList = JsonHelper.FromJsonArray<TileDataModelInfo>(jsonStringOrg);

        foreach (var tileDataModelInfo in tileDataModelInfoList)
        {
            if (tileDataModelInfo.type != TileDataModel.Type.LargeParts)
            {
                continue;
            }
            var parentID = tileDataModelInfo.largePartsDataModel.parentId;
            if (largePartsTileDictionary.ContainsKey(parentID) == false)
            {
                largePartsTileDictionary.Add(parentID, new List<string>());
            }
            largePartsTileDictionary[parentID].Add(tileDataModelInfo.id);
        }

        foreach (var kv in largePartsTileDictionary)
        {
            var parentID = kv.Key;
            // パーツ単位でスプライトを結合して親ID名で書き出し
            // TileDataModelを読み込む
            // 名前があるのでパス指定で
            var tileDataModelList = new List<TileDataModel>();
            var tileDataModelSpriteList = new List<Sprite>();
            foreach (var id in kv.Value)
            {
                var tileDataModelPath = $"Assets/RPGMaker/Storage/Map/TileAssets/LargeParts/{id}.asset";
                //var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<TileDataModel>(tileDataModelPath);
                if (asset != null)
                {
                    tileDataModelList.Add(asset);
                    tileDataModelSpriteList.Add(asset.m_DefaultSprite);
                    asset.m_DefaultSprite.name = asset.id;
                }
            }

            // 結合済みスプライトのパス
            var margedSpritePath = Path.Combine("Assets/RPGMaker/Storage/Map/TileAssets/LargeParts/", $"{parentID}.png");
            // テクスチャが作成済みかをチェック
            var objects = AssetDatabase.LoadAllAssetsAtPath(margedSpritePath);
            if (objects.Length == 0)
            {
                // spriteが見つからなかった場合は作る
                CreateMergedTexture(tileDataModelSpriteList, margedSpritePath);
                // 再読み込み
                objects = AssetDatabase.LoadAllAssetsAtPath(margedSpritePath);
            }
            else
            {
                continue;
            }

            // 分割済みSpriteの抽出
            List<Sprite> splitedSpriteList = new List<Sprite>();
            foreach (var obj in objects)
            {
                var sprite = obj as Sprite;
                if (sprite == null)
                {
                    continue;
                }
                splitedSpriteList.Add(sprite);
            }

            tileDataModelList.ForEach(tileDataModel =>
            {
                var sprite = splitedSpriteList.FirstOrDefault(sp => sp.name == tileDataModel.id);
                if (sprite != null)
                {
                    tileDataModel.m_DefaultSprite = sprite;
                    tileDataModel.tileImageDataModel.texture = sprite.texture;
                    EditorUtility.SetDirty(tileDataModel);
                }
            });

            tileDataModelSpriteList.ForEach(sprite =>
            {
                var oldSpriteFolder = $"Assets/RPGMaker/Storage/Map/TileAssets/LargeParts/{sprite.name}";
                // 置換後に個別のタイルを削除
                DeleteFolderAndMeta(oldSpriteFolder);
            });
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            // 一応効くっぽいのでGC
            GC.Collect();
            Resources.UnloadUnusedAssets();
        }
    }

    /// <summary>
    /// NormalTileのテクスチャが置いてあるフォルダをなくす処理
    /// </summary>
    /// <param name="normalTileList"></param>
    static void NormalTileConvert(TileDataModel tileDataModel) {

        var assetPath = AssetDatabase.GetAssetPath(tileDataModel);
        var assetDirectory = Path.GetDirectoryName(assetPath);
        var assetFileName = Path.GetFileNameWithoutExtension(assetPath);
        var spritePath = AssetDatabase.GetAssetPath(tileDataModel.m_DefaultSprite);
        var spriteExt = Path.GetExtension(spritePath);
        var newPath = Path.Combine(assetDirectory, assetFileName + spriteExt);
        // 移動
        AssetDatabase.MoveAsset(spritePath, newPath);
        // 古いのを消す
        DeleteFolderAndMeta(Path.Combine(assetDirectory, assetFileName));
    }

    /// <summary>
    /// オートタイルの変換処理
    /// </summary>
    /// <param name="autoTileList"></param>
    static void AutoTileConvert(TileDataModel tileDataModel) {
        // 対応したspriteSheetがあるかどうか
        // アセットの親フォルダを取得する
        var rootPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(tileDataModel));
        var targetFolder = Path.Combine(rootPath, tileDataModel.id);
        // 結合済みスプライトのパス
        var spritePath = Path.Combine(rootPath, $"{tileDataModel.id}.png");
        // テクスチャが作成済みかをチェック
        var objects = AssetDatabase.LoadAllAssetsAtPath(spritePath);
        if (objects.Length == 0)
        {
            // spriteが見つからなかった場合は作る
            var spriteGUIDArray = AssetDatabase.FindAssets("t:sprite", new string[] { targetFolder });
            var sprites = new List<Sprite>();
            foreach (var spriteGUID in spriteGUIDArray)
            {
                var tempPath = AssetDatabase.GUIDToAssetPath(spriteGUID);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(tempPath);
                sprites.Add(sprite);
            }
            if (sprites.Count == 0)
            {
                Debug.LogWarning($"sprite count is 0. any sprite not found.{targetFolder}");
                return;
            }
            // 結合画像の作成
            CreateMergedTexture(sprites, spritePath);
            // 再読み込み
            objects = AssetDatabase.LoadAllAssetsAtPath(spritePath);
        }

        // Spriteの抽出
        Sprite defaultSprite = null;
        List<Sprite> spriteList = new List<Sprite>();
        foreach (var obj in objects)
        {
            var sprite = obj as Sprite;
            if (sprite == null)
            {
                continue;
            }
            // defaultSpriteはサムネイルとして別で抽出
            if (sprite.name == "defaultSprite")
            {
                defaultSprite = sprite;
            }
            spriteList.Add(sprite);
        }

        tileDataModel.m_DefaultSprite = defaultSprite;
        tileDataModel.tileImageDataModel.texture = defaultSprite.texture;
        // 各タイルが持っているスプライトをピックアップ
        tileDataModel.m_TilingRules.ForEach(tr =>
        {
            for (int i = 0; i < tr.m_Sprites.Length; i++)
            {
                if (tr.m_Sprites[i] == null)
                {
                    continue;
                }
                // 該当するものをテクスチャ内のスプライトから検出して差し替え
                // 名前でマッチング
                var sprite = spriteList.FirstOrDefault(sp =>
                {
                    return sp.name == tr.m_Sprites[i].name;
                });
                if (sprite == null)
                {
                    continue;
                }
                tr.m_Sprites[i] = sprite;
            }
        });
        EditorUtility.SetDirty(tileDataModel);
        // 置換後に個別のタイルを削除
        DeleteFolderAndMeta(targetFolder);

        // 一応
        GC.Collect();
        Resources.UnloadUnusedAssets();

    }

    /// <summary>
    /// 結合したテクスチャを作成して指定パスに保存する
    /// </summary>
    /// <param name="relativeRootPath"></param>
    /// <param name="tileAssetName"></param>
    static void CreateMergedTexture(List<Sprite> spriteList, string spritePath) {

        var spriteMetaDatList = new List<SpriteMetaData>();
        var textureList = new List<Texture2D>();
        spriteList.ForEach(sprite => textureList.Add(CreateBorderdTexture(sprite.texture)));

        // 結合後テクスチャの生成
        var combinedTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        // テクスチャ結合の実行
        var rect = combinedTexture.PackTextures(textureList.ToArray(), 2, 8192, false);
        for (int i = 0; i < rect.Length; i++)
        {
            var spriteMetaData = new SpriteMetaData();
            spriteMetaData.name = spriteList[i].name;
            var x = rect[i].x * combinedTexture.width;
            var y = rect[i].y * combinedTexture.height;
            var w = rect[i].width * combinedTexture.width;
            var h = rect[i].height * combinedTexture.height;

            // 拡大した際の影響が出るためテクスチャに対してスプライトは小さくする
            if (w == 98 && (h == 98 || h == 114))
            {
                x += 1;
                y += 1;
                w -= 2;
                h -= 2;
            }
            spriteMetaData.rect = new Rect(x, y, w, h);
            spriteMetaData.border = Vector4.one;
            spriteMetaDatList.Add(spriteMetaData);
        }
        // 新規テクスチャの保存先指定
        var texturePath = Path.Combine(Application.dataPath.Replace("Assets", ""), spritePath);
        File.WriteAllBytes(texturePath, combinedTexture.EncodeToPNG());
        // 作成したテクスチャをAssetDatabaseに取り込む
        AssetDatabase.Refresh();
        // インポート情報の作成
        TextureImporter importer = (TextureImporter) TextureImporter.GetAtPath(spritePath);
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.sRGBTexture = true;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritesheet = spriteMetaDatList.ToArray();
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.spritePixelsPerUnit = 96;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.isReadable = false;
        AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceUpdate);
    }

    /// <summary>
    /// 画像の読み込み処理
    /// テクスチャにRead/Write属性を付けないで操作するため複製を作る
    /// Spriteを等倍でマルチ化すると解像度差でちょくちょく隙間ができるため、外周を１ドット拡大する
    /// </summary>
    /// <param name="texture"></param>
    /// <returns></returns>
    static Texture2D CreateBorderdTexture(Texture2D texture) {
        // テクスチャのRead/Writeを変更できないので複製を作成する
        var copyTexture = new Texture2D(texture.width, texture.height, texture.format, false, false);
        // テクスチャのコピー
        Graphics.CopyTexture(texture, copyTexture);

        // 拡張後ボーターサイズ
        var borderedSize = new Vector2Int(texture.width + 2, texture.height + 2);
        // 結果テクスチャ
        var resultTexture = new Texture2D(borderedSize.x, borderedSize.y, TextureFormat.RGBA32, false);
        // テクスチャのカラー抽出
        resultTexture.SetPixels(1, 1, texture.width, texture.height, copyTexture.GetPixels(0, 0, texture.width, texture.height));
        // 上端と下端
        resultTexture.SetPixels(1, 0, texture.width, 1, copyTexture.GetPixels(0, 0, texture.width, 1));
        resultTexture.SetPixels(1, borderedSize.y - 1, texture.width, 1, copyTexture.GetPixels(0, texture.height - 1, texture.width, 1));
        // 左端と右端
        resultTexture.SetPixels(0, 1, 1, texture.height, copyTexture.GetPixels(0, 0, 1, texture.height));
        resultTexture.SetPixels(borderedSize.x - 1, 1, 1, texture.height, copyTexture.GetPixels(texture.width - 1, 0, 1, texture.height));
        // ４隅
        resultTexture.SetPixel(0, 0, copyTexture.GetPixel(0, 0));
        resultTexture.SetPixel(0, borderedSize.y - 1, copyTexture.GetPixel(0, texture.height - 1));
        resultTexture.SetPixel(borderedSize.x - 1, 0, copyTexture.GetPixel(texture.width - 1, 0));
        resultTexture.SetPixel(borderedSize.x - 1, borderedSize.y - 1, copyTexture.GetPixel(texture.width - 1, texture.height - 1));
        return resultTexture;
    }

    /// <summary>
    /// 指定フォルダとmetaの削除
    /// </summary>
    /// <param name="targetFolder"></param>
    static void DeleteFolderAndMeta(string targetFolder) {
        // 置換後に個別のタイルを削除
        if (Directory.Exists(targetFolder))
        {
            Directory.Delete(targetFolder, true);
        }
        var metaFile = targetFolder + ".meta";
        if (File.Exists(metaFile))
        {
            File.Delete(metaFile);
        }
    }

    static void ShrinkSpriteBorderSize98to96() {

        //var result = EditorUtility.DisplayDialog(EditorLocalize.LocalizeText("WORD_1649"), EditorLocalize.LocalizeText("WORD_1650"), "OK", "Cancel");
        //if (result != true)
        //{
        //    return;
        //}

        // 指定フォルダ以下のスプライトを読み込む
        var tileDataModelGUIDs = AssetDatabase.FindAssets("t:Texture2D", new string[] { "Assets/RPGMaker/Storage/Map/TileAssets" });

        AssetDatabase.StartAssetEditing();

        try
        {
            foreach (var guid in tileDataModelGUIDs)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer == null)
                {
                    continue;
                }
                if (importer.spriteImportMode != SpriteImportMode.Multiple)
                {
                    continue;
                }
                importer.textureType = TextureImporterType.GUI;                        //テクスチャモードをスプライトに設定
                importer.textureType = TextureImporterType.Sprite;                        //テクスチャモードをスプライトに設定
                importer.spriteImportMode = SpriteImportMode.Multiple;

                //importer.isReadable = true;
                var metaData = importer.spritesheet;
                for (var index = 0; index < metaData.Length; index++)
                {
                    if (metaData[index].rect.width == 98 &&
                        (metaData[index].rect.height == 98 || metaData[index].rect.height == 114))
                    {
                        metaData[index].rect = new Rect(
                            metaData[index].rect.x + 1,
                            metaData[index].rect.y + 1,
                            metaData[index].rect.width - 2,
                            metaData[index].rect.height - 2);
                    }
                }
                importer.spritesheet = metaData;

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        // 一応
        GC.Collect();
        Resources.UnloadUnusedAssets();
    }
}