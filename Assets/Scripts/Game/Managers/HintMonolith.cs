using UnityEngine;
using TMPro;

public class HintMonolith : MonoBehaviour, IInteractable
{
    public TextMeshPro screenText;
    public ParticleSystem dustParticles;
    public AudioSource activationSound;
    public MeshRenderer screenRenderer; 

    private Color originalTextColor;
    private Color originalScreenEmission;

    private void Awake()
    {
        if (screenText != null) originalTextColor = screenText.color;
        
        if (screenRenderer != null && screenRenderer.material.HasProperty("_EmissionColor"))
        {
            originalScreenEmission = screenRenderer.material.GetColor("_EmissionColor");
        }

        SetVisualsActive(false);
    }

    // Allow the player to physically interact with the Monolith instead of pressing H!
    public string GetInteractionPrompt()
    {
        return "Press E to Consult the Oracle";
    }

    public void Interact()
    {
        HintManager manager = FindObjectOfType<HintManager>();
        if (manager != null)
        {
            manager.ShowHint();
        }
    }

    public void SetText(string text)
    {
        if (screenText != null) screenText.text = text;
    }

    public void SetVisualsActive(bool active)
    {
        if (active && activationSound != null) activationSound.Play();
        if (active && dustParticles != null) dustParticles.Play();
        else if (!active && dustParticles != null) dustParticles.Stop();

        if (screenText != null) screenText.gameObject.SetActive(active);
        
        if (screenRenderer != null)
        {
            if (active) screenRenderer.material.EnableKeyword("_EMISSION");
            else screenRenderer.material.DisableKeyword("_EMISSION");
        }
    }

    public void SetFadeLevel(float alpha)
    {
        if (screenText != null)
        {
            screenText.color = new Color(originalTextColor.r, originalTextColor.g, originalTextColor.b, alpha);
        }

        if (screenRenderer != null && screenRenderer.material.HasProperty("_EmissionColor"))
        {
            screenRenderer.material.SetColor("_EmissionColor", originalScreenEmission * alpha);
        }
    }
}
