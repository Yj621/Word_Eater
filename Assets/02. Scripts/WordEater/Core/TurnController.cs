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
        public int MistakesLeft { get; private set; }

        public TurnController(GrowthConfig growth) => _growth = growth;


        /// <summary>
        /// 단계 시작 시 초기화
        /// </summary>
        public void StartStage(GrowthStage stage)
        {
            var cfg = _growth.Get(stage);
            // 턴 설정 제거
            MistakesLeft = cfg.maxMistakes;

            // 이벤트 인자 중 TurnsLeft는 의미가 없어졌으므로 -1 혹은 제거된 버전을 사용해야 함
            // 여기서는 기존 호환성을 위해 -1을 넘기거나, 이벤트 정의 자체를 수정해야 합니다.
            GameEvents.OnStageStarted?.Invoke(stage, -1, MistakesLeft);
        }

        /// <summary>
        /// 오답 1회 등록
        /// </summary>
        public bool RegisterMistake()
        {
            MistakesLeft -= 1;
            Debug.Log("Left Mistake : " + MistakesLeft);

            GameEvents.RaiseMistakesChanged(MistakesLeft);
            GameEvents.RaiseMistakeHit(); // 오답 연출
            Handheld.Vibrate(); // 진동

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
        public void ForceRestore(int mistakesLeft, GrowthStage stage)
        {
            var cfg = _growth.Get(stage);
            MistakesLeft = Mathf.Clamp(mistakesLeft, 0, cfg.maxMistakes);

            GameEvents.OnStageStarted?.Invoke(stage, -1, MistakesLeft);
            GameEvents.RaiseMistakesChanged(MistakesLeft);
        }
    }

}
