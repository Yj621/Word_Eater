using System.Collections.Generic;
using UnityEngine;
using TMPro;

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

    [HideInInspector] public JamoMagnet attachedVowel, attachedFinal;
    [HideInInspector] public JamoMagnet attachedVowelSide, attachedVowelBelow;

    RectTransform rt;
    public static readonly HashSet<JamoMagnet> All = new();

    static readonly HashSet<string> InvalidFinal = new() { "ㄸ", "ㅉ", "ㅃ" };

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        All.Add(this);

       
        if (role == JamoRole.Jungseong && !string.IsNullOrEmpty(glyph))
            vowelAttach = GuessVowelAttach(glyph);

        if (role == JamoRole.Choseong)
        {
            rightAnchor = EnsureChildSocket(rightAnchor, "RightMag");
            bottomAnchor = EnsureChildSocket(bottomAnchor, "DownMag");
            bottomFinalAnchor = EnsureChildSocket(bottomFinalAnchor, "FinalMag");
        }
    }

    void OnDestroy() => All.Remove(this);

    public static VowelAttach GuessVowelAttach(string g)
        => (g == "ㅗ" || g == "ㅛ" || g == "ㅜ" || g == "ㅠ" || g == "ㅡ") ? VowelAttach.Below : VowelAttach.Side;

    bool HasAnySockets()
        => (rightAnchor != null) || (bottomAnchor != null) || (bottomFinalAnchor != null);

    public void SetGlyph(string g)
    {
        glyph = g;
       
    }

    public bool TrySnap(RectTransform dragRoot, Camera uiCamera)
    {
        bool tryingFinal = (role != JamoRole.Jungseong);

        JamoMagnet best = null;
        RectTransform targetAnchor = null;
        float bestDist = float.MaxValue;

        foreach (var m in All)
        {
            if (!m || m.role != JamoRole.Choseong) continue;
            if (m == this) continue;

            RectTransform cand = null;

            if (!tryingFinal)
            {
                if (m.attachedVowel) continue;
                cand = (vowelAttach == VowelAttach.Side) ? m.rightAnchor : m.bottomAnchor;
                if (!CanAttachVowelToBase(m, vowelAttach, glyph)) continue;
            }
            else
            {
               
                if (InvalidFinal.Contains(glyph)) continue;
                cand = m.bottomFinalAnchor;
            }

            if (!cand) continue;
            if (!cand.transform.IsChildOf(m.transform)) continue;

            var a = RectTransformUtility.WorldToScreenPoint(uiCamera, cand.position);
            var me = RectTransformUtility.WorldToScreenPoint(uiCamera, rt.position);
            float d = Vector2.Distance(a, me);
            if (d < bestDist) { bestDist = d; best = m; targetAnchor = cand; }
        }

        if (!best || !targetAnchor || bestDist > snapRadius) return false;

        if (tryingFinal)
        {
            // 이미 받침이 있으면 -> 겹받침 합성 시도
            if (best.attachedFinal)
            {
                return TryFuseFinal(best, best.attachedFinal, this);
            }

            AttachTo(targetAnchor);
            role = JamoRole.Jongseong;
            best.attachedFinal = this;
            return true;
        }
        else
        {
            // (모음 처리 그대로)
            AttachTo(targetAnchor);
            if (vowelAttach == VowelAttach.Side) best.attachedVowelSide = this;
            else best.attachedVowelBelow = this;

            if (!best.attachedVowel) TryFuseVowel(best);
            return true;
        }
    }

    bool CanAttachVowelToBase(JamoMagnet baseCho, VowelAttach incomingType, string incomingGlyph)
    {
        var db = JamoVowelFuseDB.Instance;
        if (!db) return true;

        if (incomingType == VowelAttach.Side && baseCho.attachedVowelBelow != null)
        {
            string below = (baseCho.attachedVowelBelow.glyph ?? "").Trim();
            string side = (incomingGlyph ?? "").Trim();
            return db.Find(below, side) != null;
        }
        if (incomingType == VowelAttach.Below && baseCho.attachedVowelSide != null)
        {
            string below = (incomingGlyph ?? "").Trim();
            string side = (baseCho.attachedVowelSide.glyph ?? "").Trim();
            return db.Find(below, side) != null;
        }
        return true;
    }

    void AttachTo(RectTransform socket)
    {
        rt.SetParent(socket, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = attachOffset;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        rt.SetAsLastSibling();

        var cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        var drag = GetComponent("DraggableWordUI") as Behaviour;
        if (drag) drag.enabled = false;
    }

    void TryFuseVowel(JamoMagnet baseCho)
{
    var below = baseCho.attachedVowelBelow;
    var side  = baseCho.attachedVowelSide;
    if (!below || !side) return;

    var rule = JamoVowelFuseDB.Instance?.Find((below.glyph ?? "").Trim(), (side.glyph ?? "").Trim());
    if (rule == null) return;

    // ---- 새 합성 프리팹 소환 경로 ----
    if (rule.fusedPrefab)
    {
        // 보통 복합모음은 오른쪽 소켓 기준으로 붙입니다.
        var parent = baseCho.rightAnchor ? baseCho.rightAnchor : baseCho.GetComponent<RectTransform>();

        // 1) 프리팹 생성
        var fused = Instantiate(rule.fusedPrefab, parent, false);
        var frt   = fused.GetComponent<RectTransform>();
        if (frt)
        {
            frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.5f);
            frt.pivot     = new Vector2(0.5f, 0.5f);
            frt.anchoredPosition = rule.fusedOffset; // 룰에서 미세 보정
            frt.localScale = Vector3.one;
        }

        // 2) 자석 컴포넌트 보장 + 상태 설정
        var fm = fused.GetComponent<JamoMagnet>() ?? fused.AddComponent<JamoMagnet>();
        fm.role = JamoRole.Jungseong; // 복합 모음은 '중성' 단일 조각
        fm.SetGlyph(!string.IsNullOrEmpty(rule.fusedGlyph)
                    ? rule.fusedGlyph
                    : (below.glyph + side.glyph));    // 글리프 동기화

        // 3) 입력 잠금(베이스만 드래그 가능하게 유지)
        var cg = fused.GetComponent<CanvasGroup>() ?? fused.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        var drag = fused.GetComponent("DraggableWordUI") as Behaviour;
        if (drag) drag.enabled = false;

        // 4) 기존 파트 제거 + 베이스 상태 갱신
        Destroy(below.gameObject);
        Destroy(side.gameObject);
        baseCho.attachedVowelBelow = null;
        baseCho.attachedVowelSide  = null;
        baseCho.attachedVowel      = fm;

        return;
    }

    // ---- (백업 경로) 새 프리팹이 없으면 호스트 글자만 교체 ----
    var parent2 = baseCho.rightAnchor ? baseCho.rightAnchor : baseCho.GetComponent<RectTransform>();
    var host    = side;     // 호스트 유지
    var remove  = below;    // 아래모음 제거

    host.transform.SetParent(parent2, false);
    var hrt = host.GetComponent<RectTransform>();
    if (hrt) hrt.anchoredPosition = rule.fusedOffset;

    host.role = JamoRole.Jungseong;
    host.SetGlyph(!string.IsNullOrEmpty(rule.fusedGlyph) ? rule.fusedGlyph : (below.glyph + side.glyph));

    Destroy(remove.gameObject);
    baseCho.attachedVowelBelow = null;
    baseCho.attachedVowelSide  = null;
    baseCho.attachedVowel      = host;

    var cg2 = host.GetComponent<CanvasGroup>() ?? host.gameObject.AddComponent<CanvasGroup>();
    cg2.blocksRaycasts = false;
    var drag2 = host.GetComponent("DraggableWordUI") as Behaviour;
    if (drag2) drag2.enabled = false;
}

    bool TryFuseFinal(JamoMagnet baseCho, JamoMagnet first, JamoMagnet second)
    {
        var rule = JamoVowelFuseDB.Instance?.Find(first.glyph, second.glyph);
        if (rule == null || !rule.fusedPrefab) return false;

        var parent = baseCho.bottomFinalAnchor ? baseCho.bottomFinalAnchor
                                               : baseCho.GetComponent<RectTransform>();
        var fused = Instantiate(rule.fusedPrefab, parent, false);

        var frt = fused.GetComponent<RectTransform>();
        if (frt)
        {
            frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.5f);
            frt.pivot = new Vector2(0.5f, 0.5f);
            frt.anchoredPosition = rule.fusedOffset;
            frt.localScale = Vector3.one;
            frt.localRotation = Quaternion.identity;
            frt.SetAsLastSibling();
        }

        var fm = fused.GetComponent<JamoMagnet>() ?? fused.AddComponent<JamoMagnet>();
        fm.role = JamoRole.Jongseong;
        fm.SetGlyph(string.IsNullOrEmpty(rule.fusedGlyph) ? (first.glyph + second.glyph) : rule.fusedGlyph);

        Destroy(first.gameObject);
        Destroy(second.gameObject);
        baseCho.attachedFinal = fm;

        var cg = fused.GetComponent<CanvasGroup>() ?? fused.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        var drag = fused.GetComponent("DraggableWordUI") as Behaviour;
        if (drag) drag.enabled = false;

        return true;
    }

    RectTransform EnsureChildSocket(RectTransform socket, string childName)
    {
        if (socket && socket.transform.IsChildOf(transform)) return socket;
        var t = transform.Find(childName);
        var rtChild = t ? t.GetComponent<RectTransform>() : null;
        return rtChild;
    }
}
