using UnityEngine;
using DG.Tweening;

public class SwitchEntities : MonoBehaviour
{
    public GameObject objectToDisable;
    public GameObject objectToEnable;

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
        if (objectToDisable != null)
        {
            var cg = GetOrAddCanvasGroup(objectToDisable);

            // 애니메이션: 알파 감소 및 크기 축소
            DOTween.Sequence()
                .Join(cg.DOFade(startAlpha, animationDuration).SetEase(alphaEaseType))
                .Join(objectToDisable.transform.DOScale(startScale, animationDuration).SetEase(scaleEaseType))
                .OnComplete(() => objectToDisable.SetActive(false));
        }

        if (objectToEnable != null)
        {
            objectToEnable.SetActive(true);
            objectToEnable.transform.localScale = Vector3.one * startScale;

            var cg = GetOrAddCanvasGroup(objectToEnable);
            cg.alpha = startAlpha;

            // 애니메이션: 알파 증가 및 크기 증가
            DOTween.Sequence()
                .Join(objectToEnable.transform.DOScale(1f, animationDuration).SetEase(scaleEaseType))
                .Join(cg.DOFade(endAlpha, animationDuration).SetEase(alphaEaseType));
        }
    }

    // 캔버스 그룹이 없으면 추가해주는 헬퍼 함수
    private CanvasGroup GetOrAddCanvasGroup(GameObject obj)
    {
        var cg = obj.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = obj.AddComponent<CanvasGroup>();
        return cg;
    }
}
