using System;
using System.Collections.Generic;
using System.Text;

public static class HangulCompose
{
    // 초성(호환 자모 → 유니코드 정식 자모 매핑)
    static readonly Dictionary<char, char> Lmap = new()
    {
        ['ㄱ'] = 'ᄀ',
        ['ㄲ'] = 'ᄁ',
        ['ㄴ'] = 'ᄂ',
        ['ㄷ'] = 'ᄃ',
        ['ㄸ'] = 'ᄄ',
        ['ㄹ'] = 'ᄅ',
        ['ㅁ'] = 'ᄆ',
        ['ㅂ'] = 'ᄇ',
        ['ㅃ'] = 'ᄈ',
        ['ㅅ'] = 'ᄉ',
        ['ㅆ'] = 'ᄊ',
        ['ㅇ'] = 'ᄋ',
        ['ㅈ'] = 'ᄌ',
        ['ㅉ'] = 'ᄍ',
        ['ㅊ'] = 'ᄎ',
        ['ㅋ'] = 'ᄏ',
        ['ㅌ'] = 'ᄐ',
        ['ㅍ'] = 'ᄑ',
        ['ㅎ'] = 'ᄒ',
    };

    // 중성(호환 자모 → 유니코드 정식 자모 매핑, 복합 모음 포함)
    static readonly Dictionary<char, char> Vmap = new()
    {
        ['ㅏ'] = 'ᅡ',
        ['ㅐ'] = 'ᅢ',
        ['ㅑ'] = 'ᅣ',
        ['ㅒ'] = 'ᅤ',
        ['ㅓ'] = 'ᅥ',
        ['ㅔ'] = 'ᅦ',
        ['ㅕ'] = 'ᅧ',
        ['ㅖ'] = 'ᅨ',
        ['ㅗ'] = 'ᅩ',
        ['ㅘ'] = 'ᅪ',
        ['ㅙ'] = 'ᅫ',
        ['ㅚ'] = 'ᅬ',
        ['ㅛ'] = 'ᅭ',
        ['ㅜ'] = 'ᅮ',
        ['ㅝ'] = 'ᅯ',
        ['ㅞ'] = 'ᅰ',
        ['ㅟ'] = 'ᅱ',
        ['ㅠ'] = 'ᅲ',
        ['ㅡ'] = 'ᅳ',
        ['ㅢ'] = 'ᅴ',
        ['ㅣ'] = 'ᅵ',
    };

    // 종성(호환 자모 → 유니코드 정식 자모 매핑, 겹받침 포함)
    static readonly Dictionary<char, char> Tmap = new()
    {
        ['\0'] = '\0',
        ['ㄱ'] = 'ᆨ',
        ['ㄲ'] = 'ᆩ',
        ['ㄳ'] = 'ᆪ',
        ['ㄴ'] = 'ᆫ',
        ['ㄵ'] = 'ᆬ',
        ['ㄶ'] = 'ᆭ',
        ['ㄷ'] = 'ᆮ',
        ['ㄹ'] = 'ᆯ',
        ['ㄺ'] = 'ᆰ',
        ['ㄻ'] = 'ᆱ',
        ['ㄼ'] = 'ᆲ',
        ['ㄽ'] = 'ᆳ',
        ['ㄾ'] = 'ᆴ',
        ['ㄿ'] = 'ᆵ',
        ['ㅀ'] = 'ᆶ',
        ['ㅁ'] = 'ᆷ',
        ['ㅂ'] = 'ᆸ',
        ['ㅄ'] = 'ᆹ',
        ['ㅅ'] = 'ᆺ',
        ['ㅆ'] = 'ᆻ',
        ['ㅇ'] = 'ᆼ',
        ['ㅈ'] = 'ᆽ',
        ['ㅊ'] = 'ᆾ',
        ['ㅋ'] = 'ᆿ',
        ['ㅌ'] = 'ᇀ',
        ['ㅍ'] = 'ᇁ',
        ['ㅎ'] = 'ᇂ',
    };

    // 유니코드 한글 자소 상수
    const int SBase = 0xAC00;
    const int LBase = 0x1100;
    const int VBase = 0x1161;
    const int TBase = 0x11A7;
    const int LCount = 19;
    const int VCount = 21;
    const int TCount = 28; // T=0(받침 없음) 포함
    const int NCount = VCount * TCount;

    public static char Compose(char L, char V, char? T = null)
    {
        int LIndex = L - LBase;
        int VIndex = V - VBase;
        int TIndex = T.HasValue ? (T.Value - TBase) : 0;

        if (LIndex < 0 || LIndex >= LCount) throw new ArgumentException("L");
        if (VIndex < 0 || VIndex >= VCount) throw new ArgumentException("V");
        if (TIndex < 0 || TIndex >= TCount) throw new ArgumentException("T");

        int SIndex = (LIndex * VCount + VIndex) * TCount + TIndex;
        return (char)(SBase + SIndex);
    }

    /// <summary>
    /// 호환용 자모(ㄱ, ㅏ, ㄴ 등) 문자열을 입력받아 한글 한 글자로 합성
    /// </summary>
    public static char ComposeCompat(string L_compat, string V_compat, string T_compat = null)
    {
        if (string.IsNullOrEmpty(L_compat) || string.IsNullOrEmpty(V_compat))
            throw new ArgumentException("L/V empty");

        char L = Lmap[L_compat[0]];
        char V = Vmap[V_compat[0]];
        char T = '\0';
        if (!string.IsNullOrEmpty(T_compat))
        {
            if (!Tmap.TryGetValue(T_compat[0], out T))
                throw new ArgumentException($"invalid T '{T_compat}'");
        }
        return Compose(L, V, T == '\0' ? (char?)null : T);
    }
}
