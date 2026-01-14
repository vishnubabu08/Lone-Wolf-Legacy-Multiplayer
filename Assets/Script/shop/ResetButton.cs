using UnityEngine;
using UnityEngine.UI;

public class ResetButton : MonoBehaviour
{
    public void OnClickReset()
    {
        // This finds the manager automatically and runs the reset
        if (FirebaseManager.instance != null)
        {
            FirebaseManager.instance.ResetAccount();
        }
    }
}