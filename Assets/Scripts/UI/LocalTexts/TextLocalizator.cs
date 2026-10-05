using System;
using System.Linq;
using Assets.Scripts.Localization;
using Assets.Scripts.Services;
using TMPro;
using UnityEngine;
using Object = System.Object;

namespace IdleKnightHero.UI.LocalTexts
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TextLocalizator : MonoBehaviour
    {
        private TextMeshProUGUI _textComponent;
        private String _localizationKey;
        private Object[] _params;

        private TextMeshProUGUI TextComponent { get { return _textComponent ?? (_textComponent = GetComponent<TextMeshProUGUI>()); } }

#if UNITY_EDITOR
        public String LocalizationKey { get { return TextComponent.text; } }
#endif

        private void Awake()
        {
            TrySetLocalizationKey();
            LocalizationText.Language.Changed += LanguageOnChanged;
            LanguageOnChanged(GameSettings.Language);
        }

        private void OnDestroy()
        {
            LocalizationText.Language.Changed -= LanguageOnChanged;
        }

        private void TrySetLocalizationKey()
        {
            if (String.IsNullOrEmpty(_localizationKey))
            {
                _localizationKey = TextComponent.text;
            }
        }

        private void LanguageOnChanged(SystemLanguage language)
        {
            if (_params == null)
            {
                TextComponent.text = LocalizationText.GetText(_localizationKey);
                TextComponent.isRightToLeftText = LocalizationText.Language.Value == SystemLanguage.Arabic;
            }
            else
            {
                SetParams(_params);
            }
        }

        public void SetLocalizationKey(String newLocalizationKey)
        {
            _localizationKey = newLocalizationKey;
            LanguageOnChanged(LocalizationText.Language.Value);
        }

        public void SetParams(params Object[] args)
        {
            TrySetLocalizationKey();
            _params = args;
            if (LocalizationText.Language.Value == SystemLanguage.Arabic)
            {
                var localizedString = LocalizationText.GetText(_localizationKey);
                var substitutionStartIndex = -1;
                var substitutionLength = -1;
                var substitutionArgumentIndex = -1;
                for (var j = 0; j < localizedString.Length; ++j)
                {
                    if (localizedString[j] == '{')
                    {
                        substitutionStartIndex = j;
                        substitutionLength = 1;
                        var argumentIndex = "";
                        for (var k = j + 1; k < localizedString.Length && Char.IsNumber(localizedString[k]); ++k)
                        {
                            argumentIndex += localizedString[k];
                        }
                        if (!Int32.TryParse(argumentIndex, out substitutionArgumentIndex))
                        {
                            Debug.LogError("Failed to parse substitution index!");
                            substitutionStartIndex = -1;
                            substitutionLength = -1;
                        }
                    }
                    else
                    {
                        if (substitutionLength != -1)
                        {
                            substitutionLength++;
                            if (localizedString[j] == '}')
                            {
                                var substitution = localizedString.Substring(substitutionStartIndex, substitutionLength).Remove(1, substitutionArgumentIndex.ToString().Length).Insert(1, "0");
                                var actualValue = String.Format(substitution, args[substitutionArgumentIndex]);
                                if (actualValue[actualValue.Length - 1] == '%')
                                {
                                    actualValue = '%' + actualValue.Substring(0, actualValue.Length - 1);
                                }
                                if (actualValue.Any(Char.IsNumber))
                                {
                                    var charArray = actualValue.ToCharArray();
                                    Array.Reverse(charArray); //suitable only for string without graphemes (so it's fine for numbers)
                                    actualValue = new String(charArray);
                                }
                                localizedString = localizedString.Remove(substitutionStartIndex, substitutionLength);
                                localizedString = localizedString.Insert(substitutionStartIndex, actualValue);
                                j += actualValue.Length - substitutionLength;
                                substitutionStartIndex = -1;
                                substitutionLength = -1;
                            }
                        }
                    }
                }
                TextComponent.text = localizedString;
                TextComponent.isRightToLeftText = true;
            }
            else
            {
                TextComponent.text = String.Format(LocalizationText.GetText(_localizationKey), args);
                TextComponent.isRightToLeftText = false;
            }
        }
    }
}