﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(SphereCollider), typeof(Light))]
public class FlareLight : MonoBehaviour {

    [SerializeField]
    private float range = 3f;
    [SerializeField]
    private int segments = 90;
    [SerializeField]
    private float lightIntensity = 8;
    [SerializeField]
    LayerMask raycastIgnore;
    [SerializeField]
    float height = 1f;
    void Start()
    {
        var col = GetComponent<SphereCollider>();
        col.radius = range*10;
        col.isTrigger = true;
        var light = GetComponent<Light>();
        light.type = LightType.Point;
        light.range = range;
        light.intensity = lightIntensity;
        
    }

#if DEBUG
    // For now, this doesn't even need to exist in the final version
    void Update()
    {
        for (int i = 0; i <= segments; i++)
        {
            Vector3 direction = (Quaternion.AngleAxis(i * 360 / segments, Vector3.up) * Vector3.forward).normalized;
            Debug.DrawRay(transform.position + new Vector3(0, height, 0), direction * range, Color.blue);
        }
    }
#endif
    void OnTriggerStay(Collider col)
    {
        if (LayerMask.LayerToName(col.gameObject.layer) == "Enemy" && !col.GetComponentInChildren<SkinnedMeshRenderer>().enabled && RaySweep(col))
        {
            col.GetComponentsInChildren<SkinnedMeshRenderer>().ToList().ForEach(x => x.enabled = true);
            col.GetComponentsInChildren<cakeslice.Outline>().ToList().ForEach(x => x.enabled = true);
        }
    }

    void OnTriggerExit(Collider col)
    {
        if (LayerMask.LayerToName(col.gameObject.layer) == "Enemy")
        {
            col.GetComponentsInChildren<SkinnedMeshRenderer>().ToList().ForEach(x => x.enabled = false);
            col.GetComponentsInChildren<cakeslice.Outline>().ToList().ForEach(x => x.enabled = false);
        }
    }

    private bool RaySweep(Collider col)
    {
        RaycastHit hit;

        for (int i = 0; i < segments; i++)
        {
            Vector3 direction = (Quaternion.AngleAxis(i * 360 / segments, Vector3.up) * Vector3.forward).normalized;
            Physics.Raycast(transform.position + new Vector3(0, height, 0), direction, out hit, range, LayerMask.GetMask("Enemy", "Obstacle"));
            if (hit.collider && hit.collider.Equals(col)) return true;
        }
        return false;
    }
}
