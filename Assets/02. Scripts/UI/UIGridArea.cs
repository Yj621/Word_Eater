using UnityEngine;

/// <summary>
/// 캔버스 안에서 아이콘을 배치할 수 있는 격자(Grid) 영역을 정의
/// - 가로 칸 수는 항상 고정(예: 3칸)
/// - 세로 칸 수는 해상도와 높이에 따라 동적으로 계산
/// - 좌우 여백은 해상도에 따라 자동 계산되어 아이콘이 항상 가운데 정렬
/// - 세로 줄 간격은 cellSpacing.y로 일정하게 유지됨
/// - 좌표 <-> 셀 인덱스 변환 기능 제공
/// - 드래그 아이콘이 영역 밖으로 나가지 않도록 클램프 기능 제공
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIGridArea : MonoBehaviour
{
    [Header("한 칸의 크기(px)")]
    public Vector2 cellSize = new Vector2(130, 200);

    [Header("칸 사이 간격")]
    public Vector2 cellSpacing = new Vector2(20, 20);

    [Header("영역 안쪽 여백 (세로만 의미 있음)")]
    public Vector2 padding = new Vector2(20, 20);

    [Header("영역 밖 드래그 금지 여부")]
    public bool clampToBounds = true;

    [Header("가로 칸수 고정")]
    [SerializeField] private int fixedColumns = 3;

    private RectTransform _rt;

    // 현재 그리드의 가로/세로 칸 수
    public int columns { get; private set; }
    public int rows { get; private set; }

    // 해상도에 따라 좌우 자동 여백(가운데 정렬용)
    float _autoPadX = 0f;

    /// <summary>
    /// RectTransform 초기화 및 최소 가로폭 보정
    /// </summary>
    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        EnsureMinWidthForFixedColumns();
        UpdateGrid();
    }

    /// <summary>
    /// 해상도(레이아웃) 변경 시 자동으로 호출 → 그리드 정보 재계산
    /// </summary>
    void OnRectTransformDimensionsChange()
    {
        if (_rt != null) UpdateGrid();
    }

    /// <summary>
    /// 최소 3칸(고정된 칸 수) 아이콘을 수용할 수 있도록 RectTransform의 최소 가로 크기를 보정
    /// </summary>
    void EnsureMinWidthForFixedColumns()
    {
        float needWidth =
            cellSize.x * fixedColumns +
            cellSpacing.x * (fixedColumns - 1) +
            padding.x * 2f;

        if (_rt.rect.width < needWidth)
            _rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, needWidth);
    }

    /// <summary>
    /// 현재 RectTransform 크기와 cellSize, cellSpacing을 기반으로
    /// - 가로 칸 수(columns)는 항상 고정
    /// - 세로 칸 수(rows)는 자동 계산
    /// - 좌우 여백(_autoPadX)은 가운데 정렬되도록 자동 분배
    /// </summary>
    public void UpdateGrid()
    {
        var r = _rt.rect;

        // 가로 칸 수 고정
        columns = Mathf.Max(1, fixedColumns);

        // 가로 콘텐츠 총 폭 (아이콘 + 간격)
        float contentW = columns * cellSize.x + (columns - 1) * cellSpacing.x;

        // 남는 폭을 좌우로 나눠서 가운데 정렬
        _autoPadX = Mathf.Max(0f, (r.width - contentW) * 0.5f);

        // 세로 칸 수 계산 (cellSpacing.y로 간격 일정하게 유지)
        float usableH = Mathf.Max(0, r.height - padding.y * 2f);
        float pitchY = cellSize.y + cellSpacing.y;
        rows = Mathf.Max(1, Mathf.FloorToInt((usableH + cellSpacing.y) / pitchY));
    }

    /// <summary>
    /// 실제 적용되는 좌우 패딩(자동 계산된 가운데 정렬 보정 포함)
    /// </summary>
    float PadX() => Mathf.Max(padding.x, _autoPadX);

    /// <summary>
    /// 아이콘(RectTransform)을 그리드 영역 안에 클램프
    /// - pivot/크기를 고려하여 영역 밖으로 벗어나지 않도록 제한
    /// </summary>
    public Vector2 ClampToArea(Vector2 local, RectTransform icon)
    {
        Rect r = _rt.rect;

        float padX = PadX();
        float left   = r.xMin + padX;
        float right  = r.xMax - padX;
        float bottom = r.yMin + padding.y;
        float top    = r.yMax - padding.y;

        Vector2 size = icon.rect.size;
        Vector2 pivot = icon.pivot;

        float minX = left   + size.x * pivot.x;
        float maxX = right  - size.x * (1f - pivot.x);
        float minY = bottom + size.y * pivot.y;
        float maxY = top    - size.y * (1f - pivot.y);

        return new Vector2(Mathf.Clamp(local.x, minX, maxX),
                           Mathf.Clamp(local.y, minY, maxY));
    }

    /// <summary>
    /// 그리드 원점(좌하단 + padding 적용된 좌표) 반환
    /// </summary>
    Vector2 GetOrigin()
    {
        var r = _rt.rect;
        return new Vector2(r.xMin + PadX(), r.yMin + padding.y);
    }

    /// <summary>
    /// 셀 인덱스가 유효한지 여부 확인
    /// </summary>
    public bool IsValidCell(Vector2Int idx)
        => idx.x >= 0 && idx.y >= 0 && idx.x < columns && idx.y < rows;

    /// <summary>
    /// 셀 인덱스를 그리드 범위 내로 강제로 맞춤
    /// </summary>
    public Vector2Int ClampCell(Vector2Int idx)
        => new Vector2Int(Mathf.Clamp(idx.x, 0, columns - 1),
                          Mathf.Clamp(idx.y, 0, rows - 1));

    /// <summary>
    /// grid 로컬 좌표 → 셀 인덱스로 변환
    /// </summary>
    public Vector2Int PosToCell(Vector2 local)
    {
        Vector2 origin = GetOrigin();
        Vector2 pitch = cellSize + cellSpacing;
        Vector2 d = local - origin;
        return new Vector2Int(Mathf.FloorToInt(d.x / pitch.x),
                              Mathf.FloorToInt(d.y / pitch.y));
    }

    /// <summary>
    /// 셀 인덱스 → grid 로컬 좌표(해당 셀의 중심점)
    /// </summary>
    public Vector2 CellToPos(Vector2Int idx)
    {
        idx = ClampCell(idx);
        Vector2 origin = GetOrigin();
        Vector2 pitch = cellSize + cellSpacing;
        return origin + new Vector2(idx.x * pitch.x, idx.y * pitch.y) + cellSize * 0.5f;
    }
}
