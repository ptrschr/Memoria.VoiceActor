using System.Collections.Generic;
using System.Linq;

namespace Memoria.VoiceActor
{
    internal class CharacterConfiguration
    {
        public string Name { get; set; }
        public int AudioSplitLengthCharacters { get; set; }
        public int ModelToken { get; set; }
        public CharacterConfiguration()
        {

        }

        public CharacterConfiguration(string name)
        {
            Name = name;
            AudioSplitLengthCharacters = 100;
        }

        public static IEnumerable<CharacterConfiguration> GetZoneCharacterConfigurations(Dictionary<string, IEnumerable<TextLine>> zoneData)
        {
            List<string> characterNames = new List<string>();
            foreach (var zoneLines in zoneData)
            {
                characterNames.AddRange(
                    GetCharacterNames(zoneLines.Value).Distinct());
            }

            return characterNames.Distinct().OrderBy(obj => obj).Select(name => new CharacterConfiguration(name));
        }

        private static IEnumerable<string> GetCharacterNames(IEnumerable<TextLine> textLines)
        {
            List<string> characterNames = new List<string>();
            characterNames.AddRange(textLines.Where(textLine => textLine.ReadyForSync).Select(textLine => textLine.Character));
            return characterNames;
        }
    }
}