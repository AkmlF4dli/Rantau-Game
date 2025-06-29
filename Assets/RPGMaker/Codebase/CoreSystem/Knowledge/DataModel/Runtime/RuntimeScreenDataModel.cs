using RPGMaker.Codebase.Runtime.Common;
using System;
using System.Collections.Generic;
using UnityEngine; 

namespace RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime
{
    [Serializable]
    public class RuntimeScreenDataModel
    {
        public ScreenPicture picture;
        public List<int> tone;
        public ScreenWeather weather;
        public float timer = -1;
        public PostEffect postEffect;
        public Minimap minimap;

        [Serializable]
        public class ScreenWeather
        {
            public int power;
            public int type;
        }

        [Serializable]
        public class ScreenPicture
        {
            public List<PictureData> pictureData;
            public List<MoveData> moveData;
            public List<ChangeColorData> changeColorData;
            public List<ChangeSizeData> changeSizeData;
            public List<RotationData> rotateData;
        }

        [Serializable]
        public struct PictureData
        {
            public int key;
            public List<string> parameters;
            public float posX;
            public float posY;
        }

        [Serializable]
        public struct MoveData
        {
            public int key;
            public int MoveType;
            public float StartPosX;
            public float StartPosY;
            public float TargetPosX;
            public float TargetPosY;
            public float MoveTime;
            public float MoveTargetTime;
            public bool MoveToggle;
        }

        [Serializable]
        public struct ChangeColorData
        {
            public int key;
            public float ChangeColorTime;
            public float ChangeColorTargetTime;
            public bool ChangeColorToggle;
            public float TargetColorR;
            public float TargetColorG;
            public float TargetColorB;
            public float NowColorR;
            public float NowColorG;
            public float NowColorB;
            public float Gray;
            public float TargetGray;
        }

        [Serializable]
        public struct ChangeSizeData
        {
            public int key;
            public float ChangeSizeTime;
            public float ChangeSizeTargetTime;
            public float StartSizeX;
            public float StartSizeY;
            public float TargetSizeX;
            public float TargetSizeY;
        }

        [Serializable]
        public struct RotationData
        {
            public int key;
            public float rotation;
            public float nowRotation;
        }

        [Serializable]
        public class PostEffect
        {
            public List<PostEffectData> postEffectData;
        }

        [Serializable]
        public struct PostEffectData
        {
            public int type;
            public List<int> param;
        }

        [Serializable]
        public class Minimap
        {
            public bool show;
            public int x;
            public int y;
            public int width;
            public int height;
            public float scale;
            public int opacity;
            public string passableColor;
            public string unpassableColor;
            public string eventColor;
            public string frameName;
            public string maskName;
        }

        public RuntimeScreenDataModel() {
            picture = new ScreenPicture();
            picture.pictureData = new List<PictureData>();
            picture.moveData = new List<MoveData>();
            picture.changeColorData = new List<ChangeColorData>();
            picture.changeSizeData = new List<ChangeSizeData>();
            picture.rotateData = new List<RotationData>();
            weather = new ScreenWeather();
            tone = new List<int>();
            tone.Add(0);
            tone.Add(0);
            tone.Add(0);
            tone.Add(0);
            timer = -1;
            postEffect = new PostEffect
            {
                postEffectData = new List<PostEffectData>()
            };
        }

        /// <summary>
        /// ToneColorを返す
        /// 設定されていない場合は、Color.clearを返す
        /// </summary>
        /// <returns></returns>
        public Color GetToneColor() 
        {
            if (tone != null && tone.Count == 4)
            {
                Color color = new Color(
                    tone[0],
                    tone[1],
                    tone[2],
                    tone[3]
                );
                return color;
            }
            return Color.clear;
        }
    }
}