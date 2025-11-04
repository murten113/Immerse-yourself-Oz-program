using UnityEngine;
using UnityEngine.UI;

public enum SlotState { Neutral, Correct, WrongPlace, Wrong }

[RequireComponent(typeof(Image))]
public class CavitySlot : MonoBehaviour
{
    [Header("Slot Family")]
    public OrganFamily slotFamily;

    [Header("UI Images")]
    public Image organImage;
    public Image frameImage;

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

    public void SetOrganSprite(Sprite s)
    {
        if (organImage != null)
        {
            organImage.sprite = s;
            organImage.enabled = (s != null);
        }
        Debug.Log($"{slotFamily} slot now shows {(s ? s.name : "no sprite")}");
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
        }

        if (showSelection && isSelected)
            baseColor = Color.Lerp(baseColor, selectedTint, selectedTintStrength);

        frameImage.color = baseColor;
        Debug.Log($"{slotFamily} slot color set to {state}");
    }
}
