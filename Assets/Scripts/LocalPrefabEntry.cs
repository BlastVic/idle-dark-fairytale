using UnityEngine;

// One small resource record per prefab keeps loading lazy. Prefabs remain editable
// at their authored paths; Unity packs their shared dependencies only once.
public sealed class LocalPrefabEntry : ScriptableObject
{
    public GameObject prefab;
}
