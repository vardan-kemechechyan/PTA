using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parking : MonoBehaviour
{
    [SerializeField] ParticleSystem[] effects;

    [SerializeField] MeshRenderer[] rend;

    public void PlayEffect() 
    {
        foreach (var e in effects)
            e.Play();
    }

    public void SetColor(Color color)
    {
        foreach (var r in rend)
            r.material.color = new Color(color.r, color.g, color.b, r.material.color.a);
    }
}
