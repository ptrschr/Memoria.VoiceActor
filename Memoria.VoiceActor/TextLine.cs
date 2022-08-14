using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Memoria.VoiceActor
{
    public class TextLine
    {
        public string ZoneId { get; set; }
        public string TextId { get; set; }
        private string TextRaw { get; }
        private string MessageRaw { get; set; }
        public string Message { get; set; }
        public string LanguageId { get; set; }
        public bool DialogAvailable
        {
            get { return !string.IsNullOrWhiteSpace(Message); }
        }

        public bool ReadyForSync
        {
            get { return DialogAvailable && !string.IsNullOrWhiteSpace(Character); }
        }
        public string Character { get; set; }

        public TextLine(string text, string languageId)
        {
            TextRaw = text;

            string[] textSplit = text.Split(" = ");
            ReadMeta(textSplit[0]);
            if (languageId == "GR")
            {
                ReadText_GR(textSplit[1]);
            }
            if (languageId == "US")
            {
                ReadText_US(textSplit[1]);
            }
            
            LanguageId = languageId;
        }

        internal void ApplyConfiguration(TextLineConfiguration textLineConfiguration)
        {
            if (TextId != textLineConfiguration.TextId || ZoneId != textLineConfiguration.ZoneId || LanguageId != textLineConfiguration.LanguageId)
            {
                throw new InvalidOperationException($"Tried to set a configuration to {nameof(TextLine)} which is not intended.");
            }
            if (!string.IsNullOrWhiteSpace(textLineConfiguration.Character))
            {
                Character = textLineConfiguration.Character;
            }
            if (!string.IsNullOrWhiteSpace(textLineConfiguration.Text))
            {
                Message = textLineConfiguration.Character;
            }
        }

        private void ReadText_GR(string text)
        {
            string newText;
            var firstRow = text.Split("\r\n")[0];
            bool containsCharacter = firstRow.Contains(":");

            if (containsCharacter)
            {
                List<string> characterSplit = text.Split(":").ToList();
                newText = string.Join(':', characterSplit.GetRange(1, characterSplit.Count - 1));
                ReadCharacter(characterSplit[0]);
            }
            else
            {
                newText = text;
            }
            MessageRaw = newText;
            Message = newText;
        }

        private void ReadText_US(string text)
        {
            string newText;
            bool containsCharacter = text.Contains("“");

            if (containsCharacter)
            {
                var firstRow = text.Split("\r\n")[0];
                List<string> characterSplit = firstRow.Split("“").ToList();
                ReadCharacter(characterSplit[0]);

                List<string> messageSplit = text.Split("“").ToList();
                newText = string.Join('“', messageSplit.GetRange(1, messageSplit.Count - 1));
            }
            else
            {
                newText = text;
            }
            MessageRaw = newText;
            Message = newText;
        }

        public void ReplaceTags(Dictionary<string, string> tags)
        {
            foreach (var tag in tags)
            {
                Message = Message.Replace("{" + tag.Key + "}", tag.Value);
            }
            Regex pattern = new Regex(@"\{.*?}");
            Message = pattern.Replace(Message, string.Empty).Replace("\"", string.Empty).Replace(";", string.Empty).Replace("”", string.Empty);
        }

        private void ReadCharacter(string characterInput)
        {
            var characterInputSplit = characterInput.Split("}");
            string characterName;
            if (string.IsNullOrWhiteSpace(characterInputSplit[characterInputSplit.Length -1]))
            {
                characterName = characterInputSplit[characterInputSplit.Length - 2];
            }
            else
            {
                characterName = characterInputSplit[characterInputSplit.Length - 1];
            }
            characterName = characterName.Replace("{", string.Empty);
            this.Character = characterName;

        }

        private void ReadMeta(string meta)
        {
            meta = meta.Replace("\"", string.Empty).Replace("$", string.Empty);
            string[] metaSplit = meta.Split("_");
            ZoneId = RemoveLeadingZeros(metaSplit[0]);
            TextId = RemoveLeadingZeros(metaSplit[metaSplit.Length - 1]);
        }

        // Function to remove all leading
        // zeros from a given string
        private string RemoveLeadingZeros(string str)
        {
            Regex pattern = new Regex("^0+(?!$)");
            return pattern.Replace(str, string.Empty);
        }
    }
}