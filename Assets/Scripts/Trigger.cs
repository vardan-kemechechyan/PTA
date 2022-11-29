using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Trigger : MonoBehaviour
{
    public int id;

    [SerializeField] GameObject path;

    public enum TriggerEvent 
    {
        None,
        Finish,
        Crash,
        Offroad,
    }

    [SerializeField] TriggerEvent triggerEvent;

    private void OnTriggerEnter(Collider other)
    {
        if (GameManager.GetInstance().IsPlay && GameManager.GetInstance().IsInputEnabled && other.CompareTag("Player")) 
        {
            var vechicle = other.gameObject.GetComponent<WheelVehicle>();
        
            if (vechicle && vechicle.control == WheelVehicle.Control.Player) 
            {
                switch (triggerEvent)
                {
                    case TriggerEvent.Finish:
                        if (TryGetComponent(out Parking p))
                            p.PlayEffect();
        
                        if (id == GameManager.GetInstance().stage.finish)
                            GameManager.GetInstance().CompleteStage();
                        else
                            GameManager.GetInstance().Crash();
                        break;
                    case TriggerEvent.Crash:
                        GameManager.GetInstance().Crash();
                        break;
                    case TriggerEvent.Offroad:
                        GameManager.GetInstance().Offroad();
                        break;
                }
            }
        }
    }

    public void ShowPath(bool show)
    {
        path.SetActive(show);
    }
}
