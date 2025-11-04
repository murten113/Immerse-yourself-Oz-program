using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum OrganFamily
{
    Heart,
    Lungs,
    Brain
}

public enum OrganId
{
    Heart1 = 1,
    Heart2 = 2,
    Heart3 = 3,
    Lungs1 = 4,
    Lungs2 = 5,
    Lungs3 = 6,
    Brain1 = 7,
    Brain2 = 8,
    Brain3 = 9
}

public static class OrganHelpers
{
    public static OrganFamily GetFamily(OrganId id)
    {
        switch (id)
        {
            case OrganId.Heart1:
            case OrganId.Heart2:
            case OrganId.Heart3:
                return OrganFamily.Heart;
            case OrganId.Lungs1:
            case OrganId.Lungs2:
            case OrganId.Lungs3:
                return OrganFamily.Lungs;
            default:
                return OrganFamily.Brain;
        }
    }

    public static int GetVariant(OrganId id)
    {
        switch (id)
        {
            case OrganId.Heart1: return 1;
            case OrganId.Heart2: return 2;
            case OrganId.Heart3: return 3;
            case OrganId.Lungs1: return 1;
            case OrganId.Lungs2: return 2;
            case OrganId.Lungs3: return 3;
            case OrganId.Brain1: return 1;
            case OrganId.Brain2: return 2;
            case OrganId.Brain3: return 3;
            default: return 0;
        }
    }
}

public class OrganPlacementGame : MonoBehaviour
{
    [Header("UI Slots")]
    public CavitySlot slotA;
    public CavitySlot slotB;
    public CavitySlot slotC;

    [Header("Correct Targets (exact organ id per slot)")]
    public OrganId correctForA = OrganId.Heart1;
    public OrganId correctForB = OrganId.Lungs1;
    public OrganId correctForC = OrganId.Brain1;

    [Header("Sprites (map each OrganId to a sprite)")]
    public List<OrganSpritePair> organSprites = new List<OrganSpritePair>();

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip sfxWrong;              // wrong family
    public AudioClip sfxRightWrongPlace;    // right family, wrong variant
    public AudioClip sfxCorrect;            // right family, right variant

    private readonly Dictionary<KeyCode, OrganId> keyToOrgan = new Dictionary<KeyCode, OrganId>();
    private readonly Dictionary<OrganId, Sprite> idToSprite = new Dictionary<OrganId, Sprite>();
    private CavitySlot selectedSlot;

    void Awake()
    {
        // 1–9 mapping
        keyToOrgan[KeyCode.Alpha1] = OrganId.Heart1;
        keyToOrgan[KeyCode.Alpha2] = OrganId.Heart2;
        keyToOrgan[KeyCode.Alpha3] = OrganId.Heart3;
        keyToOrgan[KeyCode.Alpha4] = OrganId.Lungs1;
        keyToOrgan[KeyCode.Alpha5] = OrganId.Lungs2;
        keyToOrgan[KeyCode.Alpha6] = OrganId.Lungs3;
        keyToOrgan[KeyCode.Alpha7] = OrganId.Brain1;
        keyToOrgan[KeyCode.Alpha8] = OrganId.Brain2;
        keyToOrgan[KeyCode.Alpha9] = OrganId.Brain3;

        idToSprite.Clear();
        foreach (var pair in organSprites)
        {
            if (!idToSprite.ContainsKey(pair.id) && pair.sprite != null)
                idToSprite.Add(pair.id, pair.sprite);
        }

        SelectSlot(slotA);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A)) SelectSlot(slotA);
        if (Input.GetKeyDown(KeyCode.B)) SelectSlot(slotB);
        if (Input.GetKeyDown(KeyCode.C)) SelectSlot(slotC);

        foreach (var kvp in keyToOrgan)
        {
            if (Input.GetKeyDown(kvp.Key))
            {
                TryPlaceOrgan(kvp.Value);
                break;
            }
        }
    }

    private void SelectSlot(CavitySlot slot)
    {
        if (slot == null) return;
        selectedSlot = slot;
        slotA.SetSelected(slot == slotA);
        slotB.SetSelected(slot == slotB);
        slotC.SetSelected(slot == slotC);
    }

    private void TryPlaceOrgan(OrganId chosen)
    {
        if (selectedSlot == null) return;

        OrganId target = GetTargetForSlot(selectedSlot);

        bool familyMatch = OrganHelpers.GetFamily(chosen) == OrganHelpers.GetFamily(target);
        bool variantMatch = chosen == target;

        // Show the placement first
        Sprite spr = idToSprite.TryGetValue(chosen, out var s) ? s : null;
        selectedSlot.SetOrgan(chosen, spr);

        if (!familyMatch)
        {
            // Wrong family -> doesn't work, clear
            selectedSlot.BlinkWrongThenClear();
            Play(sfxWrong);
            return;
        }

        if (familyMatch && !variantMatch)
        {
            // Right family, wrong variant -> special sound, keep visible
            selectedSlot.PulseWarning();
            Play(sfxRightWrongPlace);
            return;
        }

        // Exact match
        selectedSlot.LockIn();
        Play(sfxCorrect);

        if (AllCorrectAndLocked())
        {
            Debug.Log("All organs placed correctly.");
        }
    }

    private OrganId GetTargetForSlot(CavitySlot slot)
    {
        if (slot == slotA) return correctForA;
        if (slot == slotB) return correctForB;
        return correctForC;
    }

    private bool AllCorrectAndLocked()
    {
        return slotA.IsLockedAndCorrect(correctForA)
            && slotB.IsLockedAndCorrect(correctForB)
            && slotC.IsLockedAndCorrect(correctForC);
    }

    private void Play(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    [Serializable]
    public class OrganSpritePair
    {
        public OrganId id;
        public Sprite sprite;
    }
}

