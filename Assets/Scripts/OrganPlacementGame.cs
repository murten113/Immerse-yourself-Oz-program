using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum OrganFamily { Heart, Lungs, Brain }

public enum OrganId
{
    Heart1 = 1, Heart2 = 2, Heart3 = 3,
    Lungs1 = 4, Lungs2 = 5, Lungs3 = 6,
    Brain1 = 7, Brain2 = 8, Brain3 = 9
}

public static class OrganHelpers
{
    public static OrganFamily GetFamily(OrganId id)
    {
        switch (id)
        {
            case OrganId.Heart1:
            case OrganId.Heart2:
            case OrganId.Heart3: return OrganFamily.Heart;
            case OrganId.Lungs1:
            case OrganId.Lungs2:
            case OrganId.Lungs3: return OrganFamily.Lungs;
            default: return OrganFamily.Brain;
        }
    }
}

public class OrganPlacementGame : MonoBehaviour
{
    [Header("Slots")]
    public CavitySlot heartSlot;
    public CavitySlot lungsSlot;
    public CavitySlot brainSlot;

    [Header("Correct Organs")]
    public OrganId correctHeart = OrganId.Heart1;
    public OrganId correctLungs = OrganId.Lungs1;
    public OrganId correctBrain = OrganId.Brain1;

    [Header("Debug Mode")]
    public bool printDebug = true;

    [Header("Round Flow")]
    public float reactionDuration = 1.5f;
    public bool resetOnFailure = true;
    public bool resetOnSuccess = false;

    [Header("Manual Placement")]
    public bool manualPlacement = true;

    public UnityEvent OnLeverPulled;
    public UnityEvent OnRoundSuccess;
    public UnityEvent OnRoundFailure;

    private readonly Dictionary<KeyCode, OrganId> keyToOrgan = new Dictionary<KeyCode, OrganId>();

    private OrganId? inHeartSlot;
    private OrganId? inLungsSlot;
    private OrganId? inBrainSlot;

    private CavitySlot selectedSlot;

    void Awake()
    {
        keyToOrgan[KeyCode.Alpha1] = OrganId.Heart1;
        keyToOrgan[KeyCode.Alpha2] = OrganId.Heart2;
        keyToOrgan[KeyCode.Alpha3] = OrganId.Heart3;
        keyToOrgan[KeyCode.Alpha4] = OrganId.Lungs1;
        keyToOrgan[KeyCode.Alpha5] = OrganId.Lungs2;
        keyToOrgan[KeyCode.Alpha6] = OrganId.Lungs3;
        keyToOrgan[KeyCode.Alpha7] = OrganId.Brain1;
        keyToOrgan[KeyCode.Alpha8] = OrganId.Brain2;
        keyToOrgan[KeyCode.Alpha9] = OrganId.Brain3;

        selectedSlot = heartSlot;
        Debug.Log("Game started. Use A/B/C to select cavity and 1–9 to place organs. Space = pull lever.");
    }

    void Update()
    {
        if (manualPlacement)
        {
            if (Input.GetKeyDown(KeyCode.A)) { selectedSlot = heartSlot; Debug.Log("Selected cavity: HEART"); }
            if (Input.GetKeyDown(KeyCode.B)) { selectedSlot = lungsSlot; Debug.Log("Selected cavity: LUNGS"); }
            if (Input.GetKeyDown(KeyCode.C)) { selectedSlot = brainSlot; Debug.Log("Selected cavity: BRAIN"); }
        }

        foreach (var kv in keyToOrgan)
        {
            if (Input.GetKeyDown(kv.Key))
            {
                PlaceOrgan(kv.Value);
                break;
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            PullLever();
        }
    }

    public void PullLever()
    {
        OnLeverPulled?.Invoke();
        Debug.Log("=== Lever pulled ===");
        EvaluateAndReact();
    }

    private void PlaceOrgan(OrganId organ)
    {
        var fam = OrganHelpers.GetFamily(organ);

        if (manualPlacement && selectedSlot != null)
        {
            if (selectedSlot == heartSlot) inHeartSlot = organ;
            else if (selectedSlot == lungsSlot) inLungsSlot = organ;
            else if (selectedSlot == brainSlot) inBrainSlot = organ;
            Debug.Log($"Placed {organ} ({fam}) in {selectedSlot.slotFamily} slot.");
        }
        else
        {
            // auto-snap by family
            if (fam == OrganFamily.Heart) { inHeartSlot = organ; Debug.Log($"Auto-placed {organ} in HEART slot."); }
            else if (fam == OrganFamily.Lungs) { inLungsSlot = organ; Debug.Log($"Auto-placed {organ} in LUNGS slot."); }
            else { inBrainSlot = organ; Debug.Log($"Auto-placed {organ} in BRAIN slot."); }
        }
    }

    private void EvaluateAndReact()
    {
        bool heartOk = EvaluateSlot(inHeartSlot, OrganFamily.Heart, correctHeart, "Heart");
        bool lungsOk = EvaluateSlot(inLungsSlot, OrganFamily.Lungs, correctLungs, "Lungs");
        bool brainOk = EvaluateSlot(inBrainSlot, OrganFamily.Brain, correctBrain, "Brain");

        bool allOk = heartOk && lungsOk && brainOk;

        if (allOk)
        {
            Debug.Log("All organs correct → Animal lives!");
            OnRoundSuccess?.Invoke();
            if (resetOnSuccess) StartCoroutine(RestartAfterDelay(reactionDuration));
        }
        else
        {
            Debug.Log("Some organs incorrect → Animal dies.");
            OnRoundFailure?.Invoke();
            if (resetOnFailure) StartCoroutine(RestartAfterDelay(reactionDuration));
        }
    }

    private bool EvaluateSlot(OrganId? placed, OrganFamily thisSlotFamily, OrganId correctId, string name)
    {
        if (!placed.HasValue)
        {
            Debug.Log($"{name} slot empty → WRONG (red)");
            return false;
        }

        OrganFamily placedFam = OrganHelpers.GetFamily(placed.Value);
        OrganFamily correctFam = OrganHelpers.GetFamily(correctId);

        if (placedFam != correctFam)
        {
            Debug.Log($"{name} slot has {placed.Value} → Correct organ family but wrong slot (orange).");
            return false;
        }

        if (placed.Value == correctId)
        {
            Debug.Log($"{name} slot has correct organ {placed.Value} → GOOD (green).");
            return true;
        }

        Debug.Log($"{name} slot has wrong variant {placed.Value} → WRONG (red).");
        return false;
    }

    private System.Collections.IEnumerator RestartAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        inHeartSlot = inLungsSlot = inBrainSlot = null;
        Debug.Log("=== Round reset ===");
    }
}
