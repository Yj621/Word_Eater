using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PhoneSwiper : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("필수 참조")]
    public RectTransform viewport;   // 폰 화면 RectTransform (보이는 프레임)
    public RectTransform content;    // 페이지들을 담는 컨테이너

    [Header("페이지 (비우면 content의 자식 자동 수집)")]
    public RectTransform[] pages;

    [Header("페이지 인디케이터 (점)")]
    public Image[] dots;             // 점 이미지들
    public Color dotActive = Color.white;
    public Color dotInactive = new Color(1, 1, 1, 0.3f);

    [Header("동작 설정")]
    public int startPage = 0;
    public float swipeThreshold = 120f;   // 드래그 종결 시 페이지 전환 임계 픽셀
    public float snapSpeed = 12f;         // 스냅 속도(클수록 빠름)
    public bool useUnscaledTime = true;
    public bool isUsingTab = false;

    int pageCount;
    int current;
    float pageWidth;
    Vector2 dragStartPointer;
    Vector2 dragStartContentPos;
    bool dragging;
    Coroutine snapCo;

    void Awake()
    {
        isUsingTab = false;
        if (!viewport) viewport = transform as RectTransform;

        if (pages == null || pages.Length == 0)
            pages = content.Cast<Transform>().Select(t => t as RectTransform).Where(r => r != null).ToArray();

        pageCount = pages.Length;
        startPage = Mathf.Clamp(startPage, 0, Mathf.Max(0, pageCount - 1));
        current = startPage;

        EnsureViewportMask();
        Relayout();
        JumpTo(current);
        UpdateDots();
    }

    void EnsureViewportMask()
    {
        // 화면 밖 안 보이도록
        if (viewport && !viewport.GetComponent<Mask>())
        {
            var img = viewport.GetComponent<Image>();
            if (!img) img = viewport.gameObject.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0); // 투명
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        }
    }

    void OnRectTransformDimensionsChange()
    {
        // 해상도/캔버스 스케일 변경 시 레이아웃 재계산
        if (viewport && content && pageCount > 0)
        {
            Relayout();
            JumpTo(current);
        }
    }

    void Relayout()
    {
        pageWidth = viewport.rect.width;

        // content는 가운데 기준
        content.anchorMin = content.anchorMax = new Vector2(0.5f, 0.5f);
        content.pivot = new Vector2(0.5f, 0.5f);

        // 각 페이지를 가로로 나열(화면 크기에 맞춰 꽉 채움)
        for (int i = 0; i < pageCount; i++)
        {
            var p = pages[i];
            p.anchorMin = p.anchorMax = new Vector2(0.5f, 0.5f);
            p.pivot = new Vector2(0.5f, 0.5f);
            p.sizeDelta = new Vector2(viewport.rect.width, viewport.rect.height);
            p.anchoredPosition = new Vector2(i * pageWidth, 0f);
        }

        // content 폭을 넉넉히(필수는 아님)
        content.sizeDelta = new Vector2(pageCount * pageWidth, viewport.rect.height);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isUsingTab) return;
        dragging = true;
        dragStartPointer = eventData.position;
        dragStartContentPos = content.anchoredPosition;
        if (snapCo != null) StopCoroutine(snapCo);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isUsingTab || !dragging) return;
        float dx = eventData.position.x - dragStartPointer.x;
        float minX = -((pageCount - 1) * pageWidth);
        float targetX = Mathf.Clamp(dragStartContentPos.x + dx, minX, 0f);
        content.anchoredPosition = new Vector2(targetX, dragStartContentPos.y);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isUsingTab)
        {                      
            dragging = false;
            SnapTo(current);
            return;
        }
        dragging = false;
        float totalDx = eventData.position.x - dragStartPointer.x;

        if (Mathf.Abs(totalDx) > swipeThreshold)
        {
            if (totalDx < 0) SetPage(current + 1);   // 왼쪽으로 넘김 → 다음 페이지
            else SetPage(current - 1);   // 오른쪽으로 넘김 → 이전 페이지
        }
        else
        {
            // 기존 페이지로 스냅백
            SnapTo(current);
        }
    }

    public void Next() => SetPage(current + 1);
    public void Prev() => SetPage(current - 1);

    public void SetPage(int index)
    {
        index = Mathf.Clamp(index, 0, pageCount - 1);
        if (index == current) { SnapTo(current); return; }
        current = index;
        SnapTo(current);
        UpdateDots();
    }

    void JumpTo(int index)
    {
        float x = -index * pageWidth;
        content.anchoredPosition = new Vector2(x, content.anchoredPosition.y);
    }

    void SnapTo(int index)
    {
        if (snapCo != null) StopCoroutine(snapCo);
        snapCo = StartCoroutine(CoSnap(-index * pageWidth));
    }

    IEnumerator CoSnap(float targetX)
    {
        float t = 0f;
        float fromX = content.anchoredPosition.x;
        while (true)
        {
            t += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) * snapSpeed;
            float x = Mathf.Lerp(fromX, targetX, Mathf.Clamp01(t));
            content.anchoredPosition = new Vector2(x, content.anchoredPosition.y);
            if (Mathf.Abs(x - targetX) < 0.5f) break;
            yield return null;
        }
        content.anchoredPosition = new Vector2(targetX, content.anchoredPosition.y);
        snapCo = null;
    }

    void UpdateDots()
    {
        if (dots == null) return;
        for (int i = 0; i < dots.Length; i++)
        {
            if (!dots[i]) continue;
            dots[i].color = (i == current) ? dotActive : dotInactive;
        }
    }
}
