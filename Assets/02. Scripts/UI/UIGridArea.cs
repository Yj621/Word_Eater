using UnityEngine;

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
    public int columns { get; private set; }
    public int rows { get; private set; }

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        RecomputeGrid();
    }

    void OnRectTransformDimensionsChange()  // 해상도/캔버스 스케일 바뀔 때 자동 갱신
    {
        if (_rt != null) RecomputeGrid();
    }

    public void RecomputeGrid()
    {
        var r = _rt.rect;
        float usableW = Mathf.Max(0, r.width - padding.x * 2f);
        float usableH = Mathf.Max(0, r.height - padding.y * 2f);

        Vector2 pitch = cellSize + cellSpacing;

        // 칸 수 = (usable + 간격) / pitch  (가장자리 마지막 간격을 고려해 +spacing 보정)
        columns = Mathf.Max(1, Mathf.FloorToInt((usableW + cellSpacing.x) / pitch.x));
        rows = Mathf.Max(1, Mathf.FloorToInt((usableH + cellSpacing.y) / pitch.y));
        // Debug.Log($"grid {columns}x{rows}, rect={r}");
    }

    /// <summary>
    /// 영역 내로 좌표 제한 (피벗 위치에 관계없이 안전하게)
    /// </summary>
    public Vector2 ClampToArea(Vector2 local, RectTransform icon)
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
    private Vector2 BottomLeftWithPadding()
    {
        var r = _rt.rect;
        return new Vector2(r.xMin + padding.x, r.yMin + padding.y);
    }

    public bool IsInsideIndex(Vector2Int idx)
        => idx.x >= 0 && idx.y >= 0 && idx.x < columns && idx.y < rows;

    public Vector2Int ClampIndex(Vector2Int idx)
        => new Vector2Int(
            Mathf.Clamp(idx.x, 0, columns - 1),
            Mathf.Clamp(idx.y, 0, rows - 1)
        );

    public Vector2Int LocalPosToIndex(Vector2 local)
    {
        Vector2 origin = BottomLeftWithPadding();
        Vector2 pitch = cellSize + cellSpacing;
        Vector2 delta = local - origin;

        int ix = Mathf.FloorToInt(delta.x / pitch.x);
        int iy = Mathf.FloorToInt(delta.y / pitch.y);

        return new Vector2Int(ix, iy);
    }

    public Vector2 IndexToLocalPos(Vector2Int idx)
    {
        idx = ClampIndex(idx);
        Vector2 origin = BottomLeftWithPadding();
        Vector2 pitch = cellSize + cellSpacing;
        return origin + new Vector2(idx.x * pitch.x, idx.y * pitch.y) + cellSize * 0.5f;
    }
}
