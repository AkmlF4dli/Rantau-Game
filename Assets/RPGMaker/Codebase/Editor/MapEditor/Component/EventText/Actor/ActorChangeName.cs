using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Editor.Common;
using System.Linq;
using UnityEngine.UIElements;

namespace RPGMaker.Codebase.Editor.MapEditor.Component.EventText.Actor
{
    public class ActorChangeName : AbstractEventText, IEventCommandView
    {
        public override VisualElement Invoke(string indent, EventDataModel.EventCommand eventCommand) {
            ret = EditorLocalize.LocalizeText("WORD_0916") + " : ";

            var actorId = eventCommand.parameters[0];
            var characterActorDataModels = DatabaseManagementService.LoadCharacterActor();
            var actor = characterActorDataModels.FirstOrDefault(c => c.uuId == actorId);

            ret += EditorLocalize.LocalizeText("WORD_0039") + " : " + actor?.basic.name + ", ";

            if (eventCommand.parameters[1] == "1")
                ret += eventCommand.parameters[2];

            var body = ret;
            ret = indent + "◆" + body;

            AddFoldout(indent, eventCommand);

            LabelElement.text = body;
            Element.Add(LabelElement);
            return Element;
        }
    }
}