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

/// <summary>
/// 게임 오버 시 부활 및 체크포인트 기능을 관리하는 시스템임
/// </summary>
public class GameReviveSystem : MonoBehaviour
{
    public static GameReviveSystem Instance { get; private set; }
    [SerializeField] private ADPopup revivePopup; // 광고 팝업

    private WordEaterCheckpoint _cp; // 저장된 체크포인트
    private bool _reviveOffered;     // 부활 제안 중인지 여부

    void Awake() => Instance = this;

    /// <summary>
    /// 현재 워드이터의 상태를 체크포인트로 저장함
    /// </summary>
    public void SaveCheckpoint(WordEater.Core.WordEater we, int batteryPercent)
    {
        _cp = new WordEaterCheckpoint
        {
            Position = we.transform.position,
            BatteryPercent = Mathf.Clamp(batteryPercent, 0, 100),
            Stage = we.CurrentStage,
            CurrentAnswer = we.Answer
        };
    }

    /// <summary>
    /// 플레이어 사망 시 호출되어 광고 부활 팝업을 띄움
    /// </summary>
    public void OnPlayerDied(Action onGiveUp)
    {
        if (_reviveOffered) return;
        _reviveOffered = true;

        if (revivePopup == null)
        {
            _reviveOffered = false;
            onGiveUp?.Invoke();
            return;
        }

        // 팝업 동안 게임 시간을 정지시킴
        Time.timeScale = 0f;

        revivePopup.Configure(
            title: "배터리 방전!",
            watchAdText: "충전하고 계속하기",
            noThanksText: "아니오"
        );

        revivePopup.Show(
            onAccept: () =>
            {
                // 광고 시청 완료 시 부활함
                ReviveFromCheckpoint();
                _reviveOffered = false;
                Time.timeScale = 1f; // 시간 재개함
            },
            onDecline: () =>
            {
                // 거절 시 게임오버 콜백 호출함
                _reviveOffered = false;
                Time.timeScale = 1f;
                onGiveUp?.Invoke();
            }
        );
    }

    /// <summary>
    /// 저장된 체크포인트 데이터를 기반으로 플레이어를 부활시킴
    /// </summary>
    public void ReviveFromCheckpoint()
    {
        var player = FindFirstObjectByType<WordEater.Core.WordEater>();
        if (player == null) return;

        // 체크포인트가 없으면 배터리만 채우고 제자리 부활함
        if (_cp == null)
        {
            var bat = player.GetComponent<BatterySystem>() ?? FindFirstObjectByType<BatterySystem>();
            if (bat != null) bat.RefillToMax();
            player.RevivePlayer();
            return;
        }

        // 위치 및 상태 복구함
        player.transform.position = _cp.Position;

        var battery = player.GetComponent<BatterySystem>() ?? FindFirstObjectByType<BatterySystem>();
        if (battery != null) battery.RefillToMax();

        player.RestoreAnswer(_cp.CurrentAnswer, _cp.Stage);

        // 플레이어 다시 활성화함
        player.RevivePlayer();
    }
}