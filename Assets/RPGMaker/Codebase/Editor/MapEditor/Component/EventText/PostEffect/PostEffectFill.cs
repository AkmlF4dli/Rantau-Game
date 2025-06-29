using System;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Editor.Common;
using UnityEngine.UIElements;

namespace RPGMaker.Codebase.Editor.MapEditor.Component.EventText.PostEffect
{
    public class PostEffectFill : AbstractEventText, IEventCommandView
    {
        public override VisualElement Invoke(string indent, EventDataModel.EventCommand eventCommand)
        {
            ret = indent;
            ret += "◆" + EditorLocalize.LocalizeText("WORD_6004") + " : ";

            if (eventCommand.parameters[3] == "0")
            {
                // 単色
                ret += EditorLocalize.LocalizeText("WORD_6030") + " ";
                ret += "(" + eventCommand.parameters[4] + "," +
                       eventCommand.parameters[5] + "," +
                       eventCommand.parameters[6] + "," +
                       eventCommand.parameters[7] + "), ";
            }
            else
            {
                // グラデーション
                ret += EditorLocalize.LocalizeText("WORD_6031") + " ";
                ret += eventCommand.parameters[8] switch {
                    "0" => EditorLocalize.LocalizeText("WORD_6036"), // 上から下へ
                    "1" => EditorLocalize.LocalizeText("WORD_6037"), // 左から右へ
                    "2" => EditorLocalize.LocalizeText("WORD_6038"), // 左上から右下へ
                    "3" => EditorLocalize.LocalizeText("WORD_6039"), // 右上から左下へ
                    _ => ""
                };
                ret += " ";
                ret += "(" + eventCommand.parameters[9] + "," +
                       eventCommand.parameters[10] + "," +
                       eventCommand.parameters[11] + "," +
                       eventCommand.parameters[12] + "), ";
                ret += "(" + eventCommand.parameters[13] + "," +
                       eventCommand.parameters[14] + "," +
                       eventCommand.parameters[15] + "," +
                       eventCommand.parameters[16] + "), ";
            }
            
            ret += eventCommand.parameters[17] switch {
                "0" => EditorLocalize.LocalizeText("WORD_6040"), // 通常
                "1" => EditorLocalize.LocalizeText("WORD_6041"), // 乗算
                "2" => EditorLocalize.LocalizeText("WORD_6042"), // 加算
                "3" => EditorLocalize.LocalizeText("WORD_6043"), // 除算
                "4" => EditorLocalize.LocalizeText("WORD_6044"), // スクリーン
                "5" => EditorLocalize.LocalizeText("WORD_6045"), // オーバーレイ
                _ => ""
            };
            ret += " ";
            
            if (eventCommand.parameters[0] == "1")
            {
                // マップが変わるまで
                ret += EditorLocalize.LocalizeText("WORD_6103");
            }
            else
            {
                // フレーム指定
                ret += eventCommand.parameters[1] + " " + EditorLocalize.LocalizeText("WORD_1088") + " ";
                if (eventCommand.parameters[2] == "1") ret += "(" + EditorLocalize.LocalizeText("WORD_1087") + ")";
            }
            
            LabelElement.text = ret;
            Element.Add(LabelElement);
            return Element;
        }
    }
}