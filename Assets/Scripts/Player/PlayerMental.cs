using UnityEngine;
using System.Collections;

public class PlayerMental : MonoBehaviour
{
    [Header("���ŷ� ����")]
    public float maxMentalHealth = 100f;
    public float currentMentalHealth;
    public float mentalDecayRate = 0.56f; // �ʴ� 0.56�� ���� (3��)
    public float groomingRecoveryRate = 20f; // �׷�� �� �ʴ� 20�� ȸ�� (5��)

    [Header("UI ����")]
    public MentalBarUI mentalHealthUI; // ���ŷ� UI

    private bool isGrooming = false;
    private bool isTired = false; // ���ŷ� �� ����
    private PlayerController playerController;
    private Animator animator;

    void Start()
    {
        // �ʱ� ���ŷ� ����
        currentMentalHealth = maxMentalHealth;

        // ������Ʈ ����
        playerController = GetComponent<PlayerController>();
        animator = GetComponent<Animator>();

        // ���ŷ� UI ������Ʈ
        UpdateMentalUI();

        // ���ŷ� ���� �ڷ�ƾ ����
        StartCoroutine(MentalHealthDecay());
    }

    void Update()
    {
        CheckGroomingStatus();
    }

    void CheckGroomingStatus()
    {
        // �׷�� ���� Ȯ�� (QŰ)
        bool wasGrooming = isGrooming;
        isGrooming = Input.GetKey(KeyCode.Q);

        // �׷�� ����/���� �α�
        if (isGrooming && !wasGrooming)
        {
            Debug.Log("�׷�� ���� - ���ŷ� ȸ�� ��!");
            if (mentalHealthUI != null)
                mentalHealthUI.ShowGroomingEffect();
        }
        else if (!isGrooming && wasGrooming)
        {
            Debug.Log("�׷�� ����");
            if (mentalHealthUI != null)
                mentalHealthUI.HideGroomingEffect();
        }
    }

    IEnumerator MentalHealthDecay()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f); // 1�ʸ��� ����

            if (isGrooming)
            {
                // �׷�� ���̸� ���ŷ� ȸ��
                RecoverMentalHealth(groomingRecoveryRate);
            }
            else
            {
                // �׷�� ���� �ƴϸ� ���ŷ� ����
                DecreaseMentalHealth(mentalDecayRate);
            }
        }
    }

    // ���ŷ� ����
    public void DecreaseMentalHealth(float amount)
    {
        currentMentalHealth -= amount;
        currentMentalHealth = Mathf.Clamp(currentMentalHealth, 0, maxMentalHealth);

        UpdateMentalUI();

        // ���ŷ� �� üũ
        CheckTiredState();
    }

    // ���ŷ� ȸ��
    public void RecoverMentalHealth(float amount)
    {
        currentMentalHealth += amount;
        currentMentalHealth = Mathf.Clamp(currentMentalHealth, 0, maxMentalHealth);

        UpdateMentalUI();

        // ���ŷ� ȸ�� üũ
        CheckTiredState();
    }

    // �Ƿ� ���� Ȯ�� �� ó��
    void CheckTiredState()
    {
        bool wasTired = isTired;
        isTired = currentMentalHealth <= 0;

        // �Ƿ� ���� ��ȭ ��
        if (isTired && !wasTired)
        {
            // �Ƿ� ���� ����
            Debug.Log("���ŷ� ��! �Ƿ� ���� ����");
            OnTiredStart();
        }
        else if (!isTired && wasTired)
        {
            // �Ƿ� ���� ����
            Debug.Log("���ŷ� ȸ��! �Ƿ� ���� ����");
            OnTiredEnd();
        }
    }

    // �Ƿ� ���� ����
    void OnTiredStart()
    {
        if (animator != null)
        {
            animator.SetBool("IsTired", true);
        }

        // �÷��̾� �ɷ� ���� (PlayerController���� ����)
    }

    // �Ƿ� ���� ����  
    void OnTiredEnd()
    {
        if (animator != null)
        {
            animator.SetBool("IsTired", false);
        }
    }

    // ���ŷ� UI ������Ʈ
    void UpdateMentalUI()
    {
        if (mentalHealthUI != null)
        {
            mentalHealthUI.UpdateMentalHealthBar(currentMentalHealth, maxMentalHealth);
        }
        else
        {
            Debug.Log($"���ŷ�: {currentMentalHealth:F1}/{maxMentalHealth}"); // UI ���� �� �α׷� ǥ��
        }
    }

    // �׽�Ʈ�� �޼���� (Inspector���� ��Ŭ������ ���� ����)
    [ContextMenu("�׽�Ʈ: 10 ���ŷ� ����")]
    public void TestMentalDamage()
    {
        DecreaseMentalHealth(10f);
    }

    [ContextMenu("�׽�Ʈ: 20 ���ŷ� ȸ��")]
    public void TestMentalRecover()
    {
        RecoverMentalHealth(20f);
    }

    [ContextMenu("�׽�Ʈ: ���ŷ� ��� �Ҹ�")]
    public void TestMentalEmpty()
    {
        DecreaseMentalHealth(currentMentalHealth);
    }

    // �ܺο��� ȣ�� ������ �޼����
    public float GetMentalHealthPercentage()
    {
        return currentMentalHealth / maxMentalHealth;
    }

    public bool IsMentallyHealthy()
    {
        return currentMentalHealth > 0;
    }

    public bool IsGrooming()
    {
        return isGrooming;
    }

    public bool IsTired()
    {
        return isTired;
    }
}