using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using YARG.Menu.Settings;
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

    public void LoginToYARGSpy()
    {
        Dictionary<string, string> login = new()
            {
                { "username", Username.Value },
                { "password", PasswordSettingVisual.value }
            };

        UnityWebRequest req = new UnityWebRequest("https://api.yargspy.com/user/login", "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(login)));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("content-type", "application/json");

        var operation = req.SendWebRequest();
        operation.completed += _ =>
        {
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
        };
    }

    public void LogoutYARGSpy()
    {
        Token.Value = "";
        user = null;
        SettingsManager.SaveSettings();
        BuildLoggedOutSettings();
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
        MonoSingleton<SettingsMenu>.Instance?.RefreshAndKeepPosition();
    }
}