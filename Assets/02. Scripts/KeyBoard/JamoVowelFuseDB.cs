using System;
using System.Collections.Generic;
using UnityEngine;

public class JamoVowelFuseDB : MonoBehaviour
{
    public static JamoVowelFuseDB Instance;

    [Serializable]
    public class Rule
    {
        public string first;            // 기존 받침(예: "ㄱ", "ㄹ", "ㅂ", "ㄴ")
        public string second;           // 새로 붙는 받침(예: "ㅅ", "ㄱ", "ㅅ", "ㅈ", "ㅎ" ...)
        public GameObject fusedPrefab;  // 결과(예: ㄳ, ㄺ, ㅄ, ㄵ, ㄶ, ㄽ, ㄾ, ㄿ, ㅀ)
        public string fusedGlyph;       // "ㄳ" 등 (선택)
        public Vector2 fusedOffset;     // 미세 보정
    }

    public List<Rule> rules = new();
    Dictionary<(string, string), Rule> dict;

    void Awake()
    {
        Instance = this;
        dict = new();
        foreach (var r in rules)
        {
            if (string.IsNullOrWhiteSpace(r.first) || string.IsNullOrWhiteSpace(r.second) || !r.fusedPrefab) continue;
            dict[(r.first.Trim(), r.second.Trim())] = r;
        }
    }

    public Rule Find(string first, string second)
    {
        if (dict == null) return null;
        first = (first ?? "").Trim();
        second = (second ?? "").Trim();
        // 순서 고정 규칙이지만, 편의상 역순도 시도
        return dict.TryGetValue((first, second), out var r)
             ? r
             : (dict.TryGetValue((second, first), out var r2) ? r2 : null);
    }
}
