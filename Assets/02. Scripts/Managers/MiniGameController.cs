using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MiniGameController : MonoBehaviour
{
    [Header("게임 목록(패널 또는 프리팹)")]
    [SerializeField] private GameObject[] minigames;

    [Header("타이머 UI (Slider)")]
    [SerializeField] private Slider timer;

    // 외부 참조
    private AlgorithmPanel algorithmPanel;

    // 내부 상태
    private int _currentIndex = -1;
    private Coroutine _timerCo;
    private bool _running;
    public float _timeLimitEasy = 5f;
    public float _timeLimitHard = 3f;

    public int ClearCount = 0;

    private void Awake()
    {
        algorithmPanel = GetComponentInParent<AlgorithmPanel>();
        if (minigames != null)
        {
            foreach (var g in minigames) if (g) g.SetActive(false);
        }
        if (timer) { timer.gameObject.SetActive(true); timer.value = 0f; }
    }

    private void Update()
    {
        Debug.Log(ClearCount);
    }

    /// <summary>
    /// AlgorithmPanel에서 GameTab이 열릴 때 호출해주면 됨.
    /// </summary>
    public void Begin()
    {
        if (_running) return;
        _running = true;

        // 모드에 따라 타이머 세팅
        float limit = algorithmPanel != null && algorithmPanel.Mode ? _timeLimitEasy : _timeLimitHard;
        SetupTimer(limit);

        // 첫 게임 시작
        StartRandomGame(skipIndex: -1);
    }

    /// <summary>
    /// AlgorithmPanel에서 탭 닫을 때/실패할 때 호출
    /// </summary>
    public void StopAllGames()
    {
        _running = false;
        if (_timerCo != null) { StopCoroutine(_timerCo); _timerCo = null; }
        if (minigames != null)
        {
            foreach (var g in minigames) if (g) g.SetActive(false);
        }
    }

    // === 미니게임들에서 호출할 API ===
    public void NotifyClear()
    {
        if (!_running) return;
        // 다음 게임으로 즉시 진행
        ClearCount++;
        float limit = algorithmPanel != null && algorithmPanel.Mode ? _timeLimitEasy : _timeLimitHard;
        SetupTimer(limit);
        StartRandomGame(skipIndex: _currentIndex);
    }

    public void NotifyFail()
    {
        if (!_running) return;
        // 실패 처리: 탭 닫기
        FailAndClose();
    }

    // === 내부 구현 ===
    private void StartRandomGame(int skipIndex)
    {
        // 전부 끄기
        if (minigames != null)
        {
            foreach (var g in minigames) if (g) g.SetActive(false);
        }

        if (minigames == null || minigames.Length == 0)
        {
            Debug.LogWarning("[MiniGameController] 등록된 미니게임이 없음");
            FailAndClose();
            return;
        }

        // 랜덤 인덱스 (직전과 다르게)
        int next = Random.Range(0, minigames.Length);
        if (minigames.Length > 1)
        {
            int guard = 0;
            while (next == skipIndex && guard++ < 8)
                next = Random.Range(0, minigames.Length);
        }
        _currentIndex = next;

        var go = minigames[_currentIndex];
        go.SetActive(true);

        // 미니게임에 컨트롤러 참조를 넘겨서 클리어/실패 알리게 하기
        // (MiniGameHook를 해당 미니게임 루트에 붙여줘)
        var hook = go.GetComponent<MiniGameHook>();
        if (hook != null) hook.Bind(this);
    }

    private void SetupTimer(float limit)
    {
        if (timer == null) return;

        timer.maxValue = limit;
        timer.value = limit;

        if (_timerCo != null) StopCoroutine(_timerCo);
        _timerCo = StartCoroutine(CoTimer());
    }

    private IEnumerator CoTimer()
    {
        while (timer.value > 0f && _running)
        {
            timer.value -= Time.deltaTime;
            yield return null;
        }

        _timerCo = null;
        if (!_running) yield break;
        if (timer.value <= 0f)
        {
            // 시간초과 → 실패
            FailAndClose();
        }
    }

    private void FailAndClose()
    {
        StopAllGames();
        if (algorithmPanel != null)
        {
            // 패널 쪽 애니메이션/상태는 기존 함수 그대로 사용
            algorithmPanel.StartCoroutine(algorithmPanel.CloasePageTab());
        }
    }
}
