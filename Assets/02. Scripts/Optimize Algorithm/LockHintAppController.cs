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

    /// <summary>
    /// GameManager나 버튼 이벤트에서 호출하는 진입점 함수
    /// </summary>
    public void OpenLockHint()
    {
        // 필수 컴포넌트 연결 안 돼 있으면 에러 로그 띄우고 중단함
        if (battery == null || lockUI == null)
        {
            Debug.LogWarning("[LockHint] battery 또는 lockUI 미할당됨");
            return;
        }

        // 배터리 소모 시도함 (성공 시 true 반환)
        // OptimizeLock 타입은 보통 배터리 10% 소모함
        if (!battery.TryConsume(ActionType.OptimizeLock))
        {
            // 배터리 없으면 광고 팝업 띄우고 끝냄
            battery.ShowBatteryAdPopup();
            return;
        }

        // 워드이터한테서 정답 단어 가져옴. 없으면 "?" 처리함
        string answer = (wordEater != null) ? wordEater.Answer : "";
        if (string.IsNullOrEmpty(answer)) answer = "?";

        // 이번에 어떤 힌트(길이만 vs 초성)를 줄지 결정함
        LockHintMode mode = DecideMode(answer);

        // UI에 최종적으로 힌트 표시 요청함
        lockUI.ShowHint(answer, mode);

        if (mode == LockHintMode.LengthOnly) gamemanager.saveLock(0);
        else if (mode == LockHintMode.FirstChosung) gamemanager.saveLock(1);
        else gamemanager.saveLock(2);

        gamemanager.saveCountInmanager(3);
    }

    /// <summary>
    /// 힌트 모드를 확률적으로 결정하는 함수
    /// </summary>
    private LockHintMode DecideMode(string answer)
    {
        // 정답 없거나 1글자면 무조건 길이만 보여줌 (1글자 초성은 너무 쉬움)
        if (string.IsNullOrEmpty(answer) || answer.Length <= 1)
            return LockHintMode.LengthOnly;

        // 설정한 확률(chosungChance)에 따라 초성 보여줄지 결정함
        bool giveCho = Random.value < chosungChance;

        // 꽝이면 길이만 보여줌
        if (!giveCho) return LockHintMode.LengthOnly;

        // 초성 보여주기로 했으면 앞/뒤 중 하나 50:50으로 고름
        var mode = (Random.value < 0.5f) ? LockHintMode.FirstChosung : LockHintMode.LastChosung;

        // 예외 처리: 2글자 단어인데 뒤 초성 알려주면 추론이 너무 쉬울 수 있어서 앞 초성으로 강제함(취향 차이임)
        if (answer.Length == 2 && mode == LockHintMode.LastChosung)
            mode = LockHintMode.FirstChosung;

        return mode;
    }
}