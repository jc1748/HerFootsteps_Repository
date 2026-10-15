using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HerFootsteps.Editor
{
    // Explicit local verification requests only; never runs setup on load or synthesizes input.
    [InitializeOnLoad]
    public static class Milestone6LiveCheck
    {
        static Milestone6LiveCheck()=>EditorApplication.update+=Poll;
        private static void Poll()
        {
            const string path="Library/HerFootsteps/Milestone6Live.request";
            if(!EditorApplication.isPlaying || EditorApplication.isCompiling || !File.Exists(path))return;
            string command=File.ReadAllText(path).Trim();File.Delete(path);
            if(SceneManager.GetActiveScene().path!=Milestone6Setup.ScenePath)return;
            var player=Object.FindAnyObjectByType<FirstPersonMotor>();
            if(command=="prepare-battery")
            {
                var pickup=Object.FindObjectsByType<InventoryPickup>(FindObjectsSortMode.None).OrderBy(p=>Vector3.Distance(p.transform.position,player.transform.position)).First();
                player.Teleport(pickup.transform.position+Vector3.back*1.4f-Vector3.up*.7f);
                player.transform.rotation=Quaternion.identity;
                // Reset the motor's stored pitch, then aim through its existing look API.
                player.ApplyLook(new Vector2(0,10000));player.ApplyLook(new Vector2(0,-850));
                var camera=player.GetComponentInChildren<Camera>();
                float pitch=Vector3.SignedAngle(Vector3.forward,pickup.transform.position-camera.transform.position,Vector3.right);
                player.ApplyLook(new Vector2(0,-pitch/.1f));
                Physics.SyncTransforms();player.GetComponent<PlayerInteractor>().RefreshTarget();
            }
            var inventory=player.GetComponent<PlayerInventory>();var hud=player.GetComponent<PresentationHud>();var input=player.GetComponent<PlayerInputReader>();var flashlight=player.GetComponent<PlayerFlashlight>();
            Directory.CreateDirectory("Logs");
            File.AppendAllText("Logs/Milestone6-live.txt",$"{command}: control={input.HasControl}; locked={Cursor.lockState}; selected={hud.Selected+1}; quantities={string.Join(",",inventory.Slots.Select(s=>s.Quantity))}; battery={flashlight.Battery:0.0}; light={flashlight.IsActive}; target={player.GetComponent<PlayerInteractor>().Target?.name}; position={player.transform.position}; hunt={Object.FindAnyObjectByType<HuntController>().State}\n");
        }
    }
}
