using UnityEngine;

namespace WordEater.Data
{
    [System.Serializable]
    public class StageConfig
    {
        public int turnsPerStage = 4; // 해당 단계에서 플레이어가 행동할 수 있는 총 턴 수
        public int maxMistakes = 2;   // 허용 가능한 오답 횟수
        public int requiredCorrectToAdvance = 2; // 다음 단계로 진화하기 위해 필요한 정답 횟수
    }

}
