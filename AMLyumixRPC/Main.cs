using Microsoft.VisualBasic;
using System.Diagnostics;
using System.Globalization;

namespace AMLyumixRPC
{
    public class Main
    {
        private AppleMusicClientScraper amScraper;
        private AppleMusicDiscordClient discordClient;
        private AppleMusicLastFmScrobbler lastFmScrobblerClient;
        private AppleMusicListenBrainzScrobbler listenBrainzScrobblerClient;
        private Logger? logger;
        private static bool isRunning = true;

        public static string[] validChangeRegion = Constants.ValidAppleMusicRegions;

        public static void ChangeRegion(string region)
        {
            if (Constants.ValidAppleMusicRegions.Contains(region))
            {
                AMLyumixRPC.Properties.Settings.Default.AppleMusicRegion = region;
            }

        }

        public LastFmCredentials lastFmCredentials
        {
            get
            {
                var creds = new LastFmCredentials();
                creds.apiKey = AMLyumixRPC.Properties.Settings.Default.LastfmAPIKey;
                creds.apiSecret = AMLyumixRPC.Properties.Settings.Default.LastfmSecret;
                creds.username = AMLyumixRPC.Properties.Settings.Default.LastfmUsername;
                return creds;
            }
        }

        public ListenBrainzCredentials listenBrainzCredentials
        {
            get
            {
                var creds = new ListenBrainzCredentials();
                creds.userToken = AMLyumixRPC.Properties.Settings.Default.ListenBrainzUserToken;
                return creds;
            }
        }

        public void App()
        {
            // make logger
            try
            {
                logger = new Logger();
                logger.Log("Application started");
            }
            catch
            {
                logger = null;
            }

            AMLyumixRPC.Properties.Settings.Default.AppleMusicRegion = "us";
            AMLyumixRPC.Properties.Settings.Default.ShowRPWhenMusicPaused = true;
            AMLyumixRPC.Properties.Settings.Default.EnableDiscordRP = true;
            AMLyumixRPC.Properties.Settings.Default.ShowAppleMusicIcon = true;
            AMLyumixRPC.Properties.Settings.Default.EnableRPCoverImages = true;
            AMLyumixRPC.Properties.Settings.Default.Save();
            logger?.Log($"Using region {AMLyumixRPC.Properties.Settings.Default.AppleMusicRegion}");

            // start Discord RPC
            var subtitleOptions = (AppleMusicDiscordClient.RPSubtitleDisplayOptions)AMLyumixRPC.Properties.Settings.Default.RPSubtitleChoice;
            var classicalComposerAsArtist = AMLyumixRPC.Properties.Settings.Default.ClassicalComposerAsArtist;
            discordClient = new(Constants.DiscordClientID, enabled: false, subtitleOptions: subtitleOptions, logger: logger);

            // start Last.FM scrobbler
            var amRegion = AMLyumixRPC.Properties.Settings.Default.AppleMusicRegion;
            lastFmScrobblerClient = new AppleMusicLastFmScrobbler(region: amRegion, logger: logger);
            _ = lastFmScrobblerClient.init(lastFmCredentials);

            // start ListenBrainz scrobbler
            listenBrainzScrobblerClient = new AppleMusicListenBrainzScrobbler(region: amRegion, logger: logger);
            _ = listenBrainzScrobblerClient.init(listenBrainzCredentials);

            // start Apple Music scraper
            var lastFMApiKey = AMLyumixRPC.Properties.Settings.Default.LastfmAPIKey;

            if (lastFMApiKey == null || lastFMApiKey == "")
            {
                logger?.Log("No Last.FM API key found");
            }

            amScraper = new(isRunning, lastFMApiKey, Constants.RefreshPeriod, classicalComposerAsArtist, AMLyumixRPC.Properties.Settings.Default.AppleMusicRegion, (newInfo) => {

                // don't update scraper if Apple Music is paused or not open
                if (newInfo != null && (AMLyumixRPC.Properties.Settings.Default.ShowRPWhenMusicPaused || !newInfo.IsPaused))
                {

                    // Discord RP update
                    if (AMLyumixRPC.Properties.Settings.Default.EnableDiscordRP)
                    {
                        discordClient.Enable();
                        discordClient.SetPresence(newInfo, AMLyumixRPC.Properties.Settings.Default.ShowAppleMusicIcon, AMLyumixRPC.Properties.Settings.Default.EnableRPCoverImages);
                    }
                    else
                    {
                        discordClient.Disable();
                    }

                    // Last.FM scrobble update
                    if (AMLyumixRPC.Properties.Settings.Default.LastfmEnable)
                    {
                        lastFmScrobblerClient.Scrobbleit(newInfo);
                    }

                    // ListenBrainz scrobble update
                    if (AMLyumixRPC.Properties.Settings.Default.ListenBrainzEnable)
                    {
                        listenBrainzScrobblerClient.Scrobbleit(newInfo);
                    }
                }
                else
                {
                    discordClient.Disable();
                }
            }, logger);
        }

        private void Application_Exit(object sender, EventArgs e)
        {
            discordClient.Disable();
            logger?.Log("Application finished");
        }
        public void Stop()
        {
            logger?.Log("Application finished");
            discordClient.Disable();
            AppleMusicClientScraper.timer.Stop();
            isRunning = false;
        }

        internal void UpdateRPSubtitleDisplay(AppleMusicDiscordClient.RPSubtitleDisplayOptions newVal)
        {
            discordClient.subtitleOptions = newVal;
        }

        internal async Task<bool> UpdateLastfmCreds()
        {
            return await lastFmScrobblerClient.UpdateCredsAsync(lastFmCredentials);
        }

        internal async Task<bool> UpdateListenBrainzCreds()
        {
            return await listenBrainzScrobblerClient.UpdateCredsAsync(listenBrainzCredentials);
        }

        internal void UpdateRegion()
        {
            var region = AMLyumixRPC.Properties.Settings.Default.AppleMusicRegion;
            logger?.Log($"Changed region to {region}");
            amScraper.ChangeRegion(region);
        }

        internal void UpdateScraperPreferences(bool composerAsArtist)
        {
            amScraper.composerAsArtist = composerAsArtist;
        }

        private static void SaveSettings()
        {
            Properties.Settings.Default.Save();
        }
    }
}
