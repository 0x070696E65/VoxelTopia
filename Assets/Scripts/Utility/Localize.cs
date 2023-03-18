using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Symvolution.Scripts
{
    public class Localize : MonoBehaviour
    {
        public const string JP = "JAPANESE";
        public const string EN = "ENGLISH";
        public const string RU = "RUSSIAN";
        public const string IT = "ITALIAN";
        public const string KO = "KOREAN";
        public const string UR = "UKRAINIAN";
        
        //0 JP
        //1 EN
        //2 RU
        //3 IT
        //4 KO
        //5 UR
        private static List<Font> fonts;
        private static List<TMP_FontAsset> tmpFonts;
        
        //[SerializeField] private DialogueSystemController dialogueSystemController;
        //[SerializeField] private ShopManager shopManager;
        
        /// <summary>
        /// ローカライズ対応の国を示すキーが入る
        /// </summary>
        public static string localizeKey = "";
        
        /// <summary>
        /// Localize用のCSVファイルがあるパスをさす
        /// </summary>
        private const string _localizeCSVPath = "Localize";

        private static readonly Dictionary<string, Dictionary<string, string>> _localizeData =
            new Dictionary<string, Dictionary<string, string>>();
        
        public static void Init(List<Font> _fonts, string lungage)
        {
            _SetLocalizeFont(_fonts);
            SetLocalizeKey(lungage);
            _LoadLocalizeData();
        }

        public static void LanguageChange()
        {
            //if(shopManager != null)shopManager.SetLanguage();
        }
        
        private static void _LoadLocalizeData()
        {
            // データが残ってる場合は削除
            if (_localizeData.Count != 0)
            {
                _localizeData.Clear();
            }
            
            string localizeCSV = Resources.Load<TextAsset>(_localizeCSVPath).ToString();
            
            // 一行ごとに切り分ける
            string[] textlines = localizeCSV.Split(new char[]{'\r', '\n'});
            
            // 最初の一行はキーとして利用するため、先に取得しておく
            var firstRow = textlines[0].Split(',');
            
            // _localizeDataに追加処理
            for (var i = 1; i < textlines.Length; i++)
            {
                var row = textlines[i].Split(',');
                _localizeData[row[0]] = new Dictionary<string, string>();
                for (var j = 1; j < row.Length; j++)
                {
                    // キーに該当し、国コードが指定された場所に文字列をセットする
                    try
                    {
                        _localizeData[row[0]][firstRow[j]] = row[j];
                    }
                    catch
                    {
                        throw new Exception(row[j-1] + " : " +i.ToString()+" : "+j.ToString());
                    }
                }
            }
        }

        public static void SetLocalizeKey(string _key)
        {
            localizeKey = _key;
        }

        public static Font GetLocalizeFont()
        {
            return localizeKey switch
            {
                JP => fonts[0],
                EN => fonts[1],
                RU => fonts[2],
                IT => fonts[3],
                KO => fonts[4],
                UR => fonts[5],
                _ => fonts[0]
            };
        }

        private static void _SetLocalizeFont(List<Font> _fonts)
        {
            fonts = _fonts;
        }
        
        public static void SetLocalizeTmpFont(List<TMP_FontAsset> _fonts)
        {
            tmpFonts = _fonts;
        }
        
        public static TMP_FontAsset GetLocalizeTmpFont()
        {
            return localizeKey switch
            {
                JP => tmpFonts[0],
                EN => tmpFonts[1],
                RU => tmpFonts[2],
                IT => tmpFonts[3],
                KO => tmpFonts[4],
                UR => tmpFonts[5],
                _ => tmpFonts[0]
            };
        }

        /// <summary>
        /// ローカライズされた単語を取得する
        /// </summary>
        /// <param name="key">国指定用のコード</param>
        public static string Get(string key)
        {
            if (_localizeData.TryGetValue(key, out var pair))
            {
                var result = pair.TryGetValue(localizeKey, out var text) ? text : key;
                if (result.Contains("\\n"))
                {
                    result = result.Replace(@"\n", Environment.NewLine);
                }
                if (result.Contains("\\c"))
                {
                    result = result.Replace(@"\c", ",");
                }
                return result;
            }
            return key;
        }
    }
}