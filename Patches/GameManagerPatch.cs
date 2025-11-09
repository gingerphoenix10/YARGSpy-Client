/*
 *  [Patches]
 *  
 *  SaveReplay - Postfix
 *  - Sends the replay to YARGSpy servers once a replay is saved
 *  
 *  Start - Prefix
 *  - Initializes the in-game leaderboard once a song is started
 *  
 *  Update - Postfix
 *  - Checks if score is updated, and perform UI updates / overtake detections
 *  
 */


using Cysharp.Text;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using YARG.Core.Replays;
using YARG.Core.Song;
using YARG.Gameplay;
using YARG.Menu.Persistent;
using YARGSpy.Settings;
using YARGSpy.Helpers;
using YARG.Gameplay.HUD;
using YARG.Gameplay.Player;

namespace YARGSpy.Patches;

[HarmonyPatch(typeof(GameManager))]
internal static class GameManagerPatch
{
    public static int placement;
    public static JArray entries;
    public static Transform scores;
    public static int currentScore;
    public static string Username;

    public static readonly Color PlayerColor = new Color(0, 0, 0, 0.37f);
    public static readonly Color LocalColor = new Color(0.289f, 0f, 0.591f, 0.37f);
    public static readonly Color SelfColor = new Color(0.5f, 0.5f, 0, 0.37f);

    [HarmonyPatch(nameof(GameManager.SaveReplay))]
    [HarmonyPostfix]
    internal static void SaveReplayPostfix(GameManager __instance, ref ReplayInfo __result)
    {
        if (__instance._songRunner.SongTime < __instance.SongLength)
            return;

        if (!SpySettings.instance.UploadScores.Value)
            return;

        if (SpySettings.user == null)
        {
            ToastManager.ToastError("Upload failed: Not logged in");
            return;
        }

        if (__result == null)
        {
            ToastManager.ToastError("Upload failed: Replay wasn't saved for some reason");
            return;
        }

        if (!File.Exists(__result.FilePath))
            return;

        ReplayReadResult result;
        ReplayInfo info;
        ReplayData replayData;
        (result, info, replayData) = ReplayIO.TryDeserialize(__result.FilePath, new ReplayReadOptions { KeepFrameTimes = false });

        if (result != ReplayReadResult.Valid)
        {
            ToastManager.ToastError("Upload failed: Invalid replay");
            return;
        }

        if (replayData.PlayerCount > 1)
        {
            ToastManager.ToastError("Upload failed: Co-op runs not yet supported");
            return;
        }

        if (info.BandScore == 0)
        {
            ToastManager.ToastError("Upload failed: Score is 0");
            return;
        }

        for (int frameIndex = 0; frameIndex < replayData.Frames.Length; frameIndex++)
        {
            ReplayFrame currentFrame = replayData.Frames[frameIndex];
            if (!ValidityHelper.ValidEngine(currentFrame, info))
            {
                //ToastManager.ToastError("Upload failed: Invalid engine (this is wrong sometimes)");
                //return;
            }
        }
        APIHelper.UploadScore(__instance, __result);
    }

