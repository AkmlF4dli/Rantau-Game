using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.SystemSetting;
using RPGMaker.Codebase.Runtime.Battle.Objects;
using RPGMaker.Codebase.Runtime.Map;
using RPGMaker.Codebase.Runtime.Map.Item;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace RPGMaker.Codebase.Runtime.Common
{
    public class PartyWindow : WindowBase
    {
        public enum PartyType
        {
            Item,
            Skill
        }

        private List<CharacterItem> _characterItem;
        private GameAction _gameAction;
        private GameItem _gameItem;
        private PartyType _partyType = PartyType.Item;
        private string _useActorId = "";
        private string _useId      = "";
        Action _callback;
        private int _pattern;
        private bool isAll = false;

        /// <summary>
        /// 初期化処理
        /// </summary>
        /// <param name="type"></param>
        /// <param name="useActorId"></param>
        /// <param name="useId"></param>
        /// <param name="gameItem"></param>
        /// <param name="callback"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void Init(PartyType type, string useActorId, string useId, GameItem gameItem, Action callback) {
#else
        public async Task Init(PartyType type, string useActorId, string useId, GameItem gameItem, Action callback) {
#endif
            _callback = callback;
            _partyType = type;
            _useActorId = useActorId;
            _useId = useId;
            _gameItem = gameItem;

            SystemSettingDataModel systemSettingDataModel = DataManager.Self().GetSystemDataModel();
            _pattern = int.Parse(systemSettingDataModel.uiPatternId) + 1;
            if (_pattern < 1 || _pattern > 6)
                _pattern = 1;
            var partyItems = transform.Find("MenuArea/PartyWindow/PartyItems").gameObject;
            var party = DataManager.Self().GetRuntimeSaveDataModel().runtimePartyDataModel;
            var actors = DataManager.Self().GetRuntimeSaveDataModel().runtimeActorDataModels;
            _characterItem = new List<CharacterItem>();

            //U247 説明用のオブジェクトを無効へ
            var uICanvas = GameObject.Find("Menu/MenuWindow05(Clone)/UICanvas");
　　        if (uICanvas)
            {
                var description = uICanvas.transform.Find("PartyWindow/MenuArea/Description");
                description.gameObject.SetActive(false);

                var itemNameBack = uICanvas.transform.Find("PartyWindow/MenuArea/ItemNameBack");
                itemNameBack.gameObject.SetActive(false);
            }
            //パーティ部分
            for (var i = 0; i < 4; i++)
            {
                if (i < party.actors.Count)
                {
                    for (var j = 0; j < actors.Count; j++)
                    {
                        if (actors[j].actorId == party.actors[i])
                        {
                            var characterItem = partyItems.transform.Find("Actor" + (i + 1)).gameObject
                                .GetComponent<CharacterItem>();
                            if (characterItem == null)
                            {
                                characterItem = partyItems.transform.Find("Actor" + (i + 1)).gameObject
                                    .AddComponent<CharacterItem>();
                            }
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                            characterItem.Init(actors[j]);
#else
                            await characterItem.Init(actors[j]);
#endif
                            _characterItem.Add(characterItem);
                            partyItems.transform.Find("Actor" + (i + 1)).gameObject.SetActive(true);
                            break;
                        }
                    }
                }
                else
                {
                    partyItems.transform.Find("Actor" + (i + 1)).gameObject.SetActive(false);
                }
            }
            

            //決定音やブザー音は、共通部品では鳴動しない
            foreach (var t in _characterItem)
            {
                t.GetComponent<WindowButtonBase>().SetSilentClick(true);
            }

            Init();
            SetFocusAft();
        }

        /// <summary>
        /// フォーカス設定
        /// </summary>
        private async void SetFocusAft(int index = 0) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            await Task.Delay(10);
#else
            await UniteTask.Delay(10);
#endif
            isAll = false;
            //味方全員対象の場合
            if (_gameItem.Scope == 8 || _gameItem.Scope == 10 || _gameItem.Scope == 13 || _gameItem.Scope == 50)
            {
                isAll = true;
                foreach (var t in _characterItem)
                {
                    t.GetComponent<WindowButtonBase>().SetAlreadyHighlight(true);
                    t.GetComponent<WindowButtonBase>().SetAnimation(true);
                    t.GetComponent<WindowButtonBase>().SetDefaultClick();
                }

                for (var i = 0; i < _characterItem.Count; i++)
                {
                    var nav = _characterItem[i].GetComponent<Button>().navigation;
                    nav.mode = Navigation.Mode.None;
                    _characterItem[i].GetComponent<Button>().navigation = nav;
                }
                //先頭にフォーカスをあてる
                _characterItem[index].GetComponent<WindowButtonBase>().SetEnabled(true);
                _characterItem[index].GetComponent<Button>().Select();
            }
            else
            {
                foreach (var t in _characterItem)
                {
                    t.GetComponent<WindowButtonBase>().SetAlreadyHighlight(false);
                    t.GetComponent<WindowButtonBase>().SetAnimation(true);
                }

                //対象が「使用者への影響」にしか存在しない場合は、フォーカス移動を行わせない
                bool notFocusChange = false;
                if (_partyType == PartyType.Skill)
                    if (_gameItem.Scope == 0 && _gameItem.UserScope == 1)
                        notFocusChange = true;

                //十字キーでの操作登録
                for (var i = 0; i < _characterItem.Count; i++)
                {
                    var nav = _characterItem[i].GetComponent<Button>().navigation;
                    nav.mode = Navigation.Mode.Explicit;

                    //UIパターンに応じて十字キーを変更する
                    if (_pattern == 1 || _pattern == 2 || _pattern == 3 || _pattern == 4)
                    {
                        if (!notFocusChange)
                        {
                            nav.selectOnUp = _characterItem[i == 0 ? _characterItem.Count - 1 : i - 1].GetComponent<Button>();
                            nav.selectOnDown = _characterItem[(i + 1) % _characterItem.Count].GetComponent<Button>();
                        }
                        else
                        {
                            nav.selectOnUp = null;
                            nav.selectOnDown = null;
                        }
                    }
                    else
                    {
                        if (!notFocusChange)
                        {
                            nav.selectOnLeft = _characterItem[i == 0 ? _characterItem.Count - 1 : i - 1].GetComponent<Button>();
                            nav.selectOnRight = _characterItem[(i + 1) % _characterItem.Count].GetComponent<Button>();
                        }
                        else
                        {
                            nav.selectOnLeft = null;
                            nav.selectOnRight = null;
                        }
                    }

                    _characterItem[i].GetComponent<Button>().navigation = nav;
                    _characterItem[i].GetComponent<Button>().targetGraphic = _characterItem[i].GetComponent<Button>().transform.Find("Highlight").GetComponent<Image>();
                }
                //先頭にフォーカスをあてる
                if (!notFocusChange)
                {
                    _characterItem[index].GetComponent<WindowButtonBase>().SetEnabled(true);
                    _characterItem[index].GetComponent<Button>().Select();
                }
                else
                {
                    var party = DataManager.Self().GetGameParty();

                    //GameAction取得
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    for (int num = 0; num < party.Actors.Count; num++)
#else
                    var partyActors = await party.GetActors();
                    for (int num = 0; num < partyActors.Count; num++)
#endif
                    {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        if (party.Actors[num].ActorId == _useActorId)
#else
                        if (partyActors[num].ActorId == _useActorId)
#endif
                        {
                            _characterItem[num].GetComponent<WindowButtonBase>().SetEnabled(true);
                            _characterItem[num].GetComponent<Button>().Select();
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// スキル使用処理
        /// </summary>
        /// <param name="index"></param>
        /// <param name="skillId"></param>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void UseSkill(int index, string skillId) {
#else
        private async Task UseSkill(int index, string skillId) {
#endif
            //パーティ情報取得
            var party = DataManager.Self().GetGameParty();

            //GameAction取得
            GameActor item = null;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            for (int i = 0; i < party.Actors.Count; i++)
                if (party.Actors[i].ActorId == _useActorId)
#else
            var partyActors = await party.GetActors();
            for (int i = 0; i < partyActors.Count; i++)
                if (partyActors[i].ActorId == _useActorId)
#endif
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    item = party.Actors[i];
#else
                    item = partyActors[i];
#endif
                    break;
                }

            _gameAction = new GameAction(item);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            await _gameAction.InitForConstructor(item);
#endif

            //使用スキル設定
            var skill = DataManager.Self().GetSkillCustomDataModel(skillId);
            _gameAction.SetSkill(skill.basic.id);

            //対象者を取得
            var gameBattlers =
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                isAll ? _gameAction.MakeTargets() : new List<GameBattler> {_characterItem[index].GameActor};
#else
                isAll ? await _gameAction.MakeTargets() : new List<GameBattler> {_characterItem[index].GameActor};
#endif
            foreach (var battler in gameBattlers)
            {
                battler.Result ??= new GameActionResult();
            }

            //コモンイベントの有無を判断
            bool isCommonEvent = false;
            bool isCommonEventForUser = false;
            foreach (var effect in _gameAction.Item.Effects)
            {
                if (isCommonEvent) break;
                isCommonEvent = _gameAction.IsEffectCommonEvent(effect);
            }

            //使用者への影響にチェックが入っている場合、使用者への影響側のコモンイベントも確認
            if (_gameAction.IsForUser())
                foreach (var effect in _gameAction.Item.EffectsMyself)
                {
                    if (isCommonEventForUser) break;
                    isCommonEventForUser = _gameAction.IsEffectCommonEvent(effect);
                }

            //ブザー音を鳴動するかどうか
            bool isSuccess = isCommonEvent || isCommonEventForUser;
            int hitCount = 0;

            //スキルを利用して効果があるかどうかの確認
            bool isEffect = false;
            foreach (var battler in gameBattlers)
            {
                if (_gameAction.TestApply(battler) && (_gameAction.IsForFriend() || _gameAction.IsForOpponentAndFriend()))
                {
                    isEffect = true;
                    break;
                }
            }

            if (_gameAction.IsForUser())
            {
                if (_gameAction.TestApplyMyself(item))
                {
                    isEffect = true;
                }
            }

            //コストの確認
            if ((isEffect || isCommonEvent || isCommonEventForUser) && item.CanPaySkillCost(_gameItem))
            {
                //対象者に対してアクションを実行
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                item.UseItem(_gameItem);
#else
                await item.UseItem(_gameItem);
#endif
                foreach (var battler in gameBattlers)
                {
                    //対象者へApply
                    if (_gameAction.TestApply(battler) && (_gameAction.IsForFriend() || _gameAction.IsForOpponentAndFriend()))
                    {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        _gameAction.Apply(battler);
#else
                        await _gameAction.Apply(battler);
#endif
                        hitCount++;
                    }
                }

                //使用者への影響が設定されている場合、アクションを実行
                if (_gameAction.IsForUser())
                {
                    if (_gameAction.TestApplyMyself(item))
                    {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        _gameAction.ApplyMyself(item);
#else
                        await _gameAction.ApplyMyself(item);
#endif
                        hitCount++;
                    }
                }

                //コモンイベント実行
                if (isCommonEvent || isCommonEventForUser)
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _gameAction.SetCommonEvent(isCommonEventForUser);
#else
                    await _gameAction.SetCommonEvent(isCommonEventForUser);
#endif

                if (hitCount > 0)
                {
                    isSuccess = true;
                    for (int i = 0; i < _characterItem.Count; i++)
                    {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        _characterItem[i].UpdateData(_characterItem[i].RuntimeActorDataModel);
#else
                        await _characterItem[i].UpdateData(_characterItem[i].RuntimeActorDataModel);
#endif
                    }
                }
            }

            //音声鳴動
            if (isSuccess || isCommonEvent || isCommonEventForUser)
            {
                _gameAction.UseItemPlaySe(1);
                MenuManager.MenuBase.AllUpdateStatus();
                SetFocusAft(index);
            }
            else
            {
                SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_SE, systemSettingDataModel.soundSetting.buzzer);
                SoundManager.Self().PlaySe();
            }
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void UseItem(int index, string itemId) {
#else
        private async Task UseItem(int index, string itemId) {
#endif
            //パーティ情報取得
            var item = _characterItem[index];
            var party = DataManager.Self().GetGameParty();

            //GameAction取得
            _gameAction = new GameAction(item.GameActor);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            await _gameAction.InitForConstructor(item.GameActor);
#endif
            _gameAction.SetItem(_gameItem.ItemId);

            //使用アイテム設定
            //アイテムを所持していない場合は終了
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            if (!party.HasItem(_gameItem))
#else
            if (!await party.HasItem(_gameItem))
#endif
            {
                SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_SE, systemSettingDataModel.soundSetting.buzzer);
                SoundManager.Self().PlaySe();
                return;
            }

            //対象者を取得
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var gameBattlers = isAll ? _gameAction.MakeTargets() : new List<GameBattler> {_characterItem[index].GameActor};
#else
            var gameBattlers = isAll ? await _gameAction.MakeTargets() : new List<GameBattler> {_characterItem[index].GameActor};
#endif
            foreach (var battler in gameBattlers)
            {
                battler.Result ??= new GameActionResult();
            }

            //コモンイベントの有無を判断
            bool isCommonEvent = false;
            bool isCommonEventForUser = false;
            foreach (var effect in _gameAction.Item.Effects)
            {
                if (isCommonEvent) break;
                isCommonEvent = _gameAction.IsEffectCommonEvent(effect);
            }

            //使用者への影響にチェックが入っている場合、使用者への影響側のコモンイベントも確認
            if (_gameAction.IsForUser())
                foreach (var effect in _gameAction.Item.EffectsMyself)
                {
                    if (isCommonEventForUser) break;
                    isCommonEventForUser = _gameAction.IsEffectCommonEvent(effect);
                }

            //スキルを利用して効果があるかどうかの確認
            bool isEffect = false;
            foreach (var battler in gameBattlers)
            {
                if (_gameAction.TestApply(battler) && (_gameAction.IsForFriend() || _gameAction.IsForOpponentAndFriend()))
                {
                    isEffect = true;
                    break;
                }
            }

            if (_gameAction.IsForUser())
            {
                if (_gameAction.TestApplyMyself(item.GameActor))
                {
                    isEffect = true;
                }
            }

            //ブザー音を鳴動するかどうか
            bool isSuccess = isCommonEvent || isCommonEventForUser;
            int hitCount = 0;

            if (isEffect || isCommonEvent || isCommonEventForUser)
            {
                //対象者に対してアクションを実行
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                item.GameActor.UseItem(_gameItem);
#else
                await item.GameActor.UseItem(_gameItem);
#endif
                foreach (var battler in gameBattlers)
                {
                    //対象者へApply
                    if (_gameAction.TestApply(battler) && (_gameAction.IsForFriend() || _gameAction.IsForOpponentAndFriend()))
                    {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        _gameAction.Apply(battler);
#else
                        await _gameAction.Apply(battler);
#endif
                        hitCount++;
                    }
                }

                //使用者への影響が設定されている場合、アクションを実行
                if (_gameAction.TestApplyMyself(item.GameActor))
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _gameAction.ApplyMyself(item.GameActor);
#else
                    await _gameAction.ApplyMyself(item.GameActor);
#endif
                    hitCount++;
                }

                //コモンイベント実行
                if (isCommonEvent || isCommonEventForUser)
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    _gameAction.SetCommonEvent(isCommonEventForUser);
#else
                    await _gameAction.SetCommonEvent(isCommonEventForUser);
#endif

                if (hitCount > 0)
                {
                    isSuccess = true;
                    for (int i = 0; i < _characterItem.Count; i++)
                    {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        _characterItem[i].UpdateData(_characterItem[i].RuntimeActorDataModel);
#else
                        await _characterItem[i].UpdateData(_characterItem[i].RuntimeActorDataModel);
#endif
                    }
                }
            }

            //音声鳴動
            if (isSuccess || isCommonEvent || isCommonEventForUser)
            {
                _gameAction.UseItemPlaySe(0);
                MenuManager.MenuBase.AllUpdateStatus();
                SetFocusAft(index);
            }
            else
            {
                SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_SE, systemSettingDataModel.soundSetting.buzzer);
                SoundManager.Self().PlaySe();
            }
        }


        public void ButtonEvent(int index) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            _ = ButtonEventAsync(index);
        }
        public async Task ButtonEventAsync(int index) {
#endif
            if (_partyType == PartyType.Item)
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                UseItem(index, _useId);
#else
                await UseItem(index, _useId);
#endif
            else
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                UseSkill(index, _useId);
#else
                await UseSkill(index, _useId);
#endif
        }

        public new void Back() {
            gameObject.SetActive(false);
            if (_callback != null) _callback();
        }
    }
}