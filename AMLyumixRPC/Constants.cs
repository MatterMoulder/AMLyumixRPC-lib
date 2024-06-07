using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace AMLyumixRPC
{
    internal static class Constants {
        public static string ProgramVersionBase {
            get {
                try {
                    var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                    if (exePath == null) {
                        return "";
                    }
                    FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(exePath);
                    return $"v{fvi.FileVersion}";
                } catch (Exception ex) {
                    new Logger().Log($"Error getting version string: {ex}");
                    return "";
                }
            }
        } 
#if DEBUG
        public static string  ProgramVersion = $"{ProgramVersionBase}";
#else
        public static string  ProgramVersion = ProgramVersionBase;
#endif                        
        public static int    MaxLogFiles                    = 10;
        public static int    RefreshPeriod                  = 3; // seconds
        public static int    NumFailedSearchesBeforeAbandon = 3;
        public static string AppDataFolderName              = "LyumixRPC";
        public static string DiscordClientID                = "1155375785914408991";
        public static string DiscordAppleMusicImageKey      = "nonart";
        public static string DiscordAppleMusicPlayImageKey  = "playing";
        public static string DiscordAppleMusicPauseImageKey = "paused";
        public static string LastFMCredentialTargetName     = "Last FM Password";
        public static int    LastFMTimeBeforeScrobbling     = 20; // seconds
        public static string GithubReleasesApiUrl           = "";
        public static string GithubReleasesUrl              = "";
        public static string DefaultAppleMusicRegion        = "us";
        public static string WindowsStartupFolder => Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        public static string WindowsAppDataFolder => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        public static string AppDataFolder => Path.Combine(WindowsAppDataFolder, AppDataFolderName);
        public static string AppShortcutPath => Path.Join(WindowsStartupFolder, "LyumixRPC.lnk");
        public static string? ExePath => Process.GetCurrentProcess().MainModule?.FileName;

        public static readonly HttpClient HttpClient = new();

        public static string[] ValidAppleMusicRegions = [
            "ae","ag","ai","am","ao","ar","at","au",
            "az","ba","bb","be","bg","bh","bj","bm",
            "bo","br","bs","bt","bw","by","bz","ca",
            "cd","cg","ch","ci","cl","cm","cn","co",
            "cr","cv","cy","cz","de","dk","dm","do",
            "dz","ec","ee","eg","es","fi","fj","fm",
            "fr","ga","gb","gd","ge","gh","gm","gr",
            "gt","gw","gy","hk","hn","hr","hu","id",
            "ie","il","in","iq","is","it","jm","jo",
            "jp","ke","kg","kh","kn","kr","kw","ky",
            "kz","la","lb","lc","lk","lr","lt","lu",
            "lv","ly","ma","md","me","mg","mk","ml",
            "mm","mn","mo","mr","ms","mt","mu","mv",
            "mw","mx","my","mz","na","ne","ng","ni",
            "nl","no","np","nz","om","pa","pe","pg",
            "ph","pl","pt","py","qa","ro","rs","ru",
            "rw","sa","sb","sc","se","sg","si","sk",
            "sl","sn","sr","sv","sz","tc","td","th",
            "tj","tm","tn","to","tr","tt","tw","tz",
            "ua","ug","us","uy","uz","vc","ve","vg",
            "vn","vu","xk","ye","za","zm","zw"
        ];
    }
}