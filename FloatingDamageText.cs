using TMPro;
using UnityEngine;

/// <summary>
/// FloatingDamageText
/// -------------------
/// Lightweight world-space floating number.
/// Client-only visual feedback.
/// </summary>
public class FloatingDamageText : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private float floatSpeed = 1.5f;
    [SerializeField] private float lifetime = 1.0f;

    private float _timer;
    private Color _startColor;

    public void Initialize(int amount)
    {
        text.text = $"-{amount}";
        _startColor = text.color;
        _timer = 0f;
    }

    private void Update()
    {
        // Float upward
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        // Fade out
        _timer += Time.deltaTime;
        float t = _timer / lifetime;
        text.color = new Color(
            _startColor.r,
            _startColor.g,
            _startColor.b,
            1f - t
        );

        if (_timer >= lifetime)
            Destroy(gameObject);
    }
}
