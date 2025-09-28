using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Puzzle2X2Game : MonoBehaviour
{
    [Header("퍼즐 세트들 (여러 이미지 세트)")]
    public PuzzleSet2X2[] puzzleSets;

    [Header("정답 슬롯(좌상→0, 우상→1, 좌하→2, 우하→3)")]
    public RectTransform[] targetSlots; // size 4, 제자리(정답 위치)

    [Header("초기 흩뿌릴 영역(트레이)")]
    public RectTransform spawnArea;     // 조각을 랜덤으로 뿌릴 UI 영역

    [Header("조각 프리팹")]
    public PuzzlePiece2X2 piecePrefab;

    [Header("필수: 같은 캔버스")]
    public Canvas canvas;

    [Header("초기 랜덤 회전 허용 (0/90/180/270 중 하나)")]
    public bool randomizeRotation = true;

    [Header("슬롯 스냅 허용 반경(px)")]
    public float snapRadius = 80f;

    MiniGameHook _hook;

    // 슬롯 점유 현황 (index 0..3)
    PuzzlePiece2X2[] _occupied;
    int _correctCount;
    List<PuzzlePiece2X2> _pieces = new();

    void Awake()
    {
        _hook = GetComponent<MiniGameHook>();
    }

    void OnEnable()
    {
        StartRound();
    }

    void OnDisable()
    {
        Cleanup();
    }

    void StartRound()
    {
        Cleanup();

        if (canvas == null || targetSlots == null || targetSlots.Length != 4 || spawnArea == null || piecePrefab == null)
        {
            Debug.LogError("[Puzzle2x2Game] 참조가 비었거나 슬롯 수가 4가 아님.");
            enabled = false; return;
        }
        if (puzzleSets == null || puzzleSets.Length == 0)
        {
            Debug.LogError("[Puzzle2x2Game] 퍼즐 세트가 비어 있음.");
            enabled = false; return;
        }

        _occupied = new PuzzlePiece2X2[4];
        _correctCount = 0;

        // 세트 랜덤 선택
        int setIdx = Random.Range(0, puzzleSets.Length);
        var set = puzzleSets[setIdx];
        if (set.quads == null || set.quads.Length != 4)
        {
            Debug.LogError("[Puzzle2x2Game] 세트의 quads는 반드시 4장이어야 함.");
            enabled = false; return;
        }

        // 4조각 생성 (정답 슬롯 인덱스 = 0..3)
        for (int i = 0; i < 4; i++)
        {
            var p = Instantiate(piecePrefab, spawnArea, false);
            p.Setup(this, canvas, i, set.quads[i]);
            _pieces.Add(p);

            // 초기 배치: 영역 내 랜덤
            var area = spawnArea.rect;
            var sz = p.Rect.rect.size;
            float xHalf = (area.width - sz.x) * 0.5f;
            float yHalf = (area.height - sz.y) * 0.5f;
            Vector2 pos = new Vector2(Random.Range(-xHalf, xHalf), Random.Range(-yHalf, yHalf));
            p.Rect.anchoredPosition = pos;

            // 초기 랜덤 회전(선택)
            if (randomizeRotation)
            {
                int r = Random.Range(0, 4); // 0,1,2,3 → 0,90,180,270
                p.SetRotationSteps(r);
            }
        }
    }

    void Cleanup()
    {
        foreach (var p in _pieces)
            if (p != null) Destroy(p.gameObject);
        _pieces.Clear();
        _occupied = null;
        _correctCount = 0;
    }

    // Piece가 드롭 끝내며 "가장 가까운 슬롯" 요청 → 스냅 허용이면 그 슬롯 index 반환, 아니면 -1
    public int GetSnapSlotIndex(Vector2 pieceScreenPos)
    {
        // 각 슬롯 중심의 스크린 좌표와 비교
        Camera cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;

        int closest = -1;
        float best = float.MaxValue;

        for (int i = 0; i < targetSlots.Length; i++)
        {
            var slot = targetSlots[i];
            Vector2 slotScreen = RectTransformUtility.WorldToScreenPoint(cam, slot.position);
            float d = Vector2.Distance(pieceScreenPos, slotScreen);
            if (d < best)
            {
                best = d; closest = i;
            }
        }

        // 스냅 반경 이내일 때만 허용
        return (best <= snapRadius) ? closest : -1;
    }

    // 슬롯 점유 시도 (중복 방지). 성공 시 true
    public bool TryOccupySlot(int slotIndex, PuzzlePiece2X2 piece)
    {
        if (slotIndex < 0 || slotIndex >= 4) return false;
        if (_occupied[slotIndex] != null && _occupied[slotIndex] != piece) return false;

        // 다른 슬롯에서 차지 중이었다면 해제
        for (int i = 0; i < 4; i++)
        {
            if (_occupied[i] == piece) _occupied[i] = null;
        }
        _occupied[slotIndex] = piece;
        return true;
    }

    // 조각이 슬롯에 붙거나 회전이 바뀔 때마다 정답 여부 갱신
    public void RecheckPieceState(PuzzlePiece2X2 piece)
    {
        bool wasCorrect = piece.isCountedCorrect;
        bool nowCorrect = piece.CurrentSlotIndex == piece.CorrectSlotIndex && piece.IsAtCorrectRotation;

        if (wasCorrect == nowCorrect) return;

        piece.isCountedCorrect = nowCorrect;
        _correctCount += nowCorrect ? 1 : -1;

        if (_correctCount >= 4)
        {
            // 모두 정답!
            _hook?.ReportClear();
        }
    }

    public RectTransform GetSlot(int index) => targetSlots[index];
}
