using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Editor.Common;
using UnityEngine.UIElements;

namespace RPGMaker.Codebase.Editor.MapEditor.Component.EventText.FlowControl
{
    public class FlowIfElse : AbstractEventText, IEventCommandView
    {
        public override VisualElement Invoke(string indent, EventDataModel.EventCommand eventCommand) {
            var body = EditorLocalize.LocalizeText("WORD_1601");
            ret = indent + " : " + body;

            AddFoldout(indent, eventCommand);

            LabelElement.text = body;
            Element.Add(LabelElement);
            return Element;
        }
    }
}