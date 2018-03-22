using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ReplayParser.ReplaySorter.Sorting;

namespace ReplayParser.ReplaySorter.UserInput
{
    public static class User
    {
        public static SearchDirectory AskDirectory()
        {
            Console.WriteLine("Replay directory?");
            string directory = Console.ReadLine();

            while (!Directory.Exists(directory) /*&& directory != string.Empty*/)
            {
                Console.WriteLine("Could not find directory {0}.", directory);
                Console.WriteLine("Please specify an existing directory containing your replays.");
                directory = Console.ReadLine();
            }

            Console.WriteLine("Include subdirectories? Y(es)/N(o).");
            string consoleInputIncludeSubdirectories = Console.ReadLine().Trim().ToUpper();
            bool CorrectInput = false;
            SearchOption searchoption = new SearchOption();

            while (!CorrectInput)
            {
                switch (consoleInputIncludeSubdirectories)
                {
                    case "Y":
                    case "YES":
                        searchoption = SearchOption.AllDirectories;
                        CorrectInput = true;
                        break;
                    case "N":
                    case "NO":
                        searchoption = SearchOption.TopDirectoryOnly;
                        CorrectInput = true;
                        break;
                    default:
                        Console.WriteLine("Invalid input. Write Y(es) or N(o).");
                        consoleInputIncludeSubdirectories = Console.ReadLine().Trim().ToUpper();
                        break;
                }
            }

            return new SearchDirectory(directory, searchoption);
        }

        public static Answer AskYesNo()
        {
            string consoleInput = Console.ReadLine().Trim().ToUpper();
            bool CorrectInput = false;
            bool YesNo = false;

            while (!CorrectInput)
            {
                switch (consoleInput)
                {
                    case "Y":
                    case "YES":
                        CorrectInput = true;
                        YesNo = true;
                        break;
                    case "N":
                    case "NO":
                        CorrectInput = true;
                        YesNo = false;
                        break;
                    default:
                        Console.WriteLine("Invalid input. Write Y(es) or N(o).");
                        consoleInput = Console.ReadLine().Trim().ToUpper();
                        break;
                }
            }

            return new Answer(YesNo);
        }

        public static Answer AskCriteria()
        {
            bool ParseSuccess = false;
            bool StopProgram = false;
            string[] Criteria = null;
            StringBuilder CriteriaStringSeparatedByCommas = new StringBuilder();
            while (!ParseSuccess)
            {
                CriteriaStringSeparatedByCommas.Clear();
                ParseSuccess = true;
                string consoleInput = Console.ReadLine();
                if (consoleInput.ToUpper() == "STOP")
                {
                    StopProgram = true;
                    break;
                }
                Criteria = Array.ConvertAll(consoleInput.ToUpper().Split(new char[] { ' ' }), x => x.Trim());

                for (int i = 0; i < Criteria.Length; i++)
                {
                    try
                    {
                        Enum.Parse(typeof(Criteria), Criteria[i]);
                        if (i == Criteria.Length - 1)
                            CriteriaStringSeparatedByCommas.Append(Criteria[i]);
                        else
                            CriteriaStringSeparatedByCommas.Append(Criteria[i] + ",");
                    }
                    catch
                    {
                        Console.WriteLine("Invalid criteria: {0}", Criteria[i]);
                        ParseSuccess = false;
                    }
                }

                //foreach (var criteria in Criteria)
                //{
                //    // ... does this work properly for multiple criteria?? no!
                //    //if (!Enum.TryParse(criteria, out ChosenCriteria))
                //    //{
                //    //    Console.WriteLine("Invalid criteria: {0}", criteria);
                //    //    ParseSuccess = false;
                //    //}
                //    try
                //    {
                //        Enum.Parse(typeof(Criteria), criteria);
                //        if (Criteria[Criteria.Length -1 ] == criteria)
                //            CriteriaStringSeparatedByCommas.Append(criteria);
                //        else
                //            CriteriaStringSeparatedByCommas.Append(criteria + ",");
                //    }
                //    catch
                //    {
                //        Console.WriteLine("Invalid criteria: {0}", criteria);
                //        ParseSuccess = false;
                //    }
                //}
            }
            // if our comma separated list contains the same value multiple times, this shouldn't be a problem since Enum.TryParse does a bitwise OR 
            Criteria ChosenCriteria;
            Enum.TryParse(CriteriaStringSeparatedByCommas.ToString(), out ChosenCriteria);
            return new Answer(ChosenCriteria, Criteria, StopProgram);
        }

