using UnityEngine;

namespace DarkCanvas.Common
{
    /// <summary>
    /// Hides the assigned game object on game start.
    /// </summary>
    public class HideOnPlay : MonoBehaviour
    {
        private void Start()
        {
            gameObject.SetActive(false);
        }
    }
}