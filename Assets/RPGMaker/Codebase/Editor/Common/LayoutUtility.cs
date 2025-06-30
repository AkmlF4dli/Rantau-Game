using System;
using UnityEditor;

namespace RPGMaker.Codebase.Editor.Common
{
    public class LayoutUtility
    {
        public static void LoadLayout(string path) {
            //バージョンアップ時の隙間でのみエラーになる問題の対処
            try
            {
                EditorUtility.LoadWindowLayout(path);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log(e.Message);
            }
        }
    }
}