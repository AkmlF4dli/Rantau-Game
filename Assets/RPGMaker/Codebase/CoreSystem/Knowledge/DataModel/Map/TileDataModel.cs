using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.Enum;
using RPGMaker.Codebase.CoreSystem.Service.MapManagement;
using UnityEngine;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Map
{
    [Serializable]
    public class TileDataModel : RuleTile
    {
        public enum DamageFloorType
        {
            Fix,
            Rate,
            None
        }

        public enum ImageAdjustType
        {
            Scale,
            Split,
            None
        }

        public enum PassType
        {
            CanPassNormally,
            CanPassUnder,
            CannotPass
        }

        // local class / enum
        //-------------------------------------------------------------------------------------------------------
        public enum Type
        {
            AutoTileA, // 地面型オートタイル 長方形
            AutoTileB, // 建物型オートタイル 正方形
            AutoTileC,
            Effect,
            NormalTile,
            Region,
            BackgroundCollision,
            LargeParts
        }

        public const int             TileSize = 96;
        public const int             LargePartsTileSize = 96;
        public       int             animationFrame;
        public       int             animationSpeed;
        public       DamageFloorType damageFloorType;
        public       float           damageFloorValue;
        public       int             terrainTagValue;
        public       string          filename;
        public       bool            hasAnimation;

        public     string              id;
        public     ImageAdjustType     imageAdjustType;
        public     bool                isBush;
        public     bool                isCounter;
        public     bool                isDamageFloor;
        public     bool                isLadder;
        public     bool                isPassBottom;
        public     bool                isPassLeft;
        public     bool                isPassRight;
        public     bool                isPassTop;
        public     LargePartsDataModel largePartsDataModel;
        public new string              name;
        public     PassType            passType = PassType.CannotPass;
        public     int                 regionId;

        // WithSerialNumberDataModelを多重継承できないため致し方なく直接定義
        public int                serialNumber;
        public TileImageDataModel tileImageDataModel;
        public Type               type;
        public List<VehicleType>  vehicleTypes;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public string SerialNumberString => GetTileDataModelInfo() == null ? "00000" : GetTileDataModelInfo()?.listNumber.ToString("00000");
#else
        public async Task<string> SerialNumberString() {
            var tileDataModelInfo = await GetTileDataModelInfo();
            return tileDataModelInfo == null ? "00000" : tileDataModelInfo?.listNumber.ToString("00000");
        }
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public TileDataModelInfo tileDataModelInfo => GetTileDataModelInfo();
#else
        public async Task<TileDataModelInfo> tileDataModelInfo() {
            return await GetTileDataModelInfo();
        }
#endif

        /**
         * 通れるか？ (vehicleIdがnull以外の場合は乗り物判定)
         */
        private bool CanStepOn(CharacterMoveDirectionEnum moveDirectionEnum, string vehicleId = null) {
            return vehicleId != null ?
                VehiclePassTypeIs(vehicleId, PassType.CanPassNormally) :
                passType == PassType.CanPassNormally && IsAllowedDirection(moveDirectionEnum);
        }

        /**
         * 下を潜って通れるか？ (vehicleIdがnull以外の場合は乗り物判定)
         */
        public bool CanGoThrough(CharacterMoveDirectionEnum moveDirectionEnum, string vehicleId = null) {
            return vehicleId != null ?
                VehiclePassTypeIs(vehicleId, PassType.CanPassUnder) :
                passType == PassType.CanPassUnder && IsAllowedDirection(moveDirectionEnum);
        }

        /**
         * 通れないか？ (vehicleIdがnull以外の場合は乗り物判定)
         */
        public bool CannotEnter(CharacterMoveDirectionEnum moveDirectionEnum, string vehicleId = null) {
            return !(CanStepOn(moveDirectionEnum, vehicleId) || CanGoThrough(moveDirectionEnum, vehicleId));
        }

        /**
         * 許可された移動方向か
         */
        private bool IsAllowedDirection(CharacterMoveDirectionEnum moveDirectionEnum) {
            return moveDirectionEnum switch
            {
                CharacterMoveDirectionEnum.Up => isPassBottom, // 上向きでの進入 = 下からの進入
                CharacterMoveDirectionEnum.Down => isPassTop, // 下向きでの進入 = 上からの進入
                CharacterMoveDirectionEnum.Left => isPassRight, // 左向きでの進入 = 右からの進入
                CharacterMoveDirectionEnum.Right => isPassLeft, // 右向きでの進入 = 左からの進入
                _ => throw new ArgumentOutOfRangeException(nameof(moveDirectionEnum), moveDirectionEnum, null)
            };
        }


        //乗り物判定----------------------------------------------------------------------------------------------------------------------

        /**
         * 指定の通行設定か？
         */
        private bool VehiclePassTypeIs(string vehicleId, PassType passType)
        {
            var vehicleType = vehicleTypes.FirstOrDefault(vehicleType => vehicleType.vehicleId == vehicleId);
            return vehicleType?.vehiclePassType == passType;
        }

        //他----------------------------------------------------------------------------------------------------------------------

        /**
         * 大型パーツの子タイルを新規作成する
         */
        public static TileDataModel CreateLargePartsChildTileDataModel(TileDataModel parentTileDataModel, int num = 0) {
            DebugUtil.Assert(parentTileDataModel.type == Type.LargeParts);

            var tileEntity = CreateInstance<TileDataModel>();

            // TileDataModelの全フィールド値をコピー (継承元のフィールド含まず)。
            var baseTypeFieldNames = typeof(TileDataModel).BaseType.GetFields().Select(f => f.Name).ToList();
            foreach (var field in
                typeof(TileDataModel).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                // 継承元の型に同名フィールドがあれば対象外。
                if (baseTypeFieldNames.Contains(field.Name)) continue;

                DebugUtil.Log($"field.Name={field.Name}");
                field.SetValue(tileEntity, field.GetValue(parentTileDataModel));
            }

            // string以外の参照型のフィールドにインスタンス生成した値を代入。
            tileEntity.tileImageDataModel = new TileImageDataModel(
                parentTileDataModel.tileImageDataModel.texture, parentTileDataModel.tileImageDataModel.filename);
            tileEntity.vehicleTypes = new List<VehicleType>(parentTileDataModel.vehicleTypes);
            tileEntity.largePartsDataModel =
                parentTileDataModel.largePartsDataModel != null
                    ? new LargePartsDataModel(
                        parentTileDataModel.largePartsDataModel.parentId,
                        parentTileDataModel.largePartsDataModel.x,
                        parentTileDataModel.largePartsDataModel.y)
                    : null;

            // idを設定。
            tileEntity.id = Guid.NewGuid().ToString();
            tileEntity.serialNumber += num;

            return tileEntity;
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private TileDataModelInfo GetTileDataModelInfo() {
#else
        private async Task<TileDataModelInfo> GetTileDataModelInfo() {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var tiles = new MapManagementService().LoadTileTable();
#else
            var tiles = await new MapManagementService().LoadTileTable();
#endif
            for (int i = 0; i < tiles.Count; i++)
                if (tiles[i].id == id)
                    return tiles[i];

            var info = new TileDataModelInfo();
            info.id = id;
            info.type = type;
            info.largePartsDataModel = largePartsDataModel;
            return info;
        }

        public bool isEqual(TileDataModel data) {
            if (animationFrame != data.animationFrame ||
                animationSpeed != data.animationSpeed ||
                damageFloorType != data.damageFloorType ||
                damageFloorValue != data.damageFloorValue ||
                terrainTagValue != data.terrainTagValue ||
                filename != data.filename ||
                hasAnimation != data.hasAnimation ||
                id != data.id ||
                imageAdjustType != data.imageAdjustType ||
                isBush != data.isBush ||
                isCounter != data.isCounter ||
                isDamageFloor != data.isDamageFloor ||
                isLadder != data.isLadder ||
                isPassBottom != data.isPassBottom ||
                isPassLeft != data.isPassLeft ||
                isPassRight != data.isPassRight ||
                isPassTop != data.isPassTop ||
                largePartsDataModel != null && !largePartsDataModel.isEqual(data.largePartsDataModel) ||
                name != data.name ||
                passType != data.passType ||
                regionId != data.regionId ||
                serialNumber != data.serialNumber ||
                type != data.type)
                return false;

            if (vehicleTypes.Count != data.vehicleTypes.Count)
                return false;

            for (int i = 0; i < vehicleTypes.Count; i++)
                if (!vehicleTypes[i].isEqual(data.vehicleTypes[i]))
                    return false;
 
            return true;
        }
    }

    [Serializable]
    public class TileDataModelInfo
    {
        // local class / enum
        //-------------------------------------------------------------------------------------------------------
        public string id;
        public LargePartsDataModel largePartsDataModel;

        public int serialNumber;
        public int listNumber;
        public TileDataModel.Type type;
        public int regionId;

        private TileDataModel tileDataModel = null;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public TileDataModel TileDataModel => GetTileDataModel();
#else
        public async Task<TileDataModel> TileDataModel() { return await GetTileDataModel(); }
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private TileDataModel GetTileDataModel() {
#else
        private async Task<TileDataModel> GetTileDataModel() {
#endif
            if (tileDataModel != null) return tileDataModel;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            tileDataModel = new MapManagementService().LoadTile(this);
#else
            tileDataModel = await new MapManagementService().LoadTile(this);
#endif
            return tileDataModel;
        }
        public bool isLoadedTileDataModel() {
            return tileDataModel != null;
        }
    }
}