using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Opt-in Play Mode probe; never saved into a scene or prefab.
public static class ShelterPointerProbe
{
    public static void Run(ShelterHomeUI ui,string path){ui.StartCoroutine(Probe(ui,path));}
    static IEnumerator Probe(ShelterHomeUI ui,string path){
        yield return new WaitForEndOfFrame();
        foreach(var b in ui.GetComponentsInChildren<Button>()){
            if(!b.gameObject.activeInHierarchy)continue;
            var r=(RectTransform)b.transform;var point=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
            bool pass=hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==b;
            File.AppendAllText(path,(pass?"PASS":"FAIL")+" actual frame raycast "+b.name+" => "+(hits.Count==0?"none":hits[0].gameObject.name)+"\n");
        }
    }
}
