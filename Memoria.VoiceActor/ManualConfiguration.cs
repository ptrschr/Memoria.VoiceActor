using System;
using System.Collections.Generic;
using System.Linq;

namespace Memoria.VoiceActor
{
    internal static class ManualConfiguration
    {
        internal static void SetTextLineConfiguration(IEnumerable<TextLine> textLines)
        {
            foreach (var textLineConfiguration in ReadTextLineConfigurations())
            {
                var textLine = textLines.FirstOrDefault(textLine => textLine.TextId == textLineConfiguration.TextId && textLine.ZoneId == textLineConfiguration.ZoneId);
                if (textLine == null)
                {
                    continue;
                }

                textLine.ApplyConfiguration(textLineConfiguration);
            }
        }

        internal static IEnumerable<TextLineConfiguration> ReadTextLineConfigurations()
        {
            return new List<TextLineConfiguration>
            {
                new TextLineConfiguration
                {
                    Character="Zidane",
                    TextId="34",
                    ZoneId="2",
                    LanguageId="US"
                }
            };
        }

        internal static IEnumerable<CharacterConfiguration> ReadCharacterConfiguration()
        {
            return new List<CharacterConfiguration>
            {
                new CharacterConfiguration
                {
                    Name = "Zidane"
                }
            };
        }
    }
}