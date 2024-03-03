using Klak.Ndi;
using Klak.TestTools;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Properties;
using Cursor = UnityEngine.Cursor;

namespace Dcam {

public sealed class VideoSourceSelector : MonoBehaviour
{
    #region Data source accessor for UI Toolkit

    [CreateProperty]
    public List<string> SourceList => GetCachedSourceList();

    #endregion

    #region Predefined settings

    const string PrefKey = "VideoSourceName";
    const float CacheInterval = 1;

    #endregion

    #region Source list cache

    (List<string> list, float time) _sourceList
      = (new List<string>(), -1000);

    bool ShouldUpdateSourceList
      => Cursor.visible && Time.time - _sourceList.time > CacheInterval;

    List<string> GetCachedSourceList()
    {
        if (ShouldUpdateSourceList)
        {
            var uvc = WebCamTexture.devices.Select(dev => "UVC - " + dev.name);
            var ndi = NdiFinder.sourceNames.Select(name => "NDI - " + name);
            _sourceList.list = uvc.Concat(ndi).ToList();
            _sourceList.time = Time.time;
        }
        return _sourceList.list;
    }

    #endregion

    #region UI properties/methods

    VisualElement UIRoot
      => GetComponent<UIDocument>().rootVisualElement;

    void ToggleUI()
      => UIRoot.Q("Selector").visible = (Cursor.visible ^= true);

    void SelectSource(string name)
    {
        var source = GetComponent<ImageSource>();
        source.SourceName = name.Substring(6);
        source.SourceType = name.StartsWith("UVC") ?
          ImageSourceType.Webcam : ImageSourceType.Ndi;
        PlayerPrefs.SetString(PrefKey, name);
    }

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        // This component as a UI data source
        UIRoot.dataSource = this;

        // UI root as a clickable UI visibility toggle
        UIRoot.AddManipulator(new Clickable(ToggleUI));

        // Dropdown selection callback
        var list = UIRoot.Q<DropdownField>("Dropdown");
        list.RegisterValueChangedCallback(evt => SelectSource(evt.newValue));

        // Initially hidden UI
        ToggleUI();

        // Initial source selection
        if (PlayerPrefs.HasKey(PrefKey))
            SelectSource(list.value = PlayerPrefs.GetString(PrefKey));
    }

    #endregion
}

} // namespace Dcam
