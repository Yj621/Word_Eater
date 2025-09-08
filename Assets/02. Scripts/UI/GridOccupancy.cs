using System.Collections.Generic;
using UnityEngine;

public class GridOccupancy : MonoBehaviour
{
    public static GridOccupancy Instance { get; private set; }
    void Awake() => Instance = this;

    // 인덱스 → 아이콘
    private readonly Dictionary<Vector2Int, DraggableIcon> _byIndex = new();
    // 아이콘 → 인덱스
    private readonly Dictionary<DraggableIcon, Vector2Int> _byIcon = new();

    public bool IsOccupied(Vector2Int idx) => _byIndex.ContainsKey(idx);

    /// <summary>
    /// 아이콘이 현재 점유 중인 칸을 해제
    /// </summary>
    public void Release(DraggableIcon icon)
    {
        if (_byIcon.TryGetValue(icon, out var idx))
        {
            _byIcon.Remove(icon);
            // idx가 여전히 자신을 가리킬 때만 삭제(방어)
            if (_byIndex.TryGetValue(idx, out var who) && who == icon)
                _byIndex.Remove(idx);
        }
    }

    /// <summary>
    /// target 근처에서 가장 가까운 빈 칸을 찾고, 즉시 점유까지 원자적으로 처리
    /// </summary>
    public Vector2Int TryReserveNearestFree(
       Vector2Int target, DraggableIcon icon, int maxRadius, System.Func<Vector2Int, bool> isInside)
    {
        bool hadPrev = _byIcon.TryGetValue(icon, out var prevIdx);
        Release(icon);

        // target이 범위 밖이면 가장 가까운 범위 내부 인덱스로 바꿔도 좋음(여기선 그냥 검사)
        if (isInside(target) && !IsOccupied(target))
        {
            OccupyInternal(target, icon);
            return target;
        }

        for (int r = 1; r <= maxRadius; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                int dy1 = r - Mathf.Abs(dx);

                var cand1 = new Vector2Int(target.x + dx, target.y + dy1);
                if (isInside(cand1) && !IsOccupied(cand1))
                {
                    OccupyInternal(cand1, icon);
                    return cand1;
                }

                var cand2 = new Vector2Int(target.x + dx, target.y - dy1);
                if (isInside(cand2) && !IsOccupied(cand2))
                {
                    OccupyInternal(cand2, icon);
                    return cand2;
                }
            }
        }

        if (hadPrev && isInside(prevIdx) && !IsOccupied(prevIdx))
        {
            OccupyInternal(prevIdx, icon);
            return prevIdx;
        }
        return target; // 점유하지 않음
    }


    /// <summary>
    /// 내부 점유 처리 (양방향 매핑 동기화)
    /// </summary>
    private void OccupyInternal(Vector2Int idx, DraggableIcon icon)
    {
        _byIndex[idx] = icon;
        _byIcon[icon] = idx;
    }

    /// <summary>
    /// (초기배치용) 인덱스로 강제 점유. 기존 점유와 충돌 시 덮어씌우지 않음.
    /// </summary>
    public bool TryOccupyAt(Vector2Int idx, DraggableIcon icon)
    {
        if (IsOccupied(idx)) return false;
        Release(icon);
        OccupyInternal(idx, icon);
        return true;
    }

    /// <summary>
    /// 디버그/유틸: 아이콘의 현재 인덱스 얻기
    /// </summary>
    public bool TryGetIndex(DraggableIcon icon, out Vector2Int idx) => _byIcon.TryGetValue(icon, out idx);
}
