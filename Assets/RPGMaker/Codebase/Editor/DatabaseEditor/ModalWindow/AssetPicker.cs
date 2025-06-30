using System.Collections.Generic;
using System.Threading.Tasks;
using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.AssetManage;
using RPGMaker.Codebase.CoreSystem.Knowledge.Enum;
using RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement.Repository;
using RPGMaker.Codebase.Editor.Common;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGMaker.Codebase.Editor.DatabaseEditor.ModalWindow
{
    public class AssetPicker : ImagePicker
    {
        public enum CharacterType
        {
            Map = 0,
            Battle
        }

        private const int TypeMoveCharacter = 0;
        private const int TypeObject = 1;
        private const int TypeSvBattleCharacter = 3;

        public bool ObjectOnly { get; set; }
        public CharacterType CharacterSdType { get; set; } = CharacterType.Map;

        protected string[] AssetIds;

        public static AssetPicker Instantiate(
            bool addNone = true,
            string noneText = "WORD_0113",
            bool isObjectOnly = false
        )
        {
            var ins = CreateInstance<AssetPicker>();
            ins.AddNone = addNone;
            ins.NoneText = noneText;
            ins.ObjectOnly = isObjectOnly;

            return ins;
        }

        protected override async Task SelectImage()
        {
            while (!ContainerInitialized)
            {
                await Task.Delay(1);
            }

            if (!string.IsNullOrEmpty(CurrentSelectedName))
            {
                var index = -1;
                for (var i = 0; i < ItemNames.Length; i++)
                {
                    if (AssetIds[i] == CurrentSelectedName)
                    {
                        index = i;
                        break;
                    }
                }

                if (index >= 0)
                {
                    CurrentSelectedIndex = index;
                    Texture2D texture = LoadTexture(index);
                    PreviewImage.style.backgroundImage = texture;
                    PreviewName.text = ItemNames[index];
                    PreviewImageSize.text = $"{texture.width}x{texture.height}";
                    var pickerItem = ItemContainer[index].Q<VisualElement>("Wrapper");
                    pickerItem.EnableInClassList("--selected", true);

                    ToScroll(index);
                }
            }
        }

        protected override void InitData()
        {
            var order = AssetManageRepository.OrderManager.Load();
            List<AssetManageDataModel> assetManageData = new List<AssetManageDataModel>();
            var databaseManagementService = Hierarchy.Hierarchy.databaseManagementService;
            var manageData = databaseManagementService.LoadAssetManage();
            List<AssetCategoryEnum> categories;
            if (ObjectOnly)
            {
                categories = new List<AssetCategoryEnum>()
                    { AssetCategoryEnum.OBJECT };
            }
            else if (CharacterSdType == CharacterType.Map)
            {
                categories = new List<AssetCategoryEnum>()
                    { AssetCategoryEnum.MOVE_CHARACTER, AssetCategoryEnum.OBJECT };
            }
            else
            {
                categories = new List<AssetCategoryEnum>()
                    { AssetCategoryEnum.SV_BATTLE_CHARACTER };
            }

            foreach (var orderData in order.orderDataList)
            {
                if (orderData.idList == null) continue;
                if (!categories.Contains((AssetCategoryEnum)orderData.assetTypeId)) continue;

                foreach (var id in orderData.idList)
                {
                    foreach (var data in manageData)
                    {
                        if (data.id == id) assetManageData.Add(data);
                    }
                }
            }

            var tempAssetNames = new List<string>();
            var tempPaths = new List<string>();
            var tempIds = new List<string>();

            if (AddNone)
            {
                tempAssetNames.Add(EditorLocalize.LocalizeText(NoneText));
                tempPaths.Add("");
                tempIds.Add("");
            }

            foreach (var asset in assetManageData)
            {
                string path;
                switch (asset.assetTypeId)
                {
                    case TypeMoveCharacter:
                        path = PathManager.IMAGE_CHARACTER;
                        break;
                    case TypeObject:
                        path = PathManager.IMAGE_OBJECT;
                        break;
                    case TypeSvBattleCharacter:
                        path = PathManager.IMAGE_SV_CHARACTER;
                        break;
                    default:
                        throw new System.Exception("Invalid asset type");
                }

                tempAssetNames.Add(asset.name);
                tempPaths.Add(path + asset.imageSettings[0].path);
                tempIds.Add(asset.id);
            }

            ItemNames = tempAssetNames.ToArray();
            Paths = tempPaths.ToArray();
            AssetIds = tempIds.ToArray();
        }

        protected override void UpdateSelectedItem()
        {
            base.UpdateSelectedItem();

            CurrentSelectedName = AssetIds[CurrentSelectedIndex];
        }
    }
}