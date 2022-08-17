using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using System.IO;
using FakeYou_Wrapper;

namespace Memoria.VoiceActor
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            GetPossibleActionInput();
        }

        private static void GetPossibleActionInput()
        {
            Console.WriteLine("Possible Actions:\r\nGenerate Zone Files (1),\r\nGenerate Manual Textline Configuration (2)\r\nGather available voices (3)\r\nStart Voice Acting for all Zones (4),\r\nStart Voice Acting for Zone (5:ZoneID)");
            Console.Write("Choose Action > ");
            var input = Console.ReadLine();
            switch (input)
            {
                case "1":
                    GenerateZoneFiles();
                    break;
                case "2":
                    GenerateManualTextLineConfiguration();
                    break;
                case "3":
                    GatherAvailableVoices();
                    break;
                case "4":
                    StartVoiceActing();
                    break;
                default:
                    if (input.StartsWith("5:"))
                    {
                        try
                        {
                            //input = (int)input.Replace("5:", string.Empty);
                            //StartVoiceActing(input);
                        }
                        catch
                        {
                            GetPossibleActionInput();
                        }
                    }
                    else
                    {
                        GetPossibleActionInput();
                    }
                    break;
            }
        }

        private static void GatherAvailableVoices()
        {
            List<FakeYouCSharp.VoiceModel> voices =
                FakeYouCSharp.GetListOfVoices();

            string availableVoicesPath = System.IO.Path.Combine(
                    Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
                    Settings.Default.AvailableVoicesPath);

            if (File.Exists(availableVoicesPath))
            {
                File.Delete(availableVoicesPath);
            }

            string voicesJson = JsonConvert.SerializeObject(
                    voices,
                    Formatting.Indented);

            System.IO.File.WriteAllText(availableVoicesPath, voicesJson);
            Console.WriteLine($"Generated: \"{availableVoicesPath}\"");

            //Generator generator = new Generator();
            //generator.GenerateInput("", "", "", 100);
        }

        private static void StartVoiceActing()
        {
            throw new NotImplementedException();
        }

        private static void GenerateManualTextLineConfiguration()
        {
            IEnumerable<TextLineConfiguration> textLineConfigurations = new List<TextLineConfiguration>
            {
                new TextLineConfiguration
                {
                    Character = "Zidane",
                    LanguageId = "US",
                    Text = "Temp",
                    TextId = "-1",
                    ZoneId = "-1"
                }
            };

            string textLineConfigurationPath = System.IO.Path.Combine(
                    Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
                    Settings.Default.TextLineConfigurationPath);

            if (File.Exists(textLineConfigurationPath))
            {
                File.Delete(textLineConfigurationPath);
            }

            string textLineConfigurationsJson = JsonConvert.SerializeObject(
                    textLineConfigurations,
                    Formatting.Indented);

            System.IO.File.WriteAllText(textLineConfigurationPath, textLineConfigurationsJson);
            Console.WriteLine($"Generated: \"{textLineConfigurationPath}\"");
        }

        private static void GenerateZoneFiles()
        {
            Console.WriteLine($"Configured {nameof(Settings.Default.TextResourcesPath)}: {Settings.Default.TextResourcesPath}");

            IEnumerable<string> availableLanguages = GetAvailableLanguages(Settings.Default.TextResourcesPath);

            Console.WriteLine($"Available languages: {string.Join(",", availableLanguages)}");

            Console.Write("Choose language > ");

            string language = Console.ReadLine();

            while (string.IsNullOrWhiteSpace(language) || !availableLanguages.Contains(language))
            {
                Console.WriteLine("Invalid language entered.");
                Console.Write("Choose language > ");
                language = Console.ReadLine();
            }

            Dictionary<string, IEnumerable<TextLine>> zoneData = ReadText(language);

            SaveZoneFiles(zoneData);
        }

        private static IEnumerable<string> GetAvailableLanguages(string textResourcesPath)
            => Directory.GetDirectories(textResourcesPath)
            .Select(languageDirectory => Path.GetFileName(languageDirectory));

        private static void SaveZoneFiles(Dictionary<string, IEnumerable<TextLine>> zoneData)
        {
            IEnumerable<CharacterConfiguration> characterConfigurations =
                    CharacterConfiguration.GetZoneCharacterConfigurations(zoneData);

            string zoneBasePath = System.IO.Path.Combine(
                    Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
                    Settings.Default.ZoneExportPath);

            if (Directory.Exists(zoneBasePath))
            {
                Directory.Delete(zoneBasePath, true);
            }
            System.IO.Directory.CreateDirectory(zoneBasePath);

            string characterConfigurationPath = System.IO.Path.Combine(
                    zoneBasePath,
                    Settings.Default.CharacterConfigurationPath);

            string characterConfigurationsJson = JsonConvert.SerializeObject(
                    characterConfigurations,
                    Formatting.Indented);

            System.IO.File.WriteAllText(characterConfigurationPath, characterConfigurationsJson);

            Console.WriteLine($"Generated: \"{characterConfigurationPath}\"");

            foreach (var zoneItem in zoneData)
            {
                string zonePath = System.IO.Path.Combine(
                    zoneBasePath,
                    $"{zoneItem.Key}.JSON");

                string zoneJson = JsonConvert.SerializeObject(
                    zoneItem.Value,
                    Formatting.Indented);

                System.IO.File.WriteAllText(zonePath, zoneJson);

                Console.WriteLine($"Generated: \"{zonePath}\"");
            }
        }

        //private static void StartVoiceActing(Dictionary<string, IEnumerable<TextLine>> textLines)
        //{
        //    var characterNames = GetCharacterNames(textLines);

        //    var tags = characterNames.Distinct().ToDictionary(keySelector: m => m, elementSelector: m => m);

        //    foreach (var textLine in textLines)
        //    {
        //        textLine.ReplaceTags(tags);
        //    }
        //    var syncItems = textLines.Where(textLine => textLine.ReadyForSync);
        //}

        private static Dictionary<string, IEnumerable<TextLine>> ReadText(string languageId)
        {
            string path = Settings.Default.TextResourcesPath;
            string languagePath = Path.Combine(Settings.Default.TextResourcesPath, languageId);
            string fieldPath = System.IO.Path.Combine(languagePath, "Field");

            string[] zoneFiles = System.IO.Directory.GetFiles(fieldPath);

            Dictionary<string, IEnumerable<TextLine>> zoneData = new Dictionary<string, IEnumerable<TextLine>>();
            foreach (var zoneFile in zoneFiles)
            {
                IEnumerable<TextLine> zoneTextLines = ReadZoneFile(zoneFile, languageId);
                ManualConfiguration.SetTextLineConfiguration(zoneTextLines);

                zoneData.Add(zoneTextLines.First().ZoneId, zoneTextLines);
            }
            return zoneData;
        }

        private static IEnumerable<TextLine> ReadZoneFile(string fieldTextFilePath, string languageId)
        {
            string fieldText = System.IO.Path.GetFileNameWithoutExtension(fieldTextFilePath);
            bool textStarted = false;
            List<TextLine> textLines = new List<TextLine>();
            string currentText = string.Empty;
            foreach (var fieldTextLine in System.IO.File.ReadAllLines(fieldTextFilePath))
            {
                if (!textStarted && fieldTextLine.StartsWith($"\"${fieldText}"))
                {
                    textStarted = true;
                    currentText += fieldTextLine;
                }
                else if (textStarted)
                {

                    if (fieldTextLine.StartsWith($"\"${fieldText}"))
                    {
                        textLines.Add(new TextLine(currentText, languageId));
                        currentText = string.Empty;
                    }
                    else
                    {
                        currentText += "\r\n";
                    }

                    currentText += fieldTextLine;
                }
                else
                {
                    textStarted = false;
                }
            }
            if (!string.IsNullOrWhiteSpace(currentText))
            {
                textLines.Add(new TextLine(currentText, languageId));
            }
            return textLines;
        }
    }
}
