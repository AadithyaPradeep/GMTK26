using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives the Lives prefab heart sprites: lost hearts turn grey.
/// </summary>
public class BossLivesDisplay : MonoBehaviour
{
    [SerializeField] private Color aliveColor = Color.white;
    [SerializeField] private Color deadColor = new Color(0.35f, 0.35f, 0.35f, 1f);

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

        // Prefer exactly maxLives hearts if more exist.
        while (hearts.Count > maxLives)
            hearts.RemoveAt(hearts.Count - 1);
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
