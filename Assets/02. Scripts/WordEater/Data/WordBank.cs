using System.Collections.Generic;
using UnityEngine;

namespace WordEater.Data
{
    [CreateAssetMenu(fileName = "WordBank", menuName = "WordEater/WordBank")]
    public class WordBank : ScriptableObject
    {
        public List<WordEntry> entries = new List<WordEntry>()
        {
            new WordEntry{ word="주사위", topic="확률", related=new[]{"던지기","면","게임"}, difficulty=0 },
            new WordEntry{ word="확률",   topic="통계", related=new[]{"기댓값","표본","분포"}, difficulty=1 },
            new WordEntry{ word="통계학", topic="수학", related=new[]{"검정","추정","회귀"}, difficulty=2 },

            new WordEntry{ word="거울",   topic="빛",   related=new[]{"반사","상","유리"}, difficulty=0 },
            new WordEntry{ word="반사",   topic="광학", related=new[]{"각도","법선","스펙"}, difficulty=1 },
            new WordEntry{ word="광학",   topic="물리", related=new[]{"굴절","간섭","파장"}, difficulty=2 },

            new WordEntry{ word="나무",   topic="자연", related=new[]{"잎","숲","뿌리"}, difficulty=0 },
        };
    }
}

