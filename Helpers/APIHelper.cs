using BepInEx;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SocialPlatforms.Impl;
using YARG.Core.Replays;
using YARG.Core.Song;
using YARG.Gameplay;
using YARG.Menu.Persistent;
using YARGSpy.Settings;
using YARGSpy.Settings.Patches;

namespace YARGSpy.Helpers;

public class APIHelper
{

    private static readonly string API_URI = "https://api.yargspy.com";

    public static async Task<UnityWebRequest> Get(string endpoint, bool useToken = false, string contentType = "application/json")
    {
        UnityWebRequest GetReq = UnityWebRequest.Get("https://api.yargspy.com"+endpoint);
        GetReq.SetRequestHeader("content-type", contentType ?? "application/json");

        if (useToken)
            GetReq.SetRequestHeader("Authorization", $"Bearer {SpySettings.instance.Token.Value}");

        await GetReq.SendWebRequest();
        return GetReq;
    }
    public static async Task<UnityWebRequest> Post(string endpoint, object data, bool useToken = false, string contentType = "application/json")
    {
        UnityWebRequest GetReq = null;

        if (data is string)
        {
            Plugin.Logger.LogInfo("Using string data");
            GetReq = new UnityWebRequest(API_URI + endpoint, "POST");
            GetReq.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes((string)data));
            GetReq.downloadHandler = new DownloadHandlerBuffer();
        }
        else if (data is WWWForm form)
            GetReq = UnityWebRequest.Post(API_URI + endpoint, form);
        else
            GetReq = new UnityWebRequest(API_URI + endpoint, "POST");

        if (!(data is WWWForm) && !contentType.IsNullOrWhiteSpace())
            GetReq.SetRequestHeader("content-type", contentType ?? "application/json");

        if (useToken)
            GetReq.SetRequestHeader("Authorization", $"Bearer {SpySettings.instance.Token.Value}");

        await GetReq.SendWebRequest();
        return GetReq;
    }

    public static async void GetUser()
    {
        if (SpySettings.instance.Token.Value.IsNullOrWhiteSpace())
            return;

        UnityWebRequest GetUserReq = await Get("/user/profile", true);
        if (GetUserReq.result != UnityWebRequest.Result.Success || GetUserReq.responseCode != 200)
        {
            ToastManager.ToastError($"YARGSpy login error:\n{GetUserReq.error}");
            return;
        }

        SpySettings.user = (JObject)JObject.Parse(GetUserReq.downloadHandler.text)["user"];
        ToastManager.ToastSuccess($"Successfully logged into YARGSpy as {SpySettings.user["username"]!.ToString()}!", () => { });
        SpySettings.BuildLoggedInSettings();
    }

    public static async void UploadScore(GameManager __instance, ReplayInfo __result, List<RequestFile> files = null)
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
        form.AddField("reqType", files.Count>0 ? "complete" : "replayOnly");

        UnityWebRequest sendScore = await Post("/replay/register", form, true, "");

        if (sendScore.responseCode == 422 && files.Count == 0)
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

// Used from https://gist.github.com/krzys-h/9062552e33dd7bd7fe4a6c12db109a1a . Makes requests
public class UnityWebRequestAwaiter : INotifyCompletion
{
    private UnityWebRequestAsyncOperation asyncOp;
    private Action continuation;

    public UnityWebRequestAwaiter(UnityWebRequestAsyncOperation asyncOp)
    {
        this.asyncOp = asyncOp;
        asyncOp.completed += OnRequestCompleted;
    }

    public bool IsCompleted { get { return asyncOp.isDone; } }

    public void GetResult() { }

    public void OnCompleted(Action continuation)
    {
        this.continuation = continuation;
    }

    private void OnRequestCompleted(AsyncOperation obj)
    {
        continuation();
    }
}

public static class ExtensionMethods
{
    public static UnityWebRequestAwaiter GetAwaiter(this UnityWebRequestAsyncOperation asyncOp)
    {
        return new UnityWebRequestAwaiter(asyncOp);
    }
}
