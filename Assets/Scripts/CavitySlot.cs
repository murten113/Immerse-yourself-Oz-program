using UnityEngine;
using UnityEngine.UI;

public enum SlotState { Neutral, Correct, WrongPlace, Wrong }

[RequireComponent(typeof(Image))]
public class CavitySlot : MonoBehaviour
{
    [Header("Images")]
    public Image organImage;   // organ sprite
    public Image frameImage;   // tinted outline/background

    [Header("Colors")]
    public Color neutralColor = Color.white;
    public Color correctColor = new Color(0.5f, 1f, 0.5f);  // green
    public Color wrongPlaceColor = new Color(1f, 0.7f, 0.3f); // orange
    public Color wrongColor = new Color(1f, 0.5f, 0.5f);    // red

    [Header("Selection Outline (optional)")]
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

    public void SetOrganSprite(Sprite s)
    {
        if (organImage != null)
        {
            organImage.sprite = s;
            organImage.enabled = (s != null);
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

    public void SetNeutral()
    {
        SetState(SlotState.Neutral);
    }

    public void Clear()
    {
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
            case SlotState.WrongPlace: baseColor = wrongPlaceColor; break;
            case SlotState.Wrong: baseColor = wrongColor; break;
            case SlotState.Neutral: baseColor = neutralColor; break;
        }

        if (showSelection && isSelected)
        {
            // simple mix to indicate selection without extra UI
            baseColor = Color.Lerp(baseColor, selectedTint, selectedTintStrength);
        }

        frameImage.color = baseColor;
    }
}
