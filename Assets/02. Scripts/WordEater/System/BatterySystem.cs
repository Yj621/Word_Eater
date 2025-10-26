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
        [Header("테스트/실사용 겸용: 0~100%")]
        [SerializeField, Range(0, 100)]
        private int currentBattery = 100; // 퍼센트가 단일 소스

        [Header("총 배터리 칸 수")]
        [SerializeField] private int maxCells = 5;   // 전체 칸 개수

        [Header("시작 칸 수")]
        [SerializeField] private int startCells = 5; // 게임 시작 시 충전 상태 (기본 풀 충전)

/*        [Header("몇 번 시도에 1칸 소모할지")]
        [SerializeField] private int feedAttemptsPerCell = 2;      // FeedData: 2번 시도 → 1칸
        [SerializeField] private int optimizeAttemptsPerCell = 2;  // OptimizeAlgo: 2번 시도 → 1칸
*/

        // 누적 카운터
        private int feedCountBuffer = 0;
        private int optimizeCountBuffer = 0;

        public int MaxCells => maxCells;          // 최대 칸 수 (읽기 전용)
        public int CurrentCells { get; private set; } // 현재 칸 (퍼센트로부터 환산)
        public int CurrentPercent => currentBattery;


        // 1칸 = 몇 % 인지
        private float PercentPerCell => 100f / Mathf.Max(1, maxCells);

        private void Awake()
        {
            // 퍼센트 -> 칸 환산 및 UI 초기화
            SyncCellsFromPercent();
            RaiseChanged();
        }

        // 인스펙터에서 값 바꿀 때 에디터에서도 바로 UI 반영되게(런타임 중에도 동작함)
        private void OnValidate()
        {
            // maxCells 최소 1 보장
            maxCells = Mathf.Max(1, maxCells);
            currentBattery = Mathf.Clamp(currentBattery, 0, 100);
            SyncCellsFromPercent();
            RaiseChanged();
        }


        /// <summary>
        /// 액션 시도 시 배터리를 소모하는 메서드
        /// 성공적으로 소모되면 true
        /// 배터리가 부족하면 false 반환
        /// </summary>
        public bool TryConsume(ActionType action)
        {
            float costInCells = GetCellsCost(action); // float 형태로 비용 가져오기
            int costInPercent = Mathf.RoundToInt(costInCells * PercentPerCell);

            // 현재 퍼센트가 필요한 퍼센트보다 적으면 막기
            if (currentBattery < costInPercent)
            {
                GameEvents.OnActionBlockedLowBattery?.Invoke(action);
                return false;
            }

            // float 비용으로 배터리 소모
            Consume(costInCells);

            if (CurrentCells <= 0 && currentBattery <= 0) // 배터리가 0이 되었는지 명확히 체크
                GameEvents.OnBatteryDepleted?.Invoke();

            return true;
        }

        /// <summary>
        /// 배터리 회복 (예: 아이템 사용, 광고 보상)
        /// </summary>
        public void Refill(int cells)
        {
            // 칸 → 퍼센트로 환산해서 올림
            int addPercent = Mathf.RoundToInt(cells * PercentPerCell);
            currentBattery = Mathf.Clamp(currentBattery + addPercent, 0, 100);
            SyncCellsFromPercent();
            RaiseChanged();
        }

        /// <summary>외부에서 퍼센트로 직접 세팅(테스트용)</summary>
        public void SetBatteryPercent(int percent)
        {
            currentBattery = Mathf.Clamp(percent, 0, 100);
            SyncCellsFromPercent();
            RaiseChanged();
        }

        /// <summary>각 액션별 기본 소모량(칸)</summary>
        private float GetCellsCost(ActionType action)
        {
            switch (action)
            {
                case ActionType.FeedData: return 0.5f;     // 1회 시 0.5칸(10%) 소모
                case ActionType.OptimizeAlgo: return 0.5f; // 1회 시 0.5칸(10%) 소모
                case ActionType.CleanNoise: return 2f;
                default: return 1f;
            }
        }
        // ---- 내부 유틸 ----

        private void Consume(float cellsToConsume)
        {
            int decPercent = Mathf.RoundToInt(cellsToConsume * PercentPerCell);
            currentBattery = Mathf.Clamp(currentBattery - decPercent, 0, 100);
            SyncCellsFromPercent();
            RaiseChanged();
        }


        private void SyncCellsFromPercent()
        {
            CurrentCells = Mathf.Clamp(
                Mathf.CeilToInt(maxCells * (currentBattery / 100f)), // Round 대신 Ceil로 변경하여 1%라도 남으면 1칸으로 표시
                0, MaxCells);
        }

        private void RaiseChanged()
        {
            // UI 텍스트는 퍼센트를 직접 사용하도록 이벤트 인자 변경
            GameEvents.OnBatteryChanged?.Invoke(CurrentCells, MaxCells, currentBattery);
        }
    }
}
