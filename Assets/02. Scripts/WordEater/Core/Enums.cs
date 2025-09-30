using UnityEngine;

namespace WordEater.Core
{
    public enum GrowthStage { Bit = 0, Byte = 1, Word = 2 }

    public enum ActionType
    {
        FeedData,      // 데이터 주입(정답 시 정답수 +1 / 오답 시 오답수 +1)
                       // 비트 : 2번 / 바이트 : 4번 / 워드 : 6번

        OptimizeAlgo,  // 최적 알고리즘(미니게임 자리, 힌트/버프 획득)
                       // 유사 단어 지급(메세지) / 단어에 대한 힌트(전화)

        CleanNoise     // 노이즈 제거(2턴 소모, 배율/보상 버프)
    }
    
}