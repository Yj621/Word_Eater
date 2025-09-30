using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class FastClick : MonoBehaviour
{
    [Header("버튼이 움직일 영역(필수)")]
    [SerializeField] private RectTransform spawnArea;

    [Header("스폰할 버튼 프리팹(필수)")]
    [SerializeField] private Button buttonPrefab;

    [Header("설정")]
    [SerializeField] private int clicksToClear = 10;   // 10번 성공 시 클리어
    [SerializeField] private float minMoveDistance = 80f; // 이전 위치와 최소 거리(픽셀)
    [SerializeField] private bool playPopOnMove = true;   // 이동 시 살짝 튀는 연출

    private Button _btn;
    private RectTransform _btnRt;
    private int _count;
    private Vector2 _lastPos;
    private MiniGameHook _hook;

    private void Awake()
    {
        _hook = GetComponent<MiniGameHook>(); // 같은 루트에 붙어있으면 자동으로 참조
    }

    private void OnEnable()
    {
        StartGame();
    }

    private void OnDisable()
    {
        Cleanup();
    }

    private void StartGame()
    {
        if (spawnArea == null || buttonPrefab == null)
        {
            Debug.LogError("[FastClick] spawnArea나 buttonPrefab이 비어있음.");
            enabled = false;
            return;
        }

        _count = 0;

        if (_btn == null)
        {
            _btn = Instantiate(buttonPrefab, spawnArea, false);
            _btnRt = _btn.GetComponent<RectTransform>();

            // 버튼 클릭 시 처리
            _btn.onClick.AddListener(OnButtonClicked);
        }

        // 초기 위치 배치
        MoveButtonRandom(true);
        _btn.gameObject.SetActive(true);
    }

    private void Cleanup()
    {
        if (_btn != null)
        {
            // _btn.onClick.RemoveListener(OnButtonClicked); // <= 이 줄 지워
            _btn.gameObject.SetActive(false);
        }
    }


    private void OnButtonClicked()
    {
        _count++;

        if (_count >= clicksToClear)
        {
            // 클리어
            _btn.gameObject.SetActive(false);
            _hook?.ReportClear();
            return;
        }

        MoveButtonRandom(false);
    }

    private void MoveButtonRandom(bool first)
    {
        if (_btnRt == null || spawnArea == null) return;

        // 버튼과 영역의 사이즈
        var areaRect = spawnArea.rect;
        var btnSize = _btnRt.rect.size;

        // 버튼이 영역 밖으로 나가지 않도록 여유 폭/높이 확보
        float xHalf = (areaRect.width - btnSize.x) * 0.5f;
        float yHalf = (areaRect.height - btnSize.y) * 0.5f;

        // anchor/pivot이 (0.5, 0.5) 기준일 때 중앙을 원점으로 랜덤
        Vector2 pos;
        int guard = 0;
        do
        {
            float x = Random.Range(-xHalf, xHalf);
            float y = Random.Range(-yHalf, yHalf);
            pos = new Vector2(x, y);

            // 첫 위치는 거리 체크 생략
            if (first) break;

            guard++;
            if (guard > 20) break; // 과도한 루프 방지
        }
        while (Vector2.Distance(pos, _lastPos) < minMoveDistance);

        _btnRt.anchoredPosition = pos;
        _lastPos = pos;

        if (playPopOnMove)
        {
            // 간단한 팝 연출
            _btnRt.localScale = Vector3.one * 0.85f;
            // 코루틴/트윈 없을 때도 부드럽게: Lerp를 간단히
            StopAllCoroutines();
            StartCoroutine(ScaleBack());
        }
    }

    private IEnumerator ScaleBack()
    {
        float t = 0f;
        while (t < 0.12f)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / 0.12f);
            _btnRt.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, k);
            yield return null;
        }
        _btnRt.localScale = Vector3.one;
    }
}
