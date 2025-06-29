using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;

[Serializable]
public class EventSearch_SelectValue : EditorWindow
{
    public static void OpenModalWindow(string title, List<string> values, Action<int> onCompleteCallback) {
        var wnd = CreateInstance<EventSearch_SelectValue>();
        wnd.titleContent = new GUIContent(title);
        wnd.mValues = values;
        wnd.mOnCompleteCallback = onCompleteCallback;
        wnd.ShowModal();
    }

    Action<int> mOnCompleteCallback = null;
    List<string> mValues = new List<string>();
    int mSelectedIndex = -1;

    public void CreateGUI() {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // Import UXML
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/RPGMaker/Codebase/Editor/Additional/EventSearcher/EventSearch_SelectValue.uxml");
        var body = visualTree.Instantiate();
        root.Add(body);

        var buttonOK = root.Q<Button>("Button_OK");
        buttonOK.RegisterCallback<ClickEvent>(evt => {
            mOnCompleteCallback?.Invoke(mSelectedIndex);
            Close();
        });
        buttonOK.SetEnabled(false);

        var listColumn = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/RPGMaker/Codebase/Editor/Additional/EventSearcher/EventSearch_SelectColumn.uxml");
        var listView = body.Q<ListView>();
        listView.makeItem = () => listColumn.Instantiate();
        listView.bindItem = (item, Index) =>
        {
            var idLabel = item.Q<Label>("Label_ID");
            var nameLabel = item.Q<Label>("Label_Name");
            idLabel.text =string .Format("#{0:D4}", Index+1);
            nameLabel.text = string.IsNullOrEmpty(mValues[Index]) ? "(blank name)" : mValues[Index];

            item.RegisterCallback<ClickEvent>(evt =>
            {
                mSelectedIndex = listView.selectedIndex;
                buttonOK.SetEnabled(true);
            });
        };
        listView.itemsSource = mValues;
        listView.onItemsChosen += obj =>
        {
            mOnCompleteCallback?.Invoke(mSelectedIndex);
            Close();
        };


        var buttonCancel= root.Q<Button>("Button_Cancel");
        buttonCancel.RegisterCallback<ClickEvent>(evt => {
            Close();
        });
    }
}