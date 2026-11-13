using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MirrorBallUIPulse : MonoBehaviour
{
    [SerializeField] private Transform targetTransform;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }
    private void Start()
    {
        targetTransform = FindAnyObjectByType<FeverAnimation>().transform;
    }
    private void LateUpdate()
    {
        if (targetTransform != null)
        {
            rectTransform.localScale = targetTransform.localScale;
        }
    }
}
