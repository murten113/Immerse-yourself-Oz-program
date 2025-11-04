using UnityEngine;

public class CavitySlot : MonoBehaviour
{
    public OrganFamily slotFamily;

    // No visuals needed for debugging, just a label
    void Awake()
    {
        Debug.Log($"Slot ready: {slotFamily}");
    }
}
