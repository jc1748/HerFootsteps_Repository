using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace HerFootsteps.Editor
{
    // Explicit Editor-only requests for observing the real Game view. Never saved into gameplay.
    [InitializeOnLoad]
    public static class Milestone7CLiveCheck
    {
        static Milestone7CLiveCheck()=>EditorApplication.update+=Poll;
        private static void Poll()
        {
            const string path="Library/HerFootsteps/Milestone7CLive.request";
            if(!File.Exists(path)||EditorApplication.isCompiling||!EditorApplication.isPlaying)return;
            string command=File.ReadAllText(path).Trim();File.Delete(path);
            if(SceneManager.GetActiveScene().path!=Milestone7BSetup.ScenePath)return;
            try
            {
                var player=Object.FindAnyObjectByType<FirstPersonMotor>();var comp=player.GetComponent<PlayerComposure>();var camera=player.GetComponentInChildren<Camera>();
                var director=Object.FindAnyObjectByType<HallucinationDirector>();var flashlight=player.GetComponent<PlayerFlashlight>();
                var events=Object.FindObjectsByType<HallucinationEvent>(FindObjectsSortMode.None);
                if(command=="prepare")
                {
                    director.enabled=false;player.enabled=false;
                    foreach(var e in events)e.Ended+=RecordEnd;
                    foreach(var rate in Object.FindObjectsByType<ComposureRateSource>(FindObjectsSortMode.None))rate.enabled=false;
                    comp.Apply(100-comp.Current,"Editor visual check");
                    foreach(var e in events)e.Tick(80);
                    comp.Apply(-75,"Editor visual check");
                    // Follow existing designer route points, selecting a physically valid spawn through the real director.
                    var points=Object.FindObjectsByType<BlockoutRoute>(FindObjectsSortMode.None).SelectMany(r=>r.GetComponentsInChildren<Transform>().Skip(1));
                    foreach(var p in points)
                    {
                        player.Teleport(p.position+Vector3.up*.2f);camera.transform.rotation=Quaternion.LookRotation(Vector3.forward);Physics.SyncTransforms();
                        if(director.Request(3))break;
                    }
                    comp.Apply(75,"End staging encounter");foreach(var e in events)e.Tick(80);comp.Apply(-75,"Editor visual check");
                    if(flashlight.IsActive)flashlight.Toggle();Time.timeScale=.1f;
                }
                else if(command=="light") {flashlight.RestoreBattery(100);if(!flashlight.IsActive)flashlight.Toggle();}
                else if(command=="dark") {if(flashlight.IsActive)flashlight.Toggle();}
                else if(command=="fog-off")RenderSettings.fog=false;
                else if(command=="fog-on")Object.FindAnyObjectByType<ForestAtmosphere>().Apply();
                else if(command=="whisper")director.Request(0);
                else if(command=="steps")director.Request(1);
                else if(command=="wildlife")director.Request(2);
                else if(command=="beast")director.Request(3);
                else if(command=="dismiss")
                {var beast=Object.FindAnyObjectByType<SanityBeastHallucination>();camera.transform.LookAt(beast.transform.position+Vector3.up*1.1f);flashlight.RestoreBattery(100);if(!flashlight.IsActive)flashlight.Toggle();Time.timeScale=1;}
                else if(command=="reset")
                {comp.Apply(100-comp.Current,"Editor recover");foreach(var e in events)e.Tick(80);comp.Apply(-75,"Editor low composure");Time.timeScale=.1f;}
                else if(command=="hunt")
                {
                    comp.Apply(100-comp.Current,"Editor recover");
                    var hunt=Object.FindAnyObjectByType<HuntController>();hunt.Tick(100,true);hunt.TryRequestHunt(player.transform.position,"Editor light review");hunt.Tick(5,true);
                    camera.transform.LookAt(hunt.Cryptid.transform.position+Vector3.up*1.3f);
                }
                else if(command=="debug")player.GetComponent<PresentationDebugMode>().SetVisible(true);
                else if(command=="normal-time")Time.timeScale=1;
                else if(command=="stop"){Time.timeScale=1;EditorApplication.isPlaying=false;}
                else if(command.StartsWith("capture-"))
                {Directory.CreateDirectory("Logs/Milestone7C-visuals");ScreenCapture.CaptureScreenshot("Logs/Milestone7C-visuals/"+command.Substring(8)+".png");}
                File.AppendAllText("Logs/Milestone7C-live.txt",$"{DateTime.Now:HH:mm:ss} {command}: player={player.transform.position}, composure={comp.Current:F1}, light={flashlight.IsActive}, fog={RenderSettings.fog}, {director.PlacementStatus}; "+string.Join("; ",events.Select(e=>$"{e.name}={e.Status}/{e.IsActive}"))+"\n");
                if(new[]{"beast","wildlife","hunt","fog-on","fog-off","debug"}.Contains(command))
                {Directory.CreateDirectory("Logs/Milestone7C-visuals");ScreenCapture.CaptureScreenshot("Logs/Milestone7C-visuals/"+command+".png");}
            }
            catch(Exception error){Debug.LogException(error);File.AppendAllText("Logs/Milestone7C-live.txt",error+"\n");}
        }
        private static void RecordEnd(HallucinationEvent e,HallucinationEnd reason)=>File.AppendAllText("Logs/Milestone7C-live.txt",$"{DateTime.Now:HH:mm:ss} LIVE END {e.name}: {reason}\n");
    }
}
