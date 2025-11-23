using TMPro;
using WordEater.Systems;
using WordEater.Core;

public static class AlgoGuards
{
    /// <summary>
    /// 배터리 잔량 확인 및 소비 시도
    /// 배터리가 부족하면 resultText에 안내 문구 출력 후 false 반환
    /// </summary>
    public static bool EnsureBattery(BatterySystem battery, ActionType actionType, TextMeshProUGUI resultText = null)
    {
        if (battery == null)
            return true; // 배터리 시스템이 없는 경우엔 그냥 통과 (편의용)

        if (!battery.TryConsume(actionType))
        {
            if (resultText != null)
                resultText.text = "배터리가 부족합니다.";
            return false;
        }

        return true;
    }
}
