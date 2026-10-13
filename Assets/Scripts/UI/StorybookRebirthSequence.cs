using IdleKnightHero.UI;

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class StorybookRebirthSequence : MonoBehaviour
{
    public RectTransform composition,art;
    public CanvasGroup curtain,titleGroup;
    public Text title,subtitle,subjectNumber;
    public Image warmth;
    public AudioSource pageSound;
    public RectTransform[] leaves;
    public float duration=6;
    Vector2[] leafOrigins;
    Vector2 previous;
    bool playing;
    public void Play(Action complete){if(playing)return;playing=true;gameObject.SetActive(true);StartCoroutine(Perform(complete));}
    IEnumerator Perform(Action complete){leafOrigins=new Vector2[leaves.Length];for(int i=0;i<leaves.Length;i++)leafOrigins[i]=leaves[i].anchoredPosition;subjectNumber.text="No. "+GameplayCanvas.single.homeUI.subjectNumber;title.text="旧页终章";subtitle.text="";if(pageSound){pageSound.volume=SoundManager.Instance.getSFXVolume()*.45f;pageSound.Play();}float elapsed=0;while(elapsed<duration){elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/duration);Apply();art.localScale=Vector3.one*Mathf.Lerp(1,1.075f,Mathf.SmoothStep(0,1,t));curtain.alpha=1-Mathf.SmoothStep(0,1,t/.18f);warmth.color=new Color(1,.85f,.60f,Mathf.Sin(t*Mathf.PI)*.10f);if(t>.52f){title.text="故事，再次开始";subtitle.text="第 "+GameManager.single.rebirthLevel+" 次转生 · Lv. 1";}titleGroup.alpha=t<.4f?Mathf.SmoothStep(0,1,(t-.12f)/.12f)*(1-Mathf.SmoothStep(0,1,(t-.32f)/.08f)):Mathf.SmoothStep(0,1,(t-.53f)/.15f);for(int i=0;i<leaves.Length;i++){float phase=i*.71f;leaves[i].anchoredPosition=leafOrigins[i]+new Vector2(Mathf.Sin(t*4+phase)*45,t*(120+i*12));leaves[i].localRotation=Quaternion.Euler(0,0,Mathf.Sin(t*3+phase)*25);leaves[i].GetComponent<CanvasGroup>().alpha=Mathf.Sin(t*Mathf.PI)*.65f;}yield return null;}complete?.Invoke();}
    void Apply(){var size=((RectTransform)transform).rect.size;if(previous==size)return;previous=size;float fit=Mathf.Min(size.x/1080,size.y/1920);composition.localScale=Vector3.one*fit;}
    void OnDisable(){StopAllCoroutines();playing=false;}
}
