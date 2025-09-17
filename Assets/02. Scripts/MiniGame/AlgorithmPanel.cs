using System.Collections;
using UnityEngine;

public class AlgorithmPanel : MonoBehaviour
{
    Animator ani;
    public PhoneSwiper phoneSwiper;
    public bool Mode; // true : Easy, false : Hard
    public GameObject GameTab;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ani = GetComponent<Animator>();
        GameTab.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OpenEasyMode()
    {
        StartCoroutine(OpenPageTab());
        Mode = true;
    }

    public void OpenHardMode()
    {
        StartCoroutine(OpenPageTab());
        Mode = false;
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
    }

    public IEnumerator CloasePageTab()
    {
        phoneSwiper.isUsingTab = false;
        ani.SetTrigger("Close");
        yield return new WaitForSeconds(0.5f);
        GameTab.SetActive(false);
    }
}
