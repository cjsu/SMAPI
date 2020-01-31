using System.Collections.Generic;
using StardewValley;

namespace StardewModdingAPI.Framework.ModHelpers
{
    /// <summary>Provides translations stored in the mod's <c>i18n</c> folder, with one file per locale (like <c>en.json</c>) containing a flat key => value structure. Translations are fetched with locale fallback, so missing translations are filled in from broader locales (like <c>pt-BR.json</c> &lt; <c>pt.json</c> &lt; <c>default.json</c>).</summary>
    internal class TranslationHelper : BaseHelper, ITranslationHelper
    {
        /*********
        ** Fields
        *********/
        /// <summary>The underlying translation manager.</summary>
        private readonly Translator Translator;


        /*********
        ** Accessors
        *********/
        /// <summary>The current locale.</summary>
        public string Locale => this.Translator.Locale;

        /// <summary>The game's current language code.</summary>
        public LocalizedContentManager.LanguageCode LocaleEnum => this.Translator.LocaleEnum;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="modID">The unique ID of the relevant mod.</param>
        /// <param name="locale">The initial locale.</param>
        /// <param name="languageCode">The game's current language code.</param>
        public TranslationHelper(string modID, string locale, LocalizedContentManager.LanguageCode languageCode)
            : base(modID)
        {
            this.Translator = new Translator();
            this.Translator.SetLocale(locale, languageCode);
        }

        /// <summary>Get all translations for the current locale.</summary>
        public IEnumerable<Translation> GetTranslations()
        {
            return this.Translator.GetTranslations();
        }

        /// <summary>Get a translation for the current locale.</summary>
        /// <param name="key">The translation key.</param>
        public Translation Get(string key)
        {
            return this.Translator.Get(key);
        }

        /// <summary>Get a translation for the current locale.</summary>
        /// <param name="key">The translation key.</param>
        /// <param name="tokens">An object containing token key/value pairs. This can be an anonymous object (like <c>new { value = 42, name = "Cranberries" }</c>), a dictionary, or a class instance.</param>
        public Translation Get(string key, object tokens)
        {
            return this.Translator.Get(key, tokens);
        }

        /// <summary>Set the translations to use.</summary>
        /// <param name="translations">The translations to use.</param>
        internal TranslationHelper SetTranslations(IDictionary<string, IDictionary<string, string>> translations)
        {
            this.Translator.SetTranslations(translations);
            return this;
        }

        /// <summary>Set the current locale and precache translations.</summary>
        /// <param name="locale">The current locale.</param>
        /// <param name="localeEnum">The game's current language code.</param>
        internal void SetLocale(string locale, LocalizedContentManager.LanguageCode localeEnum)
        {
            this.Translator.SetLocale(locale, localeEnum);
        }
    }
}
