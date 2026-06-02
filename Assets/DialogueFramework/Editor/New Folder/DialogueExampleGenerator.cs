#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DialogueFramework.Editor
{
    public static class DialogueExampleGenerator
    {
        [MenuItem("Tools/Dialogue Framework/Generate Example Graph")]
        public static void Generate()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Example Graph", "ExampleDialogue", "asset",
                "Choose where to save the example GraphData");
            if (string.IsNullOrEmpty(path)) return;

            var graph = ScriptableObject.CreateInstance<GraphData>();

            // ── Actors ────────────────────────────────────────────────────────
            var aldric = new ActorData
            {
                s_AGuid = NewGuid(),
                s_ActorName = "Aldric the Blacksmith"
            };
            graph.s_Actors.Add(aldric);

            // ── Conversation ──────────────────────────────────────────────────
            // All nodes in this example belong to the same conversation.
            var conversation = new ConversationData
            {
                s_CGuid = NewGuid(),
                s_CName = "Aldric_Forge"
            };
            graph.s_Conversations.Add(conversation);

            // ── Quest ─────────────────────────────────────────────────────────
            string questGuid = NewGuid();
            string objGetIron = NewGuid();
            string objDeliverIron = NewGuid();

            var questSword = new QuestData
            {
                s_QGuid = questGuid,
                s_QuestTitle = "The Dawn Sword",
                s_QuestDescription = "Get iron from the northern mines and deliver it to Aldric.",
                s_QuestObjectives = new List<QuestObjectiveData>
                {
                    new() { s_OGuid = objGetIron,     s_ObjectiveDescription = "Get iron",            s_RequiredCompletedState = true },
                    new() { s_OGuid = objDeliverIron, s_ObjectiveDescription = "Deliver iron to Aldric", s_RequiredCompletedState = true }
                }
            };
            graph.s_Quests.Add(questSword);

            // ── Nodes ─────────────────────────────────────────────────────────
            //
            // FLOW:
            //  [intro]
            //    ├─(quest Completed)                  → [afterComplete]                  → end
            //    ├─(objective "Get iron" completed)   → [hasIron *EFFECT: complete Deliver*] → end
            //    ├─(quest Active + no iron)           → [noIron]                         → end
            //    ├─(quest InProgress + no iron)       → [noIronInProgress]               → end
            //    └─(quest NotStarted)                 → [offerQuest]
            //          ├─[Accept]                     → [questStarted *EFFECT: Start quest*] → end
            //          └─[Refuse]                     → [questRefused]                   → end

            float x = 100, y = 300, dx = 280, dy = 180;

            var nodeIntro = Node(graph, conversation.s_CGuid, aldric.s_AGuid, V(x, y),
                "Intro",
                "Welcome to my forge, traveler! What can I do for you?");

            // Branch A — quest not started
            var nodeOffer = Node(graph, conversation.s_CGuid, aldric.s_AGuid, V(x + dx, y - dy * 2f),
                "Quest offer",
                "I've been looking for someone with guts. I need iron from the Northern Mines. Are you interested?",
                questReqs: new[] { (questGuid, QuestStatus.NotStarted) });

            var replyAccept = new PlayerReplyData { s_RGuid = NewGuid(), s_ReplyText = "I accept. What do I need to do?" };
            var replyRefuse = new PlayerReplyData { s_RGuid = NewGuid(), s_ReplyText = "Not a good time for me." };
            nodeOffer.s_Replies = new List<PlayerReplyData> { replyAccept, replyRefuse };

            var nodeStarted = Node(graph, conversation.s_CGuid, aldric.s_AGuid, V(x + dx * 2, y - dy * 2.5f),
                "Quest accepted",
                "Excellent. The mines are north. When you have the iron, come back here.",
                effects: new[]
                {
                    new NodeEffectData { _EffectType = NodeEffectType.QuestStart, s_QuestGuid = questGuid }
                });

            var nodeRefused = Node(graph, conversation.s_CGuid, aldric.s_AGuid, V(x + dx * 2, y - dy * 1.2f),
                "Quest refused",
                "I understand. If you change your mind, you know where to find me.");

            // Branch B — active, no iron
            var nodeNoIron = Node(graph, conversation.s_CGuid, aldric.s_AGuid, V(x + dx, y + dy * 0.2f),
                "No iron (active)",
                "Back already? I don't see the iron. The mines aren't a Sunday excursion.",
                objReqs: new[] { (questGuid, objGetIron, false) },
                questReqs: new[] { (questGuid, QuestStatus.Active) });

            // Branch B2 — in progress, no iron
            var nodeNoIronIP = Node(graph, conversation.s_CGuid, aldric.s_AGuid, V(x + dx, y + dy * 1f),
                "No iron (in progress)",
                "I know you're working on it. But I still don't have the iron.",
                objReqs: new[] { (questGuid, objGetIron, false) },
                questReqs: new[] { (questGuid, QuestStatus.InProgress) });

            // Branch C — has iron ("Get iron" objective completed)
            // On entering this node we complete "Deliver iron" → all objectives done
            // → quest is auto-completed.
            var nodeHasIron = Node(graph, conversation.s_CGuid, aldric.s_AGuid, V(x + dx, y + dy * 2f),
                "With iron",
                "You did it! Top-quality iron. Here's your reward.",
                objReqs: new[] { (questGuid, objGetIron, true) },
                effects: new[]
                {
                    new NodeEffectData
                    {
                        _EffectType = NodeEffectType.ObjectiveComplete,
                        s_QuestGuid = questGuid,
                        s_ObjectiveGuid = objDeliverIron
                    }
                });

            // Branch D — completed
            var nodeAfterComplete = Node(graph, conversation.s_CGuid, aldric.s_AGuid, V(x + dx, y + dy * 3f),
                "Post-quest",
                "The Dawn Sword is ready. It was an honor to work with you.",
                questReqs: new[] { (questGuid, QuestStatus.Completed) });

            // Endings
            var endAccept   = End(graph, conversation.s_CGuid, aldric.s_AGuid, V(x + dx * 3, y - dy * 2.5f), "End: Accepted",  "Aldric gives you a map with the mines.");
            var endRefuse   = End(graph, conversation.s_CGuid, aldric.s_AGuid, V(x + dx * 3, y - dy * 1.2f), "End: Refused",   "Aldric sighs and goes back to work.");
            var endNoIron   = End(graph, conversation.s_CGuid, aldric.s_AGuid, V(x + dx * 2, y + dy * 0.2f), "End: No iron",   "Aldric points you to the door.");
            var endNoIronIP = End(graph, conversation.s_CGuid, aldric.s_AGuid, V(x + dx * 2, y + dy * 1f),   "End: In progress","Aldric nods and goes back to the anvil.");
            var endDone     = End(graph, conversation.s_CGuid, aldric.s_AGuid, V(x + dx * 2, y + dy * 2f),   "End: Delivered", "The hammer rings out in the forge.");
            var endThanks   = End(graph, conversation.s_CGuid, aldric.s_AGuid, V(x + dx * 2, y + dy * 3f),   "End: Post-quest","Aldric bows to you.");

            // ── Links — most restrictive first ────────────────────────────────
            L(graph, nodeIntro.s_NGuid, "", nodeAfterComplete.s_NGuid); // Completed
            L(graph, nodeIntro.s_NGuid, "", nodeHasIron.s_NGuid);       // "Get iron" objective completed
            L(graph, nodeIntro.s_NGuid, "", nodeNoIronIP.s_NGuid);      // InProgress, no iron
            L(graph, nodeIntro.s_NGuid, "", nodeNoIron.s_NGuid);        // Active, no iron
            L(graph, nodeIntro.s_NGuid, "", nodeOffer.s_NGuid);         // NotStarted

            L(graph, nodeOffer.s_NGuid, replyAccept.s_RGuid, nodeStarted.s_NGuid);
            L(graph, nodeOffer.s_NGuid, replyRefuse.s_RGuid, nodeRefused.s_NGuid);

            L(graph, nodeStarted.s_NGuid,       "", endAccept.s_NGuid);
            L(graph, nodeRefused.s_NGuid,       "", endRefuse.s_NGuid);
            L(graph, nodeNoIron.s_NGuid,        "", endNoIron.s_NGuid);
            L(graph, nodeNoIronIP.s_NGuid,      "", endNoIronIP.s_NGuid);
            L(graph, nodeHasIron.s_NGuid,       "", endDone.s_NGuid);
            L(graph, nodeAfterComplete.s_NGuid, "", endThanks.s_NGuid);

            // ── Save ──────────────────────────────────────────────────────────
            AssetDatabase.CreateAsset(graph, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = graph;
            EditorGUIUtility.PingObject(graph);

            Debug.Log($"[ExampleGenerator] Created at {path}");
            Debug.Log($"  Conversation name: {conversation.s_CName}");
            Debug.Log($"  Quest guid:        {questGuid}");
            Debug.Log($"  'Get iron' objective:    {objGetIron}");
            Debug.Log($"  'Deliver iron' objective: {objDeliverIron}");
            Debug.Log("── DialogueManager setup ────────────────────────────────");
            Debug.Log($"  Set m_ConversationName = '{conversation.s_CName}'");
            Debug.Log("── QuestManager binding ─────────────────────────────────");
            Debug.Log($"  Event Name: OnIronCollected");
            Debug.Log($"  Quest:      The Dawn Sword");
            Debug.Log($"  Objective:  Get iron");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static NodeData Node(
            GraphData g, string conversationGuid, string actorGuid, Vector2 pos,
            string title, string dialogue,
            (string guid, QuestStatus status)[] questReqs = null,
            (string questGuid, string objectiveGuid, bool mustBeCompleted)[] objReqs = null,
            NodeEffectData[] effects = null)
        {
            var node = new NodeData
            {
                s_NGuid = NewGuid(),
                s_NodeTitle = title,
                s_Dialogue = dialogue,
                s_NodePosition = pos,
                s_ActorGuid = actorGuid,
                s_ConversationGuid = conversationGuid,
                s_QuestRequirements     = new List<NodeQuestRequirement>(),
                s_ObjectiveRequirements = new List<NodeObjectiveRequirement>(),
                s_Effects               = new List<NodeEffectData>(),
                s_Replies               = new List<PlayerReplyData>()
            };

            if (questReqs != null)
                foreach (var (guid, status) in questReqs)
                    node.s_QuestRequirements.Add(new NodeQuestRequirement
                    {
                        s_QuestGuid      = guid,
                        s_RequiredStatus = status
                    });

            if (objReqs != null)
                foreach (var (q, o, must) in objReqs)
                    node.s_ObjectiveRequirements.Add(new NodeObjectiveRequirement
                    {
                        s_QuestGuid       = q,
                        s_ObjectiveGuid   = o,
                        s_MustBeCompleted = must
                    });

            if (effects != null)
                foreach (var e in effects)
                    node.s_Effects.Add(e);

            g.s_Nodes.Add(node);
            return node;
        }

        private static NodeData End(GraphData g, string conversationGuid, string actorGuid, Vector2 pos, string title, string dialogue)
            => Node(g, conversationGuid, actorGuid, pos, title, dialogue);

        private static void L(GraphData g, string from, string port, string to)
            => g.s_Links.Add(new NodeLinkData
            {
                s_OutputNodeGuid = from,
                s_OutputPortGuid = port,
                s_InputNodeGuid = to
            });

        private static Vector2 V(float x, float y) => new(x, y);
        private static string NewGuid() => System.Guid.NewGuid().ToString();
    }
}
#endif
