using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WordEater.Core;

namespace WordEater.Systems
{
    /// <summary>
    /// 배터리 시스템: 칸 단위 표시, 자동 회복, 오프라인 보상, 광고 충전 등을 관리함
    /// </summary>
    public class BatterySystem : MonoBehaviour
    {
        #region [설정 및 연결]
        [Header("테스트/실사용 겸용: 0~100%")]
        [SerializeField, Range(0, 100)]
        private int currentBattery = 100; // 현재 배터리 퍼센트 (0~100)

        [Header("총 배터리 칸 수")]
        [SerializeField] private int maxCells = 5; // UI에 표시될 최대 배터리 칸 개수

        [Header("광고 충전 팝업")]
        [SerializeField] private ADPopup batteryPopup; // 배터리 부족 시 광고 충전 유도 팝업

        [Header("자동 회복 설정")]
        [SerializeField] private bool enableAutoRecharge = true; // 게임 중 자동 회복 활성화 여부
        [SerializeField] private int rechargeRatePerHour = 10;   // 시간당 회복량 (%)
        #endregion

        #region [프로퍼티]
        /// <summary> 최대 배터리 칸 수 </summary>
        public int MaxCells => maxCells;
        /// <summary> 현재 배터리 칸 수 (UI 표시용) </summary>
        public int CurrentCells { get; private set; }
        /// <summary> 현재 배터리 잔량 (%) </summary>
        public int CurrentPercent => currentBattery;
        #endregion

        // 내부 상태 변수 (파일 매니저와 동기화됨)
        private bool isFirstRun = true;

        private void Start()
        {
            // 1. FileManager에서 저장된 배터리 데이터 로드
            LoadBatteryState();

            // 2. 퍼센트 기반으로 칸 개수(CurrentCells) 계산 및 UI 동기화
            SyncCellsFromPercent();

            // 3. 첫 실행 여부 확인 및 오프라인 보상 로직
            if (isFirstRun)
            {
                // 첫 실행 → 오프라인 보상 없음, 플래그 끄고 저장
                isFirstRun = false;
                SaveBatteryState();
            }
            else
            {
                // 재방문 → 오프라인(꺼져있던 시간) 동안의 회복량 계산
                CheckOfflineRecharge();
            }

            // 초기 상태 알림
            RaiseChanged();

            // 자동 회복 코루틴 시작
            if (enableAutoRecharge)
                StartCoroutine(RuntimeRechargeRoutine());
        }

        private void OnValidate()
        {
            // 인스펙터 값 변경 시 유효성 검사 및 UI 갱신
            maxCells = Mathf.Max(1, maxCells);
            currentBattery = Mathf.Clamp(currentBattery, 0, 100);
            SyncCellsFromPercent();
            RaiseChanged();
        }

        #region [Public API]

        /// <summary>
        /// 광고 시청 후 배터리 충전 팝업을 띄움
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

            // 광고 충전 팝업 표시 (수락 시 RefillToMax 실행)
            batteryPopup.YesNoPanelShow(
                onAccept: () => RefillToMax(),
                onDecline: () => Debug.Log("[Battery] 광고 충전 거절")
            );
        }

        /// <summary>
        /// 특정 행동에 필요한 배터리를 소모함
        /// </summary>
        /// <param name="action">수행할 행동 타입</param>
        /// <returns>소모 성공 여부 (배터리 부족 시 false)</returns>
        public bool TryConsume(ActionType action)
        {
            int costPercent = GetPercentCost(action);

            // 잔량 부족 체크
            if (currentBattery < costPercent)
            {
                GameEvents.OnActionBlockedLowBattery?.Invoke(action);
                return false;
            }

            // 소모 처리
            currentBattery -= costPercent;
            SyncCellsFromPercent();
            RaiseChanged();

            // 방전 이벤트 알림
            if (currentBattery <= 0)
            {
                GameEvents.OnBatteryDepleted?.Invoke();
            }

            return true;
        }

        /// <summary>
        /// 배터리를 강제로 0으로 만듦 (강제 제출 시 사망 확정용)
        /// </summary>
        public void ForceEmpty()
        {
            currentBattery = 0;
            SyncCellsFromPercent();
            RaiseChanged();

            // 방전 이벤트 발생
            GameEvents.OnBatteryDepleted?.Invoke();
            Debug.Log("[Battery] 강제 방전(ForceEmpty) 실행됨");
        }

        /// <summary>
        /// 배터리를 지정된 양만큼 충전함
        /// </summary>
        public void Refill(int percentAmount)
        {
            currentBattery = Mathf.Clamp(currentBattery + percentAmount, 0, 100);
            SyncCellsFromPercent();
            RaiseChanged();
        }

        /// <summary>
        /// 배터리를 100%까지 완전 충전함
        /// </summary>
        public void RefillToMax()
        {
            currentBattery = 100;
            SyncCellsFromPercent();
            RaiseChanged();
        }

        /// <summary>
        /// 배터리 퍼센트를 직접 설정함 (디버그용)
        /// </summary>
        public void SetBatteryPercent(int percent)
        {
            currentBattery = Mathf.Clamp(percent, 0, 100);
            SyncCellsFromPercent();
            RaiseChanged();
        }

        #endregion

        #region [Internal Logic]

        /// <summary>
        /// 행동별 배터리 소모 비용(%)을 반환함
        /// </summary>
        private int GetPercentCost(ActionType action)
        {
            switch (action)
            {
                case ActionType.SubmitBit: return 20;
                case ActionType.SubmitByte: return 15;
                case ActionType.SubmitWord: return 10;
                case ActionType.OptimizeAlgoCall: return 20;
                case ActionType.OptimizeAlgoMessage: return 15;
                case ActionType.CleanNoise: return 20;
                case ActionType.MinigameStart: return 20;
                default: return 0;
            }
        }

