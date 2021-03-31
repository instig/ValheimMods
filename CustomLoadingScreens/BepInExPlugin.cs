﻿using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace CustomLoadingScreens
{
    [BepInPlugin("aedenthorn.CustomLoadingScreens", "Custom Loading Screens", "0.3.1")]
    public partial class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static BepInExPlugin context;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<string> loadingText;
        public static ConfigEntry<bool> differentSpawnScreen;
        public static ConfigEntry<bool> differentSpawnTip;
        public static ConfigEntry<bool> showTipsOnLoadingScreen;
        public static ConfigEntry<bool> removeVignette;
        public static ConfigEntry<Color> spawnColorMask;
        public static ConfigEntry<Color> loadingColorMask;
        public static ConfigEntry<Color> loadingTextColor;
        public static ConfigEntry<Color> tipTextColor;

        public static List<string> loadingScreens = new List<string>();
        public static Dictionary<string, string> loadingScreens2 = new Dictionary<string, string>();
        public static Dictionary<string, DateTime> fileWriteTimes = new Dictionary<string, DateTime>();
        public static List<string> screensToLoad = new List<string>();
        public static string[] loadingTips = new string[0];
        public static Dictionary<string, Texture2D> cachedScreens = new Dictionary<string, Texture2D>();

        private static Sprite loadingSprite;
        private static Sprite loadingSprite2;
        private static string loadingTip;
        private static string loadingTip2;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;

            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            differentSpawnScreen = Config.Bind<bool>("General", "DifferentSpawnScreen", true, "Use a different screen for the spawn part");
            differentSpawnTip = Config.Bind<bool>("General", "DifferentSpawnTip", true, "Use a different tip for the spawn part");
            showTipsOnLoadingScreen = Config.Bind<bool>("General", "ShowTipsOnLoadingScreen", true, "Show tips on loading screen.");
            spawnColorMask = Config.Bind<Color>("General", "SpawnColorMask", new Color(0.532f,0.588f, 0.853f,1f), "Change the color mask of the spawn screen (set last number to 0 to disable)");
            loadingColorMask = Config.Bind<Color>("General", "LoadingColorMask", Color.white, "Change the color mask of the initial loading screen (set to white to disable)");
            removeVignette = Config.Bind<bool>("General", "RemoveMask", true, "Remove dark edges for the spawn part");
            nexusID = Config.Bind<int>("General", "NexusID", 0, "Nexus mod ID for updates");
            loadingText = Config.Bind<string>("General", "LoadingText", "Loading...", "Custom Loading... text");
            loadingTextColor = Config.Bind<Color>("General", "LoadingTextColor", new Color(1, 0.641f, 0, 1), "Custom Loading... text color");
            tipTextColor = Config.Bind<Color>("General", "TipTextColor", Color.white, "Custom tip text color");

            if (!modEnabled.Value)
                return;

            LoadCustomLoadingScreens();
            LoadTips();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        private void LoadTips()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CustomLoadingScreens", "tips.txt");
            if (!File.Exists(path))
            {
                Dbgl("No tips, creating empty file");
                File.Create(path);
                return;
            }
            loadingTips = File.ReadAllLines(path);
        }

        private static void LoadCustomLoadingScreens()
        {
            loadingScreens.Clear();
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CustomLoadingScreens");

            if (!Directory.Exists(path)) 
            {
                Dbgl($"Directory {path} does not exist! Creating.");
                Directory.CreateDirectory(path);
                return;
            }

            foreach (string file in Directory.GetFiles(path, "*.png", SearchOption.AllDirectories))
            {
                loadingScreens.Add(file);
            }
            Dbgl($"Directory {path} got {loadingScreens.Count} screens.");

        }

        private static Sprite GetRandomLoadingScreen()
        {
            if (!loadingScreens.Any())
                return null;

            Texture2D tex = new Texture2D(2, 2);
            byte[] imageData = File.ReadAllBytes(loadingScreens[UnityEngine.Random.Range(0,loadingScreens.Count)]);
            tex.LoadImage(imageData);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero, 1);
        }

        [HarmonyPatch(typeof(FejdStartup), "Start")]
        public static class FejdStartup_Start_Patch
        {

            public static void Prefix(FejdStartup __instance)
            {
            }
        }
        [HarmonyPatch(typeof(FejdStartup), "LoadMainScene")]
        public static class LoadMainScene_Patch
        {

            public static void Prefix(FejdStartup __instance)
            {
                Dbgl($"loading main scene");
                Dbgl($"getting new random images");

                loadingSprite = GetRandomLoadingScreen();
                if (differentSpawnScreen.Value)
                    loadingSprite2 = GetRandomLoadingScreen();

                Dbgl($"getting new random images");

                if (loadingTips.Any())
                {
                    loadingTip = loadingTips[UnityEngine.Random.Range(0, loadingTips.Length)];
                    if (differentSpawnTip.Value)
                        loadingTip2 = loadingTips[UnityEngine.Random.Range(0, loadingTips.Length)];
                }


                Image image = Instantiate(__instance.m_loading.transform.Find("Bkg").GetComponent<Image>(), __instance.m_loading.transform);
                if(image == null)
                {
                    Dbgl($"missed bkg");
                    return;
                }
                Dbgl($"setting sprite to loading screen");

                image.sprite = loadingSprite;
                image.color = loadingColorMask.Value;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;

                if (loadingTips.Any() && showTipsOnLoadingScreen.Value)
                {
                    GameObject hud = Resources.FindObjectsOfTypeAll<GameObject>().First(g => g.name == "HUD");
                    Instantiate(hud.transform.Find("LoadingBlack/Loading/panel_separator").gameObject, __instance.m_loading.transform);
                    Text text = Instantiate(hud.transform.Find("LoadingBlack/Loading/Tip").gameObject, __instance.m_loading.transform).GetComponent<Text>();
                    if (text != null)
                    {
                        text.text = loadingTip;
                        text.color = tipTextColor.Value;
                    }
                }
                else
                {
                    Text text = Instantiate(__instance.m_loading.transform.Find("Text").GetComponent<Text>(), __instance.m_loading.transform);
                    text.text = loadingText.Value;
                    text.color = loadingTextColor.Value;
                }
                __instance.m_loading.transform.Find("Text").gameObject.SetActive(false);
            }
        }
        [HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(ZNet), "RPC_ClientHandshake")]
        public static class ZNet_RPC_ClientHandshake_Patch
        {
            public static bool Prefix(ZNet __instance, ZRpc rpc, bool needPassword)
            {
                Dbgl("RPC_ClientHandshake");

                if (!__instance.IsServer())
                {
                    Image image = Instantiate(Hud.instance.transform.Find("LoadingBlack").Find("Bkg").GetComponent<Image>(), Hud.instance.transform.Find("LoadingBlack").transform);
                    if (image == null)
                    {
                        Dbgl($"missed bkg");
                        return true;
                    }
                    Dbgl($"setting sprite to loading screen");

                    image.sprite = loadingSprite;
                    image.color = loadingColorMask.Value;
                    image.type = Image.Type.Simple;
                    image.preserveAspect = true;
                    if (loadingTips.Any())
                    {
                        Instantiate(Hud.instance.m_loadingTip.transform.parent.Find("panel_separator"), Hud.instance.transform.Find("LoadingBlack").transform);
                        Text text = Instantiate(Hud.instance.m_loadingTip.gameObject, Hud.instance.transform.Find("LoadingBlack").transform).GetComponent<Text>();
                        if (text != null)
                        {
                            text.text = loadingTip;
                            text.color = tipTextColor.Value;
                        }
                    }

                }

                return true;
            }
        }
        [HarmonyPatch(typeof(Hud), "UpdateBlackScreen")]
        public static class UpdateBlackScreen_Patch
        {

            public static void Prefix(Hud __instance, bool ___m_haveSetupLoadScreen, ref bool __state)
            {
                __state = !___m_haveSetupLoadScreen;
            }
            public static void Postfix(Hud __instance, bool ___m_haveSetupLoadScreen, ref bool __state)
            {
                if(__state && ___m_haveSetupLoadScreen)
                {
                    Dbgl($"setting sprite to loading screen");

                    __instance.m_loadingImage.sprite = differentSpawnScreen.Value ? loadingSprite2 : loadingSprite;
                    __instance.m_loadingImage.color = spawnColorMask.Value;

                    if (loadingTips.Any())
                    {
                        __instance.m_loadingTip.text = differentSpawnTip.Value ? loadingTip2 : loadingTip;
                    }
                    __instance.m_loadingTip.color = tipTextColor.Value;
                    
                    if (removeVignette.Value)
                    {
                        __instance.m_loadingProgress.transform.Find("TopFade").gameObject.SetActive(false);
                        __instance.m_loadingProgress.transform.Find("BottomFade").gameObject.SetActive(false);
                        __instance.m_loadingProgress.transform.Find("text_darken").gameObject.SetActive(false);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Console), "InputText")]
        static class InputText_Patch
        {
            static bool Prefix(Console __instance)
            {
                if (!modEnabled.Value)
                    return true;
                string text = __instance.m_input.text;
                if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
