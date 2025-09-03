using UnityEngine;
using WordEater.Core;
using WordEater.Data;

namespace WordEater.Core
{
    /// <summary>
    /// 현재 단계(StageConfig)에 따라 '턴/오답' 수를 관리하는 컨트롤러
    /// </summary>
    
    public class TurnController
    {
        private readonly GrowthConfig _growth; // 단계별 룰(GrowthConfig)
        public int TurnsLeft { get; private set; }     // 남은 턴
        public int MistakesLeft { get; private set; }  // 남은 허용 오답 수

        public TurnController(GrowthConfig growth) => _growth = growth;

        /// <summary>
        /// 단계 시작 시 규칙 초기화
        /// </summary>
        public void StartStage(GrowthStage stage)
        {
            var cfg = _growth.Get(stage);
            TurnsLeft = cfg.turnsPerStage;
            MistakesLeft = cfg.maxMistakes;
            GameEvnets.OnStageStarted?.Invoke(stage, TurnsLeft, MistakesLeft);
        }

        /// <summary>
        /// 액션 수행 시 턴 차감 (Clean은 2턴)
        /// </summary>
        public bool ConsumeTurn(ActionType action)
        {
            int cost = action == ActionType.CleanNoise ? 2 : 1;
            TurnsLeft -= cost;
            GameEvnets.OnTurnsChanged?.Invoke(TurnsLeft);
            return TurnsLeft >= 0;
        }

        /// <summary>
        /// 오답 1회 등록
        /// </summary>
        public bool RegisterMistake()
        {
            MistakesLeft -= 1;
            return MistakesLeft >= 0;
        }
    }
}
