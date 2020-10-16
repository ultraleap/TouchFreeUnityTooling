﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderSnapper2D : CursorSnapper
{
    public float snapRadius = 0.1f; 
    [Range(0.001f, 10)]
    public float maxDistance = 0.15f;
    public bool drawDebug = false;
    public float snapMultiplier = 2f;

    public float DistanceFromScreen;

    private CircleCollider2D snapCollider;
    private Collider2D currentSnappedCollider;

    private bool colliderInitialised = false;


    // Start is called before the first frame update
    void Start()
    {
        gameObject.layer = 8;
    }

    /// <Summary>
    /// Checks all overlapping colliders with the cursor & chooses the closest one.
    /// Snapping biased towards the button it is currently colliding with.
    /// </Summary>
    public override Vector2 CalculateSnappedPosition(Vector2 _position)
    {
        if (!colliderInitialised)
        {
            snapCollider = gameObject.AddComponent<CircleCollider2D>();
            snapCollider.radius = snapRadius;
            colliderInitialised = true;
        }

        Vector2 snappedPosition = _position;

        snapCollider.transform.position = Camera.main.ScreenToWorldPoint(snappedPosition);

        var results = new List<Collider2D>();
        int snapToIndex = 0;
        float overlapDistance = 0;

        float snappedButtonDistance = currentSnappedCollider != null ?
            (Mathf.Abs(currentSnappedCollider.Distance(snapCollider).distance) / maxDistance) * snapMultiplier : 0;

        int layerMask = LayerMask.GetMask("Cursor");
        layerMask = ~layerMask;
        ContactFilter2D contactFilter2D = new ContactFilter2D();
        contactFilter2D.layerMask = layerMask;
        contactFilter2D.useLayerMask = true;

        ColliderDistance2D colliderDistance;
        if (snapCollider.OverlapCollider(contactFilter2D, results) > 0)
        {
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                colliderDistance = result.Distance(snapCollider);

                float dist = Mathf.Abs(colliderDistance.distance) / maxDistance;

                if (dist > overlapDistance)
                {
                    overlapDistance = dist;
                    snapToIndex = i;
                }

                if (drawDebug)
                {
                    Debug.DrawLine((Vector3)colliderDistance.pointA + Vector3.forward, (Vector3)colliderDistance.pointB + Vector3.forward, Color.yellow);
                    Debug.DrawLine(result.transform.position, snapCollider.transform.position, Color.green);
                }
            }

            if (currentSnappedCollider == null || !results.Contains(currentSnappedCollider))
            {
                currentSnappedCollider = results[snapToIndex];
            }
            else
            {
                if (overlapDistance >= snappedButtonDistance)
                {
                    currentSnappedCollider = results[snapToIndex];
                }
            }
            if (drawDebug) Debug.DrawLine(currentSnappedCollider.transform.position, snapCollider.transform.position, Color.red);

        }
        else
        {
            currentSnappedCollider = null;
        }

        if (currentSnappedCollider != null)
        {
            snappedPosition = Camera.main.WorldToScreenPoint(currentSnappedCollider.transform.position);
        }
        return snappedPosition;
    }

}
