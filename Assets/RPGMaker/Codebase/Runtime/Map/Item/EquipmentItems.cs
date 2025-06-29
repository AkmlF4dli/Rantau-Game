using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Armor;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.SystemSetting;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Weapon;
using RPGMaker.Codebase.Runtime.Common;
using RPGMaker.Codebase.Runtime.Map.Menu;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TextMP = TMPro.TextMeshProUGUI;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Map.Item
{
    /// <summary>
    /// 装備アイテム
    /// </summary>
    public class EquipmentItems : MonoBehaviour
    {
        /// <summary>
        /// 説明文
        /// </summary>
        [SerializeField] private GameObject _descriptionText;
        /// <summary>
        /// 装備メニュー
        /// </summary>
        private EquipMenu _equipMenu;
        /// <summary>
        /// アイコン
        /// </summary>
        private Image  _icon;
        /// <summary>
        /// 名前
        /// </summary>
        private TextMP _name;
        /// <summary>
        /// 種別
        /// </summary>
        private Text _type;
        /// <summary>
        /// 所持数
        /// </summary>
        private TextMP _value;
        /// <summary>
        /// 装備アイテムID
        /// </summary>
        public string EquipId { get; private set; } = "";
        /// <summary>
        /// 説明文
        /// </summary>
        public string EquipsMessage { get; private set; }

        /// <summary>
        /// 現在の装備設定
        /// </summary>
        /// <param name="equipType"></param>
        /// <param name="runtimeActorDataModel"></param>
        /// <param name="equipMenu"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void NowEquips(
#else
        public async Task NowEquips(
#endif
            SystemSettingDataModel.EquipType equipType,
            RuntimeActorDataModel runtimeActorDataModel,
            EquipMenu equipMenu,
            int equipIndex
        ) {
            if(GetComponent<WindowButtonBase>() != null)
                GetComponent<WindowButtonBase>().SetRaycastTarget(true);
            _equipMenu = equipMenu;
            _type = transform.Find("Type")?.GetComponent<Text>();
            _name = transform.Find("Name").GetComponent<TextMP>();
            _icon = transform.Find("Icon").GetComponent<Image>();

            //左側の表示
            if (_type != null)
                _type.text = equipType.name;

            var id = runtimeActorDataModel.equips[equipIndex].itemId;

            EquipId = equipType.id;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            if (SetWeapon(id)) return;
            if (SetArmor(id)) return;
#else
            if (await SetWeapon(id)) return;
            if (await SetArmor(id)) return;
#endif
        }

        /// <summary>
        /// 武器設定
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private bool SetWeapon(string id) {
#else
        private async Task<bool> SetWeapon(string id) {
#endif
            _name.text = "";
            _icon.enabled = true;
            var weaponDataModels = DataManager.Self().GetWeaponDataModels();
            foreach (var t in weaponDataModels)
                if (t.basic.id == id)
                {
                    _name.text = t.basic.name;
                    EquipsMessage = t.basic.description;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _icon.sprite = GetItemImage(t.basic.iconId);
#else
                    _icon.sprite = await GetItemImage(t.basic.iconId);
#endif

                    var posX = 0f;
                    _icon.transform.localPosition = new Vector2(_icon.transform.localPosition.x + posX,
                        _icon.transform.localPosition.y);
                    _name.transform.localPosition = new Vector2(_name.transform.localPosition.x + posX,
                        _name.transform.localPosition.y);
                    if (_equipMenu != null) _equipMenu.StatusPlus(t.parameters);
                    return true;
                }

            //アイテム未所持の場合
            _name.text = "";
            EquipsMessage = "";
            _icon.enabled = false;

            return false;
        }

        /// <summary>
        /// 防具設定
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private bool SetArmor(string id) {
#else
        private async Task<bool> SetArmor(string id) {
#endif
            _name.text = "";
            _icon.enabled = true;
            var armorDataModels = DataManager.Self().GetArmorDataModels();
            foreach (var t in armorDataModels)
                if (t.basic.id == id)
                {
                    _name.text = t.basic.name;
                    EquipsMessage = t.basic.description;

                    var posX = 0f;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _icon.sprite = GetItemImage(t.basic.iconId);
#else
                    _icon.sprite = await GetItemImage(t.basic.iconId);
#endif

                    _icon.transform.localPosition = new Vector2(_icon.transform.localPosition.x + posX,
                        _icon.transform.localPosition.y);
                    _name.transform.localPosition = new Vector2(_name.transform.localPosition.x + posX,
                        _name.transform.localPosition.y);
                    if (_equipMenu != null) _equipMenu.StatusPlus(t.parameters);
                    return true;
                }

            _name.text = "";
            EquipsMessage = "";
            _icon.enabled = false;
            return false;
        }


        /// <summary>
        /// 装備タイプ名の取得
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private string _EquipTypeName(string id) {
            var systemData = DataManager.Self().GetSystemDataModel();
            for (var i = 0; i < systemData.weaponTypes.Count; i++)
                if (systemData.weaponTypes[i].id == id)
                    return systemData.weaponTypes[i].value;

            for (var i = 0; i < systemData.armorTypes.Count; i++)
                if (systemData.armorTypes[i].id == id)
                    return systemData.armorTypes[i].name;

            return "";
        }

        /// <summary>
        /// 所持しているアイテムの描画
        /// </summary>
        /// <param name="data"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void EquipWindow(List<string> data) {
#else
        public async Task EquipWindow(List<string> data) {
#endif
            _name = transform.Find("Name").GetComponent<TextMP>();
            _icon = transform.Find("Icon").GetComponent<Image>();
            _value = transform.Find("Value").GetComponent<TextMP>();

            if (data[1] != null && data[2] != null)
            {
                _name.text = data[1];
                _value.text = data[2];
                transform.Find("Space").GetComponent<TextMP>().enabled = true;
            }
            else
            {
                _name.text = "";
                _value.text = "";
                transform.Find("Space").GetComponent<TextMP>().enabled = false;
            }

            var weapons = DataManager.Self().GetWeaponDataModels();
            WeaponDataModel weaponDataModel = null;
            for (int i = 0; i < weapons.Count; i++)
                if (weapons[i].basic.id == data[0])
                {
                    weaponDataModel = weapons[i];
                    break;
                }

            if (weaponDataModel == null)
            {
                var armors = DataManager.Self().GetArmorDataModels();
                ArmorDataModel armorDataModel = null;
                for (int i = 0; i < armors.Count; i++)
                    if (armors[i].basic.id == data[0])
                    {
                        armorDataModel = armors[i];
                        break;
                    }

                if (armorDataModel != null)
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _icon.sprite = GetItemImage(armorDataModel.basic.iconId);
#else
                    _icon.sprite = await GetItemImage(armorDataModel.basic.iconId);
#endif
                }
                else
                {
                    _icon.gameObject.SetActive(false);
                }
                
            }
            else
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _icon.sprite = GetItemImage(weaponDataModel.basic.iconId);
#else
                _icon.sprite = await GetItemImage(weaponDataModel.basic.iconId);
#endif
                
            }
        }

        /// <summary>
        /// アイコン取得
        /// </summary>
        /// <param name="iconName"></param>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public Sprite GetItemImage(string iconName) {
#else
        public async Task<Sprite> GetItemImage(string iconName) {
#endif
            var iconSetTexture =
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Texture2D>(
#else
                await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Texture2D>(
#endif
                    "Assets/RPGMaker/Storage/Images/System/IconSet/" + iconName + ".png");

            var iconTexture = iconSetTexture;
            if (iconTexture == null)
            {
                _icon.gameObject.SetActive(false);
                return null;
            }

            var sprite = ImageUtility.SpriteCreate(
                iconTexture,
                new Rect(0, 0, iconTexture.width, iconTexture.height),
                new Vector2(0.5f, 0.5f)
            );
            
            var aspect = ImageManager.FixAspect( new Vector2(66f,66f), new Vector2(iconTexture.width, iconTexture.height));
            var aspectRatio = _icon.GetComponent<AspectRatioFitter>();
            if (aspectRatio == null)
            {
                aspectRatio = _icon.gameObject.AddComponent<AspectRatioFitter>();
                aspectRatio.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;

            }
            aspectRatio.aspectRatio = aspect;

            return sprite;
        }
    }
}