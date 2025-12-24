using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 자모/블럭 UI를 마우스로 드래그하고, 허용 영역/쓰레기통/스냅을 처리하는 컴포넌트
/// </summary>
public class DraggableWordUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    RectTransform rt;           // 내 RectTransform
    RectTransform dragRoot;     // 드래그 기준이 되는 루트(보통 uiSpawnRoot)
    RectTransform allowedArea;  // 허용 구역(이 안에 있으면 살아남음)
    RectTransform trashArea;    // 쓰레기통(여기 들어가면 삭제)
    Camera uiCamera;            // UI 카메라(Overlay면 null 가능)

    CanvasGroup cg;             // 드래그 중 Raycast 막기용

    KeyBoardManager owner;      // 인벤토리/키 개수 관리하는 매니저
    int sourceInventoryIndex = -1;  // 어떤 키 슬롯에서 소비되었는지
    int consumedAmount = 1;         // 소비된 개수(삭제 시 환불용)

    // 외부에서 읽을 일이 있을 수 있으니 프로퍼티로만 공개
    public RectTransform DragRoot => dragRoot;
    public RectTransform AllowedArea => allowedArea;
    public RectTransform TrashArea => trashArea;
    public Camera UiCamera => uiCamera;

    /// <summary>드래그 루트/영역/카메라 초기 세팅</summary>
    public void Init(RectTransform dragRoot, RectTransform allowedArea, RectTransform trashArea, Camera uiCamera)
    {
        this.dragRoot = dragRoot;
        this.allowedArea = allowedArea;
        this.trashArea = trashArea;
        this.uiCamera = uiCamera;
    }

    /// <summary>이 조각이 어떤 키 슬롯에서 몇 개 소비됐는지 기록</summary>
    public void BindSource(KeyBoardManager keyboard, int invIndex, int amount)
    {
        owner = keyboard;
        sourceInventoryIndex = invIndex;
        consumedAmount = Mathf.Max(1, amount);
    }

    void Awake()
    {
        rt = GetComponent<RectTransform>();

        cg = GetComponent<CanvasGroup>();
        if (!cg)
            cg = gameObject.AddComponent<CanvasGroup>();

        cg.blocksRaycasts = true;
    }

    /// <summary>드래그 시작 시 호출</summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!rt || !dragRoot) return;

        // 드래그 중에는 다른 UI가 이 오브젝트를 Raycast로 못 잡게 막기
        cg.blocksRaycasts = false;

        // 최상단으로 올려서 다른 것보다 위에 보이게
        rt.SetAsLastSibling();
    }

    /// <summary>드래그 중 마우스/터치 위치에 맞춰 이동</summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (!rt || !dragRoot) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(dragRoot, eventData.position, uiCamera, out var local))
        {
            rt.anchoredPosition = local;
        }
    }

    /// <summary>드래그 끝났을 때: 영역 판정 + 스냅 시도 + 삭제/환불 처리</summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        try
        {
            // 1) 쓰레기통에 드롭 → 삭제 + 환불
            if (trashArea &&
                RectTransformUtility.RectangleContainsScreenPoint(trashArea, eventData.position, uiCamera))
            {
                RefundAndDestroy();
                return;
            }

            // 2) 허용 구역 밖에 드롭 → 삭제 + 환불
            if (allowedArea &&
                !RectTransformUtility.RectangleContainsScreenPoint(allowedArea, eventData.position, uiCamera))
            {
                RefundAndDestroy();
                return;
            }

            // 3) 허용 구역 안에 있고, 자모라면 → 블럭 쪽으로 스냅 시도
            var magnet = GetComponent<JamoMagnet>();
            if (magnet != null)
            {
                // 실제 스냅 로직은 JamoMagnet/SyllableBlock 쪽에서 처리
                magnet.TrySnap(dragRoot, uiCamera);
            }
            // 만약 SyllableBlock 자체를 드래그하는 프리팹이라면 magnet가 없을 수 있음
        }
        finally
        {
            // 드래그 종료 후 다시 Raycast 가능하게
            if (cg) cg.blocksRaycasts = true;
        }
    }

    /// <summary>이 조각을 제거하면서, 소비했던 키 개수를 되돌려준다.</summary>
    void RefundAndDestroy()
    {
        if (owner && sourceInventoryIndex >= 0)
        {
            owner.OnPieceDeleted(sourceInventoryIndex, consumedAmount);
        }
        Destroy(gameObject);
    }
}
