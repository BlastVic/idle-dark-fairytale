using UnityEngine;

// A standalone, authored battle workbench. No campaign scene, UI, waves or save bootstrap.
[DefaultExecutionOrder(-1000)]
public sealed class SkillTestStage : MonoBehaviour
{
    public AssetManager assets;
    public Player player;
    public Enemy[] targets;
    public Camera battleCamera;
    public DarkFairytaleBackdrop backdrop;
    void Awake()
    {
        player.ab=player.GetComponent<Actor_Base>();
        player.manualControl=true;player.isCampPlayer=false;
        player.ab.currentStat=new Stat{hpMax=1000,hpNow=600,dmg=100,atkSpd=100,critDmg=150};
        foreach(var enemy in targets){
            var actor=enemy.GetComponent<Actor_Base>();actor.trainingTarget=true;
            actor.baseStat=new Stat{hpMax=100,hpNow=40,atkSpd=100,critDmg=150};
            actor.currentStat=Utilities.DeepClone(actor.baseStat);
        }
        backdrop.Initialize(battleCamera);
    }
    public void Place()
    {
        assets.battleStyle.PlacePlayer(player,battleCamera);
        player.skeletonAnimation.AnimationState.SetAnimation(0,"Idle1",true);
        for(int i=0;i<targets.Length;i++){
            assets.battleStyle.PlaceEnemy(targets[i],battleCamera,i);
            targets[i].skeletonAnimation.AnimationState.SetAnimation(0,"Idle1",true);
        }
    }
}
