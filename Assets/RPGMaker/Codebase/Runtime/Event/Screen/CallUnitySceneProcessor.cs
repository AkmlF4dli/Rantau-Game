using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Common;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement;
using RPGMaker.Codebase.Runtime.Common;
using RPGMaker.Codebase.Runtime.Map;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

using RPGMaker.Codebase.Runtime.Common.Component;
using RPGMaker.Codebase.CoreSystem.Service.RuntimeDataManagement;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime;
using SimpleJSON;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.SystemSetting;

namespace RPGMaker.Codebase.Runtime.Event.Screen
{
    /// <summary>
    /// [シーン制御]-[Unityシーンを呼び出す]
    /// </summary>
    public class CallUnitySceneProcessor : AbstractEventCommandProcessor
    {
        static CallUnitySceneProcessor _instance = null;

        private string _variableId;
        private string _saveData;
        private SoundCommonDataModel _mapBgm;
        private SoundCommonDataModel _mapBgs;
        private List<GameObject> _mapSceneGameObjectList = new List<GameObject>();

        protected override void Process(string eventId, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventId, command);
        }

        private async void ProcessAsync(string eventId, EventDataModel.EventCommand command) {
#endif
            if (!GameStateHandler.IsMap())
            {
                Debug.LogError($"Current GameState is not MAP: {GameStateHandler.CurrentGameState().ToString()}");
                ProcessEndAction();
                return;
            }
            //メニューの非表示
            MapManager.menu.MenuClose(false);

            //画面をフェードアウトする
            //HudDistributor.Instance.NowHudHandler().DisplayInit();
            //HudDistributor.Instance.NowHudHandler().FadeOut(() =>
            {
                //MAPシーンであれば、Unityシーンを呼び出す。
                var sceneName = command.parameters[0];
#if UNITY_EDITOR
                if (EditorBuildSettings.scenes.ToList().FindIndex(scene => scene.enabled && scene.path.EndsWith($"/{sceneName}.unity")) < 0)
                {
                    Debug.LogError($"{sceneName} is not in BuildSettings.");
                    ProcessEndAction();
                    return;
                }
#endif
                //ME関係を強制停止する　U326
                SoundManager.Self().ReleaseStopMe();

                var unloadMapScene = (command.parameters[1] == "1");
                _instance = this;
                _variableId = command.parameters[2];
                if (unloadMapScene)
                {
                    var data = DataManager.Self().GetRuntimeSaveDataModel();
                    _saveData = JsonUtility.ToJson(data);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    MapManager.StartUnityScene(sceneName, unloadMapScene);
#else
                    await MapManager.StartUnityScene(sceneName, unloadMapScene);
#endif
                }
                else
                {
                    _mapBgm = SoundManager.Self().GetBgmSound();
                    _mapBgs = SoundManager.Self().GetBgsSound();
                    GameStateHandler.SetGameState(GameStateHandler.GameState.UNITY_SCENE);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    MapManager.StartUnityScene(sceneName, unloadMapScene);
#else
                    await MapManager.StartUnityScene(sceneName, unloadMapScene);
#endif
                    _mapSceneGameObjectList.Clear();
                    var scene = SceneManager.GetSceneByName("SceneMap");
                    foreach (var gameObject in scene.GetRootGameObjects())
                    {
                        if (gameObject.activeSelf)
                        {
                            gameObject.SetActive(false);
                            _mapSceneGameObjectList.Add(gameObject);
                        }
                    }
                }
                return;
            }//, UnityEngine.Color.black);
        }

        private void ProcessEndAction() {
            _instance = null;
            _variableId = null;
            _saveData = null;
            _mapSceneGameObjectList.Clear();
            SendBackToLauncher.Invoke();
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static void ReturnToUnite(int result) {
#else
        public static async Task ReturnToUnite(int result) {
#endif
            if (_instance._saveData == null)
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                SetVariableValue(DataManager.Self().GetRuntimeSaveDataModel(), _instance._variableId, result);
#else
                await SetVariableValue(DataManager.Self().GetRuntimeSaveDataModel(), _instance._variableId, result);
#endif
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.name != "SceneMap")
                    {
                        foreach (var gameObject in scene.GetRootGameObjects())
                        {
                            if (gameObject.activeSelf)
                            {
                                gameObject.SetActive(false);
                            }
                        }
                    }
                }
                foreach (var gameObject in _instance._mapSceneGameObjectList)
                {
                    gameObject.SetActive(true);
                }
                _instance._mapSceneGameObjectList.Clear();

                //UnityシーンをUnload
                var sceneNameList = new List<string>();
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.name != "SceneMap")
                    {
                        sceneNameList.Add(scene.name);
                    }
                }
                foreach (var sceneName in sceneNameList)
                {
                    SceneManager.UnloadSceneAsync(sceneName);
                }

                System.Func<Task> func2 = async () =>
                {
                    if (Application.unityVersion.StartsWith("2022"))
                    {
                        // To avoid MissingReferenceException in EventSystem.
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        await Task.Delay(1);
#else
                        await UniteTask.Delay(1);
#endif
                    }
                    //状態の更新
                    GameStateHandler.SetGameState(GameStateHandler.GameState.MAP);

                    MapManager.UnitySceneToMap();

                    //マップのBGM、BGSに戻す
                    System.Func<Task> func = async () =>
                    {
                        SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_BGM, _instance._mapBgm);
                        SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_BGS, _instance._mapBgs);

                        await SoundManager.Self().PlayBgm();
                        await SoundManager.Self().PlayBgs();
                    };
                    _ = func();
                    _instance.ProcessEndAction();
                };
                _ = func2();

            }
            else
            {
                //TODO  _saveDataを使って、CONTINUEする。
                var runtimeSaveDataModel = JsonUtility.FromJson<RuntimeSaveDataModel>(_instance._saveData);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                SetVariableValue(runtimeSaveDataModel, _instance._variableId, result);
#else
                await SetVariableValue(runtimeSaveDataModel, _instance._variableId, result);
#endif
                //シーンに存在する全てのGameObjectを無効化してみる。
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    foreach (var gameObject in scene.GetRootGameObjects())
                    {
                        if (gameObject.activeSelf)
                        {
                            gameObject.SetActive(false);
                        }
                    }
                }

                DataManager.Self().LoadSaveData(runtimeSaveDataModel);
                SceneManager.LoadScene("SceneMap");
            }
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private static void SetVariableValue(RuntimeSaveDataModel runtimeSaveDataModel, string variableId, int value) {
#else
        private static async Task SetVariableValue(RuntimeSaveDataModel runtimeSaveDataModel, string variableId, int value) {
#endif
            // index取得
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var variables = new DatabaseManagementService().LoadFlags().variables;
#else
            var variables = (await new DatabaseManagementService().LoadFlags()).variables;
#endif

            var index = variables.FindIndex(x => x.id == _instance._variableId);
            if (index >= 0)
            {
                runtimeSaveDataModel.variables.data[index] = value.ToString();
            }
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public static int GetVariableValue() {
#else
        public static async Task<int> GetVariableValue() {
#endif
            // index取得
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var variables = new DatabaseManagementService().LoadFlags().variables;
#else
            var variables = (await new DatabaseManagementService().LoadFlags()).variables;
#endif

            var index = variables.FindIndex(x => x.id == _instance._variableId);
            if (index >= 0)
            {
                var runtimeSaveDataModel = DataManager.Self().GetRuntimeSaveDataModel();
                return int.Parse(runtimeSaveDataModel.variables.data[index]);
            }
            return 0;
        }
    }
}