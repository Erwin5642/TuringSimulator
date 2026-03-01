using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TuringSimulator.Controller
{
    public class PlayerInputCatcher : MonoBehaviour
    {
        [SerializeField] private Key forwardKey = Key.RightArrow;
        [SerializeField] private Key backwardKey = Key.LeftArrow;
        [SerializeField] private Key playKey = Key.UpArrow;
        [SerializeField] private Key pauseKey = Key.DownArrow;
        [SerializeField] private Key startKey = Key.Space;
        [SerializeField] private Key nextKey = Key.N;
        
        public event Action OnStartRequest;
        public event Action OnPlayRequest;
        public event Action OnForwardRequest;
        public event Action OnBackwardRequest;
        public event Action OnPauseRequest;
        public event Action OnNextRequest;

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard[forwardKey].wasPressedThisFrame)
            {
                Debug.Log("[Input]: Forward requested");
                OnForwardRequest?.Invoke();
            }
            else if (keyboard[backwardKey].wasPressedThisFrame)
            {
                Debug.Log("[Input]: Backward requested");
                OnBackwardRequest?.Invoke();
            }
            else if (keyboard[playKey].wasPressedThisFrame)
            {
                Debug.Log("[Input]: Play requested");
                OnPlayRequest?.Invoke();
            }
            else if (keyboard[pauseKey].wasPressedThisFrame)
            {
                Debug.Log("[Input]: Pause requested");
                OnPauseRequest?.Invoke();
            }
            else if (keyboard[startKey].wasPressedThisFrame)
            {
                OnStartRequest?.Invoke();
            }
            else if (keyboard[nextKey].wasPressedThisFrame)
            {
                OnNextRequest?.Invoke();
            }
        }
    }
}