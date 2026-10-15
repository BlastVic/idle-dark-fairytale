using System;
using System.Collections;
using Assets.Scripts.Services;
using IdleKnightHero.UI;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Six saved illustrated pages, read at the opening camera hold before the live home reveal.</summary>
[DisallowMultipleComponent]
public sealed class CursedBookPrologue : MonoBehaviour
{
    public const string SeenKey = "Story-Prologue-Read-v1";
    [Serializable] public sealed class Caption { public float start, end; [TextArea] public string text; }
    [Serializable] public sealed class Page {
        public string title;
        public Texture2D illustration;
        [Tooltip("Center of the visible crop, normalized within the original illustration.")]
        public Vector2 focalPoint = new Vector2(.5f, .5f);
        [Tooltip("Narration with optional synchronized action sounds; serialized field name retained for existing prefab compatibility.")]
        public AudioClip narrationAndAmbience;
        public Caption[] captions;
        [Min(0)] public float silentDuration;
    }
    public CursedBookIntro intro;
    public RawImage illustration;
    public Text subtitle;
    public Text openingText;
    public ProloguePageSurface pageSurface;
    public AudioSource narration, pageFoley;
    public AudioSource continuousAmbience;
    float ambienceFade;
    public void SetAmbienceFade(float value) { ambienceFade = Mathf.Clamp01(value); }
    public void StopAmbience() { if (continuousAmbience) continuousAmbience.Stop(); }
    void Update() { if (continuousAmbience) continuousAmbience.volume = Gain * ambienceFade; }
    public AudioClip pageTurnSound;
    public AudioClip coverOpenSound;
    [Range(0,1)] public float coverOpenGain=.65f;
    public Page[] pages;
    [Min(.1f)] public float inkDuration = 1.8f;
    [Min(0)] public float afterNarrationHold = .4f;
    [Min(0)] public float beforeNarrationHold;
    [Range(0,1)] public float narrationGain = 1;
    [Range(0,1)] public float pageTurnGain = .65f;
    [Tooltip("Editor preview only; never changes the saved player's progress.")]
    public bool previewInEditor;
    public bool silentPreview;
    public Caption[] openingCaptions;
    public float openingDuration;
    public AudioClip fullNarration;
    public Caption[] voiceCaptions;
    public float[] pageRevealTimes;
    [Min(.1f)] public float pageTurnDuration=1.6f;
    [Min(.1f)] public float pageInkDuration=.6f;
    public Texture2D openingSketch;
    public float portraitRevealStart=15f, forestRevealStart=19.3f, sketchRevealDuration=1.5f;
    public float openingBookStart=11f, openingBookEnd=14.7f;
    public float openingCameraStart=7.3f, openingCameraEnd=10.7f;
    public bool HasVoicedTimeline => fullNarration && !silentPreview;
    public float VoiceTime { get; private set; }
    public float PageReadTime { get; private set; }
    public bool Opening { get; private set; }
    public bool IsPlaying { get; private set; }
    public bool Reading { get; private set; }
    public bool TurningPage { get; private set; }
    public bool Completed { get; private set; }
    public int PageIndex { get; private set; } = -1;
    public int TurnsCompleted { get; private set; }
    public float InkProgress { get; private set; }
    public Rect SourceUVRect { get; private set; } = new Rect(0, 0, 1, 1);
    Material ink;
    Material originalMaterial;
    bool preview;
    float Gain => SoundManager.Instance ? SoundManager.Instance.getSFXVolume() : 1;

