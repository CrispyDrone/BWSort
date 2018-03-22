# BWSort
Only ReplayParser and ReplaySorter are necessary.
ReplaySorter is a console application that targets .net framework 4.6.
You provide a directory, it will parse all the replays (can include subdirectories), and allow you to both rename them according to a custom
format, and order them in a mapping hierarchy.

Currently supported sort criteria:
1. playername
2. map
3. duration
4. matchup
5. gametype

You can combine multiple sort criteria.

Syntax for renaming replays:

1)  T[separator]
with separator being one char. This will extract the teams from the replay and separate individual players per team using the separator.

2) WT[separator]
only extracts the winning team.

3) LT[separator]
only extracts the losing team

4) MU 
gets the matchup

5) M
gets the map

6) DU
gets the game length

You have to use | to separate arguments. So for example:

DU|M|WT[_]      => will give a replay of the format: 15min13sFightingSpirit(CrispyDrone_Jaedong).rep
