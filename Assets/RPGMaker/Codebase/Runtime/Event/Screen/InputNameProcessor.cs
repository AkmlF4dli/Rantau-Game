using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Map.InputName;
using UnityEngine;

namespace RPGMaker.Codebase.Runtime.Event.Screen
{
    /// <summary>
    /// [シーン制御]-[名前入力の処理]
    /// </summary>
    public class InputNameProcessor : AbstractEventCommandProcessor
    {
        protected override void Process(string eventID, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventID, command);
        }

        private async void ProcessAsync(string eventID, EventDataModel.EventCommand command) {
#endif
            //名前入力のprefabのpath
            var InputPrefabPath = "Assets/RPGMaker/Codebase/Runtime/Map/Asset/Prefab/";
            //名前入力のPrefab名
            var InputPrefabName = "InputNameCanvas";
            //アクターの名前
            var actorName = command.parameters[0];
            //文字数
            var wordCount = int.Parse(command.parameters[1]);

            //名前入力のprefab
            var inputObject = GameObject.Instantiate(
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<GameObject>(
#else
                await UnityEditorWrapper.AssetDatabaseWrapper.LoadAssetAtPath<GameObject>(
#endif
                    InputPrefabPath + InputPrefabName + ".prefab"),
                new Vector3(0.0f, 0.0f, 0.0f),
                Quaternion.identity
            );

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            inputObject.GetComponent<InputNameWindow>().Init(() =>
#else
            await inputObject.GetComponent<InputNameWindow>().Init(() =>
#endif
            {
                SendBackToLauncher.Invoke();
                // 名前入力ウィンドウのオブジェクト削除
                GameObject.Destroy(inputObject);

            }, actorName, wordCount);
        }
    }
}