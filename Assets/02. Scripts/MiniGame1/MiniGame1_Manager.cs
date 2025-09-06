using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class MiniGame1_Manager : MonoBehaviour
{
    [Header("UI OBJS")]
    public RectTransform player;
    public RectTransform goal;
    public Image sign;
    public TMP_Text timerText;

    public TouchPanel touchPanel;

    [Header("Setting")]
    public float speed = 100f;
    public int TimeLimit = 30;

    int RemainTime;
    bool SignColor = true; //True << Green, False << Red
    bool Move_Possible = true;
    bool GameEndCheck = true; //FALSE << Game End
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RemainTime = TimeLimit;
        if (timerText != null)
            timerText.text = RemainTime.ToString();


        Move_Possible = true;
        GameEndCheck = true;
        SignColor = true;

        StartCoroutine(SignalCoroutine());
        StartCoroutine(TimerCoroutine());
    }

    // Update is called once per frame
    void Update()
    {
        // Move When Red Sign
        if (GameEndCheck && !SignColor && touchPanel.isPressed) {

            GameOver();
        }

        //Move
        if (touchPanel.isPressed && Move_Possible)
        {
            player.anchoredPosition += Vector2.up * speed * Time.deltaTime;
        }

        //Clear Game
        if (GameEndCheck)
        {
            GameClear();
        }
    }

    void GameClear()
    {
        if (player.anchoredPosition.y >= goal.anchoredPosition.y)
        {
            Move_Possible = false;
            GameEndCheck = false;

            //Add Clear Logic Later
            Debug.Log("Success!");
        }
    }

    void GameOver() {
        Move_Possible = false;
        GameEndCheck = false;

        //Add Fail Logic Later
        Debug.Log("Fail!");
    }


    //Sign Change
    IEnumerator SignalCoroutine()
    {
        bool isGreen = true;

        while (GameEndCheck) {
            float waitTime = Random.Range(1f, 3f); //Random time Between 1~3f

            if (isGreen)
            {
                sign.color = Color.green;
                SignColor = true;
            }
            if (!isGreen) {
                sign.color = Color.red;
                SignColor = false;
            }

            yield return new WaitForSeconds(waitTime);
            isGreen = !isGreen;
        }
    }


    //Timer
    IEnumerator TimerCoroutine() {
        while (GameEndCheck) {
            yield return new WaitForSeconds(1f);
            RemainTime--;
            if (GameEndCheck && timerText != null)
                timerText.text = RemainTime.ToString();

            //Time Out
            if (RemainTime <= 0) {
                GameOver();
            }
        }
    }
}
