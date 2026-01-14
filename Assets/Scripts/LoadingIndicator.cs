using UnityEngine;
using TMPro;

/// <summary>
/// Simple loading indicator for analysis operations
/// </summary>
public class LoadingIndicator : MonoBehaviour
{
    [Header("References")]
    public TextMeshPro loadingText;
    public Transform spinnerTransform;

    [Header("Settings")]
    public float spinSpeed = 180f; // Degrees per second
    public string loadingMessage = "Analyzing...";

    private bool isLoading = false;
    private float dotTimer = 0f;
    private int dotCount = 0;

    void Update()
    {
        if (!isLoading) return;

        // Rotate spinner
        if (spinnerTransform != null)
        {
            spinnerTransform.Rotate(Vector3.forward, spinSpeed * Time.deltaTime);
        }

        // Animate loading dots
        if (loadingText != null)
        {
            dotTimer += Time.deltaTime;
            if (dotTimer >= 0.5f)
            {
                dotTimer = 0f;
                dotCount = (dotCount + 1) % 4;
                string dots = new string('.', dotCount);
                loadingText.text = loadingMessage + dots;
            }
        }
    }

    /// <summary>
    /// Show the loading indicator
    /// </summary>
    public void Show(string message = null)
    {
        if (message != null)
            loadingMessage = message;

        isLoading = true;
        gameObject.SetActive(true);
        dotCount = 0;
        dotTimer = 0f;

        if (loadingText != null)
            loadingText.text = loadingMessage;
    }

    /// <summary>
    /// Hide the loading indicator
    /// </summary>
    public void Hide()
    {
        isLoading = false;
        gameObject.SetActive(false);
    }
}
