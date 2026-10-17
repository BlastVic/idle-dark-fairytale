using UnityEngine;

[CreateAssetMenu(menuName="Dark Fairytale/Story Sequence")]
public sealed class StorySequence : ScriptableObject
{
    public string storyKey="Prologue";
    public string displayName="序章 · 逃离祭祀";
    public string completionKey=CursedBookPrologue.SeenKey;
    public CursedBookPrologue.Page[] pages;
}
