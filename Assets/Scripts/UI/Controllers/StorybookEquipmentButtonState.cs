using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Desaturates every authored graphic of a disabled button without adding an overlay.</summary>
[RequireComponent(typeof(Button),typeof(CanvasGroup))]
public sealed class StorybookEquipmentButtonState : MonoBehaviour
{
    public float disabledAlpha=1; // Legacy serialized value; whole-button greyscale replaces fading.
    public Shader grayscaleShader;
    Button button;
    CanvasGroup group;
    Graphic[] graphics;
    readonly Dictionary<Graphic,Material> originals=new Dictionary<Graphic,Material>();
    readonly Dictionary<Graphic,Material> grey=new Dictionary<Graphic,Material>();
    void Awake(){button=GetComponent<Button>();group=GetComponent<CanvasGroup>();graphics=GetComponentsInChildren<Graphic>(true);}
    void LateUpdate()
    {
        if(!button||graphics==null)Awake();
        if(!button)return;
        // ColorTint otherwise draws an opaque rectangle on invisible hit targets.
        var colors=button.colors;
        if(colors.disabledColor!=colors.normalColor){colors.disabledColor=colors.normalColor;button.colors=colors;}
        group.alpha=1;
        bool disabled=!button.IsInteractable();
        foreach(var graphic in graphics){
            if(!graphic||graphic.GetComponentInParent<Button>()!=button)continue;
            if(!disabled){Restore(graphic);continue;}
            if(grey.TryGetValue(graphic,out var current)&&graphic.material==current)continue;
            Restore(graphic);
            var original=graphic.material;
            var shader=original.HasProperty("_DisabledUI")?original.shader:(grayscaleShader?grayscaleShader:Shader.Find("UI/Storybook Disabled"));
            if(!shader)continue;
            var material=new Material(shader){name=original.name+" (disabled)",hideFlags=HideFlags.HideAndDontSave};
            if(shader==original.shader)material.CopyPropertiesFromMaterial(original);
            material.SetFloat("_DisabledUI",1);originals[graphic]=original;grey[graphic]=material;graphic.material=material;
        }
    }
    void Restore(Graphic graphic){if(!grey.TryGetValue(graphic,out var material))return;if(graphic&&graphic.material==material)graphic.material=originals[graphic];grey.Remove(graphic);originals.Remove(graphic);if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}
    void OnDisable(){foreach(var graphic in new List<Graphic>(grey.Keys))Restore(graphic);}
}
