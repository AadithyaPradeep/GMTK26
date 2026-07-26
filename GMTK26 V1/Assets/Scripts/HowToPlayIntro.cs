using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Shows the scene's existing "How To Play" canvas above the scene fader.
/// Enter dismisses it so World 1 can reveal and start.
/// </summary>
public static class HowToPlayIntro
{
    public static bool IsShowing { get; private set; }

    public static GameObject FindInScene()
    {
        Transform[] all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == null)
                continue;
            if (t.name != "How To Play" && t.name != "HowToPlay")
                continue;
            if (!t.gameObject.scene.IsValid() || !t.gameObject.scene.isLoaded)
                continue;
            return t.gameObject;
        }

        return null;
    }

    public static void HideIfPresent()
    {
        GameObject go = FindInScene();
        if (go != null)
            go.SetActive(false);
        IsShowing = false;
    }

    public static IEnumerator Play(GameObject howToPlayRoot)
    {
        if (howToPlayRoot == null)
            yield break;

        IsShowing = true;

        howToPlayRoot.SetActive(true);
        howToPlayRoot.transform.localScale = Vector3.one;

        CanvasGroup group = howToPlayRoot.GetComponent<CanvasGroup>();
        if (group == null)
            group = howToPlayRoot.AddComponent<CanvasGroup>();

        // Above SceneFader (9999) so HTP is visible while the world is still held black.
        Canvas canvas = howToPlayRoot.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 10050;
        }

        group.alpha = 0f;
        group.interactable = true;
        group.blocksRaycasts = true;

        yield return Fade(group, 0f, 1f, 0.35f);

        // Ignore Enter for a moment so a held/buffered key doesn't skip instantly.
        float arm = 0.2f;
        while (arm > 0f)
        {
            arm -= Time.unscaledDeltaTime;
            yield return null;
        }

        while (true)
        {
            if (Keyboard.current != null
                && (Keyboard.current.enterKey.wasPressedThisFrame
                    || Keyboard.current.numpadEnterKey.wasPressedThisFrame))
                break;
            yield return null;
        }

        yield return Fade(group, 1f, 0f, 0.3f);

        howToPlayRoot.SetActive(false);
        IsShowing = false;

        // Consume leftover Enter so the next scene doesn't see a buffered press.
        float settle = 0.15f;
        while (settle > 0f)
        {
            settle -= Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / duration);
            group.alpha = Mathf.Lerp(from, to, n);
            yield return null;
        }

        group.alpha = to;
    }
}
