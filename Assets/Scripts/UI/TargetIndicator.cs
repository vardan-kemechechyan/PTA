using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TargetIndicator : MonoBehaviour
{
    Transform parent;
    Animator animator;
    Transform target;

    [SerializeField] float distance = 5.0f;
    [SerializeField] float distanceToCar = 5.0f;

    [SerializeField] SpriteRenderer rend;

    float pathLength;
    float progress;

    void LateUpdate() 
    {
        if (GameManager.GetInstance().IsInputEnabled && target != null)
        {
            if (Vector3.Distance(parent.position, target.position) >= distance) rend.enabled = true;
            else rend.enabled = false;

            transform.rotation = Quaternion.LookRotation(target.position - transform.position);
            progress = 1 - (Vector3.Distance(target.position, parent.transform.position) / pathLength);

            GameManager.GetInstance().LevelProgress = progress > 0.9f ? 1 : progress;
        }
        else 
        {
            rend.enabled = false;
        }
    }

    public void PlayStartAnimation() 
    {
        animator.Play("TargetBounce", -1, 0);
    }

    public void SetTarget(Transform target) 
    {
        if (!parent)
            parent = transform.parent;

        if (!animator)
            animator = transform.GetChild(0).GetComponent<Animator>();

        this.target = target;
        var pos = rend.gameObject.transform.position;
        rend.gameObject.transform.position = new Vector3(pos.x, pos.y, pos.z + distanceToCar);
        pathLength = Vector3.Distance(target.position, parent.transform.position);
    }
}
