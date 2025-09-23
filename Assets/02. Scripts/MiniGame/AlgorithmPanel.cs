using System.Collections;
using UnityEngine;

public class AlgorithmPanel : MonoBehaviour
{
    Animator ani;
    public PhoneSwiper phoneSwiper;
    public bool Mode; // true : Easy, false : Hard
    public GameObject GameTab;

    // 캐시
    private MiniGameController _mini;

    void Start()
    {
        ani = GetComponent<Animator>();
        GameTab.SetActive(false);
        if (GameTab) _mini = GameTab.GetComponentInChildren<MiniGameController>(true);
    }

    public void OpenEasyMode()
    {
        Mode = true;
        StartCoroutine(OpenPageTab());
    }

    public void OpenHardMode()
    {
        Mode = false;
        StartCoroutine(OpenPageTab());
    }

    public void CloseMode()
    {
        StartCoroutine(CloasePageTab());
    }

    public IEnumerator OpenPageTab()
    {
        phoneSwiper.isUsingTab = true;
        ani.SetTrigger("Open");
        yield return new WaitForSeconds(0.5f);

        GameTab.SetActive(true);
        // 미니게임 시작
        if (_mini == null) _mini = GameTab.GetComponentInChildren<MiniGameController>(true);
        _mini?.Begin();
    }

    public IEnumerator CloasePageTab()
    {
        // 미니게임 정리
        if (_mini == null) _mini = GameTab.GetComponentInChildren<MiniGameController>(true);
        _mini?.StopAllGames();

        GameTab.SetActive(false);
        phoneSwiper.isUsingTab = false;
        ani.SetTrigger("Close");
        yield return new WaitForSeconds(0.5f);
    }
}
