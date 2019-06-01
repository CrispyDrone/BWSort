using ReplayParser.Interfaces;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using ReplayParser.ReplaySorter.Extensions;
using System.Text;

namespace ReplayParser.ReplaySorter.Renaming
{
    public class CustomReplayFormat
    {
        #region private

        #region fields

        #region static

        //TODO validate regexes
        private static readonly Regex _escapeCharacter = new Regex(@"/");
        private static readonly string _playerInfoOption1 =  @"(/p\s*(/[Rr]\s*)?(/[Ww]\s*)?)";
        private static readonly string _playerInfoOption2 = @"(/p\s*(/[Ww]\s*)?(/[Rr]\s*)?)";
        private static readonly string _playerInfoOption3 = @"(/[Rr]\s*(/p\s*)?(/[Ww]\s*)?)";
        private static readonly string _playerInfoOption4 = @"(/[Rr]\s*(/[Ww]\s*)?(/p\s*)?)";
        private static readonly string _playerInfoOption5 = @"(/[Ww]\s*(/p\s*)?(/[Rr]\s*)?)";
        private static readonly string _playerInfoOption6 = @"(/[Ww]\s*(/[Rr]\s*)?(/p\s*)?)";

        private static readonly Dictionary<Regex, CustomReplayNameSyntax> _formatRegexes = new Dictionary<Regex, CustomReplayNameSyntax>
        {
            //TODO long, short form...
            { new Regex(@"^(W[Rr])"), CustomReplayNameSyntax.WinningRaces }, // Races of winners, comma separated list
            //TODO long, short form...
            { new Regex(@"^(L[Rr])]"), CustomReplayNameSyntax.LosingRaces }, // Races of losers, comma separated list
            //TODO long, short form...
            { new Regex(@"^([Rr])"), CustomReplayNameSyntax.Races }, // All races, comma separated list
            { new Regex(@"^(W[Tt])"), CustomReplayNameSyntax.WinningTeams }, // Winning team, comma separated list of players
            { new Regex(@"^(L[Tt])"), CustomReplayNameSyntax.LosingTeams }, // Losing teams, comma separated list of players, each team surrounded by parentheses
            { new Regex(@"^(T)"), CustomReplayNameSyntax.Teams }, // Teams, comma separated list of players, each team surrounded by parentheses
            { new Regex(@"^(m)"), CustomReplayNameSyntax.MapShort }, // Map, short form i.e. first letter of each word
            { new Regex(@"^(M(?![Uu]))"), CustomReplayNameSyntax.MapLong }, // Map, long form
            { new Regex(@"^(M[Uu])"), CustomReplayNameSyntax.Matchup }, // Matchup
            { new Regex(@"^(d(?!u))"), CustomReplayNameSyntax.Date }, // Date, yy-MM-dd
            { new Regex(@"^(D(?![Uu]))"), CustomReplayNameSyntax.DateTime }, // DateTime, yy-MM-dd hh:mm:ss
            { new Regex(@"^(du)"), CustomReplayNameSyntax.DurationShort }, // Duration, short format
            { new Regex(@"^(D[Uu])"), CustomReplayNameSyntax.DurationLong }, // Duration, long format
            { new Regex(@"^(F)"), CustomReplayNameSyntax.GameFormat }, // game format i.e. 1v1, 2v2, ... 
            { new Regex(@"^(gt)"), CustomReplayNameSyntax.GameTypeShort }, // game type i.e. melee, free for all, short form
            { new Regex(@"^(G[Tt])"), CustomReplayNameSyntax.GameTypeLong }, // game type i.e. melee, free for all, long form
            { new Regex($@"^(?:<{_playerInfoOption1}|{_playerInfoOption2}|{_playerInfoOption3}|{_playerInfoOption4}|{_playerInfoOption5}|{_playerInfoOption6}>)"), CustomReplayNameSyntax.PlayerInfo }, // player specific instructions
            // { new Regex(@"^(?:<(((/p|/[Rr]|/[Ww])(?:\s+)?)+)>)"), CustomReplayNameSyntax.PlayerInfo }, // player specific instructions
            { new Regex(@"^(P)"), CustomReplayNameSyntax.Players }, // comma separated list of all players
            { new Regex(@"^(p\d+)"), CustomReplayNameSyntax.PlayerX }, // extract the x'th player
            { new Regex(@"^(r\d+)"), CustomReplayNameSyntax.PlayerXRaceShort }, // extract the x'th player's race, short form
            { new Regex(@"^(R\d+)"), CustomReplayNameSyntax.PlayerXRaceLong }, // extract the x'th player's race, long form
            { new Regex(@"^(w\d+)"), CustomReplayNameSyntax.PlayerXVictoryStatusShort }, // extract the the victory status of the x'th player, short form
            { new Regex(@"^(W\d+)"), CustomReplayNameSyntax.PlayerXVictoryStatusLong } // extract the the victory status of the x'th player, long form
            //TODO add players+observers option?
            // { new Regex(@""), CustomReplayNameSyntax. }, // 
        };

