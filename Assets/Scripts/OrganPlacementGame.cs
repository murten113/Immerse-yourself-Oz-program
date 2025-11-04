using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

[Serializable]
public class OrganVisualPair
{
    public OrganId id;
    public Sprite sprite;                        // optional static image
    public RuntimeAnimatorController animation;  // optional looping animation
}

public class OrganPlacementGame : MonoBehaviour
{
    [Header("Slots (fixed family per slot)")]
    public CavitySlot heartSlot;
    public CavitySlot lungsSlot;
    public CavitySlot brainSlot;

    [Header("Exact correct target per slot (variant matters)")]
    public OrganId correctHeart = OrganId.Heart1;
    public OrganId correctLungs = OrganId.Lungs1;
    public OrganId correctBrain = OrganId.Brain1;

    [Header("Visuals (Sprite or Animation per organ)")]
    public List<OrganVisualPair> organVisuals = new List<OrganVisualPair>();

    [Header("Audio (optional)")]
    public AudioSource audioSource;
    public AudioClip sfxFailure;
    public AudioClip sfxSuccess;

    [Header("Round flow")]
    [Tooltip("Seconds to wait after reaction before resetting on failure/success when enabled.")]
    public float reactionDuration = 1.5f;
    public bool resetOnFailure = true;
    public bool resetOnSuccess = false;

    [Header("Events to hook custom audio/FX")]
    public UnityEvent OnLeverPulled;
    public UnityEvent OnRoundSuccess;
    public UnityEvent OnRoundFailure;
    public UnityEvent OnRoundClearedForRestart;

    [Header("Placement Controls")]
    [Tooltip("Manual placement: select cavity with A/B/C, then press 1–9 to place there.")]
    public bool manualPlacement = true;

    // Input and lookups
    private readonly Dictionary<KeyCode, OrganId> keyToOrgan = new Dictionary<KeyCode, OrganId>();
    private readonly Dictionary<OrganId, OrganVisualPair> idToVisual = new Dictionary<OrganId, OrganVisualPair>();

    // What’s currently inside each visible slot (may be wrong place)
    private OrganId? inHeartSlot;
    private OrganId? inLungsSlot;
    private OrganId? inBrainSlot;

    // Selected slot for manual placement
    private CavitySlot selectedSlot;

    void Awake()
    {
        // key → organ map
        keyToOrgan[KeyCode.Alpha1] = OrganId.Heart1;
        keyToOrgan[KeyCode.Alpha2] = OrganId.Heart2;
        keyToOrgan[KeyCode.Alpha3] = OrganId.Heart3;
        keyToOrgan[KeyCode.Alpha4] = OrganId.Lungs1;
        keyToOrgan[KeyCode.Alpha5] = OrganId.Lungs2;
        keyToOrgan[KeyCode.Alpha6] = OrganId.Lungs3;
        keyToOrgan[KeyCode.Alpha7] = OrganId.Brain1;
        keyToOrgan[KeyCode.Alpha8] = OrganId.Brain2;
        keyToOrgan[KeyCode.Alpha9] = OrganId.Brain3;

        // visuals lookup
        idToVisual.Clear();
        foreach (var v in organVisuals)
        {
            if (v != null && !idToVisual.ContainsKey(v.id))
                idToVisual.Add(v.id, v);
        }

        ClearAll();
        selectedSlot = heartSlot; // default selection for manual mode
        HighlightSelection();

        Debug.Log("Game ready. A/B/C select cavity, 1–9 place organ (sprite/animation), Space pulls the lever.");
    }

    void Update()
    {
        // Select cavity (manual mode)
        if (manualPlacement)
        {
            if (Input.GetKeyDown(KeyCode.A)) { selectedSlot = heartSlot; Debug.Log("Selected cavity: HEART"); HighlightSelection(); }
            if (Input.GetKeyDown(KeyCode.B)) { selectedSlot = lungsSlot; Debug.Log("Selected cavity: LUNGS"); HighlightSelection(); }
            if (Input.GetKeyDown(KeyCode.C)) { selectedSlot = brainSlot; Debug.Log("Selected cavity: BRAIN"); HighlightSelection(); }
        }

        // Place organ
        foreach (var kv in keyToOrgan)
        {
            if (Input.GetKeyDown(kv.Key))
            {
                PlaceOrgan(kv.Value);
                break;
            }
        }

        // Lever
        if (Input.GetKeyDown(KeyCode.Space))
            PullLever();
    }

    public void PullLever()
    {
        OnLeverPulled?.Invoke();
        Debug.Log("=== Lever pulled ===");
        EvaluateAndReact();
    }

