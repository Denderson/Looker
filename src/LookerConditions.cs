using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Looker.Plugin;

namespace Looker
{
    public static class LookerConditionsClass
    {
        private static bool _checkFailed;
        private static readonly int StaticRandom = RXRandom.Int(100);

        public static bool? LookerConditions(string text, RainWorldGame game)
        {
            if (!text.ToLower().Contains("looker") || game == null || !game.IsStorySession)
            {
                return null;
            }

            string[] array;
            char? sign = null;
            if (text.Contains("="))
            {
                sign = '=';
                array = text.Split('=');
            }
            else if (text.Contains(">"))
            {
                sign = '>';
                array = text.Split('>');
            }
            else if (text.Contains('<'))
            {
                sign = '<';
                array = text.Split('<');
            }
            else if (text.Contains('-'))
            {
                sign = '-';
                array = text.Split('-');
            }
            else
            {
                array = [text];
            }

            bool? result;
            if (sign == null)
            {
                switch (array[0].ToLowerInvariant())
                {
                    case "lookerelse":
                        {
                            result = _checkFailed;
                            break;
                        }
                    case "lookerskywhales":
                        {
                            result = !OptionsMenu.noSkyWhales.Value;
                            break;
                        }
                    case "lookermask":
                        {
                            result = SaveFileCode.GetBool(game.GetStorySession.saveState, "ReachedThrone");
                            break;
                        }
                    case "lookerending1":
                        {
                            result = SaveFileCode.GetBool(game.GetStorySession.saveState, "BathEnding");
                            break;
                        }
                    case "lookerending2":
                        {
                            result = SaveFileCode.GetBool(game.GetStorySession.saveState, "MaskEnding");
                            break;
                        }
                    case "lookerending3":
                        {
                            result = SaveFileCode.GetBool(game.GetStorySession.saveState, "LinkEnding");
                            break;
                        }
                    case "lookerending4":
                        {
                            result = SaveFileCode.GetBool(game.GetStorySession.saveState, "PuzzleEnding");
                            break;
                        }
                    case "lookersliver":
                        {
                            result = OptionsMenu.metSliver.Value;
                            break;
                        }
                    default: return null;
                }
            }
            else if (array.Length == 2 && int.TryParse(array[1], out var condition))
            {
                int value;
                switch (array[0].ToLowerInvariant())
                {
                    case "lookerremix":
                        {
                            value = OptionsMenu.spawnFileDifficulty.Value;
                            break;
                        }
                    case "lookerrandom":
                        {
                            value = RXRandom.Int(100);
                            break;
                        }
                    case "lookerstaticrandom":
                        {
                            value = StaticRandom;
                            break;
                        }
                    case "lookerwarp":
                        {
                            value = SaveFileCode.LinkCount(game.GetStorySession.saveState);
                            break;
                        }
                    default: return null;
                }
                result = ((sign == '=' && value == condition) || (sign == '>' && value > condition) || (sign == '<' && value < condition) || (sign == '-' && value >= condition));
            }
            else return null;

            if (result != null) _checkFailed = (bool)!result;
            return result;
        }
    }
}
