using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Flag;
using RPGMaker.Codebase.Editor.Common;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace RPGMaker.Codebase.Editor.MapEditor.Component.EventText.SceneControl
{
    public class SceneCallUnityScene : AbstractEventText, IEventCommandView
    {
        public override VisualElement Invoke(string indent, EventDataModel.EventCommand eventCommand) {
            ret = indent;

            ret += "◆" + EditorLocalize.LocalizeText("WORD_5026") + " : " + eventCommand.parameters[0];
            if (eventCommand.parameters[1] == "1")
            {
                ret += $", {EditorLocalize.LocalizeText("WORD_5027")}";
            }
            {
                var varName = EditorLocalize.LocalizeText("WORD_2505");
                if (eventCommand.parameters[2].Length > 0)
                {
                    var variables = DatabaseManagementService.LoadFlags().variables;
                    var data = variables.FirstOrDefault(c => c.id == eventCommand.parameters[2]);
                    if (data != null)
                    {
                        var name = data.name;
                        if (name == "") name = EditorLocalize.LocalizeText("WORD_1518");
                        varName = "#" + (variables.IndexOf(data) + 1).ToString("0000") + " " + name;
                    }
                }
                ret += $", {varName}";
            }

            LabelElement.text = ret;
            Element.Add(LabelElement);
            return Element;
        }
    }
}