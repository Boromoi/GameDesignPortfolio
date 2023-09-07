using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Swinging : MonoBehaviour
{
    [Header("Sound")]
    [SerializeField] AudioClip grapplingAudioClip;

    [Header("Input")]
    [SerializeField] private KeyCode swingKey = KeyCode.Mouse0;

    [Header("Refrences")]
    private PlayerMovement playerMovement;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform gunTip, cam, player;
    public LayerMask whatIsGrappleable;

    [Header("Swinging")]
    [SerializeField] private float maxSwingDistance = 25f;
    private Vector3 swingPoint;
    private SpringJoint joint;
    private Vector3 currentGrapplePosition;

    [Header("Swinging Movement")]
    [SerializeField] private Transform orientation;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float horizontalThrustForce;
    [SerializeField] private float forwardThrustForce;
    [SerializeField] private float extendCableSpeed;

    [Header("Prediction")]
    private RaycastHit predictionHit;
    [SerializeField] private float predictionSphereCastRadius;
    [SerializeField] private Transform predictionPoint;

    public void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    public void Update()
    {
        if (Input.GetKeyDown(swingKey))
            StartSwinging();
        if (Input.GetKeyUp(swingKey))
            StopSwinging();

        CheckForHitPoints();

        if (joint != null)
            SwingingMovement();
    }

    public void LateUpdate()
    {
        DrawSwingingLine();
    }

    public void StartSwinging()
    {
        // Return if predictionHit not found
        if (predictionHit.point == Vector3.zero) return;

        // Stop grappling and start swinging
        if (GetComponent<Grappling>() != null)
            GetComponent<Grappling>().StopGrapple();
        playerMovement.ResetRestrictions();

        playerMovement.getSwinging = true;

        SoundManager.instance.PlaySound(grapplingAudioClip);

        swingPoint = predictionHit.point;
        joint = player.gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = swingPoint;

        float distanceFromPoint = Vector3.Distance(player.position, swingPoint);

        // The distance grapple will keep you from the swing point
        joint.maxDistance = distanceFromPoint * 0.8f;
        joint.minDistance = distanceFromPoint * 0.25f;

        // Customizable settings
        joint.spring = 4.5f;
        joint.damper = 7f;
        joint.massScale = 4.5f;

        lineRenderer.positionCount = 2;
        currentGrapplePosition = gunTip.position;
    }

    public void StopSwinging()
    {
        playerMovement.getSwinging = false;

        lineRenderer.positionCount = 0;
        Destroy(joint);
    }

    public void DrawSwingingLine()
    {
        // if not swinging don't draw the line
        if (!joint) return;

        currentGrapplePosition = Vector3.Lerp(currentGrapplePosition, swingPoint, Time.deltaTime * 8f);

        lineRenderer.SetPosition(0, gunTip.position);
        lineRenderer.SetPosition(1, swingPoint);
    }

    public void SwingingMovement()
    {
        // Right
        if (Input.GetKey(KeyCode.D))
        {
            rb.AddForce(orientation.right * horizontalThrustForce * Time.deltaTime);
        }

        // Left
        if (Input.GetKey(KeyCode.A))
        {
            rb.AddForce(-orientation.right * horizontalThrustForce * Time.deltaTime);
        }

        // Forward
        if (Input.GetKey(KeyCode.W))
        {
            rb.AddForce(orientation.forward * horizontalThrustForce * Time.deltaTime);
        }

        // Shorten line
        if (Input.GetKey(KeyCode.Space))
        {
            Vector3 directionToPoint = swingPoint - transform.position;
            rb.AddForce(directionToPoint.normalized * forwardThrustForce * Time.deltaTime);

            float distanceFromPoint = Vector3.Distance(transform.position, swingPoint);

            joint.maxDistance = distanceFromPoint * 0.8f;
            joint.minDistance = distanceFromPoint * 0.25f;
        }

        // Extend line
        if (Input.GetKey(KeyCode.S))
        {
            float extendDistanceFromPoint = Vector3.Distance(transform.position, swingPoint) + extendCableSpeed;

            joint.maxDistance = extendDistanceFromPoint * 0.8f;
            joint.minDistance = extendDistanceFromPoint * 0.25f;
        }
    }

    //if (joint != null) return;

    private void CheckForHitPoints()
    {
        if (joint != null) return;

        RaycastHit sphereCastHit;
        Physics.SphereCast(cam.position, predictionSphereCastRadius, cam.forward,
                            out sphereCastHit, maxSwingDistance, whatIsGrappleable);

        RaycastHit rayCastHit;
        Physics.Raycast(cam.position, cam.forward,
                            out rayCastHit, maxSwingDistance, whatIsGrappleable);

        Vector3 realHitPoint;

        // Direct hit
        if (rayCastHit.point != Vector3.zero)
        {
            realHitPoint = rayCastHit.point;
        }

        // Predicted Hit
        else if (sphereCastHit.point != Vector3.zero)
        {
            realHitPoint = sphereCastHit.point;
        }

        // Miss
        else
        {
            realHitPoint = Vector3.zero;
        }

        if (realHitPoint != Vector3.zero)
        {
            predictionPoint.gameObject.SetActive(true);
            predictionPoint.position = realHitPoint;
        }
        // realHitPoint not found
        else
        {
            predictionPoint.gameObject.SetActive(false);
        }

        predictionHit = rayCastHit.point == Vector3.zero ? sphereCastHit : rayCastHit;
    }
}