        /// <summary>
        /// 현재 퍼센트(0~100)를 기반으로 UI 칸 개수(0~MaxCells)를 계산함
        /// </summary>
        private void SyncCellsFromPercent()
        {
            CurrentCells = Mathf.Clamp(
                Mathf.CeilToInt(maxCells * (currentBattery / 100f)),
                0, MaxCells
            );
        }

        /// <summary>
        /// 배터리 상태 변경 이벤트를 발생시켜 UI 등을 갱신함
        /// </summary>
        private void RaiseChanged()
        {
            GameEvents.OnBatteryChanged?.Invoke(CurrentCells, MaxCells, currentBattery);
        }

        #endregion

        #region [App Lifecycle & Save/Load]

        /// <summary>
        /// 앱이 일시정지되거나 복귀할 때 호출됨
        /// </summary>
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) // 나갈 때: 데이터 저장
            {
                SaveBatteryState();
            }
            else // 들어올 때: 오프라인 보상 계산
            {
                CheckOfflineRecharge();
            }
        }

        /// <summary>
        /// 앱 종료 시 데이터 저장
        /// </summary>
        private void OnApplicationQuit()
        {
            SaveBatteryState();
        }

        private void SaveBatteryState()
        {
            if (FileManager.Instance == null) return;

            // 현재 시간 (Binary String) 저장
            string currentTime = DateTime.UtcNow.ToBinary().ToString();

            // FileManager에게 저장 요청
            FileManager.Instance.SaveBatteryInfo(currentBattery, currentTime, isFirstRun);
        }

        private void LoadBatteryState()
        {
            if (FileManager.Instance == null) return;

            var data = FileManager.Instance.batteryData;

            this.currentBattery = data.SavedBattery;
            this.isFirstRun = data.IsFirstRun;
        }

        #endregion

        #region [Offline Recharge]

        /// <summary>
        /// 게임이 꺼져있던 시간을 계산하여 배터리 회복 및 보상 아이템 지급
        /// </summary>
        private void CheckOfflineRecharge()
        {
            if (FileManager.Instance == null) return;

            string timeStr = FileManager.Instance.batteryData.ExitTime;

            // 저장된 시간이 없으면 패스
            if (string.IsNullOrEmpty(timeStr)) return;

            long binaryTime = Convert.ToInt64(timeStr);
            DateTime lastExitTime = DateTime.FromBinary(binaryTime);
            TimeSpan timePassed = DateTime.UtcNow - lastExitTime;

            // 회복량 계산
            double totalHoursPassed = timePassed.TotalHours;
            int amountToRecover = (int)(totalHoursPassed * rechargeRatePerHour);

            if (amountToRecover <= 0) return;

            // 배터리 갱신 적용
            int beforeBattery = currentBattery;
            currentBattery = Mathf.Clamp(currentBattery + amountToRecover, 0, 100);
            SyncCellsFromPercent();

            int actualRecovered = currentBattery - beforeBattery;
            Debug.Log($"[Battery] 부재중 {timePassed.TotalMinutes:F1}분 경과. {actualRecovered}% 회복.");

            // 메시지 및 아이템 처리
            string finalMessage = "";
            bool itemAcquired = false;
            ItemType acquiredItemType = ItemType.BatteryRefill;

            // 10초 이상 자리를 비웠을 때만 아이템 획득 시도 (테스트용 짧은 시간)
            if (timePassed.TotalSeconds >= 10.0f)
            {
                acquiredItemType = ItemDropManager.Instance.ObtainRandomItem(showUI: false);
                itemAcquired = true;
            }

            if (itemAcquired)
            {
                string itemName = acquiredItemType switch
                {
                    ItemType.BatteryRefill => "배터리 채우기",
                    ItemType.HintChosung => "초성 힌트권",
                    ItemType.FillKeyCounts => "자음/모음 채우기",
                    ItemType.ReviveTicket => "워드이터 1회 부활권",
                    _ => "알 수 없는 아이템"
                };

                if (currentBattery >= 100)
                    finalMessage = $"푹 쉬고 오셨군요!\n배터리 완충 + 워드이터가 <color=yellow>[{itemName}]</color> 1개를 물어왔습니다!";
                else
                    finalMessage = $"푹 쉬고 오셨군요!\n배터리 {actualRecovered}% 충전 + 워드이터가 <color=yellow>[{itemName}]</color> 1개를 물어왔습니다!";
            }
            else
            {
                if (currentBattery >= 100)
                    finalMessage = "푹 쉬고 오셨군요!\n휴식하는 동안 배터리가 모두 충전되었습니다.";
                else
                    finalMessage = $"푹 쉬고 오셨군요!\n휴식하는 동안 배터리가 <color=green>{actualRecovered}%</color> 충전되었습니다.";
            }

            if (!string.IsNullOrEmpty(finalMessage))
            {
                UIManager.Instance.Show(finalMessage);
            }
        }

        #endregion

        /// <summary>
        /// 런타임 중 일정 시간마다 배터리를 1%씩 회복하는 코루틴
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

                // 1% 회복에 걸리는 시간 계산
                float secondsForOnePercent = 3600f / rechargeRatePerHour;
                yield return new WaitForSeconds(secondsForOnePercent);
                Refill(1);
            }
        }
    }
}