using System;
using UnityEngine;

namespace HerFootsteps
{
    // Attention resource, not a replacement for spatial NoiseChannel events or AI hearing.
    public sealed class NoiseExposure : MonoBehaviour
    {
        [SerializeField] private NoiseChannel channel;
        [SerializeField] private HuntController hunt;
        [SerializeField] private Transform player;
        [Header("Provisional exposure per player event")]
        [SerializeField, Min(1)] private float threshold = 100;
        [SerializeField, Min(0)] private float walking = .4f, sprinting = 2, environment = 20;
        [SerializeField, Min(0)] private float breathing = 1, forcedBreath = 18, scream = 100;
        [SerializeField] private float current;
        [SerializeField] private bool locked;
        [SerializeField] private string status = "Waiting";
        public float Current => current;
        public float Threshold => Mathf.Max(1, threshold);
        public bool Locked => locked;
        public string Status => status;
        public event Action Changed;
        private void OnEnable()
        {
            current=0; locked=false;
            if(channel!=null)channel.Emitted+=OnNoise;
            if(hunt!=null){hunt.StateChanged+=OnState;OnState(hunt.State);}
        }
        private void OnDisable()
        {
            if(channel!=null)channel.Emitted-=OnNoise;
            if(hunt!=null)hunt.StateChanged-=OnState;
        }
        private void OnState(HuntState state)
        {
            if(state==HuntState.Warning||state==HuntState.Active||state==HuntState.Failed)
            {locked=true;current=Threshold;status="Locked during "+state;}
            else if(state==HuntState.SafePeriod||state==HuntState.Cooldown)
            {locked=false;current=0;status="Suppressed during "+state;}
            else status="Accumulating; no passive decay";
            Changed?.Invoke();
        }
        private void OnNoise(NoiseEvent sound)
        {
            if(player==null||hunt==null||locked)return;
            if(sound.Instigator!=player.gameObject && (sound.Source==null||!sound.Source.transform.IsChildOf(player)))return;
            // Safe/cooldown events are discarded, not banked for an automatic later hunt.
            if(hunt.State!=HuntState.Exploration)return;
            float amount=sound.Kind switch
            {NoiseKind.Walking=>walking,NoiseKind.Sprinting=>sprinting,NoiseKind.Environment=>environment,
             NoiseKind.Breathing=>breathing,NoiseKind.ForcedBreath=>forcedBreath,NoiseKind.Scream=>scream,_=>0};
            current=Mathf.Clamp(current+Mathf.Max(0,amount),0,Threshold);
            status=sound.Kind+" +"+amount.ToString("0.0");Changed?.Invoke();
            if(current<Threshold)return;
            if(!hunt.TryRequestHunt(sound.Position,"Noise exposure: "+sound.Kind))
            {current=0;locked=false;status="Request discarded: "+hunt.LastGateResult;Changed?.Invoke();}
        }
    }
}
