﻿using UnityEditor;
using UnityEngine;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;

using Debug = UnityEngine.Debug;

// -=-=-=- //

namespace DefaultNamespace
{
    public class LaunchGame : MonoBehaviour
    {
        private const string SteamRunGamePath = "steam://rungameid/248970";

        static LaunchGame()
        {
            BuildMap.MapBuilt += OnMapBuilt;
        }

        private static void OnMapBuilt()
        {
            RunGame();
        }

        [MenuItem("Vectorier/Launch/Run Game %#R")]
        private static void RunGame()
        {
            string gameExecutablePath;

            if (VectorierSettings.UseShortcutLaunch)
            {
                gameExecutablePath = VectorierSettings.GameShortcutPath ?? SteamRunGamePath;
            }
            else
            {
                gameExecutablePath = Path.Combine(VectorierSettings.GameDirectory, "Vector.exe") ?? SteamRunGamePath;
            }

            if (string.IsNullOrEmpty(gameExecutablePath))
            {
                Debug.LogWarning("Game executable path is not set! Please set it in the Project setting.");
                return;
            }
            try
            {
                var gameProcess = new Process
                {
                    StartInfo = {
                        FileName = gameExecutablePath
                    },
                    EnableRaisingEvents = true
                };

                gameProcess.Exited += (sender, args) => {
                    Debug.Log("Game exited.");
                };

                gameProcess.Start();
            }
            catch (Win32Exception)
            {
                Debug.LogError($"Cannot run the game from path: \"{gameExecutablePath}!\"");
            }

        }

        [MenuItem("Vectorier/Launch/Build and Run Game (Fast) %#&R")]
        public static void BuildAndRun()
        {
            // Set the flag before building
            BuildMap.IsBuildForRunGame = true;
            BuildMap.Build(false, true);
            RunGame();
        }
    }
}