        //TODO validate regexes
        private static readonly HashSet<char> _invalidFileChars = Path.GetInvalidFileNameChars().ToHashSet();

        #endregion

        private readonly string _customFormat;
        private readonly List<Tuple<CustomReplayNameSyntax, string>> _customFormatSections;

        #endregion

        #region constructors

        private CustomReplayFormat(string format, List<Tuple<CustomReplayNameSyntax, string>> customFormatSections)
        {
            _customFormat = format;
            _customFormatSections = customFormatSections;
        }

        #endregion

        #endregion

        #region public

        #region constructors

        public static CustomReplayFormat Create(string format)
        {
            if (!TryParse(format, out var customReplayFormat))
                throw new ArgumentException(nameof(format), "Invalid custom syntax!");

            return customReplayFormat;
        }

        #endregion

        #region properties

        public string CustomFormat => _customFormat;

        #endregion

        #region methods

        #region static

        public static bool TryParse(string toCheck, out CustomReplayFormat customReplayFormat)
        {
            if (string.IsNullOrWhiteSpace(toCheck))
            {
                customReplayFormat = null;
                return false;
            }

            var customFormatStringBuilder = new StringBuilder();
            List<Tuple<CustomReplayNameSyntax, string>> customReplayFormatSections = new List<Tuple<CustomReplayNameSyntax, string>>();

            var matches = _escapeCharacter.Matches(toCheck);
            var previousMatchIndexEnd = 0;
            var matchCounter = 0;
            foreach (Match match in matches)
            {
                var nextMatch = match.NextMatch();
                var stringContainingFormatSpecifier = toCheck.Substring(
                            Math.Min(match.Index + 1, toCheck.Length - 1),
                            nextMatch.Success ? (nextMatch.Index - (match.Index + 1)) : (toCheck.Length - (match.Index + 1))
                        );

                var literalFormat = toCheck.Substring(previousMatchIndexEnd, match.Index - previousMatchIndexEnd);
                var formatRegex = _formatRegexes.FirstOrDefault(r => r.Key.IsMatch(stringContainingFormatSpecifier));

                if (formatRegex.Equals(default(KeyValuePair<Regex, CustomReplayNameSyntax>)))
                {
                    customReplayFormat = null;
                    return false;
                }

                var formatSpecifier = formatRegex.Key.Match(stringContainingFormatSpecifier).Groups[0].Value;
                customFormatStringBuilder.Append($"{literalFormat}{{{matchCounter++}}}");
                customReplayFormatSections.Add(Tuple.Create(formatRegex.Value, formatSpecifier));
                previousMatchIndexEnd = match.Index + 1 + formatSpecifier.Length;
            }
            // If escapeCharacter doesn't match a single time, it just means it will be interpreted as a literal string...
            customReplayFormat = new CustomReplayFormat(customFormatStringBuilder.ToString(), customReplayFormatSections);
            return true;
        }

        #endregion

        #region instance

        // public Dictionary<CustomReplayNameSyntax, string> GenerateReplayNameSections(IReplay replay)
        // {
        //     throw new NotImplementedException();
        // }

        //TODO implement
        public string GenerateReplayName(IReplay replay)
        {
            // use replaywrapper object that has methods for each replay formatting item
            // replaywrapper needs to know the replay, the customreplaynamesyntax and in a few cases the actual regex itself for more information
            // use string.Format with the formatting string and pass all the formatting items to it
            var replayWrapper = ReplayDecorator.Create(replay);
            var customReplayNameSectionsReplacements = new string[_customFormatSections.Count];
            for (int i = 0; i < _customFormatSections.Count; i++)
            {
                customReplayNameSectionsReplacements[i] = replayWrapper.GetReplayItem(_customFormatSections[i]);
            }

            return string.Format(_customFormat, customReplayNameSectionsReplacements);
        }

        public override string ToString()
        {
            return CustomFormat;
        }

        #endregion

        #endregion

        #endregion
    }
}
