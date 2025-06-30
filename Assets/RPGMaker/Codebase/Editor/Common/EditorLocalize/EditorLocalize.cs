using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGMaker.Codebase.Editor.Common
{
    /// <summary>
    /// エディターローカライズ クラス
    /// 
    /// ◆ローカライズ方針
    ///  大きく分けて2つ。
    /// 
    ///  1. uxml に直接記載しているテキスト文字の場合。
    ///  
    ///      uxml を読み込んだ直後に、uxml 内に含まれている text 文字列を、ローカライズ対象として置換する。
    ///      
    ///      対象クラスは以下 (追加の可能性あり)。
    ///          Label
    ///          Button
    ///		
    ///		使用メソッドは以下。
    ///		    public static VisualElement LocalizeElements(VisualElement visualElement)
    ///
    ///
    ///  2. プログラム内で直接埋め込んでいるテキストの場合。
    ///  
    ///	    たとえば、イベント一覧等では、設定しているパラメータに応じて表示する文字を変更する必要がある。
    ///      そのようなケースは、プログラム内部でローカライズを直接行う。
    ///
    ///		使用メソッドは以下。
    ///          public static string LocalizeWindowTitle(string text);
    ///          public static string LocalizeText(string text)
    ///          public static List<string> LocalizeTexts(List<string> texts)
    ///          public static Dictionary<T, string> LocalizeDictionaryValues<T>(Dictionary<T, string> dictionary)
    ///          
    /// ◆ローカライズ用データ
    /// 
    /// ローカライズデータは、クラス EditorLocalizeData で定義している。
    /// キーが重複している場合、後から出現したキーは無視される
    ///
    /// ◆設定言語の取得
    /// Unityエディターの設定言語は非公開クラス LocalizationDatabase のプロパティから取得している。
    /// UnityEditor.LocalizationDatabase.currentEditorLanguageProperty
    /// 起動時・エディタ言語切替時は再コンパイルが走りEditorLocalizeが破棄されるので、コンストラクタで切替後の言語でDictionaryを再構築する
    /// 言語設定を更新する際はTrySetSystemLanguageを使用する。この際もDictionaryを再構築する
    /// </summary>
    public static class EditorLocalize
    {
        public static bool TrySetSystemLanguage(SystemLanguage systemLanguage) {
            if (UniteEditorLanguage == systemLanguage)
            {
                return false;
            }
            UniteEditorLanguage = systemLanguage;
            UpdateLocalizeDictionary();
            return true;
        }

        // システム設定言語
        private static SystemLanguage UniteEditorLanguage = SystemLanguage.Unknown;
        // ローカライズ辞書。
        private static readonly Dictionary<string, string> CurrentDictionay = new Dictionary<string, string>();
        // ローカライズ対象外の文字列。
        private static readonly HashSet<string> NonLocalizableStrings = new HashSet<string> { "+", "-", "X", "Y", "%", "％", ":", ";" };

        /// <summary>
        /// 静的コンストラクタ。
        /// </summary>
        static EditorLocalize() {
            UpdateLocalizeDictionary();
        }

        /// <summary>
        /// ローカライズ辞書を更新する
        /// </summary>
        static void UpdateLocalizeDictionary() {
            // 言語設定の取得
            // 言語設定時にビルドが走って再取得できるのでここで１回のみ行う
            var currentLanguage = GetNowLanguage();
            int languageIndex = (int) EditorLocalizeData.DataType.Japanese;
            switch (currentLanguage)
            {
                case SystemLanguage.English:
                    languageIndex = (int) EditorLocalizeData.DataType.English;
                    break;
                case SystemLanguage.Chinese:
                    languageIndex = (int) EditorLocalizeData.DataType.Chinese;
                    break;
            }

            // 辞書抽出
            var localizeData = EditorLocalizeData.LocaliseData;
            CurrentDictionay.Clear();
            for (var lineIndex = 0; lineIndex < localizeData.GetLength(0); lineIndex++)
            {
                if (CurrentDictionay.ContainsKey(localizeData[lineIndex, 0]))
                {
                    // 重複キーの検出
                }
                else
                {
                    CurrentDictionay.Add(localizeData[lineIndex, 0], localizeData[lineIndex, languageIndex]);
                }
            }
        }

        /// <summary>
        /// ウィンドウタイトルをローカライズしたテキストに変換する。
        /// ウィンドウタイトルは特別な処理をする可能性を考慮し、変換用専用メソッドを用意。
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string LocalizeWindowTitle(string text) {
            return LocalizeText(text);
        }

        /// <summary>
        /// テキストリストをローカライズしたテキストリストに変換する。
        /// </summary>
        /// <param name="texts"></param>
        /// <returns></returns>
        public static List<string> LocalizeTexts(List<string> texts) {
            var result = new List<string>();
            texts.ForEach(text => result.Add(LocalizeText(text)));
            return result;
        }

        /// <summary>
        /// ディクショナリ内の値テキストをローカライズしたディクショナリに変換する。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public static Dictionary<T, string> LocalizeDictionaryValues<T>(Dictionary<T, string> dictionary) {
            var keys = new List<T>(dictionary.Keys);
            foreach (var key in keys) dictionary[key] = LocalizeText(dictionary[key]);
            return dictionary;
        }

        /// <summary>
        /// テキストをローカライズしたテキストに変換する。
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string LocalizeText(string text) {
            var trimmedText = text.Trim();

            // null、空白、数値に解釈できる文字列、ローカライズ対象外の文字列は変換しない。
            if (string.IsNullOrWhiteSpace(text) ||
                decimal.TryParse(trimmedText, out _) ||
                NonLocalizableStrings.Contains(trimmedText))
                return text;

            if (CurrentDictionay.TryGetValue(text, out var convertedText))
            {
                return convertedText;
                //LocalizeFailureLog(ref text);
                // テキストが見つからなかった
            }
            return text;
        }

        /// <summary>
        /// テキストをローカライズしたテキストをフォーマットを変換してフォーマットを通す。
        /// </summary>
        /// <param name="text">テキスト</param>
        /// <param name="arg">フォーマットの引数</param>
        /// <returns>フォーマットを通した後の文字列</returns>
        public static string LocalizeTextFormat(string text, params string[] arg) {
            return string.Format(LocalizeText(text), arg);
        }

        /// <summary>
        /// 指定VisualElement以下のヒエラルキーの対象となる型に設定されたテキストをローカライズしたテキストに変換する。
        /// </summary>
        /// <param name="visualElement"></param>
        /// <returns></returns>
        public static VisualElement LocalizeElements(VisualElement visualElement) {
            if (visualElement != null)
            {
                LocalizeTypedText<Label>(visualElement);
                LocalizeTypedText<Button>(visualElement);
            }
            return visualElement;
        }

        /// <summary>
        /// 指定VisualElement以下のヒエラルキーの指定の型に設定されたテキストをローカライズしたテキストに変換する。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="visualElement"></param>
        /// <returns></returns>
        private static void LocalizeTypedText<T>(VisualElement visualElement) where T : TextElement {
            visualElement.Query<T>().ForEach(element => element.text = LocalizeText(element.text));
        }

        /// <summary>
        /// 現在の言語設定を取得
        /// </summary>
        /// <returns></returns>
        public static SystemLanguage GetNowLanguage() {
            var systemLanguage = GetOsLanguage();
            //GetOsLanguage
            switch (systemLanguage)
            {
                case SystemLanguage.Japanese:
                    return SystemLanguage.Japanese;
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.ChineseTraditional:
                    return SystemLanguage.Chinese;
            }
            return SystemLanguage.English;
        }

        /// <summary>
        /// オペレーティングシステムの設定言語を取得。
        /// </summary>
        /// <returns></returns>
        private static SystemLanguage GetOsLanguage() {
            if (UniteEditorLanguage == SystemLanguage.Unknown)
            {
                UniteEditorLanguage = Application.systemLanguage;
            }
            return UniteEditorLanguage;
        }

        /// <summary>
        /// Unityエディターの設定言語を取得。
        /// 非公開クラス UnityEditor.LocalizationDatabase のプロパティ currentEditorLanguage の値を取得する。
        /// </summary>
        /// <returns></returns>
        private static SystemLanguage GetCurrentEditorLanguage() {
            var assembly = typeof(EditorWindow).Assembly;
            var localizationDatabaseType = assembly.GetType("UnityEditor.LocalizationDatabase");
            var currentEditorLanguageProperty = localizationDatabaseType.GetProperty("currentEditorLanguage");
            return (SystemLanguage) currentEditorLanguageProperty.GetValue(null);
        }
    }
}
