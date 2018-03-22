using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ReplayParser.Interfaces;

namespace ReplayParser.ReplaySorter.Sorting.SortCommands
{
    public class SortOnMap : ISortCommand
    {
        public SortOnMap(SortCriteriaParameters sortcriteriaparameters, bool keeporiginalreplaynames, Sorter sorter)
        {
            SortCriteriaParameters = sortcriteriaparameters;
            KeepOriginalReplayNames = keeporiginalreplaynames;
            Sorter = sorter;
        }
        public bool KeepOriginalReplayNames { get; set; }

        public SortCriteriaParameters SortCriteriaParameters { get; set; }
        public Criteria SortCriteria { get { return Criteria.MAP; } }
        public bool IsNested { get; set; }
        public Sorter Sorter { get; set; }

        public IDictionary<string, IDictionary<string, IReplay>> Sort()
        {
            // Dictionary<directory, dictionary<file, replay>>
            IDictionary<string, IDictionary<string, IReplay>> DirectoryFileReplay = new Dictionary<string, IDictionary<string, IReplay>>();

            // extract maps from replays, try to group the duplicates
            ReplayMapEqualityComparer MapEq = new ReplayMapEqualityComparer();
            IDictionary<IReplayMap, List<IReplay>> Maps = new Dictionary<IReplayMap, List<IReplay>>(MapEq);


            foreach (var replay in Sorter.ListReplays)
            {
                if (!Maps.Keys.Contains(replay.ReplayMap))
                {
                    Maps.Add(new KeyValuePair<IReplayMap, List<IReplay>>(replay.ReplayMap, new List<IReplay> { replay }));
                }
                else
                {
                    Maps[replay.ReplayMap].Add(replay);
                }
            }

            string sortDirectory = Sorter.CurrentDirectory + @"\" + Sorter.SortCriteria.ToString();
            Sorter.CreateDirectory(sortDirectory);

            foreach (var map in Maps)
            {
                var MapName = map.Key.MapName;
                IDictionary<string, IReplay> FileReplays = new Dictionary<string, IReplay>();

                foreach (char invalidChar in Sorter.InvalidFileChars)
                {
                    MapName = MapName.Replace(invalidChar.ToString(), string.Empty);
                }
                foreach (char invalidChar in Sorter.InvalidFileCharsAdditional)
                {
                    MapName = MapName.Replace(invalidChar.ToString(), string.Empty);
                }

                try
                {
                    if (!Directory.Exists(sortDirectory + @"\" + MapName))
                    {
                        Directory.CreateDirectory(sortDirectory + @"\" + MapName);
                    }
                    else
                    {
                        int counter = 1;
                        string TempName = MapName;
                        while (Directory.Exists(sortDirectory + @"\" + TempName))
                        {
                            TempName = IncrementName(MapName, ref counter);
                        }
                        MapName = TempName;
                        Directory.CreateDirectory(sortDirectory + @"\" + MapName);
                    }
                    var MapReplays = Maps[map.Key];
                    foreach (var replay in MapReplays)
                    {
                        try
                        {
                            string File = string.Empty;
                            if (IsNested == false)
                            {
                                File = ReplayHandler.CopyReplay(Sorter.ListReplays, replay, Sorter.Files, sortDirectory, MapName, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                            }
                            else
                            {
                                File = ReplayHandler.MoveReplay(Sorter.ListReplays, replay, Sorter.Files, sortDirectory, MapName, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                            }

                            FileReplays.Add(new KeyValuePair<string, IReplay>(/*Sorter.Files.ElementAt(Sorter.ListReplays.IndexOf(replay))*/File, replay));
                        }
                        catch (IOException IOex)
                        {
                            Console.WriteLine(IOex.Message);
                        }
                        catch (NotSupportedException NSE)
                        {
                            Console.WriteLine(NSE.Message);
                        }
                        catch (NullReferenceException nullex)
                        {
                            Console.WriteLine(nullex.Message);
                        }
                        catch (ArgumentException AEX)
                        {
                            Console.WriteLine(AEX.Message);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                    // key already exists... how/why?? "Untitled Scenario"... different maps, same "internal" name
                    var MapFolder = sortDirectory + @"\" + MapName;
                    //var TempName = sortDirectory + @"\" + MapName;
                    //int count = 1;
                    //while (DirectoryFileReplay.ContainsKey(TempName))
                    //    TempName = IncrementName(MapFolder, ref count);
                    //MapFolder = TempName;

                    DirectoryFileReplay.Add(new KeyValuePair<string, IDictionary<string, IReplay>>(MapFolder, FileReplays));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(DirectoryFileReplay.Count.ToString());
                }
            }
            // not implemented yet
            return DirectoryFileReplay;
        }

        private string IncrementName(string Name, ref int counter)
        {
            return string.Format("{0}({1})", Name, counter++);
        }
    }
}
