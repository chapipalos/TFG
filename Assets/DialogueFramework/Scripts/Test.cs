using DialogueFramework;
using UnityEngine;
using UnityEngine.InputSystem;

public class Test : MonoBehaviour
{
    public DialogueManager dialogue;

    private PlayerInput playerInput;

    public QuestController quest;
    public GameObject questPanel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    // Update is called once per frame
    void Update()
    {
        if (playerInput.actions["Interact"].WasPerformedThisFrame())
        {
            if (!dialogue.startDialogue)
            {
                dialogue.StartDialogue();
            }
        }

        if (playerInput.actions["Test"].WasPerformedThisFrame())
        {
            GameEventBus.Raise("OnHierroRecogido");
        }

        if (playerInput.actions["Quest"].WasPerformedThisFrame())
        {
            if (questPanel.activeSelf)
            {
                questPanel.SetActive(false);
                quest.CloseQuestPanel();
            }
            else
            {
                questPanel.SetActive(true);
                quest.OpenQuestPanel();
            }
        }
    }
}
