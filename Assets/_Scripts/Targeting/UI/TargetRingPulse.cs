using UnityEngine;

/// <summary>
/// TargetRingPulse
/// --------------
/// Visual upgrade for the target ring:
/// - Pulses the ring's material using a MaterialPropertyBlock
/// - Does NOT instantiate / duplicate materials (safe for performance)
/// - Local-only (pure visual)
///
/// How it works:
/// - We adjust a shader property each frame.
/// - For most Standard/URP Lit shaders, "_BaseColor" exists.
/// - If your shader uses a different property, change baseColorProperty.
///
/// Recommended:
/// - Use an Unlit or URP Lit material on the ring.
/// - Keep this subtle; it's UI feedback, not VFX spam.
/// </summary>
[DisallowMultipleComponent]
public class TargetRingPulse : MonoBehaviour
{
    [Header("Color")]
    [Tooltip("The base ring color at minimum pulse.")]
    [SerializeField] private Color baseColor = Color.yellow;

    [Tooltip("Pulse strength added on top of baseColor (multiplier).")]
    [Range(0f, 3f)]
    [SerializeField] private float pulseStrength = 0.75f;

    [Tooltip("Pulse speed in cycles per second.")]
    [Range(0.05f, 5f)]
    [SerializeField] private float pulseSpeed = 1.25f;

    [Header("Shader Property")]
    [Tooltip("URP Lit/Unlit typically uses _BaseColor. Standard shader uses _Color.")]
    [SerializeField] private string baseColorProperty = "_BaseColor";

    private Renderer _renderer;
    private MaterialPropertyBlock _mpb;
    private int _colorId;

    private void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer == null)
        {
            Debug.LogWarning("[TargetRingPulse] No Renderer found.");
            enabled = false;
            return;
        }

        _mpb = new MaterialPropertyBlock();
        _colorId = Shader.PropertyToID(baseColorProperty);

        // If the shader doesn't have _BaseColor, users often have _Color.
        // We'll auto-fallback to _Color if needed.
        if (!_renderer.sharedMaterial.HasProperty(_colorId))
        {
            int fallbackId = Shader.PropertyToID("_Color");
            if (_renderer.sharedMaterial.HasProperty(fallbackId))
            {
                baseColorProperty = "_Color";
                _colorId = fallbackId;
            }
        }
    }

    private void Update()
    {
        // A clean 0..1 pulse curve using sine.
        float t01 = (Mathf.Sin(Time.time * Mathf.PI * 2f * pulseSpeed) + 1f) * 0.5f;

        // Blend between baseColor and a slightly brighter version.
        // Note: multiplying colors can push HDR values if your material supports it.
        Color pulseColor = baseColor * (1f + t01 * pulseStrength);

        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(_colorId, pulseColor);
        _renderer.SetPropertyBlock(_mpb);
    }

    private void OnDisable()
    {
        // Reset property block when disabled so the ring doesn't keep a stale tint.
        if (_renderer == null || _mpb == null)
            return;

        _renderer.GetPropertyBlock(_mpb);
        _mpb.Clear();
        _renderer.SetPropertyBlock(_mpb);
    }

    public void SetBaseColor(Color c)
    {
        baseColor = c;
    }

}