    private void PlaceOrgan(OrganId organ)
    {
        OrganVisualPair visual = idToVisual.TryGetValue(organ, out var v) ? v : null;

        if (manualPlacement && selectedSlot != null)
        {
            if (selectedSlot == heartSlot) inHeartSlot = organ;
            else if (selectedSlot == lungsSlot) inLungsSlot = organ;
            else if (selectedSlot == brainSlot) inBrainSlot = organ;

            selectedSlot.SetOrganSprite(visual?.sprite);
            selectedSlot.SetOrganAnimation(visual?.animation);
            Debug.Log($"Placed {organ} ({OrganHelpers.GetFamily(organ)}) in {selectedSlot.slotFamily} slot.");
        }
        else
        {
            // Auto-snap by family
            var fam = OrganHelpers.GetFamily(organ);
            if (fam == OrganFamily.Heart)
            {
                inHeartSlot = organ;
                heartSlot.SetOrganSprite(visual?.sprite);
                heartSlot.SetOrganAnimation(visual?.animation);
                Debug.Log($"Auto-placed {organ} in HEART slot.");
            }
            else if (fam == OrganFamily.Lungs)
            {
                inLungsSlot = organ;
                lungsSlot.SetOrganSprite(visual?.sprite);
                lungsSlot.SetOrganAnimation(visual?.animation);
                Debug.Log($"Auto-placed {organ} in LUNGS slot.");
            }
            else
            {
                inBrainSlot = organ;
                brainSlot.SetOrganSprite(visual?.sprite);
                brainSlot.SetOrganAnimation(visual?.animation);
                Debug.Log($"Auto-placed {organ} in BRAIN slot.");
            }
        }

        // Reset visual state to neutral whenever a placement changes
        heartSlot.SetState(SlotState.Neutral);
        lungsSlot.SetState(SlotState.Neutral);
        brainSlot.SetState(SlotState.Neutral);
        HighlightSelection();
    }

    private void EvaluateAndReact()
    {
        bool heartOk = EvaluateSlot(inHeartSlot, OrganFamily.Heart, correctHeart, heartSlot);
        bool lungsOk = EvaluateSlot(inLungsSlot, OrganFamily.Lungs, correctLungs, lungsSlot);
        bool brainOk = EvaluateSlot(inBrainSlot, OrganFamily.Brain, correctBrain, brainSlot);

        bool allOk = heartOk && lungsOk && brainOk;

        if (allOk)
        {
            Debug.Log("All organs correct → Animal LIVES.");
            OnRoundSuccess?.Invoke();
            PlayOneShot(sfxSuccess);
            if (resetOnSuccess) StartCoroutine(RestartAfterDelay(reactionDuration));
        }
        else
        {
            Debug.Log("One or more organs wrong → Animal DIES.");
            OnRoundFailure?.Invoke();
            PlayOneShot(sfxFailure);
            if (resetOnFailure) StartCoroutine(RestartAfterDelay(reactionDuration));
        }
    }

    private bool EvaluateSlot(OrganId? placed, OrganFamily thisSlotFamily, OrganId correctId, CavitySlot slot)
    {
        string slotName = thisSlotFamily.ToString().ToUpper();

        if (!placed.HasValue)
        {
            Debug.Log($"{slotName} slot is EMPTY → RED");
            slot.SetState(SlotState.Wrong);
            return false;
        }

        OrganFamily placedFam = OrganHelpers.GetFamily(placed.Value);
        OrganFamily correctFam = OrganHelpers.GetFamily(correctId); // equals this slot family by design

        if (placedFam != correctFam)
        {
            // Right family but wrong cavity → orange
            Debug.Log($"{slotName} slot has {placed.Value} → ORANGE (correct family, wrong slot)");
            slot.SetState(SlotState.WrongPlace);
            return false;
        }

        if (placed.Value == correctId)
        {
            Debug.Log($"{slotName} slot has {placed.Value} → GREEN (correct)");
            slot.SetState(SlotState.Correct);
            return true;
        }

        Debug.Log($"{slotName} slot has {placed.Value} → RED (wrong variant)");
        slot.SetState(SlotState.Wrong);
        return false;
    }

    private System.Collections.IEnumerator RestartAfterDelay(float seconds)
    {
        if (seconds > 0f) yield return new WaitForSeconds(seconds);
        ClearAll();
        Debug.Log("=== Round reset ===");
        OnRoundClearedForRestart?.Invoke();
    }

    private void ClearAll()
    {
        inHeartSlot = inLungsSlot = inBrainSlot = null;
        heartSlot.Clear();
        lungsSlot.Clear();
        brainSlot.Clear();
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
            Debug.Log($"Playing sound: {clip.name}");
        }
    }

    private void HighlightSelection()
    {
        if (!manualPlacement) return;
        heartSlot.SetSelectedVisual(selectedSlot == heartSlot);
        lungsSlot.SetSelectedVisual(selectedSlot == lungsSlot);
        brainSlot.SetSelectedVisual(selectedSlot == brainSlot);
    }
}
