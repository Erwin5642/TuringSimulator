using TMPro;
using UnityEngine;

namespace TuringSimulator.View
{
    public class LevelUI : MonoBehaviour
    {
        [SerializeField] private TextMeshPro levelTitle;
        [SerializeField] private TextMeshPro levelDescription;

        public void SetLevelTitle(string title)
        {
            levelTitle.text = title;
        }

        public void SetLevelDescription(string description)
        {
             levelDescription.text = description;
        }
    }
}
