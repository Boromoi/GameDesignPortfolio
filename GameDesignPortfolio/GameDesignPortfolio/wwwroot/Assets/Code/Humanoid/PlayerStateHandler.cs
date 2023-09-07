using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStateHandler : MonoBehaviour
{
    [SerializeField]
    PlayerMovement pM;
    [SerializeField]
    Animator animator;

    public enum playerStates
    {
        idle, 
        walking, 
        running, 
        jumping,
        falling,
        die
    }

    public playerStates playerCurrentState = playerStates.idle;

    private void Awake()
    {
        //pM = GetComponent<PlayerMovement>();
// animator = GameObject.FindGameObjectWithTag("Player").GetComponent<Animator>();
    }

    public void Update()
    {
        switch (playerCurrentState)
        {
            case playerStates.idle:
                Idle();
                break;
            case playerStates.walking:
                Walk();
                break;
            case playerStates.running:
                Run();
                break;
            case playerStates.jumping:
                Jump();
                break;
            case playerStates.falling:
                Falling();
                break;
            case playerStates.die:
                Die();
                break;
        }
    }

    public void Idle()
    {
        // play Idle animation
        animator.Play("Idle00-Idle0");

        // if the player is moving and running is not true
        if ((pM.getHorizontalInput != 0 || pM.getVerticalInput != 0) && !pM.getRunning)
        {
            playerCurrentState = playerStates.walking;
        }
        
        // if the player is moving and running is true
        if ((pM.getHorizontalInput != 0 || pM.getVerticalInput != 0) && pM.getRunning)
        {
            playerCurrentState = playerStates.running;
        }

        // if the player is jumping (going up)
        if (pM.getJumping)
        {
            playerCurrentState = playerStates.jumping;
        }

        // if the player is falling (going down)
        if (pM.getFalling)
        {
            playerCurrentState = playerStates.falling;
        }
    }

    public void Walk()
    {
        // play Walk animation
        animator.Play("Walk00-Walk0");

        // if the player is standing still
        if (pM.getHorizontalInput == 0 && pM.getVerticalInput == 0)
        {
            playerCurrentState = playerStates.idle;
        }

        // if the player is moving and running is true
        if ((pM.getHorizontalInput != 0 || pM.getVerticalInput != 0) && pM.getRunning)
        {
            playerCurrentState = playerStates.running;
        }

        // if the player is jumping (going up)
        if (pM.getJumping)
        {
            playerCurrentState = playerStates.jumping;
        }

        // if the player is falling (going down)
        if (pM.getFalling)
        {
            playerCurrentState = playerStates.falling;
        }
    }

    public void Run()
    {
        // play Running animation
        animator.Play("Run00-Run0");

        // if the player is standing still
        if (pM.getHorizontalInput == 0 && pM.getVerticalInput == 0)
        {
            playerCurrentState = playerStates.idle;
        }

        // if the player is jumping (going up)
        if (pM.getJumping)
        {
            playerCurrentState = playerStates.jumping;
        }

        // if the player is falling (going down)
        if (pM.getFalling)
        {
            playerCurrentState = playerStates.falling;
        }
    }

    public void Jump()
    {
        // play Jumping animation
        animator.Play("Jump00-Jump0");

        // if the player is falling (going down)
        if (pM.getFalling)
        {
            playerCurrentState = playerStates.falling;
        }
    }

    public void Falling()
    {
        // play Falling animation

        // if the player is standing still
        if (pM.getHorizontalInput == 0 && pM.getVerticalInput == 0 && pM.getGrounded)
        {
            playerCurrentState = playerStates.idle;
        }

        // if the player is moving and running is not true
        if ((pM.getHorizontalInput != 0 || pM.getVerticalInput != 0) && !pM.getRunning && pM.getGrounded)
        {
            playerCurrentState = playerStates.walking;
        }

        // if the player is moving and running is true
        if ((pM.getHorizontalInput != 0 || pM.getVerticalInput != 0) && pM.getRunning && pM.getGrounded)
        {
            playerCurrentState = playerStates.running;
        }

        // if the player is falling (going down)
        if (pM.getFalling)
        {
            playerCurrentState = playerStates.falling;
        }
    }

    public void Die()
    {
        // play Death animation
    }
}
