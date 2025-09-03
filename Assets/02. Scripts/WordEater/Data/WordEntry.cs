using UnityEngine;

namespace WordEater.Data
{
    [System.Serializable]
    public class WordEntry
    {
        [Tooltip("정답 단어")] 
        public string word;

        [Tooltip("주제/맥락(색/상성 힌트에 사용)")] 
        public string topic;

        [Tooltip("연관 키워드(힌트/AI 프롬프트 시드로 활용)")]
        public string[] related;

        [Tooltip("난이도(0=쉬움 ~ 3=어려움)")]
        public int difficulty;
    }
}