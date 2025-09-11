using UnityEngine;
using WordEater.Core;

namespace WordEater.Systems
{
    /// <summary>
    /// 칸 단위 배터리 시스템.
    /// - 배터리 = 일정 개수의 칸(maxCells)
    /// - FeedData(먹이주기) / OptimizeAlgo(최적화) / CleanNoise(노이즈 제거) 액션 시 칸 소모
    /// - FeedData와 OptimizeAlgo는 2번 시도할 때마다 1칸 차감
    /// - 배터리 잔량 변경 시 GameEvents.OnBatteryChanged 이벤트
    /// - 잔량 부족 시 GameEvents.OnActionBlockedLowBattery 이벤트
    /// - 배터리 완전 소진 시 GameEvents.OnBatteryDepleted 이벤트
    /// </summary>
    public class BatterySystem : MonoBehaviour
    {
        [Header("총 배터리 칸 수")]
        [SerializeField] private int maxCells = 5;   // 전체 칸 개수

        [Header("시작 칸 수")]
        [SerializeField] private int startCells = 5; // 게임 시작 시 충전 상태 (기본 풀 충전)

        [Header("몇 번 시도에 1칸 소모할지")]
        [SerializeField] private int feedAttemptsPerCell = 2;      // FeedData: 2번 시도 → 1칸
        [SerializeField] private int optimizeAttemptsPerCell = 2;  // OptimizeAlgo: 2번 시도 → 1칸

        // 누적 카운터
        private int feedCountBuffer = 0;
        private int optimizeCountBuffer = 0;

        public int MaxCells => maxCells;          // 최대 칸 수 (읽기 전용)
        public int CurrentCells { get; private set; } // 현재 남은 칸 수

        private void Awake()
        {
            // 시작 시 남은 칸을 세팅 (0 ~ max 범위로 제한)
            CurrentCells = Mathf.Clamp(startCells, 0, maxCells);

            // 배터리 UI 초기 세팅
            GameEvents.OnBatteryChanged?.Invoke(CurrentCells, MaxCells);
        }

        /// <summary>
        /// 액션 시도 시 배터리를 소모하는 메서드
        /// 성공적으로 소모되면 true
        /// 배터리가 부족하면 false 반환
        /// </summary>
        public bool TryConsume(ActionType action)
        {
            int need = GetCellsCost(action);

            // 누적형 소모 처리 (FeedData / OptimizeAlgo)
            if (action == ActionType.FeedData)
            {
                feedCountBuffer++;
                // 임계치 미만: 아직 소모 안 함(성공 처리만)
                if (feedCountBuffer < Mathf.Max(1, feedAttemptsPerCell))
                    return true;

                // 임계치 도달: 1칸 소모
                feedCountBuffer = 0;
                need = 1;
            }
            else if (action == ActionType.OptimizeAlgo)
            {
                optimizeCountBuffer++;
                if (optimizeCountBuffer < Mathf.Max(1, optimizeAttemptsPerCell))
                    return true;

                optimizeCountBuffer = 0;
                need = 1;
            }

            // 일반 소모 처리
            if (CurrentCells < need)
            {
                GameEvents.OnActionBlockedLowBattery?.Invoke(action);
                return false;
            }

            CurrentCells -= need;
            GameEvents.OnBatteryChanged?.Invoke(CurrentCells, MaxCells);

            if (CurrentCells <= 0)
                GameEvents.OnBatteryDepleted?.Invoke();

            return true;
        }

        /// <summary>
        /// 배터리 회복 (예: 아이템 사용, 광고 보상)
        /// </summary>
        public void Refill(int cells)
        {
            CurrentCells = Mathf.Clamp(CurrentCells + cells, 0, MaxCells);
            GameEvents.OnBatteryChanged?.Invoke(CurrentCells, MaxCells);
        }

        /// <summary>
        /// 각 액션별 기본 소모량 정의
        /// - FeedData: 1칸 (실제로는 2번 시도에 1칸)
        /// - OptimizeAlgo: 1칸
        /// - CleanNoise: 2칸
        /// </summary>
        private int GetCellsCost(ActionType action)
        {
            switch (action)
            {
                case ActionType.FeedData: return 1;
                case ActionType.OptimizeAlgo: return 1;
                case ActionType.CleanNoise: return 1;
                default: return 1;
            }
        }
    }
}
