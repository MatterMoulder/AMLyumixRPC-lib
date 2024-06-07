﻿using System;
using System.Text;
using System.Linq;
using AMLyumixRPC;
using DiscordRPC;
using System.Net.Http;
using System.Threading.Tasks;

internal class AppleMusicDiscordClient {
    public enum RPSubtitleDisplayOptions {
        ArtistAlbum = 0, ArtistOnly = 1, AlbumOnly = 2
    }

    private static readonly HttpClient webClient = new HttpClient();

    public static RPSubtitleDisplayOptions SubtitleOptionFromIndex(int i) {
        return (RPSubtitleDisplayOptions)i;
    }

    public RPSubtitleDisplayOptions subtitleOptions;
    DiscordRpcClient? client;
    string discordClientID;
    bool enabled = false;
    Logger? logger;
    int maxStringLength = 127;

    public AppleMusicDiscordClient(string discordClientID, bool enabled = true, RPSubtitleDisplayOptions subtitleOptions = RPSubtitleDisplayOptions.ArtistAlbum, Logger? logger = null) {
        this.discordClientID = discordClientID;
        this.enabled = enabled;
        this.subtitleOptions = subtitleOptions;
        this.logger = logger;

        if (enabled) {
            InitClient();
        }
    }

    private string TrimString(string str) {
        return str.Length > maxStringLength ? str.Substring(0, maxStringLength - 1) : str;
    }

    private string GetTrimmedArtistList(AppleMusicInfo amInfo) {
        if (amInfo.ArtistList?.Count > 1) {
            return $"{amInfo.ArtistList.First()}, Various Artists";
        } else {
            return amInfo.SongArtist; // TODO fix this so it always prevents string length violations
        }
    }

    public static async Task<string> ConvertLink(string originalUrl)
    {
        return $"https://songwhip.com/{originalUrl}";
        // Example for Songwhip
        var response = await webClient.GetAsync($"https://songwhip.com/{originalUrl}");
        
        if (response.IsSuccessStatusCode)
        {
            // Return the final redirected URL
            return response.RequestMessage.RequestUri.ToString();
        }
        else
        {
            // Handle the error response
            return originalUrl;
        }
    }

    public async void SetPresence(AppleMusicInfo amInfo, bool showSmallImage, bool showBigImage) {
        if (!enabled) {
            return;
        }

        var songName = TrimString(amInfo.SongName);
        var songSubtitle = amInfo.SongSubTitle.Length > maxStringLength ? amInfo.SongSubTitle.Replace(amInfo.SongArtist, GetTrimmedArtistList(amInfo)) : amInfo.SongSubTitle;
        var songArtist = amInfo.SongArtist.Length > maxStringLength ? GetTrimmedArtistList(amInfo) : amInfo.SongArtist;
        var songAlbum = TrimString(amInfo.SongAlbum);

        // hack to show 1-character song names
        while (songName.Length < 2) {
            songName += "\u0000";
        }

        // pick the subtitle format to show
        var subtitle = "";
        switch (subtitleOptions) {
            case RPSubtitleDisplayOptions.ArtistAlbum:
                subtitle = songSubtitle;
                break;
            case RPSubtitleDisplayOptions.ArtistOnly:
                subtitle = songArtist;
                break;
            case RPSubtitleDisplayOptions.AlbumOnly:
                subtitle = songAlbum;
                break;
        }
        if (ASCIIEncoding.Unicode.GetByteCount(subtitle) > 128) {
            // TODO fix this to account for multibyte unicode characters
            subtitle = subtitle.Substring(0, 60) + "...";
        }
        try {
            var rp = new RichPresence() {
                Details = songName,
                State = subtitle,
                Assets = new Assets() {
                    LargeImageKey = (showBigImage ? amInfo.CoverArtUrl : null) ?? Constants.DiscordAppleMusicImageKey,
                    LargeImageText = songAlbum
                }
            };
            
            if (amInfo.SongUrl != null) {
                string link = await ConvertLink(amInfo.SongUrl);
                rp.Buttons = [new() {
                    Label = "Listen on Any Services", 
                    Url = link
                }];
            }

            if (amInfo.IsPaused) {
                rp.Assets.SmallImageKey = Constants.DiscordAppleMusicPauseImageKey;
                rp.Assets.SmallImageText = "Paused";
            } else if (showSmallImage) {
                if (amInfo.CoverArtUrl != null) {
                    rp.Assets.SmallImageKey = Constants.DiscordAppleMusicPlayImageKey;
                    rp.Assets.SmallImageText = "Playing";
                }
            }

            // add timestamps, if they're there
            if (!amInfo.IsPaused && amInfo.PlaybackStart != null && amInfo.PlaybackEnd != null) {
                rp = rp.WithTimestamps(new Timestamps((DateTime)amInfo.PlaybackStart, (DateTime)amInfo.PlaybackEnd));
            }

            if (client == null) {
                logger?.Log($"Tried to set Discord RP, but no client");
            } else {
                client.SetPresence(rp);
                logger?.Log($"Set Discord RP to:\n{amInfo}");
            }

        } catch (Exception ex) {
            logger?.Log($"Couldn't set Discord RP:\n{ex}");
        }

    }
    public void Enable() {
        if (enabled) {
            return;
        }
        enabled = true;
        InitClient();
    }
    public void Disable() {
        if (!enabled) {
            return;
        }
        enabled = false;
        client?.ClearPresence();
        DeinitClient();
    }
    private void InitClient() {
        client = new DiscordRpcClient(discordClientID, logger: logger);
        client.Initialize();
    }
    private void DeinitClient() {
        if (client != null) {
            client.Deinitialize();
            client.Dispose();
            client = null;
        }
    }
}
