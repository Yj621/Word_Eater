using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WordEater.Core;

namespace WordEater.Systems
{
    /// <summary>
    /// 칸 단위 배터리 시스템 (FileManager 통합 버전)
    /// </summary>
    public class BatterySystem : MonoBehaviour
    {
        [Header("테스트/실사용 겸용: 0~100%")]
        [SerializeField, Range(0, 100)]
        private int currentBattery = 100;

        [Header("총 배터리 칸 수")]
        [SerializeField] private int maxCells = 5;

        [Header("광고 충전 팝업")]
        [SerializeField] private ADPopup batteryPopup;

        [Header("자동 회복 설정")]
        [SerializeField] private bool enableAutoRecharge = true;
        [SerializeField] private int rechargeRatePerHour = 10;

        public int MaxCells => maxCells;
        public int CurrentCells { get; private set; }
        public int CurrentPercent => currentBattery;

        // 내부 상태 변수 (파일 매니저와 동기화됨)
        private bool isFirstRun = true;

        private void Start()
        {
            // 1. FileManager에서 데이터 로드
            LoadBatteryState();

            // 2. 칸 개수 UI 동기화
            SyncCellsFromPercent();

            // 3. 첫 실행 여부 확인 로직
            if (isFirstRun)
            {
                // 첫 실행 → 오프라인 보상 없음, 플래그 끄고 저장
                isFirstRun = false;
                SaveBatteryState();
            }
            else
            {
                // 재방문 → 오프라인 보상 계산
                CheckOfflineRecharge();
            }

            RaiseChanged();

            if (enableAutoRecharge)
                StartCoroutine(RuntimeRechargeRoutine());
        }

        private void OnValidate()
        {
            maxCells = Mathf.Max(1, maxCells);
            currentBattery = Mathf.Clamp(currentBattery, 0, 100);
            SyncCellsFromPercent();
            RaiseChanged();
        }

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
                onAccept: () => RefillToMax(),
                onDecline: () => Debug.Log("[Battery] 광고 충전 거절")
            );
        }

        public bool TryConsume(ActionType action)
        {
            int costPercent = GetPercentCost(action);

            if (currentBattery < costPercent)
            {
                GameEvents.OnActionBlockedLowBattery?.Invoke(action);
                return false;
            }

            currentBattery -= costPercent;
            SyncCellsFromPercent();
            RaiseChanged();

            if (currentBattery <= 0)
            {
                GameEvents.OnBatteryDepleted?.Invoke();
            }

            return true;
        }

        private int GetPercentCost(ActionType action)
        {
            switch (action)
            {
                case ActionType.SubmitBit: return 20;
                case ActionType.SubmitByte: return 15;
                case ActionType.SubmitWord: return 10;
                case ActionType.OptimizeAlgoCall: return 15;
                case ActionType.OptimizeAlgoMessage: return 10;
                case ActionType.CleanNoise: return 20;
                case ActionType.MinigameStart: return 20;
                default: return 0;
            }
        }

        public void Refill(int percentAmount)
        {
            currentBattery = Mathf.Clamp(currentBattery + percentAmount, 0, 100);
            SyncCellsFromPercent();
            RaiseChanged();
        }

        public void RefillToMax()
        {
            currentBattery = 100;
            SyncCellsFromPercent();
            RaiseChanged();
        }

        public void SetBatteryPercent(int percent)
        {
            currentBattery = Mathf.Clamp(percent, 0, 100);
            SyncCellsFromPercent();
            RaiseChanged();
        }

        private void SyncCellsFromPercent()
        {
            CurrentCells = Mathf.Clamp(
                Mathf.CeilToInt(maxCells * (currentBattery / 100f)),
                0, MaxCells
            );
        }

        private void RaiseChanged()
        {
            GameEvents.OnBatteryChanged?.Invoke(CurrentCells, MaxCells, currentBattery);
        }

        // ---------------- 앱 생명주기 저장 로직 ---------------- //

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) // 나갈 때: 저장
            {
                SaveBatteryState();
            }
            else // 들어올 때: 오프라인 보상 계산
            {
                // 잠시 나갔다 온 경우 데이터 다시 로드 후 보상 계산
                // (FileManager 데이터가 이미 최신이겠지만, 시간 계산을 위해 로직 수행)
                CheckOfflineRecharge();
            }
        }

        private void OnApplicationQuit()
        {
            SaveBatteryState();
        }

        // ---------------- FileManager 연동 로직 ---------------- //

        private void SaveBatteryState()
        {
            if (FileManager.Instance == null) return;

            // 현재 시간 (Binary String)
            string currentTime = DateTime.UtcNow.ToBinary().ToString();

            // FileManager에게 저장 요청
            FileManager.Instance.SaveBatteryInfo(currentBattery, currentTime, isFirstRun);
        }

        private void LoadBatteryState()
        {
            if (FileManager.Instance == null) return;

            // FileManager의 메모리 데이터 가져오기
            var data = FileManager.Instance.batteryData;

            this.currentBattery = data.SavedBattery;
            this.isFirstRun = data.IsFirstRun;
        }

        // ---------------- 오프라인 보상 로직 ---------------- //

        private void CheckOfflineRecharge()
        {
            if (FileManager.Instance == null) return;

            string timeStr = FileManager.Instance.batteryData.ExitTime;

            // 저장된 시간이 없으면 패스
            if (string.IsNullOrEmpty(timeStr)) return;

            long binaryTime = Convert.ToInt64(timeStr);
            DateTime lastExitTime = DateTime.FromBinary(binaryTime);
            TimeSpan timePassed = DateTime.UtcNow - lastExitTime;

            double totalHoursPassed = timePassed.TotalHours;
            int amountToRecover = (int)(totalHoursPassed * rechargeRatePerHour);

            if (amountToRecover <= 0) return;

            // 배터리 갱신
            int beforeBattery = currentBattery;
            currentBattery = Mathf.Clamp(currentBattery + amountToRecover, 0, 100);
            SyncCellsFromPercent();

            int actualRecovered = currentBattery - beforeBattery;
            Debug.Log($"[Battery] 부재중 {timePassed.TotalMinutes:F1}분 경과. {actualRecovered}% 회복.");

            // 메시지 및 아이템 처리
            string finalMessage = "";
            bool itemAcquired = false;
            ItemType acquiredItemType = ItemType.BatteryRefill;

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

        private IEnumerator RuntimeRechargeRoutine()
        {
            while (true)
            {
                if (currentBattery >= 100)
                {
                    yield return null;
                    continue;
                }

                float secondsForOnePercent = 3600f / rechargeRatePerHour;
                yield return new WaitForSeconds(secondsForOnePercent);
                Refill(1);
            }
        }
    }
}