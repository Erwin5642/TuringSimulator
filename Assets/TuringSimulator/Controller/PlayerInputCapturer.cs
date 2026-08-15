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
        [SerializeField] private Key menuKey = Key.M;
        
        public event Action OnStartRequest;
        public event Action OnPlayRequest;
        public event Action OnForwardRequest;
        public event Action OnBackwardRequest;
        public event Action OnPauseRequest;
        public event Action OnNextRequest;
        public event Action OnMenuRequest;
        public event Action OnAbortRequest;
        public event Action OnStartOrAbortRequest;
        public event Action OnPlayOrPauseRequest;

        public void RequestStart()
        {
            Debug.Log("[Input]: Start requested");
            OnStartRequest?.Invoke();
        }

        public void RequestPlay()
        {
            Debug.Log("[Input]: Play requested");
            OnPlayRequest?.Invoke();
        }

        public void RequestPause()
        {
            Debug.Log("[Input]: Pause requested");
            OnPauseRequest?.Invoke();
        }

        public void RequestStepForward()
        {
            Debug.Log("[Input]: Forward requested");
            OnForwardRequest?.Invoke();
        }

        public void RequestStepBackward()
        {
            Debug.Log("[Input]: Backward requested");
            OnBackwardRequest?.Invoke();
        }

        public void RequestNext()
        {
            Debug.Log("[Input]: Next requested");
            OnNextRequest?.Invoke();
        }

        public void RequestMenu()
        {
            Debug.Log("[Input]: Menu requested");
            OnMenuRequest?.Invoke();
        }

        public void RequestAbort()
        {
            Debug.Log("[Input]: Abort requested");
            OnAbortRequest?.Invoke();
        }

        public void RequestStartOrAbort()
        {
            Debug.Log("[Input]: Start/Abort toggle requested");
            OnStartOrAbortRequest?.Invoke();
        }

        public void RequestPlayOrPause()
        {
            Debug.Log("[Input]: Play/Pause toggle requested");
            OnPlayOrPauseRequest?.Invoke();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard[forwardKey].wasPressedThisFrame)
            {
                RequestStepForward();
            }
            else if (keyboard[backwardKey].wasPressedThisFrame)
            {
                RequestStepBackward();
            }
            else if (keyboard[playKey].wasPressedThisFrame)
            {
                RequestPlay();
            }
            else if (keyboard[pauseKey].wasPressedThisFrame)
            {
                RequestPause();
            }
            else if (keyboard[startKey].wasPressedThisFrame)
            {
                RequestStart();
            }
            else if (keyboard[nextKey].wasPressedThisFrame)
            {
                RequestNext();
            }
            else if (keyboard[menuKey].wasPressedThisFrame)
            {
                RequestMenu();
            }
        }
    }
}