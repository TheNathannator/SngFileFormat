using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SngLib;
using SongLib;
using static SngCli.JpegEncoding;

namespace SngCli
{
    public static class SngEncode
    {
        private static List<string> SearchForFolders(string rootFolder)
        {
            List<string> validSubfolders = new List<string>();
            string[] subfolders = Directory.GetDirectories(rootFolder);

            foreach (string subfolder in subfolders)
            {
                if (IsValidSubfolder(subfolder))
                {
                    validSubfolders.Add(subfolder);
                    continue;
                }

                validSubfolders.AddRange(SearchForFolders(subfolder));
            }

            return validSubfolders;
        }

        private static bool IsValidSubfolder(string subfolder)
        {
            string[] files = Directory.GetFiles(subfolder);
            bool hasMidiOrChart = files.Any(f => f.EndsWith(".mid") || f.EndsWith(".chart"));
            bool hasAudioFile = files.Any(f => f.EndsWith(".wav") || f.EndsWith(".ogg") || f.EndsWith(".opus") || f.EndsWith(".mp3"));
            bool hasSongIni = files.Any(f => f.EndsWith("song.ini"));

            return hasMidiOrChart && hasAudioFile && hasSongIni;
        }

        private static bool ParseMetadata(SngFile sngFile, string iniPath)
        {
            IniFile iniFile = new IniFile();
            iniFile.Load(iniPath);
            if (iniFile.IsSection("song"))
            {
                var name = iniFile.GetString("song", "name", "");
                var artist = iniFile.GetString("song", "artist", "");
                var album = iniFile.GetString("song", "album", "");
                var genre = iniFile.GetString("song", "genre", "");
                var year = iniFile.GetString("song", "year", "");
                var bandDiff = iniFile.GetInt("song", "diff_band", -1);
                var guitarDiff = iniFile.GetInt("song", "diff_guitar", -1);
                var rhythmDiff = iniFile.GetInt("song", "diff_rhythm", -1);
                var guitarCoopDiff = iniFile.GetInt("song", "diff_guitar_coop", -1);
                var bassDiff = iniFile.GetInt("song", "diff_bass", -1);
                var drumsDiff = iniFile.GetInt("song", "diff_drums", -1);
                var proDrumsDiff = iniFile.GetInt("song", "diff_drums_real", -1);
                var keysDiff = iniFile.GetInt("song", "diff_keys", -1);
                var gHLGuitarDiff = iniFile.GetInt("song", "diff_guitarghl", -1);
                var gHLBassDiff = iniFile.GetInt("song", "diff_bassghl", -1);
                var gHLGuitarCoopDiff = iniFile.GetInt("song", "diff_guitar_coop_ghl", -1);
                var gHLRhythmDiff = iniFile.GetInt("song", "diff_rhythm_ghl", -1);
                var previewStart = iniFile.GetInt("song", "preview_start_time", -1);
                var iconName = iniFile.GetString("song", "icon", "").ToLowerInvariant();
                var playlistTrack = iniFile.GetInt("song", "playlist_track", 16000);
                var modchart = iniFile.GetBool("song", "modchart", false);
                var songLength = iniFile.GetInt("song", "song_length", 0);
                var forceProDrums = iniFile.GetBool("song", "pro_drums", false);
                var forceFiveLane = iniFile.GetBool("song", "five_lane_drums", false);
                var topLevelPlaylist = iniFile.GetString("song", "playlist", "").ToLowerInvariant();
                var subPlaylist = iniFile.GetString("song", "sub_playlist", "").ToLowerInvariant();


                int albumTrack;
                if (iniFile.IsKey("song", "album_track"))
                    albumTrack = iniFile.GetInt("song", "album_track", 16000);
                else
                    albumTrack = iniFile.GetInt("song", "track", 16000);

                var charter = iniFile.GetString("song", iniFile.IsKey("song", "charter") ? "charter" : "frets", "");

                var customHOPO = iniFile.GetInt("song", "hopo_frequency", 0);
                var isEighthHOPO = iniFile.GetBool("song", "eighthnote_hopo", false);
                var multiplierNote = iniFile.GetInt("song", "multiplier_note", 0);
                var offset = iniFile.GetInt("song", "delay", 0);
                var videoStart = iniFile.GetInt("song", "video_start_time", 0);
                var endEventsEnabled = iniFile.GetBool("song", "end_events", true);

                // Save metadata to sng file
                sngFile.SetString("name", name);
                sngFile.SetString("artist", artist);
                sngFile.SetString("album", album);
                sngFile.SetString("genre", genre);
                sngFile.SetString("year", year);
                sngFile.SetInt("diff_band", bandDiff);
                sngFile.SetInt("diff_guitar", guitarDiff);
                sngFile.SetInt("diff_rhythm", rhythmDiff);
                sngFile.SetInt("diff_guitar_coop", guitarCoopDiff);
                sngFile.SetInt("diff_bass", bassDiff);
                sngFile.SetInt("diff_drums", drumsDiff);
                sngFile.SetInt("diff_drums_real", proDrumsDiff);
                sngFile.SetInt("diff_keys", keysDiff);
                sngFile.SetInt("diff_guitarghl", gHLGuitarDiff);
                sngFile.SetInt("diff_bassghl", gHLBassDiff);
                sngFile.SetInt("diff_guitar_coop_ghl", gHLGuitarCoopDiff);
                sngFile.SetInt("diff_rhythm_ghl", gHLRhythmDiff);
                sngFile.SetInt("preview_start_time", previewStart);
                sngFile.SetString("icon", iconName);
                sngFile.SetInt("playlist_track", playlistTrack);
                sngFile.SetBool("modchart", modchart);
                sngFile.SetInt("song_length", songLength);
                sngFile.SetBool("pro_drums", forceProDrums);
                sngFile.SetBool("five_lane_drums", forceFiveLane);
                sngFile.SetString("playlist", topLevelPlaylist);
                sngFile.SetString("sub_playlist", subPlaylist);
                sngFile.SetInt("album_track", albumTrack);
                sngFile.SetString("album_track", charter);
                sngFile.SetInt("hopo_frequency", customHOPO);
                sngFile.SetBool("eighthnote_hopo", isEighthHOPO);
                sngFile.SetInt("multiplier_note", multiplierNote);
                sngFile.SetInt("delay", offset);
                sngFile.SetInt("video_start_time", videoStart);
                sngFile.SetBool("end_events", endEventsEnabled);

                // TODO - should we automatically parse any ch unrecognized tags and pass them in as-is?
                return true;
            }
            else
            {
                return false;
            }
        }

