
public static class KoreanUtils
{
    // 초성 리스트
    private static readonly char[] ChoSungs =
    {
            'ㄱ', 'ㄲ', 'ㄴ', 'ㄷ', 'ㄸ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㅃ',
            'ㅅ', 'ㅆ', 'ㅇ', 'ㅈ', 'ㅉ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ'
        };

    public static string GetChosungString(string word)
    {
        string result = "";
        foreach (char c in word)
        {
            if (c >= 0xAC00 && c <= 0xD7A3) // 한글 범위
            {
                int uniVal = c - 0xAC00;
                int choIndex = uniVal / (21 * 28);
                result += ChoSungs[choIndex];
            }
            else
            {
                result += c; // 한글 아니면 그대로 (띄어쓰기 등)
            }
        }
        return result;
    }
}
