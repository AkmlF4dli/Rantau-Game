#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RPGMaker.Codebase.CoreSystem.Knowledge.Misc
{
    public static class CoreSystemLocalize
    {
        // テスト用の強制言語設定。
        private const SystemLanguage ForceSystemLanguage = SystemLanguage.Unknown;

        // 重複キーカウント。
        private static readonly Dictionary<string, int> DuplicateKeyCount = new Dictionary<string, int>();

        // ローカライズ辞書。
        private static readonly Dictionary<string, Dictionary<SystemLanguage, string>> LocaliseDictionary =
            new Dictionary<string, Dictionary<SystemLanguage, string>>();

        // ローカライズ対象外の文字列。
        private static readonly HashSet<string> NonLocalizableStrings = new HashSet<string> { "+", "-", "X", "Y", "%", "％", ":", ";" };

        /**
         * 静的コンストラクタ。
         */
        static CoreSystemLocalize() {
            // 内容が重複した行を除いた、行と列の2次元ジャグ配列を作成。
            var localiseData = new List<List<string>>();
            {
                var lineValueHashSet = new HashSet<string>();
                for (var lineIndex = 0; lineIndex < CoreSystemLocalizeData.LocaliseData.GetLength(0); lineIndex++)
                {
                    var localiseLineData = new List<string>();
                    for (var columnIndex = 0; columnIndex < (int) CoreSystemLocalizeData.DataType.ValueBottom + 1; columnIndex++)
                    {
                        localiseLineData.Add(CoreSystemLocalizeData.LocaliseData[lineIndex, columnIndex]);
                    }

                    var lineValue = string.Join("\t", localiseLineData);

                    // 全ての値が同じ行は追加しない。
                    if (lineValueHashSet.Contains(lineValue))
                    {
                        continue;
                    }

                    lineValueHashSet.Add(lineValue);
                    localiseData.Add(localiseLineData);
                }
            }

            // 重複キーを考慮した翻訳用辞書を作成。
            foreach (var lineValues in localiseData)
            {
                var key = lineValues[(int) CoreSystemLocalizeData.DataType.Key];

                if (!DuplicateKeyCount.ContainsKey(key))
                {
                    DuplicateKeyCount.Add(key, 0);
                }

                DuplicateKeyCount[key]++;

                // 最初のキー重複時は、1つ目の要素のキーを重複用のものに変更。
                if (DuplicateKeyCount[key] == 2)
                {
                    continue;
                }

                // 重複キーを重複用のものに変更。
                if (DuplicateKeyCount[key] >= 2)
                {
                    var newKey = key + DuplicateKeySuffix(DuplicateKeyCount[key]);
                    InitalizeDuplicateKeyLog(
                        key,
                        newKey,
                        lineValues.Skip((int) CoreSystemLocalizeData.DataType.ValueTop).Take((int) CoreSystemLocalizeData.ValueCount).ToList());
                    key = newKey;
                }

                LocaliseDictionary.Add(
                    key,
                    new Dictionary<SystemLanguage, string>
                    {
                        { SystemLanguage.Japanese, lineValues[(int)CoreSystemLocalizeData.DataType.Japanese] },
                        { SystemLanguage.English, lineValues[(int)CoreSystemLocalizeData.DataType.English] },
                        { SystemLanguage.Chinese, lineValues[(int)CoreSystemLocalizeData.DataType.Chinese] },
                    });
            }
        }

        /**
         * ディクショナリ内の値テキストをローカライズしたディクショナリに変換する。
         */
        public static Dictionary<T, string> LocalizeDictionaryValues<T>(Dictionary<T, string> dictionary) {
            var keys = new List<T>(dictionary.Keys);
            foreach (var key in keys)
            {
                dictionary[key] = LocalizeText(dictionary[key]);
            }

            return dictionary;
        }

        /**
         * テキストをローカライズしたテキストに変換する。
         */
        public static string LocalizeText(string text) {
            var trimmedText = text.Trim();

            // null、空白、数値に解釈できる文字列、ローカライズ対象外の文字列は変換しない。
            if (string.IsNullOrWhiteSpace(text) ||
                decimal.TryParse(trimmedText, out decimal _) ||
                NonLocalizableStrings.Contains(trimmedText))
            {
                return text;
            }

            if (LocaliseDictionary.TryGetValue(text, out Dictionary<SystemLanguage, string> textByLanguages))
            {
                text = textByLanguages[GetKeyLanguage()];
            }
            else
            {
                LocalizeFailureLog(ref text);
            }

            return text;
        }

        /**
         * ローカライズ翻訳値リストを取得。
         */
        private static List<string> GetLocaliseValues(string key) {
            return new List<string>
                {
                    LocaliseDictionary[key][SystemLanguage.Japanese],
                    LocaliseDictionary[key][SystemLanguage.English],
                    LocaliseDictionary[key][SystemLanguage.Chinese]
                };
        }

        /**
         * キー重複時のキーに追加する接尾辞を取得。
         */
        private static string DuplicateKeySuffix(int count) {
            return $" ({count})";
        }


        /**
         * 初期化時のキー重複ログ出力。
         */
        [System.Diagnostics.Conditional("DEBUG")]
        private static void InitalizeDuplicateKeyLog(string key, string newKey, List<string> values) {
            var valuesString = string.Join(", ", values.Select(v => $"\"{v}\""));
        }

        /**
         * ローカライズ失敗ログ出力。
         */
        [System.Diagnostics.Conditional("DEBUG")]
        private static void LocalizeFailureLog(ref string text) {
        }

        /**
         * キーで使用する言語を取得。
         */
        private static SystemLanguage GetKeyLanguage() {
            var language = GetOsLanguage();
            switch (language)
            {
                case SystemLanguage.Japanese:
                    return SystemLanguage.Japanese;

                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.ChineseTraditional:
                    return SystemLanguage.Chinese;

                default:
                    return SystemLanguage.English;
            }
        }

        /**
         * オペレーティングシステムの設定言語を取得。
         */
        private static SystemLanguage GetOsLanguage() {

            return Application.systemLanguage;
        }
    }
}

#endif