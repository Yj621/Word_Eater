using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

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

    [Tooltip("탭 UI가 켜져 있을 때 스와이프 잠금 여부")]
    public bool isUsingTab = false;

    public SlideManager slidemanager;

    int pageCount;
    int current;
    float pageWidth;
    Vector2 dragStartPointer;
    Vector2 dragStartContentPos;
    bool dragging;
    Coroutine snapCo;

    void Awake()
    {
        if (!viewport) viewport = transform as RectTransform;

        if (pages == null || pages.Length == 0)
        {
            pages = content.Cast<Transform>()
                           .Select(t => t as RectTransform)
                           .Where(r => r != null)
                           .ToArray();
        }

        pageCount = pages.Length;
        startPage = Mathf.Clamp(startPage, 0, Mathf.Max(0, pageCount - 1));
        current = startPage;

        EnsureViewportMask();
        Relayout();
        JumpTo(current);
        UpdateDots();
        isUsingTab = false;
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
        // 페이지 폭 기준 계산 (원본 page 크기 유지)
        if (pageCount > 0)
        {
            pageWidth = pages[0].rect.width;
        }
        else
        {
            pageWidth = viewport ? viewport.rect.width : 0f;
        }

        // content는 건드리지 않고, 개별 페이지의 x만 배치
        for (int i = 0; i < pageCount; i++)
        {
            var p = pages[i];
            var pos = p.anchoredPosition;
            pos.x = i * pageWidth;
            p.anchoredPosition = pos;
        }
    }

    // 외부에서 탭 열릴 때/닫힐 때 호출해주면 좋음
    public void SetSwipeLock(bool locked)
    {
        isUsingTab = locked;

        if (locked && dragging)
        {
            // 드래그 중에 잠그면 현재 페이지로 스냅 + 드래그 강제 종료
            dragging = false;
            SnapTo(current);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isUsingTab) return;  // 탭 사용 중이면 스와이프 금지

        dragging = true;


        dragStartPointer = eventData.position;
        dragStartContentPos = content.anchoredPosition;

        if (snapCo != null) StopCoroutine(snapCo);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging || isUsingTab || slidemanager.BlockJJS) return;

        if (dragStartPointer.y >= Screen.height * 0.7) return;

        float dx = eventData.position.x - dragStartPointer.x;
        float minX = -((pageCount - 1) * pageWidth);
        float targetX = Mathf.Clamp(dragStartContentPos.x + dx, minX, 0f);
        content.anchoredPosition = new Vector2(targetX, dragStartContentPos.y);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging || slidemanager.BlockJJS)
            return;

        if (dragStartPointer.y >= Screen.height * 0.7) return;

        dragging = false;


        // 드래그 끝나기 전에 탭이 켜진 상태가 되었으면 그냥 현재 페이지로 스냅
        if (isUsingTab)
        {
            SnapTo(current);
            return;
        }

        float totalDx = eventData.position.x - dragStartPointer.x;

        if (Mathf.Abs(totalDx) > swipeThreshold)
        {
            if (totalDx < 0) SetPage(current + 1);   // 왼쪽으로 넘김 → 다음 페이지
            else SetPage(current - 1);               // 오른쪽으로 넘김 → 이전 페이지
        }
        else
        {
            // 기존 페이지로 스냅백
            SnapTo(current);
        }
    }

    // ---- 버튼/탭 이동 ----

    public void Next() => SetPage(current + 1);
    public void Prev() => SetPage(current - 1);

    public void GoToPage(int index)
    {
        SetPage(index);
    }

    public void SetPage(int index)
    {
        if (pageCount <= 0) return;

        index = Mathf.Clamp(index, 0, pageCount - 1);
        if (index == current)
        {
            SnapTo(current);
            return;
        }

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
        if (dots == null || dots.Length == 0) return;

        for (int i = 0; i < dots.Length; i++)
        {
            if (!dots[i]) continue;
            dots[i].color = (i == current) ? dotActive : dotInactive;
        }
    }
}
