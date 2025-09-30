using System.Collections.Generic;
using UnityEngine;

public class FolderPut : MonoBehaviour
{
    [Header("영역 & 폴더")]
    [SerializeField] private RectTransform playArea;     // 상위 이미지(하트가 생성될 영역)
    [SerializeField] private RectTransform folderZone;   // 하위 폴더(드롭 존)

    [Header("프리팹 & 개수")]
    [SerializeField] private DragableHeart heartPrefab; // 하트 프리팹 (아래 스크립트 붙어있어야 함)
    [SerializeField] private int heartsToSpawn = 5;

    [Header("필수: 같은 캔버스")]
    [SerializeField] private Canvas canvas;

    private readonly List<DragableHeart> _hearts = new();
    private int _placedCount;
    private MiniGameHook _hook;

    void Awake()
    {
        _hook = GetComponent<MiniGameHook>(); // 같은 루트에 붙여둔 Hook
    }

    void OnEnable()
    {
        StartRound();
    }

    void OnDisable()
    {
        Cleanup();
    }

    private void StartRound()
    {
        Cleanup();
        _placedCount = 0;

        if (!playArea || !folderZone || !heartPrefab || !canvas)
        {
            Debug.LogError("[FolderPut] 참조가 비었어. playArea/folderZone/heartPrefab/canvas 확인해줘.");
            enabled = false;
            return;
        }

        // heartsToSpawn 만큼 랜덤 스폰
        for (int i = 0; i < heartsToSpawn; i++)
            SpawnOne();
    }

    private void SpawnOne()
    {
        var heart = Instantiate(heartPrefab, playArea);
        heart.gameObject.SetActive(true);
        heart.Init(canvas, playArea, folderZone, OnHeartPlaced);

        // 랜덤 위치(버튼/이미지 크기만큼 가장자리 여유)
        var area = playArea.rect;
        var sz = heart.Rect.rect.size;
        float xHalf = (area.width - sz.x) * 0.5f;
        float yHalf = (area.height - sz.y) * 0.5f;

        var randomPos = new Vector2(Random.Range(-xHalf, xHalf), Random.Range(-yHalf, yHalf));
        heart.Rect.anchoredPosition = randomPos;

        _hearts.Add(heart);
    }

    private void OnHeartPlaced(DragableHeart heart)
    {
        _placedCount++;
        // 모두 성공?
        if (_placedCount >= heartsToSpawn)
        {
            // 더 못 건드리게 잠금
            foreach (var h in _hearts) h.Lock();
            _hook?.ReportClear();
        }
    }

    private void Cleanup()
    {
        foreach (var h in _hearts)
            if (h) Destroy(h.gameObject);
        _hearts.Clear();
    }
}
