using UnityEngine;
using UnityEngine.UI;

namespace X
{
    [System.Serializable]
    public class ClueBookSection
    {
        public string sectionName;
        public RectTransform rect;
        [TextArea(2, 5)]
        public string content;
    }
}