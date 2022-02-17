using IPA;
using IPALogger = IPA.Logging.Logger;

using BS_Utils.Gameplay;
using BS_Utils.Utilities;

using TMPro;
using UnityEngine;

using System;
using System.Net;
using System.Threading;

using HarmonyLib;

namespace PPGains
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static Plugin Instance { get; private set; }
        internal static IPALogger Log { get; private set; }


        [Init]
        public Plugin(IPALogger logger)
        {
            Instance = this;
            Log = logger;
        }


        static float ppRef = 0;
        static float rankRef = 0;

        static TMP_Text text;
        static WebClient webClient;
        static BS_Utils.Utilities.Config config;

        [OnStart]
        public void OnApplicationStart()
        {
            //Plugin.Log.Info("meow");

            Harmony harmony = new Harmony("Catse.BeatSaber.PPGains");
            harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());

            webClient = new WebClient();
            config = new BS_Utils.Utilities.Config("PPGains");

            int currentHour = (int)(DateTimeOffset.Now.ToUnixTimeSeconds() / 3600);
            int cfgHour = config.GetInt("PPGains", "timestamp");
            if (cfgHour > 0 && (currentHour - cfgHour) < 4)
            {
                ppRef = config.GetFloat("PPGains", "pp");
                rankRef = config.GetFloat("PPGains", "rank");
            }

            BSEvents.menuSceneLoaded += OnMenuSceneLoaded;
            

        }

        void OnMenuSceneLoaded()
        {
            new Thread(new ThreadStart(PPThread)).Start();
        }

        static void PPThread()
        {
            text.text = "PP Gains";
            Thread.Sleep(3000);
            string res = webClient.DownloadString("https://scoresaber.com/api/player/" + GetUserInfo.GetUserID() + "/full");
            float pp = (float)Convert.ToDouble(res.Substring(res.IndexOf("\"pp\":") + 6, 16).Split(',')[0], System.Globalization.CultureInfo.InvariantCulture);
            float rank = (float)Convert.ToDouble(res.Substring(res.IndexOf("\"rank\":") + 8, 16).Split(',')[0], System.Globalization.CultureInfo.InvariantCulture);
            if (ppRef == 0)
            {
                ppRef = pp;
                rankRef = rank;
                config.SetFloat("PPGains", "pp", pp);
                config.SetFloat("PPGains", "rank", rank);
            }
            text.text = "PP Gained: " + (pp - ppRef).ToString("0.00") + ", Ranks: " + (rankRef - rank);

            config.SetInt("PPGains", "timestamp", (int)(DateTimeOffset.Now.ToUnixTimeSeconds() / 3600));

        }


        [HarmonyPatch(typeof(LevelStatsView))]
        [HarmonyPatch("ShowStats", MethodType.Normal)]
        class LevelStatsViewPatches : LevelStatsView
        {
            
            static void Postfix(ref LevelStatsViewPatches __instance)
            {
                if (text == null)
                {
                    text = BeatSaberMarkupLanguage.BeatSaberUI.CreateText(__instance.transform as RectTransform, "PP Gains", new Vector2(0, 0));
                    text.fontSize = 4f;
                    text.color = new Color(1f, 1f, 1f, 1f);
                    text.rectTransform.position += new Vector3(-0.35f, 1.95f, 0);
                    new Thread(new ThreadStart(PPThread)).Start();
                }
            }
        }





        [OnExit]
        public void OnApplicationQuit()
        {

        }
    }


}
