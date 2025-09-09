using UnityEngine;
/// <summary>
/// 캔버스 안에 격자 배치 영역을 정의
/// - 셀 크기/간격/패딩으로 칸 수(columns, rows)를 계산
/// - 좌표 <-> 인덱스 변환
/// - 드래그 아이콘이 영역 밖으로 나가지 않도록 클램프
/// - 여기서 다루는 좌표는 모두 이 UIGridArea의 RectTransform 로컬 좌표
/// - 다른 RectTransform(아이콘 부모 등)에서 넘어온 좌표는 DraggableIcon에서 변환해 전달
[RequireComponent(typeof(RectTransform))]
public class UIGridArea : MonoBehaviour
{
    [Header("한 칸의 크기(px단위)")] 
    public Vector2 cellSize = new Vector2(200, 200);

    [Header("칸 사이 간격")]
    public Vector2 cellSpacing = new Vector2(50, 50);

    [Header("영역 안쪽 여백")]
    public Vector2 padding =  new Vector2(30, 30);

    [Header("영역 밖 드래그 금지 여부")]
    public bool clampToBounds = true;

    private RectTransform _rt;


    // 현재 그리드가 가질 수 있는 가로/세로 칸 수
    public int columns { get; private set; }
    public int rows { get; private set; }

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        RecalculateGrid();
    }

    /// <summary>
    /// 레이아웃(해상도/캔버스 스케일) 변경 시 자동 호출되어 칸 수를 재계산하는 메서드
    /// </summary>
    void OnRectTransformDimensionsChange()  
    {
        if (_rt != null) RecalculateGrid();
    }

    /// <summary>
    /// 영역 크기와 셀 피치(셀크기+간격)를 사용해 columns/rows를 재계산하는 메서드
    /// </summary>
    public void RecalculateGrid()
    {
        var r = _rt.rect;
        float usableW = Mathf.Max(0, r.width - padding.x * 2f);
        float usableH = Mathf.Max(0, r.height - padding.y * 2f);

        Vector2 pitch = cellSize + cellSpacing;

        // 칸 수 = (usable + 마지막 간격 보정) / pitch
        columns = Mathf.Max(1, Mathf.FloorToInt((usableW + cellSpacing.x) / pitch.x));
        rows = Mathf.Max(1, Mathf.FloorToInt((usableH + cellSpacing.y) / pitch.y));
    }

    /// <summary>
    /// 아이콘(RectTransform)의 피벗/크기를 고려하여
    /// 전달된 위치(local, grid 로컬 기준)가 영역 안에 있도록 좌표를 클램프하는 메서드
    /// </summary>
    public Vector2 ClampPositionToArea(Vector2 local, RectTransform icon)
    {
        Rect r = _rt.rect;
        float left = r.xMin + padding.x;
        float right = r.xMax - padding.x;
        float bottom = r.yMin + padding.y;
        float top = r.yMax - padding.y;

        Vector2 size = icon.rect.size;
        Vector2 pivot = icon.pivot;

        float minX = left + size.x * pivot.x;
        float maxX = right - size.x * (1f - pivot.x);
        float minY = bottom + size.y * pivot.y;
        float maxY = top - size.y * (1f - pivot.y);

        return new Vector2(
            Mathf.Clamp(local.x, minX, maxX),
            Mathf.Clamp(local.y, minY, maxY)
        );
    }

    /// <summary>
    /// 좌하단(로컬) + 패딩을 적용한 원점 좌표를 반환하는 메서드
    /// </summary>

    private Vector2 GetBottomLeftWithPadding()
    {
        var r = _rt.rect;
        return new Vector2(r.xMin + padding.x, r.yMin + padding.y);
    }

    /// <summary>
    /// 해당 셀 인덱스가 그리드 내부인지 여부
    /// </summary>

    public bool IsValidCellIndex(Vector2Int idx)
        => idx.x >= 0 && idx.y >= 0 && idx.x < columns && idx.y < rows;

    /// <summary>
    /// 셀 인덱스를 그리드 범위로 자르는 메서드
    /// </summary>

    public Vector2Int ClampCellIndex(Vector2Int idx)
        => new Vector2Int(
            Mathf.Clamp(idx.x, 0, columns - 1),
            Mathf.Clamp(idx.y, 0, rows - 1)
        );

    /// <summary>
    /// grid 로컬 좌표 → 셀 인덱스
    /// </summary>
    public Vector2Int LocalPositionToCell(Vector2 local)
    {
        Vector2 origin = GetBottomLeftWithPadding();
        Vector2 pitch = cellSize + cellSpacing;
        Vector2 delta = local - origin;

        int ix = Mathf.FloorToInt(delta.x / pitch.x);
        int iy = Mathf.FloorToInt(delta.y / pitch.y);

        return new Vector2Int(ix, iy);
    }

    /// <summary>
    /// 셀 인덱스 → grid 로컬 좌표(해당 셀의 중심점)
    /// </summary>
    public Vector2 CellToLocalPosition(Vector2Int idx)
    {
        idx = ClampCellIndex(idx);
        Vector2 origin = GetBottomLeftWithPadding();
        Vector2 pitch = cellSize + cellSpacing;
        return origin + new Vector2(idx.x * pitch.x, idx.y * pitch.y) + cellSize * 0.5f;
    }
}
