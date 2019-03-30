using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using ReplayParser.ReplaySorter.ReplayRenamer;

namespace ReplayParser.ReplaySorter
{
    public class CustomReplayFormat
    {
        public CustomReplayFormat()
        {

        }

        public CustomReplayFormat(string format)
        {
            this.CustomFormat = format;
        }

        // are enums always defined as classes, or can i have one associated with a particular class? (since it only really makes sense in this particular class)
        // think you can only define them like a class


        private string customFormat;
        public string CustomFormat
        {
            get
            {
                return customFormat;
            }
            set
            {
                string customformat = string.Empty;
                if (TryParse(value, out customformat))
                {
                    customFormat = customformat;
                }
                else
                {
                    throw new ArgumentException();
                }
            }
        }
        // with separators
        private static Regex WLTeam = new Regex(@"^.?([wW]|[lL])?([tT](\[.?\])?).?$");
        // separators for the other ones, doesn't work yet!
        //private static Regex MatchUpMapDurationRace = new Regex(@"^.?([mM]|[dD]|([mM][uU])|([wW][rR])|([lL][rR])).?$");
        //private static Regex MatchUpMapDateDurationRace = new Regex(@"^.?(([MD]|[md])|([mM][uU])|([wW][Rr])|([lL][rR])|([dD][uU])).?$");
        private static Regex MatchUpMapDateDurationRace = new Regex(@"^(([MD]|[md])|([mM][uU])|([wW][Rr])|([lL][rR])|([dD][uU]))$");
        private static Regex Separator = new Regex(@"\[.\]$");
        private Dictionary<CustomReplayNameSyntax, char> Separators = new Dictionary<CustomReplayNameSyntax, char>();
        private static char[] InvalidFileChars = Path.GetInvalidFileNameChars();
        //public static Regex WLTeam = new Regex(@"^([wW]|[lL])?([tT])$");
        //public static Regex MatchUpMapDurationRace = new Regex(@"^.?([MD]|MU|WR|LR).?$");
        public bool TryParse(string tocheck, out string format)
        {
            if (tocheck == null)
            {
                format = string.Empty;
                return false;
            }
            if (tocheck == string.Empty)
            {
                format = string.Empty;
                return false;
            }

            string[] arguments = tocheck.Split(new char[] { '|' });

            int i;
            for (i = 0; i < arguments.Length && (WLTeam.IsMatch(arguments[i]) || MatchUpMapDateDurationRace.IsMatch(arguments[i])); i++)
            {
                if (Separator.IsMatch(arguments[i]))
                {
                    char InternalSeparator = arguments[i].ElementAt(arguments[i].IndexOf('[') + 1);
                    foreach (char invalidChar in InvalidFileChars)
                    {
                        if (InternalSeparator == invalidChar)
                        {
                            throw new ArgumentException("Separator is an invalid character for file names.");
                        }
                    } 
                    arguments[i] = arguments[i].Remove(arguments[i].IndexOf('['));
                    CustomReplayNameSyntax EnumArgument;
                    if (Enum.TryParse(arguments[i].ToUpper(), out EnumArgument))
                    {
                        Separators.Add(EnumArgument, InternalSeparator);
                    }
                }
                arguments[i] = arguments[i].ToUpper();
            }
            if (i < arguments.Length)
            {
                format = string.Empty;
                return false;
            }
            
            format = string.Join("|", arguments);
            return true;
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
                if (Separators.ContainsKey(argument.Type))
                {
                    ReplayNameSections.Add(argument.Type, argument.GetSection(Separators[argument.Type].ToString()));
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

        public override string ToString()
        {
            return CustomFormat;
        }
    }
}
