using System.Collections;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using UnityEngine.UI;

public class AlgorithmPanel : MonoBehaviour
{
    Animator ani;
    public PhoneSwiper phoneSwiper;
    public bool Mode; // true : Easy, false : Hard
    public GameObject GameTab;
    public GameObject ResultTab;
    public bool IsOpen => GameTab.activeSelf || ResultTab.activeSelf;
    public Button[] Buttons;

    // 캐시
    private MiniGameController _mini;

    void Start()
    {
        ani = GetComponent<Animator>();
        GameTab.SetActive(false);
        ResultTab.SetActive(false);
        if (GameTab) _mini = GameTab.GetComponentInChildren<MiniGameController>(true);
    }

    private void Update()
    {
            for(int i = 0; i < Buttons.Length; i++)
            {
                Buttons[i].interactable = !IsOpen;
            }
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
        ani.SetTrigger("Close");
        yield return new WaitForSeconds(0.5f);

        // 결과 주입 -> 활성화
        var rp = ResultTab.GetComponentInChildren<ResultPanel>(true);
        if (rp != null)
            rp.Init(Mode, _mini != null ? _mini.ClearCount : 0);

        ResultTab.SetActive(true);
    }

    public void CloseResultTab()
    {
        ResultTab.SetActive(false);
        phoneSwiper.isUsingTab = false;
       _mini.ClearCount = 0;
    }
}
