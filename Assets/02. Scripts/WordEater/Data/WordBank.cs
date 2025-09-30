using System.Collections.Generic;
using UnityEngine;

namespace WordEater.Data
{
    [CreateAssetMenu(fileName = "WordBank", menuName = "WordEater/WordBank")]
    public class WordBank : ScriptableObject
    {
        // 헷갈리지 않게 단어 리스트를 적어둠
        // Bit : 주사위 , 거울 , 빛 , 호랑이 , 별 , 바람 , 강 , 나무 , 대화

        // Byte : 확률 , 운 , 게임 , 반사 , 대칭 , 광선 , 파동 , 에너지 , 동물 , 상징 ,
        //민속 , 중력 , 운명 , 흐름 , 날씨 , 지형 , 생명 , 성장 , 순환 , 논리 , 연산 , 정보
        //소통 , 구조

        // Word : 수학 , 통계 , 문화 , 철학 , 물리 , 광학 , 천문 , 소리 , 공학
        //생물 , 자연 , 역사 , 지리 , 기상 , 지구 , 생태 , 데이터 , 언어 , 사회 , 예술


        public List<WordEntry> entries = new List<WordEntry>()
        {
            //Bit
            new WordEntry { word="주사위", stage=0, related=new[]{"확률","운","게임"} },
            new WordEntry { word="거울", stage=0, related=new[]{"반사","대칭","광선"} },
            new WordEntry { word="호랑이", stage=0, related=new[]{"동물","상징","민속"} },
            new WordEntry { word="별", stage=0, related=new[]{"중력","천문","운명"} },
            new WordEntry { word="바람", stage=0, related=new[]{"흐름","날씨","에너지"} },
            new WordEntry { word="강", stage=0, related=new[]{"흐름","지형","생명"} },
            new WordEntry { word="나무", stage=0, related=new[]{"성장","순환","생태"} },
            new WordEntry { word="대화", stage=0, related=new[]{"소통","구조","사회"} },

            // Byte
            new WordEntry { word="확률", stage=1, related=new[]{"수학","통계","게임"} },
            new WordEntry { word="운", stage=1, related=new[]{"수학","통계","게임"} },
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
            new WordEntry { word="데이터", stage=1, related=new[]{"공학","수학","통계"} },

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

