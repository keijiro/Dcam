using UnityEngine;

namespace Dcam {

public sealed class MetadataReceiver : MonoBehaviour
{
    void Update()
    {
        // NDI receiver existence
        var recv = GetComponent<Klak.Ndi.NdiReceiver>();
        if (recv == null) return;

        // Deserialization
        var xml = recv.metadata;
        if (xml == null || xml.Length == 0) return;
        var bin = Metadata.Deserialize(xml);

        // Input state update with the metadata
        GetComponent<InputHandle>().InputState = bin.InputState;
    }
}

} // namespace Dcam
