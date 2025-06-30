using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.Editor.Common.Enum;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RPGMaker.Codebase.Editor.Common
{
    /// <summary>
    /// サウンド関連の処理
    /// </summary>
    public static class SoundHelper
    {
        /// <summary>
        /// 拡張子が保存されていないデータ用に、拡張子をつけてサウンドファイル名を返却する
        /// </summary>
        /// <param name="soundType"></param>
        /// <param name="soundFileName"></param>
        /// <returns></returns>
        public static string InitializeFileName(List<SoundType> soundTypes, string soundFileName, bool isPath) {
            DirectoryInfo dir;
            string dirName;
            for (int i = 0; i < soundTypes.Count; i++)
            {
                if (soundTypes[i] == SoundType.Bgm)
                    dirName = PathManager.SOUND_BGM;
                else if (soundTypes[i] == SoundType.Bgs)
                    dirName = PathManager.SOUND_BGS;
                else if (soundTypes[i] == SoundType.Me)
                    dirName = PathManager.SOUND_ME;
                else if (soundTypes[i] == SoundType.Se)
                    dirName = PathManager.SOUND_SE;
                else
                    return "";

                dir = new DirectoryInfo(dirName);

                var info = dir.GetFiles("*.ogg");
                foreach (var f in info)
                {
                    if (soundFileName.EndsWith(".ogg"))
                    {
                        if (f.Name == soundFileName)
                        {
                            if (!isPath)
                                return soundFileName;
                            else
                                return dirName + soundFileName;
                        }
                    }
                    else
                    {
                        if (f.Name.Replace(".ogg", "") == soundFileName)
                        {
                            if (!isPath)
                                return soundFileName + ".ogg";
                            else
                                return dirName + soundFileName + ".ogg";
                        }
                    }
                }

                info = dir.GetFiles("*.wav");
                foreach (var f in info)
                {
                    if (soundFileName.EndsWith(".wav"))
                    {
                        if (f.Name == soundFileName)
                        {
                            if (!isPath)
                                return soundFileName;
                            else
                                return dirName + soundFileName;
                        }
                    }
                    else
                    {
                        if (f.Name.Replace(".wav", "") == soundFileName)
                        {
                            if (!isPath)
                                return soundFileName + ".wav";
                            else
                                return dirName + soundFileName + ".wav";
                        }
                    }
                }
            }

            return "";
        }
        /// <summary>
        /// 表示用に、拡張子の無い文字を返却する
        /// </summary>
        /// <param name="soundType"></param>
        /// <param name="soundFileName"></param>
        /// <returns></returns>
        public static string RemoveExtention(string soundFileName) {
            return soundFileName.Replace(".ogg", "").Replace(".wav","");
        }

        /// <summary>
        /// オーディオリスナーをシーン内から探して取得する
        /// エデッタ用関数
        /// </summary>
        /// <returns></returns>
        public static AudioSource GetEditorSceneAudioSource() {
            AudioSource audioSource = null;
            var flag = HideFlags.HideAndDontSave;
            var obj = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(go => (go.hideFlags & flag) == flag && go.name == "UNITETestSoundPlayerObject");
            if (obj != default(GameObject))
            {
                audioSource = obj.GetComponent<AudioSource>();
            }
            else
            {
                obj = new GameObject("UNITETestSoundPlayerObject");
                obj.hideFlags = flag;
                obj.tag = "sound";
                audioSource = obj.AddComponent<AudioSource>();
            }
            return audioSource;
        }


    }
}