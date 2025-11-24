using UnityEngine;

namespace WordEater.Core
{
    public enum GrowthStage { Bit = 0, Byte = 1, Word = 2 }

    public enum ActionType
    {
        // 제출 관련
        SubmitBit,   // 비트 (20%)
        SubmitByte,  // 바이트 (15%)
        SubmitWord,  // 워드 (10%)

        OptimizeAlgo,  // 최적 알고리즘(미니게임 자리, 힌트/버프 획득)
                       // 유사 단어 지급(메세지) / 단어에 대한 힌트(전화)

        CleanNoise     // 노이즈 제거(2턴 소모, 배율/보상 버프)
    }
    
}