using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using WordEater.Core;
using WordEater.Systems;

public class MiniGameController : MonoBehaviour
{
    [Header("게임 목록(패널 또는 프리팹)")]
    [SerializeField] private GameObject[] minigames;
    [SerializeField] public KeyBoardManager keyboard;

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

    public WordEater.Core.WordEater wordeater;

    private void Awake()
    {
        algorithmPanel = GetComponentInParent<AlgorithmPanel>();
        if (minigames != null)
        {
            foreach (var g in minigames) if (g) g.SetActive(false);
        }
        if (timer) { timer.gameObject.SetActive(true); timer.value = 0f; }
    }

    public void Begin()
    {
        if (_running) return;

        // [Legacy] 기존 호출 대응 (바로 결제 후 시작)
        CheckPayment(() => StartGame());
    }

    // [New] 결제만 먼저 시도 (패널 열기 전에 호출)
    public void CheckPayment(System.Action onPaid)
    {
        if (wordeater != null)
        {
            wordeater.TryPayForMiniGame(() =>
            {
                onPaid?.Invoke();
            });
        }
        else
        {
            // 워드이터 없으면 프리패스
            onPaid?.Invoke();
        }
    }

    // [New] 실제 게임 시작 (패널 열린 후 호출)
    public void StartGame()
    {
        _running = true;

        // 모드에 따라 타이머 세팅
        float limit = algorithmPanel != null && algorithmPanel.Mode ? _timeLimitEasy : _timeLimitHard;
        SetupTimer(limit);

        // 첫 게임 시작
        StartRandomGame(skipIndex: -1);
    }

    // [Deprecated] 내부 호출용이었던 것 -> StartGame으로 대체
    private void RealStartGame() => StartGame();
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

    public bool CanStartMiniGame()
    {
        if (wordeater == null) return true;

        if (wordeater.isDead) return false;

        return true;
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
            CheckItemReward();
            // 패널 쪽 애니메이션/상태는 기존 함수 그대로 사용
            algorithmPanel.StartCoroutine(algorithmPanel.CloasePageTab());
            int added = keyboard.GrantRandomLetters(ClearCount);
        }
    }

    // [변경] UI 표시를 여기서 하지 않고, 획득한 아이템 타입을 리턴해서 AlgorithmPanel이 통합 표시하게 함
    public ItemType CheckItemReward()
    {
        // 보상을 받을지 여부
        bool getReward = false;
        bool isHard = (algorithmPanel != null && !algorithmPanel.Mode); 

        if (isHard && ClearCount > 0)
        {
            if (Random.value <= 1f) getReward = true; 
        }
        else if (ClearCount >= 5)
        {
            getReward = true;
        }

        // 보상 지급 로직
        if (getReward)
        {
            if (isHard)
            {
                 // 하드모드 -> 자음/모음 선택권
                 if (ItemManager.Instance != null)
                 {
                     ItemManager.Instance.AddItem(ItemType.JamoSelectionTicket, 1);
                     return ItemType.JamoSelectionTicket;
                 }
            }
            else
            {
                // 일반모드 -> 기존 랜덤 로직
                if (ItemDropManager.Instance != null)
                {
                    // showUI = false로 해서 여기서 팝업 안 띄우고 타입만 받아옴
                    return ItemDropManager.Instance.ObtainRandomItem(showUI: false);
                }
            }
        }

        // 획득 실패 혹은 조건 미달
        return (ItemType)(-1); // -1 or generic invalid
    }
}
