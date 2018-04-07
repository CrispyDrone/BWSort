# BWSort v0.6

## What
A simple console application to sort and rename replay files from the popular RTS game starcraft brood war.

## How
Run the .exe file, provide a directory containing your replays and it will try to parse all the replays (can include subdirectories), and allow you to both rename them according to a custom format, and order them in a mapping hierarchy.

###### Currently supported sort criteria:

1) playername

You can specify whether to make folders for winner-only, loser-only, both or none (this latter essentially allows you to extract/copy all replays from a directory tree,
and rename them according to a custom format).

2) map
3) duration

You can specify your own intervals. `5 10 30` will result in the intervals 0-5min, 5-10min, 10-30min, and 30+min.

4) matchup

You can specify which gametypes you want to include since it might not be so useful to know the "matchup" for a UMS game.

5) gametype

You can combine multiple sort criteria. For example `map playername` will sort your replays first on the map the game was played on, and then create additional folders
per playername within the map folder.

###### Syntax for renaming replays:

1) T[separator] with separator being one char (for example , or _ ). This will extract the teams from the replay and separate individual players per team using the separator.

2) WT[separator] only extracts the winning team.

3) LT[separator] only extracts the losing team

4) MU gets the matchup

5) M gets the map

6) DU gets the game length

7) D gets the date the game was played on

8) WR extracts the winning race

9) LR extracts the losing race

You have to use | to separate arguments. So for example:

DU|M|WT[_] => will give a replay of the format: 15min13sFightingSpirit(CrispyDrone_Jaedong).rep

## How to install

1) clone repository, compile

or

2) download zip file under Releases, extract, run the application file. You will need .net framework 4.6, it might prompt you to install it.

## Remarks

1. For now only 1.18 or later replays are supported. I was looking into a way to decompress the PKWARE compressed replay files, but the algorithm used in other parsers didn't make much sense to me. If anyone wants to help me implement this, feel free to contact me.
2. There will be errors for certain criteria like playername. If you specify to make a folder for the winner or for both, but the replay doesn't have a winner, it'll show an error.
3. Another common error is the "unable to distinguish player from observer", which means that not a single player did a build, unit training, or unit morph action. In most cases, this is a replay of a few seconds long where no player did anything, so you can safely ignore these too.

## Known issues:
1. When using multiple sort criteria (nested sort), the sort folder's name will not be in the correct order.
2. The order of the matchup from a replay, and using the T[] argument, will not necessarily be the same (so it could say ZvP, while the first player is the protoss, and the second one the zerg)
3. When using nested sorting, it creates an intermediary folder for each sort criteria, instead of directly adding the result folders. For example: map duration will create the following folders Map,Duration => Fighting spirit => duration => 0-10m instead of Map,Duration => Fighting spirit => 0-10m
4. Teams aren't extracted properly because for most game types the team number is the same for opposing teams... I think I do know a way to fix this.


## Towards the future:
1. Bug fixing
2. Possibly support for 1.16 replays
3. Fixing team extraction (this will also fix matchups not being determined properly)
4. Graphical user interface

Many thanks to SimplySerenity for porting the replay parser to C#. You can find the original project here: https://github.com/SimplySerenity/SCReplayFileParser
