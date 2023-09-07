using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Unlocks")]
    public bool hasGrapplingGun = false;
    public bool hasPowergrapplingGun = false;

    private Rigidbody rb;

    [Header("Movement")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float swingingSpeed;
    [SerializeField] private float grapplingSpeed;
    [SerializeField] private float walkingSpeed;

    [SerializeField] private float groundDrag;

    [Header("Jumping")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpCooldown;
    [SerializeField] private float airMultiplier;
    private bool readyToJump = true;

    [Header("Keybinds")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;

    [Header("Ground Check")]
    [SerializeField] private float playerHeight;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private bool grounded;

    [Header("Camera Effects")]
    [SerializeField] private PlayerCamera cam;
    [SerializeField] private float grappleFov = 95f;
    [SerializeField] private float swingingFov = 90f;
    [SerializeField] private float walkingFov = 80f;

    [SerializeField] private Transform orientation;

    private float horizontalInput;
    private float verticalInput;

    private Vector3 moveDirection;

    private Vector3 velocityToSet;

    [SerializeField] private bool enableMovementOnNextTouch;

    [Header("Player States")]
    [SerializeField] private bool freeze;
    public bool getFreeze { get { return freeze; } set { freeze = value; } }

    [SerializeField] private bool activeGrapple;
    [SerializeField] private bool swinging;
    public bool getSwinging { get { return swinging; } set { swinging = value; } }

    [SerializeField] private MovementState playerState;

    [SerializeField]
    private enum MovementState
    {
        freeze,
        grappling,
        swinging,
        walking,
        air
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<PlayerCamera>();
    }

    private void Update()
    {
        // Ground Check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        MyInput();
        SpeedControl();
        StateHandler();

        // Handle Drag
        if (grounded && !activeGrapple)
        {
            rb.drag = groundDrag;
        }
        else
        {
            rb.drag = 0;
        }
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void StateHandler()
    {
        // Freeze state
        if (freeze)
        {
            playerState = MovementState.freeze;
            moveSpeed = 0;
            rb.velocity = Vector3.zero;
        }

        // Grappling state
        else if (activeGrapple)
        {
            playerState = MovementState.grappling;
            moveSpeed = grapplingSpeed;

            cam.DoFov(grappleFov);
        }

        // Swinging state
        else if (swinging)
        {
            playerState = MovementState.swinging;
            moveSpeed = swingingSpeed;

            cam.DoFov(swingingFov);
        }

        // Walking state
        else if (grounded)
        {
            moveSpeed = walkingSpeed;

            cam.DoFov(walkingFov);
        }

        // Mode - Air
        else
        {
            playerState = MovementState.air;
        }
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // When to jump
        if (Input.GetKey(jumpKey) && readyToJump && grounded)
        {
            readyToJump = false;

            Jump();

            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (enableMovementOnNextTouch)
        {
            enableMovementOnNextTouch = false;
            ResetRestrictions();

            GetComponent<Grappling>().StopGrapple();
        }
    }

    private void MovePlayer()
    {
        if (activeGrapple) return;
        if (swinging) return;

        // Calculate movement direction 
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        cam.DoFov(walkingFov);

        // On Ground
        if (grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }
        //In Air
        else if (!grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }
    }

    private void SpeedControl()
    {
        if (activeGrapple) return;

        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // limit velocity if needed
        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }

    private void Jump()
    {
        // Reset the y velocity
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    /// <summary>
    /// This method is used for the grappling hook to jump to position
    /// </summary>
    /// <param name="targetPosition">The position where to jump to</param>
    /// <param name="trajectoryHeight">The heigth of the trajectory that you hit during the jump</param>
    public void JumpToPosition(Vector3 targetPosition, float trajectoryHeight)
    {
        activeGrapple = true;

        // Set the velocity for the jump to the target position.
        velocityToSet = CalculateJumpVelocity(transform.position, targetPosition, trajectoryHeight);
        // Set the velocity on the player and also change the camera's field of view
        Invoke(nameof(SetVelocity), 0.1f);
        // Change the field of fiew back and disable the activeGrapple boolean
        Invoke(nameof(ResetRestrictions), 3f);
    }

    /// <summary>
    /// This method is to calculate the velocity to create a nice arc for the grappling hook.
    /// </summary>
    /// <param name="startPoint">This is the players starting position from where the arc begins</param>
    /// <param name="endPoint">Where the player will end up</param>
    /// <param name="trajectoryHeight">The height that you want to touch in the arc</param>
    /// <returns>This returns the velocity the player needs to follow for a nice arc to the target position</returns>
    private Vector3 CalculateJumpVelocity(Vector3 startPoint, Vector3 endPoint, float trajectoryHeight)
    {
        float gravity = Physics.gravity.y;
        // Get the difference in start and endpoint y coordinates
        float displacementY = endPoint.y - startPoint.y;
        // Get the X and Z vector pointing towards the endpoint. (Direction)
        Vector3 displacementXZ = new Vector3(endPoint.x - startPoint.x, 0f, endPoint.z - startPoint.z);
        // Get the Y velocity
        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * trajectoryHeight);
        // Get the XZ velocity to move towards the endpoint 
        Vector3 velocityXZ = displacementXZ / (Mathf.Sqrt(-2 * trajectoryHeight / gravity)
            + Mathf.Sqrt(2 * (displacementY - trajectoryHeight) / gravity));

        return velocityXZ + velocityY;
    }

    private void SetVelocity()
    {
        enableMovementOnNextTouch = true;
        rb.velocity = velocityToSet;

        cam.DoFov(grappleFov);
    }

    public void ResetRestrictions()
    {
        activeGrapple = false;
        cam.DoFov(walkingFov);
    }


}
