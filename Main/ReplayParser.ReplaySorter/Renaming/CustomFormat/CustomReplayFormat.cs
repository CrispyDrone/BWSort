using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.ReplayRenamer;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using ReplayParser.ReplaySorter.Extensions;
using ReplayParser.ReplaySorter.CustomFormat;

namespace ReplayParser.ReplaySorter
{
    public class CustomReplayFormat
    {
        #region private

        #region fields

        private string _customFormat;
        //TODO validate regexes
        private static Dictionary<Regex, CustomReplayNameSyntax> _formatRegexes = new Dictionary<Regex, CustomReplayNameSyntax>
        {
            { new Regex(@"^W[Rr]"), CustomReplayNameSyntax.WinningRaces }, // Races of winners, comma separated list
            { new Regex(@"^L[Rr]"), CustomReplayNameSyntax.LosingRaces }, // Races of losers, comma separated list
            { new Regex(@"^[Rr]"), CustomReplayNameSyntax.Races }, // All races, comma separated list
            { new Regex(@"^W[Tt]"), CustomReplayNameSyntax.WinningTeams }, // Winning team, comma separated list of players
            { new Regex(@"^L[Tt]"), CustomReplayNameSyntax.LosingTeams }, // Losing teams, comma separated list of players, each team surrounded by parentheses
            { new Regex(@"^T"), CustomReplayNameSyntax.Teams }, // Teams, comma separated list of players, each team surrounded by parentheses
            { new Regex(@"^m"), CustomReplayNameSyntax.MapShort }, // Map, short form i.e. first letter of each word
            { new Regex(@"^M(?![Uu])"), CustomReplayNameSyntax.MapLong }, // Map, long form
            { new Regex(@"^M[Uu]"), CustomReplayNameSyntax.Matchup }, // Matchup
            { new Regex(@"^d(?!u)"), CustomReplayNameSyntax.Date }, // Date
            { new Regex(@"^D(?![Uu])"), CustomReplayNameSyntax.DateTime }, // DateTime
            { new Regex(@"^du"), CustomReplayNameSyntax.DurationShort }, // Duration, short format
            { new Regex(@"^D[Uu]"), CustomReplayNameSyntax.DurationLong }, // Duration, long format
            { new Regex(@"^F"), CustomReplayNameSyntax.GameFormat }, // game format i.e. 1v1, 2v2, ... 
            { new Regex(@"^gt"), CustomReplayNameSyntax.GameTypeShort }, // game type i.e. melee, free for all, short form
            { new Regex(@"^GT"), CustomReplayNameSyntax.GameTypeLong }, // game type i.e. melee, free for all, long form
            { new Regex(@"^<(((/p|/[Rr]|/[Ww]).*)+)>"), CustomReplayNameSyntax.PlayerInfo }, // player specific instructions
            { new Regex(@"^[P]"), CustomReplayNameSyntax.Players }, // comma separated list of all players
            { new Regex(@"^[p]\d+"), CustomReplayNameSyntax.PlayerX }, // extract the x'th player
            { new Regex(@"^[r]\d+"), CustomReplayNameSyntax.PlayerXRaceShort }, // extract the x'th player's race, short form
            { new Regex(@"^[R]\d+"), CustomReplayNameSyntax.PlayerXRaceLong }, // extract the x'th player's race, long form
            { new Regex(@"^[Ww]\d+"), CustomReplayNameSyntax.PlayerXVictoryStatus } // extract the the victory status of the x'th player
            // { new Regex(@""), CustomReplayNameSyntax. }, // 
        };

        //TODO validate regexes
        // private static Regex _playerBlockInfo = new Regex(@"^(((/p|/[Rr]|/[Ww]).*)+)$");
        private static Regex _escapeCharacter = new Regex(@"\");
        private static Dictionary<CustomReplayNameSyntax, char> _separators = new Dictionary<CustomReplayNameSyntax, char>();
        private static HashSet<char> _invalidFileChars = Path.GetInvalidFileNameChars().ToHashSet();

        #endregion

        #region constructors

        private CustomReplayFormat(string format)
        {
            _customFormat = format;
        }

        #endregion

        #endregion

        #region public

        #region constructors

        public static CustomReplayFormat Create(string format)
        {
            if (string.IsNullOrWhiteSpace(format) || !TryParse(format, out string customReplayFormat))
                throw new ArgumentException(nameof(format), "Invalid custom syntax!");

            return new CustomReplayFormat(format);
        }

        #endregion

        #region properties

        public string CustomFormat => _customFormat;

        #endregion

        #region methods

        public static bool TryParse(string tocheck, out string format)
        {
            if (string.IsNullOrWhiteSpace(tocheck))
            {
                format = string.Empty;
                return false;
            }

            bool valid = true;
            var matches = _escapeCharacter.Matches(tocheck);
            foreach (Match match in matches)
            {
                var nextMatch = match.NextMatch();
                var potentialFormatSpecifier = tocheck.Substring(
                        Math.Max(match.Index + 1, tocheck.Length - 1), 
                        nextMatch.Success ? (nextMatch.Index - (match.Index + 1)) : (tocheck.Length - (match.Index + 1))
                );

                if (_formatRegexes.FirstOrDefault(r => r.IsMatch(potentialFormatSpecifier)) == null)
                {
                    valid = false;
                    break;
                }
            }
            // If escapeCharacter doesn't match a single time, it just means it will be interpreted as a literal string...
            format = tocheck;
            return valid;
        }

        public Dictionary<CustomReplayNameSyntax, string> GenerateReplayNameSections(Interfaces.IReplay replay)
        {
            // implement factory design pattern, create respective object for each custom replay format argument
            //Dictionary<CustomReplayNameSyntax, string>[] CustomReplayNameSections = new Dictionary<CustomReplayNameSyntax, string>[]();

            //foreach (var CustomReplayNameSection in Enum.GetValues(typeof(CustomReplayNameSyntax)))
            //{

            //}
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
            //int NumberOfTeams = ((Team)ReplayNameSections[0]).Teams.Length;

            //Dictionary<int, Dictionary<CustomReplayNameSyntax, string[]>> TeamReplayNameSections = new Dictionary<int, Dictionary<CustomReplayNameSyntax, string[]>>();
            //for (int i = 0; i < NumberOfTeams; i++)
            //{
            //    TeamReplayNameSections.Add(i, new Dictionary<CustomReplayNameSyntax, string[]>());
            //}
            //CustomReplayNameSections.Add(new KeyValuePair<CustomReplayNameSyntax, string>(CustomReplayNameSyntax.T, team.))
        }

        public string GenerateReplayName(IReplay replay)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return CustomFormat;
        }

        #endregion

        #endregion
    }
}
