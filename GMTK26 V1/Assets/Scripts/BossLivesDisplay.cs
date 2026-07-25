using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives the Lives prefab heart sprites: lost hearts turn grey.
/// Clones hearts if max lives exceeds the prefab count.
/// </summary>
public class BossLivesDisplay : MonoBehaviour
{
    [SerializeField] private Color aliveColor = Color.white;
    [SerializeField] private Color deadColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    [SerializeField] private float heartSpacing = 0.45f;

    private readonly List<SpriteRenderer> hearts = new List<SpriteRenderer>();
    private int maxLives;

    public void Initialize(int max)
    {
        maxLives = Mathf.Max(1, max);
        hearts.Clear();

        SpriteRenderer[] renders = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renders.Length; i++)
        {
            if (renders[i] != null && renders[i].gameObject != gameObject)
                hearts.Add(renders[i]);
        }

        hearts.Sort((a, b) => a.transform.localPosition.x.CompareTo(b.transform.localPosition.x));

        while (hearts.Count > maxLives)
        {
            SpriteRenderer extra = hearts[hearts.Count - 1];
            hearts.RemoveAt(hearts.Count - 1);
            if (extra != null)
                Destroy(extra.gameObject);
        }

        // Duplicate the first heart until we have enough for maxLives.
        if (hearts.Count > 0)
        {
            SpriteRenderer template = hearts[0];
            float startX = template.transform.localPosition.x;
            while (hearts.Count < maxLives)
            {
                GameObject clone = Instantiate(template.gameObject, template.transform.parent);
                clone.name = "Heart (" + hearts.Count + ")";
                Vector3 lp = template.transform.localPosition;
                lp.x = startX + hearts.Count * heartSpacing;
                clone.transform.localPosition = lp;
                hearts.Add(clone.GetComponent<SpriteRenderer>());
            }

            // Recenter the row around local origin.
            float totalWidth = (maxLives - 1) * heartSpacing;
            float left = -totalWidth * 0.5f;
            for (int i = 0; i < hearts.Count; i++)
            {
                Vector3 lp = hearts[i].transform.localPosition;
                lp.x = left + i * heartSpacing;
                hearts[i].transform.localPosition = lp;
            }
        }
    }

    public void SetRemaining(int remaining)
    {
        remaining = Mathf.Clamp(remaining, 0, maxLives > 0 ? maxLives : hearts.Count);

        for (int i = 0; i < hearts.Count; i++)
        {
            if (hearts[i] == null)
                continue;

            hearts[i].color = i < remaining ? aliveColor : deadColor;
        }
    }
}
