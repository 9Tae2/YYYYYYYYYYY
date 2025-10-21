using System.Collections;
using UnityEngine;

public class AreaAttackIndicator : MonoBehaviour
{
    [Header("Visual Settings")]
    public Material indicatorMaterial;
    public Color warningColor = Color.red;
    public Color finalColor = new Color(1f, 0f, 0f, 0.8f);

    [Header("Particle Effects")]
    [Tooltip("���� ���� �غ� �ܰ� ��ƼŬ ����Ʈ")]
    public GameObject warningParticleEffect;
    [Tooltip("���� ���� ���� �ܰ� ��ƼŬ ����Ʈ")]
    public GameObject explosionParticleEffect;
    [Tooltip("��ƼŬ ����Ʈ�� �ٴڿ��� �󸶳� ���� ��������")]
    public float particleHeightOffset = 0.1f;

    [Header("Animation Settings")]
    public float fadeInDuration = 0.5f;
    public float warningDuration = 1f;
    public float fadeOutDuration = 0.3f;
    public bool pulseEffect = true;
    public float pulseSpeed = 3f;

    private GameObject indicatorObject;
    private Renderer indicatorRenderer;
    private Material materialInstance;
    private GameObject currentWarningParticle;
    private GameObject currentExplosionParticle;

    public static AreaAttackIndicator CreateIndicator(Vector3 center, float radius, float totalDuration = 1.8f)
    {
        // �� GameObject ����
        GameObject indicatorParent = new GameObject("AreaAttackIndicator");
        indicatorParent.transform.position = center;

        // ���� ��� ����
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.transform.SetParent(indicatorParent.transform);
        cylinder.transform.localPosition = Vector3.up * 0.01f; // �ٴڿ��� ��¦ ����
        cylinder.transform.localScale = new Vector3(radius * 2, 0.01f, radius * 2);

        // Collider ���� (�ð��� �뵵��)
        DestroyImmediate(cylinder.GetComponent<Collider>());

        // ������Ʈ �߰�
        AreaAttackIndicator indicator = indicatorParent.AddComponent<AreaAttackIndicator>();
        indicator.indicatorObject = cylinder;
        indicator.indicatorRenderer = cylinder.GetComponent<Renderer>();

        // �⺻ ��Ƽ���� ����
        if (indicator.indicatorMaterial == null)
        {
            indicator.CreateDefaultMaterial();
        }

        indicator.materialInstance = new Material(indicator.indicatorMaterial);
        indicator.indicatorRenderer.material = indicator.materialInstance;

        // �ִϸ��̼� ����
        indicator.StartCoroutine(indicator.PlayIndicatorAnimation(totalDuration));

        return indicator;
    }

    // ��ƼŬ ȿ���� �Բ� �ε������� ���� (�����ε� �Լ�)
    public static AreaAttackIndicator CreateIndicatorWithParticles(Vector3 center, float radius,
        GameObject warningParticle = null, GameObject explosionParticle = null, float totalDuration = 1.8f)
    {
        AreaAttackIndicator indicator = CreateIndicator(center, radius, totalDuration);

        // ��ƼŬ ����Ʈ ����
        indicator.warningParticleEffect = warningParticle;
        indicator.explosionParticleEffect = explosionParticle;

        return indicator;
    }

    void CreateDefaultMaterial()
    {
        indicatorMaterial = new Material(Shader.Find("Standard"));
        indicatorMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        indicatorMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        indicatorMaterial.SetInt("_ZWrite", 0);
        indicatorMaterial.DisableKeyword("_ALPHATEST_ON");
        indicatorMaterial.EnableKeyword("_ALPHABLEND_ON");
        indicatorMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        indicatorMaterial.renderQueue = 3000;
        indicatorMaterial.color = warningColor;
    }

    IEnumerator PlayIndicatorAnimation(float totalDuration)
    {
        float elapsedTime = 0f;
        Color startColor = warningColor;
        startColor.a = 0f;

        // ��� �ܰ� ��ƼŬ ����Ʈ ����
        if (warningParticleEffect != null)
        {
            Vector3 particlePosition = transform.position + Vector3.up * particleHeightOffset;
            currentWarningParticle = Instantiate(warningParticleEffect, particlePosition, transform.rotation);
        }

        // 1�ܰ�: ���̵� ��
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeInDuration;

            Color currentColor = Color.Lerp(startColor, warningColor, progress);
            materialInstance.color = currentColor;

            yield return null;
        }

        // 2�ܰ�: ��� �ܰ� (�޽� ȿ��)
        elapsedTime = 0f;
        while (elapsedTime < warningDuration)
        {
            elapsedTime += Time.deltaTime;

            if (pulseEffect)
            {
                float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.3f + 0.7f;
                Color pulseColor = warningColor;
                pulseColor.a *= pulse;
                materialInstance.color = pulseColor;
            }

            yield return null;
        }

        // 3�ܰ�: ���� �������� ���� + ���� ��ƼŬ
        materialInstance.color = finalColor;

        // ��� ��ƼŬ ����
        if (currentWarningParticle != null)
        {
            Destroy(currentWarningParticle);
        }

        // ���� ��ƼŬ ����Ʈ ����
        if (explosionParticleEffect != null)
        {
            Vector3 particlePosition = transform.position + Vector3.up * particleHeightOffset;
            currentExplosionParticle = Instantiate(explosionParticleEffect, particlePosition, transform.rotation);
        }

        yield return new WaitForSeconds(0.2f);

        // 4�ܰ�: ���̵� �ƿ�
        elapsedTime = 0f;
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeOutDuration;

            Color currentColor = Color.Lerp(finalColor, Color.clear, progress);
            materialInstance.color = currentColor;

            yield return null;
        }

        // ������Ʈ �ı�
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (materialInstance != null)
        {
            DestroyImmediate(materialInstance);
        }

        // ��ƼŬ ����
        if (currentWarningParticle != null)
        {
            Destroy(currentWarningParticle);
        }

        if (currentExplosionParticle != null)
        {
            Destroy(currentExplosionParticle);
        }
    }
}