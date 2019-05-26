using ReplayParser.ReplaySorter.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ReplayParser.ReplaySorter.UI.Converters
{
    // alternatively complete the parsing of the map, and somehow render a preview from it...
    public class MapToFileNameConverter : IValueConverter
    {
        // structure to map strings that match regex to a constant string, which is the map name that will be mapped to an image
        public static ILookup<char, Tuple<Regex, string>> _mapNamesToFileNamesLookup;
        public static HashSet<string> _mapNames;
        public static string _assemblyPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        static MapToFileNameConverter()
        {
            var mapRegexToFileNameDictionary = new Dictionary<Regex, string> {
                { new Regex(@"815", RegexOptions.IgnoreCase), "815" },
                // { new Regex(@"815(?!\s+I)", RegexOptions.IgnoreCase), "815" },
                // { new Regex(@"815(?=\s*III)", RegexOptions.IgnoreCase), "815_iii" },
                { new Regex(@"acheron", RegexOptions.IgnoreCase), "acheron" },
                { new Regex(@"alchemist", RegexOptions.IgnoreCase), "alchemist" },
                { new Regex(@"alternative", RegexOptions.IgnoreCase), "alternative" },
                { new Regex(@"andromeda", RegexOptions.IgnoreCase), "andromeda" },
                { new Regex(@"another", RegexOptions.IgnoreCase), "another_day" },
                { new Regex(@"arcadia", RegexOptions.IgnoreCase), "arcadia" },
                // { new Regex(@"", RegexOptions.IgnoreCase), "arcadia_2" },
                // { new Regex(@"", RegexOptions.IgnoreCase), "arcadia_ii" },
                { new Regex(@"arizona", RegexOptions.IgnoreCase), "arizona" },
                { new Regex(@"(?<!neo).*arkanoid", RegexOptions.IgnoreCase), "arkanoid" },
                { new Regex(@"ashrigo", RegexOptions.IgnoreCase), "ashrigo" },
                { new Regex(@"athena", RegexOptions.IgnoreCase), "athena" },
                { new Regex(@"autobahn", RegexOptions.IgnoreCase), "autobahn" },
                { new Regex(@"avalon", RegexOptions.IgnoreCase), "avalon" },
                { new Regex(@"avant", RegexOptions.IgnoreCase), "avant_garde" },
                // { new Regex(@"", RegexOptions.IgnoreCase), "avant_garde_2" },
                { new Regex(@"azalea", RegexOptions.IgnoreCase), "azalea" },
                { new Regex(@"(?<!neo).*aztec", RegexOptions.IgnoreCase), "aztec" },
                { new Regex(@"baekmagoji", RegexOptions.IgnoreCase), "baekmagoji" },
                { new Regex(@"beltway", RegexOptions.IgnoreCase), "beltway" },
                { new Regex(@"benzene", RegexOptions.IgnoreCase), "benzene" },
                { new Regex(@"(?<!neo).*bifrost", RegexOptions.IgnoreCase), "bifrost" },
                //{ new Regex(@"", RegexOptions.IgnoreCase), "bifrost_3" },
                { new Regex(@"blade", RegexOptions.IgnoreCase), "blade_storm" },
                { new Regex(@"(?<!neo).*blaze", RegexOptions.IgnoreCase), "blaze" },
                { new Regex(@"blitz", RegexOptions.IgnoreCase), "blitz" },
                //{ new Regex(@"", RegexOptions.IgnoreCase), "blitz_x" },
                { new Regex(@"block", RegexOptions.IgnoreCase), "block_chain" },
                { new Regex(@"bloody", RegexOptions.IgnoreCase), "bloody_ridge" },
                { new Regex(@"blue", RegexOptions.IgnoreCase), "blue_storm" },
                { new Regex(@"byzantium", RegexOptions.IgnoreCase), "byzantium" },
                //{ new Regex(@"", RegexOptions.IgnoreCase), "byzantium_2" },
                //{ new Regex(@"", RegexOptions.IgnoreCase), "byzantium_3" },
                { new Regex(@"camelot", RegexOptions.IgnoreCase), "camelot" },
                { new Regex(@"carthage", RegexOptions.IgnoreCase), "carthage" },
                //{ new Regex(@"", RegexOptions.IgnoreCase), "carthage_3" },
                { new Regex(@"central", RegexOptions.IgnoreCase), "central_plains" },
                { new Regex(@"chain", RegexOptions.IgnoreCase), "chain_reaction" },
                { new Regex(@"chariots", RegexOptions.IgnoreCase), "chariots_of_fire" },
                { new Regex(@"charity", RegexOptions.IgnoreCase), "charity" },
                { new Regex(@"chupung", RegexOptions.IgnoreCase), "chupung-ryeong" },
                { new Regex(@"circuit", RegexOptions.IgnoreCase), "circuit_breaker" },
                { new Regex(@"colosseum", RegexOptions.IgnoreCase), "colosseum" },
                //{ new Regex(@"", RegexOptions.IgnoreCase), "colosseum_ii" },
                { new Regex(@"crimson", RegexOptions.IgnoreCase), "crimson_isles" },
                { new Regex(@"cross", RegexOptions.IgnoreCase), "cross_game" },
                { new Regex(@"crossing", RegexOptions.IgnoreCase), "crossing_field" },
                { new Regex(@"dmz", RegexOptions.IgnoreCase), "dmz" },
                { new Regex(@"dahlia", RegexOptions.IgnoreCase), "dahlia_of_jungle" },
                { new Regex(@"dante.*(?!se)", RegexOptions.IgnoreCase), "dantes_peak" },
                { new Regex(@"dante.*(?=se)", RegexOptions.IgnoreCase), "dantes_peak_se" },
                { new Regex(@"sauron", RegexOptions.IgnoreCase), "dark_sauron" },
                //{ new Regex(@"", RegexOptions.IgnoreCase), "dark_sauron_2" },
                { new Regex(@"stone", RegexOptions.IgnoreCase), "dark_stone" },
                { new Regex(@"deep", RegexOptions.IgnoreCase), "deep_purple" },
                { new Regex(@"demian", RegexOptions.IgnoreCase), "demian" },
                { new Regex(@"demons", RegexOptions.IgnoreCase), "demons_forest" },
                { new Regex(@"desert", RegexOptions.IgnoreCase), "desert_fox" },
                { new Regex(@"desperado", RegexOptions.IgnoreCase), "desperado" },
                { new Regex(@"destination", RegexOptions.IgnoreCase), "destination" },
                { new Regex(@"detonation", RegexOptions.IgnoreCase), "detonation" },
                //{ new Regex(@"", RegexOptions.IgnoreCase), "detonation_f" },
                { new Regex(@"dream", RegexOptions.IgnoreCase), "dream_of_balhae" },
                { new Regex(@"eddy", RegexOptions.IgnoreCase), "eddy" },
                { new Regex(@"el\s+ ni", RegexOptions.IgnoreCase), "el_niño" },
                { new Regex(@"(?<!neo).*electric", RegexOptions.IgnoreCase), "electric_circuit" },
                { new Regex(@"elysion", RegexOptions.IgnoreCase), "elysion" },
                { new Regex(@"empire", RegexOptions.IgnoreCase), "empire_of_the_sun" },
                { new Regex(@"enter", RegexOptions.IgnoreCase), "enter_the_dragon" },
                //{ new Regex(@"", RegexOptions.IgnoreCase), "enter_the_dragon_2004" },
                { new Regex(@"estrella", RegexOptions.IgnoreCase), "estrella" },
                { new Regex(@"sky", RegexOptions.IgnoreCase), "eye_in_the_sky" },
                { new Regex(@"storm", RegexOptions.IgnoreCase), "eye_of_the_storm" },
                { new Regex(@"face", RegexOptions.IgnoreCase), "face_off" },
                { new Regex(@"fantasy", RegexOptions.IgnoreCase), "fantasy" },
                //{ new Regex(@"", RegexOptions.IgnoreCase), "fantasy_ii" },
                { new Regex(@"fighting", RegexOptions.IgnoreCase), "fighting_spirit" },
                { new Regex(@"flight", RegexOptions.IgnoreCase), "flight-dreamliner" },
                { new Regex(@"(?<!neo).*forbidden", RegexOptions.IgnoreCase), "forbidden_zone" },
                { new Regex(@"(?<!neo).*forte", RegexOptions.IgnoreCase), "forte" },
                { new Regex(@"fortress.*(?!se)", RegexOptions.IgnoreCase), "fortress" },
                { new Regex(@"fortress.*(?=se)", RegexOptions.IgnoreCase), "fortress_se" },
                { new Regex(@"full", RegexOptions.IgnoreCase), "full_moon" },
                { new Regex(@"(?<!sin).*gaema", RegexOptions.IgnoreCase), "gaema_gowon" },
                { new Regex(@"gaia", RegexOptions.IgnoreCase), "gaia" },
                { new Regex(@"gauntlet", RegexOptions.IgnoreCase), "gauntlet_2003" },
                //{ new Regex(@"", RegexOptions.IgnoreCase), "gauntlet_tg" },
                { new Regex(@"geometry", RegexOptions.IgnoreCase), "geometry" },
                { new Regex(@"glacial", RegexOptions.IgnoreCase), "glacial_epoch" },
                { new Regex(@"gladiator", RegexOptions.IgnoreCase), "gladiator" },
                { new Regex(@"gold", RegexOptions.IgnoreCase), "gold_rush" },
                { new Regex(@"gorky", RegexOptions.IgnoreCase), "gorky_island" },
                { new Regex(@"grand.*(?!se)", RegexOptions.IgnoreCase), "grand_line" },
                { new Regex(@"grand.*(?=se)", RegexOptions.IgnoreCase), "grand_line_se" },
                { new Regex(@"great", RegexOptions.IgnoreCase), "great_barrier_reef" },
                { new Regex(@"(?<!neo).*ground", RegexOptions.IgnoreCase), "ground_zero" },
                { new Regex(@"(?<!neo).*guillotine", RegexOptions.IgnoreCase), "guillotine" },
                { new Regex(@"(?<!neo).*valhalla", RegexOptions.IgnoreCase), "hall_of_valhalla" },
                { new Regex(@"hannibal", RegexOptions.IgnoreCase), "hannibal" },
                { new Regex(@"(?<!neo).*harmony", RegexOptions.IgnoreCase), "harmony" },
                { new Regex(@"heartbreak", RegexOptions.IgnoreCase), "heartbreak_ridge" },
                { new Regex(@"hitchhiker", RegexOptions.IgnoreCase), "hitchhiker" },
                { new Regex(@"holy.*(?!se)", RegexOptions.IgnoreCase), "holy_world" },
                { new Regex(@"holy.*(?=se)", RegexOptions.IgnoreCase), "holy_world_se" },
                { new Regex(@"hunters", RegexOptions.IgnoreCase), "hunters" },
                { new Regex(@"hwangsanbul", RegexOptions.IgnoreCase), "hwangsanbul" },
                { new Regex(@"hwarangdo", RegexOptions.IgnoreCase), "hwarangdo" },
                { new Regex(@"icarus", RegexOptions.IgnoreCase), "icarus" },
                { new Regex(@"incubus", RegexOptions.IgnoreCase), "incubus" },
                //{ new Regex(@"", RegexOptions.IgnoreCase), "incubus_2004" },
                { new Regex(@"indian", RegexOptions.IgnoreCase), "indian_lament" },
                //{ new Regex(@"", RegexOptions.IgnoreCase), "indian_lament_2" },
                { new Regex(@"darkness", RegexOptions.IgnoreCase), "into_the_darkness" },
                //{ new Regex(@"", RegexOptions.IgnoreCase), "into_the_darkness_2" },
                { new Regex(@"iron", RegexOptions.IgnoreCase), "iron_curtain" },
                { new Regex(@"(?<!neo).*jade", RegexOptions.IgnoreCase), "jade" },
                { new Regex(@"jim", RegexOptions.IgnoreCase), "jim_raynors_memory" },
                //{ new Regex(@"", RegexOptions.IgnoreCase), "jim_raynors_memory_j" },
                //{ new Regex(@"", RegexOptions.IgnoreCase), "jim_raynors_memory_j_v1.5" },
                { new Regex(@"judgment", RegexOptions.IgnoreCase), "judgment_day" },
                { new Regex(@"(?<!neo).*jungle", RegexOptions.IgnoreCase), "jungle_story" },
                { new Regex(@"katrina", RegexOptions.IgnoreCase), "katrina" },
                { new Regex(@"korhal", RegexOptions.IgnoreCase), "korhal_of_ceres" },
                { new Regex(@"mancha", RegexOptions.IgnoreCase), "la_mancha" },
                { new Regex(@"(?<!neo).*legacy", RegexOptions.IgnoreCase), "legacy_of_char" },
                { new Regex(@"loki", RegexOptions.IgnoreCase), "loki" },
                //{ new Regex(@"", RegexOptions.IgnoreCase), "loki_ii" },
                { new Regex(@"longinus", RegexOptions.IgnoreCase), "longinus" },
                //{ new Regex(@"", RegexOptions.IgnoreCase), "longinus_2" },
                { new Regex(@"temple", RegexOptions.IgnoreCase), "lost_temple" },
                //{ new Regex(@"", RegexOptions.IgnoreCase), "lost_temple_kpga" },
                { new Regex(@"luna", RegexOptions.IgnoreCase), "luna" },
                //{ new Regex(@"", RegexOptions.IgnoreCase), "luna_mbcgame" },
                //{ new Regex(@"", RegexOptions.IgnoreCase), "luna_the_final" },
                { new Regex(@"martian", RegexOptions.IgnoreCase), "martian_cross" },
                { new Regex(@"match", RegexOptions.IgnoreCase), "match_point" },
                { new Regex(@"medusa", RegexOptions.IgnoreCase), "medusa" },
                { new Regex(@"mercury.*(?!zero)", RegexOptions.IgnoreCase), "mercury" },
                { new Regex(@"mercury.*(?=zero)", RegexOptions.IgnoreCase), "mercury_zero" },
                { new Regex(@"cristo", RegexOptions.IgnoreCase), "monte_cristo" },
                { new Regex(@"hall.*(?!se)", RegexOptions.IgnoreCase), "monty_hall" },
                { new Regex(@"hall.*(?=se)", RegexOptions.IgnoreCase), "monty_hall_se" },
                { new Regex(@"glaive", RegexOptions.IgnoreCase), "moon_glaive" },
                { new Regex(@"multiverse", RegexOptions.IgnoreCase), "multiverse" },
                { new Regex(@"namja", RegexOptions.IgnoreCase), "namja_iyagi" },
                { new Regex(@"nemesis", RegexOptions.IgnoreCase), "nemesis" },
                { new Regex(@"(?<=neo).*arkanoid", RegexOptions.IgnoreCase), "neo_arkanoid" },
                { new Regex(@"(?<=neo).*aztec", RegexOptions.IgnoreCase), "neo_aztec" },
                { new Regex(@"(?<=neo).*arkanoid", RegexOptions.IgnoreCase), "neo_bifrost" },
                { new Regex(@"(?<=neo).*arkanoid", RegexOptions.IgnoreCase), "neo_blaze" },
                { new Regex(@"(?<=neo).*electric", RegexOptions.IgnoreCase), "neo_electric_circuit" },
                { new Regex(@"(?<=neo).*forbidden", RegexOptions.IgnoreCase), "neo_forbidden_zone" },
                { new Regex(@"(?<=neo).*forte", RegexOptions.IgnoreCase), "neo_forte" },
                { new Regex(@"(?<=neo).*ground", RegexOptions.IgnoreCase), "neo_ground_zero" },
                { new Regex(@"(?<=neo).*guillotine", RegexOptions.IgnoreCase), "neo_guillotine" },
                { new Regex(@"(?<=neo).*valhalla", RegexOptions.IgnoreCase), "neo_hall_of_valhalla" },
                { new Regex(@"(?<=neo).*harmony", RegexOptions.IgnoreCase), "neo_harmony" },
                { new Regex(@"(?<=neo).*jungle", RegexOptions.IgnoreCase), "neo_jungle_story" },
                { new Regex(@"(?<=neo).*legacy", RegexOptions.IgnoreCase), "neo_legacy_of_char" },
                { new Regex(@"(?<=neo).*requiem", RegexOptions.IgnoreCase), "neo_requiem" },
                { new Regex(@"(?<=neo).*vortex", RegexOptions.IgnoreCase), "neo_silent_vortex" },
                { new Regex(@"(?<=neo).*sylphid", RegexOptions.IgnoreCase), "neo_sylphid" },
                { new Regex(@"(?<=neo).*transistor", RegexOptions.IgnoreCase), "neo_transistor" },
                { new Regex(@"(?<=neo).*vertigo", RegexOptions.IgnoreCase), "neo_vertigo" },
                { new Regex(@"(?<=new).*bloody", RegexOptions.IgnoreCase), "new_bloody_ridge" },
                { new Regex(@"(?<=new).*heartbreak", RegexOptions.IgnoreCase), "new_heartbreak_ridge" },
                { new Regex(@"(?<=new).*sniper", RegexOptions.IgnoreCase), "new_sniper_ridge" },
                { new Regex(@"nostalgia", RegexOptions.IgnoreCase), "nostalgia" },
                { new Regex(@"(?<=odd).*eye", RegexOptions.IgnoreCase), "odd-eye" },
                //{ new Regex(@"", RegexOptions.IgnoreCase), "odd-eye_2" },
                //{ new Regex(@"", RegexOptions.IgnoreCase), "odd-eye_3" },
                { new Regex(@"odin", RegexOptions.IgnoreCase), "odin" },
                { new Regex(@"old.*plains", RegexOptions.IgnoreCase), "old_plains_to_hill" },
                { new Regex(@"othello", RegexOptions.IgnoreCase), "othello" },
                { new Regex(@"outlier", RegexOptions.IgnoreCase), "outlier" },
                { new Regex(@"outsider.*(?!se)", RegexOptions.IgnoreCase), "outsider" },
                { new Regex(@"outsider.*(?=se)", RegexOptions.IgnoreCase), "outsider_se" },
                { new Regex(@"overwatch", RegexOptions.IgnoreCase), "overwatch" },
                //{ new Regex(@"", RegexOptions.IgnoreCase), "overwatch_(asl_map)" },
                { new Regex(@"paradoxxx", RegexOptions.IgnoreCase), "paradoxxx" },
                //{ new Regex(@"", RegexOptions.IgnoreCase), "paradoxxx_2" },
                { new Regex(@"parallel", RegexOptions.IgnoreCase), "parallel_lines" },
                //{ new Regex(@"", RegexOptions.IgnoreCase), "parallel_lines_3" },
                { new Regex(@"paranoid", RegexOptions.IgnoreCase), "paranoid_android" },
                { new Regex(@"pathfinder", RegexOptions.IgnoreCase), "pathfinder" },
                { new Regex(@"(?<!sin).*peaks.*baekdu", RegexOptions.IgnoreCase), "peaks_of_baekdu" },
                { new Regex(@"pelennor", RegexOptions.IgnoreCase), "pelennor" },
                { new Regex(@"persona", RegexOptions.IgnoreCase), "persona" },
                { new Regex(@"(?<!sin).*pioneer", RegexOptions.IgnoreCase), "pioneer_period" },
                { new Regex(@"(?<!old).*plains.*hill", RegexOptions.IgnoreCase), "plains_to_hill" },
                //{ new Regex(@"", RegexOptions.IgnoreCase), "plains_to_hill_d" },
                { new Regex(@"plasma", RegexOptions.IgnoreCase), "plasma" },
                { new Regex(@"polaris", RegexOptions.IgnoreCase), "polaris_rhapsody" },
                { new Regex(@"python", RegexOptions.IgnoreCase), "python" },
                { new Regex(@"r.*point", RegexOptions.IgnoreCase), "r-point" },
                { new Regex(@"ragnarok", RegexOptions.IgnoreCase), "ragnarok" },
                { new Regex(@"raid", RegexOptions.IgnoreCase), "raid_assault" },
                //{ new Regex(@"", RegexOptions.IgnoreCase), "raid_assault_2" },
                { new Regex(@"(?<!neo).*requiem", RegexOptions.IgnoreCase), "requiem" },
                { new Regex(@"king", RegexOptions.IgnoreCase), "return_of_the_king" },
                { new Regex(@"reverse", RegexOptions.IgnoreCase), "reverse_temple" },
                { new Regex(@"valkyries", RegexOptions.IgnoreCase), "ride_of_valkyries" },
                { new Regex(@"rivalry", RegexOptions.IgnoreCase), "rivalry" },
                { new Regex(@"river", RegexOptions.IgnoreCase), "river_of_flames" },
                { new Regex(@"roadkill", RegexOptions.IgnoreCase), "roadkill" },
                { new Regex(@"roadrunner", RegexOptions.IgnoreCase), "roadrunner" },
                { new Regex(@"(?<!gold).*rush", RegexOptions.IgnoreCase), "rush_hour" },
                //{ new Regex(@"", RegexOptions.IgnoreCase), "rush_hour_2" },
                //{ new Regex(@"", RegexOptions.IgnoreCase), "rush_hour_3" },
                //{ new Regex(@"", RegexOptions.IgnoreCase), "rush_hour_iii" },
                { new Regex(@"seongangil", RegexOptions.IgnoreCase), "seongangil" },
                { new Regex(@"shin.*baekdu", RegexOptions.IgnoreCase), "shin_peaks_of_baekdu" },
                { new Regex(@"showdown", RegexOptions.IgnoreCase), "showdown" },
                { new Regex(@"(?<!neo).*vortex", RegexOptions.IgnoreCase), "silent_vortex" },
                { new Regex(@"sin.*815", RegexOptions.IgnoreCase), "sin_815" },
                { new Regex(@"chupung", RegexOptions.IgnoreCase), "sin_chupung-ryeong" },
                { new Regex(@"(?<=sin).*gaema", RegexOptions.IgnoreCase), "sin_gaema_gowon" },
                { new Regex(@"(?<=sin).*baekdu", RegexOptions.IgnoreCase), "sin_peaks_of_baekdu" },
                { new Regex(@"sin.*pioneer", RegexOptions.IgnoreCase), "sin_pioneer_period" },
                { new Regex(@"(?<!new).*sniper", RegexOptions.IgnoreCase), "sniper_ridge" },
                { new Regex(@"snowbound", RegexOptions.IgnoreCase), "snowbound" },
                { new Regex(@"space", RegexOptions.IgnoreCase), "space_odyssey" },
                { new Regex(@"sparkle", RegexOptions.IgnoreCase), "sparkle" },
                { new Regex(@"(?<!neo).*sylphid", RegexOptions.IgnoreCase), "sylphid" },
                { new Regex(@"symmetry", RegexOptions.IgnoreCase), "symmetry_of_psy" },
                { new Regex(@"taebaek", RegexOptions.IgnoreCase), "taebaek_mountains" },
                { new Regex(@"tau", RegexOptions.IgnoreCase), "tau_cross" },
                { new Regex(@"tears", RegexOptions.IgnoreCase), "tears_of_the_moon" },
                { new Regex(@"(?<!odd).*eye", RegexOptions.IgnoreCase), "the_eye" },
                { new Regex(@"hunters", RegexOptions.IgnoreCase), "the_hunters" },
                { new Regex(@"huntress", RegexOptions.IgnoreCase), "the_huntress" },
                { new Regex(@"third", RegexOptions.IgnoreCase), "third_world" },
                { new Regex(@"tiamat", RegexOptions.IgnoreCase), "tiamat" },
                { new Regex(@"tornado", RegexOptions.IgnoreCase), "tornado" },
                { new Regex(@"(?<!neo).*sylphid", RegexOptions.IgnoreCase), "transistor" },
                { new Regex(@"triatholn", RegexOptions.IgnoreCase), "triathlon" },
                { new Regex(@"tripod", RegexOptions.IgnoreCase), "tripod" },
                { new Regex(@"troy", RegexOptions.IgnoreCase), "troy" },
                { new Regex(@"tucson", RegexOptions.IgnoreCase), "tucson" },
                { new Regex(@"boat", RegexOptions.IgnoreCase), "u-boat" },
                //{ new Regex(@"", RegexOptions.IgnoreCase), "u-boat_2004" },
                { new Regex(@"ultimatum", RegexOptions.IgnoreCase), "ultimatum" },
                { new Regex(@"crater", RegexOptions.IgnoreCase), "un_goro_crater" },
                { new Regex(@"usan", RegexOptions.IgnoreCase), "usan_nation" },
                { new Regex(@"valley", RegexOptions.IgnoreCase), "valley_of_wind" },
                { new Regex(@"vampire", RegexOptions.IgnoreCase), "vampire" },
                { new Regex(@"vertigo", RegexOptions.IgnoreCase), "vertigo_plus" },
                { new Regex(@"whiteout", RegexOptions.IgnoreCase), "whiteout" },
                { new Regex(@"wishbone", RegexOptions.IgnoreCase), "wishbone" },
                { new Regex(@"wuthering", RegexOptions.IgnoreCase), "wuthering_heights" },
                { new Regex(@"xeno", RegexOptions.IgnoreCase), "xeno_sky" },
                { new Regex(@"zodiac", RegexOptions.IgnoreCase), "zodiac" }
            };

            _mapNamesToFileNamesLookup = mapRegexToFileNameDictionary.ToLookup(kvp => kvp.Value.First(), kvp => Tuple.Create(kvp.Key, kvp.Value));
            _mapNames = new HashSet<string>(mapRegexToFileNameDictionary.Values);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var mapName = value as string;
            if (string.IsNullOrWhiteSpace(mapName))
                return string.Empty;

            mapName = FileHandler.RemoveInvalidChars(mapName);
            mapName = mapName.Trim(' ');
            mapName = mapName.ToLower();

            var regexes = _mapNamesToFileNamesLookup.FirstOrDefault(l => l.Key == mapName.First());
            var imagePath = string.Empty;

            if (regexes != null)
            {
                foreach (var regex in regexes)
                {
                    if (regex.Item1.IsMatch(mapName))
                    {
                        var filePath = System.IO.Path.Combine(_assemblyPath, $@"images\maps\{regex.Item2}.jpg");
                        if (!System.IO.File.Exists(filePath))
                            break;

                        imagePath = $"/images/maps/{regex.Item2}.jpg";
                    }
                }
            }

            if (imagePath == string.Empty)
            {
                var matchingMap = _mapNames.Where(m => mapName.Contains(m.Replace('-', ' ').Replace('_', ' '))).FirstOrDefault();
                if (matchingMap != null)
                {
                    var filePath = System.IO.Path.Combine(_assemblyPath, $@"images\maps\{matchingMap}.jpg");
                    if (System.IO.File.Exists(filePath))
                        imagePath = $"/images/maps/{matchingMap}.jpg";
                }
            }

            return "pack://siteoforigin:,,," + (imagePath == string.Empty ? "/images/maps/placeholder.jpg" : imagePath);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
