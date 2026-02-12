using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GridLayoutGroup이 있는 영역을 관리
/// 드래그 중인 아이콘이 어느 위치(Index)에 들어가야 할지 알려주는 역할만 수행
/// </summary>
[RequireComponent(typeof(GridLayoutGroup))]
public class UIGridArea : MonoBehaviour
{
    private GridLayoutGroup _grid;
    private RectTransform _rt;

    public GridLayoutGroup Grid => _grid;

    private void Awake()
    {
        _grid = GetComponent<GridLayoutGroup>();
        _rt = GetComponent<RectTransform>();
    }

    /// <summary>
    /// 현재 드래그 중인 월드 좌표(position)를 기준으로,
    /// 그리드 내의 자식들 중 가장 가까운 인덱스를 반환
    /// </summary>
    public int GetInsertIndex(Vector2 dragIconWorldPos)
    {
        int closestIndex = 0;
        float minDistance = float.MaxValue;

        // 그리드 안의 모든 자식(아이콘들)을 순회하며 거리 비교
        // (Placeholder가 포함되어 있으므로 실시간으로 위치가 바뀜)
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i) as RectTransform;
            if (child == null) continue;

            // 거리 계산
            float dist = Vector2.Distance(child.position, dragIconWorldPos);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestIndex = i;
            }
        }

        return closestIndex;
    }
}