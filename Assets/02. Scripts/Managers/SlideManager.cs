using UnityEngine;

public class SlideManager : MonoBehaviour
{
    private Vector2 startPos;
    private Vector2 endPos;

    public GameManager gamemanager;
    public PhoneSwiper phoneswiper;

    public RectTransform SettingPanel;
    Vector2 originPos;
    Vector2 tempPos;

    Vector2 UpPos;

    public bool isOK = true;
    public bool isSlide = false;
    public bool BlockJJS = false;
    public bool isOn = false;
    void Awake()
    {
        originPos = SettingPanel.anchoredPosition;

    }
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            startPos = Input.mousePosition;
        }

        if (Input.GetMouseButton(0))
        {
            tempPos = Input.mousePosition;
            DuringSilde();

        }

        if (Input.GetMouseButtonUp(0))
        {
            isSlide = false;
            endPos = Input.mousePosition;
            DetectSwipe();

        }

        // 모바일
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
                startPos = touch.position;

            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                tempPos = Input.mousePosition;
                DuringSilde();
            }

            if (touch.phase == TouchPhase.Ended)
            {
                endPos = touch.position;
                DetectSwipe();
            }
        }
    }

    private void DuringSilde()
    {
        if (startPos.y >= Screen.height * 0.7f)
        {
            float swipeY = startPos.y - tempPos.y;
            float swipeThreshold = Screen.height * 0.2f;

            if (!isSlide)
            {

                if (swipeY > swipeThreshold && isOK && !phoneswiper.isUsingTab)
                {
                    BlockJJS = true;

                    // 패널 정상화
                    SettingPanel.anchoredPosition = originPos + Vector2.up * SettingPanel.rect.height;
                    UpPos = SettingPanel.anchoredPosition;

                    SettingPanel.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                    SettingPanel.gameObject.SetActive(true);
                    isSlide = true;
                }
            }
            else
            {
                Vector2 targetPos = new Vector2(UpPos.x, UpPos.y - (swipeY / 3));

                gamemanager.SlidePanelDuring(SettingPanel, targetPos);
            }
        }
    }
    public void offBtn() {
        isOn = false;
    }

    private void DetectSwipe()
    {
        float swipeY = startPos.y - endPos.y;

        float swipeThreshold = Screen.height * 0.3f;

        if (startPos.y >= Screen.height * 0.7f)
        {
            //위에서 아래로 슬라이드
            if (swipeY > swipeThreshold)
            {
                if (isOK && !phoneswiper.isUsingTab)
                {

                    BlockJJS = true;
                    isOn = true;

                    SettingPanel.anchoredPosition = originPos + Vector2.up * Screen.height;
                    gamemanager.SlidePanelSetting(SettingPanel, originPos, 0);
                }
            }
        }
        //아래에서 위로 슬라이드
        if (endPos.y - startPos.y > swipeThreshold)
        {
            if (SettingPanel.gameObject.activeSelf && isOn)
            {
                isOn = false;
                gamemanager.SlidePanelSetting(SettingPanel, originPos, 1);
            }
        }
        else
        {
            if (isOK && SettingPanel.gameObject.activeSelf && isOn)
            {
                isOn = false;
                gamemanager.SlidePanelSetting(SettingPanel, originPos, 1);
            }
        }


    }
}
