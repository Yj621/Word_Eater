using System.Collections.Generic;
using UnityEngine;

namespace WordEater.Data
{
    [CreateAssetMenu(fileName = "WordBank", menuName = "WordEater/WordBank")]
    public class WordBank : ScriptableObject
    {
        public List<WordEntry> entries = new List<WordEntry>()
        {
            //Bit
            new WordEntry { word="주사위", stage=0, related=new[]{"확률","운","게임"} },
            new WordEntry { word="거울", stage=0, related=new[]{"반사","대칭","광선"} },
            new WordEntry { word="호랑이", stage=0, related=new[]{"동물","상징","민속"} },
            new WordEntry { word="별", stage=0, related=new[]{"중력","운명","에너지"} },
            new WordEntry { word="바람", stage=0, related=new[]{"흐름","날씨","에너지"} },
            new WordEntry { word="강", stage=0, related=new[]{"흐름","지형","생명"} },
            new WordEntry { word="나무", stage=0, related=new[]{"성장","순환","조각"} },
            new WordEntry { word="대화", stage=0, related=new[]{"소통","구조"} },
            new WordEntry { word="빛", stage=0, related=new[]{"반사","광선","파동"} },
            new WordEntry { word="가을", stage=0, related=new[]{"날씨","생명","성장"} },
            new WordEntry { word="컴퓨터", stage=0, related=new[]{"논리","연산","정보"} },
            new WordEntry { word="동상", stage=0, related=new[]{"상징","민속","조각"} },
            new WordEntry { word="얼음", stage=0, related=new[]{"반사","지형","조각","음식"} },
            new WordEntry { word="야구", stage=0, related=new[]{"게임","운","운동"} },
            new WordEntry { word="김치", stage=0, related=new[]{"민속","음식","에너지"} },
            new WordEntry { word="개미", stage=0, related=new[]{"동물","생명"} },
            new WordEntry { word="독서", stage=0, related=new[]{"성장","정보","논리"} },

            // Byte
            new WordEntry { word="확률", stage=1, related=new[]{"수학","통계"} },
            new WordEntry { word="운", stage=1, related=new[]{"수학","통계"} },
            new WordEntry { word="게임", stage=1, related=new[]{"수학","문화","예술"} },
            new WordEntry { word="반사", stage=1, related=new[]{"광학","물리","천문"} },
            new WordEntry { word="대칭", stage=1, related=new[]{"광학","물리","천문"} },
            new WordEntry { word="광선", stage=1, related=new[]{"광학","물리","천문"} },
            new WordEntry { word="파동", stage=1, related=new[]{"물리","소리","공학"} },
            new WordEntry { word="에너지", stage=1, related=new[]{"물리","공학","생물"} },
            new WordEntry { word="동물", stage=1, related=new[]{"생물","자연","생태"} },
            new WordEntry { word="상징", stage=1, related=new[]{"문화","예술","철학"} },
            new WordEntry { word="민속", stage=1, related=new[]{"문화","역사","예술"} },
            new WordEntry { word="중력", stage=1, related=new[]{"물리","천문","지구"} },
            new WordEntry { word="운명", stage=1, related=new[]{"철학","문화","역사"} },
            new WordEntry { word="흐름", stage=1, related=new[]{"자연","기상","생태"} },
            new WordEntry { word="날씨", stage=1, related=new[]{"기상","자연","지리"} },
            new WordEntry { word="지형", stage=1, related=new[]{"자연","지리","역사"} },
            new WordEntry { word="생명", stage=1, related=new[]{"생물","자연","생태"} },
            new WordEntry { word="성장", stage=1, related=new[]{"생태","자연","생물"} },
            new WordEntry { word="순환", stage=1, related=new[]{"생태","자연","지구"} },
            new WordEntry { word="논리", stage=1, related=new[]{"철학","공학","수학"} },
            new WordEntry { word="연산", stage=1, related=new[]{"데이터","공학","수학"} },
            new WordEntry { word="정보", stage=1, related=new[]{"데이터","공학","수학"} },
            new WordEntry { word="소통", stage=1, related=new[]{"언어","사회","문화"} },
            new WordEntry { word="구조", stage=1, related=new[]{"언어","사회","문화"} },
            new WordEntry { word="조각", stage=1, related=new[]{"예술","철학","역사"} },
            new WordEntry { word="운동", stage=1, related=new[]{"문화","생물","예술"} },
            new WordEntry { word="음식", stage=1, related=new[]{"문화","생물","역사"} },

            // Word
            new WordEntry { word="수학", stage=2, related=new string[]{ } },
            new WordEntry { word="통계", stage=2, related=new string[]{ } },
            new WordEntry { word="문화", stage=2, related=new string[]{ } },
            new WordEntry { word="철학", stage=2, related=new string[]{ } },
            new WordEntry { word="물리", stage=2, related=new string[]{ } },
            new WordEntry { word="광학", stage=2, related=new string[]{ } },
            new WordEntry { word="천문", stage=2, related=new string[]{ } },
            new WordEntry { word="소리", stage=2, related=new string[]{ } },
            new WordEntry { word="공학", stage=2, related=new string[]{ } },
            new WordEntry { word="생물", stage=2, related=new string[]{ } },
            new WordEntry { word="자연", stage=2, related=new string[]{ } },
            new WordEntry { word="역사", stage=2, related=new string[]{ } },
            new WordEntry { word="지리", stage=2, related=new string[]{ } },
            new WordEntry { word="기상", stage=2, related=new string[]{ } },
            new WordEntry { word="지구", stage=2, related=new string[]{ } },
            new WordEntry { word="생태", stage=2, related=new string[]{ } },
            new WordEntry { word="데이터", stage=2, related=new string[]{ } },
            new WordEntry { word="언어", stage=2, related=new string[]{ } },
            new WordEntry { word="사회", stage=2, related=new string[]{ } },
            new WordEntry { word="예술", stage=2, related=new string[]{ } },
        };
    }
}

