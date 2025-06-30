using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace RPGMaker.Codebase.Editor.Common.View
{
    public class SliderAndFieldBaseFix
    {
        public static void IntegerSliderCallBack(
            VisualElement root,
            int min,
            int max,
            string unit,
            int nowValue,
            Action<int> callBack,
            bool isMouseCaptureOut = false
        )
        {
            VisualElement sliderArea = new VisualElement();
            sliderArea.AddToClassList("multiple_item_in_row");
            root.Add(sliderArea);
            SliderInt sliderInt = new SliderInt();
            IntegerField integerField = new IntegerField();
            integerField.style.flexGrow = 0f;
            //Maxの桁数取得
            var digits = min.ToString().Length;
            var digits2 = max.ToString().Length;
            digits = digits > digits2 ? digits : digits2;
            if (digits == 1) digits = 2;
            integerField.style.width = 20 * digits;
            Label label = new Label(unit);
            sliderArea.Add(sliderInt);
            sliderArea.Add(integerField);
            sliderArea.Add(label);

            sliderInt.lowValue = min;
            sliderInt.highValue = max;
            sliderInt.value = nowValue;
            integerField.value = nowValue;

            BaseInputFieldHandler.IntegerFieldCallback(integerField, evt =>
            {
                sliderInt.value = integerField.value;
                callBack(sliderInt.value);
            }, min, max);

            if (isMouseCaptureOut)
            {
                sliderInt.RegisterCallback<MouseCaptureOutEvent>(evt =>
                {
                    integerField.value = sliderInt.value;
                    callBack(sliderInt.value);
                });

                sliderInt.RegisterValueChangedCallback(evt => { integerField.value = sliderInt.value; });
            }
            else
            {
                sliderInt.RegisterValueChangedCallback(evt =>
                {
                    integerField.value = sliderInt.value;
                    callBack(sliderInt.value);
                });
            }
        }
    }
}