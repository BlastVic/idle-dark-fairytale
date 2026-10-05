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
    Camera origGameplayCam;
    private void OnEnable()
    {
        origGameplayCam = GameplayCanvas.single.GetComponent<Canvas>().worldCamera;
        mapCam.gameObject.SetActive(true);
        GameplayCanvas.single.GetComponent<Canvas>().worldCamera = mapCam;
        TutorialManager.single.PlayTutorial(1);

    }
    private void OnDisable()
    {
        mapCam.gameObject.SetActive(false);

        GameplayCanvas.single.GetComponent<Canvas>().worldCamera = origGameplayCam;
    }

    public void BackButton()
    {
        Router.single.StartCoroutine(Router.single.GoToCampFromMap());
    }
}
