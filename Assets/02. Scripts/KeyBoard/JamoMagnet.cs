using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public enum JamoRole { Choseong, Jungseong, Jongseong }    // 자모 역할
public enum VowelAttach { Side, Below }                    // 모음 방향 힌트

[RequireComponent(typeof(RectTransform))]
public class JamoMagnet : MonoBehaviour
{
    [Header("자모 기본 정보")]
    public JamoRole role = JamoRole.Choseong;              // 초/중/종 구분
    public string glyph;                                   // 표시 문자(ㄱ, ㅏ 등)

    [Header("모음 방향 힌트(옵션)")]
    public VowelAttach vowelAttach = VowelAttach.Side;     // ㅗ/ㅛ/ㅜ/ㅠ/ㅡ → Below, 나머지 Side

    [Header("스냅 범위")]
    public float snapRadius = 80f;                         // 근처 자모/Syl 탐색 반경

    RectTransform rt;


    // 근처 자모 탐색용 글로벌 리스트
    public static readonly HashSet<JamoMagnet> All = new HashSet<JamoMagnet>();

    void OnEnable()
    {
        rt = GetComponent<RectTransform>();
        All.Add(this);

        // 모음이면 글자 보고 방향 추정
        if (role == JamoRole.Jungseong && !string.IsNullOrEmpty(glyph))
            vowelAttach = GuessVowelAttach(glyph);
    }

    void OnDisable()
    {
        All.Remove(this);
    }

    /// <summary>모음 글자로부터 기본 방향 추정</summary>
    public static VowelAttach GuessVowelAttach(string g)
    {
        if (g == "ㅗ" || g == "ㅛ" || g == "ㅜ" || g == "ㅠ" || g == "ㅡ")
            return VowelAttach.Below;
        return VowelAttach.Side;
    }

    /// <summary>자모 글자 교체용</summary>
    public void SetGlyph(string g) => glyph = g;

    /// <summary>
    /// 드래그 끝났을 때 호출: 
    /// 1) 기존 Syl 블럭에 붙이기 시도
    /// 2) 실패하면 근처 자모(초+중)와 합쳐서 새 Syl 생성
    /// </summary>
    public bool TrySnap(RectTransform dragRoot, Camera uiCamera)
    {
        // 1) 기존 블럭에 붙이기 (새 블럭 생성은 false)
        if (SyllableBlock.TrySnapJamoToAnyBlock(this, uiCamera, createIfNone: false))
            return true;

        // 2) 근처 자모(초+중)끼리 합쳐서 새 블럭 만들기
        if (dragRoot && JamoMagnet.TryFuseWithNearbyJamo(this, uiCamera, dragRoot, snapRadius))
            return true;

        return false;
    }


    /// <summary>
    /// a 주변에서 반경 radius 내의 다른 자모를 찾아
    /// 초성+중성 조합이면 새 SyllableBlock을 만들고 둘 다 없앤다.
    /// </summary>
    public static bool TryFuseWithNearbyJamo(JamoMagnet a, Camera uiCam, RectTransform parent, float radius)
    {
        if (!a || !parent) return false;
        var art = a.GetComponent<RectTransform>();
        if (!art) return false;

        Vector2 aScreen = RectTransformUtility.WorldToScreenPoint(uiCam, art.position);

        JamoMagnet best = null;
        float bestDist = radius;

        // 가장 가까운 자모 찾기
        foreach (var b in All)
        {
            if (!b || b == a) continue;

            var brt = b.GetComponent<RectTransform>();
            if (!brt) continue;

            Vector2 bScreen = RectTransformUtility.WorldToScreenPoint(uiCam, brt.position);
            float d = Vector2.Distance(aScreen, bScreen);
            if (d < bestDist)
            {
                best = b;
                bestDist = d;
            }
        }

        if (!best) return false;

        // 초+중 조합만 허용
        JamoMagnet cho = null, jung = null;
        if (a.role == JamoRole.Choseong && best.role == JamoRole.Jungseong) { cho = a; jung = best; }
        else if (a.role == JamoRole.Jungseong && best.role == JamoRole.Choseong) { cho = best; jung = a; }
        else return false;

        if (!SyllableBlock.Prefab) return false;

        // 새 Syl 블럭 생성 (두 자모의 중간 위치)
        var block = Object.Instantiate(SyllableBlock.Prefab, parent);
        var blockRT = block.GetComponent<RectTransform>();
        if (blockRT)
        {
            Vector2 bScr = RectTransformUtility.WorldToScreenPoint(uiCam, best.GetComponent<RectTransform>().position);
            Vector2 midScreen = (aScreen + bScr) * 0.5f;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, midScreen, uiCam, out var local);
            blockRT.anchoredPosition = local;
            blockRT.localScale = Vector3.one;
        }

        // 음절 정보 세팅
        block.choseong = (cho.glyph ?? "").Trim();
        block.jungseong = (jung.glyph ?? "").Trim();
        block.jongseong = null;
        block.SetSyllable(block.choseong, block.jungseong, null);
        block.PlayBirthAnim();

        // 블럭도 드래그 가능하게 설정 복사
        var srcDrag = a.GetComponent<DraggableWordUI>();
        var blockDrag = block.GetComponent<DraggableWordUI>();
        if (srcDrag && blockDrag)
        {
            blockDrag.Init(srcDrag.DragRoot, srcDrag.AllowedArea, srcDrag.TrashArea, srcDrag.UiCamera);
        }

        // 원본 자모 삭제
        Object.Destroy(cho.gameObject);
        Object.Destroy(jung.gameObject);

        return true;
    }

    #region DOTween Animations

    /// <summary>키에서 뽑아졌을 때 “툭” 튀어나오는 느낌</summary>
    public void PlaySpawnAnim()
    {
        var rt = GetComponent<RectTransform>();
        var cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        rt.localScale = Vector3.zero;
        cg.alpha = 0f;

        Sequence seq = DOTween.Sequence();
        seq.Append(cg.DOFade(1f, 0.15f));
        seq.Join(rt.DOScale(1.1f, 0.15f).SetEase(Ease.OutBack));
        seq.Append(rt.DOScale(1f, 0.08f));
    }

    /// <summary>삭제될 때 (쓰레기통 or 범위 밖) 쑥 빨려들어가듯 사라짐</summary>
    /// <param name="trash">쓰레기통 RectTransform (null이면 제자리에서 축소)</param>
    public void PlayTrashAnim(RectTransform trash, System.Action onComplete)
    {
        var rt = GetComponent<RectTransform>();
        rt.DOKill();

        Vector3 targetPos = trash ? trash.position : rt.position;

        Sequence seq = DOTween.Sequence();
        seq.Append(rt.DOMove(targetPos, 0.15f).SetEase(Ease.InQuad));
        seq.Join(rt.DOScale(0f, 0.15f));
        seq.Join(rt.DORotate(new Vector3(0, 0, 180f), 0.15f, RotateMode.FastBeyond360));
        seq.OnComplete(() => onComplete?.Invoke());
    }

    #endregion

}
