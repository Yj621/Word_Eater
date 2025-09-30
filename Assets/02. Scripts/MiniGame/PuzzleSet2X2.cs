using UnityEngine;

[System.Serializable]
public struct PuzzleSet2X2
{
    [Tooltip("좌상(0), 우상(1), 좌하(2), 우하(3) 순서로 4장")]
    public Sprite[] quads; // length == 4
    public string name;    // 디버그용
}
