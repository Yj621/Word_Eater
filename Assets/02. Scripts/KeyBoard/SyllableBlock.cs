using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SyllableBlock : MonoBehaviour
{
    public static SyllableBlock Prefab;

    [Header("UI")]
    public TextMeshProUGUI label;
    public RectTransform centerAnchor;      // 기본 중심
    public RectTransform vowelSideAnchor;   // 옆모음 스냅용
    public RectTransform vowelBelowAnchor;  // 아래모음 스냅용
    public RectTransform finalAnchor;       // 받침 스냅용
    public float snapRadius = 80f;

    [Header("현재 자모 상태")]
    public string choseong;
    public string jungseong;
    public string jongseong;

    public static readonly List<SyllableBlock> All = new();

    float _baseFontSize;

    enum AnchorKind { Center, VowelSide, VowelBelow, Final }

    // -------------------------
    //   복합 모음 / 겹받침 테이블
    // -------------------------

    // ㅗ+ㅣ=ㅚ, ㅗ+ㅏ=ㅘ, ㅜ+ㅓ=ㅝ, ㅡ+ㅣ=ㅢ 등
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

    // 겹받침: ㄱ+ㅅ=ㄳ, ㄴ+ㅈ=ㄵ, ㄹ+ㄱ=ㄺ, ...
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
        if (!All.Contains(this))
            All.Add(this);

        if (Prefab == null)
            Prefab = this;

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
    //   label에 글자 반영
    // -------------------------

    public void SetSyllable(string cho, string jung, string jong)
    {
        choseong = cho;
        jungseong = jung;
        jongseong = jong;

        if (!label) return;

        label.enableAutoSizing = false;
        label.fontSize = _baseFontSize;

        // 아직 완성 전이면 조각 그대로 보여주기
        if (string.IsNullOrEmpty(choseong) || string.IsNullOrEmpty(jungseong))
        {
            label.text = (choseong ?? "") + (jungseong ?? "") + (jongseong ?? "");
            return;
        }

        // 완성 가능하면 ComposeCompat로 합치기
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
    //   복합 모음 / 겹받침 헬퍼
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
    //   역할별 스냅 기준 앵커 선택
    // -------------------------

    static RectTransform GetTargetAnchorForJamo(SyllableBlock block, JamoMagnet jamo)
    {
        if (!block || jamo == null) return null;

        switch (jamo.role)
        {
            case JamoRole.Choseong:
                return block.centerAnchor;

            case JamoRole.Jungseong:
                if (jamo.vowelAttach == VowelAttach.Side)
                    return block.vowelSideAnchor ? block.vowelSideAnchor : block.centerAnchor;
                else
                    return block.vowelBelowAnchor ? block.vowelBelowAnchor : block.centerAnchor;

            case JamoRole.Jongseong:
                return block.finalAnchor ? block.finalAnchor : block.centerAnchor;
        }

        return block.centerAnchor;
    }

    // -------------------------
    //   이 블럭이 자모를 받을 수 있는지
    // -------------------------

    bool CanAccept(JamoMagnet j, JamoRole asRole)
    {
        if (!j) return false;

        bool hasL = !string.IsNullOrEmpty(choseong);
        bool hasV = !string.IsNullOrEmpty(jungseong);
        bool hasT = !string.IsNullOrEmpty(jongseong);
        string g = (j.glyph ?? "").Trim();

        switch (asRole)
        {
            case JamoRole.Choseong:
                // 완전 빈 블럭일 때만 초성 허용
                return !hasL && !hasV && !hasT;

            case JamoRole.Jungseong:
                // 초성이 있어야 모음 허용
                if (!hasL) return false;

                // 모음이 없으면 OK
                if (!hasV) return true;

                // 이미 모음이 있으면 → 복합 모음으로 합칠 수 있을 때만 허용
                return TryFuseVowel(jungseong, g, out _);

            case JamoRole.Jongseong:
                // 초+중이 있어야 받침 허용
                if (!hasL || !hasV) return false;

                // 받침이 없으면 OK
                if (!hasT) return true;

                // 이미 받침 있으면 → 겹받침으로 합칠 수 있을 때만 허용
                return TryFuseFinal(jongseong, g, out _);
        }

        return false;
    }

    bool CanAccept(JamoMagnet j) => CanAccept(j, j.role);

    // -------------------------
    //   실제로 블럭 상태 갱신 + 자모 제거
    // -------------------------

    bool AttachJamoWithRole(JamoMagnet j, JamoRole asRole)
    {
        if (!CanAccept(j, asRole)) return false;

        string g = (j.glyph ?? "").Trim();

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
                else
                {
                    if (TryFuseVowel(jungseong, g, out var fused))
                        jungseong = fused;
                    else
                        return false;
                }
                break;

            case JamoRole.Jongseong:
                if (string.IsNullOrEmpty(jongseong))
                {
                    jongseong = g;
                }
                else
                {
                    if (TryFuseFinal(jongseong, g, out var fusedT))
                        jongseong = fusedT;
                    else
                        return false;
                }
                break;
        }

        SetSyllable(choseong, jungseong, jongseong);
        Object.Destroy(j.gameObject);
        return true;
    }

    bool AttachJamo(JamoMagnet j) => AttachJamoWithRole(j, j.role);

    // -------------------------
    //   자모를 가장 가까운 블럭에 붙이기
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

        // 1. 기존 블럭 중 "centerAnchor" 기준으로 가장 가까운 것 찾기
        SyllableBlock bestBlock = null;
        float bestBlockDist = float.MaxValue;

        foreach (var b in All)
        {
            if (!b || !b.centerAnchor) continue;

            Vector2 centerScreen = RectTransformUtility.WorldToScreenPoint(uiCam, b.centerAnchor.position);
            float d = Vector2.Distance(jamoScreen, centerScreen);
            if (d < b.snapRadius && d < bestBlockDist)
            {
                bestBlock = b;
                bestBlockDist = d;
            }
        }

        // 2. 근처 블럭이 없으면 새 블럭 생성 (옵션)
        if (!bestBlock)
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

            bestBlock = blockGO.GetComponent<SyllableBlock>();

            // 드래그 설정 복사
            var jamoDrag = jamo.GetComponent<DraggableWordUI>();
            var blockDrag = blockGO.GetComponent<DraggableWordUI>();
            if (jamoDrag && blockDrag)
            {
                blockDrag.Init(jamoDrag.DragRoot, jamoDrag.AllowedArea, jamoDrag.TrashArea, jamoDrag.UiCamera);
            }
        }

        // 3. 이 블럭 안에서 "어느 앵커가 가장 가까운지"를 보고 역할 결정
        RectTransform targetAnchor;
        AnchorKind anchorKind = GetClosestAnchor(bestBlock, jamoScreen, uiCam, out targetAnchor);

        // 기본은 자모의 원래 role
        JamoRole effectiveRole = jamo.role;

        // 블럭의 현재 상태
        bool hasL = !string.IsNullOrEmpty(bestBlock.choseong);
        bool hasV = !string.IsNullOrEmpty(bestBlock.jungseong);
        bool hasT = !string.IsNullOrEmpty(bestBlock.jongseong);

        switch (anchorKind)
        {
            case AnchorKind.Final:
                // ★ 초성과 중성이 이미 있는 상태에서 아래(final)에 떨어지면 무조건 종성 취급
                if (hasL && hasV)
                    effectiveRole = JamoRole.Jongseong;
                break;

            case AnchorKind.VowelSide:
            case AnchorKind.VowelBelow:
                // 옆/아래 모음 자리면 중성으로 취급 (role이 Jong이어도 위치가 모음자리면 막을 수도 있음)
                effectiveRole = JamoRole.Jungseong;
                break;

            case AnchorKind.Center:
                // 가운데는 기본적으로 초성/중성 판단:
                //  - 블럭이 완전히 비어 있으면 초성
                //  - 초성이 없고, 자모가 모음이라면 중성으로도 처리 가능 (원하면 확장)
                if (!hasL && !hasV && !hasT)
                    effectiveRole = JamoRole.Choseong;
                break;
        }

        // 4. 최종 역할을 가지고 실제 붙이기 시도
        bool applied = bestBlock.AttachJamoWithRole(jamo, effectiveRole);
        if (!applied)
            return false;

        return true;
    }


    // -------------------------
    //   화면 좌표에 새 블럭 만들기
    // -------------------------

    public static SyllableBlock CreateBlockAtScreen(
        Vector2 screenPos,
        RectTransform parent,
        Camera uiCam)
    {
        if (Prefab == null)
        {
            Debug.LogError("[SyllableBlock] Prefab 이 등록되어 있지 않음");
            return null;
        }

        var block = Object.Instantiate(Prefab, parent);
        var rt = block.GetComponent<RectTransform>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent, screenPos, uiCam, out var local);
        rt.anchoredPosition = local;
        rt.localScale = Vector3.one;

        return block;
    }

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

        // 후보들 검사
        TryCandidate(center, AnchorKind.Center);
        TryCandidate(side, AnchorKind.VowelSide);
        TryCandidate(below, AnchorKind.VowelBelow);
        TryCandidate(final, AnchorKind.Final);

        targetAnchor = bestAnchor;             
        return bestKind;
    }
}

