using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Armor;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Class;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Weapon;
using RPGMaker.Codebase.CoreSystem.Knowledge.Enum;
using RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement;
using RPGMaker.Codebase.Runtime.Common;
using RPGMaker.Codebase.Runtime.Map.Component.Character;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TextMP = TMPro.TextMeshProUGUI;

namespace RPGMaker.Codebase.Runtime.Map.Item
{
    public class EquipmentProfile : MonoBehaviour
    {
        private Image                     _body;
        private ClassDataModel            _classDataModel;
        private List<ClassDataModel>      _classDataModels;
        private DatabaseManagementService _databaseManagementService;
        private TextMP                    _EqValueAg;

        //装備品の加算部部分
        private TextMP _EqValueAt;
        private TextMP _EqValueDf;
        private TextMP _EqValueLu;
        private TextMP _EqValueMa;
        private TextMP _EqValueMd;

        private Image _face;

        //キャラクター情報の部分
        private TextMP _name;

        //データ部分
        private RuntimeActorDataModel _runtimeActorDataModel;
        private Text                  _StNameAg;

        //ステータスの項目名の部分
        private Text   _StNameAt;
        private Text   _StNameDf;
        private Text   _StNameLu;
        private Text   _StNameMa;
        private Text   _StNameMd;
        private TextMP _StValueAg;

