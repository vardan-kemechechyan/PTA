using System.Collections;
using UnityEngine;

public class ObjectMaterial : MonoBehaviour
{
    public enum Type 
    {
        None,
        Wood,
        Metal,
        Plastic,
        Concrete
    }

    public Type type;
}
