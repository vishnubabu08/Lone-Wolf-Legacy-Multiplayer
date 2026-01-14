using UnityEngine;
using UnityEngine.UI;
using TMPro; // Required for TextMeshPro

public class PasswordToggler : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField passwordInput; // Drag your Password Input Field here
    public Image eyeIcon;                // Drag the Image component of your Eye Button

    [Header("Icons")]
    public Sprite eyeOpen;   // Drag your "Show" icon here
    public Sprite eyeClosed; // Drag your "Hide" icon here (usually an eye with a slash)

    private bool isHidden = true;

    public void TogglePasswordVisibility()
    {
        isHidden = !isHidden;

        if (isHidden)
        {
            // Hide Password (show stars)
            passwordInput.contentType = TMP_InputField.ContentType.Password;
            if (eyeIcon != null) eyeIcon.sprite = eyeClosed;
        }
        else
        {
            // Show Password (show text)
            passwordInput.contentType = TMP_InputField.ContentType.Standard;
            if (eyeIcon != null) eyeIcon.sprite = eyeOpen;
        }

        // IMPORTANT: Refreshes the text immediately so the stars appear/disappear
        passwordInput.ForceLabelUpdate();
    }
}