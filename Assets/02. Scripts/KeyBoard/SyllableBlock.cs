using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class SyllableBlock : MonoBehaviour
{
    public static SyllableBlock Prefab;

    [Header("UI")]
    public TextMeshProUGUI label;
    public RectTransform centerAnchor;
    public RectTransform vowelSideAnchor;
    public RectTransform vowelBelowAnchor;
    public RectTransform finalAnchor;
    public float snapRadius = 80f;

    // 현재 조합 상태
    public string choseong;
    public string jungseong;
    public string jongseong;
    float _baseFontSize;
    public static readonly List<SyllableBlock> All = new List<SyllableBlock>();

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

    public void SetSyllable(string cho, string jung, string jong)
    {
        choseong = cho;
        jungseong = jung;
        jongseong = jong;

        if (!label) return;

        label.enableAutoSizing = false;
        label.fontSize = _baseFontSize;

        // 1) 아직 완성 전이면 – 조각 그대로 보여주기만 하고 리턴
        if (string.IsNullOrEmpty(choseong) || string.IsNullOrEmpty(jungseong))
        {
            label.text = (choseong ?? "") + (jungseong ?? "") + (jongseong ?? "");
            return;
        }

        // 2) 완성 가능하면 ComposeCompat로 합치기
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

    public static bool TrySnapJamoToAnyBlock(JamoMagnet jamo, Camera uiCam, bool createIfNone = false)
    {
        if (!jamo) return false;

        var jamoRT = jamo.GetComponent<RectTransform>();
        if (!jamoRT) return false;

        Vector2 jamoScreen = RectTransformUtility.WorldToScreenPoint(uiCam, jamoRT.position);

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

        // ❗️기존엔 여기서 무조건 생성했는데,
        // 이제는 createIfNone이 true일 때만 생성하도록 바꿈
        if (!best)
        {
            if (!createIfNone) return false;

            var prefab = SyllableBlock.Prefab;
            if (!prefab)
            {
                Debug.LogWarning("[SyllableBlock] BlockPrefab not assigned");
                return false;
            }

            var parent = jamoRT.parent as RectTransform;
            var blockGO = Object.Instantiate(prefab.gameObject, parent);
            var blockRT = blockGO.GetComponent<RectTransform>();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, jamoScreen, uiCam, out var local);
            blockRT.anchoredPosition = local;
            blockRT.localScale = Vector3.one;

            best = blockGO.GetComponent<SyllableBlock>();

            var jamoDrag = jamo.GetComponent<DraggableWordUI>();
            var blockDrag = blockGO.GetComponent<DraggableWordUI>();
            if (jamoDrag && blockDrag)
                blockDrag.Init(jamoDrag.DragRoot, jamoDrag.AllowedArea, jamoDrag.TrashArea, jamoDrag.UiCamera);
        }

        // 역할에 따라 채우기
        if (jamo.role == JamoRole.Choseong) best.choseong = jamo.glyph;
        else if (jamo.role == JamoRole.Jungseong) best.jungseong = jamo.glyph;
        else best.jongseong = jamo.glyph;

        best.SetSyllable(best.choseong, best.jungseong, best.jongseong);

        Object.Destroy(jamo.gameObject);
        return true;
    }



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

        return block;
    }

    public static SyllableBlock ConvertJamoToBlock(JamoMagnet jamo, Camera uiCam)
    {
        if (!jamo) return null;

        var jamoRT = jamo.GetComponent<RectTransform>();
        if (!jamoRT) return null;

        if (!Prefab)
        {
            Debug.LogWarning("[SyllableBlock] BlockPrefab not assigned");
            return null;
        }

        var parent = jamoRT.parent as RectTransform;
        if (!parent) return null;

        // 현재 자모 위치(화면좌표)
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(uiCam, jamoRT.position);

        // 블럭 생성
        var blockGO = Instantiate(Prefab.gameObject, parent);
        var block = blockGO.GetComponent<SyllableBlock>();
        var blockRT = blockGO.GetComponent<RectTransform>();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, uiCam, out var local);
        blockRT.anchoredPosition = local;
        blockRT.localScale = Vector3.one;

        // 드래그 설정 복사
        var jamoDrag = jamo.GetComponent<DraggableWordUI>();
        var blockDrag = blockGO.GetComponent<DraggableWordUI>();
        if (jamoDrag && blockDrag)
            blockDrag.Init(jamoDrag.DragRoot, jamoDrag.AllowedArea, jamoDrag.TrashArea, jamoDrag.UiCamera);

        // 역할에 맞게 "부분"만 채움
        if (jamo.role == JamoRole.Choseong) block.choseong = jamo.glyph;
        else if (jamo.role == JamoRole.Jungseong) block.jungseong = jamo.glyph;
        else block.jongseong = jamo.glyph;

        block.SetSyllable(block.choseong, block.jungseong, block.jongseong);

        Destroy(jamo.gameObject);
        return block;
    }

    bool CanAccept(JamoMagnet j)
    {
        if (!j) return false;

        bool hasL = !string.IsNullOrEmpty(choseong);
        bool hasV = !string.IsNullOrEmpty(jungseong);
        bool hasT = !string.IsNullOrEmpty(jongseong);

        if (j.role == JamoRole.Choseong)
        {
            // 블럭이 완전 비어있을 때만 초성 받기
            return !hasL && !hasV && !hasT;
        }

        if (j.role == JamoRole.Jungseong)
        {
            // 초성 있어야 모음 받기
            if (!hasL) return false;

            // 모음이 없을 때만 허용 (복합모음은 나중에)
            return !hasV;
        }

        // 종성
        {
            // 초+중 완성된 블럭에만 허용
            if (!hasL || !hasV) return false;

            // 종성 없을 때만 허용 (겹받침은 나중에)
            return !hasT;
        }
    }

}
