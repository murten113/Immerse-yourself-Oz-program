using UnityEngine;
using UnityEngine.UI;

public enum SlotState { Neutral, Correct, WrongPlace, Wrong, Empty }

[RequireComponent(typeof(Image))]
public class CavitySlot : MonoBehaviour
{
    [Header("Slot Family")]
    public OrganFamily slotFamily;

    [Header("UI")]
    public Image organImage;
    public Image frameImage;

    [Header("Animation (optional)")]
    public Animator organAnimator;

    [Header("Colors")]
    public Color neutralColor = Color.white;
    public Color correctColor = new Color(0.5f, 1f, 0.5f);
    public Color wrongPlaceColor = new Color(1f, 0.7f, 0.3f);
    public Color wrongColor = new Color(1f, 0.5f, 0.5f);

    [Header("Selection Tint")]
    public bool showSelection = true;
    public Color selectedTint = new Color(1f, 0.95f, 0.6f);
    public float selectedTintStrength = 0.3f;

    private SlotState state = SlotState.Neutral;
    private bool isSelected;

    void Reset()
    {
        var img = GetComponent<Image>();
        if (organImage == null) organImage = img;
        if (frameImage == null) frameImage = img;
    }

    public void SetOrganSprite(Sprite sprite)
    {
        if (organImage != null)
        {
            organImage.sprite = sprite;
            organImage.enabled = (sprite != null);
        }
        StopAnimation();
        Debug.Log($"{slotFamily} slot shows sprite: {(sprite ? sprite.name : "none")}");
    }

    public void SetOrganAnimation(RuntimeAnimatorController controller)
    {
        if (organAnimator == null)
        {
            Debug.LogWarning($"{slotFamily} slot has no Animator assigned.");
            return;
        }

        if (controller != null)
        {
            organAnimator.runtimeAnimatorController = controller;
            organAnimator.enabled = true;
            Debug.Log($"{slotFamily} slot playing animation: {controller.name}");
        }
        else
        {
            StopAnimation();
        }
    }

    private void StopAnimation()
    {
        if (organAnimator != null)
        {
            organAnimator.enabled = false;
            organAnimator.runtimeAnimatorController = null;
        }
    }

    public void SetState(SlotState s)
    {
        state = s;
        ApplyColors();
    }

    public void SetSelectedVisual(bool selected)
    {
        isSelected = selected;
        ApplyColors();
    }

    public void ClearIfEmpty()
    {
        if(state == SlotState.Empty)
        {
            Clear();
        }
    }

    public void Clear()
    {
        StopAnimation();
        if (organImage != null)
        {
            organImage.sprite = null;
            organImage.enabled = false;
        }
        SetState(SlotState.Neutral);
    }

    private void ApplyColors()
    {
        if (frameImage == null) return;

        Color baseColor = neutralColor;
        switch (state)
        {
            case SlotState.Correct: baseColor = correctColor; break;
            case SlotState.Empty: baseColor = wrongColor; break;
            case SlotState.WrongPlace: baseColor = wrongPlaceColor; break;
            case SlotState.Wrong: baseColor = wrongColor; break;
        }

        if (showSelection && isSelected)
            baseColor = Color.Lerp(baseColor, selectedTint, selectedTintStrength);

        frameImage.color = baseColor;
    }
}
