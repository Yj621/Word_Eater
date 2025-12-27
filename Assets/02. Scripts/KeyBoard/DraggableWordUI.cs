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

    public void OnEndDrag(PointerEventData eventData)
    {
        try
        {
            var magnet = GetComponent<JamoMagnet>();

            // 1) 쓰레기통에 바로 버리기
            if (trashArea && RectTransformUtility.RectangleContainsScreenPoint(trashArea, eventData.position, uiCamera))
            {
                if (magnet != null)
                {
                    // 두트윈 애니 후 환불+삭제
                    magnet.PlayTrashAnim(trashArea, RefundAndDestroy);
                }
                else
                {
                    RefundAndDestroy();
                }
                return;
            }

            // 2) 처음 드래그부터 바로 범위 밖이면 그 자리에서 삭제
            if (allowedArea && !RectTransformUtility.RectangleContainsScreenPoint(allowedArea, eventData.position, uiCamera))
            {
                if (magnet != null)
                {
                    magnet.PlayTrashAnim(null, RefundAndDestroy);   // 제자리 축소 삭제
                }
                else
                {
                    RefundAndDestroy();
                }
                return;
            }

            // 3) 허용구역 안이면 스냅/조립 로직
            if (magnet)
            {
                bool snapped = magnet.TrySnap(dragRoot, uiCamera);
                // TrySnap 내부에서 Syl 조립 / 삭제까지 처리하니까
                // true면 여기서 끝, false면 그냥 지금 자리 유지
                if (snapped) return;
            }
        }
        finally
        {
            if (cg) cg.blocksRaycasts = true;
        }
    }


    /// <summary>이 조각을 제거하면서, 소비했던 키 개수를 되돌려준다.</summary>
    public void RefundAndDestroy()
    {
        if (owner && sourceInventoryIndex >= 0)
        {
            owner.OnPieceDeleted(sourceInventoryIndex, consumedAmount);
        }
        Destroy(gameObject);
    }
}