[RequireComponent(typeof(Image))]
public class CavitySlot : MonoBehaviour
{
    [Header("UI")]
    public Image organImage;   // sprite shown for placed organ
    public Image borderImage;  // border highlight

    [Header("Colors")]
    public Color normalBorder = Color.white;
    public Color selectedBorder = new Color(1f, 0.85f, 0.3f);
    public Color lockedBorder = new Color(0.5f, 1f, 0.5f);

    [Header("Feedback")]
    public float blinkDuration = 0.25f;

    private OrganId? currentOrgan;
    private bool locked;

    void Reset()
    {
        var img = GetComponent<Image>();
        if (organImage == null) organImage = img;
        if (borderImage == null) borderImage = img;
    }

    public void SetSelected(bool isSelected)
    {
        if (borderImage != null && !locked)
            borderImage.color = isSelected ? selectedBorder : normalBorder;
    }

    public void SetOrgan(OrganId organ, Sprite sprite)
    {
        currentOrgan = organ;
        if (organImage != null)
        {
            organImage.sprite = sprite;
            organImage.enabled = (sprite != null);
        }
    }

    public void BlinkWrongThenClear()
    {
        if (!locked) StartCoroutine(BlinkAndClearRoutine());
    }

    public void PulseWarning()
    {
        if (!locked) StartCoroutine(PulseRoutine());
    }

    public void LockIn()
    {
        locked = true;
        if (borderImage != null) borderImage.color = lockedBorder;
    }

    public bool IsLockedAndCorrect(OrganId correctOrgan)
    {
        return locked && currentOrgan.HasValue && currentOrgan.Value.Equals(correctOrgan);
    }

    private System.Collections.IEnumerator BlinkAndClearRoutine()
    {
        Color pre = borderImage != null ? borderImage.color : Color.white;
        if (borderImage != null) borderImage.color = Color.red;
        yield return new WaitForSeconds(blinkDuration);
        if (borderImage != null) borderImage.color = pre;

        currentOrgan = null;
        if (organImage != null)
        {
            organImage.sprite = null;
            organImage.enabled = false;
        }
    }

    private System.Collections.IEnumerator PulseRoutine()
    {
        Color pre = borderImage != null ? borderImage.color : Color.white;
        if (borderImage != null) borderImage.color = new Color(1f, 0.6f, 0.3f);
        yield return new WaitForSeconds(blinkDuration);
        if (borderImage != null) borderImage.color = pre;
    }
}