    [HarmonyPatch(nameof(GameManager.Start))]
    [HarmonyPrefix]
    internal static void StartPrefix(GameManager __instance)
    {
        placement = 1;
        entries = null;
        scores = null;
        currentScore = 0;
        Username = null;
        if (!SpySettings.instance.ShowBoard.Value)
            return;
        __instance.SongStarted += async () =>
        {
            if (__instance.IsPractice || !__instance.IsSongStarted)
                return;

            GameObject SpyCanvas = GameObject.Instantiate(Plugin.bundle.LoadAsset<GameObject>("YARGSpyCanvas"));
            SpyCanvas.GetComponent<Canvas>().sortingOrder = 1;
            scores = SpyCanvas.transform.Find("Scores");
            foreach (Transform scoreBox in scores)
            {
                scoreBox.gameObject.SetActive(false);
            }

            Username = SpySettings.user != null ? SpySettings.user["username"]!.ToString() : __instance.Players[0].Player.Profile.Name;

            Transform p1 = scores.Find("1");
            p1.gameObject.SetActive(true);
            placement = 1;
            p1.GetComponent<Image>().color = LocalColor;
            p1.Find("Name").GetComponent<TextMeshProUGUI>().text = Username;
            p1.Find("Score").GetComponent<TextMeshProUGUI>().text = "0";
            p1.Find("Placement").GetComponent<TextMeshProUGUI>().text = $"#{placement}";
            p1.Find("Percentage").GetComponent<TextMeshProUGUI>().enabled = false;
            entries = new();

            string hash = BitConverter.ToString(__instance.Song.Hash.HashBytes).Replace("-", string.Empty).ToLower();

            UnityWebRequest getId = await APIHelper.Get($"/song/hashToId?hash={hash}");
            if (getId.result != UnityWebRequest.Result.Success)
            {
                Plugin.Logger.LogError("[id] " + getId.error);
            }
            else
            {
                if (getId.responseCode != 200)
                {
                    Plugin.Logger.LogError("[id] " + getId.downloadHandler.text);
                    return;
                }
                JObject idReponse = JObject.Parse(getId.downloadHandler.text);
                string songId = idReponse["id"]!.ToString();
                Dictionary<string, object> leaderboard = new()
                    {
                        { "allowedModifiers", new List<int>() { 0,4,5,8,9,10 } },
                        { "allowSlowdowns", false },
                        { "id", songId },
                        { "instrument", 255 },
                        { "limit", 100 },
                        { "page", 1 }
                    };

                UnityWebRequest getBoard = await APIHelper.Post("/song/leaderboard", JsonConvert.SerializeObject(leaderboard));
                if (getBoard.result != UnityWebRequest.Result.Success || getBoard.responseCode != 200)
                {
                    Plugin.Logger.LogError("[board] " + getBoard.error);
                }
                else
                {
                    JObject boardResponse = JObject.Parse(getBoard.downloadHandler.text);
                    entries = (JArray)boardResponse["entries"];
                    placement = entries.Count + 1;
                    for (int i = 1; i <= 5; i++)
                    {
                        if (entries.Count >= i)
                        {
                            Transform remote = scores.Find(i.ToString());
                            Transform local = scores.Find((i + 1).ToString());
                            remote.Find("Name").GetComponent<TextMeshProUGUI>().text = entries[i - 1]["uploader"]["username"]!.ToString();
                            TextMeshProExtensions.SetTextFormat<string, int>((TMP_Text)remote.Find("Score").GetComponent<TextMeshProUGUI>(), "{0}{1:N0}", "", int.Parse(entries[i - 1]["score"]!.ToString()));
                            remote.Find("Placement").GetComponent<TextMeshProUGUI>().text = $"#{i}";
                            remote.Find("Percentage").GetComponent<TextMeshProUGUI>().text = $"{(int)Math.Floor((double)(entries[i - 1]["childrenScores"][0]["percent"]) * 100)}%";
                            remote.Find("Percentage").GetComponent<TextMeshProUGUI>().enabled = true;
                            remote.gameObject.SetActive(true);
                            remote.GetComponent<Image>().color = PlayerColor;
                            if (SpySettings.user != null && entries[i - 1]["uploader"]["username"]!.ToString() == Username)
                                remote.GetComponent<Image>().color = SelfColor;
                            local.GetComponent<Image>().color = LocalColor;
                            local.Find("Name").GetComponent<TextMeshProUGUI>().text = Username;
                            local.Find("Score").GetComponent<TextMeshProUGUI>().text = "0";
                            local.Find("Placement").GetComponent<TextMeshProUGUI>().text = $"#{placement}";
                            local.Find("Percentage").GetComponent<TextMeshProUGUI>().enabled = false;
                            local.gameObject.SetActive(true);
                        }
                    }
                    if (entries.Count > 5)
                    {
                        Transform above = scores.Find("5");
                        above.Find("Name").GetComponent<TextMeshProUGUI>().text = entries[entries.Count - 1]["uploader"]["username"]!.ToString();
                        TextMeshProExtensions.SetTextFormat<string, int>((TMP_Text)above.Find("Score").GetComponent<TextMeshProUGUI>(), "{0}{1:N0}", "", int.Parse(entries[entries.Count - 1]["score"]!.ToString()));
                        above.Find("Placement").GetComponent<TextMeshProUGUI>().text = $"#{entries.Count}";
                        above.Find("Percentage").GetComponent<TextMeshProUGUI>().text = $"{(int)Math.Floor((double)(entries[entries.Count - 1]["childrenScores"][0]["percent"]) * 100)}%";
                        above.Find("Percentage").GetComponent<TextMeshProUGUI>().enabled = true;
                        above.GetComponent<Image>().color = PlayerColor;
                        if (SpySettings.user != null && entries[entries.Count - 1]["uploader"]["username"]!.ToString() == Username)
                            above.GetComponent<Image>().color = SelfColor;
                        above.gameObject.SetActive(true);
                    }
                }
            }

        };
    }

    [HarmonyPatch(nameof(GameManager.Update))]
    [HarmonyPostfix]
    internal static void Update(GameManager __instance)
    {
        if (!SpySettings.instance.ShowBoard.Value)
            return;
        if (__instance.BandScore != currentScore)
        {
            currentScore = __instance.BandScore;
            Transform playerBox;
            if (GameManagerPatch.placement >= 6)
                playerBox = GameManagerPatch.scores.Find("6");
            else
                playerBox = GameManagerPatch.scores.Find(GameManagerPatch.placement.ToString());

            TextMeshProExtensions.SetTextFormat<string, int>((TMP_Text)playerBox.Find("Score").GetComponent<TextMeshProUGUI>(), "{0}{1:N0}", "", currentScore);

            if (placement > 1)
            {
                while (placement > 1 && currentScore > int.Parse(entries[placement - 2]["score"]!.ToString()))
                {
                    placement--;
                    Overtake();
                }
            }
        }
        TweensHelper.UpdateTweens();
    }

