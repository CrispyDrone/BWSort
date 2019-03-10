# BWSort v0.9

## What
A simple GUI application to sort and rename replay files from the popular RTS game starcraft brood war. There is also an (outdated) console version that is unfortunately not written with the linux philosophy in mind.

## How
After an optional installation, run the .exe file (ending in .UI for the GUI or without .UI for the console). Provide a directory containing your replays and it will try to parse all the replays, and allow you to both rename them according to a custom format, and order them in a mapping hierarchy.

IMPORTANT: Due to the presence of some bugs and it being hard to verify edge case behavior, I suggest __not__ working on your original replay folder but instead always making a copy and working on that one!

### Sorting replays

1. Playername

   You can specify whether to make folders for winner-only, loser-only, both or none (this latter essentially allows you to extract/copy all replays from a directory tree, and rename them according to a custom format).

2. Map
3. Duration

   You can specify your own intervals. `5 10 30` will result in the intervals 0-5min, 5-10min, 10-30min, and 30+min.

4. Matchup

   You can specify which gametypes you want to include since it might not be so useful to know the "matchup" for a UMS game.

5. Gametype


Finally it is possible to combine multiple sort criteria. For example `map playername` will sort your replays first on the map the game was played on, and then create additional folders per playername within the map folder.

In case you just want to rename your replays, check out the next section.

### Renaming replays
This release includes support for direct renaming of replays after parsing. You can do any of the following: 
+ After sorting you decide you wanted to rename your replays? Use the rename last sort option.
+ You want to rename the replays inside the folder you've parsed? Use rename in place.
+ You want to copy all replays to another directory and rename them? Specify an output directory and rename!

Currently you will get a warning if you try to rename in place after sorting or renaming to an output directory. This is to warn you that you will not be able to rename the last sort, or undo a rename any more. To continue, you have to "restore replay names". Be careful however! See [remarks](#remarks) and [known issues](#known-issues).

It is also possible to "undo" (see clarification below) your last renaming action, whether it was a rename in place, a rename last sort, or a rename to another directory.

To clarify:
+ Restoring replays is an interal operation you can do if stuff isn't working any more. This precludes having to parse replays again from scratch. Generally you shouldn't need it unless you decide you want to do a rename in place after a sort or rename to output directory.
  + However do __not__ use restore after "rename in place" because you will have to parse again and won't be able to undo the rename since it won't be able to find the replay files any more!
+ Undo last rename is badly named. It restores the replay names to whatever they were when you parsed them.

#### Syntax for renaming replays:
The current syntax is quite sensitive, so be careful to not include any unnecessary spaces anywhere. The syntax consists of:
1. T[separator] with separator being a single character (for example `,` or `_` ). This will extract the teams from the replay and separate individual players per team using the separator.
2. WT[separator] only extracts the winning team.
3. LT[separator] only extracts the losing team.
4. MU gets the matchup.
5. M gets the map.
6. DU gets the game length.
7. D gets the date the game was played on.
8. WR extracts the winning race.
9. LR extracts the losing race.

You have to use vertical bars `|` to separate arguments. So for example:

`DU|M|WT[_]` => will give a replay of the format: 15min13sFightingSpirit(CrispyDrone_Jaedong).rep

## How to install
Currently there are 3 ways to get the program:
1. Use the setup.exe installer from .rar archive
2. Use the non-setup based .exe from the .rar archive labelled as such
3. Compile from source

You can find the latest release here: https://github.com/CrispyDrone/BWSort/releases/tag/v0.9

## Remarks
1. Do __NOT__ use restore after "rename in place". It will force you to reparse the replays and you won't be able to undo the renaming. Restore is for when the program for some reason doesn't seem to work any more. Or you want to rename your replays in place after sorting or renaming to an output directory.
2. For now only 1.18 or later replays are supported. I was looking into a way to decompress the PKWARE compressed replay files, but the algorithm used in other parsers didn't make much sense to me. If anyone wants to help me implement this, feel free to contact me.
3. There will be errors for certain criteria like playername. If you specify to make a folder for the winner or for both, but the replay doesn't have a winner, it'll show an error.
4. Another common error is the "unable to distinguish player from observer", which means that not a single player did a build, unit training, or unit morph action. In most cases, this is a replay of a few seconds long where no player did anything, so you can safely ignore these too.
5. If a replay shows up in a non-terminal folder (in case of multipe sort criteria), this means one of the errors as mentioned above occurred, meaning it was impossible to determine the winner, the matchup,...


## Known issues:
1. When using multiple sort criteria (nested sort), the sort folder's name will not be in the correct order.
2. The order of the matchup from a replay, and using the T[] argument, will not necessarily be the same (so it could say ZvP, while the first player is the protoss, and the second one the zerg)
3. When using nested sorting, it creates an intermediary folder for each sort criteria, instead of directly adding the result folders. For example: map duration will create the following folders Map,Duration => Fighting spirit => duration => 0-10m instead of Map,Duration => Fighting spirit => 0-10m
4. Teams aren't extracted properly because for most game types the team number is the same for opposing teams... I think I do know a way to fix this.
5. Restore after "rename in place" will force you to reparse replays.

## Towards the future:
For users:
1. Support to search replays based on criteria, and then select the filtered replays to perform a sort or renaming action.
2. Improved GUI, more intuitive with more features
3. Unix style console application accepting parameters instead of acting like a wizard.
4. Trying to improve the parsing algorithm which will mean more reliable sorting and renaming
5. Support for 1.16 replays
6. Fixing team extraction (this will also fix matchups not being determined properly)
7. Bug fixing

For my reputation:
1. A complete rewrite from scratch with a much better designed codebase. I started this project roughly 1.5 months after I had started programming. Many of the important design decisions were made during this period when I had no experience with writing bigger projects.

## Change history
### v0.9
+ Added support for renaming replays without sorting.
  + Rename replays in place
  + Rename the last sort
  + Rename replays to an output directory
+ Added support to "undo" (i.e. restore) replay names to their originals at time of parsing.

### v0.8
+ Fixed major issue with nested sorts resulting in replays being sorted into the wrong folder, and receiving the wrong names.

### v0.7
+ Added a simple graphical user interface 

### v0.6 
+ Fixed issue with wrong replays being sorted/renamed in case you opted to not move replays that encountered errors during parsing.
+ Added the possibility to specify output directories for "bad replays" and sorting.

### v0.5
First release:
+ Sort replays according to criteria, and optionally rename them according to a custom format.



Many thanks to SimplySerenity for porting the replay parser to C#. You can find the original project here: https://github.com/SimplySerenity/SCReplayFileParser
