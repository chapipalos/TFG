using DialogueFramework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestController : MonoBehaviour
{
    public GameObject m_QuestEntryPrefab;
    public GameObject m_ObjectivePrefab;

    public Transform m_ContentQuestsParent;
    public Transform m_ContentObjectivesParent;

    private List<Button> m_QuestButtons = new();
    private List<GameObject> m_ObjectiveEntries = new();

    public TextMeshProUGUI m_QuestTitleText;
    public TextMeshProUGUI m_QuestStatusText;
    public TextMeshProUGUI m_QuestDescriptionText;

    public void OpenQuestPanel()
    {
        foreach ((QuestData data, QuestRuntimeData runtime) quest in QuestManager.Instance.GetAllQuests())
        {
            var entry = Instantiate(m_QuestEntryPrefab, m_ContentQuestsParent);
            entry.GetComponentInChildren<TextMeshProUGUI>().text = quest.data.title;
            var button = entry.GetComponent<Button>();
            button.onClick.AddListener(() => SelectQuest(quest.data, quest.runtime));
            m_QuestButtons.Add(button);
        }
        m_QuestButtons[0].onClick.Invoke();
    }

    private void SelectQuest(QuestData data, QuestRuntimeData runtime)
    {
        m_QuestTitleText.text = data.title;
        m_QuestDescriptionText.text = data.description;
        m_QuestStatusText.text = runtime.Status.ToString();

        foreach (var objEntry in m_ObjectiveEntries)
            Destroy(objEntry);
        m_ObjectiveEntries.Clear();

        foreach (var obj in data.objectives)
        {
            bool completed = runtime.ObjectiveCompleted.TryGetValue(obj.guid, out bool v) && v;

            var objEntry = Instantiate(m_ObjectivePrefab, m_ContentObjectivesParent);
            objEntry.GetComponentInChildren<TextMeshProUGUI>().text = obj.description;
            objEntry.GetComponentInChildren<Toggle>().isOn = completed;
            m_ObjectiveEntries.Add(objEntry);
        }
    }

    public void CloseQuestPanel()
    {
        foreach (var button in m_QuestButtons)
            Destroy(button.gameObject);
        m_QuestButtons.Clear();

        foreach (var objEntry in m_ObjectiveEntries)
            Destroy(objEntry);

        m_QuestButtons.Clear();
        m_QuestTitleText.text = string.Empty;
        m_QuestDescriptionText.text = string.Empty;
        m_QuestStatusText.text = string.Empty;
    }
}
