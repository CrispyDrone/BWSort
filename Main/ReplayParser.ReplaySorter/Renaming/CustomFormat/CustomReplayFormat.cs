using ReplayParser.Interfaces;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using ReplayParser.ReplaySorter.Extensions;
using System.Text;
using ReplayParser.ReplaySorter.IO;

namespace ReplayParser.ReplaySorter.Renaming
{
    /// <summary>
    /// Custom format for replays. Use either the static factory to get an instance at risk of throwing an invalid argument exception in case the format is bad, or use the TryPasre
    /// method, which will return null for an invalid format instead. This class also keeps track of how many replays have been renamed according to this format. This is used for
    /// generating the appropriate counters when using the Counter renaming format item.
    /// </summary>
    public class CustomReplayFormat
    {
        #region private

        #region fields

        #region static

        //TODO validate regexes
        private static readonly Regex _escapeCharacter = new Regex(@"/");
        private static readonly Regex _playerInfoBlockEnd = new Regex(@">"); // .*? lazy instead of greedy
        private static readonly string _playerInfoOption1 =  @"[^/]*/p[^/]*(?=/>)";
        private static readonly string _playerInfoOption2 =  @"[^/]*/[Ww][^/]*(?=/>)";
        private static readonly string _playerInfoOption3 =  @"[^/]*/[Rr][^/]*(?=/>)";
        private static readonly string _playerInfoOption4 =  @"[^/]*/p[^/]*/[Ww][^/]*(?=/>)";
        private static readonly string _playerInfoOption5 =  @"[^/]*/p[^/]*/[Rr][^/]*(?=/>)";
        private static readonly string _playerInfoOption6 =  @"[^/]*/[Ww][^/]*/p[^/]*(?=/>)";
        private static readonly string _playerInfoOption7 =  @"[^/]*/[Ww][^/]*/[Rr][^/]*(?=/>)";
        private static readonly string _playerInfoOption8 =  @"[^/]*/[Rr][^/]*/p[^/]*(?=>)";
        private static readonly string _playerInfoOption9 =  @"[^/]*/[Rr][^/]*/[Ww][^/]*(?=/>)";
        private static readonly string _playerInfoOption10 = @"[^/]*/p[^/]*/[Ww][^/]*/[Rr][^/]*(?=/>)";
        private static readonly string _playerInfoOption11 = @"[^/]*/p[^/]*/[Rr][^/]*/[Ww][^/]*(?=/>)";
        private static readonly string _playerInfoOption12 = @"[^/]*/[Ww][^/]*/p[^/]*/[Rr][^/]*(?=/>)";
        private static readonly string _playerInfoOption13 = @"[^/]*/[Ww][^/]*/[Rr][^/]*/p[^/]*(?=/>)";
        private static readonly string _playerInfoOption14 = @"[^/]*/[Rr][^/]*/p[^/]*/[Ww][^/]*(?=/>)";
        private static readonly string _playerInfoOption15 = @"[^/]*/[Rr][^/]*/[Ww][^/]*/p[^/]*(?=/>)";

