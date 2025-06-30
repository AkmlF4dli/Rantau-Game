using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Editor.Common;
using UnityEngine.UIElements;

namespace RPGMaker.Codebase.Editor.MapEditor.Component.EventText.PostEffect
{
    public class PostEffectRemove : AbstractEventText, IEventCommandView
    {
        public override VisualElement Invoke(string indent, EventDataModel.EventCommand eventCommand)
        {
            // TODO: 仮
            ret = indent;
            ret += "◆" + EditorLocalize.LocalizeText("WORD_6006") + " : ";
            LabelElement.text = ret;
            Element.Add(LabelElement);
            return Element;
        }
    }
}