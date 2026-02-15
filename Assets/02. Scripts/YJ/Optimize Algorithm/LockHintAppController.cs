using UnityEngine;
using WordEater.Core;
using WordEater.Systems;

public class LockHintAppController : MonoBehaviour
{
    [SerializeField] private BatterySystem battery;          // 배터리 시스템 연결
    [SerializeField] private AlgorithmLock lockUI;           // UI 스크립트 연결
    [SerializeField] private WordEater.Core.WordEater wordEater; // 정답 들고 있는 메인 객체
    [SerializeField] private GameManager gamemanager;

    [Range(0f, 1f)][SerializeField] private float chosungChance = 0.5f; // 초성 힌트 뜰 확률

    // 마지막으로 힌트를 확인한 단어를 저장
    private string lastAnswer = "";
    /// <summary>
    /// GameManager나 버튼 이벤트에서 호출하는 진입점 함수
    /// </summary>
    public void OpenLockHint()
    {
        if (battery == null || lockUI == null) return;

        string currentAnswer = (wordEater != null) ? wordEater.Answer : "";

        // 중복 확인 로직
        if (!string.IsNullOrEmpty(lastAnswer) && lastAnswer == currentAnswer)
        {
            UIManager.Instance.Show("힌트를 확인했습니다.\n히스토리에서 다시 볼 수 있습니다.");
            return;
        }

        if (GameManager.Instance.sharedAdPopup != null)
        {
            // [핵심 추가] 광고 모드를 끕니다. (그래야 광고 없이 바로 ExecuteLockHint가 실행됨)
            GameManager.Instance.sharedAdPopup.SetAdMode(false);

            GameManager.Instance.sharedAdPopup.Configure(
                title: "배터리가 10% 소모됩니다.\n글자 수 힌트를 보시겠습니까?",
                watchAdText: "힌트 보기",
                noThanksText: "취소"
            );

            GameManager.Instance.sharedAdPopup.YesNoPanelShow(
                onAccept: () =>
                {
                    ExecuteLockHint(currentAnswer);
                },
                onDecline: () =>
                {
                    Debug.Log("취소됨");
                }
            );
        }
    }

    private void ExecuteLockHint(string currentAnswer)
    {
        // 배터리 소모 체크
        if (!battery.TryConsume(ActionType.OptimizeLock))
        {
            battery.ShowBatteryAdPopup(); // 배터리 부족 시 광고 유도 팝업
            return;
        }

        // 힌트 데이터 생성
        LockHintMode mode = DecideMode(currentAnswer);
        lockUI.ShowHint(currentAnswer, mode);
        lastAnswer = currentAnswer;

        // 세이브 데이터 기록
        if (mode == LockHintMode.LengthOnly) gamemanager.saveLock(0);
        else if (mode == LockHintMode.FirstChosung) gamemanager.saveLock(1);
        else gamemanager.saveLock(2);

        gamemanager.saveCountInmanager(3);

        // 힌트 생성이 완료되었으므로 GameManager에게 패널 오픈 명령
        GameManager.Instance.OpenLockPanelUI();
    }

    private LockHintMode DecideMode(string answer)
    {
        if (string.IsNullOrEmpty(answer) || answer.Length <= 1)
            return LockHintMode.LengthOnly;

        bool giveCho = Random.value < chosungChance;
        if (!giveCho) return LockHintMode.LengthOnly;

        var mode = (Random.value < 0.5f) ? LockHintMode.FirstChosung : LockHintMode.LastChosung;

        if (answer.Length == 2 && mode == LockHintMode.LastChosung)
            mode = LockHintMode.FirstChosung;

        return mode;
    }
}