        private static readonly Dictionary<Regex, CustomReplayNameSyntax> _formatRegexes = new Dictionary<Regex, CustomReplayNameSyntax>
        {
            //TODO long, short form...
            { new Regex(@"^(W[Rr])"), CustomReplayNameSyntax.WinningRaces }, // Races of winners, comma separated list
            //TODO long, short form...
            { new Regex(@"^(L[Rr])"), CustomReplayNameSyntax.LosingRaces }, // Races of losers, comma separated list
            //TODO long, short form...
            { new Regex(@"^([Rr](?!\d+))"), CustomReplayNameSyntax.Races }, // All races, comma separated list
            { new Regex(@"^(W[Tt])"), CustomReplayNameSyntax.WinningTeams }, // Winning team, comma separated list of players
            { new Regex(@"^(L[Tt])"), CustomReplayNameSyntax.LosingTeams }, // Losing teams, comma separated list of players, each team surrounded by parentheses
            { new Regex(@"^(T)"), CustomReplayNameSyntax.Teams }, // Teams, comma separated list of players, each team surrounded by parentheses
            { new Regex(@"^(m)"), CustomReplayNameSyntax.MapShort }, // Map, short form i.e. first letter of each word
            { new Regex(@"^(M(?![Uu]))"), CustomReplayNameSyntax.MapLong }, // Map, long form
            { new Regex(@"^(M[Uu])"), CustomReplayNameSyntax.Matchup }, // Matchup
            { new Regex(@"^(d(?!u))"), CustomReplayNameSyntax.Date }, // Date, yy-MM-dd
            { new Regex(@"^(D(?![Uu]))"), CustomReplayNameSyntax.DateTime }, // DateTime, yy-MM-ddThhmmss
            { new Regex(@"^(du)"), CustomReplayNameSyntax.DurationShort }, // Duration, short format
            { new Regex(@"^(D[Uu])"), CustomReplayNameSyntax.DurationLong }, // Duration, long format
            { new Regex(@"^(F)"), CustomReplayNameSyntax.GameFormat }, // game format i.e. 1v1, 2v2, ... 
            { new Regex(@"^(gt)"), CustomReplayNameSyntax.GameTypeShort }, // game type i.e. melee, free for all, short form
            { new Regex(@"^(G[Tt])"), CustomReplayNameSyntax.GameTypeLong }, // game type i.e. melee, free for all, long form
            { new Regex($@"^(<({_playerInfoOption1}|{_playerInfoOption2}|{_playerInfoOption3}|{_playerInfoOption4}|{_playerInfoOption5}|{_playerInfoOption6}|{_playerInfoOption7}|{_playerInfoOption8}|{_playerInfoOption9}|{_playerInfoOption10}|{_playerInfoOption11}|{_playerInfoOption12}|{_playerInfoOption13}|{_playerInfoOption14}|{_playerInfoOption15})/>)"), CustomReplayNameSyntax.PlayerInfo }, // player specific instructions
            // { new Regex(@"^(?:<(((/p|/[Rr]|/[Ww])(?:\s+)?)+)>)"), CustomReplayNameSyntax.PlayerInfo }, // player specific instructions
            { new Regex(@"^(P)"), CustomReplayNameSyntax.PlayersWithObservers }, // comma separated list of all players, including observers
            { new Regex(@"^(p)"), CustomReplayNameSyntax.Players }, // comma separated list of all players, excluding observers
            { new Regex(@"^(P\d+)"), CustomReplayNameSyntax.PlayerX }, // extract the x'th player
            { new Regex(@"^(r\d+)"), CustomReplayNameSyntax.PlayerXRaceShort }, // extract the x'th player's race, short form
            { new Regex(@"^(R\d+)"), CustomReplayNameSyntax.PlayerXRaceLong }, // extract the x'th player's race, long form
            { new Regex(@"^(w\d+)"), CustomReplayNameSyntax.PlayerXVictoryStatusShort }, // extract the the victory status of the x'th player, short form
            { new Regex(@"^(W\d+)"), CustomReplayNameSyntax.PlayerXVictoryStatusLong }, // extract the the victory status of the x'th player, long form
            { new Regex(@"^(O)"), CustomReplayNameSyntax.OriginalName }, // extract the original name of the replay
            { new Regex(@"^(c)"), CustomReplayNameSyntax.CounterShort }, // counter that will increment for each replay being renamed
            { new Regex(@"^(C)"), CustomReplayNameSyntax.CounterLong } // fixed counter that will increment for each replay being renamed
            //TODO add players+observers option?
            // { new Regex(@""), CustomReplayNameSyntax. }, // 
        };

        //TODO validate regexes
        private static readonly HashSet<char> _invalidFileChars = Path.GetInvalidFileNameChars().ToHashSet();

        #endregion

        #region instance

        private readonly List<Tuple<CustomReplayNameSyntax, string>> _customFormatSections;
        private int _counter;

        #endregion

        #endregion

        #region constructors

        private CustomReplayFormat(string format, List<Tuple<CustomReplayNameSyntax, string>> customFormatSections, int maxNumberOfReplays, bool throwOnExceedingMax)
        {
            CustomFormat = format;
            _customFormatSections = customFormatSections;
            MaxNumberOfReplays = maxNumberOfReplays;
            ThrowOnExceedingMax = throwOnExceedingMax;
        }

        #endregion

        #endregion

        #region public

        #region constructors

        public static CustomReplayFormat Create(string format, int maxNumberOfReplays, bool throwOnExceedingMax)
        {
            if (maxNumberOfReplays <= 0)
                throw new ArgumentException(nameof(maxNumberOfReplays), "Max number of replays cannot be less than or equal to zero.");

            if (!TryParse(format, maxNumberOfReplays, throwOnExceedingMax, out var customReplayFormat))
                throw new ArgumentException(nameof(format), "Invalid custom syntax!");

            return customReplayFormat;
        }

        #endregion

        #region properties

        public string CustomFormat { get; }
        public IEnumerable<Tuple<CustomReplayNameSyntax, string>> CustomFormatSections => _customFormatSections.AsEnumerable();
        public int Counter => _counter;
        public int MaxNumberOfReplays { get; }
        public bool ThrowOnExceedingMax { get; }

