namespace PokemonStatsReporter.Services.Localization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;

    using PokemonStatsReporter.Extensions;
    using PokemonStatsReporter.Utilities;

    public class Translator : Language<string, string, Dictionary<string, string>>
    {
        #region Variables

        private const string SourceLocaleUrl = "https://raw.githubusercontent.com/WatWowMap/pogo-translations/master/static/locales/";
        private static readonly string _appLocalesFolder = Directory.GetCurrentDirectory() + $"/../{Strings.LocaleFolder}";
        private static readonly string _binLocalesFolder = Directory.GetCurrentDirectory() + $"/{Strings.BasePath}/{Strings.LocaleFolder}";

        private static readonly ILogger<Translator> _logger =
            new Logger<Translator>(LoggerFactory.Create(x => x.AddConsole()));

        #endregion

        #region Singleton

        private static Translator _instance;
        public static Translator Instance =>
            _instance ??= new Translator
            {
                LocaleDirectory = _binLocalesFolder,
                //CurrentCulture = 
            };

        #endregion

        #region Static Methods

        public static async Task CreateLocaleFilesAsync()
        {
            // Copy any missing base locale files to bin directory
            await CopyLocaleFilesAsync();

            var files = GetBaseLocaleFileNames();
            foreach (var file in files)
            {
                // Replace locale prefix
                var localeFile = Path.GetFileName(file).Replace("_", null);
                var locale = Path.GetFileNameWithoutExtension(localeFile);

                var url = SourceLocaleUrl + localeFile;
                var json = await NetUtils.GetAsync(url);
                if (json == null)
                {
                    _logger.LogWarning($"Failed to fetch locales from {url}, skipping...");
                    return;
                }

                _logger.LogInformation($"Creating locale {locale}...");
                var remote = json.FromJson<Dictionary<string, string>>();
                var keys = remote.Keys.ToList();
                for (var i = 0; i < keys.Count; i++)
                {
                    var key = keys[i];
                    // Make locale variables compliant with Handlebars/Mustache templating
                    remote[key] = remote[key].Replace("%", "{")
                                             .Replace("}", "}}");
                }

                if (locale != "en")
                {
                    // Include en as fallback first
                    var enTransFallback = File.ReadAllText(
                        Path.Combine(_appLocalesFolder, "_en.json")
                    );
                    var fallbackTranslations = enTransFallback.FromJson<Dictionary<string, string>>();
                    remote = remote.Merge(fallbackTranslations, updateValues: true);
                }

                var appTranslationsData = File.ReadAllText(Path.Combine(_appLocalesFolder, file));
                var appTranslations = appTranslationsData.FromJson<Dictionary<string, string>>();
                remote = remote.Merge(appTranslations, updateValues: true);

                File.WriteAllText(
                    Path.Combine(_binLocalesFolder, localeFile),
                    remote.ToJson()
                );
                _logger.LogInformation($"{locale} file saved.");
            }
        }

        #endregion

        #region Public Methods

        public override string Translate(string value)
        {
            try
            {
                return base.Translate(value) ?? value;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to find locale translation for key '{value}'\nError: {ex}");
            }
            return value;
        }

        public string Translate(string value, params object[] args)
        {
            try
            {
                var text = args?.Length > 0
                    ? string.Format(base.Translate(value), args)
                    : base.Translate(value);
                return text ?? value;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to find locale translation for key '{value}' and arguments: '{string.Join(",", args)}'\nError: {ex}");
            }
            return value;
        }

        public string GetPokemonName(uint pokeId)
        {
            return Translate($"poke_{pokeId}");
        }

        public string GetFormName(uint formId, bool includeNormal = false)
        {
            if (formId == 0)
                return null;

            var form = Translate("form_" + formId);
            var normal = Translate("NORMAL");
            if (!includeNormal && string.Compare(form, normal, true) == 0)
                return string.Empty;
            return form;
        }

        public string GetCostumeName(uint costumeId)
        {
            if (costumeId == 0)
                return null;

            var costume = Translate("costume_" + costumeId);
            return costume;
        }

        public string GetEvolutionName(uint evoId)
        {
            if (evoId == 0)
                return null;

            var evo = Translate("evo_" + evoId);
            return evo;
        }

        #endregion

        #region Private Methods

        private static async Task CopyLocaleFilesAsync()
        {
            // Copy base locale files from app directory to bin directory if they do not exist
            var files = GetBaseLocaleFileNames();
            foreach (var file in files)
            {
                // Replace locale prefix
                var localeFile = Path.GetFileName(file);
                var localeBin = Path.Combine(_binLocalesFolder, localeFile);
                if (File.Exists(localeBin))
                    continue;

                _logger.LogDebug($"Copying base locale '{localeFile}' to {localeBin}...");
                var baseLocalePath = Path.Combine(_appLocalesFolder, file);
                File.Copy(baseLocalePath, localeBin);
            }

            await Task.CompletedTask;
        }

        private static List<string> GetBaseLocaleFileNames(string extension = "*.json", string prefix = "_")
        {
            // Get a list of locale file names that have prefix '_'
            var files = Directory.GetFiles(_appLocalesFolder, extension)
                                 .Select(fileName => Path.GetFileName(fileName))
                                 .Where(fileName => fileName.StartsWith(prefix))
                                 .ToList();
            return files;
        }

        #endregion
    }
}