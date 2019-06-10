# BWSort v1.0
## What
A GUI application to sort, rename, filter and backup replays from the popular RTS-game Starcraft Brood War. 

## Features
+ Parse replays by importing replay files from multiple directories. View the parsed replays in the replay list view.
  + You can create ignore files and use them to ignore replays while parsing.
+ Categorize or "sort" replays into a new directory by choosing from the following list of criteria: player name, match-up, map, duration and game type. 
  + You can preview sort output
+ Rename replays according to a special syntax allowing you to extract information such as player names, match-up, race, duration, date,... You can rename replays as is, or also move them into a new output directory.
+ Filter parsed replays on player-name, winner, race, match-up, duration, date or map. Will support units and build orders in the near future as well!
+ Backup entire directories of replays and have the possibility to always recover them exactly as they were! You can easily share database files with each other manually. In the future there might be built-in way.

## How to install
Currently there are 3 ways to get the program:
1. Use the setup.exe installer from .RAR archive
2. Use the non-setup based .exe from the .RAR archive labelled as such
3. Compile from source

You can find the latest release here: <https://github.com/CrispyDrone/BWSort/releases/tag/v1.0>

## User guide
After an optional installation, run the .exe file.

### Parse replays
First things first, you will have to parse replays. Choose a directory by clicking the set directory button, and next click on the Add button:

![Choose a directory, and discover replays.](./images/help/parse-tab-step01.png)

You can repeatedly import replays from directories and they will show up in the listbox at the bottom of the screen. You can remove replays you don't want to parse by right clicking on them and choosing "Remove file". You can see that all the replays have a yellow box in front of them. This means these replays are waiting to be parsed. Next, click on parse and wait until all replays have been parsed. Once this finishes, a green box will indicate success, whereas a red box will indicate a failure to parse the replays.

![Parse replays, and receive feedback in the form of a green or red box.](./images/help/parse-tab-step02.png)

If there are certain replays that you never want to parse, you can make use of an ignore file. Click on the `Create - Edit` button at the right side of the window. This will open a new buffer for you to create an ignore file in, or it will edit the ignore file that's configured in the [advanced settings](#advanced-settings). 

![Create an ignore file to have the possibility to ignore specific files and directories during parsing.](./images/help/ignore-file.png)

You can click on the `Import directory` to import all the filenames (recursively) in that directory. You can click on the `Select filenames` to select multiple replays you would like to ignore. __Note__ that you specify filenames, however the ignore functionality works based on file hashes. This means that it will ignore these replays you've specified, regardless of what the actual file name is!

### View and filter replays
After you've parsed replays, you can see your results in the Search tab:

![Search for specific replays and see their stats in the search tab](./images/help/search-tab-after-parsing.png)

