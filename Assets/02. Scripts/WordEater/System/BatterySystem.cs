using System;
using System.Collections;
using System.Collections.Generic;
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

        [Header("광고 충전 팝업")]
        [SerializeField] private ADPopup batteryPopup; // 같은 프리팹이어도 되고, 다른 오브젝트여도 됨

        [Header("자동 회복 설정")]
        [SerializeField] private bool enableAutoRecharge = true; // 자동 회복 켜기/끄기
        [SerializeField] private int rechargeRatePerHour = 10;   // 1시간당 회복량 (%)
        // 내부 변수
        private const string KEY_EXIT_TIME = "Battery_ExitTime";
        private const string KEY_SAVED_BATTERY = "Battery_SavedPercent";

        public int MaxCells => maxCells;          // 최대 칸 수 (읽기 전용)
        public int CurrentCells { get; private set; } // 현재 칸 (퍼센트로부터 환산)
        public int CurrentPercent => currentBattery;

        private void Awake()
        {
            // 데이터 로드만 수행
            LoadBatteryState();
            SyncCellsFromPercent();
        }

        private void Start()
        {
            // UIManager가 Awake에서 초기화되므로, 
            // 팝업 로직(오프라인 보상)을 실행
            CheckOfflineRecharge();

            // 이벤트 전파
            RaiseChanged();

            // 자동 회복 코루틴
            if (enableAutoRecharge)
            {
                StartCoroutine(RuntimeRechargeRoutine());
            }
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
        /// 광고 보고 배터리 충전 팝업 표시
        /// - UI 버튼 OnClick에 이 메서드를 연결해서 사용
        /// </summary>
        public void ShowBatteryAdPopup()
        {
            if (batteryPopup == null)
            {
                Debug.LogWarning("[Battery] batteryAdPopup 미할당");
                return;
            }

            batteryPopup.Configure(
                title: "광고보고 배터리 충전하기",
                watchAdText: "충전하기",
                noThanksText: "아니오"
            );

            batteryPopup.Show(
                onAccept: () =>
                {
                    RefillToMax();
                },
                onDecline: () =>
                {
                    Debug.Log("[Battery] 광고 충전 거절");
                }
            );
        }


        /// <summary>
        /// 액션 시도 시 배터리를 소모하는 메서드
        /// 성공적으로 소모되면 true
        /// 배터리가 부족하면 false 반환
        /// </summary>
        /// <summary>
        /// 액션 수행 시 배터리 소모 (퍼센트 단위)
        /// </summary>
        public bool TryConsume(ActionType action)
        {
            // 행동에 따른 퍼센트 비용 가져오기
            int costPercent = GetPercentCost(action);

            // 배터리 부족 체크
            if (currentBattery < costPercent)
            {
                GameEvents.OnActionBlockedLowBattery?.Invoke(action);
                return false;
            }

            // 배터리 차감
            currentBattery -= costPercent;

            // UI 갱신 (퍼센트 -> 칸 환산)
            SyncCellsFromPercent();
            RaiseChanged();

            // 배터리 소진 이벤트
            if (currentBattery <= 0)
            {
                // 0%가 되면 게임오버 등의 로직 실행
                GameEvents.OnBatteryDepleted?.Invoke();
            }

            return true;
        }

        /// <summary>
        /// 행동별 배터리 소모량 (%) 정의
        /// </summary>
        private int GetPercentCost(ActionType action)
        {
            switch (action)
            {
                // [제출 시 소모되는 배터리]
                case ActionType.SubmitBit: return 20; // 비트
                case ActionType.SubmitByte: return 15; // 바이트
                case ActionType.SubmitWord: return 10; // 워드

                // [힌트 요소]
                case ActionType.OptimizeAlgo: return 20; // 전화

                // [미니게임]
                case ActionType.CleanNoise: return 20;

                default: return 0; // 정의되지 않은 행동은 소모 없음
            }
        }

        /// <summary>
        /// 배터리 회복 (예: 아이템 사용, 광고 보상)
        /// </summary>
        public void Refill(int percentAmount)
        {
            currentBattery = Mathf.Clamp(currentBattery + percentAmount, 0, 100);
            SyncCellsFromPercent();
            RaiseChanged();
        }

        /// <summary>
        /// 배터리를 100%로 완전 회복
        /// </summary>
        public void RefillToMax()
        {
            currentBattery = 100;    // 퍼센트 기준
            SyncCellsFromPercent();  // 칸 환산
            RaiseChanged();          // UI & 이벤트 반영
        }

        /// <summary>외부에서 퍼센트로 직접 세팅(테스트용)</summary>
        public void SetBatteryPercent(int percent)
        {
            currentBattery = Mathf.Clamp(percent, 0, 100);
            SyncCellsFromPercent();
            RaiseChanged();
        }


        /// <summary>
        /// currentBattery(%) 값을 기준으로 칸(CurrentCells)을 다시 계산하여 동기화
        /// - 1%라도 남아 있으면 최소 1칸으로 표시하도록 Ceil 방식 사용
        /// - 항상 0 ~ MaxCells 범위로 클램프
        /// </summary>
        private void SyncCellsFromPercent()
        {
            CurrentCells = Mathf.Clamp(
                Mathf.CeilToInt(maxCells * (currentBattery / 100f)),
                0,
                MaxCells
            );
        }


        /// <summary>
        /// 배터리 상태 변경 이벤트를 호출
        /// - UI 및 시스템 전반에서 CurrentCells, MaxCells, 퍼센트를 업데이트하는 용도로 사용
        /// - GameEvents.OnBatteryChanged(CurrentCells, MaxCells, currentBattery) 호출
        /// </summary>
        private void RaiseChanged()
        {
            GameEvents.OnBatteryChanged?.Invoke(CurrentCells, MaxCells, currentBattery);
        }

        // 앱이 일시정지(홈 화면으로 나감)되거나 종료될 때 저장
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) // 나갈 때
            {
                SaveBatteryState();
            }
            else // 다시 들어올 때 (앱을 완전히 끄지 않고 복귀)
            {
                // 잠시 나갔다 온 시간만큼 또 계산해주고 싶다면 여기서 CheckOfflineRecharge() 호출 가능
                // 여기서는 간단하게 처리하기 위해 생략하거나 재계산 로직 추가
                CheckOfflineRecharge();
            }
        }

        private void OnApplicationQuit()
        {
            SaveBatteryState();
        }

        /// <summary>
        /// 꺼져있던 시간을 계산해서 배터리를 채워주는 로직 (방치형 보상)
        /// </summary>
        private void CheckOfflineRecharge()
        {
            if (!PlayerPrefs.HasKey(KEY_EXIT_TIME)) return;

            // 마지막 종료 시간 불러오기
            string timeStr = PlayerPrefs.GetString(KEY_EXIT_TIME);
            long binaryTime = Convert.ToInt64(timeStr);
            DateTime lastExitTime = DateTime.FromBinary(binaryTime);

            // 시간 차이 계산 (현재 시간 - 마지막 시간)
            TimeSpan timePassed = DateTime.UtcNow - lastExitTime;
            double totalHoursPassed = timePassed.TotalHours;

            // 회복량 계산 (시간 * 시간당 회복량)
            // ex) 2.5시간 지남 * 10 = 25% 회복
            int amountToRecover = (int)(totalHoursPassed * rechargeRatePerHour);

            if (amountToRecover > 0)
            {
                currentBattery = Mathf.Clamp(currentBattery + amountToRecover, 0, 100);
                SyncCellsFromPercent(); // UI 갱신을 위해 호출

                Debug.Log($"[Battery] 부재중 {timePassed.TotalMinutes:F1}분 경과. {amountToRecover}% 회복됨.");

                // UIManager를 통해 팝업 호출 (Start에서 호출하므로 안전함)
                UIManager.Instance.Show(
                    $"푹 쉬고 오셨군요!\n휴식하는 동안 배터리가 <color=green>{amountToRecover}%</color> 충전되었습니다."
                );
            }
        }

        /// <summary>
        /// 게임 플레이 중 실시간으로 배터리가 차오르는 코루틴
        /// </summary>
        private IEnumerator RuntimeRechargeRoutine()
        {
            while (true)
            {
                // 배터리가 꽉 찼으면 대기
                if (currentBattery >= 100)
                {
                    yield return null;
                    continue;
                }

                // 1시간에 10% -> 1% 차는데 걸리는 시간 계산
                // 10% = 3600초(1시간)
                // 1% = 360초 (6분)
                float secondsForOnePercent = 3600f / rechargeRatePerHour;

                // 해당 시간만큼 대기
                yield return new WaitForSeconds(secondsForOnePercent);

                // 1% 증가
                Refill(1);
            }
        }

        // ---------------- 데이터 저장/로드 ---------------- //
        private void SaveBatteryState()
        {
            // 현재 시간 저장 (UTC 기준 권장)
            PlayerPrefs.SetString(KEY_EXIT_TIME, DateTime.UtcNow.ToBinary().ToString());
            // 현재 배터리량도 저장해야 정확함 (아니면 켤 때마다 100%나 초기값으로 될 수 있음)
            PlayerPrefs.SetInt(KEY_SAVED_BATTERY, currentBattery);
            PlayerPrefs.Save();
        }

        private void LoadBatteryState()
        {
            if (PlayerPrefs.HasKey(KEY_SAVED_BATTERY))
            {
                currentBattery = PlayerPrefs.GetInt(KEY_SAVED_BATTERY);
            }
        }
    }

}
