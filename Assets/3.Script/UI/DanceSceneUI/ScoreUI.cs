using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class ScoreUI : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI score_t;
    [SerializeField] private TextMeshProUGUI addScore_t;

    private int cachingScore;
    private Vector2 addScore_ancore;
    private Sequence seq;
    private void OnEnable()
    {
        addScore_ancore = addScore_t.transform.position;
        score_t.text = "0";
        addScore_t.text = "0";
        addScore_t.alpha = 0f;
    }
    public void ScoreUpdate(int totalScore)
    {
        score_t.text = $"{totalScore:N0}";
        ScorePlus(totalScore);
    }

    public void ScorePlus(int totalScore)
    {
        int additiveScore;

        if (totalScore > cachingScore)
        {
            additiveScore = totalScore - cachingScore;
            addScore_t.text = $"{additiveScore:N0}";

            addScore_t.transform.position = addScore_ancore;
            addScore_t.alpha = 0f;

            seq.Kill();

            seq = DOTween.Sequence();

            seq.Append(addScore_t.transform.DOMove(new Vector2(addScore_ancore.x, addScore_ancore.y - 5f), 0.2f))
                .Join(addScore_t.DOFade(1f, 0.2f))
                .Append(addScore_t.DOFade(0f, 0.3f));
        }

        cachingScore = totalScore;
    }

    #region µð¹ö±×

    public void DEBUG_ScoreUpdate()
    {
        StartCoroutine(DEBUG_ScoreUpdate_co());
    }

    public IEnumerator DEBUG_ScoreUpdate_co()
    {

        ScoreUpdate(11000);

        yield return new WaitForSeconds(3f);

        ScoreUpdate(11002);

        yield return new WaitForSeconds(3f);

        ScoreUpdate(13000);
    }

    #endregion
}
