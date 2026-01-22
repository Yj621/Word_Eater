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
            // CheckItemReward(); // [수정] AlgorithmPanel에서 처리하므로 중복 호출 제거
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

        // [변경] 사용자 요청: 
        // 1. 하드모드: ClearCount만큼 자음선택권 지급 (기존 유지)
        // 2. 이지모드: 3단계부터 매 단계 70% 확률로 아이템 개수 누적 (스택)
        if (isHard)
        {
             // 하드모드 (기존 로직: 100% 지급, 개수는 ClearCount)
             if (ItemManager.Instance != null && ClearCount > 0)
             {
                 ItemManager.Instance.AddItem(ItemType.JamoSelectionTicket, ClearCount);

                if (ClearCount >= 3) {
                    // 초성 힌트는 3개 클리어시마다 한개 씩 ex) 3클 -> 1개 , 5클 -> 1개, 7클 -> 2개
                    ItemManager.Instance.AddItem(ItemType.HintChosung, ClearCount/3);
                }

                 return ItemType.JamoSelectionTicket;
             }
        }
        else
        {
            // 이지모드
            int earnedCount = 0;
            // "3개 이상 깨면 한 문제마다 70퍼센트의 확률로" -> 3, 4, 5... 단계에 대해 각각 롤링
            for (int i = 1; i <= ClearCount; i++)
            {
                if (i >= 3) // 3단계부터 시작
                {
                    if (Random.value <= 0.7f) earnedCount++;
                }
            }

            if (earnedCount > 0)
            {
                if (ItemDropManager.Instance != null)
                {
                    // [변경] 골고루 지급: 획득한 개수(earnedCount)만큼 반복해서 따로따로 뽑는다.
                    ItemType lastType = (ItemType)(-1);
                    
                    for (int k = 0; k < earnedCount; k++)
                    {
                        // showUI = false (나중에 통합 알림 혹은 알림 생략)
                        lastType = ItemDropManager.Instance.ObtainRandomItem(showUI: false);
                    }

                    Debug.Log($"[MiniGame] 이지모드 보상: 총 {earnedCount}회 가챠 실행 완료");
                    return lastType; // 마지막 획득한 타입을 리턴 (대표용)
                }
            }
        }

        // 획득 실패 혹은 조건 미달
        return (ItemType)(-1); // -1 or generic invalid
    }
}
