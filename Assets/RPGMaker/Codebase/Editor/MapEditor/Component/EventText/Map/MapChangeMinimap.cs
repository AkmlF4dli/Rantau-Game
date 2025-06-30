using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Editor.Common;
using System.Collections.Generic;
using UnityEngine.UIElements;
using static RPGMaker.Codebase.Runtime.Event.Map.MapChangeMinimapProcessor;

namespace RPGMaker.Codebase.Editor.MapEditor.Component.EventText.Map
{
    public class MapChangeMinimap : AbstractEventText, IEventCommandView
    {
        public override VisualElement Invoke(string indent, EventDataModel.EventCommand eventCommand) {
            ret = indent;
            ret += "◆" + EditorLocalize.LocalizeText("WORD_6106") + " : ";
            if (eventCommand.parameters[ParameterDisplayOnOff] == "1")
            {
                ret += "ON";

                var posIndex = int.Parse(eventCommand.parameters[ParameterPositionType]);
                if (posIndex == PositionTypeXy)
                {
                    ret += $",({eventCommand.parameters[ParameterPositionX]},{eventCommand.parameters[ParameterPositionY]})";
                }
                else
                {
                    var posText = EditorLocalize.LocalizeText(new List<string>() { "WORD_0294", "WORD_0292", "WORD_0293", "WORD_0291", "WORD_0983" }[posIndex]);
                    ret += $",{posText}";
                }

                ret += $",{eventCommand.parameters[ParameterWidth]}x{eventCommand.parameters[ParameterHeight]}";
                ret += $",{eventCommand.parameters[ParameterScale]}";
                ret += $",{eventCommand.parameters[ParameterOpacity]}";

                ret += $",({eventCommand.parameters[ParameterPassableColor]})";
                ret += $",({eventCommand.parameters[ParameterUnpassableColor]})";
                ret += $",({eventCommand.parameters[ParameterEventColor]})";
            }
            else
            {
                ret += "OFF";
            }

            LabelElement.text = ret;
            Element.Add(LabelElement);
            return Element;
        }
    }
}