        private static readonly string videoPattern = @"(?i).*\.(mp4|avi|webm|vp8|ogv|mpeg)$";
        private static Regex videoRegex = new Regex(videoPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly string imagePattern = @"(?i).*\.(png|jpg|jpeg)$";
        private static Regex imageRegex = new Regex(imagePattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly string audioPattern = @"(?i).*\.(wav|opus|ogg|mp3)$";
        private static Regex audioRegex = new Regex(audioPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static async Task EncodeSong(string songFolder)
        {
            var conf = SngEncodingConfig.Instance;

            SngFile sngFile = new SngFile();
            Random.Shared.NextBytes(sngFile.Seed);

            (string name, byte[]? data) fileData = ("", null);

            var fileList = Directory.GetFiles(songFolder);

            foreach (var file in fileList)
            {
                var fileName = Path.GetFileName(file);
                if (audioRegex.IsMatch(file))
                {
                    if (conf.EncodeOpus && !fileName.EndsWith(".opus", StringComparison.OrdinalIgnoreCase))
                    {
                        fileData = await AudioEncoding.ToOpus(file, conf.OpusBitrate);
                    }
                    else
                    {
                        fileData = (Path.GetFileName(file), await File.ReadAllBytesAsync(file));
                    }
                }
                else if (string.Equals(fileName, "song.ini", StringComparison.OrdinalIgnoreCase))
                {
                    if (!ParseMetadata(sngFile, file))
                    {
                        Console.WriteLine($"Error: Failed to parse metadata for chart {songFolder}");
                        return;
                    }
                    continue;
                }
                else if (string.Equals(fileName, "notes.mid", StringComparison.OrdinalIgnoreCase))
                {
                    fileData = ("notes.mid", await File.ReadAllBytesAsync(file));
                }
                else if (string.Equals(fileName, "notes.chart", StringComparison.OrdinalIgnoreCase))
                {
                    fileData = ("notes.chart", await File.ReadAllBytesAsync(file));
                }
                else if (imageRegex.IsMatch(file))
                {
                    if (fileName.StartsWith("album", StringComparison.OrdinalIgnoreCase))
                    {
                        if (conf.EncodeJpeg)
                        {
                            fileData = await JpegEncoding.EncodeImageToJpeg(file, conf.JpegQuality, conf.ForceSize);
                        }
                        else
                        {

                            fileData = ("notes.chart", await File.ReadAllBytesAsync(file));
                        }
                    }
                    else if (fileName.StartsWith("background", StringComparison.OrdinalIgnoreCase))
                    {
                        if (conf.EncodeJpeg)
                        {
                            fileData = await JpegEncoding.EncodeImageToJpeg(file, conf.JpegQuality, false, SizeTiers.None);
                        }
                        else
                        {

                            fileData = (fileName, await File.ReadAllBytesAsync(file));
                        }
                    }
                    else if (fileName.StartsWith("highway", StringComparison.OrdinalIgnoreCase))
                    {
                        if (conf.EncodeJpeg)
                        {
                            fileData = await JpegEncoding.EncodeImageToJpeg(file, conf.JpegQuality, false, SizeTiers.None);
                        }
                        else
                        {
                            fileData = (fileName, await File.ReadAllBytesAsync(file));
                        }
                    }
                }
                else if (!conf.ExcludeVideo && videoRegex.IsMatch(file) && fileName.StartsWith("video", StringComparison.OrdinalIgnoreCase))
                {
                    fileData = (fileName, await File.ReadAllBytesAsync(file));
                }

                if (fileData.data != null)
                {
                    sngFile.AddFile(fileData.name, new SngFile.FileData { Masked = true, Contents = fileData.data });
                }
            }
            var saveFile = $"{Path.GetFileName(songFolder)}.sng";
            Console.WriteLine($"Saving file: {saveFile}");
            SngSerializer.SaveSngFile(sngFile, Path.Combine(conf.OutputPath!, saveFile));
        }


        public static async Task ProcessSongs()
        {
            var conf = SngEncodingConfig.Instance;

            Console.WriteLine("SngCli scanning song folders");

            List<string> songFolders = SearchForFolders(conf.InputPath!);
            if (conf.NoThreads)
            {
                foreach (var songFolder in songFolders)
                {
                    await EncodeSong(songFolder);
                }
            }
            else
            {
                await Parallel.ForEachAsync(songFolders, async (songFolder, token) =>
                {
                    await EncodeSong(songFolder);
                });
            }
        }
    }
}