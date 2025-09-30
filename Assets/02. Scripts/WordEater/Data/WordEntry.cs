using UnityEngine;

namespace WordEater.Data
{
    [System.Serializable]
    public class WordEntry
    {
        [Tooltip("정답 단어")] 
        public string word;

        [Tooltip("단계.0->bit , 1->byte , 2->word 각 단계에 맞는 단어만 나게 하기 위함")] 
        public int stage;

        [Tooltip("하위 단어들 모음")]
        public string[] related;
    }
}