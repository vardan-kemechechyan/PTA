using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarTrailDrawer : MonoBehaviour
{
    [SerializeField] LineRenderer TrailFromStartToFinish;
    [SerializeField] Material BaseMaterialToDuplicate;

    public void SetTrailRendererMaterialColor( Color32 _trailColor )
    {
        TrailFromStartToFinish.material = new Material( BaseMaterialToDuplicate );


        TrailFromStartToFinish.startColor = _trailColor;
        TrailFromStartToFinish.endColor = _trailColor;
    }

    public void SetTrailEnabled( bool _enableStatus )
    {
        TrailFromStartToFinish.gameObject.SetActive( _enableStatus );
    }

    public void SetTrailRendererNewPosition( Vector3 _trailNewPosition )
    {
        TrailFromStartToFinish.SetPosition( TrailFromStartToFinish.positionCount - 1, _trailNewPosition );
        TrailFromStartToFinish.positionCount++;
        TrailFromStartToFinish.SetPosition( TrailFromStartToFinish.positionCount - 1, _trailNewPosition );
    }

    public void ResetPositions()
    {
        TrailFromStartToFinish.positionCount = 1;
    }
}
