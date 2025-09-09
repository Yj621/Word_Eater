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
            AttachTo(targetAnchor);
            role = JamoRole.Jongseong;
            best.attachedFinal = this;
            return true;
        }
        else
        {
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
        var side = baseCho.attachedVowelSide;
        if (!below || !side) return;

        var rule = JamoVowelFuseDB.Instance?.Find((below.glyph ?? "").Trim(), (side.glyph ?? "").Trim());
        if (rule == null) return;

        var parent = baseCho.rightAnchor ? baseCho.rightAnchor : baseCho.GetComponent<RectTransform>();

        var host = side;
        var remove = below;

        host.transform.SetParent(parent, false);
        var hrt = host.GetComponent<RectTransform>();
        hrt.anchoredPosition = rule.fusedOffset;

        host.role = JamoRole.Jungseong;
        host.SetGlyph(!string.IsNullOrEmpty(rule.fusedGlyph) ? rule.fusedGlyph : (below.glyph + side.glyph));

        Destroy(remove.gameObject);
        baseCho.attachedVowelBelow = null;
        baseCho.attachedVowelSide = null;
        baseCho.attachedVowel = host;

        var cg = host.GetComponent<CanvasGroup>() ?? host.gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        var drag = host.GetComponent("DraggableWordUI") as Behaviour;
        if (drag) drag.enabled = false;
    }

    RectTransform EnsureChildSocket(RectTransform socket, string childName)
    {
        if (socket && socket.transform.IsChildOf(transform)) return socket;
        var t = transform.Find(childName);
        var rtChild = t ? t.GetComponent<RectTransform>() : null;
        return rtChild;
    }
}
