using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WordEater.Core;

/// <summary>
/// 배터리 상태를 칸 단위 이미지로 표시하는 UI
/// </summary>
public class BatteryUI : MonoBehaviour
{
    // - cellImages: 인스펙터에 배터리 칸 이미지들을 순서대로 넣어줌 
    [SerializeField] private List<Image> cellImages; // 배터리 칸 이미지들

    private void OnEnable()
    {
        // 배터리 변화 이벤트 구독 시작
        GameEvents.OnBatteryChanged += HandleBatteryChanged;
    }

    private void OnDisable()
    {
        // 씬에서 사라질 때 이벤트 구독 해제
        GameEvents.OnBatteryChanged -= HandleBatteryChanged;
    }

    /// <summary>
    /// 배터리 잔량이 바뀔 때 호출되는 콜백
    /// 남은 칸 수(current)에 따라 이미지 On/Off 처리
    /// </summary>
    private void HandleBatteryChanged(int current, int max)
    {
        for (int i = 0; i < cellImages.Count; i++)
        {
            bool on = i < current; // 현재 잔량보다 index가 작으면 켜짐
            cellImages[i].enabled = on;
            // 필요하면 색상 바꾸기: cellImages[i].color = on ? fullColor : emptyColor
        }
    }
}
