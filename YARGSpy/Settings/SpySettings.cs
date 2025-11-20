using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using YARG;
using YARG.Core;
using YARG.Core.Game;
using YARG.Core.Input;
using YARG.Core.Logging;
using YARG.Core.Replays;
using YARG.Menu.Data;
using YARG.Menu.Dialogs;
using YARG.Menu.Persistent;
using YARG.Menu.Settings;
using YARG.Player;
using YARG.Scores;
using YARG.Settings;
using YARG.Settings.Metadata;
using YARG.Settings.Types;
using YARGSpy.Helpers;
using YARGSpy.Settings.CustomTypes;

namespace YARGSpy.Settings;

public class SpySettings
{
    public static SpySettings instance;
    public static JObject user;

    public static MetadataTab SpyTab = new("YARGSpy");

    public StringSetting Username { get; } = new("");
    public PasswordSetting Password { get; } = new("");
    public StringSetting Token { get; } = new(""); // Shouldn't actually create a settings option for this. Stays hidden in settings
    public ToggleSetting UploadScores { get; } = new(true);
    public ToggleSetting ShowBoard { get; } = new(true);

    public async Task LoginToYARGSpy()
    {
        Dictionary<string, string> login = new()
            {
                { "username", Username.Value },
                { "password", PasswordSettingVisual.value }
            };

        UnityWebRequest req = await APIHelper.Post("/user/login", JsonConvert.SerializeObject(login));

        if (req.result != UnityWebRequest.Result.Success)
        {
            Plugin.Logger.LogError(req.error);
        }
        else
        {
            if (req.responseCode != 200)
            {
                Plugin.Logger.LogError(req.downloadHandler.text);
                return;
            }
            JObject response = JObject.Parse(req.downloadHandler.text);
            Token.Value = response["token"]!.ToString();
            SettingsManager.SaveSettings();
            APIHelper.GetUser();
        }
    }

    public void LogoutYARGSpy()
    {
        Token.Value = "";
        user = null;
        SettingsManager.SaveSettings();
        BuildLoggedOutSettings();
    }

    private static byte[] HexToBytes(string hex)
    {
        if (hex.Length % 2 != 0)
            throw new ArgumentException("Hex string must have an even length.");

        int length = hex.Length / 2;
        byte[] bytes = new byte[length];

        for (int i = 0; i < length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }

        return bytes;
    }

    public MessageDialog ShowScoreDialog(Action Yes, Action YesAll, Action No, Action Cancel)
    {
        if (DialogManager.Instance.IsDialogShowing)
        {
            throw new InvalidOperationException("A dialog already exists! Clear the previous dialog before showing a new one.");
        }

        Dialog val = (Dialog)(DialogManager.Instance._currentDialog = UnityEngine.Object.Instantiate(DialogManager.Instance._messagePrefab, DialogManager.Instance._dialogContainer));
        val.ClearButtons();
        val.AddDialogButton("Menu.Common.Yes", MenuData.Colors.ConfirmButton, () => { DialogManager.Instance.ClearDialog(); Yes(); });
        val.AddDialogButton("Menu.Common.YesAll", MenuData.Colors.ConfirmButton, () => { DialogManager.Instance.ClearDialog(); YesAll(); });
        val.AddDialogButton("Menu.Common.No", MenuData.Colors.CancelButton, () => { DialogManager.Instance.ClearDialog(); No(); });
        val.AddDialogButton("Menu.Common.Cancel", MenuData.Colors.CancelButton, () => { DialogManager.Instance.ClearDialog(); Cancel(); });
        return val as MessageDialog;
    }

