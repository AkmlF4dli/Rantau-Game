using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Editor.Common;
using UnityEngine.UIElements;

namespace RPGMaker.Codebase.Editor.MapEditor.Component.EventText.FlowControl
{
    public class FlowCustomMove : AbstractEventText, IEventCommandView
    {
        public override VisualElement Invoke(string indent, EventDataModel.EventCommand eventCommand) {
            var name = "";
            var text = "";
            if (eventCommand.parameters[1] == "-1")
                name = EditorLocalize.LocalizeText("WORD_0920");
            else if (eventCommand.parameters[1] == "-2")
                name = EditorLocalize.LocalizeText("WORD_0860");
            else
                name = GetEventDisplayName(eventCommand.parameters[1]);

            text = EditorLocalize.LocalizeText("WORD_2800") + " : " + name;

            if (eventCommand.parameters[2] == "1")
            {
                if (text != "") text += ",";
                text += EditorLocalize.LocalizeText("WORD_0989");
            }

            if (eventCommand.parameters[3] == "1")
            {
                if (text != "") text += ",";
                text += EditorLocalize.LocalizeText("WORD_0990");
            }

            if (eventCommand.parameters[4] == "1")
            {
                if (text != "") text += ",";
                text += EditorLocalize.LocalizeText("WORD_0952");
            }

            if (text != "") text = " (" + text + ")";

            var body = text;
            ret = indent + "◆" + body;

            AddFoldout(indent, eventCommand);

            LabelElement.text = body;
            Element.Add(LabelElement);
            return Element;
        }
    }
}
