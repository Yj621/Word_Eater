using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SyllableBlock : MonoBehaviour
{
    public static SyllableBlock Prefab;

    [Header("UI")]
    public TextMeshProUGUI label;
    public RectTransform centerAnchor;      // 중앙 기준
    public RectTransform vowelSideAnchor;   // 옆모음 기준
    public RectTransform vowelBelowAnchor;  // 아래모음 기준
    public RectTransform finalAnchor;       // 종성 기준
    public float snapRadius = 80f;

    [Header("현재 자모 상태")]
    public string choseong;
    public string jungseong;
    public string jongseong;

    public static readonly List<SyllableBlock> All = new();

    float _baseFontSize;

    // 앵커 타입
    enum AnchorKind { Center, VowelSide, VowelBelow, Final }

    // 복합 모음 테이블
    static readonly Dictionary<(string, string), string> VowelFuse = new()
    {
        { ("ㅗ", "ㅏ"), "ㅘ" }, { ("ㅏ", "ㅗ"), "ㅘ" },
        { ("ㅗ", "ㅐ"), "ㅙ" }, { ("ㅐ", "ㅗ"), "ㅙ" },
        { ("ㅗ", "ㅣ"), "ㅚ" }, { ("ㅣ", "ㅗ"), "ㅚ" },

        { ("ㅜ", "ㅓ"), "ㅝ" }, { ("ㅓ", "ㅜ"), "ㅝ" },
        { ("ㅜ", "ㅔ"), "ㅞ" }, { ("ㅔ", "ㅜ"), "ㅞ" },
        { ("ㅜ", "ㅣ"), "ㅟ" }, { ("ㅣ", "ㅜ"), "ㅟ" },

        { ("ㅡ", "ㅣ"), "ㅢ" }, { ("ㅣ", "ㅡ"), "ㅢ" },
    };

    // 겹받침 테이블
    static readonly Dictionary<(string, string), string> FinalFuse = new()
    {
        { ("ㄱ", "ㅅ"), "ㄳ" },
        { ("ㄴ", "ㅈ"), "ㄵ" }, { ("ㄴ", "ㅎ"), "ㄶ" },
        { ("ㄹ", "ㄱ"), "ㄺ" }, { ("ㄹ", "ㅁ"), "ㄻ" },
        { ("ㄹ", "ㅂ"), "ㄼ" }, { ("ㄹ", "ㅅ"), "ㄽ" },
        { ("ㄹ", "ㅌ"), "ㄾ" }, { ("ㄹ", "ㅍ"), "ㄿ" },
        { ("ㄹ", "ㅎ"), "ㅀ" },
        { ("ㅂ", "ㅅ"), "ㅄ" },
    };

    void Awake()
    {
        if (!All.Contains(this)) All.Add(this);
        if (!Prefab) Prefab = this;

        if (label)
        {
            _baseFontSize = label.fontSize;
            label.enableAutoSizing = false;
        }
    }

    void OnDestroy()
    {
        All.Remove(this);
    }

    // -------------------------
    // 글자 렌더링
    // -------------------------

    public void SetSyllable(string cho, string jung, string jong)
    {
        choseong = cho;
        jungseong = jung;
        jongseong = jong;

        if (!label) return;

        label.enableAutoSizing = false;
        label.fontSize = _baseFontSize;

        // 초성/중성 없으면 조각 그대로
        if (string.IsNullOrEmpty(choseong) || string.IsNullOrEmpty(jungseong))
        {
            label.text = (choseong ?? "") + (jungseong ?? "") + (jongseong ?? "");
            return;
        }

        try
        {
            char syllable = HangulCompose.ComposeCompat(choseong, jungseong, jongseong);
            label.text = syllable.ToString();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SyllableBlock] compose fail L='{choseong}' V='{jungseong}' T='{jongseong}': {e.Message}");
            label.text = (choseong ?? "") + (jungseong ?? "") + (jongseong ?? "");
        }
    }

    // -------------------------
    // 복합 모음 / 겹받침
    // -------------------------

    static bool TryFuseVowel(string v1, string v2, out string fused)
    {
        fused = null;
        if (string.IsNullOrEmpty(v1) || string.IsNullOrEmpty(v2)) return false;
        return VowelFuse.TryGetValue((v1, v2), out fused);
    }

    static bool TryFuseFinal(string t1, string t2, out string fused)
    {
        fused = null;
        if (string.IsNullOrEmpty(t1) || string.IsNullOrEmpty(t2)) return false;
        return FinalFuse.TryGetValue((t1, t2), out fused);
    }

    // -------------------------
    // 이 블럭이 특정 역할로 자모를 받을 수 있는지
    // -------------------------

    bool CanAccept(JamoRole asRole, string glyph)
    {
        bool hasL = !string.IsNullOrEmpty(choseong);
        bool hasV = !string.IsNullOrEmpty(jungseong);
        bool hasT = !string.IsNullOrEmpty(jongseong);

        switch (asRole)
        {
            case JamoRole.Choseong:
                // 완전히 빈 블럭일 때만 초성 허용
                return !hasL && !hasV && !hasT;

            case JamoRole.Jungseong:
                if (!hasL) return false;      // 초성이 있어야 모음 가능
                if (!hasV) return true;       // 모음 비어 있으면 무조건 허용
                // 이미 모음이 있으면 복합 모음만 허용
                return TryFuseVowel(jungseong, glyph, out _);

            case JamoRole.Jongseong:
                if (!hasL || !hasV) return false; // 초+중 없으면 받침 불가
                if (!hasT) return true;           // 받침 비어 있으면 허용
                // 이미 받침이 있으면 겹받침만 허용
                return TryFuseFinal(jongseong, glyph, out _);
        }

        return false;
    }

    bool AttachJamoWithRole(JamoMagnet j, JamoRole asRole)
    {
        if (!j) return false;
        string g = (j.glyph ?? "").Trim();
        if (!CanAccept(asRole, g)) return false;

        switch (asRole)
        {
            case JamoRole.Choseong:
                choseong = g;
                break;

            case JamoRole.Jungseong:
                if (string.IsNullOrEmpty(jungseong))
                {
                    jungseong = g;
                }
                else if (TryFuseVowel(jungseong, g, out var vv))
                {
                    jungseong = vv;
                }
                else return false;
                break;

            case JamoRole.Jongseong:
                if (string.IsNullOrEmpty(jongseong))
                {
                    jongseong = g;
                }
                else if (TryFuseFinal(jongseong, g, out var tt))
                {
                    jongseong = tt;
                }
                else return false;
                break;
        }

        SetSyllable(choseong, jungseong, jongseong);
        Object.Destroy(j.gameObject);
        return true;
    }

    // -------------------------
    // 앵커 선택
    // -------------------------

    static AnchorKind GetClosestAnchor(
        SyllableBlock block,
        Vector2 jamoScreen,
        Camera uiCam,
        out RectTransform targetAnchor)
    {
        targetAnchor = null;

        RectTransform center = block.centerAnchor;
        RectTransform side = block.vowelSideAnchor;
        RectTransform below = block.vowelBelowAnchor;
        RectTransform final = block.finalAnchor;

        float bestDist = float.MaxValue;
        AnchorKind bestKind = AnchorKind.Center;
        RectTransform bestAnchor = null;

        void TryCandidate(RectTransform rt, AnchorKind kind)
        {
            if (!rt) return;
            Vector2 scr = RectTransformUtility.WorldToScreenPoint(uiCam, rt.position);
            float d = Vector2.Distance(jamoScreen, scr);
            if (d < bestDist)
            {
                bestDist = d;
                bestKind = kind;
                bestAnchor = rt;
            }
        }

        TryCandidate(center, AnchorKind.Center);
        TryCandidate(side, AnchorKind.VowelSide);
        TryCandidate(below, AnchorKind.VowelBelow);
        TryCandidate(final, AnchorKind.Final);

        targetAnchor = bestAnchor;
        return bestKind;
    }

    // -------------------------
    // 자모를 가장 가까운 Syl 블럭에 붙이기
    // -------------------------

    public static bool TrySnapJamoToAnyBlock(
        JamoMagnet jamo,
        Camera uiCam,
        bool createIfNone = true)
    {
        if (!jamo) return false;
        var jamoRT = jamo.GetComponent<RectTransform>();
        if (!jamoRT) return false;

        Vector2 jamoScreen = RectTransformUtility.WorldToScreenPoint(uiCam, jamoRT.position);

        // 1. 가까운 블럭 찾기
        SyllableBlock best = null;
        float bestDist = float.MaxValue;

        foreach (var b in All)
        {
            if (!b || !b.centerAnchor) continue;

            Vector2 centerScreen = RectTransformUtility.WorldToScreenPoint(uiCam, b.centerAnchor.position);
            float d = Vector2.Distance(jamoScreen, centerScreen);
            if (d < b.snapRadius && d < bestDist)
            {
                best = b;
                bestDist = d;
            }
        }

        // 2. 블럭 없으면 새로 만들기
        if (!best)
        {
            if (!createIfNone) return false;
            if (!Prefab)
            {
                Debug.LogWarning("[SyllableBlock] Prefab not assigned");
                return false;
            }

            var parent = jamoRT.parent as RectTransform;
            var blockGO = Object.Instantiate(Prefab.gameObject, parent);
            var blockRT = blockGO.GetComponent<RectTransform>();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, jamoScreen, uiCam, out var local);
            blockRT.anchoredPosition = local;
            blockRT.localScale = Vector3.one;

            best = blockGO.GetComponent<SyllableBlock>();

            // 드래그 세팅 복사
            var jamoDrag = jamo.GetComponent<DraggableWordUI>();
            var blockDrag = blockGO.GetComponent<DraggableWordUI>();
            if (jamoDrag && blockDrag)
            {
                blockDrag.Init(jamoDrag.DragRoot, jamoDrag.AllowedArea, jamoDrag.TrashArea, jamoDrag.UiCamera);
            }
        }

        // 3. 어느 앵커에 가까운지 판단
        RectTransform targetAnchor;
        var kind = GetClosestAnchor(best, jamoScreen, uiCam, out targetAnchor);

        bool hasL = !string.IsNullOrEmpty(best.choseong);
        bool hasV = !string.IsNullOrEmpty(best.jungseong);

        // 기본은 자모가 들고 있는 역할
        JamoRole attachRole = jamo.role;

        // ★ 중요 로직:
        // - 초+중이 이미 있고, 아래쪽(아래 모음/종성 앵커) 근처면 → 종성으로 강제
        // - 그 외엔 모음앵커면 중성, 나머지는 prefab role 유지
        switch (kind)
        {
            case AnchorKind.VowelSide:
                // 옆 모음 위치: 모음이면 중성, 자음이면 그냥 원래 role
                if (jamo.role == JamoRole.Jungseong) attachRole = JamoRole.Jungseong;
                break;

            case AnchorKind.VowelBelow:
                if (hasL && hasV && jamo.role != JamoRole.Jungseong)
                {
                    // 초+중 이미 있고 자음을 아래에 붙이면 → 종성
                    attachRole = JamoRole.Jongseong;
                }
                else if (jamo.role == JamoRole.Jungseong)
                {
                    attachRole = JamoRole.Jungseong;
                }
                break;

            case AnchorKind.Final:
                // 종성 앵커 근처 = 무조건 종성
                attachRole = JamoRole.Jongseong;
                break;

            case AnchorKind.Center:
            default:
                // 중앙은 기본 역할 그대로
                break;
        }

        // 4. 실제로 붙이기 시도
        bool ok = best.AttachJamoWithRole(jamo, attachRole);
        return ok;
    }

    // 외부에서 직접 블럭을 생성할 때 사용
    public static SyllableBlock CreateBlockAtScreen(
        Vector2 screenPos,
        RectTransform parent,
        Camera uiCam)
    {
        if (!Prefab)
        {
            Debug.LogError("[SyllableBlock] Prefab 이 등록되어 있지 않음");
            return null;
        }

        var block = Object.Instantiate(Prefab, parent);
        var rt = block.GetComponent<RectTransform>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPos, uiCam, out var local);
        rt.anchoredPosition = local;
        rt.localScale = Vector3.one;
        return block;
    }
}
