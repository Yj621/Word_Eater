using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableWordUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    RectTransform rt;                 // 내 RectTransform
    RectTransform dragRoot;           // 화면 전체를 덮는 드래그 루트(= uiSpawnRoot)
    RectTransform allowedArea;        // 허용 구역
    RectTransform trashArea;          // 쓰레기통
    Camera uiCamera;                  // Overlay면 null

    CanvasGroup cg;                   // 드래그 중 Raycast 막기용(선택)

    public void Init(RectTransform dragRoot, RectTransform allowedArea, RectTransform trashArea, Camera uiCamera)
    {
        this.dragRoot = dragRoot;
        this.allowedArea = allowedArea;
        this.trashArea = trashArea;
        this.uiCamera = uiCamera;
    }

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!rt || !dragRoot) return;

        // 드래그 중 다른 UI와 충돌 줄이기
        cg.blocksRaycasts = false;

        // 화면 최상단으로
        rt.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!rt || !dragRoot) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(dragRoot, eventData.position, uiCamera, out var local))
        {
            rt.anchoredPosition = local;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 쓰레기통 안에 드롭 → 삭제
        if (trashArea && RectTransformUtility.RectangleContainsScreenPoint(trashArea, eventData.position, uiCamera))
        {
            Destroy(gameObject);
            return;
        }

        // 허용 구역 밖 → 삭제
        if (allowedArea && !RectTransformUtility.RectangleContainsScreenPoint(allowedArea, eventData.position, uiCamera))
        {
            Destroy(gameObject);
            return;
        }

        // 정상 드롭 → 그대로 고정
        cg.blocksRaycasts = true;
    }
}
