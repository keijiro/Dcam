using UnityEngine;
using UnityEngine.UI;

namespace Dcam {

sealed class TimeView : MonoBehaviour
{
    [SerializeField] Text _timeText = null;

    void Update()
      => _timeText.text = System.DateTime.Now.ToString("HH:mm:ss");
}

} // namespace Dcam
