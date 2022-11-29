using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class EndStageMessage : MonoBehaviour
    {
        Text message;
        Animation animation;

        void Start()
        {
            message = GetComponentInChildren<Text>();
            animation = GetComponent<Animation>();
        }

        public void Play(string text) 
        {
            message.text = text;
            animation.Play();
        }
    }
}