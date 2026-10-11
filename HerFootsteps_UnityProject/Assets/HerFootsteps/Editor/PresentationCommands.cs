using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HerFootsteps.Editor
{
    [InitializeOnLoad]
    public static class PresentationCommands
    {
        static PresentationCommands() => EditorApplication.update += Poll;
        private static void Poll()
        {
            const string request = "Library/HerFootsteps/Presentation.request";
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(request)) return;
            string command = File.ReadAllText(request).Trim(); if (command.Length == 0) return; File.Delete(request);
            try
            {
                if (command == "import") AssetDatabase.ImportPackage("Assets/Forst/Conifers [BOTD]/Conifers_URP.unitypackage", false);
                else if (command == "tests") Milestone1Setup.RunTests();
                else if (command == "playtests") Milestone1Setup.RunPlayTests();
                else if (command == "open") EditorSceneManager.OpenScene("Assets/Scenes/Milestone5_Presentation.unity");
                else EditorApplication.ExecuteMenuItem("Her Footsteps/Presentation/" + command);
            }
            catch (Exception error) { Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/Presentation-error.txt", error.ToString()); Debug.LogException(error); }
        }
    }
}
