using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomSnapPoint : MonoBehaviour
{
   public enum ConnectionType
    {
        Road,
        AngledRoad
    }

    public ConnectionType Type;

    private void OnDrawGizmos()
    {
        switch (Type)
        {
            case ConnectionType.Road:
                Gizmos.color = Color.green;
                break;
            case ConnectionType.AngledRoad:
                Gizmos.color = Color.red;
                break;
        }
        Gizmos.DrawSphere(transform.position, radius: 0.3f);
    }
    
}
