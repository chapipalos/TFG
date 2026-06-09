using DialogueFramework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DialogueFramework
{
    public class QuestController : MonoBehaviour
    {
        public GameObject m_QuestEntryPrefab;
        public GameObject m_ObjectivePrefab;

        public Transform m_ContentQuestsParent;
        private RectTransform m_ContentQuestsRectTransform;
        public Transform m_ActiveQuestsContentParent;
        private RectTransform m_ActiveQuestsContentRectTransform;
        public Transform m_InProgressQuestsContentParent;
        private RectTransform m_InProgressQuestsContentRectTransform;
        public Transform m_CompletedQuestsContentParent;
        private RectTransform m_CompletedQuestsContentRectTransform;

        public Transform m_ContentObjectivesParent;

        private List<Button> m_QuestButtons = new();
        private List<GameObject> m_ObjectiveEntries = new();

        public TextMeshProUGUI m_QuestTitleText;
        public TextMeshProUGUI m_QuestStatusText;
        public TextMeshProUGUI m_QuestDescriptionText;

        private void Awake()
        {
            //m_ContentQuestsRectTransform = m_ContentQuestsParent.GetComponent<RectTransform>();
            //m_ActiveQuestsContentRectTransform = m_ActiveQuestsContentParent.GetComponent<RectTransform>();
            //m_InProgressQuestsContentRectTransform = m_InProgressQuestsContentParent.GetComponent<RectTransform>();
            //m_CompletedQuestsContentRectTransform = m_CompletedQuestsContentParent.GetComponent<RectTransform>();
        }

        public void OpenQuestPanel()
        {
            gameObject.SetActive(true);
            foreach ((QuestData data, QuestRuntimeData runtime) quest in QuestManager.m_Instance.GetAllQuests())
            {
                var entry = Instantiate(m_QuestEntryPrefab);
                switch (quest.runtime.m_Status)
                {
                    case QuestStatus.Active:
                        entry.transform.SetParent(m_ActiveQuestsContentParent, false);
                        break;
                    case QuestStatus.InProgress:
                        entry.transform.SetParent(m_InProgressQuestsContentParent, false);
                        break;
                    case QuestStatus.Completed:
                        entry.transform.SetParent(m_CompletedQuestsContentParent, false);
                        break;
                    default:
                        Destroy(entry);
                        continue;
                }
                entry.GetComponentInChildren<TextMeshProUGUI>().text = quest.data.s_QuestTitle;
                var button = entry.GetComponent<Button>();
                button.onClick.AddListener(() => SelectQuest(quest.data, quest.runtime));
                m_QuestButtons.Add(button);
            }
            if (m_QuestButtons.Count > 0)
            {
                m_QuestButtons[0].onClick.Invoke();
                //LayoutRebuilder.ForceRebuildLayoutImmediate(m_ContentQuestsRectTransform);
                //LayoutRebuilder.ForceRebuildLayoutImmediate(m_ActiveQuestsContentRectTransform);
                //LayoutRebuilder.ForceRebuildLayoutImmediate(m_InProgressQuestsContentRectTransform);
                //LayoutRebuilder.ForceRebuildLayoutImmediate(m_CompletedQuestsContentRectTransform);
            }
        }

        private void SelectQuest(QuestData data, QuestRuntimeData runtime)
        {
            m_QuestTitleText.text = data.s_QuestTitle;
            m_QuestDescriptionText.text = data.s_QuestDescription;
            m_QuestStatusText.text = runtime.m_Status.ToString();

            foreach (var objEntry in m_ObjectiveEntries)
                Destroy(objEntry);
            m_ObjectiveEntries.Clear();

            foreach (var obj in data.s_QuestObjectives)
            {
                bool completed = runtime.m_ObjectiveCompleted.TryGetValue(obj.s_OGuid, out bool v) && v;

                var objEntry = Instantiate(m_ObjectivePrefab, m_ContentObjectivesParent);
                objEntry.GetComponentInChildren<TextMeshProUGUI>().text = obj.s_ObjectiveDescription;
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
            gameObject.SetActive(false);
        }
    }
}
