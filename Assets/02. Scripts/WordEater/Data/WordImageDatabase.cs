using UnityEngine;
using System.Collections.Generic;


[System.Serializable]
public class WordStageImages
{
    public string wordId;
    public Sprite stage1;
    public Sprite stage2;
    public Sprite stage3;
}


[CreateAssetMenu(fileName = "WordImageDatabase", menuName = "WordEater/Word Image Database")]
public class WordImageDatabase : ScriptableObject
{
    public List<WordStageImages> entries;
}

