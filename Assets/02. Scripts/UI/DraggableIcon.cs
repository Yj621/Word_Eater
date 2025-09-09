using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


/// <summary>
/// 아이콘을 드래그하여 UIGridArea 위의 셀에 스냅 배치하는 컴포넌트
/// 
/// 핵심 포인트
/// 1) 좌표계가 서로 다르므로 부모 로컬 <-> Grid 로컬 변환이 필요
/// 2) 드래그 중에는 Grid 로컬에서 좌표를 계산/클램프
/// 3) 드랍 시엔 셀 중심으로 스냅. GridOccupancy로 빈칸 탐색/점유를 관리
/// </summary>

[RequireComponent(typeof(RectTransform))]
public class DraggableIcon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    [Header("아이콘들이 놓일 영역")]
    public UIGridArea gridArea;   
    [Header("같은 캔버스 참조(스케일 보정용)")]
    public Canvas canvas;
    [Header("드래그 끝날 때 부드럽게 스냅")]
    public bool smoothSnap = true;
    public float snapLerpSpeed = 20f;

    [Header("자리표시자(원래 자리 표시)")]
    public Sprite placeholderSprite;
    public Color placeholderColor = new Color(1, 1, 1, 0.25f);
    public bool placeholderAsBorder = false;   // true면 테두리만(투명 바탕), false면 연한 사각형
    public float placeholderBorderThickness = 2f;

    private RectTransform _rt;
    private CanvasGroup _canvasGroup;

    // 좌표 변환에 필요한 RectTransform 참조
    private RectTransform _gridRT;
    private RectTransform _parentRT;

    // 드래그 상태 데이터
    private Vector2 _dragOffsetInGridSpace;     // 포인터와 아이콘 pivot의 거리 (Grid 로컬 기준)
    private Vector2 _startAnchoredPosInParent;  // 드래그 시작 시 아이콘 위치(아이콘 부모 로컬 기준)
    private bool _isSnapping;

    // 자리표시자 인스턴스
    private GameObject _placeholderGO;
    private RectTransform _placeholderRT;
    private Image _placeholderIcon;    // 원래 아이콘 그림

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        _gridRT = gridArea.GetComponent<RectTransform>();
        _parentRT = _rt.parent as RectTransform;
        RegisterInitialOccupancy();
    }
    private void RegisterInitialOccupancy()
    {
        if (GridOccupancy.Instance == null || gridArea == null) return;

        // 현재 anchoredPosition(부모 로컬) → Grid 로컬 → 셀 인덱스
        Vector2 inGrid = ParentToGridLocal(_rt.anchoredPosition);
        Vector2Int cell = gridArea.LocalPositionToCell(inGrid);
        cell = gridArea.ClampCellIndex(cell);

        // 그 셀을 우선 점유 시도
        if (!GridOccupancy.Instance.TryOccupyAtCell(cell, this))
        {
            // 이미 누가 있으면, 가장 가까운 빈 칸을 예약/점유하고 아이콘도 그 위치로 이동
            var reserved = GridOccupancy.Instance.ReserveNearestFreeCell(
                cell, this, maxRadius: 30, gridArea.IsValidCellIndex);

            Vector2 snappedInGrid = gridArea.CellToLocalPosition(reserved);
            Vector2 snappedInParent = GridToParentLocal(snappedInGrid);
            _rt.anchoredPosition = snappedInParent;  // 장면 시작 시 겹침을 자동 해소
        }
    }

    /// <summary>
    /// 클릭 시 아이콘을 가장 위로 올려서 다른 UI 위에 보이도록하는 메서드
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        _rt.SetAsLastSibling();
    }

    /// <summary>
    /// 드래그 시작
    /// - 시작 위치 저장(취소 대비)
    /// - 레이캐스트 차단 해제 & 투명도 살짝 낮춤(드래그 UX)
    /// - 포인터와 아이콘 사이 오프셋을 Grid 로컬 기준으로 계산해 저장
    /// - 기존 점유 해제(GridOccupancy)
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        _isSnapping = false;
        _startAnchoredPosInParent = _rt.anchoredPosition;

        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.alpha = 0.5f;

        if (GridOccupancy.Instance != null)
            GridOccupancy.Instance.ReleaseIcon(this);

        // 포인터: Screen → Grid 로컬
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _gridRT, eventData.position, eventData.pressEventCamera, out var pointerInGrid))
        {
            //  아이콘 현재 위치: 부모 로컬 → Grid 로컬
            Vector2 iconInGrid = ParentToGridLocal(_rt.anchoredPosition);

            // 오프셋(아이콘 중심 - 포인터) 보존
            _dragOffsetInGridSpace = iconInGrid - pointerInGrid;
        }
        // 자리표시자 만들기
        ShowPlaceholderAtStartCell();
    }

    /// <summary>
    /// 드래그 중
    /// - 포인터를 Grid 로컬로 변환
    /// - 시작 시 저장한 오프셋을 더해 아이콘의 목표 위치(Grid 로컬)를 얻음
    /// - 필요하면 영역 클램프
    /// - 다시 아이콘 부모 로컬로 변환해 anchoredPosition 반영
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _gridRT, eventData.position, eventData.pressEventCamera, out var pointerInGrid))
        {
            Vector2 targetInGrid = pointerInGrid + _dragOffsetInGridSpace;

            if (gridArea.clampToBounds)
                targetInGrid = gridArea.ClampPositionToArea(targetInGrid, _rt);

            _rt.anchoredPosition = GridToParentLocal(targetInGrid);
        }
    }

    /// <summary>
    /// 드래그 종료
    /// - 투명도/레이캐스트 복원
    /// - 현재 위치를 기반으로 가까운 셀 중심으로 스냅
    /// - GridOccupancy 사용 시, 충돌하지 않는 가장 가까운 빈 칸을 찾아 점유
    /// - 최종 스냅 좌표를 Grid → 부모 로컬로 변환해 적용(부드러운 스냅 선택 가능)
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.alpha = 1f;

        // 아이콘 현재 좌표: 부모 로컬 → Grid 로컬
        Vector2 currentInGrid = ParentToGridLocal(_rt.anchoredPosition);

        // 가장 가까운 셀 인덱스 계산 → 셀 중심(Grid 로컬)
        Vector2Int cell = gridArea.LocalPositionToCell(currentInGrid);
        cell = gridArea.ClampCellIndex(cell);
        Vector2 snappedInGrid = gridArea.CellToLocalPosition(cell);

        // 점유 관리(있다면): target 셀 기준으로 가장 가까운 빈 칸을 찾아 점유
        if (GridOccupancy.Instance != null)
        {
            Vector2Int targetCell = gridArea.LocalPositionToCell(snappedInGrid);
            targetCell = gridArea.ClampCellIndex(targetCell);

            GridOccupancy.Instance.ReserveNearestFreeCell(
                targetCell, this, maxRadius: 30, gridArea.IsValidCellIndex);

            if (GridOccupancy.Instance.TryGetCellOfIcon(this, out var occupiedCell))
            {
                occupiedCell = gridArea.ClampCellIndex(occupiedCell);
                snappedInGrid = gridArea.CellToLocalPosition(occupiedCell);
            }
            else
            {
                // 시작 위치(부모 로컬) → Grid 로컬로 되돌림
                snappedInGrid = ParentToGridLocal(_startAnchoredPosInParent);
            }
        }

        // 최종 좌표도 한 번 더 안정적으로 클램프
        snappedInGrid = gridArea.ClampPositionToArea(snappedInGrid, _rt);

        // 적용은 아이콘 부모 로컬 기준
        Vector2 snappedInParent = GridToParentLocal(snappedInGrid);

        if (smoothSnap)
        {
            StopAllCoroutines();
            StartCoroutine(SmoothSnapTo(snappedInParent));
        }
        else
        {
            _rt.anchoredPosition = snappedInParent;
        }
        // 드롭이 끝났으니 자리표시자 제거
        HidePlaceholder();
    }

    /// <summary>
    /// 스냅 목표 위치까지 Lerp로 부드럽게 이동
    /// </summary>
    private IEnumerator SmoothSnapTo(Vector2 target)
    {
        _isSnapping = true;
        while (Vector2.Distance(_rt.anchoredPosition, target) > 0.1f)
        {
            _rt.anchoredPosition = Vector2.Lerp(
                _rt.anchoredPosition, target, Time.deltaTime * snapLerpSpeed);
            yield return null;
        }
        _rt.anchoredPosition = target;
        _isSnapping = false;
    }

    // ===== 좌표 변환 유틸리티 =====

    /// <summary>아이콘 '부모 로컬' → Grid 로컬</summary>
    private Vector2 ParentToGridLocal(Vector2 parentLocal)
    {
        Vector3 world = _parentRT.TransformPoint(parentLocal);
        return (Vector2)_gridRT.InverseTransformPoint(world);
    }

    /// <summary>Grid 로컬 → 아이콘 '부모 로컬'</summary>
    private Vector2 GridToParentLocal(Vector2 gridLocal)
    {
        Vector3 world = _gridRT.TransformPoint(gridLocal);
        return (Vector2)_parentRT.InverseTransformPoint(world);
    }

    // === 자리표시자 ===
    private void ShowPlaceholderAtStartCell()
    {
        Vector2 startInGrid = ParentToGridLocal(_startAnchoredPosInParent);
        Vector2Int startCell = gridArea.LocalPositionToCell(startInGrid);
        startCell = gridArea.ClampCellIndex(startCell);
        Vector2 startCellCenterInGrid = gridArea.CellToLocalPosition(startCell);
        Vector2 startCellCenterInParent = GridToParentLocal(startCellCenterInGrid);

        if (_placeholderGO == null)
        {
            _placeholderGO = new GameObject("Placeholder");
            _placeholderRT = _placeholderGO.AddComponent<RectTransform>();
            _placeholderRT.SetParent(_parentRT, worldPositionStays: false);
            _placeholderRT.anchorMin = new Vector2(0.5f, 0.5f);
            _placeholderRT.anchorMax = new Vector2(0.5f, 0.5f);
            _placeholderRT.pivot = _rt.pivot;

            var img = _placeholderGO.AddComponent<Image>();
            img.raycastTarget = false;

            // 자리표시자용 스프라이트 (인스펙터에서 drag & drop)
            img.sprite = placeholderSprite;
            img.color = placeholderColor; // 투명도 조절

            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(_placeholderRT, false);
            var iconRT = iconGO.AddComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.5f, 0.5f);
            iconRT.anchorMax = new Vector2(0.5f, 0.5f);
            iconRT.pivot = _rt.pivot;


            _placeholderIcon = iconGO.AddComponent<Image>();
            _placeholderIcon.raycastTarget = false;
            _placeholderIcon.preserveAspect = true;
            // 처음엔 현재 아이콘 스프라이트를 복사
            var myIcon = GetComponent<Image>();
            _placeholderIcon.sprite = myIcon != null ? myIcon.sprite : null;
            _placeholderIcon.color = new Color(1, 1, 1, 0.85f); // 원하면 투명도 조절
        }

        // 크기/위치 동기화
        _placeholderRT.sizeDelta = new Vector2(280f, 280f);         // 전체 프레임 크기
        _placeholderRT.anchoredPosition = startCellCenterInParent;  // 셀 중심

        // 아이콘 크기(원 아이콘과 동일)
        if (_placeholderIcon != null)
        {
            var iconRT = _placeholderIcon.rectTransform;
            iconRT.sizeDelta = _rt.sizeDelta; // 아이콘 정사각형이라면 그대로. 필요 시 여백 주기 가능
                                              // 예: iconRT.sizeDelta = _rt.sizeDelta - new Vector2(12,12);
        }

        // 배경만 쓰고 싶으면 아래 라인으로 아이콘 숨김/보임 제어 가능
        // _placeholderIcon.enabled = true;

        // 자리표시자는 뒤에, 드래그중 아이콘은 위에 보이게
        _placeholderRT.SetSiblingIndex(Mathf.Max(0, _rt.GetSiblingIndex() - 1));

        _placeholderGO.SetActive(true);
    }


    private void HidePlaceholder()
    {
        if (_placeholderGO != null)
            _placeholderGO.SetActive(false);
    }
}