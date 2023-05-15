using UnityEngine;

namespace Dcam {

public sealed class MetadataReceiver : MonoBehaviour
{
    void Update()
    {
        // Deserialization
        var xml = GetComponent<Klak.Ndi.NdiReceiver>().metadata;
        if (xml == null || xml.Length == 0) return;
        var metadata = Metadata.Deserialize(xml);

        // Input state update with the metadata
        GetComponent<InputHandle>().InputState = metadata.InputState;
    }
}

} // namespace Dcam
