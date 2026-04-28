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
    [SerializeField] private float minDragDistance = 50f; // 최소 드래그 거리
    [SerializeField] private float throwPower = 1.0f;     // 투척 힘 계수
    [SerializeField] private float throwDropY = -200f;    // 실패 판정 Y 높이 (시작점 기준)

    [Header("Physics Settings")]
    [SerializeField] private float gravity = -2500f;   // 중력 (픽셀 단위라 큼)
    [SerializeField] private float bounciness = 0.7f;  // 반발 계수
    [SerializeField] private float rimRadius = 15f;    // 림 충돌체 반지름
    [SerializeField] private float ballRadius = 30f;   // 공 반지름
    [SerializeField] private float hoopWidth = 110f;   // 골대 너비

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
    
    // Physics State
    private Vector2 _velocity;
    private bool _hasPassedRimHeight = false;

    // Drag State
    private Vector2 _dragStartPos;
    private bool _isValidDrag = false;

    // Layering
    private Canvas _ballCanvas;

    private void Awake()
    {
        _hook = GetComponent<MiniGameHook>();
        if (ball != null) _startPos = ball.anchoredPosition;
    }

    private void Start()
    {
        SetupSortingLayers();
    }

    private void OnEnable()
    {
        InitializeGame();
    }
    
    private void OnDisable()
    {
        if (ball != null) ball.DOKill();
    }

    private void InitializeGame()
    {
        _currentGoals = goalCount;
        UpdateUI();
        ResetBall(false);
    }

    private void SetupSortingLayers()
    {
        AttachCanvas(rimBack, 10);
        AttachCanvas(rimFront, 20);
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

    // 화면 밖으로 나가는 것 방지용 경계 (기존 xBound 대신 패널 사용)
    // [SerializeField] private float xBound = 450f; 
    [SerializeField] private RectTransform gameAreaPanel; // [New] 게임 유효 영역 패널

    private void Update()
    {
        // 1. 골대 좌우 이동
        if (hoopMoveRoot != null)
        {
            float sinX = Mathf.Sin(Time.time * hoopMoveSpeed) * hoopMoveRange;
            hoopMoveRoot.anchoredPosition = new Vector2(sinX, hoopMoveRoot.anchoredPosition.y);
        }

        // 2. 물리 시뮬레이션
        if (_isBallFlying && ball != null)
        {
            float dt = Time.deltaTime;

            _velocity.y += gravity * dt;
            ball.anchoredPosition += _velocity * dt;

            CheckRimCollision();
            CheckGameStatus();
            HandleBallLayerSorting();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_isBallFlying) return;
        _dragStartPos = eventData.position;
        _isValidDrag = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Drag Feedback (Optional)
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isValidDrag || _isBallFlying) return;

        Vector2 dragVector = eventData.position - _dragStartPos;

        if (dragVector.y > 0 && dragVector.magnitude >= minDragDistance)
        {
            ThrowBall(dragVector);
        }
    }

    private void ThrowBall(Vector2 dragVec)
    {
        _isBallFlying = true;
        _hasPassedRimHeight = false;
        
        // 힘 조절 (기존 Logic 대비 적절한 값으로 SCALING)
        float powerMult = throwPower * 4.0f; 
        
        _velocity = dragVec * powerMult;

        // 너무 약하면 최소치 보정
        if (_velocity.y < 800f) _velocity.y = 800f;
    }

    private void CheckRimCollision()
    {
        if (ball == null || hoopMoveRoot == null) return;

        Vector2 ballPos = ball.anchoredPosition;
        // 림의 좌표 (골대는 hoopMoveRoot 기준)
        Vector2 hoopPos = hoopMoveRoot.anchoredPosition;
        
        // 림 왼쪽/오른쪽 포인트 계산 (너비의 절반)
        float halfWidth = hoopWidth * 0.5f;
        Vector2 leftRim = hoopPos + new Vector2(-halfWidth, 0);
        Vector2 rightRim = hoopPos + new Vector2(halfWidth, 0);

        ProcessBounce(ref ballPos, leftRim);
        ProcessBounce(ref ballPos, rightRim);

        ball.anchoredPosition = ballPos;
    }

    private void ResetBallDelayed()
    {
        ResetBall(true);
    }

    private void ProcessBounce(ref Vector2 ballPos, Vector2 rimPos)
    {
        // [수정] 위에서 아래로 떨어질 때(Velocity Y < 0)만 튕김 처리
        if (_velocity.y >= 0) return;

        float distSqr = (ballPos - rimPos).sqrMagnitude;
        float minDist = rimRadius + ballRadius;

        if (distSqr < minDist * minDist)
        {
            float dist = Mathf.Sqrt(distSqr);
            Vector2 normal = (ballPos - rimPos).normalized;

            // 위치 보정
            float overlap = minDist - dist;
            ballPos += normal * overlap;

            // 속도 반사
            if (Vector2.Dot(_velocity, normal) < 0)
            {
                Vector2 reflect = Vector2.Reflect(_velocity, normal);
                _velocity = reflect * bounciness;
                _velocity *= 0.9f;

                // 튕겼을 때 자연스럽게 사라지게 하기 위해
                // 여기서는 별도 처리 안 함 (GameStatus에서 바닥 떨어지면 처리)
                // 만약 튕기자마자 사라지게 하려면 여기서 예약 가능
            }
        }
    }

    private void CheckGameStatus()
    {
        if (hoopMoveRoot == null || ball == null) return;

        Vector2 pos = ball.anchoredPosition;
        float hoopY = hoopMoveRoot.anchoredPosition.y;

        // [New] 1. 패널 기반 경계 체크
        if (gameAreaPanel != null)
        {
            // 월드 좌표 기준 비교
            Vector3 ballWorldPos = ball.position;
            Vector3[] panelCorners = new Vector3[4];
            gameAreaPanel.GetWorldCorners(panelCorners);
            
            // UI World Corners: 0=BottomLeft, 1=TopLeft, 2=TopRight, 3=BottomRight
            // Min = 0, Max = 2
            float minX = panelCorners[0].x;
            float maxX = panelCorners[2].x;
            float minY = panelCorners[0].y;
            float maxY = panelCorners[2].y;

            if (ballWorldPos.x < minX || ballWorldPos.x > maxX || 
                ballWorldPos.y < minY || ballWorldPos.y > maxY)
            {
                FadeOutAndReset();
                return;
            }
        }
        else
        {
             // Fallback: 기존 로직 (패널 미할당 시)
             // 1. 좌우 경계
             /* 
             if (Mathf.Abs(pos.x) > xBound) 
             {
                 FadeOutAndReset(); 
                 return;
             }
             */
             
             // 2. 하단 경계
             if (pos.y < _startPos.y + throwDropY && !_hasPassedRimHeight)
             {
                 FadeOutAndReset();
                 return;
             }
        }

        // 2. 골 판정
        if (_velocity.y < 0 && pos.y < hoopY && pos.y > hoopY - 60f)
        {
             if (Mathf.Abs(pos.x - hoopMoveRoot.anchoredPosition.x) < 40f)
             {
                 if (!_hasPassedRimHeight)
                 {
                     _hasPassedRimHeight = true;
                     OnGoal();
                     return;
                 }
             }
        }

        // 3. 실패 판정 (기존 로직 보존 - 패널 확인과는 별개로 높이 기준 실패 처리도 필요할 수 있음)
        // 하지만 패널 방식이 우선이므로 여기서는 패널이 없을 때만 동작하도록 하거나,
        // 패널 밖으로 나가는 순간 이미 처리되므로 사실상 중복될 수 있음. 
        // 안전을 위해 남겨둠 (단, 패널 밖 = 실패이므로 이미 처리됨)
    }

    private void OnGoal()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.SFXStart(SoundManager.SFXType.goalSound);
        // Debug.Log("GOAL!");
        _currentGoals--;
        UpdateUI();

        // 골인 후 잠시 떨어지다가 페이드아웃
        Invoke(nameof(FadeOutAndReset), 0.5f);

        if (_currentGoals <= 0)
        {
            if (goalText != null) goalText.text = "CLEARED!";
            _hook?.ReportClear();
        }
    }

    // [신규] 공을 서서히 투명하게 만들고 원위치로 리셋
    private void FadeOutAndReset()
    {
        // 중복 호출 방지
        if (!_isBallFlying && ball.anchoredPosition.Equals(_startPos)) return;
        
        // 이미 페이드아웃 중이라면 패스하고 싶지만, 간단하게 그냥 실행
        CancelInvoke(nameof(FadeOutAndReset));
        CancelInvoke(nameof(ResetBallDelayed));

        // 물리 멈춤
        _isBallFlying = false;

        // 투명하게 Fade Out
        var img = ball.GetComponent<Image>();
        if (img != null)
        {
            img.DOFade(0f, 0.4f).OnComplete(() =>
            {
                ResetBall(true); // 리셋하며 다시 불투명하게
            });
        }
        else
        {
            ResetBall(true);
        }
    }

    private void ResetBall(bool animation = true)
    {
        _isBallFlying = false;
        _velocity = Vector2.zero;
        CancelInvoke(nameof(FadeOutAndReset));
        
        if (_ballCanvas != null) _ballCanvas.sortingOrder = 30;

        ball.DOKill(); // Fade 애니메이션 등 중단
        
        ball.anchoredPosition = _startPos;
        ball.localScale = Vector3.one;

        // 투명도 복구
        var img = ball.GetComponent<Image>();
        if (img != null) img.color = new Color(img.color.r, img.color.g, img.color.b, 1f);

        if (animation)
        {
            ball.localScale = Vector3.zero;
            ball.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
        }
    }

    private void UpdateUI()
    {
        if (goalText != null) goalText.text = $"남은 골: {_currentGoals}";
    }

    private void HandleBallLayerSorting()
    {
        if (ball == null || rimBack == null || rimFront == null || _ballCanvas == null) return;

        float rimY = rimFront.anchoredPosition.y;
        float ballY = ball.anchoredPosition.y;

        // 공이 림 근처에 왔을 때
        if (ballY < rimY + 20f)
        {
            float ballX = ball.anchoredPosition.x;
            float hoopX = hoopMoveRoot.anchoredPosition.x;
            float width = hoopWidth * 0.4f; 

            // 골대 안쪽이면 뒤로 보냄
            if (Mathf.Abs(ballX - hoopX) < width)
            {
                _ballCanvas.sortingOrder = 15;
            }
            else
            {
                _ballCanvas.sortingOrder = 30;
            }
        }
        else
        {
            _ballCanvas.sortingOrder = 30;
        }
    }
}
