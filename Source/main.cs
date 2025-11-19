// Usage:
// Make a config file in GameData/YourMod/MusicDefinitions.cfg  (replace YourMod with your mod folder name)
// The config file should contain:
// BACKGROUND_MUSIC
// {
//     type = WAV
//     path = YourMod/PathToFile/YourAudioFile.wav
//     planet = PlanetName
//     biome = BiomeName
// }
// Replace PlanetName with the actual name of the planet (e.g., Kerbin).
// Replace BiomeName with the name of the biome (do not include parameter if you want to make the music heard everywhere around the planet)
// Replace path with the relative path to the audio file within GameData, such as Almajara-Core/Music/1.wav
// Multiple BACKGROUND_MUSIC entries can be made in the same config file for different planets and audio files.
// Only one audio should be specified per planet, because multiple songs for the same planet would all play at once.
// The audio file must be in WAV format, otherwise music won't play.
// The mod will play the specified audio file as background music when the vessel is over the specified planet.
// Music types:
// WAV - Default, loads up an external file. Requires "path".
// BUILTIN/VAB - Uses the built-in music for the VAB.
// BUILTIN/SPH - Uses the built-in music for the SPH.
// BUILTIN/TrackingStation - Uses the built-in music for the tracking station.
// BUILTIN/SpaceCenterDay - Uses the built-in music for the Space Center during the day.
// BUILTIN/SpaceCenterNight - Uses the built-in music for the Space Center during the night.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;
using UnityEngine;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;

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
        public string type;
        public string biome;

        public List<GameObject> musicObjects = new List<GameObject>();  // List of GameObjects used for music

        public void Start()
        {
            music = MusicLogic.fetch;
            stockPlaylist = music.spacePlaylist;
            emptySongsList = new List<AudioClip> { AudioClip.Create("none", 44100, 1, 44100, false) };

            string[] foldersMods = Directory.GetDirectories(KSPUtil.ApplicationRootPath + "GameData");
            foreach (string modPath in foldersMods)
            {
                if (File.Exists(modPath + "/MusicDefinitions.cfg"))
                {
                    foreach (ConfigNode node in ConfigNode.Load(modPath + "/MusicDefinitions.cfg").GetNodes("BACKGROUND_MUSIC"))
                    {
                        // Create the game object
                        gameObject2 = new GameObject();
                        GameObject.DontDestroyOnLoad(gameObject2);
                        musicObjects.Add(gameObject2);

                        // Get the nodes
                        planet = node.GetValue("planet");
                        type = node.GetValue("type");
                        if (type == "WAV")
                            path = node.GetValue("path");
                        biome = node.GetValue("biome");

                        // Rename the object
                        if (biome == null)
                        {
                            gameObject2.name = "ConfigMusic" + "|" + planet + "|*";
                        }
                        else
                        {
                            gameObject2.name = "ConfigMusic" + "|" + planet + "|" + biome;
                        }

                        // Create the audio source
                        source = gameObject2.AddComponent<AudioSource>();

                        // Audio achieves god mode
                        source.spatialBlend = 0;
                        source.dopplerLevel = 0;
                        source.loop = false;

                        // Disable stock music
                        music.audio1.Stop();
                        music.spacePlaylist = emptySongsList;

                        // Get audio if needed
                        if (type == "WAV" || type == "")
                        {
                            StartCoroutine(GetAudioClip(source));
                        }
                        else
                        {
                            if (type == "BUILTIN/VAB")
                                source.clip = null;

                            source.loop = true;
                            source.time = 0;
                        }
                    }
                }
            }
        }

        IEnumerator GetAudioClip(AudioSource source) // Gets the audio clip from file
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file:///" + KSPUtil.ApplicationRootPath + "GameData/" + path, AudioType.WAV))
            {
                yield return www.SendWebRequest();

                source.clip = DownloadHandlerAudioClip.GetContent(www);
                source.loop = true;
                source.time = 0;
            }
        }

        public void FixedUpdate()
        {
            foreach (GameObject obj in musicObjects)
            {
                source = obj.GetComponent<AudioSource>();

                // Error handling
                if (obj.name.Split('|').Length != 3)
                {
                    UnityEngine.Debug.LogError("ConfigBasedBackgroundMusic: Invalid object name format: " + obj.name);
                    continue;
                }
                if (obj.name.Split('|')[0] != "ConfigMusic")
                {
                    UnityEngine.Debug.LogError("ConfigBasedBackgroundMusic: Invalid object name header: " + obj.name.Split('|')[0]);
                    continue;
                }

                string BODY = obj.name.Split('|')[1];
                string BIOME = obj.name.Split('|')[2];

                if (FlightGlobals.ActiveVessel.mainBody.name == BODY) // Check if the vessel is over the specified planet
                {
                    if (!source.isPlaying)
                    {
                        if (BIOME == "*")
                        {
                            source.Play();  // Play music if over the planet and is not already playing
                            music.audio1.Stop();                   // Disable stock music
                            music.spacePlaylist = emptySongsList;  // Disable stock music
                        }
                        else
                        {
                            if (BIOME == FlightGlobals.ActiveVessel.mainBody.BiomeMap.GetAtt(FlightGlobals.ActiveVessel.latitude * 0.01745329238474369, FlightGlobals.ActiveVessel.longitude * 0.01745329238474369).name)
                            {
                                source.Play();  // Play music if over the planet and is not already playing
                                music.audio1.Stop();                   // Disable stock music
                                music.spacePlaylist = emptySongsList;  // Disable stock music
                            }
                            else
                            {
                                source.Stop();  // Stop music if not over the biome
                                music.audio1.Play();                  // Restore stock music
                                music.spacePlaylist = stockPlaylist;  // Restore stock music
                            }
                        }
                    }
                }
                else
                {
                    source.Stop();  // Stop music if not over the planet
                    music.audio1.Play();                  // Restore stock music
                    music.spacePlaylist = stockPlaylist;  // Restore stock music
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