using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
public class JudgementUI : MonoBehaviour
{
    [SerializeField] private Sprite[] judgementSprites = new Sprite[4];

    private Image judgement_img;

    private Color judgement_color;
    private Vector2 AncorePos;

    private void Awake()
    {
        if (TryGetComponent(out judgement_img))
        {
            judgement_color = judgement_img.color;
        }
        AncorePos = gameObject.transform.position;
    }

    private void OnEnable()
    {
        judgement_color.a = 0f;
    }

    public void JudgementUpdate(Judge judge)
    {
        if (judgementSprites == null || judgement_img == null)
        {
            return;
        }

        judgement_img.sprite = judgementSprites[(int)judge];

        StopCoroutine(Anim_Judgement_co());
        StartCoroutine(Anim_Judgement_co());

    }

    public IEnumerator Anim_Judgement_co()
    {
        judgement_color.a = 0f;
        transform.position = new Vector2(AncorePos.x, AncorePos.y - 20f);

        judgement_img.DOFade(1f, 0.1f);
        transform.DOMove(AncorePos, 0.2f).SetEase(Ease.OutElastic);

        yield return new WaitForSeconds(0.3f);

        judgement_img.DOFade(0, 0.1f);
    }

    #region µð¹ö±×
    public void DEBUG_JudgementTest()
    {
        StartCoroutine(DEBUG_JudgementTest_co());
    }

    public IEnumerator DEBUG_JudgementTest_co()
    {
        for (int i = 0; i < 5; i++)
        {
            JudgementUpdate(Judge.Just);
            yield return new WaitForSeconds(0.6f);

            JudgementUpdate(Judge.Good);
            yield return new WaitForSeconds(0.6f);

            JudgementUpdate(Judge.Bad);
            yield return new WaitForSeconds(0.6f);

            JudgementUpdate(Judge.Miss);
            yield return new WaitForSeconds(0.6f);
        }
    }
    #endregion
}