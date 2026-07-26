using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Ghost Cluck: chases the farmer (via ChickenWander) and haunts with lights-out
/// when close enough and the white cooldown hits zero.
/// </summary>
public class GhostChicken : MonoBehaviour
{
    [Header("Haunt")]
    [SerializeField] private float cooldownSeconds = 5f;
    [SerializeField] private float hauntDuration = 2.5f;
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private int maxHaunts = 2;

    [Header("UI")]
    [SerializeField] private TextMeshPro text;

    [Header("Optional VFX")]
    [SerializeField] private GameObject hauntEffect;

    private ChickenWander wander;
    private float cooldown;
    private bool haunting;
    private int hauntCount;
    private Coroutine hauntRoutine;

    public bool IsHaunting => haunting;

    private void Awake()
    {
        wander = GetComponent<ChickenWander>();
        EnsureTimerText();
        if (text != null)
            text.color = Color.white;

        if (hauntEffect != null)
            hauntEffect.SetActive(false);
    }

    private void OnEnable()
    {
        if (wander != null)
            wander.RefreshTypeFlags();

        cooldown = cooldownSeconds;
        haunting = false;
        hauntCount = 0;
        RefreshTimerLabel();
    }

    private void OnDisable()
    {
        if (hauntRoutine != null)
        {
            StopCoroutine(hauntRoutine);
            hauntRoutine = null;
        }

        EndHauntVisual();
        haunting = false;
    }

    private void Update()
    {
        if (haunting)
            return;

        if (cooldown > 0f)
        {
            cooldown -= Time.deltaTime;
            if (cooldown < 0f)
                cooldown = 0f;
            RefreshTimerLabel();
        }

        if (cooldown > 0f)
            return;

        if (!IsCloseToFarmer())
            return;

        hauntRoutine = StartCoroutine(HauntRoutine());
    }

    private IEnumerator HauntRoutine()
    {
        haunting = true;
        if (text != null)
            text.text = string.Empty;

        Transform farmer = wander != null ? wander.farmerTransform : null;
        HauntVisionController ctrl = HauntVisionController.EnsureExists(farmer);
        ctrl.BeginHaunt(this);

        if (hauntEffect != null)
            hauntEffect.SetActive(true);

        yield return new WaitForSeconds(hauntDuration);

        EndHauntVisual();
        haunting = false;
        hauntCount++;
        hauntRoutine = null;

        if (hauntCount >= maxHaunts)
        {
            Destroy(gameObject);
            yield break;
        }

        cooldown = cooldownSeconds;
        RefreshTimerLabel();
    }

    private void EndHauntVisual()
    {
        if (HauntVisionController.Instance != null)
            HauntVisionController.Instance.EndHaunt(this);

        if (hauntEffect != null)
            hauntEffect.SetActive(false);
    }

    private bool IsCloseToFarmer()
    {
        if (wander == null || wander.farmerTransform == null)
            return false;

        float dist = Vector2.Distance(transform.position, wander.farmerTransform.position);
        return dist <= attackRange;
    }

    private void RefreshTimerLabel()
    {
        if (text == null || haunting)
            return;

        text.color = Color.white;
        text.text = Mathf.CeilToInt(cooldown).ToString();
    }

    private void EnsureTimerText()
    {
        if (text != null)
            return;

        text = GetComponentInChildren<TextMeshPro>();
        if (text != null)
        {
            text.color = Color.white;
            return;
        }

        GameObject go = new GameObject("Timer");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0.4f, 1.94f, 0f);

        text = go.AddComponent<TextMeshPro>();
        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = 4f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.text = Mathf.CeilToInt(cooldownSeconds).ToString();
        text.sortingOrder = 20;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
#endif
}
