using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordEater.Core;

/// <summary>
/// 배터리 상태를 칸 단위 이미지로 표시하는 UI
/// </summary>
public class BatteryUI : MonoBehaviour
{
    [SerializeField] private List<Image> cellImages; // 배터리 칸 이미지들

    [Header("배터리 색상")]
    [SerializeField] private Color fullColor = Color.white;
    [SerializeField] private Color orangeColor = new Color(1f, 0.64f, 0f); // 주황
    [SerializeField] private Color redColor = Color.red;

    [SerializeField] private TextMeshProUGUI batteryText; // % 텍스트
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
    private void HandleBatteryChanged(int current, int max, int percent)
    {
        float ratio = (float)current / max;

        // 색상 결정
        Color targetColor;
        if (ratio >= 0.8f)
            targetColor = fullColor;
        else if (ratio >= 0.5f)
            targetColor = orangeColor;
        else
            targetColor = redColor;

        // 칸별 이미지 On/Off 및 색상 적용
        for (int i = 0; i < cellImages.Count; i++)
        {
            bool on = i < current;
            cellImages[i].enabled = on;
            if (on)
                cellImages[i].color = targetColor;
        }
        if (batteryText != null)
        {
            batteryText.text = $"{percent}%";
        }
    }
}
