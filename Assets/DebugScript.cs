using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class DebugScript : MonoBehaviour
{
    [SerializeField] private ToggleGroup toggleGroup;

    public void TestCall()
    {
        Debug.Log($"[UI] Event from: {GetCallerName()}", gameObject);
        Debug.Log($"{toggleGroup.ActiveToggles().FirstOrDefault()}");
    }

    private string GetCallerName()
    {
        // Try to get the GameObject that sent this event
        var sender = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
        return sender != null ? sender.name : "Unknown Sender";
    }
}
