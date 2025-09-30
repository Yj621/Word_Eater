using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 2번 탭마다 이미지(균열) 단계가 바뀌고,
/// 총 30번 탭하면 클리어되는 미니게임.
/// </summary>
[RequireComponent(typeof(Image))]
public class PhoneCrack : MonoBehaviour, IPointerClickHandler
{
    [Header("균열 단계 스프라이트 (순서대로)")]
    [SerializeField] private Sprite[] crackStages;

    [Header("설정")]
    [SerializeField] private int totalTapsRequired = 30;  // 목표: 30탭
    [SerializeField] private int tapsPerStage = 2;        // 2탭마다 다음 이미지


    private Image _img;
    private int _tapCount;
    private int _currentStage; // 0부터 시작
    private MiniGameHook _hook;

    private void Awake()
    {
        _img = GetComponent<Image>();
        _hook = GetComponent<MiniGameHook>(); // 루트에 Hook 붙어있으면 바로 잡혀옴
        if (_img == null)
            Debug.LogError("[PhoneCrack] Image 컴포넌트가 필요함.");
    }

    private void OnEnable()
    {
        ResetGame();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        HandleTap();
    }

    private void Update()
    {
        // 에디터/PC 테스트용 마우스 클릭 대응
        if (Input.GetMouseButtonDown(0))
        {
            // UI가 아닌 월드쪽 충돌을 막고 싶다면,
            // EventSystem.current.IsPointerOverGameObject() 체크도 가능
            HandleTap();
        }
    }

    private void HandleTap()
    {
        _tapCount++;
     
        // 단계 계산 (2탭 = 1단계 진전)
        int stage = Mathf.FloorToInt(_tapCount / (float)tapsPerStage);
        if (stage != _currentStage)
        {
            _currentStage = stage;
            UpdateCrackSprite(_currentStage);
        }

        // 목표 달성?
        if (_tapCount >= totalTapsRequired)
        {
            // 클리어 보고
            _hook?.ReportClear();
            // 혹시 다음 라운드를 위해 잠깐 눌림 막고 싶으면 아래처럼 비활성화
            // enabled = false;
        }
    }

    private void UpdateCrackSprite(int stageIndex)
    {
        if (crackStages == null || crackStages.Length == 0 || _img == null) return;

        // crackStages 길이가 부족해도 마지막 스프라이트로 고정
        int clamped = Mathf.Clamp(stageIndex, 0, crackStages.Length - 1);
        _img.sprite = crackStages[clamped];
    }

    private void ResetGame()
    {
        _tapCount = 0;
        _currentStage = 0;
        UpdateCrackSprite(0);
    }
}
