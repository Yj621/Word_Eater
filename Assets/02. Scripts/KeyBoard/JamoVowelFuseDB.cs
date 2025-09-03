using System;
using System.Collections.Generic;
using UnityEngine;

public class JamoVowelFuseDB : MonoBehaviour
{
    public static JamoVowelFuseDB Instance;
    [Serializable]
    public class Rule
    {
        public string below;           // "ㅗ","ㅜ","ㅡ"
        public string side;            // "ㅏ","ㅐ","ㅣ","ㅓ","ㅔ"
        public GameObject fusedPrefab; // 결과(ㅘ, ㅙ, ㅚ, ㅝ, ㅞ, ㅟ, ㅢ)
        public string fusedGlyph;      // "ㅘ" 등 (선택)
        public Vector2 fusedOffset;    // 미세 보정
    }

    public List<Rule> rules = new();
    Dictionary<(string, string), Rule> dict;

    void Awake()
    {
        Instance = this;
        dict = new();
        foreach (var r in rules)
            if (!string.IsNullOrEmpty(r.below) && !string.IsNullOrEmpty(r.side) && r.fusedPrefab)
                dict[(r.below, r.side)] = r;
    }

    public Rule Find(string below, string side)
        => dict != null && dict.TryGetValue((below, side), out var r) ? r : null;
}
