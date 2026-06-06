using UnityEngine;

public class MouseRotate : MonoBehaviour
{
    public float rotationSpeed = 200f;

    void Update()
    {
        if (Input.GetMouseButton(0)) // Hold left mouse button
        {
            Vector2 mousePos = Input.mousePosition;

            // Screen center
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

            // Direction from center to mouse
            Vector2 direction = mousePos - screenCenter;

            // Normalize for consistent speed
            direction.Normalize();

            // Rotate based on direction
            float rotateX = -direction.y * rotationSpeed * Time.deltaTime;
            float rotateY = direction.x * rotationSpeed * Time.deltaTime;

            transform.Rotate(rotateX, rotateY, 0f, Space.World);
        }
    }
}
