using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class SwitchEntities : MonoBehaviour
{
    public List<GameObject> objectsToDisable = new List<GameObject>();
    public List<GameObject> objectsToEnable = new List<GameObject>();

    [Header("애니메이션 설정")]
    [Tooltip("애니메이션 재생 시간 (초)")]
    public float animationDuration = 0.15f;

    [Tooltip("시작 크기 비율 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    public float startScale = 0.8f;

    [Tooltip("크기 애니메이션 Ease 타입")]
    public Ease scaleEaseType = Ease.OutQuad;

    [Space(10)]
    [Tooltip("시작 알파값 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    public float startAlpha = 0f;

    [Tooltip("목표 알파값 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    public float endAlpha = 1f;

    [Tooltip("알파 애니메이션 Ease 타입")]
    public Ease alphaEaseType = Ease.OutQuad;

    public void SwitchObjects()
    {
        foreach (var obj in objectsToDisable)
        {
            if (obj != null)
            {
                var cg = GetOrAddCanvasGroup(obj);

                DOTween.Sequence()
                    .Join(cg.DOFade(startAlpha, animationDuration).SetEase(alphaEaseType))
                    .Join(obj.transform.DOScale(startScale, animationDuration).SetEase(scaleEaseType))
                    .OnComplete(() => obj.SetActive(false));
            }
        }

        foreach (var obj in objectsToEnable)
        {
            if (obj != null)
            {
                obj.SetActive(true);
                obj.transform.localScale = Vector3.one * startScale;

                var cg = GetOrAddCanvasGroup(obj);
                cg.alpha = startAlpha;

                DOTween.Sequence()
                    .Join(obj.transform.DOScale(1f, animationDuration).SetEase(scaleEaseType))
                    .Join(cg.DOFade(endAlpha, animationDuration).SetEase(alphaEaseType));
            }
        }
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject obj)
    {
        var cg = obj.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = obj.AddComponent<CanvasGroup>();
        return cg;
    }
}
