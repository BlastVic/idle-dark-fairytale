using System;
using System.Linq;
using UnityEngine;
using Spine.Unity;

namespace RedHoodPrototype {
    // Independent preview; all controls are authored in RedHoodPreviewHUD.prefab.
    public sealed class RedHoodAnimationPreview : MonoBehaviour {
        public SkeletonAnimation character;
        public RedHoodPreviewHud hud;
        public Camera previewCamera;
        public SpriteRenderer backdrop;
        public Bounds motionBounds;
        [Range(.1f,2f)] public float speed=1;
        public bool autoAttack, paused, loop, autoCycle, returnToIdle=true;
        public float zoom=1;
        public string Selected {get;private set;}="Idle1";
        public string[] AnimationNames {get;private set;}=Array.Empty<string>();
        public int HitCount {get;private set;}
        public int CompleteCount {get;private set;}
        public float Timeline => Current==null ? 0 : Current.AnimationTime;
        public float Duration => Current==null ? 0 : Current.Animation.Duration;
        public string Playing => Current==null ? "—" : Current.Animation.Name;
        public float EffectiveSpeed => speed*((Playing=="Death1" || Playing=="Revive" || Playing=="Skill2" || Playing=="Skill3") ? 1.2f : 1);
        public Spine.TrackEntry Current => character && character.AnimationState!=null ? character.AnimationState.GetCurrent(0) : null;
        bool inspecting, finished, completionHandled;float finishDelay;
        void Start() {
            if(!character){enabled=false;return;}
            character.Initialize(false);
            AnimationNames=character.Skeleton.Data.Animations.Select(a=>a.Name).ToArray();
            character.AnimationState.Event+=OnEvent;
            character.AnimationState.Complete+=OnComplete;
            if(hud)hud.Bind(this);
            Play("Idle1");
        }
        void OnDestroy(){if(character && character.AnimationState!=null){character.AnimationState.Event-=OnEvent;character.AnimationState.Complete-=OnComplete;}}
        void OnEvent(Spine.TrackEntry entry,Spine.Event e){if(inspecting||entry!=Current)return;if(e.Data.Name=="OnHit")HitCount++;if(e.Data.Name=="OnComplete")CompleteCount++;}
        void OnComplete(Spine.TrackEntry entry){if(!inspecting&&entry==Current&&!entry.Loop){finished=true;completionHandled=true;finishDelay=.12f;}}
        public void Select(string name){autoAttack=false;autoCycle=false;Play(name);}
        public void Play(string name){
            if(character.Skeleton.Data.FindAnimation(name)==null)return;
            Selected=name;paused=false;inspecting=false;finished=false;completionHandled=false;HitCount=CompleteCount=0;
            character.timeScale=speed;
            var entry=character.AnimationState.SetAnimation(0,name,loop || name=="Idle1" || name=="VictoryIdle");
            entry.TimeScale=(name=="Death1" || name=="Revive" || name=="Skill2" || name=="Skill3") ? 1.2f : 1;
            character.Update(0);character.LateUpdate();
        }
        public void Replay(){Play(Selected);}
        public void SetPaused(bool value){paused=value;inspecting=false;character.timeScale=paused ? 0 : speed;}
        public void SetLoop(bool value){loop=value;if(Current!=null)Current.Loop=value;finished=false;}
        public void SetCycle(bool value){autoCycle=value;if(value){autoAttack=false;loop=false;Play(Selected);if(Current!=null)Current.Loop=false;}}
        public void SetAutoAttack(bool value){autoAttack=value;if(value){autoCycle=false;loop=false;Play("Attack1");}}
        public void Seek(float seconds){
            paused=true;inspecting=true;finished=false;completionHandled=false;character.timeScale=0;
            string clip=Playing=="—"?Selected:Playing;
            character.AnimationState.ClearTracks();character.Skeleton.SetToSetupPose();
            var entry=character.AnimationState.SetAnimation(0,clip,false);entry.MixDuration=0;entry.TimeScale=(clip=="Death1" || clip=="Revive" || clip=="Skill2" || clip=="Skill3") ? 1.2f : 1;
            entry.TrackTime=Mathf.Clamp(seconds,0,entry.Animation.Duration);entry.AnimationLast=entry.TrackTime;
            character.AnimationState.Apply(character.Skeleton);character.Skeleton.UpdateWorldTransform();character.LateUpdate();
        }
        public void Step(float direction){Seek(Timeline+direction/60f);}
        public void ResetView(){zoom=1;}
        void Update(){
            if(!character||Current==null)return;character.timeScale=paused?0:speed;
            if(paused)return;
            if(!completionHandled&&!Current.Loop&&Current.IsComplete){completionHandled=true;finished=true;finishDelay=.12f;}
            // Cycling must also leave intrinsically looping Idle clips.
            if(autoCycle && Current.TrackTime>=Current.Animation.Duration){finished=true;finishDelay=0;Current.Loop=false;}
            if(!finished)return;finishDelay-=Time.unscaledDeltaTime;if(finishDelay>0)return;finished=false;
            if(autoAttack){Play(Playing=="Attack1"?"Attack2":"Attack1");return;}
            if(autoCycle){var index=Array.IndexOf(AnimationNames,Selected);Play(AnimationNames[(index+1)%AnimationNames.Length]);Current.Loop=false;return;}
            if(Playing=="Victory" && character.Skeleton.Data.FindAnimation("VictoryIdle")!=null){Transition("VictoryIdle");return;}
            if(returnToIdle && Playing!="Idle1" && Playing!="VictoryIdle" && Playing!="Death1" && Playing!="Rebirth")Transition("Idle1");
        }
        void Transition(string name){var e=character.AnimationState.SetAnimation(0,name,true);e.TimeScale=1;}
        void LateUpdate(){
            if(!previewCamera||!hud||!hud.previewArea)return;
            var corners=new Vector3[4];hud.previewArea.GetWorldCorners(corners);
            var a=previewCamera.ScreenToWorldPoint(new Vector3(corners[0].x,corners[0].y,20));
            var b=previewCamera.ScreenToWorldPoint(new Vector3(corners[2].x,corners[2].y,20));
            float fit=Mathf.Min((b.x-a.x)/Mathf.Max(.01f,motionBounds.size.x),(b.y-a.y)/Mathf.Max(.01f,motionBounds.size.y))*.92f*zoom;
            character.transform.localScale=Vector3.one*fit;
            character.transform.position=new Vector3((a.x+b.x)/2-motionBounds.center.x*fit,a.y-motionBounds.min.y*fit,0);
            if(backdrop&&backdrop.sprite){float h=2*previewCamera.orthographicSize;backdrop.transform.localScale=Vector3.one*Mathf.Max(h/backdrop.sprite.bounds.size.y,h*previewCamera.aspect/backdrop.sprite.bounds.size.x);backdrop.transform.position=Vector3.zero;}
        }
    }
}
