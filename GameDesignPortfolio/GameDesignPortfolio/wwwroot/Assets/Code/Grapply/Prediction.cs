using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Prediction : MonoBehaviour
{
    [SerializeField] private float maxSwingDistance = 25f;

    [SerializeField] private Transform gunTip, cam, player;
    public LayerMask whatIsGrappleable;

    [Header("Prediction")]
    private RaycastHit predictionHit;
    [SerializeField] private float predictionSphereCastRadius;
    [SerializeField] private Transform predictionPoint;

    private void CheckForHitPoints()
    {
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
