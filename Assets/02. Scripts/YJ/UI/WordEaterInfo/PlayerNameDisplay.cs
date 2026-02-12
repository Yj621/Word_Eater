using UnityEngine;
using TMPro;

public class PlayerNameDisplay : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("이름 앞뒤에 붙을 텍스트. {0} 자리에 이름")]
    [SerializeField] private string format = "{0}";

    private TextMeshProUGUI textMesh;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        // 게임 시작 시 현재 이름으로 갱신
        UpdateNameText(FileManager.Instance.CurrentPlayerName);
    }

    private void OnEnable()
    {
        // 이벤트 구독
        FileManager.Instance.OnNameChanged += UpdateNameText;

        // 켜질 때도 한 번 갱신 (데이터 로드 타이밍 문제 방지)
        UpdateNameText(FileManager.Instance.CurrentPlayerName);
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제 (메모리 누수 방지)
        if (FileManager.Instance != null)
        {
            FileManager.Instance.OnNameChanged -= UpdateNameText;
        }
    }

    // 실제 텍스트를 변경하는 함수
    private void UpdateNameText(string newName)
    {
        if (textMesh != null)
        {
            textMesh.text = string.Format(format, newName);
        }
    }
}