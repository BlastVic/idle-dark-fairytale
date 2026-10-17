using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapPackageUI : MonoBehaviour
{
    public const float MAP_MIN_Y = -184.7f;
    public const float MAP_MAX_Y = 0f;

    private static MapPackageUI _instance;
    public static MapPackageUI single
    {
        get
        {
            if (_instance == null)
                _instance = GameObject.FindObjectOfType<MapPackageUI>();
            return _instance;
        }
    }
    public bool isDragging = false;

    public Camera mapCam;
    public bool useLegacyTutorial = true;
    Camera origGameplayCam;
    Canvas legacyCanvas;
    private void OnEnable()
    {
        // New UI pages own their canvases; the gameplay controller may have none.
        legacyCanvas = GameplayCanvas.single.GetComponent<Canvas>();
        if(legacyCanvas)origGameplayCam = legacyCanvas.worldCamera;
        mapCam.gameObject.SetActive(true);
        if(legacyCanvas)legacyCanvas.worldCamera = mapCam;
        if(useLegacyTutorial) TutorialManager.single.PlayTutorial(1);

    }
    private void OnDisable()
    {
        mapCam.gameObject.SetActive(false);

        if(legacyCanvas)legacyCanvas.worldCamera = origGameplayCam;
    }

    public void BackButton()
    {
        Router.single.StartCoroutine(Router.single.GoToCampFromMap());
    }
}
