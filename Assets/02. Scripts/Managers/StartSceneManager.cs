using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartSceneManager : MonoBehaviour
{
    [Header("움직일 텍스트")]
    public TextMeshProUGUI startText;
    [SerializeField] private AudioSource btnSound;
    void Start()
    {
        if (startText != null)
        {
            // anchoredPosition.y를 기준으로 위아래로 움직이게 함
            RectTransform rt = startText.GetComponent<RectTransform>();
            rt.DOAnchorPosY(rt.anchoredPosition.y + 20f, 1f) // 위로 20
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo); // 무한 반복 (왔다갔다)
        }
    }
    public void OnClickStart()
    {
        if (btnSound != null && btnSound.clip != null)
        {
            btnSound.PlayOneShot(btnSound.clip);
            // 소리가 들릴 시간을 주기 위해 0.2초 후 씬 전환
            Invoke("GoToMain", 0.5f);
        }
        else
        {
            // 사운드 설정이 안 되어 있어도 게임은 넘어가게 처리
            GoToMain();
        }
    }

    private void GoToMain()
    {
        LoadingSceneManager.LoadScene("WordEater");
    }
}
