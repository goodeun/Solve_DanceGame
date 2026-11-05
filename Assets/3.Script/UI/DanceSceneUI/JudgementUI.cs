using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
public class JudgementUI : MonoBehaviour
{
    [SerializeField] private Sprite[] judgementSprites = new Sprite[4];
    [SerializeField] private Image judgement_img;

    private Vector2 AncorePos;
    private Color alpha;

    private Coroutine animCoroutine;

    private void Awake()
    {
        AncorePos = gameObject.transform.position;
        judgement_img = GetComponent<Image>();
        alpha = Color.white;
        alpha.a = 0;
    }

    private void OnEnable()
    {
        judgement_img.color = alpha;
    }

    public void JudgementUpdate(Judge judge)
    {
        if (judgementSprites == null || judgement_img == null)
        {
            return;
        }

        judgement_img.sprite = judgementSprites[(int)judge];

        if(animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
        }

        judgement_img.DOKill();
        transform.DOKill();

        animCoroutine = StartCoroutine(Anim_Judgement_co());

    }

    public IEnumerator Anim_Judgement_co()
    {
        judgement_img.color = alpha;
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