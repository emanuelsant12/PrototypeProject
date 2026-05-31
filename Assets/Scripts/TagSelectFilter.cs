using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[DisallowMultipleComponent]
public class TagSelectFilter : MonoBehaviour, IXRSelectFilter
{

    [Tooltip("Objects must have ANY of these tags to be selectable by this interactor.")]
    public List<string> allowedTags = new List<string> { };

    [Tooltip("If true, also allow objects with the 'Untagged' tag.")]
    public bool allowUntagged = false;

    public bool canProcess => true;

    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        var t = interactable.transform;

        if (allowUntagged && t.tag == "Untagged")
            return true;

        // Accept if the object matches ANY tag in the list
        for (int i = 0; i < allowedTags.Count; i++)
        {
            // CompareTag is safe/fast and avoids typos throwing errors
            if (!string.IsNullOrEmpty(allowedTags[i]) && t.CompareTag(allowedTags[i]))
                return true;
        }

        return false; // no tag matched
    }


}
