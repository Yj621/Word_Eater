using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollDragForwarder : MonoBehaviour,
    IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private HorizontalPageSnap pageSnap;

    [Range(0.05f, 1f)]
    [SerializeField] private float dragScale = 0.33f;

    private void Awake()
    {
        if (!scrollRect) scrollRect = GetComponentInParent<ScrollRect>();
        if (!pageSnap && scrollRect) pageSnap = scrollRect.GetComponent<HorizontalPageSnap>();
        if (!pageSnap) pageSnap = GetComponentInParent<HorizontalPageSnap>();
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        if (!scrollRect) return;
        scrollRect.OnInitializePotentialDrag(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!scrollRect) return;
        pageSnap?.OnBeginDrag(eventData);     // ★ 스냅도 같이 시작 알림
        scrollRect.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!scrollRect) return;

        Vector2 originalDelta = eventData.delta;
        eventData.delta = originalDelta * dragScale;

       // pageSnap?.OnDrag(eventData);          // ★ 스냅이 드래그 중 상태 알 수 있게
        scrollRect.OnDrag(eventData);

        eventData.delta = originalDelta;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!scrollRect) return;

        scrollRect.OnEndDrag(eventData);
        pageSnap?.OnEndDrag(eventData);       // ★ 손 뗐을 때 스냅 실행
    }
}
