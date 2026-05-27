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
                "Guardar Example Graph", "ExampleDialogue", "asset",
                "Elige dónde guardar el GraphData de ejemplo");
            if (string.IsNullOrEmpty(path)) return;

            var graph = ScriptableObject.CreateInstance<GraphData>();

            // ── Actores ───────────────────────────────────────────────────────
            var aldric = new ActorData { guid = NewGuid(), name = "Aldric el Herrero" };
            graph.actors.Add(aldric);

            // ── Quest ─────────────────────────────────────────────────────────
            string questGuid = NewGuid();
            var questSword = new QuestData
            {
                guid = questGuid,
                title = "La Espada del Alba",
                description = "Consigue hierro de las minas del norte y entrégaselo a Aldric.",
                objectives = new List<QuestObjectiveData>
                {
                    new() { guid = NewGuid(), description = "Conseguir hierro", requiredCompletedState = true },
                    new() { guid = NewGuid(), description = "Entregar el hierro", requiredCompletedState = true }
                }
            };
            graph.quests.Add(questSword);

            // ── Condición de jugador (solo para el hierro) ────────────────────
            var condHasIron = new ConditionData { guid = NewGuid(), name = "TieneHierro", value = false };
            graph.conditions.Add(condHasIron);

            // ── Nodos ─────────────────────────────────────────────────────────
            //
            // FLUJO:
            //  [intro]
            //    ├─(Completed)           → [afterComplete]   → fin
            //    ├─(Active + TieneHierro)→ [hasIron*COMPLETE*] → fin
            //    ├─(Active + !Hierro)    → [noIron]          → fin
            //    ├─(InProgress + !Hierro)→ [noIronInProgress]→ fin
            //    └─(NotStarted)          → [offerQuest]
            //          ├─[Acepto]        → [questStarted*START*] → fin
            //          └─[Rechazo]       → [questRefused]    → fin

            float x = 100, y = 300, dx = 280, dy = 180;

            var nodeIntro = Node(graph, aldric.guid, V(x, y),
                "Intro",
                "¡Bienvenido a mi forja, viajero! ¿Qué puedo hacer por ti?");

            // Rama A — quest no iniciada
            var nodeOffer = Node(graph, aldric.guid, V(x + dx, y - dy * 2f),
                "Oferta de quest",
                "Llevo semanas buscando a alguien con agallas. Necesito hierro de las Minas del Norte. ¿Te interesa?",
                questReqs: new[] { (questGuid, QuestStatus.NotStarted) });

            var replyAccept = new PlayerReplyData { guid = NewGuid(), text = "Acepto. ¿Qué tengo que hacer?" };
            var replyRefuse = new PlayerReplyData { guid = NewGuid(), text = "No es un buen momento." };
            nodeOffer.replies = new List<PlayerReplyData> { replyAccept, replyRefuse };

            var nodeStarted = Node(graph, aldric.guid, V(x + dx * 2, y - dy * 2.5f),
                "Quest aceptada",
                "Excelente. Las minas están al norte. Cuando tengas el hierro, vuelve aquí.",
                questGuid: questGuid, questAction: QuestActionType.Start);

            var nodeRefused = Node(graph, aldric.guid, V(x + dx * 2, y - dy * 1.2f),
                "Quest rechazada",
                "Lo entiendo. Si cambias de opinión, ya sabes dónde encontrarme.");

            // Rama B — activa, sin hierro
            var nodeNoIron = Node(graph, aldric.guid, V(x + dx, y + dy * 0.2f),
                "Sin hierro (activa)",
                "¿Ya de vuelta? No veo el hierro. Las minas no son una excursión de domingo.",
                conditions: new[] { (condHasIron.guid, false) },
                questReqs: new[] { (questGuid, QuestStatus.Active) });

            // Rama B2 — en progreso, sin hierro
            var nodeNoIronIP = Node(graph, aldric.guid, V(x + dx, y + dy * 1f),
                "Sin hierro (en progreso)",
                "Sé que estás trabajando en ello. Pero aún no tengo el hierro.",
                conditions: new[] { (condHasIron.guid, false) },
                questReqs: new[] { (questGuid, QuestStatus.InProgress) });

            // Rama C — tiene el hierro (activa o en progreso)
            var nodeHasIron = Node(graph, aldric.guid, V(x + dx, y + dy * 2f),
                "Con hierro",
                "¡Lo conseguiste! Hierro de primera calidad. Aquí tienes tu recompensa.",
                conditions: new[] { (condHasIron.guid, true) },
                questGuid: questGuid, questAction: QuestActionType.Complete);

            // Rama D — completada
            var nodeAfterComplete = Node(graph, aldric.guid, V(x + dx, y + dy * 3f),
                "Post-quest",
                "La Espada del Alba ya está lista. Fue un honor trabajar contigo.",
                questReqs: new[] { (questGuid, QuestStatus.Completed) });

            // Finales
            var endAccept = End(graph, aldric.guid, V(x + dx * 3, y - dy * 2.5f), "Fin: Aceptó", "Aldric te entrega un mapa con las minas.");
            var endRefuse = End(graph, aldric.guid, V(x + dx * 3, y - dy * 1.2f), "Fin: Rechazó", "Aldric suspira y vuelve al trabajo.");
            var endNoIron = End(graph, aldric.guid, V(x + dx * 2, y + dy * 0.2f), "Fin: Sin hierro", "Aldric te señala la puerta.");
            var endNoIronIP = End(graph, aldric.guid, V(x + dx * 2, y + dy * 1f), "Fin: En progreso", "Aldric asiente y vuelve al yunque.");
            var endDone = End(graph, aldric.guid, V(x + dx * 2, y + dy * 2f), "Fin: Entregó", "El martillo retumba en la forja.");
            var endThanks = End(graph, aldric.guid, V(x + dx * 2, y + dy * 3f), "Fin: Post-quest", "Aldric te hace una reverencia.");

            // ── Links — orden: más restrictivo primero ────────────────────────
            L(graph, nodeIntro.guid, "", nodeAfterComplete.guid); // Completed
            L(graph, nodeIntro.guid, "", nodeHasIron.guid);       // TieneHierro==true
            L(graph, nodeIntro.guid, "", nodeNoIronIP.guid);      // InProgress, sin hierro
            L(graph, nodeIntro.guid, "", nodeNoIron.guid);        // Active, sin hierro
            L(graph, nodeIntro.guid, "", nodeOffer.guid);         // NotStarted

            L(graph, nodeOffer.guid, replyAccept.guid, nodeStarted.guid);
            L(graph, nodeOffer.guid, replyRefuse.guid, nodeRefused.guid);

            L(graph, nodeStarted.guid, "", endAccept.guid);
            L(graph, nodeRefused.guid, "", endRefuse.guid);
            L(graph, nodeNoIron.guid, "", endNoIron.guid);
            L(graph, nodeNoIronIP.guid, "", endNoIronIP.guid);
            L(graph, nodeHasIron.guid, "", endDone.guid);
            L(graph, nodeAfterComplete.guid, "", endThanks.guid);

            // ── Guardar ───────────────────────────────────────────────────────
            AssetDatabase.CreateAsset(graph, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = graph;
            EditorGUIUtility.PingObject(graph);

            Debug.Log($"[ExampleGenerator] Creado en {path}");
            Debug.Log($"  TieneHierro guid: {condHasIron.guid}");
            Debug.Log($"  Quest guid:       {questGuid}");
            Debug.Log("── BINDINGS en ConditionManager ─────────────────────────");
            Debug.Log($"  Event Name:     OnHierroRecogido");
            Debug.Log($"  Condition Guid: {condHasIron.guid}");
            Debug.Log($"  Value To Set:   true");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static NodeData Node(
            GraphData g, string actorGuid, Vector2 pos,
            string title, string dialogue,
            (string guid, bool required)[] conditions = null,
            (string guid, QuestStatus status)[] questReqs = null,
            string questGuid = "", QuestActionType? questAction = null)
        {
            var node = new NodeData
            {
                guid = NewGuid(),
                title = title,
                dialogue = dialogue,
                position = pos,
                actorGuid = actorGuid,
                questGuid = questGuid,
                conditions = new List<NodeConditionData>(),
                questActions = new List<NodeQuestActionData>(),
                questRequirements = new List<NodeQuestRequirement>(),
                replies = new List<PlayerReplyData>()
            };

            if (conditions != null)
                foreach (var (guid, req) in conditions)
                    node.conditions.Add(new NodeConditionData { conditionGuid = guid, requiredValue = req });

            if (questReqs != null)
                foreach (var (guid, status) in questReqs)
                    node.questRequirements.Add(new NodeQuestRequirement { questGuid = guid, requiredStatus = status });

            if (questAction.HasValue && !string.IsNullOrEmpty(questGuid))
                node.questActions.Add(new NodeQuestActionData { questGuid = questGuid, action = questAction.Value });

            g.nodes.Add(node);
            return node;
        }

        private static NodeData End(GraphData g, string actorGuid, Vector2 pos, string title, string dialogue)
            => Node(g, actorGuid, pos, title, dialogue);

        private static void L(GraphData g, string from, string port, string to)
            => g.links.Add(new NodeLinkData { outputNodeGuid = from, outputPortGuid = port, inputNodeGuid = to });

        private static Vector2 V(float x, float y) => new(x, y);
        private static string NewGuid() => System.Guid.NewGuid().ToString();
    }
}
#endif