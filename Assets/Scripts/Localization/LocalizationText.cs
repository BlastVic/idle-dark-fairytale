using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Assets.Scripts.Extentions;
using Assets.Scripts.Services;
using UnityEngine;

namespace Assets.Scripts.Localization
{
    public static class LocalizationText
    {
        private static readonly IDictionary<String, String> _content = new Dictionary<String, String>();

        public static Observable<SystemLanguage> Language { get; private set; }

        static LocalizationText()
        {
            Language = new Observable<SystemLanguage>(GameSettings.Language);
            Language.Changed += LanguageOnChanged;
        }

        public static void Initialize()
        {
            switch (GameSettings.Language)
            {
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.ChineseTraditional:
                {
                    Language.Value = GameSettings.Language;
                    break;
                }
                default:
                {
                    Language.Value = SystemLanguage.English;
                    break;
                }
            }
        }

        private static void LanguageOnChanged(SystemLanguage language)
        {
            GameSettings.Language = language;
            CreateContent();
        }

        public static String GetText(String key)
        {
            String result;
            Content.TryGetValue(key, out result);

            return String.IsNullOrEmpty(result) ? key : result;
        }

        private static IDictionary<String, String> Content
        {
            get
            {
                if(_content==null || _content.Count == 0)
                    CreateContent();
                return _content;			
            }
        }

        private static void AddContent(XmlNode xNode)
        {
            var languageName = Language.Value == SystemLanguage.Chinese
                ? SystemLanguage.ChineseSimplified.ToString()
                : Language.Value.ToString();
            foreach (XmlNode node in xNode.ChildNodes)
            {
                if (node.LocalName == "TextKey")
                {
                    var value = node.Attributes.GetNamedItem("name").Value;
                    foreach (XmlNode langNode in node)
                    {
                        if (langNode.LocalName == languageName)
                        {
                            var text = langNode.InnerText;
                            if (_content.ContainsKey(value))
                            {
                                _content.Remove(value);
                                _content.Add(value, value + " has been found multiple times in the XML allowed only once!");
                            }
                            else
                            {
                                _content.Add(value, String.IsNullOrEmpty(text) ? "Not found: " + value : text.UnescapeCharacters());
                            }
                            break;
                        }
                    }
                }
            }
        }

        private static void CreateContent()
        {
            var xmlDocument = new XmlDocument();
            var stringReader = new StringReader(Resources.Load<TextAsset>("IdleLocalTexts").text);

            xmlDocument.LoadXml(stringReader.ReadToEnd());
            
            if (_content != null)
            {
                _content.Clear();
            }
            var xNode = xmlDocument.ChildNodes.Item(1);
            AddContent(xNode);
        }
    }
}
