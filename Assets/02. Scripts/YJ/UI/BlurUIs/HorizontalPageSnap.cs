using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class HorizontalPageSnap : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [Header("Refs")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;
    [SerializeField] private RectTransform viewport;

    [Header("Snap")]
    [SerializeField] private float snapDuration = 0.18f;     // 스냅 속도
    [SerializeField] private float velocityThreshold = 800f; // 빠르게 넘기면 다음/이전 페이지로

    public int CurrentPage { get; private set; } = 0;

    public System.Action<int, int> OnPageChanged; // (current, pageCount)

    private bool isDragging;
    private Coroutine snapRoutine;

    private void Reset()
    {
        scrollRect = GetComponent<ScrollRect>();
    }

    private void Awake()
    {
        if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();
        if (content == null) content = scrollRect.content;
        if (viewport == null) viewport = scrollRect.viewport;

        // 가로 페이징 기본값 권장
        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
    }

    private void Update()
    {
        if (isDragging) return;
        if (snapRoutine != null) return;

        // 멈춰있는데 페이지 중앙이 아니면 자동 스냅
        if (scrollRect.velocity.sqrMagnitude < 5f)
            SnapToNearest();
    }

    private void OnEnable()
    {
        Notify();
    }

    public void Refresh()
    {
        // 페이지 수/현재 페이지 재검증
        int pageCount = GetPageCount();
        CurrentPage = Mathf.Clamp(CurrentPage, 0, Mathf.Max(0, pageCount - 1));
        SetPage(CurrentPage, immediate: true);
        Notify();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        StopSnap();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        SnapToNearest();
    }

    private void LateUpdate()
    {
        // 드래그 중에는 인디케이터가 실시간으로 바뀌게 하고 싶다면 여기서 Update 가능
        // (필요하면 확장해줄게)
    }

    private int GetPageCount()
    {
        float vw = viewport.rect.width;
        if (vw <= 0.01f) return 1;
        return Mathf.Max(1, Mathf.RoundToInt(content.rect.width / vw));
    }

    private float GetNormalizedForPage(int page)
    {
        int pageCount = GetPageCount();
        if (pageCount <= 1) return 0f;
        return Mathf.Clamp01(page / (float)(pageCount - 1));
    }

    private int GetNearestPage()
    {
        int pageCount = GetPageCount();
        if (pageCount <= 1) return 0;

        float n = scrollRect.horizontalNormalizedPosition;
        int nearest = Mathf.RoundToInt(n * (pageCount - 1));
        return Mathf.Clamp(nearest, 0, pageCount - 1);
    }

    private void SnapToNearest()
    {
        int pageCount = GetPageCount();
        if (pageCount <= 1)
        {
            CurrentPage = 0;
            SetPage(0, immediate: false);
            Notify();
            return;
        }

        // 빠르게 스와이프하면 다음/이전 페이지로 넘김
        float vx = scrollRect.velocity.x;

        int target = GetNearestPage();

        if (Mathf.Abs(vx) > velocityThreshold)
        {
            // ScrollRect velocity: 왼쪽으로 드래그하면 content가 오른쪽으로 움직이고 velocity.x는 보통 양/음이 환경에 따라 달라질 수 있음
            // 아래 로직은 "normalized 증가 = 오른쪽 페이지" 기준으로 잡음
            // 대부분 Unity에서는 오른쪽으로 넘기면 normalized가 증가함.
            if (vx < 0) target += 1;  // 더 오른쪽 페이지
            else target -= 1;         // 더 왼쪽 페이지
        }

        target = Mathf.Clamp(target, 0, pageCount - 1);
        SetPage(target, immediate: false);
    }

    public void SetPage(int page, bool immediate)
    {
        int pageCount = GetPageCount();
        page = Mathf.Clamp(page, 0, pageCount - 1);

        float targetN = GetNormalizedForPage(page);

        if (immediate)
        {
            scrollRect.horizontalNormalizedPosition = targetN;
            CurrentPage = page;
            Notify();
            return;
        }

        StopSnap();
        snapRoutine = StartCoroutine(CoSnap(targetN, page));
    }

    private IEnumerator CoSnap(float targetN, int targetPage)
    {
        // 스냅 중에는 관성 꺼서 덜 흔들리게
        scrollRect.velocity = Vector2.zero;

        float startN = scrollRect.horizontalNormalizedPosition;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, snapDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f); // easeOutCubic
            scrollRect.horizontalNormalizedPosition = Mathf.Lerp(startN, targetN, eased);
            yield return null;
        }

        scrollRect.horizontalNormalizedPosition = targetN;

        if (CurrentPage != targetPage)
        {
            CurrentPage = targetPage;
            Notify();
        }

        snapRoutine = null;
    }

    private void StopSnap()
    {
        if (snapRoutine != null)
        {
            StopCoroutine(snapRoutine);
            snapRoutine = null;
        }
    }

    private void Notify()
    {
        OnPageChanged?.Invoke(CurrentPage, GetPageCount());
    }
}