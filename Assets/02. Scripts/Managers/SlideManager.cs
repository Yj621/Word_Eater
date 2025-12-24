using UnityEngine;

public class SlideManager : MonoBehaviour
{
    private Vector2 startPos;
    private Vector2 endPos;

    public GameManager gamemanager;

    public RectTransform SettingPanel;
    Vector2 originPos;


    public bool isOK = true;
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

            if (Input.GetMouseButtonUp(0))
            {
                endPos = Input.mousePosition;

                DetectSwipe();
            }

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Began)
                    startPos = touch.position;

                if (touch.phase == TouchPhase.Ended)
                {
                    endPos = touch.position;
                    DetectSwipe();
                }
            }
    }

    private void DetectSwipe()
    {

            float swipeY = startPos.y - endPos.y;

            float swipeThreshold = Screen.height * 0.5f;

        if (swipeY > swipeThreshold)
        {
            if (isOK)
            {
                SettingPanel.anchoredPosition = originPos + Vector2.up * Screen.height;

                gamemanager.SlidePanelSetting(SettingPanel, originPos,0);
            }
        }

        //아래에서 위로 슬라이드
        else if (endPos.y - startPos.y > swipeThreshold) {
            if (SettingPanel.gameObject.activeSelf) {
                gamemanager.SlidePanelSetting(SettingPanel, originPos,1);
            }
        }

    }
}
