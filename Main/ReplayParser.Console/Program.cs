using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Loader;
using ReplayParser.Interfaces;
using ReplayParser.Actions;
using System.Diagnostics;

namespace ReplayParser.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            //var replay = ReplayLoader.LoadReplay("0022_PvT_Vanko_buralzzan.rep(281).rep");

            Stopwatch sw = new Stopwatch();
            sw.Start();
            var replay = ReplayLoader.LoadReplay("1.rep");
            sw.Stop();

            System.Console.WriteLine("Parse Time: " + sw.Elapsed.TotalSeconds);
            PrintActionType<BuildAction>(replay);
            PrintActionType<GameChatAction>(replay);
        }

        public static void PrintActionType<T>(IReplay replay)
        {

            var actions1 = replay.Actions.Where(x => x is T)
                .OrderBy(x => x.Frame)
                .ToList<IAction>();

            foreach (var a in actions1)
            {
                if (typeof(T) == typeof(BuildAction))
                {
                    System.Console.Write("{0,10} - {2,-15} - {1,10}", a.Frame, a.ActionType, a.Player.Name);
                    System.Console.Write(" - {0}", ((BuildAction)a).ObjectType);
                }
                else if (typeof(T) == typeof(GenericObjectTypeAction))
                {
                    System.Console.Write("{0,10} - {2,-15} - {1,10}", a.Frame, a.ActionType, a.Player.Name);
                    System.Console.Write(" - {0}", ((GenericObjectTypeAction)a).ObjectType);
                }
                else
                {
                    System.Console.Write(a);
                }

                System.Console.WriteLine();
            }
        }
    }
}
