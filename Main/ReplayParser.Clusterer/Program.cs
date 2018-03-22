using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Loader;
using ReplayParser.Interfaces;
using ReplayParser.Actions;
using System.Diagnostics;
using System.IO;
using ReplayParser.Clusterer.BuildorderTree;

namespace ReplayParser.Clusterer
{
    class Program
    {
        static void Main(string[] args)
        {
            //if (args == null || args.Length == 0 || String.IsNullOrEmpty(args[0]))
            //{
            //    Console.WriteLine("You need to specify a folder containing the replays.");
            //    return;
            //}

            ////Set path of folder containing replay files
            //string filepath = args[0];
            string filepath = @"F:\reps";
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

            // Get list of replay filesnames
            var files = Directory.EnumerateFiles(filepath, "*.rep");
            if (take > 0)
                files = files.Take(take);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            List<IReplay> parsedReplays = new List<IReplay>();
            foreach (var file in files)
            {
                // Parse replay
                var replay = ReplayLoader.LoadReplay(file);
                parsedReplays.Add(replay);
            }

            TreeBuilder tb = new TreeBuilder(parsedReplays);
            tb.Build();

            string supbitches = tb.ToString();
            System.Console.WriteLine(supbitches);
            using (StreamWriter writer = new StreamWriter(new FileStream("graphviz.txt", FileMode.Create)))
            {
                writer.Write(supbitches);
            }

            Kmeans c = new Kmeans();
            c.Cluster(5, tb.AllGames);

            sw.Stop();
            System.Console.WriteLine("Time: " + sw.Elapsed.TotalSeconds);
            System.Console.ReadKey();
        }
    }
}
