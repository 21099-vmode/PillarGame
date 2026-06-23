using UnityEngine;
using TMPro;

public class QuizRoundManager : MonoBehaviour
{
    [Header("UI Результатов")]
    public TMP_Text roundNumberText;     public TMP_Text resultsText;     
    private int currentRound = 0;

    public void NextRoundStart()
    {
        currentRound++;
        if (roundNumberText != null)
            roundNumberText.text = $"РАУНД: {currentRound}";
    }

    public void ShowRoundResults(string deadBotName)
    {
        if (resultsText != null)
        {
            if (string.IsNullOrEmpty(deadBotName))
                resultsText.text = "Все выжили в этом раунде!";
            else
                resultsText.text = $"{deadBotName} ответил неверно и упал в лаву!";
        }
    }
}