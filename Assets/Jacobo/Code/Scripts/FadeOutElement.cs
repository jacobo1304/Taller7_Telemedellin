using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeOutElement : MonoBehaviour
{
    [Header("UI Fade (CanvasGroup)")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField, Min(0f)] private float defaultDuration = 0.35f;
    [SerializeField] private Ease ease = Ease.OutQuad;
    [SerializeField] private bool controlRaycast = true;

    [Header("3D Fade")]
    [SerializeField, Min(0f)] private float default3DDuration = 0.35f;
    [SerializeField] private bool includeInactiveChildren = false;
    [Tooltip("Si está activo, modifica sharedMaterials (afecta todos los objetos que usan ese material).")]
    [SerializeField] private bool fadeSharedMaterials = false;
    [SerializeField] private bool disableRenderersOnComplete = true;
    [SerializeField] private bool useUnscaledTimeFor3D = true;

    private Tween fadeTween;
    private Coroutine fade3DRoutine;

    private readonly List<FadeMaterialEntry> activeMaterialEntries = new List<FadeMaterialEntry>();

    private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

    private struct FadeMaterialEntry
    {
        public Renderer renderer;
        public Material material;
        public float startAlpha;
        public int colorPropertyId;
    }

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    private void OnDestroy()
    {
        KillTween();
        Kill3DFadeRoutine();
    }

    public void FadeIn()
    {
        FadeTo(1f, defaultDuration, true);
    }

    public void FadeOut()
    {
        FadeTo(0f, defaultDuration, false);
    }

    public void FadeTo(float alpha)
    {
        bool interact = alpha > 0.95f;
        FadeTo(alpha, defaultDuration, interact);
    }

    public void FadeTo(float alpha, float duration, bool interactable)
    {
        if (canvasGroup == null)
        {
            return;
        }

        KillTween();

        float target = Mathf.Clamp01(alpha);
        fadeTween = canvasGroup.DOFade(target, Mathf.Max(0f, duration))
            .SetEase(ease)
            .SetUpdate(true)
            .OnUpdate(() =>
            {
                if (!controlRaycast)
                {
                    return;
                }

                bool canInteract = canvasGroup.alpha > 0.95f && interactable;
                canvasGroup.interactable = canInteract;
                canvasGroup.blocksRaycasts = canInteract;
            })
            .OnComplete(() =>
            {
                if (!controlRaycast)
                {
                    return;
                }

                canvasGroup.interactable = interactable && target > 0.95f;
                canvasGroup.blocksRaycasts = interactable && target > 0.95f;
            });
    }

    public void SetVisible(bool visible)
    {
        SetVisible(visible, visible);
    }

    public void SetVisible(bool visible, bool interactable)
    {
        if (canvasGroup == null)
        {
            return;
        }

        KillTween();

        canvasGroup.alpha = visible ? 1f : 0f;

        if (controlRaycast)
        {
            canvasGroup.interactable = visible && interactable;
            canvasGroup.blocksRaycasts = visible && interactable;
        }
    }

    private void KillTween()
    {
        if (fadeTween == null)
        {
            return;
        }

        if (fadeTween.IsActive())
        {
            fadeTween.Kill();
        }

        fadeTween = null;
    }

    public void FadeOutSelf3D()
    {
        FadeOutSelf3D(default3DDuration);
    }

    public void FadeOutSelf3D(float duration)
    {
        FadeOut3DTarget(gameObject, false, duration);
    }

    public void FadeOutChildren3D()
    {
        FadeOutChildren3D(default3DDuration);
    }

    public void FadeOutChildren3D(float duration)
    {
        if (gameObject == null)
        {
            return;
        }

        var childRenderers = CollectRenderers(gameObject, includeSelf: false, includeInactiveChildren);
        Start3DFade(childRenderers, duration);
    }

    public void FadeOutHierarchy3D()
    {
        FadeOutHierarchy3D(default3DDuration);
    }

    public void FadeOutHierarchy3D(float duration)
    {
        FadeOut3DTarget(gameObject, true, duration);
    }

    public void FadeOut3DTarget(GameObject target, bool includeChildren)
    {
        FadeOut3DTarget(target, includeChildren, default3DDuration);
    }

    public void FadeOut3DTarget(GameObject target, bool includeChildren, float duration)
    {
        if (target == null)
        {
            return;
        }

        var renderers = CollectRenderers(target, includeSelf: true, includeChildren: includeChildren);
        Start3DFade(renderers, duration);
    }

    private List<Renderer> CollectRenderers(GameObject target, bool includeSelf, bool includeChildren)
    {
        var result = new List<Renderer>();
        if (target == null)
        {
            return result;
        }

        if (includeChildren)
        {
            var all = target.GetComponentsInChildren<Renderer>(includeInactiveChildren);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null)
                {
                    result.Add(all[i]);
                }
            }

            if (!includeSelf)
            {
                var selfRenderers = target.GetComponents<Renderer>();
                for (int i = 0; i < selfRenderers.Length; i++)
                {
                    result.Remove(selfRenderers[i]);
                }
            }

            return result;
        }

        if (includeSelf)
        {
            var selfOnly = target.GetComponents<Renderer>();
            for (int i = 0; i < selfOnly.Length; i++)
            {
                if (selfOnly[i] != null)
                {
                    result.Add(selfOnly[i]);
                }
            }
        }

        return result;
    }

    private void Start3DFade(List<Renderer> renderers, float duration)
    {
        Kill3DFadeRoutine();

        if (renderers == null || renderers.Count == 0)
        {
            return;
        }

        activeMaterialEntries.Clear();

        for (int r = 0; r < renderers.Count; r++)
        {
            Renderer renderer = renderers[r];
            if (renderer == null)
            {
                continue;
            }

            Material[] mats = fadeSharedMaterials ? renderer.sharedMaterials : renderer.materials;
            if (mats == null)
            {
                continue;
            }

            for (int m = 0; m < mats.Length; m++)
            {
                Material mat = mats[m];
                if (mat == null)
                {
                    continue;
                }

                int colorPropertyId = ResolveColorPropertyId(mat);
                if (colorPropertyId == -1)
                {
                    continue;
                }

                Color c = mat.GetColor(colorPropertyId);
                activeMaterialEntries.Add(new FadeMaterialEntry
                {
                    renderer = renderer,
                    material = mat,
                    startAlpha = c.a,
                    colorPropertyId = colorPropertyId
                });
            }
        }

        if (activeMaterialEntries.Count == 0)
        {
            if (disableRenderersOnComplete)
            {
                for (int i = 0; i < renderers.Count; i++)
                {
                    if (renderers[i] != null)
                    {
                        renderers[i].enabled = false;
                    }
                }
            }

            return;
        }

        fade3DRoutine = StartCoroutine(Fade3DCoroutine(Mathf.Max(0f, duration)));
    }

    private IEnumerator Fade3DCoroutine(float duration)
    {
        if (duration <= 0f)
        {
            Apply3DAlpha(0f);
            Complete3DFade();
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += useUnscaledTimeFor3D ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = DOVirtual.EasedValue(0f, 1f, t, ease);

            Apply3DAlpha(1f - eased);
            yield return null;
        }

        Apply3DAlpha(0f);
        Complete3DFade();
    }

    private void Apply3DAlpha(float normalized)
    {
        for (int i = 0; i < activeMaterialEntries.Count; i++)
        {
            FadeMaterialEntry entry = activeMaterialEntries[i];
            if (entry.material == null)
            {
                continue;
            }

            Color c = entry.material.GetColor(entry.colorPropertyId);
            c.a = Mathf.Clamp01(entry.startAlpha * normalized);
            entry.material.SetColor(entry.colorPropertyId, c);
        }
    }

    private void Complete3DFade()
    {
        if (disableRenderersOnComplete)
        {
            var processed = new HashSet<Renderer>();
            for (int i = 0; i < activeMaterialEntries.Count; i++)
            {
                Renderer renderer = activeMaterialEntries[i].renderer;
                if (renderer == null || processed.Contains(renderer))
                {
                    continue;
                }

                renderer.enabled = false;
                processed.Add(renderer);
            }
        }

        activeMaterialEntries.Clear();
        fade3DRoutine = null;
    }

    private void Kill3DFadeRoutine()
    {
        if (fade3DRoutine == null)
        {
            return;
        }

        StopCoroutine(fade3DRoutine);
        fade3DRoutine = null;
        activeMaterialEntries.Clear();
    }

    private static int ResolveColorPropertyId(Material material)
    {
        if (material == null)
        {
            return -1;
        }

        if (material.HasProperty(BaseColorPropertyId))
        {
            return BaseColorPropertyId;
        }

        if (material.HasProperty(ColorPropertyId))
        {
            return ColorPropertyId;
        }

        return -1;
    }
}
