using System;
using Assets.Scripts.Localization;
using Assets.Scripts.Services;
using TMPro;
using UnityEngine;

namespace IdleKnightHero.UI.LocalTexts
{
	[RequireComponent(typeof(TextMeshPro))]
    public class TextMeshLocalizator : MonoBehaviour
    {
		private TextMeshPro _textComponent;
        private String _localizationKey;

        private void Awake()
        {
			_textComponent = GetComponent<TextMeshPro>();
            _localizationKey = _textComponent.text;
            LocalizationText.Language.Changed += LanguageOnChanged;
           LanguageOnChanged(GameSettings.Language);
        }

        private void OnDestroy()
        {
            LocalizationText.Language.Changed -= LanguageOnChanged;
        }

        private void LanguageOnChanged(SystemLanguage language)
        {
            _textComponent.text = LocalizationText.GetText(_localizationKey);
            _textComponent.isRightToLeftText = LocalizationText.Language.Value == SystemLanguage.Arabic;
        }

        public void SetLocalizationKey(String newLocalizationKey)
        {
            _localizationKey = newLocalizationKey;
            LanguageOnChanged(LocalizationText.Language.Value);
        }
    }
}