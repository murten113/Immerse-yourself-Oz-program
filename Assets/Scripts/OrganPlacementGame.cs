using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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

    public static int GetVariant(OrganId id)
    {
        switch (id)
        {
            case OrganId.Heart1:
            case OrganId.Lungs1:
            case OrganId.Brain1: return 1;
            case OrganId.Heart2:
            case OrganId.Lungs2:
            case OrganId.Brain2: return 2;
            case OrganId.Heart3:
            case OrganId.Lungs3:
            case OrganId.Brain3: return 3;
            default: return 0;
        }
    }
}

public class OrganPlacementGame : MonoBehaviour
{
    [Header("Slots (fixed family per slot)")]
    public CavitySlot heartSlot; // represents the Heart cavity
    public CavitySlot lungsSlot; // represents the Lungs cavity
    public CavitySlot brainSlot; // represents the Brain cavity

    [Header("Exact correct target per slot (variant matters)")]
    public OrganId correctHeart = OrganId.Heart1;
    public OrganId correctLungs = OrganId.Lungs1;
    public OrganId correctBrain = OrganId.Brain1;

    [Header("Sprites (map each OrganId to a sprite)")]
    public List<OrganSpritePair> organSprites = new List<OrganSpritePair>();

    [Header("Audio (optional)")]
    public AudioSource audioSource;
    public AudioClip sfxFailure;
    public AudioClip sfxSuccess;

    [Header("Round flow")]
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

    // Input maps
    private readonly Dictionary<KeyCode, OrganId> keyToOrgan = new Dictionary<KeyCode, OrganId>();
    private readonly Dictionary<OrganId, Sprite> idToSprite = new Dictionary<OrganId, Sprite>();

    // What’s currently inside each visible slot (could be wrong place)
    private OrganId? inHeartSlot;
    private OrganId? inLungsSlot;
    private OrganId? inBrainSlot;

    // Selection when manual placement is enabled
    private CavitySlot selectedSlot;

    [Serializable]
    public class OrganSpritePair
    {
        public OrganId id;
        public Sprite sprite;
    }

    void Awake()
    {
        // 1–9 → organs
        keyToOrgan[KeyCode.Alpha1] = OrganId.Heart1;
        keyToOrgan[KeyCode.Alpha2] = OrganId.Heart2;
        keyToOrgan[KeyCode.Alpha3] = OrganId.Heart3;
        keyToOrgan[KeyCode.Alpha4] = OrganId.Lungs1;
        keyToOrgan[KeyCode.Alpha5] = OrganId.Lungs2;
        keyToOrgan[KeyCode.Alpha6] = OrganId.Lungs3;
        keyToOrgan[KeyCode.Alpha7] = OrganId.Brain1;
        keyToOrgan[KeyCode.Alpha8] = OrganId.Brain2;
        keyToOrgan[KeyCode.Alpha9] = OrganId.Brain3;

        // build sprite lookup
        idToSprite.Clear();
        foreach (var p in organSprites)
            if (p != null && p.sprite != null && !idToSprite.ContainsKey(p.id))
                idToSprite.Add(p.id, p.sprite);

        // start neutral
        ClearAll();
        selectedSlot = heartSlot; // default selection
        HighlightSelection();
    }

    void Update()
    {
        // Select cavity when manual placement is on
        if (manualPlacement)
        {
            if (Input.GetKeyDown(KeyCode.A)) { selectedSlot = heartSlot; HighlightSelection(); }
            if (Input.GetKeyDown(KeyCode.B)) { selectedSlot = lungsSlot; HighlightSelection(); }
            if (Input.GetKeyDown(KeyCode.C)) { selectedSlot = brainSlot; HighlightSelection(); }
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
        EvaluateAndReact();
    }

    private void PlaceOrgan(OrganId organ)
    {
        var spr = idToSprite.TryGetValue(organ, out var s) ? s : null;

        if (manualPlacement && selectedSlot != null)
        {
            // Put the organ where the player selected (even if it's the "wrong" family)
            if (selectedSlot == heartSlot) inHeartSlot = organ;
            else if (selectedSlot == lungsSlot) inLungsSlot = organ;
            else if (selectedSlot == brainSlot) inBrainSlot = organ;

            selectedSlot.SetOrganSprite(spr);
        }
        else
        {
            // Auto-snap by family (optional mode)
            var fam = OrganHelpers.GetFamily(organ);
            if (fam == OrganFamily.Heart) { inHeartSlot = organ; heartSlot.SetOrganSprite(spr); }
            else if (fam == OrganFamily.Lungs) { inLungsSlot = organ; lungsSlot.SetOrganSprite(spr); }
            else { inBrainSlot = organ; brainSlot.SetOrganSprite(spr); }
        }

        // Reset colors to neutral whenever a change is made
        heartSlot.SetState(SlotState.Neutral);
        lungsSlot.SetState(SlotState.Neutral);
        brainSlot.SetState(SlotState.Neutral);

        // Keep a subtle highlight on the selected slot (optional visual)
        HighlightSelection();
    }

    private void EvaluateAndReact()
    {
        // For each slot:
        // Green  = chosen family matches this slot AND variant matches the slot's correct OrganId.
        // Orange = chosen family is one of the correct families but placed in the wrong slot (family mismatch with this slot).
        // Red    = empty OR wrong variant in correct slot OR any other mismatch.

        bool heartOk = EvaluateSlot(inHeartSlot, OrganFamily.Heart, correctHeart, heartSlot);
        bool lungsOk = EvaluateSlot(inLungsSlot, OrganFamily.Lungs, correctLungs, lungsSlot);
        bool brainOk = EvaluateSlot(inBrainSlot, OrganFamily.Brain, correctBrain, brainSlot);

        bool allOk = heartOk && lungsOk && brainOk;

        if (allOk)
        {
            OnRoundSuccess?.Invoke();
            PlayOneShot(sfxSuccess);
            if (resetOnSuccess) StartCoroutine(RestartAfterDelay(reactionDuration));
        }
        else
        {
            OnRoundFailure?.Invoke();
            PlayOneShot(sfxFailure);
            if (resetOnFailure) StartCoroutine(RestartAfterDelay(reactionDuration));
        }
    }

    private bool EvaluateSlot(OrganId? placed, OrganFamily thisSlotFamily, OrganId correctIdForThisSlot, CavitySlot slot)
    {
        if (!placed.HasValue)
        {
            slot.SetState(SlotState.Wrong); // empty = red
            return false;
        }

        var placedFam = OrganHelpers.GetFamily(placed.Value);
        var correctFam = OrganHelpers.GetFamily(correctIdForThisSlot); // equals thisSlotFamily by design

        if (placedFam != correctFam)
        {
            // Correct organ family exists, but placed in the wrong cavity => orange
            slot.SetState(SlotState.WrongPlace);
            return false;
        }

        // Family matches this slot; now check variant
        if (placed.Value == correctIdForThisSlot)
        {
            slot.SetState(SlotState.Correct); // green
            return true;
        }
        else
        {
            // Right slot (family), wrong variant
            slot.SetState(SlotState.Wrong); // red
            return false;
        }
    }

    private System.Collections.IEnumerator RestartAfterDelay(float seconds)
    {
        if (seconds > 0f) yield return new WaitForSeconds(seconds);
        ClearAll();
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
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }

    private void HighlightSelection()
    {
        if (!manualPlacement) return;
        heartSlot.SetSelectedVisual(selectedSlot == heartSlot);
        lungsSlot.SetSelectedVisual(selectedSlot == lungsSlot);
        brainSlot.SetSelectedVisual(selectedSlot == brainSlot);
    }
}
