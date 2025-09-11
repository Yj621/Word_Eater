using System;
using WordEater.Core;

namespace WordEater.Core
{
    public static class GameEvents
    {
        public static Action<GrowthStage, int, int> OnStageStarted; // 단계, 턴 수, 남은 오답 수
        public static Action<string> OnNewWordAssigned; // 새 단어
        public static Action<string, bool> OnFeedResult; // 입력값, 정답 여부
        public static Action<GrowthStage> OnEvolved; // 진화된 단계
        public static Action OnDied; // 사망
        public static Action<int> OnTurnsChanged; //남은 턴 수

        public static Action<int, int> OnBatteryChanged;                // 현재 배터리, 최대 배터리
        public static Action OnBatteryDepleted;                         // 0칸 도달
        public static Action<ActionType> OnActionBlockedLowBattery;     // 배터리 부족으로 액션 불가
    }
}