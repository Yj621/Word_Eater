using System;
using UnityEngine;
using WordEater.Core;

[Serializable]
public class WordEaterCheckpoint
{
    public Vector3 Position;
    public int BatteryPercent;
    public int TurnsLeft;
    public int MistakesLeft;
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
            TurnsLeft = we.GetTurnsLeft(),         //
            MistakesLeft = we.GetMistakesLeft(),
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

        // 게임 정지 (UI는 UnscaledTime 기준)
        Time.timeScale = 0f;

        // 부활
        revivePopup.Configure(
            title: "광고 보고 부활하기",
            watchAdText: "광고 보기",
            noThanksText: "아니오"
        );

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
        if (_cp == null) { Debug.LogWarning("[Revive] 체크포인트 없음"); return; }

        // 위치/배터리 복원
        player.transform.position = _cp.Position;
        var battery = player.GetComponent<WordEater.Systems.BatterySystem>()
                     ?? FindFirstObjectByType<WordEater.Systems.BatterySystem>();
        if (battery != null) battery.SetBatteryPercent(Mathf.Max(_cp.BatteryPercent, 50));

        // 턴/오답/정답/단계 복원
        player.RestoreTurns(_cp.TurnsLeft, _cp.MistakesLeft);
        player.RestoreAnswer(_cp.CurrentAnswer, _cp.Stage);

        // 최종 활성화
        player.Reactivate();

        Debug.Log("[Revive] 체크포인트로 완전 복원 완료");
    }
}
