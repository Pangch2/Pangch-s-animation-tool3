using UnityEngine;
using DG.Tweening;

public class SwitchEntities : MonoBehaviour
{
    public GameObject objectToDisable;
    public GameObject objectToEnable;

    [Header("�ִϸ��̼� ����")]
    [Tooltip("�ִϸ��̼� ��� �ð� (��)")]
    public float animationDuration = 0.15f;

    [Tooltip("���� ũ�� ���� (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    public float startScale = 0.8f;

    [Tooltip("ũ�� �ִϸ��̼� Ease Ÿ��")]
    public Ease scaleEaseType = Ease.OutQuad;

    [Space(10)]
    [Tooltip("���� ���İ� (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    public float startAlpha = 0f;

    [Tooltip("��ǥ ���İ� (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    public float endAlpha = 1f;

    [Tooltip("���� �ִϸ��̼� Ease Ÿ��")]
    public Ease alphaEaseType = Ease.OutQuad;

    public void SwitchObjects()
    {
        if (objectToDisable != null)
        {
            var cg = GetOrAddCanvasGroup(objectToDisable);

            // �ִϸ��̼�: ���� ���� �� ũ�� ���
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

            // �ִϸ��̼�: ���� ���� �� ũ�� ����
            DOTween.Sequence()
                .Join(objectToEnable.transform.DOScale(1f, animationDuration).SetEase(scaleEaseType))
                .Join(cg.DOFade(endAlpha, animationDuration).SetEase(alphaEaseType));
        }
    }

    // ĵ���� �׷��� ������ �߰����ִ� ���� �Լ�
    private CanvasGroup GetOrAddCanvasGroup(GameObject obj)
    {
        var cg = obj.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = obj.AddComponent<CanvasGroup>();
        return cg;
    }
}
