using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private GameObject BeamController;

    enum Direction_8
    {
        Back,
        Back_Right,
        Right,
        Forward_Right,
        Forward,
        Forward_Left,
        Left,
        Back_Left
    }

    void Update()
    {
        if (SoulState())
            return;

        Movement();
    }
    
    private bool SoulState()
    {

        return false;
    }

    private void Movement()
    {
        if (rb.linearVelocityX != 0 || rb.linearVelocityY != 0)
        {
            DirectionalMovement();
        }
        else
        {
            if (animator.GetInteger("isIdleFacing") != -1 || animator.GetInteger("isShootingFacing") != -1)
            {
                return;
            }

            if (animator.GetInteger("isMovingInDirection") != -1)
            {
                SetInt("isIdleFacing", animator.GetInteger("isMovingInDirection") / 2);
                return;
            }
            
            if (animator.GetInteger("isShootingWhileMovingInDirection") != -1)
            {
                SetInt("isShootingFacing", animator.GetInteger("isShootingWhileMovingInDirection") / 2);
                return;
            }
        }
    }

    void DirectionalMovement()
    {
        if (rb.linearVelocityX == 0 && rb.linearVelocityY == 0)
            return;

        if (rb.linearVelocityX == 0)
        {
            VerticalMovement();
            return;
        }

        if (rb.linearVelocityY == 0)
        {
            HorizontalMovement();
            return;
        }

        DiagonalMovement();

        void HorizontalMovement()
        {
            if (rb.linearVelocityY < 0)
            {
                SetDirection(Direction_8.Left);
            }
            else
            {
                SetDirection(Direction_8.Right);
            }
        }

        void VerticalMovement()
        {
            if (rb.linearVelocityY < 0)
            {
                SetDirection(Direction_8.Back);
            }
            else
            {
                SetDirection(Direction_8.Forward);
            }
        }

        void DiagonalMovement()
        {
            // Diagonal Movement
            if (rb.linearVelocityX > 0)
            {
                if (rb.linearVelocityY > 0)
                {
                    SetDirection(Direction_8.Forward_Right);
                }
                else
                {
                    SetDirection(Direction_8.Back_Right);
                }
            }
            else
            {
                if (rb.linearVelocityY > 0)
                {
                    SetDirection(Direction_8.Forward_Left);
                }
                else
                {
                    SetDirection(Direction_8.Back_Left);
                }
            }
        }

        void SetDirection(Direction_8 direction)
        {
            if (BeamController.activeSelf)
            {
                SetInt("isShootingWhileMovingInDirection", (int)direction);
                
            }
            else
            {
                SetInt("isMovingInDirection", (int)direction);
            }
        }
    }

    void DisableOtherParameters(string parameterToKeep)
    {
        if (parameterToKeep != "isMovingInDirection")
            animator.SetInteger("isMovingInDirection", -1);
        if (parameterToKeep != "isIdleFacing")
            animator.SetInteger("isIdleFacing", -1);
        if (parameterToKeep != "isShootingWhileMovingInDirection")
            animator.SetInteger("isShootingWhileMovingInDirection", -1);
        if (parameterToKeep != "isShootingFacing")
            animator.SetInteger("isShootingFacing", -1);
    }

    void SetInt(string parameter, int value)
    {
        animator.SetInteger(parameter, value);
        DisableOtherParameters(parameter);
    }
}