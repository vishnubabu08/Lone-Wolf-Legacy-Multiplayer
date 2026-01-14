using UnityEngine;

public class ToggleMenu : MonoBehaviour
{
    public GameObject menu;
    bool isMenuActive = false;
    public Animator animator;


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleMenuVisibility();
        }
    }
    public void ToggleMenuVisibility()
    {
        isMenuActive = !isMenuActive;   
        menu.SetActive(isMenuActive);
    }

    public void PlyaAnimationbyNum(int animNum)
    {
        if(animNum== 1)
        {
            animator.Play("Pop");
        }
       else if (animNum == 2)
        {
            animator.Play("Break");
        }
       else if (animNum == 3)
        {
            animator.Play("flip");
        }

        menu.SetActive(false);
    }
}