        #endregion

        #region methods

        #region static

        public static bool TryParse(string toCheck, int maxNumberOfReplays, bool throwOnExceedingMax, out CustomReplayFormat customReplayFormat)
        {
            if (string.IsNullOrWhiteSpace(toCheck))
            {
                customReplayFormat = null;
                return false;
            }

            if (maxNumberOfReplays <= 0)
            {
                customReplayFormat = null;
                return false;
            }

            var match = _escapeCharacter.Match(toCheck);

            var customFormatStringBuilder = new StringBuilder();
            List<Tuple<CustomReplayNameSyntax, string>> customReplayFormatSections = new List<Tuple<CustomReplayNameSyntax, string>>();

            var matchCounter = 0;
            var previousMatchIndexEnd = 0;

            while (match.Success)
            {
                var matchIndex = match.Index;
                var literalFormat = toCheck.Substring(previousMatchIndexEnd, matchIndex - previousMatchIndexEnd);

                if (matchIndex + 1 >= toCheck.Length)
                {
                    customReplayFormat = null;
                    return false;
                }

                var stringContainingFormatSpecifier = toCheck.Substring(matchIndex + 1);
                var formatRegex = _formatRegexes.FirstOrDefault(r => r.Key.IsMatch(stringContainingFormatSpecifier));

                if (formatRegex.Equals(default(KeyValuePair<Regex, CustomReplayNameSyntax>)))
                {
                    customReplayFormat = null;
                    return false;
                }
                var formatSpecifier = formatRegex.Key.Match(stringContainingFormatSpecifier).Groups[1].Value;
                customFormatStringBuilder.Append($"{{{matchCounter++}}}{{{matchCounter++}}}");
                customReplayFormatSections.Add(Tuple.Create(CustomReplayNameSyntax.None, literalFormat));
                customReplayFormatSections.Add(Tuple.Create(formatRegex.Value, formatSpecifier));
                previousMatchIndexEnd = matchIndex + 1 + formatSpecifier.Length;
                match = match.NextMatch();
                while (match.Success && match.Index < previousMatchIndexEnd)
                {
                    match = match.NextMatch();
                }
            }
            customFormatStringBuilder.Append($"{{{matchCounter++}}}");
            customReplayFormatSections.Add(Tuple.Create(CustomReplayNameSyntax.None, toCheck.Substring(previousMatchIndexEnd)));
            customReplayFormat = new CustomReplayFormat(customFormatStringBuilder.ToString(), customReplayFormatSections, maxNumberOfReplays, throwOnExceedingMax);
            return true;
        }

        #endregion

        #region instance

        public string GenerateReplayName(File<IReplay> replay)
        {
            // use replaywrapper object that has methods for each replay formatting item
            // replaywrapper needs to know the replay, the customreplaynamesyntax and in a few cases the actual regex itself for more information
            // use string.Format with the formatting string and pass all the formatting items to it
            ++_counter;
            if (ThrowOnExceedingMax && Counter > MaxNumberOfReplays)
                throw new InvalidOperationException("Exceeded max number of replays!");

            var replayWrapper = ReplayDecorator.Create(replay);
            var customReplayNameSectionsReplacements = new string[_customFormatSections.Count];

            for (int i = 0; i < _customFormatSections.Count; i++)
            {
                customReplayNameSectionsReplacements[i] = replayWrapper.GetReplayItem(_customFormatSections[i], _counter, MaxNumberOfReplays);
            }

            return string.Format(CustomFormat, customReplayNameSectionsReplacements);
        }

        public void ResetCounter()
        {
            _counter = 0;
        }

        public override string ToString()
        {
            return CustomFormat;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj.GetType() != GetType())
                return false;

            var otherFormat = obj as CustomReplayFormat;
            return Equals(otherFormat);
        }

        public bool Equals(CustomReplayFormat other)
        {
            if (other == null)
                return false;

            return
                CustomFormat == other.CustomFormat &&
                CustomFormatSections.SequenceEqual(other.CustomFormatSections) &&
                MaxNumberOfReplays == other.MaxNumberOfReplays &&
                ThrowOnExceedingMax == other.ThrowOnExceedingMax;
        }

        public static bool operator == (CustomReplayFormat one, CustomReplayFormat other)
        {
            if (ReferenceEquals(one, null))
                return ReferenceEquals(other, null);

            return one.Equals(other);
        }

        public static bool operator !=(CustomReplayFormat one, CustomReplayFormat other)
        {
            return !(one == other);
        }

        #endregion

        #endregion

        #endregion
    }
}
