using UnityEngine;

namespace TuringSimulator.Controller
{
    /// <summary>
    /// Keeps legacy prefab serialization stable (factory / cardData references). Safe to remove once prefabs are cleaned up.
    /// </summary>
    public sealed class CardSlotLegacyBridge : MonoBehaviour
    {
        public Object factory;
        public Object cardData;
    }
}
