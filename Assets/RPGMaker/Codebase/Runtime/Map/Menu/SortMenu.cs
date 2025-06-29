using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime;
using RPGMaker.Codebase.Runtime.Common;
using RPGMaker.Codebase.Runtime.Map.Item;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace RPGMaker.Codebase.Runtime.Map.Menu
{
    public class SortMenu : WindowBase
    {
        //右に描画するアクター
        private                  string[]   _actorID;
        [SerializeField] private GameObject _afterPartyObject;

        //左のアクター
        private string[] _beforActorItem;
        [SerializeField] private GameObject _beforePartyObject;

        //何番目に選択されているか
        private int _selected;

        private MenuBase _base;
        private RuntimeSaveDataModel _runtimeSaveDataModel;


        public override void Update() {
            base.Update();
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void Init(MenuBase @base) {
#else
        public async Task Init(MenuBase @base) {
#endif
            _base = @base;
            _runtimeSaveDataModel = DataManager.Self().GetRuntimeSaveDataModel();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            Display();
#else
            await Display();
#endif
            //共通のウィンドウの適応
            Init();
        }

        //今のパーティを表示させる
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void Display() {
#else
        private async Task Display() {
#endif
            _actorID = new string[_runtimeSaveDataModel.runtimePartyDataModel.actors.Count];
            _beforActorItem = new string[_runtimeSaveDataModel.runtimePartyDataModel.actors.Count];
            _selected = 0;

            for (var i = 0; i < 4; i++)
                if (i < DataManager.Self().GetRuntimeSaveDataModel().runtimePartyDataModel.actors.Count)
                {
                    for (var j = 0; j < DataManager.Self().GetRuntimeSaveDataModel().runtimeActorDataModels.Count; j++)
                        if (DataManager.Self().GetRuntimeSaveDataModel().runtimeActorDataModels[j].actorId ==
                            DataManager.Self().GetRuntimeSaveDataModel().runtimePartyDataModel.actors[i])
                        {
                            var characterItem = _beforePartyObject.transform.Find("Actor" + (i + 1)).gameObject
                                .AddComponent<CharacterItem>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                            characterItem.Init(DataManager.Self().GetRuntimeSaveDataModel().runtimeActorDataModels[j]);
#else
                            await characterItem.Init(DataManager.Self().GetRuntimeSaveDataModel().runtimeActorDataModels[j]);
#endif
                            _beforePartyObject.transform.Find("Actor" + (i + 1)).gameObject.SetActive(true);
                            _afterPartyObject.transform.Find("Actor" + (i + 1)).gameObject.SetActive(true);
                            break;
                        }
                }
                else
                {
                    _beforePartyObject.transform.Find("Actor" + (i + 1)).gameObject.SetActive(false);
                    _afterPartyObject.transform.Find("Actor" + (i + 1)).gameObject.SetActive(false);
                }
            
            var selects = _beforePartyObject.GetComponentsInChildren<Button>().ToList();
            for (var i = 0; i < selects.Count; i++)
            {
                var nav = selects[i].navigation;
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnUp = selects[i == 0 ? selects.Count - 1 : i - 1];
                nav.selectOnDown = selects[(i + 1) % selects.Count];

                selects[i].navigation = nav;
                selects[i].targetGraphic = selects[i].transform.Find("Highlight").GetComponent<Image>();
            }

            if (selects.Count > 0)
                selects[0].Select();
        }

        public void SortAnimation(GameObject obj) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            _ = SortAnimationAsync(obj);
        }
        public async Task SortAnimationAsync(GameObject obj) {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            ButtonRrocessing(obj);
#else
            await ButtonRrocessing(obj);
#endif
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void SortActor(GameObject obj) {
#else
        private async Task SortActor(GameObject obj) {
#endif
            //押されたアクターIDの取得
            var actorId = obj.GetComponent<CharacterItem>().PartyId();
            //配列につめる
            _actorID[_selected] = actorId;
            _beforActorItem[_selected] = obj.name.Substring(obj.name.Length - 1);
            //次に込める配列用に増やしておく
            _selected++;
            //すべて選択し終わった場合
            if (_selected >= _runtimeSaveDataModel.runtimePartyDataModel.actors.Count)
            {
                //全員が選択されたらメインメニューに戻る
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                ReturnMenu(_actorID);
#else
                await ReturnMenu(_actorID);
#endif
            }
            else
            {
                _afterPartyObject.transform.Find("Actor" + _selected + "/Content").gameObject.SetActive(true);
                _afterPartyObject.transform.Find("Actor" + _selected + "/Mask").gameObject.SetActive(false);
                var characterItem = _afterPartyObject.transform.Find("Actor" + _selected + "/Content").gameObject
                    .AddComponent<CharacterItem>();
                var actors = DataManager.Self().GetRuntimeSaveDataModel().runtimeActorDataModels;
                for (var i = 0; i < actors.Count; i++)
                    if (actors[i].actorId == actorId)
                    {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        characterItem.Init(actors[i]);
#else
                        await characterItem.Init(actors[i]);
#endif
                        break;
                    }

                obj.GetComponent<Button>().interactable = false;
            }
        }

        public void Cancel() {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            _ = CancelAsync();
        }
        public async Task CancelAsync() {
#endif
            if (_selected <= 0)
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _base.BackMenu();
#else
                await _base.BackMenuAsync();
#endif
            }
            else
            {
                //キャンセルのSE鳴動
                SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_SE, DataManager.Self().GetSystemDataModel().soundSetting.cancel);
                SoundManager.Self().PlaySe();

                _afterPartyObject.transform.Find("Actor" + _selected + "/Content").gameObject.SetActive(false);
                _afterPartyObject.transform.Find("Actor" + _selected + "/Mask").gameObject.SetActive(true);
                _selected--;
                var obj = _beforePartyObject.transform.Find("Actor" + _beforActorItem[_selected]).gameObject;
                obj.GetComponent<Button>().interactable = true;
            }
        }

        //セーブ箇所_actorIDが入ってくる
        private void Save(string[] ID) {
            for (var i = 0; i < ID.Length; i++) _runtimeSaveDataModel.runtimePartyDataModel.actors[i] = ID[i];
        }

        //再読み込み(_actorIDが入ってくる)
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void Reload(string[] ID) {
#else
        private async Task Reload(string[] ID) {
#endif
            for (; _selected > 0; _selected--)
            {
                var obj = _beforePartyObject.transform.Find("Actor" + _selected).gameObject;
                obj.GetComponent<Button>().interactable = true;
                obj.transform.Find("Mask").gameObject.SetActive(false);
                _afterPartyObject.transform.Find("Actor" + _selected + "/Content").gameObject.SetActive(false);
                _afterPartyObject.transform.Find("Actor" + _selected + "/Mask").gameObject.SetActive(true);
            }

            Save(ID);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            Display();
#else
            await Display();
#endif
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void ReturnMenu(string[] ID) {
#else
        private async Task ReturnMenu(string[] ID) {
#endif
            for (; _selected > 0; _selected--)
            {
                var obj = _beforePartyObject.transform.Find("Actor" + _selected).gameObject;
                obj.GetComponent<Button>().interactable = true;
                _afterPartyObject.transform.Find("Actor" + _selected + "/Content").gameObject.SetActive(false);
                _afterPartyObject.transform.Find("Actor" + _selected + "/Mask").gameObject.SetActive(true);
            }

            Save(ID);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            MapManager.SortActor();
#else
            await MapManager.SortActor();
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _base.BackMenu();
#else
            await _base.BackMenuAsync();
#endif
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void ButtonRrocessing(GameObject obj) {
            SortActor(obj);
        }
#else
        private async Task ButtonRrocessing(GameObject obj) {
            await SortActor(obj);
        }
#endif
    }
}