        public static Answer AskAdditionalQuestionsSortCriteria(Criteria chosencriteria)
        {
            bool? MakeFolderWinner = true;
            bool? MakeFolderLoser = true;
            if (chosencriteria.HasFlag(Criteria.PLAYERNAME))
            {
                Console.WriteLine("Make folder for winner (1), loser (2), both (3) or none (4)?");
                string consoleInputMakeFolders = Console.ReadLine();
                int MakeFolderChoice;

                while (!int.TryParse(consoleInputMakeFolders.Trim(), out MakeFolderChoice) || (MakeFolderChoice < 1 || MakeFolderChoice > 4))
                {
                    Console.WriteLine("Invalid choice. Type 1 for winner only, 2 for loser only, 3 for both or 4 for none.");
                    consoleInputMakeFolders = Console.ReadLine();
                }
                switch (MakeFolderChoice)
                {
                    case 1:
                        MakeFolderWinner = true;
                        MakeFolderLoser = false;
                        break;
                    case 2:
                        MakeFolderLoser = true;
                        MakeFolderWinner = false;
                        break;
                    case 3:
                        MakeFolderWinner = true;
                        MakeFolderLoser = true;
                        break;
                    case 4:
                        MakeFolderWinner = false;
                        MakeFolderLoser = false;
                        break;
                    default:
                        break;
                }
            }

            IDictionary<Entities.GameType, bool> ValidGameTypes = new Dictionary<Entities.GameType, bool>();
            if (chosencriteria.HasFlag(Criteria.MATCHUP))
            {
                Console.WriteLine("Which game types need to be included? Type all for all game types, or give a space separated list of game types you want to include.");
                Console.WriteLine("Possible game types are: ");
                foreach (var gametype in Enum.GetNames(typeof(Entities.GameType)))
                {
                    Console.WriteLine(gametype);
                }
                string consoleInputValidGameTypes = Console.ReadLine().Trim().ToUpper();

                Entities.GameType ignore;
                var consoleInputValidGameTypesBools = consoleInputValidGameTypes.Split(' ').All(x => Enum.TryParse(x, true, out ignore));
                while (!consoleInputValidGameTypesBools)
                {
                    if (consoleInputValidGameTypes == "ALL")
                        break;
                    Console.WriteLine("Type all for all game types, or give a space separated list of game types you want to include.");
                    consoleInputValidGameTypes = Console.ReadLine().Trim().ToUpper();
                    consoleInputValidGameTypesBools = consoleInputValidGameTypes.Split(' ').All(x => Enum.TryParse(x, true, out ignore));
                }

                switch (consoleInputValidGameTypes)
                {
                    case "ALL":
                        foreach (var gametype in Enum.GetValues(typeof(Entities.GameType)))
                        {
                            ValidGameTypes[(Entities.GameType)gametype] = true;
                        }
                        break;
                    default:
                        foreach (var gametype in consoleInputValidGameTypes.Split(' '))
                        {
                            ValidGameTypes[(Entities.GameType)Enum.Parse(typeof(Entities.GameType), gametype, true)] = true;
                            foreach (var PossibleGameType in Enum.GetValues(typeof(Entities.GameType)))
                            {
                                if (!ValidGameTypes.ContainsKey((Entities.GameType)PossibleGameType))
                                {
                                    ValidGameTypes[(Entities.GameType)PossibleGameType] = false;
                                }
                            }
                        }
                        break;
                }
            }

            int[] Durations = null;
            if (chosencriteria.HasFlag(Criteria.DURATION))
            {
                Console.WriteLine("Give a space separated list of (non-zero) integer values you want to use as upper boundaries for duration intervals (in minutes). For example \"10 20 30\" this will result in the following intervals: 0-10, 10-20, 20-30, 30++");
                string consoleInputDurations = Console.ReadLine().Trim();
                string[] durationsStrings = consoleInputDurations.Split(' ');
                int NumberOfDurations = durationsStrings.Length;
                Durations = new int[NumberOfDurations];

                int index = 0;
                bool ParseSuccess = durationsStrings.All(x => int.TryParse(x, out Durations[index++]));
                while (!ParseSuccess || Durations.Contains(0))
                {
                    Console.WriteLine("Give a space separated list of (non-zero) integer values you want to use as upper boundaries for duration intervals (in minutes). For example \"10 20 30\" this will result in the following intervals: 0-10, 10-20, 20-30, 30++");
                    index = 0;
                    durationsStrings = Console.ReadLine().Trim().Split(' ');
                    NumberOfDurations = durationsStrings.Length;
                    Durations = new int[NumberOfDurations];
                    ParseSuccess = durationsStrings.All(x => int.TryParse(x, out Durations[index++]));
                }
                // sort the array small to large
                // can not contain 0
                // has to be integers
                // split on spaces
                //while (!ParseSuccess)
                //{
                //    ParseSuccess = true;

                //    for (int i = 0; i < NumberOfDurations; i++)
                //    {
                //        if (!int.TryParse(durationsStrings[i], out durations[i]))
                //        {
                //            ParseSuccess = false;
                //            break;
                //        }
                //        if (durations[i] == 0)
                //        {
                //            ParseSuccess = false;
                //            break;
                //        }
                //    }
                //    if (ParseSuccess == true)
                //    {
                //        break;
                //    }

                //    Console.WriteLine("Give a space separated list of integer values you want to use as upper boundaries for duration intervals (in minutes). For example \"10 20 30\" this will result in the following intervals: 0-10, 10-20, 20-30, 30++");
                //    consoleInputDurations = Console.ReadLine();
                //    durationsStrings = consoleInputDurations.Split(' ');
                //    NumberOfDurations = durationsStrings.Length;
                //    durations = new int[NumberOfDurations];
                //}
                Durations = Durations.OrderBy(x => x).ToArray();
            }

            return new Answer(new SortCriteriaParameters(MakeFolderWinner, MakeFolderLoser, ValidGameTypes, Durations));
        }
    }
}