    internal static void Overtake()
    {
        TextMeshProUGUI PlacementText = scores.Find(Math.Min(6, placement).ToString()).Find("Placement").GetComponent<TextMeshProUGUI>();
        if (placement >= 6)
        {
            Transform above = scores.Find("5");
            var newEntry = entries[placement - 2];
            above.Find("Name").GetComponent<TextMeshProUGUI>().text = newEntry["uploader"]["username"]!.ToString();
            TextMeshProExtensions.SetTextFormat<string, int>((TMP_Text)above.Find("Score").GetComponent<TextMeshProUGUI>(), "{0}{1:N0}", "", int.Parse(newEntry["score"]!.ToString()));
            above.Find("Placement").GetComponent<TextMeshProUGUI>().text = $"#{placement - 1}";
            above.Find("Percentage").GetComponent<TextMeshProUGUI>().text = $"{(int)Math.Floor((double)(newEntry["childrenScores"][0]["percent"]) * 100)}%";
            above.Find("Percentage").GetComponent<TextMeshProUGUI>().enabled = true;
            above.GetComponent<Image>().color = PlayerColor;
            if (SpySettings.user != null && newEntry["uploader"]["username"]!.ToString() == Username)
                above.GetComponent<Image>().color = SelfColor;
            above.gameObject.SetActive(true);
            TweensHelper.SetTweens(new()
                {
                    new TweensHelper.TweenInfo(5, 0, 1f, TweensHelper.EasingStyles.BelowSix, (float val) =>
                    {
                        Plugin.Logger.LogInfo(val);
                        above.localPosition = new Vector3(val, above.localPosition.y, above.localPosition.z);
                    })
                });
        }
        else
        {
            Transform moveUp = scores.Find(placement.ToString());
            Transform moveDown = scores.Find((placement + 1).ToString());
            moveUp.GetComponent<Image>().color = LocalColor;
            moveUp.Find("Name").GetComponent<TextMeshProUGUI>().text = moveDown.Find("Name").GetComponent<TextMeshProUGUI>().text;
            moveUp.Find("Score").GetComponent<TextMeshProUGUI>().text = moveDown.Find("Score").GetComponent<TextMeshProUGUI>().text;
            moveUp.Find("Placement").GetComponent<TextMeshProUGUI>().text = moveDown.Find("Placement").GetComponent<TextMeshProUGUI>().text;
            moveUp.Find("Percentage").GetComponent<TextMeshProUGUI>().enabled = false;
            moveUp.gameObject.SetActive(true);

            var overtakenEntry = entries[placement - 1];
            moveDown.Find("Name").GetComponent<TextMeshProUGUI>().text = overtakenEntry["uploader"]["username"]!.ToString();
            TextMeshProExtensions.SetTextFormat<string, int>((TMP_Text)moveDown.Find("Score").GetComponent<TextMeshProUGUI>(), "{0}{1:N0}", "", int.Parse(overtakenEntry["score"]!.ToString()));
            moveDown.Find("Placement").GetComponent<TextMeshProUGUI>().text = $"#{placement + 1}";
            moveDown.Find("Percentage").GetComponent<TextMeshProUGUI>().text = $"{(int)Math.Floor((double)(overtakenEntry["childrenScores"][0]["percent"]) * 100)}%";
            moveDown.Find("Percentage").GetComponent<TextMeshProUGUI>().enabled = true;
            moveDown.GetComponent<Image>().color = PlayerColor;
            if (SpySettings.user != null && overtakenEntry["uploader"]["username"]!.ToString() == Username)
                moveDown.GetComponent<Image>().color = SelfColor;
            moveDown.gameObject.SetActive(true);

            TweensHelper.SetTweens(new()); // Snap all tweens to final position so the values under aren't messed up

            float upperPos = moveUp.localPosition.y;
            float lowerPos = moveDown.localPosition.y;

            moveUp.localPosition = new Vector3(moveUp.localPosition.x, lowerPos, moveUp.localPosition.z);
            moveDown.localPosition = new Vector3(moveDown.localPosition.x, lowerPos, moveDown.localPosition.z);

            float tweenTime = 1f;
            TweensHelper.SetTweens(new()
                {
                    new TweensHelper.TweenInfo(lowerPos, upperPos, tweenTime, TweensHelper.EasingStyles.easeOutQuint, (float val) =>
                    {
                        moveUp.localPosition = new Vector3(moveUp.localPosition.x, val, moveUp.localPosition.z);
                    }),
                    new TweensHelper.TweenInfo(upperPos, lowerPos, tweenTime, TweensHelper.EasingStyles.easeOutQuint, (float val) =>
                    {
                        moveDown.localPosition = new Vector3(moveDown.localPosition.x, val, moveDown.localPosition.z);
                    }),
                });
        }
        PlacementText.text = $"#{placement}";
    }
}
