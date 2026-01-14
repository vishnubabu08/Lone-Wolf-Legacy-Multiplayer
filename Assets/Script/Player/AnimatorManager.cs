using UnityEngine;

public class AnimatorManager : MonoBehaviour
{
Animator animator;
    private int horizontal;
    private int vertical;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        horizontal = Animator.StringToHash("Horizontal");
        vertical = Animator.StringToHash("Vertical") ;

    }

    public void UpdateAnimValue(float horizontalMovement, float verticalMovement,bool isSprinting)
    {
        float snappedHorizontal;
        float snappedVertical;

        #region Snapped Horizontal

        if(horizontalMovement>0f && horizontalMovement < 0.55f)
        {
            snappedHorizontal = 0.5f;
        }
        else if(horizontalMovement >0.55f)
        {
            snappedHorizontal = 1f;
        }
       else if (horizontalMovement < 0f && horizontalMovement > -0.55f)
        {
            snappedHorizontal = -0.5f;
        }
        else if(horizontalMovement < -0.55f)
        {
            snappedHorizontal = -0.5f;

        }
        else
        {
            snappedHorizontal = 0 ;

        }
        #endregion

        #region Snaped Vertical

        if (verticalMovement > 0f && verticalMovement < 0.55f)
        {
            snappedVertical = 0.5f;
        }
        else if (verticalMovement > 0.55f)
        {
            snappedVertical = 1f;
        }
        else if (verticalMovement < 0f && verticalMovement > -0.55f)
        {
            snappedVertical = -0.5f;
        }
        else if (verticalMovement < -0.55f)
        {
            snappedVertical = -0.5f;

        }
        else
        {
            snappedVertical = 0;

        }


        #endregion




        if (isSprinting)
        {
           snappedHorizontal = horizontalMovement;
          snappedVertical = 2;
        }

        animator.SetFloat(horizontal, snappedHorizontal, 0.1f, Time.deltaTime);
        animator.SetFloat(vertical, snappedVertical, 0.1f, Time.deltaTime);
    }
}
