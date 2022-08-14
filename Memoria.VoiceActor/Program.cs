using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace Memoria.VoiceActor
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var textLines = ReadText("US");

            SaveZoneFiles(textLines);

            StartVoiceActing(textLines);
        }

        private static void SaveZoneFiles(IEnumerable<TextLine> textLines)
        {
            IEnumerable<string> zoneIds = textLines
                .Select(textLine => textLine.ZoneId)
                .Distinct();

            foreach (var zoneId in zoneIds)
            {
                IEnumerable<TextLine> zoneLines = textLines
                    .Where(textLine => textLine
                        .ZoneId
                        .Equals(
                            zoneId,
                            StringComparison.OrdinalIgnoreCase));

                string zonePath = System.IO.Path.Combine(Assembly
                    .GetEntryAssembly()
                    .Location, Settings.Default.ZoneExportPath);

                string zoneJson = JsonConvert.SerializeObject(
                    zoneLines,
                    Formatting.Indented);

                System.IO.File.WriteAllText(zonePath, zoneJson);
            }
        }

        private static void StartVoiceActing(IEnumerable<TextLine> textLines)
        {
            var characterNames = GetCharacterNames(textLines);

            var tags = characterNames.Distinct().ToDictionary(keySelector: m => m, elementSelector: m => m);

            foreach (var textLine in textLines)
            {
                textLine.ReplaceTags(tags);
            }
            var syncItems = textLines.Where(textLine => textLine.ReadyForSync);
        }

        private static IEnumerable<string> GetCharacterNames(IEnumerable<TextLine> textLines)
        {
            List<string> characterNames = new List<string>();
            characterNames.AddRange(textLines.Where(textLine => textLine.ReadyForSync).Select(textLine => textLine.Character));
            return characterNames;
        }

        private static IEnumerable<TextLine> ReadText(string languageId)
        {
            string path = Settings.Default.TextResourcesPath;
            foreach (string languagePath in System.IO.Directory.GetDirectories(path))
            {
                string language = System.IO.Path.GetFileName(languagePath);
                if (language != languageId)
                {
                    continue;
                }
                string fieldPath = System.IO.Path.Combine(languagePath, "Field");
                string[] fieldTextFilePaths = System.IO.Directory.GetFiles(fieldPath);


                List<TextLine> textLines = new List<TextLine>();
                foreach (var fieldTextFilePath in fieldTextFilePaths)
                {
                    IEnumerable<TextLine> fieldFileContent = ReadFieldFile(fieldTextFilePath, language);
                    ManualConfiguration.SetTextLineConfiguration(fieldFileContent);

                    textLines.AddRange(fieldFileContent);
                }
                return textLines;
            }
            throw new InvalidOperationException("Language not found.");
        }

        private static IEnumerable<TextLine> ReadFieldFile(string fieldTextFilePath, string languageId)
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
                    Console.WriteLine("Whats going on?!");
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
