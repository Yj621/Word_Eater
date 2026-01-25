using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MathQuizMiniGame : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private Button[] answerButtons;          // 3개
    [SerializeField] private TextMeshProUGUI[] answerTexts;   // 버튼에 붙은 TMP(3개)

    [Header("Rule")]
    [SerializeField] private int minValue = 1;   // A,B,C 최소값
    [SerializeField] private int maxValue = 999; // A,B,C 최대값
    [SerializeField] private int minAnswer = 1;  // 결과 최소(1자리 이상)
    [SerializeField] private int maxAnswer = 999;// 결과 최대(3자리)

    [Header("Generation")]
    [SerializeField] private int maxTry = 2000;  // 문제 생성 시도 횟수
    [SerializeField] private bool allowNegativeIntermediate = false; // 중간 결과 음수 허용 여부

    private MiniGameHook _hook;

    // 현재 정답
    private int _correctAnswer;

    // 버튼 리스너 관리용(클린하게)
    private readonly List<UnityEngine.Events.UnityAction> _actions = new();

    private void Awake()
    {
        _hook = GetComponent<MiniGameHook>();
    }

    private void OnEnable()
    {
        BindButtons();
        GenerateAndShow();
    }

    private void OnDisable()
    {
        UnbindButtons();
    }

    void BindButtons()
    {
        if (answerButtons == null) return;

        UnbindButtons();
        _actions.Clear();

        for (int i = 0; i < answerButtons.Length; i++)
        {
            int idx = i;
            UnityEngine.Events.UnityAction act = () => OnPick(idx);
            _actions.Add(act);
            answerButtons[i].onClick.AddListener(act);
        }
    }

    void UnbindButtons()
    {
        if (answerButtons == null || _actions.Count == 0) return;

        for (int i = 0; i < answerButtons.Length && i < _actions.Count; i++)
        {
            if (answerButtons[i])
                answerButtons[i].onClick.RemoveListener(_actions[i]);
        }
        _actions.Clear();
    }

    void OnPick(int btnIndex)
    {
        if (btnIndex < 0 || btnIndex >= answerTexts.Length) return;

        if (!int.TryParse(answerTexts[btnIndex].text, out int picked))
        {
            // 파싱 실패는 그냥 실패 처리(디자인 상 숫자만 들어갈 거라 거의 없음)
            _hook?.ReportFail();
            return;
        }

        if (picked == _correctAnswer) _hook?.ReportClear();
        else _hook?.ReportFail();
    }

    void GenerateAndShow()
    {
        if (!questionText || answerButtons == null || answerButtons.Length < 3 || answerTexts == null || answerTexts.Length < 3)
        {
            // Debug.LogError("[MathQuizMiniGame] UI 레퍼런스가 부족해.");
            _hook?.ReportFail();
            return;
        }

        if (!TryGenerateValidQuestion(out var q))
        {
            // Debug.LogWarning("[MathQuizMiniGame] 유효한 문제 생성 실패");
            _hook?.ReportFail();
            return;
        }

        // 문제 표시
        questionText.text = q.formatted;
        _correctAnswer = q.answer;

        // 보기 3개 만들기
        int wrong1 = MakeWrongAnswer(_correctAnswer);
        int wrong2 = MakeWrongAnswer(_correctAnswer, wrong1);

        // 섞어서 배치
        int[] choices = { _correctAnswer, wrong1, wrong2 };
        Shuffle(choices);

        for (int i = 0; i < 3; i++)
            answerTexts[i].text = choices[i].ToString();
    }

    struct Question
    {
        public int a, b, c;
        public char op1, op2;
        public int answer;
        public string formatted;
    }

    bool TryGenerateValidQuestion(out Question q)
    {
        q = default;

        for (int t = 0; t < maxTry; t++)
        {
            int a = Random.Range(minValue, maxValue + 1);
            int b = Random.Range(minValue, maxValue + 1);
            int c = Random.Range(minValue, maxValue + 1);

            char op1 = RandomOp();
            char op2 = RandomOp();

            // 중간/최종 계산 (우선순위 고려)
            int ans = 0;
            bool calcSuccess = false;

            // op2가 곱셈/나눗셈이고 op1이 덧셈/뺄셈이면 op2 먼저 계산
            bool op2First = (op2 == '*' || op2 == '/') && (op1 == '+' || op1 == '-');

            if (op2First)
            {
                // 뒤쪽 연산 먼저 (b op2 c)
                if (TryApply(b, op2, c, out int tail))
                {
                    if (!allowNegativeIntermediate && tail < 0) continue;
                    // 앞쪽 연산 (a op1 tail)
                    if (TryApply(a, op1, tail, out ans))
                    {
                        calcSuccess = true;
                    }
                }
            }
            else
            {
                // 앞쪽 연산 먼저 (a op1 b) -> 기본 순서
                if (TryApply(a, op1, b, out int head))
                {
                    if (!allowNegativeIntermediate && head < 0) continue;
                    // 뒤쪽 연산 (head op2 c)
                    if (TryApply(head, op2, c, out ans))
                    {
                        calcSuccess = true;
                    }
                }
            }

            if (!calcSuccess) continue;

            // 최종 답 조건(1~3자리)
            if (ans < minAnswer || ans > maxAnswer) continue;

            // 출력 포맷: ? * ? * ? = ?
            // (원하면 결과를 숨기고 ?로 보여주려고)
            string formatted = $"{a} {op1} {b} {op2} {c} = ?";

            q = new Question
            {
                a = a,
                b = b,
                c = c,
                op1 = op1,
                op2 = op2,
                answer = ans,
                formatted = formatted
            };
            return true;
        }

        return false;
    }

    char RandomOp()
    {
        // 사칙연산 전부
        int r = Random.Range(0, 4);
        return r switch
        {
            0 => '+',
            1 => '-',
            2 => '*',
            _ => '/',
        };
    }

    bool TryApply(int left, char op, int right, out int result)
    {
        result = 0;

        switch (op)
        {
            case '+':
                result = left + right;
                return true;

            case '-':
                result = left - right;
                return true;

            case '*':
                // int overflow 거의 안 나게 maxValue 999면 안전권이긴 함
                result = left * right;
                return true;

            case '/':
                // 0 나누기 방지 + 정수로 딱 떨어지는 경우만 허용
                if (right == 0) return false;
                if (left % right != 0) return false;
                result = left / right;
                return true;
        }

        return false;
    }

    int MakeWrongAnswer(int correct, int alsoNot = int.MinValue)
    {
        // 정답 근처에서 흔들되, 1~999 범위를 유지
        // 너무 단순하면 티나니까 여러 방식 섞음
        for (int i = 0; i < 200; i++)
        {
            int r = Random.Range(0, 4);
            int candidate;

            if (r == 0)
            {
                // 근처 오프셋
                int delta = Random.Range(-50, 51);
                if (delta == 0) delta = 7;
                candidate = correct + delta;
            }
            else if (r == 1)
            {
                // 자리 바꾸기 느낌
                candidate = (correct * 10 + Random.Range(0, 10)) % 1000;
            }
            else if (r == 2)
            {
                // 완전 랜덤
                candidate = Random.Range(minAnswer, maxAnswer + 1);
            }
            else
            {
                // 배수/나눗셈 느낌
                int mul = Random.Range(2, 6);
                candidate = correct * mul;
            }

            if (candidate < minAnswer || candidate > maxAnswer) continue;
            if (candidate == correct) continue;
            if (candidate == alsoNot) continue;

            return candidate;
        }

        // 최후 fallback
        int fallback = correct + 1;
        if (fallback > maxAnswer) fallback = correct - 1;
        if (fallback < minAnswer) fallback = minAnswer;
        if (fallback == correct) fallback = minAnswer;
        if (fallback == alsoNot) fallback = minAnswer + 2;
        return Mathf.Clamp(fallback, minAnswer, maxAnswer);
    }

    void Shuffle(int[] arr)
    {
        for (int i = 0; i < arr.Length; i++)
        {
            int r = Random.Range(i, arr.Length);
            (arr[i], arr[r]) = (arr[r], arr[i]);
        }
    }
}
