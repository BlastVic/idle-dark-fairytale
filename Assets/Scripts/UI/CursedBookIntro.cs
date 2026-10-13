using System.Collections;
using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Pre-renders the real home UI onto a page, then moves into that page without a scene cut.</summary>
public sealed class CursedBookIntro : MonoBehaviour
{
    [System.NonSerialized] public StorySequence mapStory;
    public SkeletonGraphic book;
    public CanvasGroup group;
    public IntroScript completion;
    public Text version, status;
    public RectTransform cameraRig;
    public RawImage homePage;
    public Camera homeCamera;
    public CursedBookPrologue prologue;
    [Header("Camera / reveal timing (unscaled seconds)")]
    [Range(.4f, .85f)] public float pageHoldViewportFraction = .65f;
    public float revealDuration = 1.8f;
    public float revealedHoldDuration = .3f;
    public float enterPageDuration = 2.2f;
    public float controlsRevealDuration = .9f;
    public float ControlsProgress { get; private set; }
    public string animationName = "Open";
    public bool Finished { get; private set; }
    public bool SkippedStartup { get; private set; }
    public bool AnimationFinished { get; private set; }
    public bool HomeReady { get; private set; }
    public float RevealProgress { get; private set; }
    public float PushProgress { get; private set; }
    public float Elapsed { get; private set; }
    public RenderTexture HomeTexture => homeTexture;

    ShelterResponsiveLayout homeLayout;
    CanvasGroup controls;
    float controlsAlpha;
    bool oldSafeArea;
    Vector2 finalViewport, pageViewport;
    Canvas homeCanvas;
    GraphicRaycaster homeRaycaster;
    bool raycasterWasEnabled;
    RenderMode oldMode;
    Camera oldCamera;
    float oldDistance;
    readonly List<Transform> capturedNodes = new List<Transform>();
    readonly List<int> originalLayers = new List<int>();
    RenderTexture homeTexture;
    Material revealMaterial;
    Vector3 rigStartScale;
    Color captionColor, versionColor;
    bool restoring;
    bool fullPageReading;

    void Awake()
    {
        // The legacy completion callbacks are still used, but its Start must not hide the UI we preload.
        if (completion) completion.enabled = false;
        // Establish the closed-cover pose before the first rendered/loading frame.
        if (cameraRig && homePage)
            cameraRig.anchoredPosition = -Vector2.Scale(homePage.rectTransform.anchoredPosition, cameraRig.localScale);
        if (book) {
            book.Initialize(false); book.timeScale = 0; book.unscaledTime = true;
            if (book.IsValid && book.Skeleton.Data.FindAnimation(animationName) != null) {
                // This track is manually sampled. A crossfade cannot advance while deltaTime is zero.
                book.AnimationState.ClearTracks();
                book.AnimationState.SetAnimation(0, animationName, false); book.Update(0);
            }
        }
    }

