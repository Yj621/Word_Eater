using System;
using UnityEngine;
using WordEater.Core;
using WordEater.Systems;

[Serializable]
public class WordEaterCheckpoint
{
    public Vector3 Position;
    public int BatteryPercent;
    public GrowthStage Stage;
    public string CurrentAnswer;
}

public class GameReviveSystem : MonoBehaviour
{
    public static GameReviveSystem Instance { get; private set; }
    [SerializeField] private ADPopup revivePopup;

    private WordEaterCheckpoint _cp;
    private bool _reviveOffered;

    void Awake() => Instance = this;

    public void SaveCheckpoint(WordEater.Core.WordEater we, int batteryPercent)
    {
        _cp = new WordEaterCheckpoint
        {
            Position = we.transform.position,
            BatteryPercent = Mathf.Clamp(batteryPercent, 0, 100),
            Stage = we.ReturnStage(),
            CurrentAnswer = we.CurrentAnswer
        };
    }
    public void OnPlayerDied(Action onGiveUp)
    {
        if (_reviveOffered) return;
        _reviveOffered = true;

        if (revivePopup == null)
        {
            Debug.LogWarning("[Revive] revivePopup 미할당");
            _reviveOffered = false;
            onGiveUp?.Invoke();
            return;
        }


        // 부활
        revivePopup.Configure(
                    title: "배터리 방전!",
                    watchAdText: "충전하고 계속하기", // 텍스트 수정: 광고 보기 -> 충전하기
                    noThanksText: "아니오"
                );

        // 게임 정지 (UI는 UnscaledTime 기준)
        Time.timeScale = 0f;

        revivePopup.Show(
            onAccept: () =>
            {
                ReviveFromCheckpoint();
                Debug.Log("[WordEater] 턴 고갈 → 부활");
                _reviveOffered = false;
                Time.timeScale = 1f;  // 재개
            },
            onDecline: () =>
            {
                _reviveOffered = false;
                Time.timeScale = 1f;  // 재개
                onGiveUp?.Invoke();   // → gamemanager.EndingController(1)
            }
        );
    }

    public void ReviveFromCheckpoint()
    {
        var player = FindFirstObjectByType<WordEater.Core.WordEater>();
        if (player == null) { Debug.LogWarning("[Revive] WordEater 없음"); return; }

        // 체크포인트가 혹시 없더라도 부활은 시켜줘야 하므로 방어 코드
        if (_cp == null)
        {
            Debug.LogWarning("[Revive] 체크포인트 없음, 현재 상태에서 배터리만 채움");
            var bat = player.GetComponent<BatterySystem>() ?? FindFirstObjectByType<BatterySystem>();
            if (bat != null) bat.RefillToMax(); // 그냥 풀충전
            player.Reactivate();
            return;
        }

        // 위치 복원
        player.transform.position = _cp.Position;

        // 배터리 복원 (부활이니까 무조건 100%로 채워주는 게 일반적임, 혹은 저장된 값 + 보너스)
        var battery = player.GetComponent<BatterySystem>() ?? FindFirstObjectByType<BatterySystem>();
        if (battery != null)
        {
            // 부활 혜택: 죽기 직전 배터리가 아니라, 꽉 채워서 부활시켜줌
            battery.RefillToMax();
        }

        // 턴/오답 복원 로직 삭제함 (player.RestoreTurns 삭제)

        // 정답/단계 복원
        player.RestoreAnswer(_cp.CurrentAnswer, _cp.Stage);

        // 최종 활성화 (isDead = false)
        player.Reactivate();

        Debug.Log("[Revive] 체크포인트 기반 부활 완료");
    }
}