    public async void DownloadScores()
    {

        List<YargPlayer> players = PlayerContainer._players;
        if (players.Count > 1)
        {
            DialogManager.Instance.ShowMessage("Too many active profiles", "As of now, only singleplayer scores can be downloaded from YARGSpy. Please make sure there is only 1 profile enabled to download scores to.");
            return;
        }
        if (players.Count <= 0)
        {
            DialogManager.Instance.ShowMessage("No active profiles", "Please active 1 profile to download your scores to.");
            return;
        }

        YargPlayer player = players[0];
        var profile = player.Profile;

        var scoresReq = await APIHelper.Get($"/user/scores?id={user["_id"]}&page=1&limit=1000");
        if (scoresReq.result != UnityWebRequest.Result.Success)
        {
            ToastManager.ToastError("Download failed: HTTP Error\n" + scoresReq.error);
            return;
        }
        JObject scoresResult = JObject.Parse(scoresReq.downloadHandler.text);
        JArray scores = (JArray)scoresResult["entries"];

        bool confirmAll = false;
        foreach (JObject score in scores)
        {
            if (float.Parse(score["songSpeed"]!.ToString()) < 1)
                continue;

            var playerEntries = new List<PlayerScoreRecord>();

            Guid engineID;
            if (int.Parse(score["engine"]!.ToString()) == 0)
                engineID = EnginePreset.Default.Id;
            else if (int.Parse(score["engine"]!.ToString()) == 1)
                engineID = EnginePreset.Casual.Id;
            else if (int.Parse(score["engine"]!.ToString()) == 2)
                engineID = EnginePreset.Precision.Id;
            else
                return;

            float percent = float.Parse(score["percent"]!.ToString());
            int hit = int.Parse(score["notesHit"]!.ToString());
            StarAmount stars = StarAmountHelper.GetStarsFromInt((int)Math.Floor(float.Parse(score["stars"]!.ToString())));

            playerEntries.Add(new() 
            {
                PlayerId = profile.Id,

                Instrument = (Instrument)int.Parse(score["instrument"]!.ToString()),
                Difficulty = (Difficulty)int.Parse(score["difficulty"]!.ToString()),

                EnginePresetId = engineID,

                Score = int.Parse(score["score"]!.ToString()),
                Stars = stars,

                NotesHit = hit,
                NotesMissed = (int)Math.Floor(hit - (hit / percent)),
                IsFc = percent == 1 && int.Parse(score["overhits"]!.ToString()) == 0,
                IsReplay = false,

                Percent = percent
            });

            GameRecord gameRecord = new()
            {
                Date = DateTime.Parse(score["createdAt"]!.ToString()),

                SongChecksum = HexToBytes(score["song"]["chartFileHash"]!.ToString()),
                SongName = score["song"]["name"]!.ToString(),
                SongArtist = score["song"]["artist"]!.ToString(),
                SongCharter = score["song"]["charter"]!.ToString(),

                ReplayFileName = $"{score["replayPath"]!.ToString()}.replay",
                ReplayChecksum = null,

                BandScore = int.Parse(score["score"]!.ToString()),
                BandStars = stars,

                SongSpeed = float.Parse(score["songSpeed"]!.ToString()),
                PlayedWithReplay = false,
            };

            bool exists = false;
            foreach (GameRecord record in ScoreContainer._db.QueryAllScores())
            {
                if (record.SongChecksum.SequenceEqual(gameRecord.SongChecksum)
                    && record.SongName == gameRecord.SongName
                    && record.SongArtist == gameRecord.SongArtist
                    && record.SongCharter == gameRecord.SongCharter
                    && record.BandScore == gameRecord.BandScore
                    && record.BandStars == gameRecord.BandStars
                    && record.SongSpeed == gameRecord.SongSpeed)
                { 
                    exists = true;
                    break;
                }
            }
            if (exists)
                continue;

            string scoreProfileName = score["profileName"]!.ToString();
            if (scoreProfileName != profile.Name && !confirmAll)
            {
                bool shouldSkip = false;
                bool done = false;
                MessageDialog diag = ShowScoreDialog(
                    () => { done = true; },
                    () => { confirmAll = true; done = true; },
                    () => { shouldSkip = true; done = true; },
                    () => { return; }
                );
                diag.Title.text = "Conflicting profile names";
                diag.Message.text = $"A score for \"{score["song"]["name"]!.ToString()}\" was sumbitted under a different profile name to your current profile.\n\nScore Profile: {scoreProfileName}\nCurrent Profile: {profile.Name}";
                while (!done)
                    await Task.Delay(1);
                if (shouldSkip)
                    continue;
            }

            ScoreContainer.RecordScore(gameRecord, playerEntries);
        }
    }

    public static void BuildLoggedOutSettings()
    {
        SpyTab._settings.Clear();
        SpyTab._settings.Add(new HeaderMetadata("Login"));
        SpyTab._settings.Add(nameof(Settings.SpySettings.Username));
        SpyTab._settings.Add(nameof(Settings.SpySettings.Password));
        SpyTab._settings.Add(new ButtonRowMetadata(nameof(SpySettings.LoginToYARGSpy)));
        SpyTab._settings.Add(new HeaderMetadata("Options"));
        SpyTab._settings.Add(nameof(Settings.SpySettings.ShowBoard));
        MonoSingleton<SettingsMenu>.Instance?.RefreshAndKeepPosition();
    }

    public static void BuildLoggedInSettings()
    {
        SpyTab._settings.Clear();
        SpyTab._settings.Add(new HeaderMetadata("Login"));
        SpyTab._settings.Add(new ButtonRowMetadata(nameof(SpySettings.LogoutYARGSpy)));
        SpyTab._settings.Add(new HeaderMetadata("Options"));
        SpyTab._settings.Add(nameof(Settings.SpySettings.UploadScores));
        SpyTab._settings.Add(nameof(Settings.SpySettings.ShowBoard));
        SpyTab._settings.Add(new ButtonRowMetadata(nameof(SpySettings.DownloadScores)));
        MonoSingleton<SettingsMenu>.Instance?.RefreshAndKeepPosition();
    }
}