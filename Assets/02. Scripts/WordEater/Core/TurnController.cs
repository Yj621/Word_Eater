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
        private readonly GrowthConfig _growth;
        public int TurnsLeft { get; private set; }
        public int MistakesLeft { get; private set; }

        public TurnController(GrowthConfig growth) => _growth = growth;


        /// <summary>
        /// 단계 시작 시 규칙 초기화
        /// </summary>
        public void StartStage(GrowthStage stage)
        {
            var cfg = _growth.Get(stage);
            TurnsLeft = cfg.turnsPerStage;
            MistakesLeft = cfg.maxMistakes;
            GameEvents.OnStageStarted?.Invoke(stage, TurnsLeft, MistakesLeft);
        }

        /// <summary>
        /// 액션 수행 시 턴 차감 (Clean은 2턴)
        /// </summary>
        public bool ConsumeTurn(ActionType action)
        {
            int cost = action == ActionType.CleanNoise ? 2 : 1;
            TurnsLeft -= cost;

            // (OnTurnsChanged는 기존대로 두되, 필요시 RaiseTurnsChanged로 교체 가능)
            GameEvents.OnTurnsChanged?.Invoke(TurnsLeft);
            return TurnsLeft > 0;
        }

        /// <summary>
        /// 오답 1회 등록
        /// </summary>
        public bool RegisterMistake()
        {
            MistakesLeft -= 1;
            Debug.Log("Left Mistake : " + MistakesLeft);
            // 래퍼로 호출
            GameEvents.RaiseMistakesChanged(MistakesLeft);

            // 오답 연출
            GameEvents.RaiseMistakeHit();
            // 진동
            Handheld.Vibrate();

            return MistakesLeft >= 0;
        }

        //남은 턴 변경
        public void SetMistake(int k) {
            MistakesLeft = k;
            GameEvents.RaiseMistakesChanged(MistakesLeft);
        }

        /// <summary>
        /// 부활 복원을 위한 강제 복원 API (권장: stage 넘겨서 룰에 맞게 Clamp)
        /// </summary>
        public void ForceRestore(int turnsLeft, int mistakesLeft, GrowthStage stage)
        {
            var cfg = _growth.Get(stage);
            TurnsLeft = Mathf.Clamp(turnsLeft, 0, cfg.turnsPerStage);
            MistakesLeft = Mathf.Clamp(mistakesLeft, 0, cfg.maxMistakes);

            GameEvents.OnStageStarted?.Invoke(stage, TurnsLeft, MistakesLeft);
            GameEvents.OnTurnsChanged?.Invoke(TurnsLeft);
            GameEvents.RaiseMistakesChanged(MistakesLeft);
        }

        /// <summary>
        /// stage 없이 그대로 복원 – 기존 호출부 호환용
        /// </summary>
        public void ForceRestore(int turnsLeft, int mistakesLeft)
        {
            TurnsLeft = turnsLeft;
            MistakesLeft = mistakesLeft;

            GameEvents.OnTurnsChanged?.Invoke(TurnsLeft);
            GameEvents.RaiseMistakesChanged(MistakesLeft);
        }
    }

}
