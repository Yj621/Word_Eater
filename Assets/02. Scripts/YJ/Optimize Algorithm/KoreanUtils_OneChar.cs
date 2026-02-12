using UnityEngine;

public static class KoreanUtils_OneChar
{
    // 한글 초성 모음 배열임
    private static readonly char[] ChoSungs =
    {
        'ㄱ','ㄲ','ㄴ','ㄷ','ㄸ','ㄹ','ㅁ','ㅂ','ㅃ',
        'ㅅ','ㅆ','ㅇ','ㅈ','ㅉ','ㅊ','ㅋ','ㅌ','ㅍ','ㅎ'
    };

    /// <summary>
    /// 인덱스 넣으면 해당하는 초성 문자 반환
    /// </summary>
    public static char GetCho(int choIndex)
    {
        // 인덱스가 배열 범위 벗어나지 않게 Clamp로 안전장치 걸어둠
        choIndex = Mathf.Clamp(choIndex, 0, ChoSungs.Length - 1);
        return ChoSungs[choIndex];
    }
}