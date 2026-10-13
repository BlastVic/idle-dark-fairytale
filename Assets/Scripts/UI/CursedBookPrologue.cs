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
        [Tooltip("Dry narration only; serialized field name retained for existing prefab compatibility.")]
        public AudioClip narrationAndAmbience;
        public Caption[] captions;
    }
    public CursedBookIntro intro;
    public RawImage illustration;
    public Text subtitle;
    public ProloguePageSurface pageSurface;
    public AudioSource narration, pageFoley;
    public AudioClip pageTurnSound;
    public Page[] pages;
    [Min(.1f)] public float inkDuration = 1.8f;
    [Min(0)] public float afterNarrationHold = .4f;
    [Range(0,1)] public float narrationGain = 1;
    [Range(0,1)] public float pageTurnGain = .65f;
    [Tooltip("Editor preview only; never changes the saved player's progress.")]
    public bool previewInEditor;
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
            if (page == null || !page.illustration || !page.narrationAndAmbience) return false;
        return pageSurface && intro.book.Skeleton.Data.FindAnimation("TurnPage") != null;
    }
    public IEnumerator Play(bool turnLastPage = true, string completionKey = SeenKey)
    {
        if (!Ready()) { Debug.LogError("Prologue: incomplete saved page/audio references.", this); yield break; }
        IsPlaying = true; Completed = false; TurnsCompleted = 0;
        originalMaterial = illustration.material;
        if (originalMaterial && originalMaterial.HasProperty("_Reveal")) {
            ink = new Material(originalMaterial); illustration.material = ink;
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
                t = Mathf.Min(inkDuration, t + Mathf.Min(.05f, Time.unscaledDeltaTime));
                SetInk(Mathf.SmoothStep(0, 1, t / Mathf.Max(.01f, inkDuration)));
                yield return null;
            }
            SetInk(1);
            // Speech starts only after the complete illustration is visible.
            narration.clip = page.narrationAndAmbience; narration.volume = Gain * narrationGain;
            narration.Play(); Reading = true;
            // The audio clock (not frame delta) owns the captions and the end of this page.
            while (narration.isPlaying) {
                narration.volume = Gain * narrationGain;
                float time = narration.time; string text = "";
                if (page.captions != null) foreach (var caption in page.captions)
                    if (time >= caption.start && time < caption.end) { text = caption.text; break; }
                subtitle.text = text;
                yield return null;
            }
            Reading = false; subtitle.text = "";
            yield return new WaitForSecondsRealtime(afterNarrationHold);
            // Includes the sixth turn: only afterwards may the normal home-page reveal begin.
            if (turnLastPage || i < pages.Length - 1) yield return TurnOnePage();
        }
        Completed = true; IsPlaying = false; Hide(); ReleaseInk();
        if (!preview && GameManager.single && !string.IsNullOrEmpty(completionKey) && !GameManager.single.HasSeenThis(completionKey)) {
            GameManager.single.thingsSeenPermanent.Add(completionKey);
            SaveManager.Instance.Save();
        }
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
        pageFoley.volume = Gain * pageTurnGain; pageFoley.clip = pageTurnSound; pageFoley.Play();
        float t = 0;
        while (t < track.Animation.Duration) {
            track.TrackTime = t; book.Update(0);
            // The front illustration follows the leaf, then disappears as its back turns toward us.
            illustration.color = new Color(1, 1, 1, 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.25f, .55f, t / track.Animation.Duration)));
            yield return null;
            t = Mathf.Min(track.Animation.Duration, t + Mathf.Min(.05f, Time.unscaledDeltaTime));
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
    void Hide() { if (illustration) illustration.gameObject.SetActive(false); if (subtitle) subtitle.text = ""; }
    void ReleaseInk() {
        if (ink) { illustration.material = originalMaterial; Destroy(ink); ink = null; }
    }
    void OnDisable() {
        StopAllCoroutines(); if (narration) narration.Stop(); if (pageFoley) pageFoley.Stop();
        Reading = TurningPage = IsPlaying = false; if(pageSurface)pageSurface.End(); Hide(); ReleaseInk();
    }
}
