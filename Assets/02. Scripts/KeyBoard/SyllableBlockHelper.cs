using UnityEngine;

public static class SyllableBlockHelper
{
    /// <summary>
    /// 합쳐진 글자를 표현하기 위한 블럭 프리팹.
    /// 반드시 Inspector에서 1개 Assign 해야 함.
    /// </summary>
    public static SyllableBlock BlockPrefab;

    /// <summary>
    /// 외부에서 BlockPrefab을 연결할 때 호출 (선택)
    /// </summary>
    public static void SetPrefab(SyllableBlock prefab)
    {
        BlockPrefab = prefab;
    }
}
