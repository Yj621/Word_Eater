using System;
using WordEater.Core;

namespace WordEater.Core
{
    public static class GameEvnets
    {
        public static Action<GrowthStage, int, int> OnStageStarted; // 단계, 턴 수, 남은 오답 수
        public static Action<string> OnNewWordAssigned; // 새 단어
        public static Action<string, bool> OnFeedResult; // 입력값, 정답 여부
        public static Action<GrowthStage> OnEvolved; // 진화된 단계
        public static Action OnDied; // 사망
        public static Action<int> OnTurnsChanged; //남은 턴 수
    }
}