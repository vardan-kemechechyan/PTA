using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransparncyOverHeight : MonoBehaviour
{
    MeshRenderer rend;
    Color col;

    [SerializeField] float height = 1.0f;
    [SerializeField] float multiplier = 1.0f;

    void Start()
    {
        rend = GetComponent<MeshRenderer>();
    }

    void LateUpdate()
    {
        col = rend.material.color;

        col.a = height - transform.localPosition.y;

        if (col.a <= 0)
            col.a = 0.1f;

        rend.material.color = col;
    }
}
