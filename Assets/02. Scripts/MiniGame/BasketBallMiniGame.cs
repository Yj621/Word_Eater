using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

public class BasketBallMiniGame : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Game Settings")]
    [SerializeField] private int goalCount = 3;  // 목표 골 수
    [SerializeField] private float hoopMoveSpeed = 2f; // 골대 이동 속도
    [SerializeField] private float hoopMoveRange = 300f; // 골대 좌우 이동 범위
    
    [Header("Throw Settings")]
    [SerializeField] private float minDragDistance = 50f; // 최소 드래그 거리 (이거보다 짧으면 안 던짐)
    [SerializeField] private float throwPower = 1.0f;     // 투척 힘(애니메이션 속도 조절용)
    [SerializeField] private float throwDropY = -200f;    // 실패 시 떨어질 Y 위치 (상대값)

    [Header("References")]
    [SerializeField] private RectTransform hoopMoveRoot; // 좌우로 움직일 골대 부모
    [SerializeField] private RectTransform rimBack;      // 골대 뒷부분
    [SerializeField] private RectTransform rimFront;     // 골대 앞부분
    [SerializeField] private RectTransform ball;         // 농구공
    [SerializeField] private TextMeshProUGUI goalText;   // 남은 골 수 텍스트

    private MiniGameHook _hook;
    private Vector2 _startPos;
    private int _currentGoals;
    private bool _isBallFlying = false;
    
    // 드래그 관련
    private Vector2 _dragStartPos;
    private bool _isValidDrag = false;

    // 레이어링 관련
    private Canvas _ballCanvas;

    private void Awake()
    {
        _hook = GetComponent<MiniGameHook>();
        if (ball != null) _startPos = ball.anchoredPosition;
    }

    private void Start()
    {
        // [수정] 캔버스 소팅 레이어 초기화 (확실한 앞뒤 구분을 위해)
        SetupSortingLayers();
    }

    private void OnEnable()
    {
        InitializeGame();
    }
    
    private void OnDisable()
    {
        // 게임 꺼질 때 트윈 정리
        if (ball != null) ball.DOKill();
    }

    private void InitializeGame()
    {
        _currentGoals = goalCount;
        // _isBallFlying = false; // ResetBall에서 처리됨
        
        UpdateUI();
        ResetBall(false); // 애니메이션 없이 즉시 리셋
    }

    private void SetupSortingLayers()
    {
        // Canvas 컴포넌트가 없으면 추가하고 Order 설정
        // RimBack: 10
        AttachCanvas(rimBack, 10);
        // RimFront: 20
        AttachCanvas(rimFront, 20);
        // Ball: 30 (기본값: 제일 앞)
        _ballCanvas = AttachCanvas(ball, 30);
    }

    private Canvas AttachCanvas(RectTransform target, int order)
    {
        if (target == null) return null;
        Canvas c = target.GetComponent<Canvas>();
        if (c == null) c = target.gameObject.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = order;
        return c;
    }

    void Update()
    {
        // 골대 좌우 이동 (PingPong or Sin)
        if (hoopMoveRoot != null)
        {
            float sinX = Mathf.Sin(Time.time * hoopMoveSpeed) * hoopMoveRange;
            hoopMoveRoot.anchoredPosition = new Vector2(sinX, hoopMoveRoot.anchoredPosition.y);
        }

        // 공 레이어 순서 정리 (날아가는 중일 때만)
        if (_isBallFlying)
        {
            HandleBallLayerSorting();
        }
    }

    // --- 드래그 입력 처리 ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_isBallFlying) return;

        _dragStartPos = eventData.position;
        _isValidDrag = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 드래그 중 시각적 피드백(공을 약간 당긴다거나 화살표 표시)이 필요하면 여기서 구현
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isValidDrag || _isBallFlying) return;

        Vector2 dragEndPos = eventData.position;
        Vector2 dragVector = dragEndPos - _dragStartPos;

        // 1. 위쪽으로 드래그했는지 확인 (Y > 0)
        // 2. 최소 거리 이상인지 확인
        if (dragVector.y > 0 && dragVector.magnitude >= minDragDistance)
        {
            ThrowBall(dragVector);
        }
    }

    // --- 게임 로직 ---

    private void ThrowBall(Vector2 dragVec)
    {
        _isBallFlying = true;

        // 목표 지점: 골대가 있는 Y 높이 + 약간 위
        float targetY = (hoopMoveRoot != null) ? hoopMoveRoot.anchoredPosition.y + 100f : 300f;
        
        // X축: 드래그 방향에 따라 약간 휘게 할 수도 있지만, 
        // 미니게임 난이도를 위해 일단은 "수직 위 + 드래그 기울기 약간 반영" or "그냥 수직 위"
        // 사용자 요청: "공을 던져서 넣는거지" -> 조준이 필요할 수도?
        // 일단 단순하게 X축은 현재 공 위치 유지 (StartPos.x) 하되 드래그 X성분 약간 반영
        float targetX = _startPos.x + (dragVec.x * 0.5f); 

        Vector2 peakPos = new Vector2(targetX, targetY);

        // DOTween Sequence
        Sequence seq = DOTween.Sequence();

        // 1. 올라가기 (Arc)
        float upDuration = 0.6f / throwPower;
        seq.Append(ball.DOAnchorPos(peakPos, upDuration).SetEase(Ease.OutSine));

        // 2. 떨어지기
        //   골 판정은 정점(Peak)에 도달했을 때 수행하거나, 떨어지는 궤적 계산.
        //   여기서는 정점에서 판정 후, '들어가는 궤적' or '빗나가는 궤적' 분기
        
        seq.AppendCallback(() => 
        {
            bool success = CheckGoal(peakPos.x); // 정점 X좌표 기준으로 판정
            
            // 떨어질 위치 계산
            Vector2 dropPos = peakPos;
            dropPos.y += throwDropY; // 아래로 떨어짐

            // 성공하면 약간 안쪽으로, 실패하면 바깥으로? 
            // 일단 단순 낙하
            ball.DOAnchorPos(dropPos, 0.4f).SetEase(Ease.InSine).OnComplete(() => 
            {
                if (success)
                {
                    OnGoal();
                }
                else
                {
                    // 실패: 잠시 후 리셋
                    Invoke(nameof(ResetBallDelayed), 0.5f);
                }
            });
        });
    }

    private bool CheckGoal(float ballX)
    {
        if (hoopMoveRoot == null) return false;

        float hoopX = hoopMoveRoot.anchoredPosition.x;
        // 판정 범위: 골대 중심 차이 (반지름 60f)
        if (Mathf.Abs(ballX - hoopX) < 60f)
        {
            return true;
        }
        return false;
    }

    private void OnGoal()
    {
        Debug.Log("GOAL!");
        _currentGoals--;
        UpdateUI();

        if (_currentGoals <= 0)
        {
            // 클리어!
            if (goalText != null) goalText.text = "CLEARED!";
            _hook?.ReportClear();
            // 게임 종료 연출?
        }
        else
        {
            // 다음 공 준비
            Invoke(nameof(ResetBallDelayed), 0.5f);
        }
    }

    private void ResetBallDelayed()
    {
        ResetBall(true);
    }

    private void ResetBall(bool animation = true)
    {
        _isBallFlying = false;
        ball.DOKill();
        
        // 레이어 초기화 (맨 앞으로)
        if (_ballCanvas != null) _ballCanvas.sortingOrder = 30;

        if (animation)
        {
            // 공이 '뿅' 하고 나타나는 연출 or 그냥 이동
            ball.anchoredPosition = _startPos;
            ball.localScale = Vector3.zero;
            ball.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
        }
        else
        {
            ball.anchoredPosition = _startPos;
            ball.localScale = Vector3.one;
        }
    }

    private void UpdateUI()
    {
        if (goalText != null) goalText.text = $"남은 골: {_currentGoals}";
    }

    // 레이어링 처리: 공이 내려갈 때(정점 이후) 림 뒤로 보내기
    private void HandleBallLayerSorting()
    {
        if (ball == null || rimBack == null || rimFront == null || _ballCanvas == null) return;

        // 공의 현재 Y위치가 림(Front)보다 낮아지기 시작했나? (떨어지는 중)
        // 판정 기준: RimFront의 Y 위치
        float rimY = rimFront.position.y;
        float ballY = ball.position.y;

        // 하강 중이고 림 근처까지 왔다면
        // (정점은 훨씬 높을 테니, 림Y + 50 정도보다 낮아지면 내려가는 중이라 판단 가능)
        if (ballY < rimY + (50 * transform.lossyScale.y))
        {
            // 여기서 중요: "골인 궤적"일 때만 뒤로 보내야 함.
            // X축 거리가 골대 범위 안이면 뒤로 보냄.
            
            float ballX = ball.position.x;
            float hoopX = rimFront.position.x; 

            if (Mathf.Abs(ballX - hoopX) < 80f * transform.lossyScale.x) // 넉넉하게
            {
                // RimBack(10) < Ball(15) < RimFront(20)
                _ballCanvas.sortingOrder = 15;
            }
            else
            {
                // 빗나감 -> 그냥 맨 앞
                _ballCanvas.sortingOrder = 30;
            }
        }
        else
        {
            // 높이 떠 있을 땐 무조건 맨 앞
            _ballCanvas.sortingOrder = 30;
        }
    }
}
