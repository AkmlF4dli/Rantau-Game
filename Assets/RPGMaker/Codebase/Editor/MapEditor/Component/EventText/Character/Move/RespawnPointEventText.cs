using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Editor.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace RPGMaker.Codebase.Editor.MapEditor.Component.EventText.Character.Move
{
    public class RespawnPointEventText : AbstractEventText, IEventCommandView
    {
        public override VisualElement Invoke(string indent, EventDataModel.EventCommand eventCommand) {
            ret = indent;
            var text = EditorLocalize.LocalizeText("WORD_1514") + EditorLocalize.LocalizeText("WORD_1653") + " : ";
            var StatusName = new List<string>
            {
                EditorLocalize.LocalizeText("WORD_0903"),
                EditorLocalize.LocalizeText("WORD_1654"),
            };
            int parse;
            var StatusNameID = 0;

            if (int.TryParse(eventCommand.parameters[5], out parse))
                StatusNameID = int.Parse(eventCommand.parameters[5]);

            var mapManagementService = Editor.Hierarchy.Hierarchy.mapManagementService;
            var mapEntities = mapManagementService.LoadMaps();

            //ゲームオーバー以外
            if (eventCommand.parameters[0] != "0")
            {
                text += StatusName[StatusNameID] + " : ";
            }
            string x, y;


            if (eventCommand.parameters[0] == "1")
            {
                //プレイヤー
                text += EditorLocalize.LocalizeText("WORD_0860");
            }
            else
            if (eventCommand.parameters[0] == "2")
            {
                //マップ直接指定
                var mapDataModel = mapEntities.FirstOrDefault(c => c.id == eventCommand.parameters[2]);
                text += $"{mapDataModel?.name ?? string.Empty} ";
                x = eventCommand.parameters[3];
                y = eventCommand.parameters[4].TrimStart(new char[] { '-' });
                text += $"(x : {x}, y : {y})";
            }
            else
            {
                //なし
                text += EditorLocalize.LocalizeText("WORD_1068");
            }
            ret += text;
            LabelElement.text = ret;
            Element.Add(LabelElement);
            return Element;
        }
    }
}