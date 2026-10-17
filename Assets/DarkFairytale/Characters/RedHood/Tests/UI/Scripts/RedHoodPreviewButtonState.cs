using UnityEngine;
using UnityEngine.UI;
namespace RedHoodPrototype {
 // Tint all graphics and borders; restoring a button restores its authored palette.
 public sealed class RedHoodPreviewButtonState:MonoBehaviour {
  Button button;Graphic[] graphics;Color[] colors;Shadow[] borders;Color[] borderColors;bool disabled;
  void Awake(){button=GetComponent<Button>();graphics=GetComponentsInChildren<Graphic>(true);colors=new Color[graphics.Length];for(int i=0;i<colors.Length;i++)colors[i]=graphics[i].color;borders=GetComponentsInChildren<Shadow>(true);borderColors=new Color[borders.Length];for(int i=0;i<borders.Length;i++)borderColors[i]=borders[i].effectColor;}
  Color Tint(Color c){float g=c.grayscale;return disabled?new Color(g,g,g,c.a*.55f):c;}
  void LateUpdate(){bool next=!button.interactable;if(next==disabled)return;disabled=next;for(int i=0;i<graphics.Length;i++)graphics[i].color=Tint(colors[i]);for(int i=0;i<borders.Length;i++)borders[i].effectColor=Tint(borderColors[i]);}
 }
}
