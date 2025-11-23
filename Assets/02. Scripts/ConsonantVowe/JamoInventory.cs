using System.Collections.Generic;
using UnityEngine;

public class JamoInventory : MonoBehaviour
{
    public static JamoInventory Instance { get; private set; }

    private readonly Dictionary<string, int> _counts = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Add(JamoDefsType type, string jamo)
    {
        if (string.IsNullOrEmpty(jamo)) return;
        if (_counts.TryGetValue(jamo, out var cnt)) _counts[jamo] = cnt + 1;
        else _counts[jamo] = 1;

        Debug.Log($"[JamoInventory] {type} '{jamo}' 획득! 총 {_counts[jamo]}개");
        // TODO: UI 갱신, 세이브 반영 등
    }

    public int GetCount(string jamo) => _counts.TryGetValue(jamo, out var cnt) ? cnt : 0;
}
