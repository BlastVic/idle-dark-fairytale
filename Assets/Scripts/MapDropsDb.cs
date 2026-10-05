using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDropsDb : MonoBehaviour
{
    private static MapDropsDb _instance;
    public static MapDropsDb single
    {
        get
        {
            //If _instance is null then we find it from the scene 
            if (_instance == null)
                _instance = GameObject.FindObjectOfType<MapDropsDb>();
            return _instance;
        }
    }
    public List<MapDrop> mapDropsDb;
}

[System.Serializable]
public class MapDrop
{
    public string levelKey;
    public Item[] items;
}
