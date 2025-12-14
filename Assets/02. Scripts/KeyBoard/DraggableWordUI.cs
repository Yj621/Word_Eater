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

    int sourceInventoryIndex = -1;
    int consumedAmount = 1;
    KeyBoardManager owner;

    public RectTransform DragRoot => dragRoot;
    public RectTransform AllowedArea => allowedArea;
    public RectTransform TrashArea => trashArea;
    public Camera UiCamera => uiCamera;

    public void Init(RectTransform dragRoot, RectTransform allowedArea, RectTransform trashArea, Camera uiCamera)
    {
        this.dragRoot = dragRoot;
        this.allowedArea = allowedArea;
        this.trashArea = trashArea;
        this.uiCamera = uiCamera;
    }

    public void BindSource(KeyBoardManager _keyboardm, int invIndex, int amount)
    {
        owner = _keyboardm;
        sourceInventoryIndex = invIndex;
        consumedAmount = Mathf.Max(1, amount);
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
        try
        {
            // 쓰레기통
            if (trashArea && RectTransformUtility.RectangleContainsScreenPoint(trashArea, eventData.position, uiCamera))
            {
                if (owner && sourceInventoryIndex >= 0)
                    owner.OnPieceDeleted(sourceInventoryIndex, consumedAmount);
                Destroy(gameObject);
                return;
            }

            // 허용구역 밖
            if (allowedArea && !RectTransformUtility.RectangleContainsScreenPoint(allowedArea, eventData.position, uiCamera))
            {
                if (owner && sourceInventoryIndex >= 0)
                    owner.OnPieceDeleted(sourceInventoryIndex, consumedAmount);
                Destroy(gameObject);
                return;
            }

            var magnet = GetComponent<JamoMagnet>();
            if (magnet)
            {
                // 1) "기존 블럭"에만 붙이기 시도 (여기서 블럭 생성하면 안 됨)
                bool snappedToBlock = SyllableBlock.TrySnapJamoToAnyBlock(magnet, uiCamera, createIfNone: false);
                if (snappedToBlock) return;

                // 2) 블럭이 없다면: "근처 다른 자모"와 조합 시도 → 성공하면 그때만 블럭 생성
                //    (드래그 루트가 있어야 블럭을 같은 UI 계층에 생성 가능)
                if (dragRoot && JamoMagnet.TryFuseWithNearbyJamo(magnet, uiCamera, dragRoot, magnet.snapRadius))
                    return;

                // 3) 둘 다 실패면: 그냥 자모 그대로 둔다 (단독 자모는 블럭 생성 X)
            }
        }
        finally
        {
            if (cg) cg.blocksRaycasts = true;
        }
    }

}
