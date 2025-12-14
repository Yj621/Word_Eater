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

    [Header("초성(베이스) 소켓 (이제는 거의 사용 안 함)")]
    public RectTransform rightAnchor;        // 옆모음
    public RectTransform bottomAnchor;       // 아래모음
    public RectTransform bottomFinalAnchor;  // 받침

    [Header("모음 전용")]
    public VowelAttach vowelAttach = VowelAttach.Side;

    [Header("스냅 옵션")]
    public float snapRadius = 80f;
    public Vector2 attachOffset;

    [Header("모음 오프셋(개별 프리팹 전용)")]
    public Vector2 prefabAttachOffset = Vector2.zero;

    // 예전 구조에서 쓰던 필드들 (지금은 안 써도 상관 없음)
    [HideInInspector] public JamoMagnet attachedVowel, attachedFinal;
    [HideInInspector] public JamoMagnet attachedVowelSide, attachedVowelBelow;

    RectTransform rt;
    public static readonly HashSet<JamoMagnet> All = new();

    // 받침으로 쓸 수 없는 자모 (예전 구조용, 지금은 사실 의미 거의 없음)
    static readonly HashSet<string> InvalidFinal = new() { "ㄸ", "ㅉ", "ㅃ" };

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        All.Add(this);

        // 모음 방향 자동 추정 (ㅗ/ㅛ/ㅜ/ㅠ/ㅡ 는 아래로, 나머지는 옆)
        if (role == JamoRole.Jungseong && !string.IsNullOrEmpty(glyph))
            vowelAttach = GuessVowelAttach(glyph);

        // 예전 구조의 초성 소켓들 – 지금은 거의 안 쓰지만, 프리팹 호환용으로 남겨둠
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

    public void SetGlyph(string g)
    {
        glyph = g;
        // 필요하면 여기서 TMP에 적용해도 됨
    }

    public bool TrySnap(RectTransform dragRoot, Camera uiCamera)
    {
        // 1) 완성 글자 블록에 붙이기 시도
        if (SyllableBlock.TrySnapJamoToAnyBlock(this, uiCamera))
        {
            return true;
        }

        return false;
    }

    bool HasAnySockets()
        => (rightAnchor != null) || (bottomAnchor != null) || (bottomFinalAnchor != null);

    void AttachTo(RectTransform socket)
    {
        // 지금은 거의 안 쓰지만, 혹시라도 직접 붙여야 할 때를 대비해 유지
        rt.SetParent(socket, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = attachOffset + prefabAttachOffset;

        bool isStretched = (rt.anchorMin != rt.anchorMax);
        if (isStretched)
        {
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        rt.SetAsLastSibling();

        var cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        var drag = GetComponent("DraggableWordUI") as Behaviour;
        if (drag) drag.enabled = false;
    }

    bool CanAttachVowelToBase(JamoMagnet baseCho, VowelAttach incomingType, string incomingGlyph)
    {
        // 예전 모음 합성용 – 지금은 사용 안 함.
        return true;
    }

    void TryFuseVowel(JamoMagnet baseCho)
    {
        // 예전 복합 모음(ㅘ, ㅝ 등) 합성 로직.
        // 이제는 SyllableBlock이 단일 glyph만 들고 있으니
        // 필요하다면 나중에 "SyllableBlock 안에서" 구현하는 게 낫다.
    }

    bool TryFuseFinal(JamoMagnet baseCho, JamoMagnet first, JamoMagnet second)
    {
        // 예전 겹받침(ㄳ, ㄵ 등) 합성용 – 역시 SyllableBlock 쪽으로 옮기는 게 좋음.
        return false;
    }

    RectTransform EnsureChildSocket(RectTransform socket, string childName)
    {
        if (socket && socket.transform.IsChildOf(transform)) return socket;
        var t = transform.Find(childName);
        var rtChild = t ? t.GetComponent<RectTransform>() : null;
        return rtChild;
    }

    public static bool TryFuseWithNearbyJamo(JamoMagnet a, Camera uiCam, RectTransform parent, float radius)
    {
        if (!a) return false;

        var art = a.GetComponent<RectTransform>();
        if (!art) return false;

        Vector2 aScreen = RectTransformUtility.WorldToScreenPoint(uiCam, art.position);

        JamoMagnet best = null;
        float bestDist = float.MaxValue;

        foreach (var b in JamoMagnet.All)
        {
            if (!b || b == a) continue;

            // 허용구역 밖에 있는 자모랑 합쳐지는 걸 막고 싶으면
            // 여기서 parent(=dragRoot) 안에 있는지 체크해도 됨.
            // if (b.transform.parent != parent) continue;

            var brt = b.GetComponent<RectTransform>();
            if (!brt) continue;

            Vector2 bScreen = RectTransformUtility.WorldToScreenPoint(uiCam, brt.position);

            float d = Vector2.Distance(aScreen, bScreen);
            if (d < radius && d < bestDist)
            {
                best = b;
                bestDist = d;
            }
        }

        if (!best) return false;

        // 지금은 "초성+중성"만 합치기
        JamoMagnet cho = null, jung = null;
        if (a.role == JamoRole.Choseong && best.role == JamoRole.Jungseong) { cho = a; jung = best; }
        else if (a.role == JamoRole.Jungseong && best.role == JamoRole.Choseong) { cho = best; jung = a; }
        else return false;

        // 이미 cho가 다른 중성/종성이 붙은 상태면(침투 방지) 합치기 금지
        // (니 구조에서 attached*를 거의 안 쓴다고 했지만, 혹시 남아있으면 안전장치)
        if (cho.attachedVowel || cho.attachedVowelSide || cho.attachedVowelBelow || cho.attachedFinal)
            return false;

        if (!SyllableBlock.Prefab) return false;

        // 블럭 생성
        var block = Object.Instantiate(SyllableBlock.Prefab, parent);
        var blockRT = block.GetComponent<RectTransform>();
        if (blockRT)
        {
            // 생성 위치: 둘의 중간
            Vector2 bScr = RectTransformUtility.WorldToScreenPoint(uiCam, best.GetComponent<RectTransform>().position);
            Vector2 midScreen = (aScreen + bScr) * 0.5f;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, midScreen, uiCam, out var local);
            blockRT.anchoredPosition = local;
            blockRT.localScale = Vector3.one;
        }

        // 값 세팅
        block.choseong = (cho.glyph ?? "").Trim();
        block.jungseong = (jung.glyph ?? "").Trim();
        block.jongseong = null;
        block.SetSyllable(block.choseong, block.jungseong, block.jongseong);

        // 중요: 블럭도 드래그 가능하게 Init 복사
        var srcDrag = a.GetComponent<DraggableWordUI>();
        var blockDrag = block.GetComponent<DraggableWordUI>();
        if (srcDrag && blockDrag)
        {
            blockDrag.Init(srcDrag.DragRoot, srcDrag.AllowedArea, srcDrag.TrashArea, srcDrag.UiCamera);

            // (선택) 환불/삭제까지 이어가려면 source도 넘겨줘야 함
            // 합쳐진 2개의 비용 처리 방식은 정책이 필요하지만,
            // 일단 "드롭한 쪽"만 넘기면 최소한 삭제시 환불은 됨.
            // 더 정확히 하려면 아래 "합산 환불" 참고.
            // blockDrag.BindSource(srcDragOwner, index, amount) → 현재 여기선 owner 접근이 어려움
        }

        // Raycast 복구
        var cg = block.GetComponent<CanvasGroup>();
        if (cg) cg.blocksRaycasts = true;

        // 원본 제거
        Object.Destroy(cho.gameObject);
        Object.Destroy(jung.gameObject);

        return true;
    }

}
