using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 셀 단위 점유 상태를 관리해 아이콘끼리 겹치지 않게 하는 스크립트
/// - 셀 인덱스 <-> 아이콘 양방향 매핑 유지
/// - 목표 셀 주변의 가장 가까운 빈 칸을 탐색/점유
/// - 드래그 시작 시 이전 점유 해제, 드롭 시 점유 재설정
/// </summary>
public class GridOccupancy : MonoBehaviour
{
    public static GridOccupancy Instance { get; private set; }
    private void Awake() => Instance = this;

    // 인덱스 → 아이콘
    private readonly Dictionary<Vector2Int, DraggableIcon> _cellToIcon = new();
    // 아이콘 → 인덱스
    private readonly Dictionary<DraggableIcon, Vector2Int> _iconToCell = new();

    /// <summary>해당 셀에 이미 누군가가 있는지</summary>
    public bool IsCellOccupied(Vector2Int cell) => _cellToIcon.ContainsKey(cell);

    /// <summary>
    /// 아이콘이 점유하던 셀을 해제
    /// </summary>
    public void ReleaseIcon(DraggableIcon icon)
    {
        if (_iconToCell.TryGetValue(icon, out var cell))
        {
            _iconToCell.Remove(icon);
            if (_cellToIcon.TryGetValue(cell, out var who) && who == icon)
                _cellToIcon.Remove(cell);
        }
    }

    /// <summary>
    /// target 근처에서 가장 가까운 빈 칸을 찾고, 찾자마자 점유까지 원자적으로 처리
    /// - isInside: 그리드 내부 여부를 판단하는 콜백(UIGridArea.IsValidCellIndex 전달)
    /// - maxRadius: 타깃 셀에서 얼마나 멀리까지 탐색할지(맨해튼 반경)
    /// </summary>
    public Vector2Int ReserveNearestFreeCell(
        Vector2Int target, DraggableIcon icon, int maxRadius, System.Func<Vector2Int, bool> isInside)
    {
        bool hadPrev = _iconToCell.TryGetValue(icon, out var prevCell);
        ReleaseIcon(icon);

        // 타깃이 유효하고 비어 있으면 즉시 점유
        if (isInside(target) && !IsCellOccupied(target))
        {
            OccupyCellInternal(target, icon);
            return target;
        }

        // 반경을 늘려가며 마름모(맨해튼 거리) 순회로 가장 가까운 빈 칸 탐색
        for (int r = 1; r <= maxRadius; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                int dy1 = r - Mathf.Abs(dx);

                var cand1 = new Vector2Int(target.x + dx, target.y + dy1);
                if (isInside(cand1) && !IsCellOccupied(cand1))
                {
                    OccupyCellInternal(cand1, icon);
                    return cand1;
                }

                var cand2 = new Vector2Int(target.x + dx, target.y - dy1);
                if (isInside(cand2) && !IsCellOccupied(cand2))
                {
                    OccupyCellInternal(cand2, icon);
                    return cand2;
                }
            }
        }

        // 예비 복구: 이전 칸이 아직 유효/비어있다면 그곳을 다시 점유
        if (hadPrev && isInside(prevCell) && !IsCellOccupied(prevCell))
        {
            OccupyCellInternal(prevCell, icon);
            return prevCell;
        }

        // 실패 시 target 반환(점유 실패 의미)
        return target;
    }

    /// <summary>내부 점유 처리(양방향 매핑 동기화)</summary>
    private void OccupyCellInternal(Vector2Int cell, DraggableIcon icon)
    {
        _cellToIcon[cell] = icon;
        _iconToCell[icon] = cell;
    }

    /// <summary>
    /// 셀에 강제로 점유를 시도
    /// 이미 누가 있으면 실패
    /// </summary>
    public bool TryOccupyAtCell(Vector2Int cell, DraggableIcon icon)
    {
        if (IsCellOccupied(cell)) return false;
        ReleaseIcon(icon);
        OccupyCellInternal(cell, icon);
        return true;
    }

    /// <summary>아이콘이 현재 점유 중인 셀을 얻습니다.</summary>
    public bool TryGetCellOfIcon(DraggableIcon icon, out Vector2Int cell)
        => _iconToCell.TryGetValue(icon, out cell);
}
