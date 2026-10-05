using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;
using System;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System.IO;
using Assets.Scripts.Services;
//using PlayFab.ClientModels;
//using PlayFab;

public class DebugMenu : MonoBehaviour
{
    //admob
    //ZnycuTkos4QQEOJ2U9pJSHKFDvWZrcBk
    //build notes
    // Go to Unity-iPhone / Build Phases / Link Binary with Libraries build section and add UnityFramework.framework.
    // Doing this ensures UnityFramework will be loaded at the same time as the main executable.
    // Go to Build Setting in the project and under Build Settings section, set Enable Bitcode to <No>

    //remove skills
    //lock the maps (done)?
    //block rebirths

    private static DebugMenu _instance;
    public static DebugMenu Instance
    {
        get
        {
            //If _instance is null then we find it from the scene 
            if (_instance == null)
                _instance = GameObject.FindObjectOfType<DebugMenu>();
            return _instance;
        }
    }

    public GUIStyle importantButton;
    //public SaveData _SaveData;
    public Vector3 offsetAdminPanel = new Vector3();
    public string[] allEnemies;
    string sampleText = "";

    public int m_LastFPS;
    private void Start()
    {
        //#if !UNITY_EDITOR
        //       enabled=false;
        //#endif
        //DontDestroyOnLoad(this.gameObject);
        if (System.Environment.UserDomainName == "DESKTOP-FJFK64E")
        {

        }

        //Debug.Log(System.Environment.UserDomainName);
    }

    void Update()
    {
        //#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            ToggleDebug();
        }


        m_LastFPS = (int)(1.0f / Time.smoothDeltaTime);
        //#endif
    }

    public bool m_ShowDebug = false;
    void ToggleDebug()
    {
        m_ShowDebug = !m_ShowDebug;
    }
    public bool m_ShowFPS = true;
    public GUIStyle mySty = new GUIStyle();

    void OnGUI()
    {
        GUI.skin.label.fontSize = 25;
        GUI.skin.button.fontSize = 25;
        GUI.skin.textField.fontSize = 25;
        GUI.skin.textArea.fontSize = 25;

        if (m_ShowDebug)
        {
            if (m_ShowFPS) GUI.Label(new Rect(200, 0 + offsetAdminPanel.y, 200, 50), "FPS:" + m_LastFPS);
            //GUI.Label(new Rect(200, 50 + offsetAdminPanel.y, 250, 50), "Some text field to use ");

            //BUTTONS BELOW
            if (GUI.Button(new Rect(offsetAdminPanel.x, offsetAdminPanel.y, 200, 50), "+REBIRTH test"))
            {
                //GameManager.single.IncXp(GameManager.single.xpMax * .1f);
                // StatManager.single.GetRebirthCrystalsByLevel(116, 3, 0);
                //StatManager.single.GetRebirthXpByLevel(190, 3, 50);
                //StatManager.single.GetRebirthCrystalsByLevel(190, 6, 300);

                //StatManager.single.GetRebirthCrystalsByLevel(187, 10, 550);

                //StatManager.single.GetRebirthCrystalsByLevel(290, 25, 900);


            }
            if (GUI.Button(new Rect(offsetAdminPanel.x, offsetAdminPanel.y + 50, 200, 50), "+10 Stat/skill"))
            {
                GameManager.single.statPoints += 10;
                GameManager.single.skillPoints += 10;

                GameplayCanvas.single.RefreshCampUI();
            }
            if (GUI.Button(new Rect(offsetAdminPanel.x, offsetAdminPanel.y + 100, 200, 50), "Timescale Normal"))
            {
                Debug.Log("Chest:" + InventoryManager.single.chestsOwned[0].rewardTime);
                Time.timeScale = 1;
            }
            if (GUI.Button(new Rect(offsetAdminPanel.x, offsetAdminPanel.y + 150, 200, 50), "Timescale x2"))
            {
                Time.timeScale = 2;
            }
            if (GUI.Button(new Rect(offsetAdminPanel.x, offsetAdminPanel.y + 200, 200, 50), "Increase Fusion"))
            {
                GameManager.single.IncFusion(GameManager.single.fxpMax * .25f);
            }
            if (GUI.Button(new Rect(offsetAdminPanel.x, offsetAdminPanel.y + 250, 200, 50), "Unlock Maps"))
            {
                EventManager.single.CallUnlockMaps();
            }
            if (GUI.Button(new Rect(offsetAdminPanel.x, offsetAdminPanel.y + 300, 200, 50), "HP Lev up"))
            {
                GameManager.single.atkSpdLevel++;
                GameManager.single.dmgLevel++;
                GameManager.single.hpLevel += 2;
            }
            if (GUI.Button(new Rect(offsetAdminPanel.x, offsetAdminPanel.y + 350, 200, 50), "Add Cherries"))
            {
                GameManager.single.premiumCurrency += 100;
                if (GameplayCanvas.single != null) GameplayCanvas.single.RefreshCampUI();
            }
            if (GUI.Button(new Rect(offsetAdminPanel.x, offsetAdminPanel.y + 400, 200, 50), "Kill Enemy"))
            {
                if (GameManager.single.GetLivingEnemies().Count > 0)
                    GameManager.single.GetLivingEnemies()[0].Hit(GameManager.single.GetLivingEnemies()[0].currentStat.hpNow * 2);
            }
            //  if (GUI.Button(new Rect(offsetAdminPanel.x, offsetAdminPanel.y + 400, 200, 50), "Fit Container"))
            // {
            //      GameObject container = GameObject.FindGameObjectWithTag("ItemContainer");
            //      container.AddComponent<ContentSizeFitter>();
            //   }

            //big button
            if (GUI.Button(new Rect(offsetAdminPanel.x + 200, offsetAdminPanel.y + 50, 300, 50), "! RESET GAME !"))
            {
                SaveManager.Instance.DeleteData();

            }
        }
    }


}