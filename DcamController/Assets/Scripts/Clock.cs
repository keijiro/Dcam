using UnityEngine;
using UnityEngine.UIElements;
using Unity.Properties;

namespace Dcam {

sealed public class Clock : MonoBehaviour
{
    [CreateProperty]
    public string TimeText => System.DateTime.Now.ToString("HH:mm:ss");

    void Start()
      => GetComponent<UIDocument>().rootVisualElement.Q("clock").dataSource = this;
}

} // namespace Dcam
