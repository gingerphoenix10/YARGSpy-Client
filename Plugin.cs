using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YARG.Settings;
using YARGSpy.Helpers;
using YARGSpy.Settings;

namespace YARGSpy;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    private static readonly Harmony Patcher = new(MyPluginInfo.PLUGIN_GUID);
    public static UnityEngine.Object[] bundle;

    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        Patcher.PatchAll();
        SpySettings.BuildLoggedOutSettings();
        SettingsManager.DisplayedSettingsTabs.Insert(SettingsManager.DisplayedSettingsTabs.Count-1, SpySettings.SpyTab);
        SettingsManager.AllSettingsTabs.Insert(SettingsManager.AllSettingsTabs.Count - 1, SpySettings.SpyTab);
        SceneManager.sceneLoaded += (Scene scene, LoadSceneMode _) =>
        {
            if (scene.name == "MenuScene")
            {
                Image yargLogo = GameObject.Find("/Menu Manager").transform.Find("MainMenu/Logo").GetComponent<Image>();
                Texture2D texture = new Texture2D(1, 1);
                Stream imgStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("YARGSpy.Assets.yargspy-W.png");
                byte[] buffer = new byte[16 * 1024];
                using (MemoryStream ms = new MemoryStream())
                {
                    int read;
                    while ((read = imgStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        ms.Write(buffer, 0, read);
                    }

                    texture.LoadImage(ms.ToArray());
                    yargLogo.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
                }
            }
        };
        bundle = AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("YARGSpy.Assets.yargspy_bundle")).LoadAllAssets();

        if (Settings.Patches.SettingsManagerPatch.ready == null)
            Settings.Patches.SettingsManagerPatch.ready = new UnityEvent();
        Settings.Patches.SettingsManagerPatch.ready.AddListener(() =>
        {
            APIHelper.GetUser();
        });
    }
}