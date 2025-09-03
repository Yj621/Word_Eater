using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum JamoRole { Choseong, Jungseong, Jongseong }
public enum VowelAttach { Side, Below }

[RequireComponent(typeof(RectTransform))]
public class JamoMagnet : MonoBehaviour
{
    public JamoRole role = JamoRole.Choseong;

    [Header("표시 문자(예: ㄱ, ㅏ, ㅗ, ㅝ 등)")]
    public string glyph;

    [Header("초성(베이스) 소켓")]
    public RectTransform rightAnchor;        // 옆모음 자리 (ㅏ, ㅐ, ㅣ, ㅘ, ㅞ, ㅟ, ㅢ 등)
    public RectTransform bottomAnchor;       // 아래모음 자리 (ㅗ, ㅛ, ㅜ, ㅠ, ㅡ)
    public RectTransform bottomFinalAnchor;  // 받침 자리 (종성)

    [Header("모음 전용")]
    public VowelAttach vowelAttach = VowelAttach.Side; // ㅗ/ㅛ/ㅜ/ㅠ/ㅡ만 Below, 나머지 Side

    [Header("스냅 옵션")]
    public float snapRadius = 80f;      // 픽셀 기준
    public Vector2 attachOffset;        // 소켓 기준 미세 보정

    // 베이스(초성) 상태
    [HideInInspector] public JamoMagnet attachedVowel;      // 최종 모음(단일 또는 합성)
    [HideInInspector] public JamoMagnet attachedFinal;      // 받침
    [HideInInspector] public JamoMagnet attachedVowelSide;  // 옆모음 파트(낱개)
    [HideInInspector] public JamoMagnet attachedVowelBelow; // 아래모음 파트(낱개)

    RectTransform rt;
    public static readonly HashSet<JamoMagnet> All = new();

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        All.Add(this);

        // 모음 자동 분류: 아래 5개만 Below
        if (role == JamoRole.Jungseong && !string.IsNullOrEmpty(glyph))
            vowelAttach = GuessVowelAttach(glyph);
    }

    void OnDestroy() => All.Remove(this);

    public static VowelAttach GuessVowelAttach(string g)
    {
        switch (g)
        {
            case "ㅗ":
            case "ㅛ":
            case "ㅜ":
            case "ㅠ":
            case "ㅡ":
                return VowelAttach.Below;
            default:
                return VowelAttach.Side;
        }
    }

    // 드롭 시 스냅
    public bool TrySnap(RectTransform dragRoot, Camera uiCamera)
    {
        JamoMagnet best = null;
        RectTransform targetAnchor = null;
        float bestDist = float.MaxValue;

        foreach (var m in All)
        {
            if (!m || m.role != JamoRole.Choseong) continue;

            RectTransform cand = null;

            if (role == JamoRole.Jungseong)                  // 모음은 기존대로
            {
                if (m.attachedVowel) continue;
                cand = (vowelAttach == VowelAttach.Side) ? m.rightAnchor : m.bottomAnchor;
            }
            else                                             // 자음(초성/종성 모두 포함) → 받침 후보로 본다
            {
                if (m.attachedFinal) continue;
                cand = m.bottomFinalAnchor;
            }

            if (!cand) continue;

            var a = RectTransformUtility.WorldToScreenPoint(uiCamera, cand.position);
            var me = RectTransformUtility.WorldToScreenPoint(uiCamera, rt.position);
            float d = Vector2.Distance(a, me);
            if (d < bestDist) { bestDist = d; best = m; targetAnchor = cand; }
        }

        if (!best || !targetAnchor || bestDist > snapRadius) return false;

        // 앵커에 바로 붙이기(좌표 튐 방지)
        rt.SetParent(targetAnchor, false);
        rt.anchoredPosition = attachOffset;
        rt.localScale = Vector3.one;
        rt.SetAsLastSibling();

        var cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        var drag = GetComponent("DraggableWordUI") as Behaviour;
        if (drag) drag.enabled = false;

        if (role == JamoRole.Jungseong)
        {
            var baseCho = best;
            if (vowelAttach == VowelAttach.Side) baseCho.attachedVowelSide = this;
            else baseCho.attachedVowelBelow = this;
            if (!best.attachedVowel)
                TryFuseVowel(best);
        }
        else
        {
            role = JamoRole.Jongseong;               
            best.attachedFinal = this;
        }
        return true;
    }

    void TryFuseVowel(JamoMagnet baseCho)
    {
        var below = baseCho.attachedVowelBelow;
        var side = baseCho.attachedVowelSide;
        if (!below || !side) return;

        string bg = (below.glyph ?? "").Trim();
        string sg = (side.glyph ?? "").Trim();

        var db = JamoVowelFuseDB.Instance;
        if (!db) { Debug.LogWarning("[Fuse] DB instance null"); return; }

        var rule = db.Find(bg, sg);
        if (rule == null)
        {
            Debug.LogWarning($"[Fuse] rule not found: below='{bg}', side='{sg}'");
            return;
        }
        if (!rule.fusedPrefab)
        {
            Debug.LogWarning($"[Fuse] fusedPrefab null for below='{bg}', side='{sg}'");
            return;
        }

        var parent = baseCho.rightAnchor ? baseCho.rightAnchor : baseCho.GetComponent<RectTransform>();
        var fused = Instantiate(rule.fusedPrefab, parent, false);
        var frt = fused.GetComponent<RectTransform>();
     
        frt.anchoredPosition = rule.fusedOffset;

        var fCg = fused.GetComponent<CanvasGroup>() ?? fused.AddComponent<CanvasGroup>();
        fCg.blocksRaycasts = false;
        var fDrag = fused.GetComponent("DraggableWordUI") as Behaviour;
        if (fDrag) fDrag.enabled = false;

        var fm = fused.GetComponent<JamoMagnet>() ?? fused.AddComponent<JamoMagnet>();
        fm.role = JamoRole.Jungseong;
        if (string.IsNullOrEmpty(fm.glyph))
            fm.glyph = string.IsNullOrEmpty(rule.fusedGlyph) ? (bg + sg) : rule.fusedGlyph;

        Destroy(below.gameObject);
        Destroy(side.gameObject);
        baseCho.attachedVowelBelow = null;
        baseCho.attachedVowelSide = null;
        baseCho.attachedVowel = fm;
    }

}
