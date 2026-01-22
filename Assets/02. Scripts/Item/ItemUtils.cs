using WordEater.Systems;

public static class ItemUtils
{
    public static string GetItemNameKR(ItemType type)
    {
        return type switch
        {
            ItemType.BatteryRefill => "배터리 충전",
            ItemType.HintChosung => "초성 힌트",
            ItemType.FillKeyCounts => "자음/모음 채우기",
            ItemType.ReviveTicket => "부활권",
            ItemType.JamoSelectionTicket => "자음/모음 선택권",
            _ => "알 수 없는 아이템"
        };
    }
}