    IEnumerator Start()
    {
        if (mapStory) { yield return PlayMapStory(); yield break; }
        if (StorybookPreferences.SkipLoadingAnimation) { yield return EnterHomeDirectly(); yield break; }
        Finished = AnimationFinished = HomeReady = false;
        group.alpha = 1; group.blocksRaycasts = true;
        homePage.color = new Color(1, 1, 1, 0);
        FitPageAspect();
        rigStartScale = cameraRig.localScale;
        ApplyCamera(1);
        captionColor = status.color; versionColor = version.color;
        version.text = "v" + Application.version;
        status.text = "故事，即将翻开";
        if (homePage.material && homePage.material.HasProperty("_Reveal")) {
            revealMaterial = new Material(homePage.material);
            homePage.material = revealMaterial;
            revealMaterial.SetFloat("_Reveal", 0);
            revealMaterial.SetFloat("_PaperEdge", 1);
        }
        StartCoroutine(PreloadHome());
        book.Initialize(false);
        var animation = book.IsValid ? book.Skeleton.Data.FindAnimation(animationName) : null;
        if (animation == null)
        {
            Debug.LogError("Cursed book intro: missing Spine Open animation.", this);
            while (!HomeReady) yield return null;
            Complete(); yield break;
        }
        book.unscaledTime = true;
        // Loading/shader warmup can make the first unscaled delta several seconds long.
        // Drive the track explicitly so the cover and every transition remain visible.
        book.timeScale = 0;
        // Continue the closed-cover track from Awake instead of crossfading into a duplicate.
        var track = book.AnimationState.GetCurrent(0);
        if (track == null || track.Animation != animation) {
            book.AnimationState.ClearTracks();
            track = book.AnimationState.SetAnimation(0, animationName, false);
        }
        Elapsed = 0;
        while (Elapsed < animation.Duration)
        {
            track.TrackTime = Elapsed;
            book.Update(0);
            float p = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1, animation.Duration, Elapsed));
            PushProgress = 0;
            ApplyCamera(Mathf.Lerp(1, HoldZoom(), p));
            SetLabels(1 - p);
            yield return null;
            Elapsed = Mathf.Min(animation.Duration, Elapsed + Mathf.Min(.05f, Time.unscaledDeltaTime));
        }
        track.TrackTime = animation.Duration;
        book.Update(0);
        SetLabels(0);
        ApplyCamera(HoldZoom());
        AnimationFinished = true;
        // A slow load holds on the opened book. Never push into a blank/unready home page.
        while (!HomeReady) yield return null;
        if (prologue && prologue.ShouldPlay()) {
            float from = HoldZoom();
            fullPageReading = true;
            float readingPush = 0;
            while (readingPush < .6f) {
                readingPush = Mathf.Min(.6f, readingPush + Mathf.Min(.05f, Time.unscaledDeltaTime));
                ApplyCamera(Mathf.Lerp(from, TargetZoom(), Mathf.SmoothStep(0, 1, readingPush / .6f)));
                yield return null;
            }
            yield return prologue.Play();
        }
        float time = 0;
        float revealAndHold = revealDuration + revealedHoldDuration;
        while (time < revealAndHold - .0001f)
        {
            time = Mathf.Min(revealAndHold, time + Mathf.Min(.05f, Time.unscaledDeltaTime));
            Elapsed = animation.Duration + time;
            RevealProgress = Mathf.SmoothStep(0, 1, Mathf.Clamp01(time / Mathf.Max(.01f, revealDuration)));
            homePage.color = Color.white;
            if (revealMaterial) revealMaterial.SetFloat("_Reveal", RevealProgress);
            else homePage.color = new Color(1, 1, 1, RevealProgress);
            ApplyCamera(HoldZoom());
            yield return null;
        }
        // The complete illustration is already visible before the final forward camera movement.
        time = 0;
        while (time < enterPageDuration)
        {
            time = Mathf.Min(enterPageDuration, time + Mathf.Min(.05f, Time.unscaledDeltaTime));
            Elapsed = animation.Duration + revealDuration + revealedHoldDuration + time;
            PushProgress = Mathf.SmoothStep(0, 1, time / Mathf.Max(.01f, enterPageDuration));
            // The illustration's geometry stays locked: only the book camera moves.
            if (revealMaterial) revealMaterial.SetFloat("_PaperEdge", 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.45f,.95f,PushProgress)));
            ApplyCamera(Mathf.Lerp(HoldZoom(), TargetZoom(), PushProgress));
            yield return null;
        }
        RevealProgress = PushProgress = 1;
        if (revealMaterial) revealMaterial.SetFloat("_Reveal", 1);
        homePage.color = Color.white;
        ApplyCamera(TargetZoom());
        homeLayout.respectSafeArea = oldSafeArea;
        homeLayout.introCompositionSize = finalViewport;
        homeLayout.Apply();
        time = 0;
        while (time < controlsRevealDuration) {
            time = Mathf.Min(controlsRevealDuration, time + Mathf.Min(.05f, Time.unscaledDeltaTime));
            ControlsProgress = Mathf.SmoothStep(0, 1, time / Mathf.Max(.01f, controlsRevealDuration));
            controls.alpha = controlsAlpha * ControlsProgress;
            Elapsed = animation.Duration + revealAndHold + enterPageDuration + time;
            yield return null;
        }
        ControlsProgress = 1;
        controls.alpha = controlsAlpha;
        // Let the full-screen page render once before switching ownership to the identical live canvas.
        yield return new WaitForEndOfFrame();
        yield return null;
        Complete();
    }

    IEnumerator EnterHomeDirectly()
    {
        SkippedStartup=true;
        // Keep the loading overlay until the actual saved home is ready. Do not capture it
        // into a texture, play page audio or mark any story chapter as read.
        var audio=GetComponent<CursedBookIntroAudio>();if(audio)audio.enabled=false;
        if(book)book.gameObject.SetActive(false);
        if(homePage)homePage.gameObject.SetActive(false);
        if(homeCamera)homeCamera.enabled=false;
        group.alpha=1;group.blocksRaycasts=true;
        status.text="正在加载…";version.text="v"+Application.version;
        yield return null;
        while(!GameManager.single||!LevelController.Instance||!LevelController.Instance.AssetManager||
              !LevelController.Instance.AssetManager.campPackageObj||!GameplayCanvas.single||!GameplayCanvas.single.homeUI)
            yield return null;
        foreach(var obj in completion.objectsToTurnOnAfter)if(obj)obj.SetActive(true);
        var gc=GameplayCanvas.single;gc.gameObject.SetActive(true);gc.homeUI.gameObject.SetActive(true);gc.homeUI.Refresh();
        gc.homeUI.GetComponent<ShelterResponsiveLayout>().Apply();Canvas.ForceUpdateCanvases();
        yield return null;
        HomeReady=AnimationFinished=true;RevealProgress=PushProgress=ControlsProgress=1;
        Complete();
    }

    IEnumerator PlayMapStory()
    {
        var startupAudio=GetComponent<CursedBookIntroAudio>();if(startupAudio)startupAudio.enabled=false;
        if(homeCamera)homeCamera.enabled=false;
        group.alpha=1;group.blocksRaycasts=true;
        homePage.color=Color.clear;status.text="";version.text="";
        // Let the newly instantiated CanvasScaler settle before computing the reading zoom.
        yield return null;
        FitPageAspect();Canvas.ForceUpdateCanvases();
        GetComponent<CursedBookIntroLayout>()?.Apply();
        Canvas.ForceUpdateCanvases();
        // Reuse the opening book hold framing so its paper and cover borders remain visible.
        rigStartScale=cameraRig.localScale;fullPageReading=false;
        book.Initialize(false);book.AnimationState.ClearTracks();
        var open=book.AnimationState.SetAnimation(0,animationName,false);
        open.TrackTime=open.Animation.Duration;book.Update(0);ApplyCamera(HoldZoom());
        prologue.pages=mapStory.pages;
        yield return prologue.Play(false,mapStory.completionKey);
        // Replays never preload/reveal the home UI or invoke legacy intro completion.
        Finished=true;group.blocksRaycasts=false;
    }

    IEnumerator PreloadHome()
    {
        // Let GameManager/SaveManager initialize first, then render the original saved home prefab.
        yield return null;
        while (!GameManager.single || !LevelController.Instance ||
               !LevelController.Instance.AssetManager || !LevelController.Instance.AssetManager.campPackageObj ||
               !GameplayCanvas.single || !GameplayCanvas.single.homeUI)
            yield return null;
        foreach (var obj in completion.objectsToTurnOnAfter) if (obj) obj.SetActive(true);
        var gc = GameplayCanvas.single;
        gc.gameObject.SetActive(true);
        gc.homeUI.gameObject.SetActive(true);
        gc.homeUI.Refresh();
        yield return null;
        Canvas.ForceUpdateCanvases();
        homeLayout = gc.homeUI.GetComponent<ShelterResponsiveLayout>();
        controls = homeLayout.presentationControls;
        controlsAlpha = controls.alpha; controls.alpha = 0;
        oldSafeArea = homeLayout.respectSafeArea; homeLayout.respectSafeArea = false;
        homeCanvas = gc.homeUI.GetComponent<Canvas>();
        homeRaycaster = homeCanvas.GetComponent<GraphicRaycaster>();
        if (homeRaycaster) { raycasterWasEnabled = homeRaycaster.enabled; homeRaycaster.enabled = false; }
        oldMode = homeCanvas.renderMode; oldCamera = homeCanvas.worldCamera; oldDistance = homeCanvas.planeDistance;
        foreach (var node in homeCanvas.GetComponentsInChildren<Transform>(true))
        {
            capturedNodes.Add(node); originalLayers.Add(node.gameObject.layer); node.gameObject.layer = 31;
        }
        var canvas = GetComponent<Canvas>();
        int height = Mathf.Max(1, Mathf.RoundToInt(canvas.pixelRect.height));
        int width = Mathf.RoundToInt(height * .75f);
        finalViewport = canvas.pixelRect.size;
        pageViewport = new Vector2(width, height);
        homeLayout.introCompositionSize = pageViewport;
        homeTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) {
            name = "Live home page", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
        };
        homeTexture.Create(); homeCamera.targetTexture = homeTexture;
        homeCamera.transform.position = new Vector3(0, 0, -10000);
        homeCamera.transform.rotation = Quaternion.identity;
        homeCamera.aspect = (float)width / height;
        homeCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        homeCanvas.worldCamera = homeCamera; homeCanvas.planeDistance = 1;
        homePage.texture = homeTexture;
        FitPageAspect();
        Canvas.ForceUpdateCanvases();
        yield return null;
        // CanvasScaler uses design units; never feed pixel dimensions into its saved layout.
        pageViewport = ((RectTransform)homeCanvas.transform).rect.size;
        finalViewport = new Vector2(pageViewport.y * canvas.pixelRect.width / canvas.pixelRect.height, pageViewport.y);
        homeLayout.introCompositionSize = pageViewport;
        homeLayout.Apply();
        homeLayout.LockStoryFraming();
        Canvas.ForceUpdateCanvases();
        homeCamera.enabled = true;
        homeCamera.Render();
        yield return new WaitForEndOfFrame();
        HomeReady = true;
    }

    void FitPageAspect()
    {
        // The book illustration has its own 3:4 composition, independent of phone proportions.
        var r = homePage.rectTransform;
        r.sizeDelta = new Vector2(r.sizeDelta.y * .75f, r.sizeDelta.y);
    }
    float HoldZoom() => fullPageReading ? TargetZoom() : Mathf.Max(1, TargetZoom() * pageHoldViewportFraction);
    float TargetZoom()
    {
        var core = (RectTransform)cameraRig.parent;
        return ((RectTransform)transform).rect.height / (homePage.rectTransform.rect.height * core.localScale.y * rigStartScale.y);
    }
    void ApplyCamera(float zoom)
    {
        var core = (RectTransform)cameraRig.parent;
        var viewportCenter = core.InverseTransformPoint(((RectTransform)transform).TransformPoint(Vector3.zero));
        var pageCenter = homePage.rectTransform.anchoredPosition;
        // Keep the right page centered through the opening push, stationary reveal, and final push.
        cameraRig.localScale = rigStartScale * zoom;
        cameraRig.anchoredPosition = (Vector2)viewportCenter - Vector2.Scale(pageCenter, rigStartScale) * zoom;
    }
    void SetLabels(float alpha)
    {
        status.color = new Color(captionColor.r, captionColor.g, captionColor.b, captionColor.a * alpha);
        version.color = new Color(versionColor.r, versionColor.g, versionColor.b, versionColor.a * alpha);
    }
    void Complete()
    {
        RestoreHome();
        group.blocksRaycasts = false;
        Finished = true;
        completion.AnimationNearlyComplete();
        completion.AnimationCompleted();
    }
    void RestoreHome()
    {
        if (restoring) return;
        restoring = true;
        if (homeCamera) { homeCamera.enabled = false; homeCamera.targetTexture = null; }
        if (controls) controls.alpha = controlsAlpha;
        if (homeLayout) { homeLayout.introCompositionSize = Vector2.zero; homeLayout.respectSafeArea = oldSafeArea; }
        if (homeCanvas) { homeCanvas.renderMode = oldMode; homeCanvas.worldCamera = oldCamera; homeCanvas.planeDistance = oldDistance; }
        for (int i = 0; i < capturedNodes.Count; i++) if (capturedNodes[i]) capturedNodes[i].gameObject.layer = originalLayers[i];
        capturedNodes.Clear(); originalLayers.Clear();
        if (homeRaycaster) homeRaycaster.enabled = raycasterWasEnabled;
        Canvas.ForceUpdateCanvases();
        if (homeLayout) homeLayout.Apply();
        restoring = false;
    }
    void OnDisable()
    {
        StopAllCoroutines();
        RestoreHome();
        if (homePage) homePage.texture = null;
        if (revealMaterial) { Destroy(revealMaterial); revealMaterial = null; }
        if (homeTexture) { homeTexture.Release(); Destroy(homeTexture); homeTexture = null; }
    }
}
