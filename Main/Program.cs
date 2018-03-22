using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ReplayParser.Loader;
using ReplayParser.Actions;
using System.Diagnostics;

namespace ReplayParser.FeatureExtraction
{
    class Program
    {
        private const char separater = ',';


        static void Main(string[] args)
        {
            

            if (args == null || args.Length == 0 || String.IsNullOrEmpty(args[0]))
            {
                Console.WriteLine("You need to specify a folder containing the replays.");
                return;
            }

            // Set path of folder containing replay files
            string filepath = args[0];
            if (!Directory.Exists(filepath))
            {
                Console.WriteLine("Could not find the folder specified.");
                return;
            }

            int take = 0;
            if (args.Length > 1)
            {
                take = int.Parse(args[1]);
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();

            // Define building features.
            var buildingFeatures = new List<Entities.ObjectType>() 
            {   Entities.ObjectType.Barracks, Entities.ObjectType.SupplyDepot,
                Entities.ObjectType.CommandCenter, Entities.ObjectType.Refinery,
                Entities.ObjectType.EngineeringBay, Entities.ObjectType.Bunker,
                Entities.ObjectType.Academy, Entities.ObjectType.Factory,
                Entities.ObjectType.MissileTurret, Entities.ObjectType.ComsatStation,
                Entities.ObjectType.MachineShop, Entities.ObjectType.Starport,
                Entities.ObjectType.Armory, Entities.ObjectType.ScienceFacility,
                Entities.ObjectType.ControlTower, Entities.ObjectType.PhysicsLab,
                Entities.ObjectType.CovertOps, Entities.ObjectType.NuclearSilo
            };

            // Get list of replay filesnames
            var files = Directory.EnumerateFiles(filepath, "*.rep");
            if (take > 0)
                files = files.Take(take);

            // Initialize the streamwriter for the output comma-separated file.
            using (var terranOut = new StreamWriter("terran.csv"))
            {

                // Write the head of the output-file with the features.
                string featureString = "";
                featureString += "\"GAME\"" + separater;
                featureString += "\"PLAYER\"" + separater;
                foreach (var feature in buildingFeatures)
                {
                    featureString += "\"" + feature.ToString().ToUpper() + "\"" + separater;
                }
                terranOut.WriteLine(featureString.TrimEnd(separater));

                int count = 1;

                // Iterate each replay file.
                string aggregateString = "";
                foreach (var file in files)
                {

                    // Parse replay
                    var replay = ReplayLoader.LoadReplay(file);

                    // Filter out anything but terran, while getting players
                    var players = replay.Players.Where(x => x.RaceType == Entities.RaceType.Terran);
                    if (players == null || players.Count() == 0 || players.Count() > 2)
                        continue;

                    // Iterate through each terran player.
                    foreach (var player in players)
                    {
                        // Get the build actions of the given player
                        var playerActions = replay.Actions.Where(x => x.Player == player
                                                && x.ActionType == Entities.ActionType.Build)
                                                .Cast<BuildAction>();

                        if (playerActions.Count() == 0)
                            continue;

                        // Begin building output string.
                        aggregateString = "\"" + Path.GetFileName(file).Replace(separater, '_') + "\"" + separater;
                        aggregateString += "\"" + player.Name.Replace(separater, '_') + "\"" + separater;

                        // Get first build time for each feature building
                        foreach (var feature in buildingFeatures)
                        {
                            var buildTime = playerActions.Where(x => x.ObjectType == feature)
                                .OrderBy(x => x.Frame)
                                .Select(x => x.Frame)
                                .FirstOrDefault() / 23;     // The time is in "Ticks" - divide by 23 to approximate seconds.
                            aggregateString += buildTime.ToString() + separater;
                        }

                        // Write out the players features and flush
                        terranOut.WriteLine(aggregateString.TrimEnd(separater));
                        terranOut.Flush();


                    }

                    Console.WriteLine("File #"+count+": (" + Path.GetFileName(file) + ") processed!");
                    count++;
                }

                terranOut.Flush();

            }   // Disposing streamwriter

            sw.Stop();

            Console.WriteLine("Done! Time: " + sw.Elapsed);
        }
    }
}
