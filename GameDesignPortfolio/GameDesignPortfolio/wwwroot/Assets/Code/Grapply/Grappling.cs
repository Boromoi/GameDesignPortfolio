using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grappling : MonoBehaviour
{
    [Header("Sound")]
    [SerializeField] AudioClip powerGrapplingAudioClip;

    private PlayerMovement playerMovement;
    [Header("Refrences")]
    [SerializeField] private Transform cam;
    [SerializeField] private Transform gunTip;
    [SerializeField] private LayerMask whatIsGrappleable;
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Grappling")]
    [SerializeField] private float maxGrappleDistance;
    [SerializeField] private float grappleDelayTime;
    [SerializeField] private float overshootYAxis;

    private Vector3 grapplePoint;    

    [Header("Cooldown")]
    [SerializeField] private float grappleCD;
    private float grappleCDTimer;

    [Header("Input")]
    [SerializeField] private KeyCode grappleKey = KeyCode.Mouse1;

    [Header("Prediction")]
    private RaycastHit predictionHit;
    [SerializeField] private float predictionSphereCastRadius;
    [SerializeField] private Transform predictionPoint;

    private bool grappling;

    private void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {        
        // Grapple if the grappleKey is pressed
        if (Input.GetKeyDown(grappleKey))
        {
            StartGrapple();
        }

        CheckForHitPoints();

        // Count down the timer if it is above 0
        if (grappleCDTimer > 0)
        {
            grappleCDTimer -= Time.deltaTime;
        }
    }

    private void LateUpdate()
    {
        // Draw the line out of the grappling gun
        if (grappling)
        {            
            // Set first point of the line to the gunTip position
            lineRenderer.SetPosition(0, gunTip.position);
        }
    }

    private void StartGrapple()
    {
        // Return out of the method if the countdowntimer is above 0
        if (grappleCDTimer > 0) return;
        if (predictionHit.point == Vector3.zero) return;

        // Stop swinging and start grappling
        if (GetComponent<Swinging>() != null)
            GetComponent<Swinging>().StopSwinging();

        SoundManager.instance.PlaySound(powerGrapplingAudioClip);
        grappling = true;

        playerMovement.getFreeze = true;

        if (predictionHit.point != Vector3.zero)
        {
            grapplePoint = predictionHit.point;

            Invoke(nameof(ExecuteGrapple), grappleDelayTime);
        }
        else
        {
            grapplePoint = cam.position + cam.forward * maxGrappleDistance;

            Invoke(nameof(StopGrapple), grappleDelayTime);
        }

        // Set the positionCount to 2 and Set the endpoint of the line
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(1, grapplePoint);
    }

    private void ExecuteGrapple()
    {
        playerMovement.getFreeze = false;

        Vector3 lowestPoint = new Vector3(transform.position.x, transform.position.y - 1f, transform.position.z);
        float grapplePointRelativeYPos = grapplePoint.y - lowestPoint.y;
        float highestPointOnArc = grapplePointRelativeYPos + overshootYAxis;

        if (grapplePointRelativeYPos < 0) 
            highestPointOnArc = overshootYAxis;

        playerMovement.JumpToPosition(grapplePoint, highestPointOnArc);

        Invoke(nameof(StopGrapple), 1f);
    }

    public void StopGrapple()
    {
        playerMovement.getFreeze = false;

        grappling = false;

        grappleCDTimer = grappleCD;

        playerMovement.ResetRestrictions();

        // Remove the positions from the lineRenderer
        lineRenderer.positionCount = 0;
        lineRenderer.positionCount = 2;
    }

    private void CheckForHitPoints()
    {
        RaycastHit sphereCastHit;
        Physics.SphereCast(cam.position, predictionSphereCastRadius, cam.forward,
                            out sphereCastHit, maxGrappleDistance, whatIsGrappleable);

        RaycastHit rayCastHit;
        Physics.Raycast(cam.position, cam.forward,
                            out rayCastHit, maxGrappleDistance, whatIsGrappleable);

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
