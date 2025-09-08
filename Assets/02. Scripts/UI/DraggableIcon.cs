using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class DraggableIcon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    [Header("아이콘들이 놓일 영역")]
    public UIGridArea grid;   
    [Header("같은 캔버스 참조(스케일 보정용)")]
    public Canvas canvas;
    [Header("드래그 끝날 때 부드럽게 스냅")]
    public bool smoothSnap = true;
    public float snapLerpSpeed = 20f;

    RectTransform _rt;
    CanvasGroup _cg;
    Vector2 _dragOffsetGrid;     // ← grid 로컬 기준 오프셋
    Vector2 _startLocalParent;   // ← 아이콘 부모 로컬 기준 시작 위치
    bool _isSnapping;

    RectTransform _gridRT;
    RectTransform _parentRT;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

    }
    void Start()
    {
        _gridRT = grid.GetComponent<RectTransform>();
        _parentRT = _rt.parent as RectTransform;
    }

    /// <summary>
    /// 포인터를 눌렀을 때 호출
    /// 아이콘을 최상단으로 올려 다른 UI보다 위에 보이도록 처리.
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        // 선택시 상단으로 올리고 싶으면 sibling 조정
        _rt.SetAsLastSibling();
    }

    /// <summary>
    /// 드래그 시작 시 호출
    /// 현재 위치를 저장(취소 대비)
    /// 아이콘 투명도 살짝 낮추고, Raycast 차단 해제
    /// 포인터와 아이콘 pivot의 상대적 위치(_dragOffset) 계산
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        _isSnapping = false;
        _startLocalParent = _rt.anchoredPosition;
        _cg.blocksRaycasts = false;
        _cg.alpha = 0.9f;

        if (GridOccupancy.Instance != null)
            GridOccupancy.Instance.Release(this);

        // 1) 포인터 Screen → grid 로컬
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _gridRT, eventData.position, eventData.pressEventCamera, out var gridLocal))
        {
            // 2) 현재 아이콘 부모 로컬 → grid 로컬로 변환
            var iconPosGrid = ToGridLocal(_rt.anchoredPosition);

            // 3) grid 기준 드래그 오프셋 저장
            _dragOffsetGrid = iconPosGrid - gridLocal;
        }
    }

    /// <summary>
    /// 드래그 중 호출
    /// 포인터 좌표를 Grid 로컬좌표로 변환
    /// 드래그 오프셋을 더해 아이콘 위치 갱신
    /// GridSnapArea의 clamp 옵션이 켜져 있으면 영역 밖으로 못 나가게 제한
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _gridRT, eventData.position, eventData.pressEventCamera, out var gridLocal))
        {
            // grid 기준 목표 위치
            Vector2 targetGrid = gridLocal + _dragOffsetGrid;

            if (grid.clampToBounds)
                targetGrid = grid.ClampToArea(targetGrid, _rt); // ← grid 로컬 기준 클램프

            // grid 로컬 → 아이콘 부모 로컬 변환 후 적용
            _rt.anchoredPosition = ToParentLocal(targetGrid);
        }
    }

    /// <summary>
    /// 드래그가 끝났을 때 호출
    /// 투명도 복원 및 Raycast 다시 활성화
    /// 아이콘 위치를 GridSnapArea에서 계산한 스냅 좌표로 맞춤
    /// smoothSnap 옵션이 있으면 보간 이동 코루틴 실행
    /// GridOccupancy를 이용하면 겹치지 않게 빈 칸 탐색 가능
    public void OnEndDrag(PointerEventData eventData)
    {
        _cg.blocksRaycasts = true;
        _cg.alpha = 1f;

        // 1) 현재 아이콘 위치(부모 로컬) → grid 로컬
        Vector2 curGrid = ToGridLocal(_rt.anchoredPosition);

        // 2) 스냅 좌표 직접 계산
        var idx = grid.LocalPosToIndex(curGrid);
        idx = grid.ClampIndex(idx);
        Vector2 snappedGrid = grid.IndexToLocalPos(idx);

        if (GridOccupancy.Instance != null)
        {
            var targetIdx = grid.LocalPosToIndex(snappedGrid);
            targetIdx = grid.ClampIndex(targetIdx);

            var _ = GridOccupancy.Instance.TryReserveNearestFree(
                targetIdx, this, 30, grid.IsInsideIndex);

            if (GridOccupancy.Instance.TryGetIndex(this, out var occupiedIdx))
            {
                occupiedIdx = grid.ClampIndex(occupiedIdx);
                snappedGrid = grid.IndexToLocalPos(occupiedIdx);
            }
            else
            {
                // 되돌림: 시작 위치(부모 로컬) → grid 로컬로 변환
                snappedGrid = ToGridLocal(_startLocalParent);
            }
        }

        // 3) 최종 스냅 좌표도 grid에서 한 번 더 클램프
        snappedGrid = grid.ClampToArea(snappedGrid, _rt);

        // 4) 적용할 땐 부모 로컬로 변환
        Vector2 snappedParent = ToParentLocal(snappedGrid);

        if (smoothSnap)
        {
            StopAllCoroutines();
            StartCoroutine(SmoothMove(snappedParent));
        }
        else
        {
            _rt.anchoredPosition = snappedParent;
        }
    }


    /// <summary>
    /// 스냅 위치로 부드럽게 이동시키는 코루틴
    /// Lerp를 이용해 서서히 목표 위치에 도달
    /// </summary>
    System.Collections.IEnumerator SmoothMove(Vector2 target)
    {
        _isSnapping = true;
        while (Vector2.Distance(_rt.anchoredPosition, target) > 0.1f)
        {
            _rt.anchoredPosition = Vector2.Lerp(_rt.anchoredPosition, target, Time.deltaTime * snapLerpSpeed);
            yield return null;
        }
        _rt.anchoredPosition = target;
        _isSnapping = false;
    }

    Vector2 ToGridLocal(Vector2 parentLocal)
    {
        Vector3 world = _parentRT.TransformPoint(parentLocal);
        return (Vector2)_gridRT.InverseTransformPoint(world);
    }

    // grid 로컬 → 아이콘 부모 로컬
    Vector2 ToParentLocal(Vector2 gridLocal)
    {
        Vector3 world = _gridRT.TransformPoint(gridLocal);
        return (Vector2)_parentRT.InverseTransformPoint(world);
    }
}
