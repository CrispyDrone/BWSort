using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.ReplayRenamer;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using ReplayParser.ReplaySorter.Extensions;
using ReplayParser.ReplaySorter.CustomFormat;
using System.Text;

namespace ReplayParser.ReplaySorter
{
    public class CustomReplayFormat
    {
        #region private

        #region fields

        #region static

        //TODO validate regexes
        private static Regex _escapeCharacter = new Regex(@"\");
        private static Dictionary<Regex, CustomReplayNameSyntax> _formatRegexes = new Dictionary<Regex, CustomReplayNameSyntax>
        {
            { new Regex(@"^(W[Rr])"), CustomReplayNameSyntax.WinningRaces }, // Races of winners, comma separated list
            { new Regex(@"^(L[Rr)]"), CustomReplayNameSyntax.LosingRaces }, // Races of losers, comma separated list
            { new Regex(@"^([Rr])"), CustomReplayNameSyntax.Races }, // All races, comma separated list
            { new Regex(@"^(W[Tt]"), CustomReplayNameSyntax.WinningTeams }, // Winning team, comma separated list of players
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
            { new Regex(@"^(<(((/p|/[Rr]|/[Ww]).*)+)>)"), CustomReplayNameSyntax.PlayerInfo }, // player specific instructions
            { new Regex(@"^([P])"), CustomReplayNameSyntax.Players }, // comma separated list of all players
            { new Regex(@"^([p]\d+)"), CustomReplayNameSyntax.PlayerX }, // extract the x'th player
            { new Regex(@"^([r]\d+)"), CustomReplayNameSyntax.PlayerXRaceShort }, // extract the x'th player's race, short form
            { new Regex(@"^([R]\d+)"), CustomReplayNameSyntax.PlayerXRaceLong }, // extract the x'th player's race, long form
            { new Regex(@"^([Ww]\d+)"), CustomReplayNameSyntax.PlayerXVictoryStatus } // extract the the victory status of the x'th player
            // { new Regex(@""), CustomReplayNameSyntax. }, // 
        };

        //TODO validate regexes
        // private static Regex _playerBlockInfo = new Regex(@"^(((/p|/[Rr]|/[Ww]).*)+)$");
        private static HashSet<char> _invalidFileChars = Path.GetInvalidFileNameChars().ToHashSet();

        #endregion

        private string _customFormat;
        private Dictionary<CustomReplayNameSyntax, char> _separators = new Dictionary<CustomReplayNameSyntax, char>();
        private List<Tuple<CustomReplayNameSyntax, string>> _customFormatSections;

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
                            Math.Max(match.Index + 1, toCheck.Length - 1),
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
                // customReplayFormatSections.Add(Tuple.Create(formatSpecifier.Value, format))

                // var nextMatch = match.NextMatch();
                // var potentialFormatSpecifier = tocheck.Substring(
                //         Math.Max(match.Index + 1, tocheck.Length - 1), 
                //         nextMatch.Success ? (nextMatch.Index - (match.Index + 1)) : (tocheck.Length - (match.Index + 1))
                // );

                // var literalFormat = tocheck.Substring(previousMatchIndex, )

                // var formatRegex = _formatRegexes.FirstOrDefault(r => r.Key.IsMatch(potentialFormatSpecifier));
                // if (formatRegex.Equals(default(KeyValuePair<Regex, CustomReplayNameSyntax>)))
                // {
                //     customReplayFormat = null;
                //     return false;
                // }
                // else
                // {
                //     var formatSpecifier = formatRegex.Key.Match(potentialFormatSpecifier).;
                //     customFormatStringBuilder.Append(potentialFormatSpecifier.Substring(formatSpecifier.Length));
                //     customReplayFormatSections.Add(Tuple.Create(formatRegex.Value, formatSpecifier));
                // }
            }
            // If escapeCharacter doesn't match a single time, it just means it will be interpreted as a literal string...
            customReplayFormat = new CustomReplayFormat(customFormatStringBuilder.ToString(), customReplayFormatSections);
            return true;
        }

        #endregion

        #region instance

        //TODO: fix
        public Dictionary<CustomReplayNameSyntax, string> GenerateReplayNameSections(IReplay replay)
        {
            IReplayNameSection[] IReplayNameSections = new IReplayNameSection[(Enum.GetValues(typeof(CustomReplayNameSyntax))).Length];
            IReplayNameSections[0] = new Teams(replay);
            IReplayNameSections[1] = new WinningTeam(replay);
            IReplayNameSections[2] = new LosingTeam(replay);
            IReplayNameSections[3] = new Map(replay);
            IReplayNameSections[4] = new Date(replay);
            IReplayNameSections[5] = new WinningRace(replay);
            IReplayNameSections[6] = new LosingRace(replay);
            IReplayNameSections[7] = new MatchUp(replay, (Teams)IReplayNameSections[0]);
            IReplayNameSections[8] = new Duration(replay);

            Dictionary<CustomReplayNameSyntax, string> ReplayNameSections = new Dictionary<CustomReplayNameSyntax, string>();

            foreach (var argument in IReplayNameSections)
            {
                if (_separators.ContainsKey(argument.Type))
                {
                    ReplayNameSections.Add(argument.Type, argument.GetSection(_separators[argument.Type].ToString()));
                }
                else
                {
                    ReplayNameSections.Add(argument.Type, argument.GetSection());
                }
            }

            return ReplayNameSections;
        }

        public string GenerateReplayName(IReplay replay)
        {
            // use replaywrapper object that has methods for each replay formatting item
            // replaywrapper needs to know the replay, the customreplaynamesyntax and in a few cases the actual regex itself for more information
            // use string.Format with the formatting string and pass all the formatting items to it
            throw new NotImplementedException();
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
