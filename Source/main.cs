// Usage:
// Make a config file in one of these locations:
// GameData/YourMod/Patches/MusicDefinitions.cfg
// GameData/YourMod/Misc/MusicDefinitions.cfg
// GameData/YourMod/MiscConfigs/MusicDefinitions.cfg
// GameData/YourMod/Configs/MusicDefinitions.cfg
// The config file should contain:
// BACKGROUND_MUSIC
// {
//     path = YourMod/PathToFile/YourAudioFile.wav
//     planet = PlanetName
// }
// The audio file should be in WAV format.
// The mod will play the specified audio file as background music when the vessel is over the specified planet.
// Replace PlanetName with the actual name of the planet (e.g., Kerbin).
// Replace path with the relative path to the audio file within GameData, such as Almajara-Core/Music/1.wav

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;
using UnityEngine;
using KSP.UI.Screens.DebugToolbar.Screens.Cheats;

namespace ConfigBasedBackgroundMusic
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class BackgroundAudioConfig : MonoBehaviour
    {
        private static MusicLogic music = null;
        protected static AudioSource source = null;
        static GameObject gameObject2 = null;
        protected static List<AudioClip> emptySongsList = null;
        protected static List<AudioClip> stockPlaylist = null;
        public static AudioClip myClip = null;
        public ConfigNode modules;

        public string path;
        public string planet;

        public void Start()
        {
            string[] foldersMods = Directory.GetDirectories(KSPUtil.ApplicationRootPath + "GameData");
            foreach (string modPath in foldersMods)
            {
                if (File.Exists(modPath + "/MusicDefinitions.cfg"))
                {
                    modules = ConfigNode.Load(modPath + "/MusicDefinitions.cfg");
                    break;
                }
                if (File.Exists(modPath + "/Patches/MusicDefinitions.cfg"))
                {
                    modules = ConfigNode.Load(modPath + "/Patches/MusicDefinitions.cfg");
                    break;
                }
                if (File.Exists(modPath + "/Misc/MusicDefinitions.cfg"))
                {
                    modules = ConfigNode.Load(modPath + "/Misc/MusicDefinitions.cfg");
                    break;
                }
                if (File.Exists(modPath + "/MiscConfigs/MusicDefinitions.cfg"))
                {
                    modules = ConfigNode.Load(modPath + "/MiscConfigs/MusicDefinitions.cfg");
                    break;
                }
                if (File.Exists(modPath + "/Configs/MusicDefinitions.cfg"))
                {
                    modules = ConfigNode.Load(modPath + "/Configs/MusicDefinitions.cfg");
                    break;
                }
            }

            if (modules != null)
            {
                // Create game object for music
                gameObject2 = new GameObject();
                GameObject.DontDestroyOnLoad(gameObject2);
                gameObject2.name = "ConfigBasedMusicObject";

                Debug.LogError(modules.GetNodes());
                foreach (ConfigNode node in modules.GetNodes("BACKGROUND_MUSIC"))
                {
                    path = node.GetValue("path");
                    planet = node.GetValue("planet");
                }

                // Create audio source
                source = gameObject2.AddComponent<AudioSource>();

                // Audio achieves god mode
                source.spatialBlend = 0;
                source.dopplerLevel = 0;
                source.loop = false;

                // Disable stock music
                music = MusicLogic.fetch;
                emptySongsList = new List<AudioClip>();
                emptySongsList.Add(AudioClip.Create("none", 44100, 1, 44100, false));
                music.audio1.Stop();
                stockPlaylist = music.spacePlaylist;
                music.spacePlaylist = emptySongsList;

                // Get audio
                StartCoroutine(GetAudioClip());

                // Set up audio
                source.clip = myClip;
                source.loop = true;
                source.time = 0;
            }
        }

        IEnumerator GetAudioClip() // Gets the audio clip from file
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file:///"+ KSPUtil.ApplicationRootPath + "GameData/" + path, AudioType.WAV))
            {
                yield return www.SendWebRequest();

                myClip = DownloadHandlerAudioClip.GetContent(www);
            }
        }

        public void FixedUpdate()
        {
            // Add clip if not added (happens when the request doesn't complete fast enough)
            if (source.clip == null)
                source.clip = myClip;

            // Does planet exist?
            CelestialBody BODYNAME = null;
            foreach (var b in FlightGlobals.Bodies)
            {
                if (b.name == planet)
                {
                    BODYNAME = b;
                    break;
                }
            }

            //If Sorut exists, play music when over Sorut
            if (BODYNAME != null)
            {
                if (FlightGlobals.ActiveVessel.mainBody == BODYNAME) // Is over Sorut?
                {
                    if (!source.isPlaying) source.Play();  // Play music if not already playing
                }
                else
                {
                    source.Stop();  // Stop music if not over Sorut
                    music.spacePlaylist = stockPlaylist;  // Restore stock music
                }
            }
        }

        public void onDestroy()
        {
            // Clean up when exiting flight scene
            gameObject2.DestroyGameObject();
            source.Stop();
            music.spacePlaylist = stockPlaylist;
        }
    }
}
