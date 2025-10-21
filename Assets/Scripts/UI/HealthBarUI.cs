using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("UI ������Ʈ")]
    public Canvas healthCanvas; // World Space Canvas
    public Image healthBarFill; // ü�¹� Fill �̹���
    public Image healthBarBackground; // ü�¹� ���

    [Header("���� ����")]
    public Color fullHealthColor = Color.green;    // ü�� Ǯ�� �� (�ʷϻ�)
    public Color halfHealthColor = Color.yellow;   // ü�� ������ �� (�����)
    public Color lowHealthColor = Color.red;       // ü�� ���� �� (������)

    [Header("����")]
    public Vector3 offset = new Vector3(0, 2f, 0); // ĳ���ͷκ����� ������
    public bool hideWhenFull = true; // ü���� Ǯ�� �� �����
    public float hideDelay = 3f; // Ǯ ü�� �� ���������� �ð�

    private Transform player;
    private Camera mainCamera;
    private float lastDamageTime;
    private bool isVisible = true;

    void Start()
    {
        // ī�޶� ã��
        mainCamera = Camera.main;

        // Canvas ����
        if (healthCanvas == null)
            healthCanvas = GetComponent<Canvas>();

        if (healthCanvas != null)
        {
            healthCanvas.renderMode = RenderMode.WorldSpace;
            healthCanvas.worldCamera = mainCamera;

            // Canvas ũ�⸦ ���� ũ��� (�ʹ� ���� �ʰ�)
            healthCanvas.transform.localScale = Vector3.one;
        }

        // ������ ���̱� (�׽�Ʈ��)
        SetVisible(true);
        Debug.Log("ü�¹� �ʱ�ȭ �Ϸ�!");
    }

    void Update()
    {
        // ī�޶� ���� ȸ�� (������ ȿ��)
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                           mainCamera.transform.rotation * Vector3.up);
        }

        // Player�� ���� ������Ʈ�̹Ƿ� �ڵ����� ����ٴ�

        // �ڵ� ����� ó��
        if (hideWhenFull && isVisible && Time.time - lastDamageTime > hideDelay)
        {
            if (healthBarFill != null && healthBarFill.fillAmount >= 1f)
            {
                SetVisible(false);
            }
        }
    }

    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (healthBarFill == null) return;

        float healthPercent = currentHealth / maxHealth;
        healthBarFill.fillAmount = healthPercent;

        // ü�� �ۼ�Ʈ�� ���� ���� ����
        Color targetColor = GetHealthColor(healthPercent);
        healthBarFill.color = targetColor;

        // �׻� ���̰� (�׽�Ʈ��)
        SetVisible(true);
        lastDamageTime = Time.time;

        Debug.Log($"ü�¹� ������Ʈ: {healthPercent:P0}, ����: {targetColor}");
    }

    Color GetHealthColor(float healthPercent)
    {
        if (healthPercent > 0.6f)
        {
            // 100% ~ 60%: �ʷϻ� �� ��������� ��ȭ
            float t = (1f - healthPercent) / 0.4f; // 0~1�� ����ȭ
            return Color.Lerp(fullHealthColor, halfHealthColor, t);
        }
        else if (healthPercent > 0.3f)
        {
            // 60% ~ 30%: ����� �� ���������� ��ȭ
            float t = (0.6f - healthPercent) / 0.3f; // 0~1�� ����ȭ
            return Color.Lerp(halfHealthColor, lowHealthColor, t);
        }
        else
        {
            // 30% ����: ������
            return lowHealthColor;
        }
    }

    void SetVisible(bool visible)
    {
        if (healthCanvas != null)
            healthCanvas.gameObject.SetActive(visible);
        isVisible = visible;
    }

    // ������ ���̱�/�����
    public void Show()
    {
        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
    }
}