using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class FaceAnimator : MonoBehaviour
{
    [Serializable]
    public class Keyframe
    {
        // Some exporters provide 'frame' instead of 'time'.
        public int frame;
        public float time;
        public float value;
    }

    [Serializable]
    public class AnimationData
    {
        public List<Keyframe> keyframes = new List<Keyframe>();
    }

    public enum SamplingMode
    {
        Step,
        Interpolate
    }

    public enum TimeSource
    {
        GameTime,
        AudioSource,
        PlayableDirector
    }

    [Header("Data")]
    [SerializeField] private TextAsset jsonFile;
    [Tooltip("Used when the JSON provides 'frame' instead of 'time'.")]
    [SerializeField] private float framesPerSecond = 30f;

    [Header("Target")]
    [SerializeField] private Renderer targetRenderer;
    [Tooltip("Which material slot in the Renderer to animate (0-based).")]
    [SerializeField] private int targetMaterialIndex = 0;
    [SerializeField] private string texturePropertyName = "_BaseMap"; // or "_MainTex"

    [Header("Apply")]
    [Tooltip("If enabled, applies tiling/offset via MaterialPropertyBlock (recommended when values change in Inspector but visuals don't update).")]
    [SerializeField] private bool useMaterialPropertyBlock = true;
    [Tooltip("If enabled, re-applies the property block every frame in LateUpdate (helps if Animator/Timeline overwrites property blocks after Update).")]
    [SerializeField] private bool reapplyPropertyBlockEveryFrame = true;
    [Tooltip("If enabled, also writes scale/offset to the material for Inspector visibility while still rendering via PropertyBlock.")]
    [SerializeField] private bool mirrorToMaterialWhenUsingPropertyBlock = false;

    [Header("Atlas")]
    [SerializeField] private float tilingX = 0.5f;  // 2 columns
    [SerializeField] private float tilingY = 0.25f; // 4 rows
    [Tooltip("If enabled, sets the material Texture Scale to (tilingX, tilingY) on Start. Disable if your mesh UVs are already mapped to a single tile and you want to keep (1,1).")]
    [SerializeField] private bool setTextureScaleOnStart = true;

    [Header("Playback")]
    [SerializeField] private SamplingMode samplingMode = SamplingMode.Step;
    [SerializeField] private bool loop = true;

    [Header("Time Sync")]
    [SerializeField] private TimeSource timeSource = TimeSource.GameTime;
    [Tooltip("Only used when TimeSource = GameTime.")]
    [SerializeField] private bool useUnscaledTime = false;
    [Tooltip("Only used when TimeSource = AudioSource.")]
    [SerializeField] private AudioSource syncAudioSource;
    [Tooltip("Only used when TimeSource = PlayableDirector (Timeline).")]
    [SerializeField] private PlayableDirector syncDirector;
    [Tooltip("Adds/subtracts seconds to the sampled time (useful for alignment).")]
    [SerializeField] private float timeOffset = 0f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;
    [Tooltip("If enabled, logs only when the face index changes.")]
    [SerializeField] private bool logOnlyOnFaceChange = true;
    [Tooltip("If enabled, logs extra sampling/time-source details (can be noisy).")]
    [SerializeField] private bool verboseLogs = false;
    [Tooltip("Minimum seconds between Debug.Log messages (0 = no cooldown). Uses unscaled time.")]
    [SerializeField] private float logCooldownSeconds = 0.25f;

    private AnimationData animationData;
    private Material materialInstance;
    private string resolvedTextureProperty;
    private string resolvedStProperty;
    private bool shaderSupportsStProperty = false;
    private float startTime;
    private int lastFaceIndex = -1;
    private bool warnedMissingTimeSource = false;
    private float nextAllowedLogTime = 0f;
    private MaterialPropertyBlock propertyBlock;
    private Vector2 appliedScale;
    private Vector2 appliedOffset;
    private bool isInitialized = false;

    private void Start()
    {
        startTime = GetNowTime();
        animationData = ParseJson(jsonFile);

        if (targetRenderer == null)
        {
            Debug.LogWarning($"{nameof(FaceAnimator)}: Target Renderer is not assigned.");
            return;
        }

        Material[] materials = targetRenderer.materials;
        if (materials == null || materials.Length == 0)
        {
            Debug.LogWarning($"{nameof(FaceAnimator)}: Target Renderer has no materials.");
            return;
        }

        int subMeshCount = GetSubMeshCount(targetRenderer);
        if (debugLogs)
        {
            Debug.Log($"{nameof(FaceAnimator)}: Renderer has materials={materials.Length}, subMeshes={subMeshCount}. TargetMaterialIndex={targetMaterialIndex}.");
            LogRendererDiagnostics(targetRenderer, materials, subMeshCount);
        }

        if (subMeshCount > 0 && targetMaterialIndex >= subMeshCount)
        {
            Debug.LogWarning(
                $"{nameof(FaceAnimator)}: TargetMaterialIndex={targetMaterialIndex} but mesh has only {subMeshCount} subMesh(es). " +
                "Unity will ignore extra materials, so changing offset/tiling on that index won't be visible. " +
                "Fix: ensure the model mesh has separate submeshes/material slots for mouth/eyes, or use separate renderers.");
        }

        if (targetMaterialIndex < 0 || targetMaterialIndex >= materials.Length)
        {
            Debug.LogWarning($"{nameof(FaceAnimator)}: Material index {targetMaterialIndex} is out of range (0..{materials.Length - 1}).");
            return;
        }

        materialInstance = materials[targetMaterialIndex];
        resolvedTextureProperty = ResolveTextureProperty(materialInstance, texturePropertyName);
        resolvedStProperty = string.IsNullOrEmpty(resolvedTextureProperty) ? null : resolvedTextureProperty + "_ST";

        if (string.IsNullOrEmpty(resolvedTextureProperty))
        {
            Debug.LogWarning($"{nameof(FaceAnimator)}: Material has neither '{texturePropertyName}' nor '_MainTex' texture property.");
            return;
        }

        shaderSupportsStProperty = !string.IsNullOrEmpty(resolvedStProperty) && materialInstance.HasProperty(resolvedStProperty);
        if (debugLogs)
        {
            string shaderName = materialInstance.shader != null ? materialInstance.shader.name : "(null shader)";
            Debug.Log($"{nameof(FaceAnimator)}: Using shader '{shaderName}'. Supports '{resolvedStProperty}' = {shaderSupportsStProperty}.");
        }

        if (!shaderSupportsStProperty)
        {
            Debug.LogWarning($"{nameof(FaceAnimator)}: Shader does not expose '{resolvedStProperty}'. Tiling/Offset changes may not affect rendering. Ensure the shader uses the texture's Tiling/Offset (ST) or animate the UV used by the shader.");
        }

        if (materialInstance.GetTexture(resolvedTextureProperty) == null)
        {
            Debug.LogWarning($"{nameof(FaceAnimator)}: Material '{materialInstance.name}' has no texture assigned in '{resolvedTextureProperty}'. Offsets will have no visible effect.");
        }

        Vector2 existingScale = materialInstance.GetTextureScale(resolvedTextureProperty);
        Vector2 existingOffset = materialInstance.GetTextureOffset(resolvedTextureProperty);

        appliedScale = setTextureScaleOnStart ? new Vector2(tilingX, tilingY) : existingScale;
        appliedOffset = existingOffset;

        if (useMaterialPropertyBlock)
        {
            propertyBlock ??= new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(propertyBlock, targetMaterialIndex);
            if (shaderSupportsStProperty)
            {
                propertyBlock.SetVector(resolvedStProperty, new Vector4(appliedScale.x, appliedScale.y, appliedOffset.x, appliedOffset.y));
            }
            targetRenderer.SetPropertyBlock(propertyBlock, targetMaterialIndex);

            if (mirrorToMaterialWhenUsingPropertyBlock)
            {
                materialInstance.SetTextureScale(resolvedTextureProperty, appliedScale);
                materialInstance.SetTextureOffset(resolvedTextureProperty, appliedOffset);
            }
        }
        else if (setTextureScaleOnStart)
        {
            materialInstance.SetTextureScale(resolvedTextureProperty, appliedScale);
        }

        if (debugLogs)
        {
            Texture tex = materialInstance.GetTexture(resolvedTextureProperty);
            string texInfo = tex != null ? $"'{tex.name}' wrap={tex.wrapMode}" : "(null texture)";
            Vector2 afterScale = useMaterialPropertyBlock ? appliedScale : materialInstance.GetTextureScale(resolvedTextureProperty);
            Vector2 afterOffset = useMaterialPropertyBlock ? appliedOffset : materialInstance.GetTextureOffset(resolvedTextureProperty);
            Debug.Log($"{nameof(FaceAnimator)}: Texture='{resolvedTextureProperty}' {texInfo}. ApplyMode={(useMaterialPropertyBlock ? "PropertyBlock" : "Material")}, ST='{resolvedStProperty}'. Scale {existingScale} -> {afterScale}, Offset {existingOffset} -> {afterOffset}." );
        }

        if (animationData != null && animationData.keyframes != null)
        {
            NormalizeKeyframeTimes(animationData.keyframes);
            animationData.keyframes.Sort((a, b) =>
            {
                int byTime = a.time.CompareTo(b.time);
                if (byTime != 0) return byTime;
                return a.frame.CompareTo(b.frame);
            });
        }

        float initialValue = SampleValueAtTime(0f);
        UpdateFace(initialValue);

        isInitialized = true;

        if (debugLogs)
        {
            int count = (animationData != null && animationData.keyframes != null) ? animationData.keyframes.Count : 0;
            float duration = 0f;
            if (animationData != null && animationData.keyframes != null && animationData.keyframes.Count > 0)
            {
                duration = animationData.keyframes[animationData.keyframes.Count - 1].time;
            }
            Debug.Log($"{nameof(FaceAnimator)}: Initialized. Keyframes={count}, Duration={duration:F3}s, FPS={framesPerSecond:F2}, Mode={samplingMode}, TimeSource={timeSource}, Loop={loop}, MaterialIndex={targetMaterialIndex}, Property='{resolvedTextureProperty}'.");

            if (count > 0 && duration <= 0f)
            {
                Debug.LogWarning($"{nameof(FaceAnimator)}: Keyframe duration is 0s. If your JSON uses 'frame', set a valid '{nameof(framesPerSecond)}'.");
            }
        }
    }

    private void Update()
    {
        if (materialInstance == null || string.IsNullOrEmpty(resolvedTextureProperty))
        {
            return;
        }

        if (animationData == null || animationData.keyframes == null || animationData.keyframes.Count == 0)
        {
            return;
        }

        float elapsed = GetPlaybackTime();

        float duration = animationData.keyframes[animationData.keyframes.Count - 1].time;
        if (loop && duration > 0f)
        {
            elapsed = elapsed % duration;
        }
        else if (debugLogs && verboseLogs && duration <= 0f)
        {
            Debug.LogWarning($"{nameof(FaceAnimator)}: Duration is {duration:F3}s, so looping/time sampling may look stuck. Check keyframe times/frames.");
        }

        float value = SampleValueAtTime(elapsed);

        if (debugLogs && verboseLogs && CanLogNow())
        {
            int idx = ComputeFaceIndex(value);
            int col = idx % 2;
            int row = idx / 2;
            int rows = Mathf.Max(1, Mathf.RoundToInt(1f / Mathf.Max(0.0001f, tilingY)));
            int maxRow = rows - 1;
            float offsetX = col * tilingX;
            float offsetY = (maxRow - row) * tilingY;
            Vector2 materialOffset = materialInstance != null && !string.IsNullOrEmpty(resolvedTextureProperty)
                ? materialInstance.GetTextureOffset(resolvedTextureProperty)
                : Vector2.zero;
            Vector2 materialScale = materialInstance != null && !string.IsNullOrEmpty(resolvedTextureProperty)
                ? materialInstance.GetTextureScale(resolvedTextureProperty)
                : Vector2.one;

            Vector2 effectiveOffset = useMaterialPropertyBlock ? appliedOffset : materialOffset;
            Vector2 effectiveScale = useMaterialPropertyBlock ? appliedScale : materialScale;

            Debug.Log(
                $"{nameof(FaceAnimator)}: Update sample t={elapsed:F3}s value={value:F3} -> idx={idx} col={col} row={row} " +
                $"targetOffset=({offsetX:F3},{offsetY:F3}) effectiveOffset={effectiveOffset} effectiveScale={effectiveScale} " +
                $"(mode={(useMaterialPropertyBlock ? "PropertyBlock" : "Material")}, materialOffset={materialOffset}, materialScale={materialScale})");
        }

        UpdateFace(value);
    }

    private void LateUpdate()
    {
        if (!useMaterialPropertyBlock || !reapplyPropertyBlockEveryFrame || !isInitialized)
        {
            return;
        }

        ApplyScaleOffset(appliedScale, appliedOffset);
    }

    private float GetNowTime()
    {
        return useUnscaledTime ? Time.unscaledTime : Time.time;
    }

    private float GetPlaybackTime()
    {
        float t;

        switch (timeSource)
        {
            case TimeSource.AudioSource:
                if (syncAudioSource == null)
                {
                    if (!warnedMissingTimeSource)
                    {
                        warnedMissingTimeSource = true;
                        Debug.LogWarning($"{nameof(FaceAnimator)}: TimeSource is AudioSource but syncAudioSource is not assigned. Falling back to GameTime.");
                    }
                    t = GetNowTime() - startTime;
                }
                else
                {
                    // AudioSource.time is already relative to the clip start.
                    t = syncAudioSource.time;
                }
                break;

            case TimeSource.PlayableDirector:
                if (syncDirector == null)
                {
                    if (!warnedMissingTimeSource)
                    {
                        warnedMissingTimeSource = true;
                        Debug.LogWarning($"{nameof(FaceAnimator)}: TimeSource is PlayableDirector but syncDirector is not assigned. Falling back to GameTime.");
                    }
                    t = GetNowTime() - startTime;
                }
                else
                {
                    // Director.time is timeline time in seconds.
                    t = (float)syncDirector.time;
                }
                break;

            default:
                t = GetNowTime() - startTime;
                break;
        }

        t += timeOffset;
        if (t < 0f)
        {
            t = 0f;
        }

        if (verboseLogs && debugLogs && CanLogNow())
        {
            Debug.Log($"{nameof(FaceAnimator)}: SampleTime={t:F3}s (source={timeSource}, offset={timeOffset:F3}).");
        }

        return t;
    }

    private AnimationData ParseJson(TextAsset file)
    {
        if (file == null || string.IsNullOrWhiteSpace(file.text))
        {
            Debug.LogWarning($"{nameof(FaceAnimator)}: JSON file is missing or empty.");
            return new AnimationData();
        }

        string json = file.text.Trim();

        try
        {
            // Preferred shape: { "keyframes": [ {"time":0.0,"value":1.0}, ... ] }
            AnimationData data = JsonUtility.FromJson<AnimationData>(json);
            if (data != null && data.keyframes != null && data.keyframes.Count > 0)
            {
                return data;
            }

            // Fallback shape: [ {"time":0.0,"value":1.0}, ... ]
            if (json.StartsWith("["))
            {
                var wrapper = JsonUtility.FromJson<KeyframeArrayWrapper>("{\"keyframes\":" + json + "}");
                return new AnimationData { keyframes = wrapper.keyframes ?? new List<Keyframe>() };
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"{nameof(FaceAnimator)}: Failed parsing JSON. {ex.Message}");
        }

        return new AnimationData();
    }

    [Serializable]
    private class KeyframeArrayWrapper
    {
        public List<Keyframe> keyframes = new List<Keyframe>();
    }

    private float SampleValueAtTime(float time)
    {
        if (animationData == null || animationData.keyframes == null || animationData.keyframes.Count == 0)
        {
            return 1f;
        }

        List<Keyframe> keys = animationData.keyframes;

        // Defensive: if all times are 0 (bad/unsupported JSON), return last value.
        if (keys.Count > 1 && keys[0].time == 0f && keys[keys.Count - 1].time == 0f)
        {
            return keys[keys.Count - 1].value;
        }

        if (time <= keys[0].time)
        {
            return keys[0].value;
        }

        int last = keys.Count - 1;
        if (time >= keys[last].time)
        {
            return keys[last].value;
        }

        // Find i such that keys[i].time <= time < keys[i+1].time
        int lo = 0;
        int hi = last - 1;
        int i = 0;

        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            if (keys[mid].time <= time)
            {
                i = mid;
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        Keyframe a = keys[i];
        Keyframe b = keys[i + 1];

        if (samplingMode == SamplingMode.Step)
        {
            return a.value;
        }

        float dt = Mathf.Max(0.0001f, b.time - a.time);
        float t = Mathf.Clamp01((time - a.time) / dt);
        return Mathf.Lerp(a.value, b.value, t);
    }

    public void UpdateFace(float value)
    {
        int index = ComputeFaceIndex(value);

        bool changed = index != lastFaceIndex;
        if (!changed)
        {
            if (debugLogs && !logOnlyOnFaceChange && CanLogNow())
            {
                Debug.Log($"{nameof(FaceAnimator)}: Face unchanged index={index} (value={value:F3}).");
            }
            return;
        }

        lastFaceIndex = index;

        int col = index % 2;
        int row = index / 2;

        // Default: 4 rows -> maxRow = 3, matches (3 - row) rule.
        int rows = Mathf.Max(1, Mathf.RoundToInt(1f / Mathf.Max(0.0001f, tilingY)));
        int maxRow = rows - 1;

        float offsetX = col * tilingX;
        float offsetY = (maxRow - row) * tilingY;

        appliedOffset = new Vector2(offsetX, offsetY);
        ApplyScaleOffset(appliedScale, appliedOffset);

        if (debugLogs && CanLogNow())
        {
            Debug.Log($"{nameof(FaceAnimator)}: Face changed index={index} (value={value:F3}) col={col} row={row} offset=({offsetX:F3},{offsetY:F3})");
        }
    }

    private void ApplyScaleOffset(Vector2 scale, Vector2 offset)
    {
        if (useMaterialPropertyBlock)
        {
            if (targetRenderer == null || string.IsNullOrEmpty(resolvedStProperty))
            {
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(propertyBlock, targetMaterialIndex);
            if (shaderSupportsStProperty)
            {
                propertyBlock.SetVector(resolvedStProperty, new Vector4(scale.x, scale.y, offset.x, offset.y));
            }
            targetRenderer.SetPropertyBlock(propertyBlock, targetMaterialIndex);

            if (mirrorToMaterialWhenUsingPropertyBlock && materialInstance != null && !string.IsNullOrEmpty(resolvedTextureProperty))
            {
                materialInstance.SetTextureScale(resolvedTextureProperty, scale);
                materialInstance.SetTextureOffset(resolvedTextureProperty, offset);
            }
            return;
        }

        if (materialInstance == null || string.IsNullOrEmpty(resolvedTextureProperty))
        {
            return;
        }

        if (setTextureScaleOnStart)
        {
            materialInstance.SetTextureScale(resolvedTextureProperty, scale);
        }

        materialInstance.SetTextureOffset(resolvedTextureProperty, offset);
    }

    private static int ComputeFaceIndex(float value)
    {
        int index = Mathf.FloorToInt(value) - 1;
        return Mathf.Clamp(index, 0, 7);
    }

    private static string ResolveTextureProperty(Material material, string preferred)
    {
        if (material == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(preferred) && material.HasProperty(preferred))
        {
            return preferred;
        }

        if (material.HasProperty("_MainTex"))
        {
            return "_MainTex";
        }

        return null;
    }

    private static int GetSubMeshCount(Renderer renderer)
    {
        if (renderer == null)
        {
            return -1;
        }

        if (renderer is SkinnedMeshRenderer skinned)
        {
            return skinned.sharedMesh != null ? skinned.sharedMesh.subMeshCount : -1;
        }

        MeshFilter mf = renderer.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            return mf.sharedMesh.subMeshCount;
        }

        return -1;
    }

    private static void LogRendererDiagnostics(Renderer renderer, Material[] materials, int subMeshCount)
    {
        if (renderer == null || materials == null)
        {
            return;
        }

        string rendererType = renderer.GetType().Name;
        Debug.Log($"{nameof(FaceAnimator)}: Diagnostics: Renderer='{renderer.name}' type={rendererType}.");

        for (int i = 0; i < materials.Length; i++)
        {
            Material m = materials[i];
            if (m == null)
            {
                Debug.LogWarning($"{nameof(FaceAnimator)}: Diagnostics: Material[{i}] = null");
                continue;
            }

            string shaderName = m.shader != null ? m.shader.name : "(null shader)";
            Texture baseMap = null;
            if (m.HasProperty("_BaseMap")) baseMap = m.GetTexture("_BaseMap");
            else if (m.HasProperty("_MainTex")) baseMap = m.GetTexture("_MainTex");
            string texName = baseMap != null ? baseMap.name : "(null)";
            Debug.Log($"{nameof(FaceAnimator)}: Diagnostics: Material[{i}]='{m.name}' shader='{shaderName}' baseTexture='{texName}'.");
        }

        int[] tris = GetSubmeshTriangleCounts(renderer);
        if (subMeshCount > 0 && tris != null && tris.Length == subMeshCount)
        {
            for (int i = 0; i < tris.Length; i++)
            {
                Debug.Log($"{nameof(FaceAnimator)}: Diagnostics: SubMesh[{i}] triangles={tris[i]}.");
            }

            for (int i = 0; i < tris.Length; i++)
            {
                if (tris[i] == 0)
                {
                    Debug.LogWarning($"{nameof(FaceAnimator)}: Diagnostics: SubMesh[{i}] has 0 triangles. Material slot {i} will not be visible.");
                }
            }
        }
    }

    private static int[] GetSubmeshTriangleCounts(Renderer renderer)
    {
        if (renderer is SkinnedMeshRenderer skinned)
        {
            Mesh mesh = skinned.sharedMesh;
            if (mesh == null) return null;
            int sm = mesh.subMeshCount;
            int[] counts = new int[sm];
            for (int i = 0; i < sm; i++)
            {
                counts[i] = (int)(mesh.GetIndexCount(i) / 3);
            }
            return counts;
        }

        MeshFilter mf = renderer.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            Mesh mesh = mf.sharedMesh;
            int sm = mesh.subMeshCount;
            int[] counts = new int[sm];
            for (int i = 0; i < sm; i++)
            {
                counts[i] = (int)(mesh.GetIndexCount(i) / 3);
            }
            return counts;
        }

        return null;
    }

    private void NormalizeKeyframeTimes(List<Keyframe> keys)
    {
        if (keys == null || keys.Count == 0)
        {
            return;
        }

        bool anyNonZeroTime = false;
        bool anyFrameNonZero = false;

        for (int i = 0; i < keys.Count; i++)
        {
            if (Mathf.Abs(keys[i].time) > 0.000001f)
            {
                anyNonZeroTime = true;
            }

            if (keys[i].frame != 0)
            {
                anyFrameNonZero = true;
            }
        }

        if (anyNonZeroTime)
        {
            return;
        }

        // If no explicit time is present, try using 'frame'.
        if (anyFrameNonZero || keys.Count == 1)
        {
            float fps = Mathf.Max(0.0001f, framesPerSecond);
            for (int i = 0; i < keys.Count; i++)
            {
                keys[i].time = keys[i].frame / fps;
            }

            if (debugLogs)
            {
                Debug.Log($"{nameof(FaceAnimator)}: Normalized keyframe times from 'frame' using FPS={fps:F2}." );
            }
        }
    }

    private bool CanLogNow()
    {
        if (!debugLogs)
        {
            return false;
        }

        if (logCooldownSeconds <= 0f)
        {
            return true;
        }

        float now = Time.unscaledTime;
        if (now < nextAllowedLogTime)
        {
            return false;
        }

        nextAllowedLogTime = now + logCooldownSeconds;
        return true;
    }
}
