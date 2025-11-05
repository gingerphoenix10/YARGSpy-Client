using BepInEx;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using YARG.Core.Replays;
using YARG.Core.Song;
using YARG.Gameplay;
using YARG.Menu.Persistent;
using YARGSpy.Settings;
using YARGSpy.Settings.Patches;

namespace YARGSpy.Helpers;

public class APIHelper
{
    public static void GetUser()
    {
        if (SpySettings.instance.Token.Value.IsNullOrWhiteSpace())
            return;
        UnityWebRequest GetUserReq = UnityWebRequest.Get("https://api.yargspy.com/user/profile");
        GetUserReq.SetRequestHeader("content-type", "application/json");
        GetUserReq.SetRequestHeader("Authorization", $"Bearer {SpySettings.instance.Token.Value}");
        var UserOp = GetUserReq.SendWebRequest();
        UserOp.completed += _ =>
        {
            if (GetUserReq.result != UnityWebRequest.Result.Success || GetUserReq.responseCode != 200)
            {
                ToastManager.ToastError($"YARGSpy login error:\n{GetUserReq.error}");
                return;
            }
            SpySettings.user = (JObject)JObject.Parse(GetUserReq.downloadHandler.text)["user"];
            ToastManager.ToastSuccess($"Successfully logged into YARGSpy as {SpySettings.user["username"]!.ToString()}!", () => { });
            SpySettings.BuildLoggedInSettings();
        };
    }

    public static void UploadScore(GameManager __instance, ReplayInfo __result, List<RequestFile> files = null)
    {
        if (files == null)
            files = new();
        WWWForm form = new WWWForm();
        form.AddBinaryData(
            "replayFile",
            File.ReadAllBytes(__result.FilePath),
            Path.GetFileName(__result.FilePath),
            "application/octet-stream"
        );
        foreach (RequestFile file in files)
        {
            form.AddBinaryData(
                file.fieldName,
                File.ReadAllBytes(file.path),
                Path.GetFileName(file.path),
                file.mimeType
            );
        }
        form.AddField("reqType", "complete");

        UnityWebRequest sendScore = UnityWebRequest.Post("https://api.yargspy.com/replay/register", form);
        sendScore.SetRequestHeader("Authorization", $"Bearer {SpySettings.instance.Token.Value}");
        var sendScoreOp = sendScore.SendWebRequest();
        sendScoreOp.completed += _ =>
        {
            if (sendScore.responseCode == 422)
            {
                switch (__instance.Song.SubType)
                {
                    case EntryType.Ini:
                        if (__instance.Song is UnpackedIniEntry)
                        {
                            UnpackedIniEntry entry = (UnpackedIniEntry)(__instance.Song);
                            string iniPath = Path.Combine(entry._location, "song.ini");
                            if (!File.Exists(iniPath))
                            {
                                ToastManager.ToastError("Couldn't upload: Song not on leaderboard, and chart file could not be found");
                                return;
                            }
                            string chartPath = Path.Combine(entry._location, IniSubEntry.CHART_FILE_TYPES[(int)entry._chartFormat].Filename);
                            if (!File.Exists(chartPath))
                            {
                                ToastManager.ToastError("Couldn't upload: Song not on leaderboard, and chart file could not be found");
                                return;
                            }

                            UploadScore(__instance, __result, new()
                            {
                                new RequestFile("chartFile", chartPath, entry._chartFormat==ChartFormat.Chart?"application/octet":"audio/midi"),
                                new RequestFile("songDataFile", iniPath, "application/octet")
                            });
                        }
                        break;
                }
            }
            else if (sendScore.responseCode == 200 || sendScore.responseCode == 201)
                ToastManager.ToastSuccess("Score Submitted to YARGSpy!");
            else
                ToastManager.ToastError("Upload failed: HTTP Error\n" + sendScore.error);
        };
    }
    public class RequestFile
    {
        public string fieldName;
        public string path;
        public string mimeType;
        public RequestFile(string FieldName, string Path, string MimeType)
        {
            fieldName = FieldName;
            path = Path;
            mimeType = MimeType;
        }
    }
}