        //ステータス部分
        private TextMP _StValueAt;
        private TextMP _StValueDf;
        private TextMP _StValueLu;
        private TextMP _StValueMa;
        private TextMP _StValueMd;

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ChangeCharactor(RuntimeActorDataModel runtimeActorDataModelInformation) {
#else
        public async Task ChangeCharactor(RuntimeActorDataModel runtimeActorDataModelInformation) {
#endif
            _runtimeActorDataModel = runtimeActorDataModelInformation;
            _databaseManagementService = new DatabaseManagementService();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _classDataModels = _databaseManagementService.LoadClassCommon();
#else
            _classDataModels = await _databaseManagementService.LoadClassCommon();
#endif

            //表示中のアクターの
            foreach (var classDataModel in _classDataModels)
                if (_runtimeActorDataModel.classId == classDataModel.id)
                {
                    _classDataModel = classDataModel;
                    break;
                }

            _name = transform.Find("Name").GetComponent<TextMP>();

            if (transform.Find("Face") != null)
                _face = transform.Find("Face").GetComponent<Image>();
            // 立ち絵
            if (transform.Find("body") != null) 
                _body = transform.Find("body/Image").GetComponent<Image>();

            _StValueAt = transform.Find("StatusValues/Attack/Value").GetComponent<TextMP>();
            _StValueDf = transform.Find("StatusValues/Defense/Value").GetComponent<TextMP>();
            _StValueMa = transform.Find("StatusValues/Magic/Value").GetComponent<TextMP>();
            _StValueMd = transform.Find("StatusValues/MagicDefense/Value").GetComponent<TextMP>();
            _StValueAg = transform.Find("StatusValues/Agility/Value").GetComponent<TextMP>();
            _StValueLu = transform.Find("StatusValues/Luck/Value").GetComponent<TextMP>();
            _EqValueAt = transform.Find("EquipmentValues/Attack/AttackValue").GetComponent<TextMP>();
            _EqValueDf = transform.Find("EquipmentValues/Defense/DefenseValue").GetComponent<TextMP>();
            _EqValueMa = transform.Find("EquipmentValues/Magic/MagicValue").GetComponent<TextMP>();
            _EqValueMd = transform.Find("EquipmentValues/MagicDefense/MagicDefenseValue").GetComponent<TextMP>();
            _EqValueAg = transform.Find("EquipmentValues/Agility/AgilityValue").GetComponent<TextMP>();
            _EqValueLu = transform.Find("EquipmentValues/Luck/LuckValue").GetComponent<TextMP>();
            _StNameAt = transform.Find("StatusValues/Attack").GetComponent<Text>();
            _StNameDf = transform.Find("StatusValues/Defense").GetComponent<Text>();
            _StNameMa = transform.Find("StatusValues/Magic").GetComponent<Text>();
            _StNameMd = transform.Find("StatusValues/MagicDefense").GetComponent<Text>();
            _StNameAg = transform.Find("StatusValues/Agility").GetComponent<Text>();
            _StNameLu = transform.Find("StatusValues/Luck").GetComponent<Text>();

            //各項目の有効化切り替え
            transform.Find("StatusValues/Magic").transform.gameObject
                .SetActive(_classDataModel.basic.abilityEnabled.magicAttack == 1);
            transform.Find("EquipmentValues/Magic").transform.gameObject
                .SetActive(_classDataModel.basic.abilityEnabled.magicAttack == 1);
            transform.Find("StatusValues/MagicDefense").transform.gameObject
                .SetActive(_classDataModel.basic.abilityEnabled.magicDefense == 1);
            transform.Find("EquipmentValues/MagicDefense").transform.gameObject
                .SetActive(_classDataModel.basic.abilityEnabled.magicDefense == 1);
            transform.Find("StatusValues/Agility").transform.gameObject
                .SetActive(_classDataModel.basic.abilityEnabled.speed == 1);
            transform.Find("EquipmentValues/Agility").transform.gameObject
                .SetActive(_classDataModel.basic.abilityEnabled.speed == 1);
            transform.Find("StatusValues/Luck").transform.gameObject
                .SetActive(_classDataModel.basic.abilityEnabled.luck == 1);
            transform.Find("EquipmentValues/Luck").transform.gameObject
                .SetActive(_classDataModel.basic.abilityEnabled.luck == 1);

            EqValueInit();

            //名前と顔の変更
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _SetActor(_runtimeActorDataModel);
#else
            await _SetActor(_runtimeActorDataModel);
#endif
            //ココからステータスの表示
            StValuesDisplay();
            //ステータスの項目名の表示
            StNameDisplay();
        }

        //どのキャラクターを表示させるか
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void _SetActor(RuntimeActorDataModel runtimeActorDataModelInformation) {
#else
        private async Task _SetActor(RuntimeActorDataModel runtimeActorDataModelInformation) {
#endif
            _runtimeActorDataModel = runtimeActorDataModelInformation;
            int characterType = DataManager.Self().GetUiSettingDataModel().commonMenus[0].characterType;
            //名前の表示
            _name.text = _runtimeActorDataModel.name;
            //顔の画像の表示
            //顔アイコン、立ち絵、SDキャラクターの設定値に応じて、表示するものを変更する
            //双方の部品が存在する場合は、顔アイコン,SDキャラと立ち絵画像で、表示する部品を変更する
            if (_face != null && _body != null)
            {
                if (characterType == (int) MenuIconTypeEnum.FACE)
                {
                    //顔アイコン
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    var sprite = UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(
#else
                    var sprite = await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(
#endif
                        "Assets/RPGMaker/Storage/Images/Faces/" +
                        _runtimeActorDataModel.faceImage + ".png");
                    _face.enabled = true;
                    _face.sprite = sprite;
                    _face.material = null;
                    _face.color = Color.white;
                    _face.preserveAspect = true;

                    _face.gameObject.SetActive(true);
                    _body.gameObject.SetActive(false);
                }
                else if (characterType == (int) MenuIconTypeEnum.SD)
                {
                    //SDキャラ
                    var assetId = DataManager.Self().GetActorDataModel(_runtimeActorDataModel.actorId).image.character;
                    CharacterGraphic characterGraphic = new GameObject().AddComponent<CharacterGraphic>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    characterGraphic.Init(assetId);
#else
                    await characterGraphic.Init(assetId);
#endif

                    _face.enabled = true;
                    _face.sprite = characterGraphic.GetCurrentSprite();
                    _face.color = Color.white;
                    _face.material = characterGraphic.GetMaterial();
                    _face.transform.localScale = characterGraphic.GetSize();

                    if (_face.transform.localScale.x > 1.0f || _face.transform.localScale.y > 1.0f)
                    {
                        if (_face.transform.localScale.y > 1.0f)
                        {
                            _face.transform.localScale = new Vector2(_face.transform.localScale.x / _face.transform.localScale.y, 1.0f);
                        }
                        else
                        {
                            _face.transform.localScale = new Vector2(1.0f, _face.transform.localScale.y / _face.transform.localScale.x);
                        }
                    }

                    _face.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    characterGraphic.gameObject.SetActive(false);
                    //characterGameObjects.Add(characterGraphic.gameObject);

                    _face.gameObject.SetActive(true);
                    _body.gameObject.SetActive(false);
                }
                else if (characterType == (int) MenuIconTypeEnum.PICTURE)
                {
                    //立ち絵
                    var imageName = _runtimeActorDataModel.advImage.Contains(".png")
                        ? _runtimeActorDataModel.advImage
                        : _runtimeActorDataModel.advImage + ".png";
                    var path = "Assets/RPGMaker/Storage/Images/Pictures/" + imageName;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    var tex = UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(path);
#else
                    var tex = await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(path);
#endif

                    _body.enabled = true;
                    _body.sprite = tex;
                    _body.color = Color.white;
                    _body.preserveAspect = true;

                    _face.gameObject.SetActive(false);
                    _body.gameObject.SetActive(true);
                }
                else
                {
                    _face.gameObject.SetActive(false);
                    _body.gameObject.SetActive(false);
                }
            }
            //片方の部品しかない場合は、いずれのパターンでもその部品に描画する
            else if (_face != null)
            {
                if (characterType == (int) MenuIconTypeEnum.FACE)
                {
                    //顔アイコン
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    var sprite = UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(
#else
                    var sprite = await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(
#endif
                        "Assets/RPGMaker/Storage/Images/Faces/" +
                        _runtimeActorDataModel.faceImage + ".png");
                    _face.enabled = true;
                    _face.sprite = sprite;
                    _face.color = Color.white;
                    _face.preserveAspect = true;
                }
                else if (characterType == (int) MenuIconTypeEnum.SD)
                {
                    //SDキャラ
                    var assetId = DataManager.Self().GetActorDataModel(_runtimeActorDataModel.actorId).image.character;
                    CharacterGraphic characterGraphic = new GameObject().AddComponent<CharacterGraphic>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    characterGraphic.Init(assetId);
#else
                    await characterGraphic.Init(assetId);
#endif

                    _face.enabled = true;
                    _face.sprite = characterGraphic.GetCurrentSprite();
                    _face.color = Color.white;
                    _face.material = characterGraphic.GetMaterial();
                    _face.transform.localScale = characterGraphic.GetSize();

                    if (_face.transform.localScale.x > 1.0f || _face.transform.localScale.y > 1.0f)
                    {
                        if (_face.transform.localScale.y > 1.0f)
                        {
                            _face.transform.localScale = new Vector2(_face.transform.localScale.x / _face.transform.localScale.y, 1.0f);
                        }
                        else
                        {
                            _face.transform.localScale = new Vector2(1.0f, _face.transform.localScale.y / _face.transform.localScale.x);
                        }
                    }

                    _face.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    characterGraphic.gameObject.SetActive(false);
                    //characterGameObjects.Add(characterGraphic.gameObject);
                }
                else if (characterType == (int) MenuIconTypeEnum.PICTURE)
                {
                    //立ち絵
                    var imageName = _runtimeActorDataModel.advImage.Contains(".png")
                        ? _runtimeActorDataModel.advImage
                        : _runtimeActorDataModel.advImage + ".png";
                    var path = "Assets/RPGMaker/Storage/Images/Pictures/" + imageName;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    var tex = UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(path);
#else
                    var tex = await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(path);
#endif

                    _face.enabled = true;
                    _face.sprite = tex;
                    _face.color = Color.white;
                    _face.preserveAspect = true;
                }
                else
                    _face.enabled = false;
            }
            else if (_body != null)
            {
                if (characterType == (int) MenuIconTypeEnum.FACE)
                {
                    //顔アイコン
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    var sprite = UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(
#else
                    var sprite = await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(
#endif
                        "Assets/RPGMaker/Storage/Images/Faces/" +
                        _runtimeActorDataModel.faceImage + ".png");
                    _body.enabled = true;
                    _body.sprite = sprite;
                    _body.color = Color.white;
                    _body.preserveAspect = true;
                }
                else if (characterType == (int) MenuIconTypeEnum.SD)
                {
                    //SDキャラ
                    var assetId = DataManager.Self().GetActorDataModel(_runtimeActorDataModel.actorId).image.character;
                    CharacterGraphic characterGraphic = new GameObject().AddComponent<CharacterGraphic>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    characterGraphic.Init(assetId);
#else
                    await characterGraphic.Init(assetId);
#endif

                    _body.enabled = true;
                    _body.sprite = characterGraphic.GetCurrentSprite();
                    _body.color = Color.white;
                    _body.material = characterGraphic.GetMaterial();
                    _body.transform.localScale = characterGraphic.GetSize();

                    if (_body.transform.localScale.x > 1.0f || _body.transform.localScale.y > 1.0f)
                    {
                        if (_body.transform.localScale.y > 1.0f)
                        {
                            _body.transform.localScale = new Vector2(_body.transform.localScale.x / _body.transform.localScale.y, 1.0f);
                        }
                        else
                        {
                            _body.transform.localScale = new Vector2(1.0f, _body.transform.localScale.y / _body.transform.localScale.x);
                        }
                    }

                    _body.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    characterGraphic.gameObject.SetActive(false);
                }
                else if (characterType == (int) MenuIconTypeEnum.PICTURE)
                {
                    //立ち絵
                    var imageName = _runtimeActorDataModel.advImage.Contains(".png")
                        ? _runtimeActorDataModel.advImage
                        : _runtimeActorDataModel.advImage + ".png";
                    var path = "Assets/RPGMaker/Storage/Images/Pictures/" + imageName;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    var tex = UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(path);
#else
                    var tex = await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<Sprite>(path);
#endif

                    _body.enabled = true;
                    _body.sprite = tex;
                    _body.color = Color.white;
                    _body.preserveAspect = true;
                }
                else
                    _body.enabled = false;
            }
        }

        //元のステータス
        private void StValuesDisplay() {
            _StValueAt.text = _runtimeActorDataModel.GetCurrentAttack(_classDataModel).ToString();
            _StValueDf.text = _runtimeActorDataModel.GetCurrentDefense(_classDataModel).ToString();
            _StValueMa.text = _runtimeActorDataModel.GetCurrentMagicAttack(_classDataModel).ToString();
            _StValueMd.text = _runtimeActorDataModel.GetCurrentMagicDefense(_classDataModel).ToString();
            _StValueAg.text = _runtimeActorDataModel.GetCurrentAgility(_classDataModel).ToString();
            _StValueLu.text = _runtimeActorDataModel.GetCurrentLuck(_classDataModel).ToString();
        }

        //→のステータス
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void EqValuesDisplay(List<int> parameters, string typeId, int index) {
#else
        public async Task EqValuesDisplay(List<int> parameters, string typeId, int index) {
#endif
            // 装備中のアイテムを取得
            var equipParam = new List<int> {0, 0, 0, 0, 0, 0, 0, 0};
            if (_runtimeActorDataModel.equips[index].equipType == typeId)
            {
                if (_runtimeActorDataModel.equips[index].itemId != "")
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    var weapons = _databaseManagementService.LoadWeapon();
#else
                    var weapons = await _databaseManagementService.LoadWeapon();
#endif
                    WeaponDataModel weapon = null;
                    for (int i2 = 0; i2 < weapons.Count; i2++)
                        if (weapons[i2].basic.id == _runtimeActorDataModel.equips[index].itemId)
                        {
                            weapon = weapons[i2];
                            break;
                        }

                    if (weapon == null)
                    {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        var armors = _databaseManagementService.LoadArmor();
#else
                        var armors = await _databaseManagementService.LoadArmor();
#endif
                        ArmorDataModel armor = null;
                        for (int i2 = 0; i2 < armors.Count; i2++)
                            if (armors[i2].basic.id == _runtimeActorDataModel.equips[index].itemId)
                            {
                                armor = armors[i2];
                                break;
                            }

                        equipParam = armor.parameters;
                    }
                    else
                    {
                        equipParam = weapon.parameters;
                    }
                }
            }

            _EqValueAt.enabled = true;
            _EqValueDf.enabled = true;
            _EqValueMa.enabled = true;
            _EqValueMd.enabled = true;
            _EqValueAg.enabled = true;
            _EqValueLu.enabled = true;

            if (parameters == null)
            {
                _EqValueAt.text = (int.Parse(_StValueAt.text) - equipParam[2]).ToString();
                _EqValueDf.text = (int.Parse(_StValueDf.text) - equipParam[3]).ToString();
                _EqValueMa.text = (int.Parse(_StValueMa.text) - equipParam[4]).ToString();
                _EqValueMd.text = (int.Parse(_StValueMd.text) - equipParam[5]).ToString();
                _EqValueAg.text = (int.Parse(_StValueAg.text) - equipParam[6]).ToString();
                _EqValueLu.text = (int.Parse(_StValueLu.text) - equipParam[7]).ToString();
            }
            else
            {
                _EqValueAt.text = (parameters[2] - equipParam[2] + _runtimeActorDataModel.GetCurrentAttack(_classDataModel))
                    .ToString();
                _EqValueDf.text =
                    (parameters[3] - equipParam[3] + _runtimeActorDataModel.GetCurrentDefense(_classDataModel)).ToString();
                _EqValueMa.text =
                    (parameters[4] - equipParam[4] + _runtimeActorDataModel.GetCurrentMagicAttack(_classDataModel))
                    .ToString();
                _EqValueMd.text =
                    (parameters[5] - equipParam[5] + _runtimeActorDataModel.GetCurrentMagicDefense(_classDataModel))
                    .ToString();
                _EqValueAg.text =
                    (parameters[6] - equipParam[6] + _runtimeActorDataModel.GetCurrentAgility(_classDataModel)).ToString();
                _EqValueLu.text = (parameters[7] - equipParam[7] + _runtimeActorDataModel.GetCurrentLuck(_classDataModel))
                    .ToString();
            }
        }

        public void EqValueInit() {
            _EqValueAt.text = "0";
            _EqValueDf.text = "0";
            _EqValueMa.text = "0";
            _EqValueMd.text = "0";
            _EqValueAg.text = "0";
            _EqValueLu.text = "0";

            _EqValueAt.enabled = false;
            _EqValueDf.enabled = false;
            _EqValueMa.enabled = false;
            _EqValueMd.enabled = false;
            _EqValueAg.enabled = false;
            _EqValueLu.enabled = false;
        }

        public void StatusPlus(List<int> parameters) {
            _EqValueAt.enabled = false;
            _EqValueDf.enabled = false;
            _EqValueMa.enabled = false;
            _EqValueMd.enabled = false;
            _EqValueAg.enabled = false;
            _EqValueLu.enabled = false;

            _StValueAt.text = _runtimeActorDataModel.GetCurrentAttack(_classDataModel).ToString();
            _StValueDf.text = _runtimeActorDataModel.GetCurrentDefense(_classDataModel).ToString();
            _StValueMa.text = _runtimeActorDataModel.GetCurrentMagicAttack(_classDataModel).ToString();
            _StValueMd.text = _runtimeActorDataModel.GetCurrentMagicDefense(_classDataModel).ToString();
            _StValueAg.text = _runtimeActorDataModel.GetCurrentAgility(_classDataModel).ToString();
            _StValueLu.text = _runtimeActorDataModel.GetCurrentLuck(_classDataModel).ToString();
        }

        //←ステータス項目名
        private void StNameDisplay() {
            _StNameAt.text = TextManager.stAttack;
            _StNameDf.text = TextManager.stGuard;
            _StNameMa.text = TextManager.stMagic;
            _StNameMd.text = TextManager.stMagicGuard;
            _StNameAg.text = TextManager.stSpeed;
            _StNameLu.text = TextManager.stLuck;
        }
    }
}