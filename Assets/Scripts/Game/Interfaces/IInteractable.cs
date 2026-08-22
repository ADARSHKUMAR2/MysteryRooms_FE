public interface IInteractable
{
    string GetInteractionPrompt(); // E.g., "Press E to Open Door"
    void Interact(); // What happens when player presses E
}
