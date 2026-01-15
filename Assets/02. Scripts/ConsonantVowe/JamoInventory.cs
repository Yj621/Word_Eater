using System.Collections.Generic;
using UnityEngine;

public class JamoInventory : MonoBehaviour
{
    public static JamoInventory Instance { get; private set; }

    private readonly Dictionary<string, int> _counts = new();

    [SerializeField] KeyBoardManager keyboard;   // 인스펙터에 끌어다 놓거나 자동 찾기

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    KeyBoardManager Keyboard
    {
        get
        {
            if (!keyboard)
                keyboard = FindAnyObjectByType<KeyBoardManager>(); // 씬 바뀌면 자동 재탐색
            return keyboard;
        }
    }

    public void Add(JamoDefsType type, string jamo)
    {
        if (string.IsNullOrEmpty(jamo)) return;

        // 1) 통계/로그용 내부 카운트
        if (_counts.TryGetValue(jamo, out var cnt)) _counts[jamo] = cnt + 1;
        else _counts[jamo] = 1;

        Debug.Log($"[JamoInventory] {type} '{jamo}' 획득 총 {_counts[jamo]}개");

        // 2) 실제 키보드 슬롯에 +1 지급
        var kb = Keyboard;
        if (kb != null)
        {
            if (!kb.AddKeyByGlyph(jamo))
            {
                Debug.LogWarning($"[JamoInventory] '{jamo}' 를 키보드에 추가하지 못했어.");
            }
        }
    }

    public bool CanAdd(string jamo)
    {
        var kb = Keyboard;
        if (kb == null) return false;
        return kb.CanAddKey(jamo);
    }

    public int GetCount(string jamo)
        => _counts.TryGetValue(jamo, out var cnt) ? cnt : 0;
}
