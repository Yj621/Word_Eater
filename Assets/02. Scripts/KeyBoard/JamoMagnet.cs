using System.Collections.Generic;
using UnityEngine;

public enum JamoRole { Choseong, Jungseong, Jongseong }
public enum VowelAttach { Side, Below }

[RequireComponent(typeof(RectTransform))]
public class JamoMagnet : MonoBehaviour
{
    public JamoRole role = JamoRole.Choseong;

    [Header("표시 문자")]
    public string glyph;

    [Header("초성(베이스) 소켓")]
    public RectTransform rightAnchor;        // 옆모음
    public RectTransform bottomAnchor;       // 아래모음
    public RectTransform bottomFinalAnchor;  // 받침

    [Header("모음 전용")]
    public VowelAttach vowelAttach = VowelAttach.Side;

    [Header("스냅 옵션")]
    public float snapRadius = 80f;
    public Vector2 attachOffset;

    // 상태
    [HideInInspector] public JamoMagnet attachedVowel, attachedFinal;
    [HideInInspector] public JamoMagnet attachedVowelSide, attachedVowelBelow;

    RectTransform rt;
    public static readonly HashSet<JamoMagnet> All = new();

    // 받침 불가 자음
    static readonly HashSet<string> InvalidFinal = new() { "ㄸ", "ㅉ", "ㅃ" };

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        All.Add(this);
        if (role == JamoRole.Jungseong && !string.IsNullOrEmpty(glyph))
            vowelAttach = GuessVowelAttach(glyph);
    }
    void OnDestroy() => All.Remove(this);

    public static VowelAttach GuessVowelAttach(string g)
        => (g == "ㅗ" || g == "ㅛ" || g == "ㅜ" || g == "ㅠ" || g == "ㅡ") ? VowelAttach.Below : VowelAttach.Side;

    // ---- 유틸: 이 오브젝트가 '베이스/완성 블록'인가? (소켓 보유 여부로 판단) ----
    bool HasAnySockets() => rightAnchor || bottomAnchor || bottomFinalAnchor;

    public bool TrySnap(RectTransform dragRoot, Camera uiCamera)
    {
        // 받침 후보인지 판단
        bool tryingFinal =
            role == JamoRole.Jongseong ||
            (role == JamoRole.Choseong && !HasAnySockets()); // 초성 프리팹이지만 소켓이 없으면 '단일 자음'으로 간주

        // (1) 후보 베이스 찾기
        JamoMagnet best = null;
        RectTransform targetAnchor = null;
        float bestDist = float.MaxValue;

        foreach (var m in All)
        {
            if (!m || m.role != JamoRole.Choseong) continue;

            RectTransform cand = null;
            if (!tryingFinal) // 모음
            {
                if (m.attachedVowel) continue;
                cand = (vowelAttach == VowelAttach.Side) ? m.rightAnchor : m.bottomAnchor;
            }
            else // 받침
            {
                // 완성 음절(=소켓 가진 베이스/블록)을 '받침'으로 붙이는 것 금지
                if (HasAnySockets()) continue;

                if (InvalidFinal.Contains(glyph)) continue; // ㄸ/ㅉ/ㅃ 금지

                // 받침 소켓
                cand = m.bottomFinalAnchor;
            }
            if (!cand) continue;

            var a = RectTransformUtility.WorldToScreenPoint(uiCamera, cand.position);
            var me = RectTransformUtility.WorldToScreenPoint(uiCamera, rt.position);
            float d = Vector2.Distance(a, me);
            if (d < bestDist) { bestDist = d; best = m; targetAnchor = cand; }
        }

        if (!best || !targetAnchor || bestDist > snapRadius) return false;

        // ---- (2) 붙이기 / 겹받침 합성 ----
        if (tryingFinal)
        {
            // 기존 받침이 있으면 겹받침 Fuse 시도
            if (best.attachedFinal)
            {
                if (!TryFuseFinal(best, best.attachedFinal, this)) return false; // 룰 없으면 부착 불가
                return true;
            }

            // 받침 부착
            AttachTo(targetAnchor);
            role = JamoRole.Jongseong;   // 초성이던 것도 받침으로 전환
            best.attachedFinal = this;
            return true;
        }
        else
        {
            // 모음 부착(옆/아래)
            AttachTo(targetAnchor);
            if (vowelAttach == VowelAttach.Side) best.attachedVowelSide = this;
            else best.attachedVowelBelow = this;

            if (!best.attachedVowel)
                TryFuseVowel(best); // 아래+옆 모음 모두 있으면 합성
            return true;
        }
    }

    void AttachTo(RectTransform socket)
    {
        rt.SetParent(socket, false);
        rt.anchoredPosition = attachOffset;
        rt.localScale = Vector3.one;
        rt.SetAsLastSibling();

        // 붙은 조각은 입력 잠금 → 베이스만 드래그
        var cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        var drag = GetComponent("DraggableWordUI") as Behaviour;
        if (drag) drag.enabled = false;
    }

    // ==== 모음 합성 (기존) ====
    void TryFuseVowel(JamoMagnet baseCho)
    {
        var below = baseCho.attachedVowelBelow;
        var side = baseCho.attachedVowelSide;
        if (!below || !side) return;

        var rule = JamoVowelFuseDB.Instance?.Find((below.glyph ?? "").Trim(), (side.glyph ?? "").Trim());
        if (rule == null || !rule.fusedPrefab) return;

        var parent = baseCho.rightAnchor ? baseCho.rightAnchor : baseCho.GetComponent<RectTransform>();
        var fused = Object.Instantiate(rule.fusedPrefab, parent, false);
        var frt = fused.GetComponent<RectTransform>();
        frt.anchoredPosition = rule.fusedOffset;

        var fm = fused.GetComponent<JamoMagnet>() ?? fused.AddComponent<JamoMagnet>();
        fm.role = JamoRole.Jungseong;
        if (string.IsNullOrEmpty(fm.glyph))
            fm.glyph = string.IsNullOrEmpty(rule.fusedGlyph) ? (below.glyph + side.glyph) : rule.fusedGlyph;

        Object.Destroy(below.gameObject);
        Object.Destroy(side.gameObject);
        baseCho.attachedVowelBelow = null;
        baseCho.attachedVowelSide = null;
        baseCho.attachedVowel = fm;

        // 합성 결과도 입력 잠금
        var cg = fused.GetComponent<CanvasGroup>() ?? fused.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        var drag = fused.GetComponent("DraggableWordUI") as Behaviour;
        if (drag) drag.enabled = false;
    }

    // ==== 겹받침 합성 (신규) ====
    bool TryFuseFinal(JamoMagnet baseCho, JamoMagnet first, JamoMagnet second)
    {
        var db = JamoVowelFuseDB.Instance;
        if (!db) return false;

        var rule = db.Find((first.glyph ?? "").Trim(), (second.glyph ?? "").Trim());
        if (rule == null || !rule.fusedPrefab) return false;

        var parent = baseCho.bottomFinalAnchor ? baseCho.bottomFinalAnchor : baseCho.GetComponent<RectTransform>();
        var fused = Object.Instantiate(rule.fusedPrefab, parent, false);
        var frt = fused.GetComponent<RectTransform>();
        frt.anchoredPosition = rule.fusedOffset;

        var fm = fused.GetComponent<JamoMagnet>() ?? fused.AddComponent<JamoMagnet>();
        fm.role = JamoRole.Jongseong;
        if (string.IsNullOrEmpty(fm.glyph))
            fm.glyph = string.IsNullOrEmpty(rule.fusedGlyph) ? (first.glyph + second.glyph) : rule.fusedGlyph;

        // 기존/신규 받침 제거 후 교체
        Object.Destroy(first.gameObject);
        Object.Destroy(second.gameObject);
        baseCho.attachedFinal = fm;

        // 합성 결과도 입력 잠금
        var cg = fused.GetComponent<CanvasGroup>() ?? fused.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        var drag = fused.GetComponent("DraggableWordUI") as Behaviour;
        if (drag) drag.enabled = false;

        return true;
    }
}