A crown icon indicates the winning players. An eye icon indicates observers. You can see the different teams that were part of the game. [Unfortunately, team identification is currently buggy due to known issues with the replay parser.](#known-issues). Each player's race is visible and there are some additional columns showing the map, duration, the date the game was played on, and the path of the replay file.

You can filter this list of replays by typing filter expressions in the search bar. Aside from using this search function to find a specific replay, you can also use the filtered output as input for the sorting and renaming actions, but more on that later.

As you can see, many maps don't have an actual image yet but instead a placeholder. If you want to, you can download images and add them to the `images/maps` folder. [In the future, this won't be necessary any more since I will use the data present inside the replay files and Brood War's MPQ file.](#towards-the-future) Name the file exactly like the map but use `_` instead of spaces and remove any special characters such as `'`. Check the [map section](#map-file-names) for a list of the expected file names of all maps.

#### Filter syntax
To use a filter you specify its code followed by a colon. You combine individual filters by separating them with a comma. To reset all filters and get back the original full list of replays, just use an empty filter and press enter.

Filter 		| code
---------------	| ----
Map		| m
Player		| p
Duration	| du
Match-up	| mu
Date		| d

For each of the following filters you can combine different conditions by using the vertical bar `|`:
+ Filter on map by using the `m:<mapname>` filter.

  ![Filter the replays by specifying partial or full map names. You can search for multiple map names at the same time by separating them with the vertical bar '|'.](./images/help/search-tab-filter-map.png)

  You can specify any part of the map name and it will find it. The `<mapname>` can also be a regular expression allowing for more advanced usage.

+ Filter on player by using the `p:<playername>` filter. This filter allows you to optionally specify whether this player needs to be a winner `& isWinner`, and which race they need to be `& race=<race>`.

  ![Specify a player name to filter on it. Add 'isWinner' and 'race=t' to further refine the search.](./images/help/search-tab-filter-player.png)

  You can search for any part of a player name. You can also add the `& isWinner` construct to filter out replays where this player lost, [unfortunately as mentioned before due to the buggy behavior of the parser this doesn't work that well yet.](#known-issues). You can also further restrict the set of replays to only show those where this player is of a specific race by using the `& race=<race>` construct, where race can be either the full name or the first letter (`z`, `t`, or `p`). You can search for multiple players at the same time by separating them with a comma. So for example: `p:MJ & race=z, Sadeas & race=p` would only give back replays that contain both a player whose name contains `MJ` and played as zerg, and a player with a name that contains `Sadeas` and who played as protoss.

+ Filter on duration by using the `du:<duration>` filter. This filter allows you to search for replays lesser than, greater than or equal a specific duration. You can also search for replays between 2 durations.

  ![Specify a duration to filter on it.](./images/help/search-tab-filter-duration.png)

  You can use specify digital or written durations and use the following operators `<`, `<=`, `>`, `>=`, `=` to specify ranges or an exact match. For the digital pattern you can specify minutes and seconds, so for example: `5:00` would mean 5 minutes. You can also specify hours like so `01:03:15` which would mean 1 hour, 3 minutes and 15 seconds. The written durations are of the following format `x<hours>y<minutes>z<seconds>` where any element can be optional: 
  + `<hours>`: can be either `h`, `hrs`, or `hours`
  + `<minutes>`: can be either `m`, `min`, or `minutes`
  + `<seconds>`: can be either `s`, `sec`, or `seconds`

  It is also possible to search for replays between 2 durations, use the `between x-y` construct for this. `x` and `y` can be any format previously discussed.

  ![Use between to search for replays between 2 durations.](./images/help/search-tab-filter-duration-between.png)

+ Filter on match-up by using the `mu:<matchup>` filter. This filter allows you to search for replays matching the desired match-up.

  ![Specify a match-up to filter on it.](./images/help/search-tab-filter-matchup.png)

  You can specify a match-up of the format `xvx` where x can be either `z`, `t`, `p`, or `.`. The latter is a wildcard meaning it can be any race. Currently it's not possible to search for "broken" match-ups where there's only a single team.

+ Filter on date by using the `d:<date>` filter. You can use absolute and relative dates.

  ![Specify a date in a relative or absolute format to filter on it.](./images/help/search-tab-filter-date.png)

  You can specify relative or absolute dates to filter replays. Same as for durations the operators `<`, `<=`, `>`, `>=`, `=` are available. By default the `=` is applied. Note the dates represent a specific point in time. This means that when you say `<4 months and 3 weeks ago` it does not mean "less than 4 months and 3 weeks ago" but instead it means __before__ 4 months and 3 weeks ago! The digital pattern is as follows `<year><sep><month><sep><day>`:
  + `<year>`: Mandatory, can be 2 or 4 digits. 
  + `<month>`: Optional, can be 1 or 2 digits. 
  + `<day>`: Optional, can be 1 or 2 digits.
  + `<sep>`: Separator, can be `-`, `.` or `/`.

  The relative dates support many more options:
  + You can specify `this` or `last` together with `year`, `month` or `week`.
  + You can specify `today` or `yesterday`
  + You can specify any month of the year literally such as `january`.
  + You can use a date of the format `<number><time-unit> ago` for example such as `5 months ago`, or `<number><time-unit> and ... ago` for example `5 months and 3 weeks ago`
    + 1 week is counted as 7 days
    + 1 month is counted as 31 days
    + 1 year is counted as 365 days
  + Finally there is also the `previous <number><time-unit>`. This allows you to more easily refer to a time range of a specific time unit. For example `previous 2 weeks` would span the period between the ending of last week and the beginning of 2 weeks ago.

  Finally, same as for the duration filter, you can use the `between x and y` construct to filter replays between 2 dates. `x` and `y` can be any format previously described. Note that `between` works __inclusively__ which means that it acts as if you stated `>= date1 and <= date2`.

#### Some additional examples
+ The between construct filters between 2 dates, inclusively.

  ![The between construct filters between 2 dates, inclusively.](./images/help/search-tab-filter-date-between-inclusively.png)

+ Use the greater than or equal operator to specify that you want all replays later than the point in time that follows it. **Note** As mentioned before, this might be confusing at first, so it's best to think of `>` as "later than" and `<` as earlier than.

  ![Use the greater than or equal operator to specify that you want all replays later than the point in time that follows it.](./images/help/search-tab-filter-date-greater-than-or-equal-ago.png)

+ Use the less than operator to specify that you want all replays earlier than the point in time that follows it. **Note** As mentioned before, this might be confusing at first, so it's best to think of `>` as "later than" and `<` as earlier than.

  ![Use the less than operator to specify that you want all replays earlier than the point in time that follows it.](./images/help/search-tab-filter-date-less-than-ago.png)

+ Use previous if you want to find all replays of the previous X days, weeks, months or years excluding the current day, week, month, or year. So for example, the previous 2 weeks would find all replays between Monday of 2 weeks ago and Sunday of last week.

  ![Use previous if you want to find all replays of the previous X days, weeks, months or years excluding the current day, week, month, or year.](./images/help/search-tab-filter-date-previous.png)

### Sorting replays
After parsing, you have the option to sort or categorize your replays to an output directory of your choice. You can either decide to sort the entire set of replays you've parsed, or to first filter them appropriately and then selecting the `Select as input` checkbox.

On the sort tab you specify the output directory, and can drag the blocks that represent the sort criteria into the order you want. To activate a sort criteria, click on it, it should turn blue. After you've decided on the sort criteria you want to use, you can tick off the `preview` checkbox and press the sort button below. This will render a preview inside the view. In case you also want to rename your replays while sorting, uncheck the `Keep original replay names` checkbox and you will be able to specify a renaming format. For the syntax, check the [renaming syntax section.](#renaming-syntax)

The following criteria are currently supported:

1. Playername: You can specify whether to make folders for winner-only, loser-only, both or none (this latter essentially allows you to extract/copy all replays from a directory tree, and rename them according to a custom format).
2. Map
3. Duration: You can specify your own intervals. `5 10 30` will result in the intervals 0-5min, 5-10min, 10-30min, and 30+min.
4. Matchup: You can specify which game types you want to include since it might not be so useful to know the "match-up" for a UMS-game.
5. Gametype

Finally it is possible to combine multiple sort criteria. For example `map playername` will sort your replays first on the map the game was played on, and then create additional folders per player name within the map folder.

![Preview of sort on map and duration without applying a renaming.](./images/help/sort-tab-preview.png)

### Renaming replays
You can now rename replays after parsing, either into an output directory or in place. You can either rename all replays, or only those that matched the latest used filter by ticking the `Select as input` checkbox on the search tab. If you want to experiment with the syntax without actually renaming your replays, make sure to tick off the `Preview` checkbox. After renaming, the transformation from the old to new filename will be shown in the output view. You can toggle between filenames only (hamburger icon) and the entire filepath (directory tree icon) by clicking on the button next to the 2 arrows. As you might have guessed, these 2 arrows are buttons for undoing and redoing a rename. The number of actions you can undo or redo can be configured in the advanced settings but by default is 10. If you are executing renames on many replays, be aware that this has the potential to quickly increase memory usage of BWSort. 

It is always possible to return to how the replays were named originally. Just tick off the checkbox `Restore original replay names` and execute!.

![Rename replays according to a custom format. You can rename in place or into an output directory. The output area shows how each filename has changed.](./images/help/rename-tab-in-place.png)

You can right click on a replay to open it in explorer or to select it in the search tab.

#### Renaming syntax
You can rename your replays by using special placeholders that start with the `/` character, currently BWSort supports the following placeholders:
+ `/WR` or `/Wr`: Stands for WinningRaces, which will be replaced by a comma separated list of the races of the winning players.
+ `/LR` or `/Lr`: Stands for LosingRaces, which will be replaced by a comma separated list of the races of the losing players.
+ `/R` or `/r`: Stands for Races, which will be replaced by a comma separated list of the races of all players (excluding observers).
+ `/WT` or `/Wt`: Stands for WinningTeam, which will be replaced by a comma separated list of all the names of the winning players.
+ `/LT` or `/Lt`: Stands for LosingTeams, which will be replaced by a comma separated list of all the names of the losing players. Each team will be surrounded by parentheses.
+ `/T`: Stands for Teams, which will be replaced by a comma separated list of all the names of all players, excluding observers. Each team will be surrounded by parentheses.
+ `/m`: Stands for map. This will print a short form i.e. the first letter of each word. This is sufficiently recognizable for most maps however some maps will have strange abbreviations.
+ `/M`: Stands for map. This will print the long form i.e. the full map name.
+ `/MU` or `/Mu`: Stands for Match-up. This will be replaced by the match-up.
+ `/d`: Stands for date. This will be replaced by the date in the year-month-day format.
+ `/D`: Stands for datetime. This will be replaced by the datetime in a format that resembles ISO-8601, without the timezone information, an example would be `2019-05-25T160510` which means 5 minutes and 10 seconds past 4 in the afternoon on 25th of May, 2019.
+ `/du`: Stands for duration. This will be replaced by the duration of the replay in a short format, an example would be `01_05_15` which means 1 hour, 5 minutes and 15 seconds.
+ `/DU` or `/Du`: Stands for duration. This will be replaced by the duration of the replay in a longer format by writing out the time units (hours, minutes, seconds). An example would be `1 hour 5 minutes 15 seconds`.
+ `/F`: Stands for game format. This will be replaced by the team grouping to give an indication of what kind of game it was i.e. 1v1, 2v2, 3v3...
+ `/gt`: Stands for game type. This will be replaced by the actual game type as known by Starcraft such as TopVsBottom (TvB), Melee (M), OneOnOne (OvO),... in an abbreviated form.
+ `/GT` or `/Gt`: Stands for game type. This will be replaced by the full name of the game type.
+ `/P`: Stands for players. This will be replaced by a comma separated list of all players including observers.
+ `/p`: Stands for players. This will be replaced by a comma separated list of all players excluding observers.
+ `/</>`: Stands for player info block. In this block you can specify arguments that will be applied to all players of the replay. This will return a list of the requested properties for each player, grouped by teams. You can add literal characters inside the block for example to surround the race with parentheses.
  + `/p`: Stands for player. This will be replaced by the name of the player.
  + `/R:` Stands for race. This will be replaced by the full name of the race of the player.
  + `/r`: Stands for race. This will be replaced by the first letter of the race of the player.
  + `/W`: Stands for winstatus. This will be replaced by `Winner` or `Loser` depending on whether the player is a winner or loser.
  + `/w`: Stands for winstatus. This will be replaced by `W` or `L` depending on whether the player is a winner or loser.
+ The following placeholders allow you to specify a non-negative (natural) integer. This corresponds to the identifier a player has in the game, unfortunately these are unpredictable.
  + `/Px`: Stands for player x. This will be replaced by the name of the x'th player.
  + `/Rx`: Stands for race x. This will be replaced by the full name of the race of the x'th player.
  + `/rx`: Stands for race x. This will be replaced by the first letter of the race of the x'th player.
  + `/Wx`: Stands for winstatus x. This will be replaced by `Winner` or `Loser` depending on whether the x'th player is a winner or loser.
  + `/wx`: Stands for winstatus x. This will be replaced by `W` or `L` depending on whether the x'th player is a winner or loser.
+ `/O`: Stands for original. This will be replaced by the original name of the replay.
+ `/c`: Stands for counter. This will increment on each replay that is being renamed.
+ `/C`: Stands for counter. This is exactly the same as `c` aside from padding 0's to the left to give a consistent width to the counter.

You can use these placeholders in an otherwise literally interpreted sentence: `Defiler tournament - /d - /Mu - /</p /r /w>` which would produce replays with names such as:
+ `Defiler tournament - 2019-05-03 - ZvZ - Jaedong Z W, CrispyDrone Z L`
+ `Defiler tournament - 2019-05-03 - PZvPZ - Bisu P W, Jaedong Z W, CrispyDrone Z L, AbstractDaddy P L`

#### Examples
+ Example using the `/C` construct that allows numbering of replays.

  ![Example using the `/C` construct that allows numbering of replays.](./images/help/rename-tab-example-01.png)

+ Example using the `/p` construct which extracts all players excluding observers.

  ![Example using the `/p` construct which extracts all players excluding observers.](./images/help/rename-tab-example-02.png)

### Backup replays
You can backup directories containing replays. First you will have to create a new database file; you can give it a name and create it in a specific directory, it will be automatically selected as the active database.

![Create a new database and select it as the active one.](./images/help/backup-tab-create-database.png)

To create a backup, press the create button at the bottom of the screen. A new window will pop up:

![Create a new backup by importing replays and specifying a name and optional comment.](./images/help/backup-tab-create-backup.png)

You can specify a name and an optional comment. To add replays to this backup click on the import button. At the moment, you can only import replays from one directory! This is because when restoring from a backup, it needs to be able to write to a single directory. [Maybe in the future, there will be support to make it so you can import replays from multiple directories, and it will congregate the multiple directories under one parent directory when executing the restore.](#towards-the-future) To now create a backup, click on the `Create backup` button.

![After the backup is successfully created, you can see it in the list of backup.](./images/help/backup-list.png)

After you've created a backup, you can inspect it. You'll be able to see its name, the comment, how many replays, which directory you backed up, on which date, and finally the directory and file hierarchy of the folder you backed up.

![You can inspect a backup, and see the structure of the folder and file hierarchy.](./images/help/backup-tab-inspect-backup.png)

Once you remember what this backup was all about, and you need to restore replays from it, you can press the restore button. Just select a directory and click on the restore button!

![You can restore from a backup to restore all replays exactly as they were at the time of backing up.](./images/help/backup-tab-restore-backup.png)

You can delete a backup in case you don't need it any more. Select the `Delete orphan replays` in case you want to also delete all replays that are not part of any backup.

![You can delete a backup, and specify whether to also delete all replays that are not part of any backup any more by ticking off the `Delete orphan replays` checkbox.](./images/help/backup-tab-delete-backup.png)

Finally, there are some extra buttons in the panel on the side:
+ Empty database: If for some reason you want to delete all the data in the database, you can click on this button.
+ Delete database: This will delete the database file, and from the list.
+ Clean database list: This will verify whether the databases still exist and if not, delete them from the list.
+ Add existing database: If you have an existing database that's not part of the list (someone shared it with you for example, or you moved the database to another location), use this button.

### Advanced settings

![There are many advanced settings you can fine tune to your liking.](./images/help/advanced-settings.png)

+ Max undo level: This setting controls the maximum number of undos or redos you can do.
+ Check for updates on startup: Check to check at startup whether a newer version is available.
+ Remember parsing directory: Check to remember the last-used parsing directory.
+ Include subdirectories by default while parsing: Check to always include subdirectories when discovering replays.
+ Load replays on startup: Check to start parsing replays automatically from the last-used parsing directory. This option requires Remember parsing directory to be checked.
+ Check for duplicates when parsing additional replays: When checked, will ensure no duplicate replays are parsed.
+ Ignore file path: The location of the ignore file you want to use. You still need to check the `Ignore specific files and directories` checkbox when parsing for it to have an effect. 
+ Logging directory: The directory to to write logging information to. This can be helpful to solve bugs and strange behavior.
+ Generate intermediate folders during sorting: When using multiple sort criteria, by default it will generate intermediary folders named after the criteria, if you don't like these folders, uncheck this option.

## Remarks
1. For now only replays of version 1.18 or later are supported. I haven't had the time yet to improve the parser. If anyone wants to help me implement this, feel free to contact me or make a pull request.
2. There will be errors for certain criteria like player name. If you specify to make a folder for the winner or for both, but the replay doesn't have a winner, it will fail to sort this replay.
3. Another common error is the "unable to distinguish player from observer", which means that not a single player did a build, unit training, or unit morph action. In most cases, this is a replay of a few seconds long where none of the players did a single action, so you can safely ignore these too.
4. If a replay shows up in a non-terminal folder (in case of multiple sort criteria), this means one of the errors as mentioned above occurred, meaning it was impossible to determine the winner, the match-up,...

Due to the possible presence of some bugs and it being hard to verify edge case behavior, I suggest either __not__ working on your original replay folder but instead on a copy *or* using the built-in [backup functionality](#backup-replays)!

## Known issues
1. The parser has many issues which affect:
   + team identification
   + matchup identification
   + observer identification
   + build order identification
   + the action list. [See the towards the future section.](#towards-the-future)
2. Using the `Use as input` functionality will result in a wrong reporting of the number of replays sorted or renamed.

## Towards the future
1. Add additional filters for the search tab based on units, and user defined build orders.
2. Allow sorting in place and add possibility to undo/redo.
3. Add additional sort criteria such as `date`, `build order`, `game format`,...
4. Add replay detail view with action history (i.e. build order) and some basic stats and graphs.
5. Add map rendering in the replay list view based on map data inside the replay instead of needing to use image files. This requires sprites that are present inside a BW installation's MPQ file.
6. Try to improve the parsing algorithm which will mean more reliable sorting, renaming, filtering,...:
   + This will fix team identification which is currently very buggy. Players are often reported to be on the same team even though they are opponents.
   + Match-up identification as a result is also buggy since players are not separated into the correct teams.
   + It will fix observer identification since actions in many instances aren't parsed correctly
   + It will fix the action list allowing much better insight into the build order of a replay
   + ...
7. Support for 1.16 replays
8. Allow backups of multiple directories at the same time.
9. General bug fixing.

In the very far future, there might be a complete rewrite from scratch with a much better designed codebase. To understand why this is necessary look at the [project history section](#project-history). 

## History
### Project history
At the end of 2017 I had just started to learn how to program and was still playing some Starcraft here and there. I was severely annoyed at the lack of support from Blizzard in regards to managing replays. I thought this could be the ideal way to gain some experience as a new developer and at the same time help out the Starcraft community. As it was my first real project ever, and many of the important design decisions were made during this period when I had absolutely no experience, the code base is very badly designed and a pain to work with.

### Change history
#### v1.0
+ Rewrote the renaming feature. It is now much more flexible and supports many more options.
+ Removed support to rename the last sort since it was too complex and made some aspects of the UI confusing.
+ Updated graphical user interface to be more intuitive.
  + Added a view to discover replay files before parsing allowing you to craft the set of replays you want to parse i.e. you can import from multiple directories and remove individual replays.
  + Updated the layout of the sorting and renaming tabs.
  + Added a window that will render the output of the sort or rename action.
+ Added support for previewing replay sorts.
+ Added support for previesing replay renamings.
+ Added advanced settings window that allows you to set options such as remember last parsing directory, parse on startup,...
+ Added "ignore" file functionality, which allows you to prevent parsing of specific replays based on file hashes.
+ Added a parsed replay list that shows information such as players, winners, races, map, filepath,...
+ Added filter functionality to search the parsed replay list. Currently supported filters are player name, race, winner, map, duration, match-up, date. You can then use this filtered set of replays for sorting or renaming purposes.
+ Added undo/redo functionality when renaming replays.
+ Added backup functionality that allows you to backup replay folders. This will make sure you can always restore your replay folders just the way they were at time of backup.
+ Added automatic checking for newer versions.
+ Added a help section that renders this README file inside BWSort.

#### v0.9
+ Added support for renaming replays without sorting.
  + Rename replays in place
  + Rename the last sort
  + Rename replays to an output directory
+ Added support to "undo" (i.e. reset) replay names to their originals at time of parsing.

#### v0.8
+ Fixed major issue with nested sorts resulting in replays being sorted into the wrong folder, and receiving the wrong names.

#### v0.7
+ Added a simple graphical user interface 

#### v0.6 
+ Fixed issue with wrong replays being sorted/renamed in case you opted to not move replays that encountered errors during parsing.
+ Added the possibility to specify output directories for "bad replays" and sorting.

#### v0.5
First release:
+ Sort replays according to criteria, and optionally rename them according to a custom format.

## License
This project is licensed under the GNU GPLv3 license.

## Acknowledgements
Many thanks to SimplySerenity for porting the replay parser to C#. You can find the original project here: <https://github.com/SimplySerenity/SCReplayFileParser>

## Attributions
+ <div>Crown icon made by <a href="https://www.flaticon.com/authors/pixel-perfect" title="Pixel perfect">Pixel perfect</a> from <a href="https://www.flaticon.com/" title="Flaticon">www.flaticon.com</a> is licensed by <a href="http://creativecommons.org/licenses/by/3.0/" title="Creative Commons BY 3.0" target="_blank">CC 3.0 BY</a>.</div>

## Map File Names
+ `815.jpg`
+ `acheron.jpg`
+ `alchemist.jpg`
+ `alternative.jpg`
+ `andromeda.jpg`
+ `another_day.jpg`
+ `arcadia.jpg`
+ `arizona.jpg`
+ `arkanoid.jpg`
+ `ashrigo.jpg`
+ `athena.jpg`
+ `autobahn.jpg`
+ `avalon.jpg`
+ `avant_garde.jpg`
+ `azalea.jpg`
+ `aztec.jpg`
+ `baekmagoji.jpg`
+ `beltway.jpg`
+ `benzene.jpg`
+ `bifrost.jpg`
+ `blade_storm.jpg`
+ `blaze.jpg`
+ `blitz.jpg`
+ `block_chain.jpg`
+ `bloody_ridge.jpg`
+ `blue_storm.jpg`
+ `byzantium.jpg`
+ `camelot.jpg`
+ `carthage.jpg`
+ `central_plains.jpg`
+ `chain_reaction.jpg`
+ `chariots_of_fire.jpg`
+ `charity.jpg`
+ `chupung-ryeong.jpg`
+ `circuit_breaker.jpg`
+ `colosseum.jpg`
+ `crimson_isles.jpg`
+ `cross_game.jpg`
+ `crossing_field.jpg`
+ `dmz.jpg`
+ `dahlia_of_jungle.jpg`
+ `dantes_peak.jpg`
+ `dantes_peak_se.jpg`
+ `dark_sauron.jpg`
+ `dark_stone.jpg`
+ `deep_purple.jpg`
+ `demian.jpg`
+ `demons_forest.jpg`
+ `desert_fox.jpg`
+ `desperado.jpg`
+ `destination.jpg`
+ `detonation.jpg`
+ `dream_of_balhae.jpg`
+ `eddy.jpg`
+ `el_niño.jpg`
+ `electric_circuit.jpg`
+ `elysion.jpg`
+ `empire_of_the_sun.jpg`
+ `enter_the_dragon.jpg`
+ `estrella.jpg`
+ `eye_in_the_sky.jpg`
+ `eye_of_the_storm.jpg`
+ `face_off.jpg`
+ `fantasy.jpg`
+ `fighting_spirit.jpg`
+ `flight-dreamliner.jpg`
+ `forbidden_zone.jpg`
+ `forte.jpg`
+ `fortress.jpg`
+ `fortress_se.jpg`
+ `full_moon.jpg`
+ `gaema_gowon.jpg`
+ `gaia.jpg`
+ `gauntlet_2003.jpg`
+ `geometry.jpg`
+ `glacial_epoch.jpg`
+ `gladiator.jpg`
+ `gold_rush.jpg`
+ `gorky_island.jpg`
+ `grand_line.jpg`
+ `grand_line_se.jpg`
+ `great_barrier_reef.jpg`
+ `ground_zero.jpg`
+ `guillotine.jpg`
+ `hall_of_valhalla.jpg`
+ `hannibal.jpg`
+ `harmony.jpg`
+ `heartbreak_ridge.jpg`
+ `hitchhiker.jpg`
+ `holy_world.jpg`
+ `holy_world_se.jpg`
+ `hunters.jpg`
+ `hwangsanbul.jpg`
+ `hwarangdo.jpg`
+ `icarus.jpg`
+ `incubus.jpg`
+ `indian_lament.jpg`
+ `into_the_darkness.jpg`
+ `iron_curtain.jpg`
+ `jade.jpg`
+ `jim_raynors_memory.jpg`
+ `judgment_day.jpg`
+ `jungle_story.jpg`
+ `katrina.jpg`
+ `korhal_of_ceres.jpg`
+ `la_mancha.jpg`
+ `legacy_of_char.jpg`
+ `loki.jpg`
+ `longinus.jpg`
+ `lost_temple.jpg`
+ `luna.jpg`
+ `martian_cross.jpg`
+ `match_point.jpg`
+ `medusa.jpg`
+ `mercury.jpg`
+ `mercury_zero.jpg`
+ `monte_cristo.jpg`
+ `monty_hall.jpg`
+ `monty_hall_se.jpg`
+ `moon_glaive.jpg`
+ `multiverse.jpg`
+ `namja_iyagi.jpg`
+ `nemesis.jpg`
+ `neo_arkanoid.jpg`
+ `neo_aztec.jpg`
+ `neo_bifrost.jpg`
+ `neo_blaze.jpg`
+ `neo_electric_circuit.jpg`
+ `neo_forbidden_zone.jpg`
+ `neo_forte.jpg`
+ `neo_ground_zero.jpg`
+ `neo_guillotine.jpg`
+ `neo_hall_of_valhalla.jpg`
+ `neo_harmony.jpg`
+ `neo_jungle_story.jpg`
+ `neo_legacy_of_char.jpg`
+ `neo_requiem.jpg`
+ `neo_silent_vortex.jpg`
+ `neo_sylphid.jpg`
+ `neo_transistor.jpg`
+ `neo_vertigo.jpg`
+ `new_bloody_ridge.jpg`
+ `new_heartbreak_ridge.jpg`
+ `new_sniper_ridge.jpg`
+ `nostalgia.jpg`
+ `odd-eye.jpg`
+ `odin.jpg`
+ `old_plains_to_hill.jpg`
+ `othello.jpg`
+ `outlier.jpg`
+ `outsider.jpg`
+ `outsider_se.jpg`
+ `overwatch.jpg`
+ `paradoxxx.jpg`
+ `parallel_lines.jpg`
+ `paranoid_android.jpg`
+ `pathfinder.jpg`
+ `peaks_of_baekdu.jpg`
+ `pelennor.jpg`
+ `persona.jpg`
+ `pioneer_period.jpg`
+ `plains_to_hill.jpg`
+ `plasma.jpg`
+ `polaris_rhapsody.jpg`
+ `python.jpg`
+ `r-point.jpg`
+ `ragnarok.jpg`
+ `raid_assault.jpg`
+ `requiem.jpg`
+ `return_of_the_king.jpg`
+ `reverse_temple.jpg`
+ `ride_of_valkyries.jpg`
+ `rivalry.jpg`
+ `river_of_flames.jpg`
+ `roadkill.jpg`
+ `roadrunner.jpg`
+ `rush_hour.jpg`
+ `seongangil.jpg`
+ `shin_peaks_of_baekdu.jpg`
+ `showdown.jpg`
+ `silent_vortex.jpg`
+ `sin_815.jpg`
+ `sin_chupung-ryeong.jpg`
+ `sin_gaema_gowon.jpg`
+ `sin_peaks_of_baekdu.jpg`
+ `sin_pioneer_period.jpg`
+ `sniper_ridge.jpg`
+ `snowbound.jpg`
+ `space_odyssey.jpg`
+ `sparkle.jpg`
+ `sylphid.jpg`
+ `symmetry_of_psy.jpg`
+ `taebaek_mountains.jpg`
+ `tau_cross.jpg`
+ `tears_of_the_moon.jpg`
+ `the_eye.jpg`
+ `the_hunters.jpg`
+ `the_huntress.jpg`
+ `third_world.jpg`
+ `tiamat.jpg`
+ `tornado.jpg`
+ `transistor.jpg`
+ `triathlon.jpg`
+ `tripod.jpg`
+ `troy.jpg`
+ `tucson.jpg`
+ `u-boat.jpg`
+ `ultimatum.jpg`
+ `un_goro_crater.jpg`
+ `usan_nation.jpg`
+ `valley_of_wind.jpg`
+ `vampire.jpg`
+ `vertigo_plus.jpg`
+ `whiteout.jpg`
+ `wishbone.jpg`
+ `wuthering_heights.jpg`
+ `xeno_sky.jpg`
+ `zodiac.jpg`
