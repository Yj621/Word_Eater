using UnityEngine;
using System.Collections.Generic;


[System.Serializable]
public class WordStageImages
{
    public string wordId;
    
    [Header("Main Images")]
    public Sprite stage1;
    public Sprite stage2;
    public Sprite stage3;

    [Header("Animations (Optional)")]
    public List<Sprite> stage1Anim; // Bit 단계 애니메이션
    public List<Sprite> stage2Anim; // Byte 단계 애니메이션
    public List<Sprite> stage3Anim; // Word 단계 애니메이션
}


[CreateAssetMenu(fileName = "WordImageDatabase", menuName = "WordEater/Word Image Database")]
public class WordImageDatabase : ScriptableObject
{
    public List<WordStageImages> entries;
}

