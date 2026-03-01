using System.Collections;
using System.Collections.Generic;
using TuringSimulator.Core.Types;
using TuringSimulator.View.Machine.Halt;
using UnityEngine;

namespace Tests.PlayModeTests.Mocks
{
    public class FakeHaltIndicator : MonoBehaviour, IHaltStatusIndicator
    {
        public HaltStatus CurrentStatus { get; private set; } = HaltStatus.None;

        public void Initialize()
        {
            throw new System.NotImplementedException();
        }

        public IEnumerator Show(HaltStatus status)
        {
            CurrentStatus = status;
            yield return null;
        }

        public void Reset()
        {
            throw new System.NotImplementedException();
        }
    }
}