    public bool Paused { get; private set; }
    bool resumeNarration, resumeFoley, resumeAmbience;
    float StoryDelta => Paused ? 0 : Time.unscaledDeltaTime;
    public void SetPaused(bool value) {
        if (Paused == value) return;
        Paused = value;
        if (value) {
            resumeNarration = narration && narration.isPlaying; resumeFoley = pageFoley && pageFoley.isPlaying;
            resumeAmbience = continuousAmbience && continuousAmbience.isPlaying;
            if (resumeAmbience) continuousAmbience.Pause();
            if (resumeNarration) narration.Pause(); if (resumeFoley) pageFoley.Pause();
        } else {
            if (resumeAmbience) continuousAmbience.UnPause();
            if (resumeNarration) narration.UnPause(); if (resumeFoley) pageFoley.UnPause();
        }
    }
    IEnumerator StoryWait(float seconds) { for(float t=0;t<seconds;t+=StoryDelta) yield return null; }
    void Awake() { Hide(); }
    public bool ShouldPlay()
    {
#if UNITY_EDITOR
        preview = previewInEditor || UnityEditor.SessionState.GetBool("PROLOGUE_QA_FORCE", false);
        if (preview) return true;
#endif
        // Existing players retain their normal startup. A new save receives the prologue once.
        return GameManager.single && !GameManager.single.HasSeenThis(SeenKey) &&
               !GameManager.single.HasSeenThis("Tutorial-0");
    }
    bool Ready()
    {
        if (!illustration || !subtitle || !narration || !pageFoley || !pageTurnSound || pages == null || pages.Length == 0) return false;
        foreach (var page in pages)
            if (page == null || !page.illustration || (!silentPreview && !page.narrationAndAmbience)) return false;
        return pageSurface && intro.book.Skeleton.Data.FindAnimation("TurnPage") != null;
    }
    public IEnumerator Play(bool turnLastPage = true, string completionKey = SeenKey)
    {
        if(HasVoicedTimeline && completionKey==SeenKey){yield return PlayVoiced(turnLastPage,completionKey);yield break;}
        if (!Ready()) { Debug.LogError("Prologue: incomplete saved page/audio references.", this); yield break; }
        IsPlaying = true; Completed = false; TurnsCompleted = 0;
        ambienceFade = 1;
        if (continuousAmbience && !silentPreview) continuousAmbience.Play();
        originalMaterial = illustration.material;
        if (originalMaterial && originalMaterial.HasProperty("_Reveal")) {
            ink = new Material(originalMaterial); illustration.material = ink;
        }
        if (completionKey == SeenKey && openingCaptions != null && openingCaptions.Length > 0) {
            Opening = true; PageIndex = -1;
            if(openingText)openingText.gameObject.SetActive(true);
            yield return ReadCaptions(openingCaptions, openingDuration, null);
            Opening = false; subtitle.text = "";
            if(openingText)openingText.gameObject.SetActive(false);
            yield return StoryWait(.7f);
        }
        for (int i = 0; i < pages.Length; i++) {
            PageIndex = i;
            var page = pages[i];
            illustration.texture = page.illustration;
            FitIllustration(page);
            illustration.color = Color.white;
            SetInk(0);
            illustration.gameObject.SetActive(true);
            float t = 0;
            while (t < inkDuration) {
                t = Mathf.Min(inkDuration, t + Mathf.Min(.05f, StoryDelta));
                SetInk(Mathf.SmoothStep(0, 1, t / Mathf.Max(.01f, inkDuration)));
                yield return null;
            }
            SetInk(1);
            // Speech starts only after the complete illustration is visible.
            yield return StoryWait(beforeNarrationHold);
            yield return ReadCaptions(page.captions, page.silentDuration, page.narrationAndAmbience);
            Reading = false; subtitle.text = "";
            yield return StoryWait(afterNarrationHold);
            // Includes the sixth turn: only afterwards may the normal home-page reveal begin.
            if (turnLastPage || i < pages.Length - 1) yield return TurnOnePage();
        }
        Completed = true; IsPlaying = false; Hide(); ReleaseInk();
        if (!preview && GameManager.single && !string.IsNullOrEmpty(completionKey) && !GameManager.single.HasSeenThis(completionKey)) {
            GameManager.single.thingsSeenPermanent.Add(completionKey);
            SaveManager.Instance.Save();
        }
    }
    IEnumerator PlayVoiced(bool turnLastPage, string completionKey)
    {
        if(!illustration || !subtitle || !narration || pages==null || pageRevealTimes==null || pages.Length!=pageRevealTimes.Length){Debug.LogError("Incomplete voiced prologue timeline",this);yield break;}
        IsPlaying=Opening=true;Completed=false;TurnsCompleted=0;PageIndex=-1;Hide();
        originalMaterial=illustration.material;
        if(originalMaterial && originalMaterial.HasProperty("_Reveal")){ink=new Material(originalMaterial);illustration.material=ink;}
        intro.SampleNarrativeOpening(0,0);
        narration.clip=fullNarration;narration.volume=Gain*narrationGain;narration.Play();
        Spine.TrackEntry turn=null;bool changed=false,coverPlayed=false;
        while(narration.isPlaying || Paused){
            if(Paused){yield return null;continue;}
            VoiceTime=narration.time;PageReadTime=VoiceTime; narration.volume=Gain*narrationGain;
            subtitle.text="";
            if(voiceCaptions!=null)foreach(var c in voiceCaptions)if(VoiceTime>=c.start && VoiceTime<c.end){subtitle.text=c.text;break;}
            if(PageIndex<0)intro.SampleNarrativeOpening(Mathf.InverseLerp(openingBookStart,openingBookEnd,VoiceTime),
                Mathf.InverseLerp(openingCameraStart,openingCameraEnd,VoiceTime));
            if(PageIndex<0 && openingSketch && VoiceTime>=portraitRevealStart){
                illustration.texture=openingSketch;FitIllustration(new Page{illustration=openingSketch});
                illustration.color=Color.white;illustration.gameObject.SetActive(true);
                if(ink){
                    ink.SetFloat("_SketchMode",1);
                    ink.SetFloat("_SketchAspect",(illustration.rectTransform.rect.width/illustration.rectTransform.rect.height)/((float)openingSketch.width/openingSketch.height));
                    ink.SetFloat("_PortraitReveal",Mathf.SmoothStep(0,1,Mathf.InverseLerp(portraitRevealStart,portraitRevealStart+sketchRevealDuration,VoiceTime)));
                    ink.SetFloat("_ForestReveal",Mathf.SmoothStep(0,1,Mathf.InverseLerp(forestRevealStart,forestRevealStart+sketchRevealDuration,VoiceTime)));
                }
                SetInk(1);
            }
            if(!coverPlayed && VoiceTime>=openingBookStart){
                coverPlayed=true;
                if(coverOpenSound && VoiceTime<openingBookEnd){
                    pageFoley.clip=coverOpenSound;pageFoley.volume=Gain*coverOpenGain;pageFoley.Play();
                }
            }
            int next=PageIndex;
            while(next+1<pages.Length && VoiceTime>=pageRevealTimes[next+1])next++;
            if(next!=PageIndex){
                PageIndex=next;Opening=false;changed=false;
                if(next>0 || openingSketch){
                    intro.book.AnimationState.ClearTracks();turn=intro.book.AnimationState.SetAnimation(0,"TurnPage",false);turn.MixDuration=0;turn.TrackTime=0;intro.book.Update(0);
                    pageSurface.Begin(intro.book,"turning_page_7");TurningPage=true;
                    pageFoley.clip=pageTurnSound;pageFoley.volume=Gain*pageTurnGain;pageFoley.Play();
                }
            }
            if(PageIndex>=0){
                float local=VoiceTime-pageRevealTimes[PageIndex];float turnLength=PageIndex==0&&!openingSketch?0:pageTurnDuration;
                if(local<turnLength && turn!=null){
                    turn.TrackTime=turn.Animation.Duration*local/turnLength;intro.book.Update(0);
                    illustration.color=new Color(1,1,1,1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.48f,.52f,local/turnLength)));
                }else{
                    if(!changed){
                        if(turn!=null){turn.TrackTime=turn.Animation.Duration;intro.book.Update(0);pageSurface.End();TurnsCompleted++;turn=null;}
                        TurningPage=false;if(ink)ink.SetFloat("_SketchMode",0);illustration.texture=pages[PageIndex].illustration;FitIllustration(pages[PageIndex]);illustration.color=Color.white;illustration.gameObject.SetActive(true);changed=true;
                    }
                    SetInk(Mathf.SmoothStep(0,1,Mathf.Clamp01((local-turnLength)/pageInkDuration)));
                }
            }
            Reading=Opening || (!TurningPage && InkProgress>=.999f);
            yield return null;
        }
        Reading=Opening=false;subtitle.text="";
        if(turnLastPage)yield return TurnOnePage();
        Completed=true;IsPlaying=false;Hide();ReleaseInk();
        if(!preview && GameManager.single && !GameManager.single.HasSeenThis(completionKey)){
            GameManager.single.thingsSeenPermanent.Add(completionKey);SaveManager.Instance.Save();
        }
    }
    public IEnumerator ReadHomePage(Page page)
    {
        IsPlaying = true; PageIndex = 4;
        yield return ReadCaptions(page.captions, page.silentDuration, page.narrationAndAmbience);
        subtitle.text = ""; IsPlaying = false;
    }
    IEnumerator ReadCaptions(Caption[] captions, float duration, AudioClip clip)
    {
        Reading = true; PageReadTime = 0;
        bool voiced = !silentPreview && clip;
        narration.Stop();
        if (voiced) { narration.clip = clip; narration.volume = Gain * narrationGain; narration.Play(); }
        if (captions != null) foreach (var c in captions) duration = Mathf.Max(duration, c.end);
        while (voiced ? narration.isPlaying || Paused : PageReadTime < duration) {
            if(Paused){yield return null;continue;}
            if (voiced) { PageReadTime = narration.time; narration.volume = Gain * narrationGain; }
            var captionTarget = Opening && openingText ? openingText : subtitle;
            captionTarget.text = "";
            if (captions != null) foreach (var c in captions)
                if (PageReadTime >= c.start && PageReadTime < c.end) { captionTarget.text = c.text; break; }
            yield return null;
            if (!voiced) PageReadTime += StoryDelta;
        }
        Reading = false;
    }
    IEnumerator TurnOnePage()
    {
        TurningPage = true;
        // Only the top leaf deforms; the book root and camera stay at the reading pose.
        var book = intro.book;
        book.AnimationState.ClearTracks();
        var track = book.AnimationState.SetAnimation(0, "TurnPage", false);
        track.MixDuration = 0; track.TrackTime = 0; book.Update(0);
        pageSurface.Begin(book, "turning_page_7");
        pageFoley.volume = Gain * pageTurnGain; pageFoley.clip = pageTurnSound; if (!silentPreview) pageFoley.Play();
        float duration=(HasVoicedTimeline || intro.chapterEnding) && !silentPreview ? pageTurnDuration : track.Animation.Duration;
        float t = 0;
        while (t < duration) {
            track.TrackTime = track.Animation.Duration*t/duration; book.Update(0);
            // The front illustration follows the leaf, then disappears as its back turns toward us.
            illustration.color = new Color(1, 1, 1, 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.48f, .52f, t / duration)));
            yield return null;
            t = Mathf.Min(duration, t + Mathf.Min(.05f, StoryDelta));
        }
        track.TrackTime = track.Animation.Duration; book.Update(0);
        pageSurface.End(); illustration.gameObject.SetActive(false);
        pageFoley.Stop(); TurnsCompleted++; TurningPage = false;
    }
    void FitIllustration(Page page)
    {
        var size = illustration.rectTransform.rect.size;
        float frameAspect = size.x / Mathf.Max(1, size.y);
        float sourceAspect = (float)page.illustration.width / page.illustration.height;
        float width = Mathf.Min(1, frameAspect / sourceAspect);
        float height = Mathf.Min(1, sourceAspect / frameAspect);
        SourceUVRect = new Rect(Mathf.Clamp(page.focalPoint.x - width * .5f, 0, 1 - width),
            Mathf.Clamp(page.focalPoint.y - height * .5f, 0, 1 - height), width, height);
        if (ink && ink.HasProperty("_SourceUV")) {
            illustration.uvRect = new Rect(0, 0, 1, 1);
            ink.SetVector("_SourceUV", new Vector4(SourceUVRect.x, SourceUVRect.y, width, height));
        } else illustration.uvRect = SourceUVRect;
    }
    void SetInk(float value)
    {
        InkProgress = value;
        if (ink) { ink.SetFloat("_Reveal", value); ink.SetFloat("_PaperEdge", 1); }
        else illustration.color = new Color(1, 1, 1, value);
    }
    void Hide() { if(openingText)openingText.gameObject.SetActive(false); if (illustration) illustration.gameObject.SetActive(false); if (subtitle) subtitle.text = ""; }
    void ReleaseInk() {
        if (ink) { illustration.material = originalMaterial; Destroy(ink); ink = null; }
    }
    void OnDisable() {
        StopAmbience(); StopAllCoroutines(); if (narration) narration.Stop(); if (pageFoley) pageFoley.Stop();
        Opening = Reading = TurningPage = IsPlaying = false; if(pageSurface)pageSurface.End(); Hide(); ReleaseInk();
    }
}
