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

    // ★ 복합 모음 테이블 (ㅗ+ㅣ=ㅚ 포함)
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

    // ★ 겹받침 테이블
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

    /// <summary>
    /// 현재 블럭의 자모 상태를 세팅하고 label에 반영
    /// </summary>
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
    //   복합 모음 / 겹받침
    // -------------------------

    static bool TryFuseVowel(string v1, string v2, out string fused)
    {
        if (string.IsNullOrEmpty(v1) || string.IsNullOrEmpty(v2))
        {
            fused = null;
            return false;
        }

        if (VowelFuse.TryGetValue((v1, v2), out fused))
            return true;

        fused = null;
        return false;
    }

    static bool TryFuseFinal(string t1, string t2, out string fused)
    {
        if (string.IsNullOrEmpty(t1) || string.IsNullOrEmpty(t2))
        {
            fused = null;
            return false;
        }

        if (FinalFuse.TryGetValue((t1, t2), out fused))
            return true;

        fused = null;
        return false;
    }

    // -------------------------
    //   이 블럭이 해당 자모를 받을 수 있는지
    // -------------------------

    bool CanAccept(JamoMagnet j, JamoRole asRole)
    {
        if (!j) return false;

        bool hasL = !string.IsNullOrEmpty(choseong);
        bool hasV = !string.IsNullOrEmpty(jungseong);
        bool hasT = !string.IsNullOrEmpty(jongseong);

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
                return TryFuseVowel(jungseong, j.glyph, out _);

            case JamoRole.Jongseong:
                // 초+중이 있어야 받침 허용
                if (!hasL || !hasV) return false;

                // 받침이 없으면 OK
                if (!hasT) return true;

                // 이미 받침 있으면 → 겹받침으로 합칠 수 있을 때만 허용
                return TryFuseFinal(jongseong, j.glyph, out _);
        }

        return false;
    }

    bool CanAccept(JamoMagnet j) => CanAccept(j, j.role);

    // -------------------------
    //   실제로 블럭 내부 상태 갱신
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
    //   ↓ 여기서 "교 + ㅇ → 굥" 같은 거 위치 기반으로 처리
    // -------------------------

    public static bool TrySnapJamoToAnyBlock(JamoMagnet jamo, Camera uiCam, bool createIfNone = false)
    {
        if (!jamo) return false;

        var jamoRT = jamo.GetComponent<RectTransform>();
        if (!jamoRT) return false;

        Vector2 jamoScreen = RectTransformUtility.WorldToScreenPoint(uiCam, jamoRT.position);

        SyllableBlock bestBlock = null;
        JamoRole bestRole = jamo.role;
        float bestDist = float.MaxValue;

        foreach (var b in All)
        {
            if (!b) continue;

            // 이 블럭에 대해 "어떤 역할로 붙일지" 후보들을 전부 본다.
            void Consider(RectTransform anchor, JamoRole asRole)
            {
                if (!anchor) return;

                Vector2 anchorScreen = RectTransformUtility.WorldToScreenPoint(uiCam, anchor.position);
                float d = Vector2.Distance(jamoScreen, anchorScreen);
                float radius = b.snapRadius;

                if (d < radius && d < bestDist)
                {
                    bestDist = d;
                    bestBlock = b;
                    bestRole = asRole;
                }
            }

            // 1) 자모가 원래 Jungseong이면, Side/Below 기준
            if (jamo.role == JamoRole.Jungseong)
            {
                RectTransform anchor = null;
                if (jamo.vowelAttach == VowelAttach.Side)
                    anchor = b.vowelSideAnchor ? b.vowelSideAnchor : b.centerAnchor;
                else
                    anchor = b.vowelBelowAnchor ? b.vowelBelowAnchor : b.centerAnchor;

                Consider(anchor, JamoRole.Jungseong);
            }
            // 2) 자모가 원래 Jongseong이면, finalAnchor 기준
            else if (jamo.role == JamoRole.Jongseong)
            {
                RectTransform anchor = b.finalAnchor ? b.finalAnchor : b.centerAnchor;
                Consider(anchor, JamoRole.Jongseong);
            }
            // 3) 자모가 Choseong이면, "초성"과 "받침" 두 가지 후보를 모두 본다.
            else // jamo.role == JamoRole.Choseong
            {
                // (1) 초성 후보: 비어 있는 블럭에 붙을 때
                if (b.centerAnchor)
                    Consider(b.centerAnchor, JamoRole.Choseong);

                // (2) 받침 후보: 이미 초+중이 완성된 블럭이라면 finalAnchor에 떨어졌을 때 종성으로 취급
                if (!string.IsNullOrEmpty(b.choseong) &&
                    !string.IsNullOrEmpty(b.jungseong) &&
                    b.finalAnchor)
                {
                    Consider(b.finalAnchor, JamoRole.Jongseong);
                }
            }
        }

        // 근처에 스냅 가능한 블럭이 없으면
        if (!bestBlock)
        {
            if (!createIfNone)
                return false;

            // 자모 위치에 새 블럭을 만들고 싶으면 여기서 생성 로직을 추가하면 되지만,
            // 지금 구조(자모끼리 합쳐서 블럭 생성)에서는 사용하지 않음.
            return false;
        }

        // 최종적으로 선택된 블럭 + 역할로 붙이기
        return bestBlock.AttachJamoWithRole(jamo, bestRole);
    }

    // -------------------------
    //   외부에서 화면 좌표에 블럭 생성하고 싶을 때
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
}
