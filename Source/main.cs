// Usage:
// Make a config file in GameData/YourMod/MusicDefinitions.cfg  (replace YourMod with your mod folder name)
// The config file should contain:
// BACKGROUND_MUSIC
// {
//     path = YourMod/PathToFile/YourAudioFile.wav
//     planet = PlanetName
// }
// Replace PlanetName with the actual name of the planet (e.g., Kerbin).
// Replace path with the relative path to the audio file within GameData, such as Almajara-Core/Music/1.wav
// Multiple BACKGROUND_MUSIC entries can be made in the same config file for different planets and audio files.
// Only one audio should be specified per planet.
// The audio file should be in WAV format.
// The mod will play the specified audio file as background music when the vessel is over the specified planet.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;
using UnityEngine;

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
        public List<GameObject> musicObjects = new List<GameObject>();  // List of GameObjects used for music

        public void Start()
        {
            string[] foldersMods = Directory.GetDirectories(KSPUtil.ApplicationRootPath + "GameData");
            foreach (string modPath in foldersMods)
            {
                if (File.Exists(modPath + "/MusicDefinitions.cfg"))
                {
                    gameObject2 = new GameObject();
                    GameObject.DontDestroyOnLoad(gameObject2);
                    musicObjects.Add(gameObject2);

                    foreach (ConfigNode node in ConfigNode.Load(modPath + "/MusicDefinitions.cfg").GetNodes("BACKGROUND_MUSIC"))
                    {
                        path = node.GetValue("path");
                        planet = node.GetValue("planet");
                    }
                    gameObject2.name = "ConfigMusic"+planet;

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

                    // Get audio and assign it to source
                    StartCoroutine(GetAudioClip(source));
                }
            }
        }

        IEnumerator GetAudioClip(AudioSource source) // Gets the audio clip from file
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file:///"+ KSPUtil.ApplicationRootPath + "GameData/" + path, AudioType.WAV))
            {
                yield return www.SendWebRequest();

                source.clip = DownloadHandlerAudioClip.GetContent(www); ;
                source.loop = true;
                source.time = 0;
            }
        }

        public void FixedUpdate()
        {
            foreach (GameObject obj in musicObjects)
            {
                source = obj.GetComponent<AudioSource>();

                CelestialBody BODYNAME = null;
                foreach (var b in FlightGlobals.Bodies)
                {
                    if ("ConfigMusic"+b.name == obj.name)
                    {
                        BODYNAME = b;
                        break;
                    }
                }

                if (BODYNAME != null)
                {
                    if (FlightGlobals.ActiveVessel.mainBody == BODYNAME) // Check if the vessel is over the specified planet
                    {
                        if (!source.isPlaying) source.Play();  // Play music if over the planet and is not already playing
                    }
                    else
                    {
                        source.Stop();  // Stop music if not over the planet
                        music.spacePlaylist = stockPlaylist;  // Restore stock music
                    }
                }
            }
        }

        public void onDestroy()
        {
            // Clean up when exiting flight scene
            foreach (GameObject obj in musicObjects)
            {
                obj.GetComponent<AudioSource>().Stop();
                obj.DestroyGameObject();
            }
            music.spacePlaylist = stockPlaylist;
        }
    }
}
