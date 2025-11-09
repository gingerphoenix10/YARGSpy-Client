using BepInEx;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SocialPlatforms.Impl;
using YARG.Core.Extensions;
using YARG.Core.IO;
using YARG.Core.IO.Ini;
using YARG.Core.Replays;
using YARG.Core.Song;
using YARG.Core.Song.Cache;
using YARG.Gameplay;
using YARG.Helpers;
using YARG.Menu.Persistent;
using YARGSpy.Settings;
using YARGSpy.Settings.Patches;

namespace YARGSpy.Helpers;

public class APIHelper
{

    private static readonly string API_URI = "https://api.yargspy.com";
    //private static readonly string API_URI = "http://localhost:3000";

    public static async Task<UnityWebRequest> Get(string endpoint, bool useToken = false, string contentType = "application/json")
    {
        UnityWebRequest GetReq = UnityWebRequest.Get("https://api.yargspy.com"+endpoint);
        GetReq.SetRequestHeader("content-type", contentType ?? "application/json");

        if (useToken)
            GetReq.SetRequestHeader("Authorization", $"Bearer {SpySettings.instance.Token.Value}");

        await GetReq.SendWebRequest();

        if (GetReq.responseCode >= 400 && GetReq.responseCode < 500)
            Plugin.Logger.LogError($"\n\nGET error:\nEndpoint: {endpoint}\nContent Type: {contentType ?? "application/json"}\nResponse:\n{GetReq.downloadHandler.text}\n\n");

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

        if (GetReq.responseCode >= 400 && GetReq.responseCode < 500)
            Plugin.Logger.LogError($"\n\nPOST error:\nEndpoint: {endpoint}\nContent Type: {contentType ?? "application/json"}\nResponse:\n{GetReq.downloadHandler.text}\n\n");

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
                file.contents,
                file.fileName,
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
                    if (__instance.Song is UnpackedIniEntry iniEntry)
                    {
                        string iniPath = Path.Combine(iniEntry._location, "song.ini");
                        if (!File.Exists(iniPath))
                        {
                            ToastManager.ToastError("Couldn't upload: Song not on leaderboard, and chart file could not be found");
                            return;
                        }
                        string chartPath = Path.Combine(iniEntry._location, IniSubEntry.CHART_FILE_TYPES[(int)iniEntry._chartFormat].Filename);
                        if (!File.Exists(chartPath))
                        {
                            ToastManager.ToastError("Couldn't upload: Song not on leaderboard, and chart file could not be found");
                            return;
                        }

                        UploadScore(__instance, __result, new()
                        {
                            new RequestFile("chartFile", chartPath, iniEntry._chartFormat==ChartFormat.Chart?"application/octet":"audio/midi"),
                            new RequestFile("songDataFile", iniPath, "application/octet")
                        });
                    }
                    break;
                case EntryType.Sng:
                    var song = SngFile.TryLoadFromFile(__instance.Song.ActualLocation, true);
                    File.WriteAllText(Path.Combine(PathHelper.PersistentDataPath, "export.json"), JsonConvert.SerializeObject(song.Modifiers));
                    if (!song.IsLoaded)
                        return;
                    string ini = "[Song]";

                    List<object> entries = new()
                    {
                        song.Modifiers._booleans,
                        song.Modifiers._doubles,
                        song.Modifiers._floats,
                        song.Modifiers._int16s,
                        song.Modifiers._int32s,
                        song.Modifiers._int64Arrays,
                        song.Modifiers._int64s,
                        song.Modifiers._strings,
                        song.Modifiers._uint16s,
                        song.Modifiers._uint32s,
                        song.Modifiers._uint64s,
                    };

                    foreach (var typeEntry in entries)
                    {
                        if (!(typeEntry is IDictionary dict))
                            continue;
                        foreach (DictionaryEntry valueEntry in dict) {
                            ini += "\n"+valueEntry.Key.ToString()+" = "+valueEntry.Value.ToString();
                        }
                    }

                    List<RequestFile> uploadFiles = new();
                    uploadFiles.Add(new RequestFile("songDataFile", Encoding.UTF8.GetBytes(ini), "song.ini", "application/octet"));
                    foreach (KeyValuePair<string, SngFileListing> file in song.Listings)
                    {
                        if (file.Key.EndsWith(".chart") || file.Key.EndsWith(".mid") || file.Key.EndsWith(".midi"))
                        {
                            using (MemoryStream ms = new MemoryStream())
                            {
                                song.CreateStream(file.Key, file.Value).CopyTo(ms);
                                uploadFiles.Add(new RequestFile("chartFile", ms.ToArray(), file.Key, file.Key.EndsWith(".chart") ? "application/octet" : "audio/midi"));
                            }
                        }
                    }
                    UploadScore(__instance, __result, uploadFiles);
                    break;
                default:
                    Plugin.Logger.LogInfo("Entry type: " + __instance.Song.SubType);
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
        public Byte[] contents;
        public string fileName;
        public string mimeType;
        public RequestFile(string FieldName, string path, string MimeType)
        {
            fieldName = FieldName;
            contents = File.ReadAllBytes(path);
            fileName = Path.GetFileName(path);
            mimeType = MimeType;
        }

        public RequestFile(string FieldName, Byte[] Contents, string FileName, string MimeType)
        {
            fieldName = FieldName;
            contents = Contents;
            fileName = FileName;
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
