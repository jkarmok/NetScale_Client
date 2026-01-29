using System;
using UnityEngine;

namespace Infrastructure
{
    public class GameLoop : MonoBehaviour
    {
        public event Action<float> Updated;
        public event Action<float> Tickeed;
        
        public void Update()
        {
            Updated?.Invoke(Time.deltaTime);
        }
        public void FixedUpdate()
        {
            Tickeed?.Invoke(Time.fixedDeltaTime);
        }
    }
}