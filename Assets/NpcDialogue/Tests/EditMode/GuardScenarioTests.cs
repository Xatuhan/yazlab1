using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace YazLab.NpcDialogue.Tests
{
    // PDF 4.4: en az 5 diyalog senaryosu gerçek yerel modelle (Ollama açık olmalı) ve oyunun gerçek koduyla denenir.
    // Testin geçme koşulu: her turda oyun hatasız cevap alır (geçerli karar + boş olmayan söz).
    // Beklenen kararın kaç kez geldiği ayrıca ölçülür ve Logs/npc_scenarios.md'ye rapor tablosu olarak yazılır
    // (model her seferinde farklı cümle kurduğu için bu bir orandır, geçme koşulu değildir).
    public class GuardScenarioTests
    {
        private const int Runs = 3;
        private static readonly string TestDir = Path.Combine(Application.dataPath, "NpcDialogue", "Tests", "EditMode");
        private static readonly string DataDir = Path.Combine(Application.streamingAssetsPath, "NpcDialogue");

        private class Step
        {
            public char Kind;          // '>' oyuncu, '!' eşya, '@' saldırı dosyasından
            public string[] Options;
        }

        private class Scenario
        {
            public string Name;
            public string Expected;
            public readonly List<Step> Steps = new List<Step>();
        }

        private static List<Scenario> LoadScenarios()
        {
            var list = new List<Scenario>();
            Scenario current = null;
            foreach (string raw in File.ReadAllLines(Path.Combine(TestDir, "dialogue_scenarios.txt")))
            {
                string line = raw.Trim();
                if (line.StartsWith("## "))
                {
                    string[] parts = line.Substring(3).Split('|');
                    current = new Scenario { Name = parts[0].Trim(), Expected = parts[1].Trim() };
                    list.Add(current);
                }
                else if (current != null && line.Length > 2 && (line[0] == '>' || line[0] == '!' || line[0] == '@'))
                {
                    current.Steps.Add(new Step
                    {
                        Kind = line[0],
                        Options = line.Substring(1).Trim().Split(new[] { " || " }, System.StringSplitOptions.None)
                    });
                }
            }
            return list;
        }

        // "@ talimat-ezme 1": B'nin saldırı dosyasındaki kategorinin 1. cümlesi (cümle bu dosyada yazılmaz).
        private static string AttackLine(string spec)
        {
            string[] parts = spec.Split(' ');
            int index = int.Parse(parts[1]);
            string category = null;
            int seen = 0;
            foreach (string raw in File.ReadAllLines(Path.Combine(TestDir, "injection_attacks.txt")))
            {
                string line = raw.Trim();
                if (line.StartsWith("#")) { category = line.Substring(1).Trim(); continue; }
                if (line.Length > 0 && category == parts[0] && ++seen == index)
                    return line;
            }
            Assert.Fail("saldırı cümlesi bulunamadı: " + spec);
            return null;
        }

        [UnityTest, Explicit, Category("Ollama")]
        public IEnumerator Scenarios_RealModel()
        {
            Assert.IsTrue(OllamaSettings.TryParse(File.ReadAllText(Path.Combine(DataDir, "ollama_settings.json")),
                out OllamaSettings settings, out string error), error);
            Assert.IsTrue(ItemCatalog.TryParse(File.ReadAllText(Path.Combine(DataDir, "items.json")),
                out ItemCatalog items, out error), error);
            string prompt = File.ReadAllText(Path.Combine(DataDir, "guard_prompt.txt"));
            string reminder = File.ReadAllText(Path.Combine(DataDir, "guard_reminder.txt"));
            string patience = File.ReadAllText(Path.Combine(DataDir, "guard_patience.txt"));
            string[] doubt = System.Array.FindAll(File.ReadAllLines(Path.Combine(DataDir, "guard_doubt.txt")), l => l.Trim().Length > 0);
            string proofs = File.ReadAllText(Path.Combine(DataDir, "guard_proofs.txt")).Trim();

            var report = new StringBuilder();
            report.AppendLine("| Senaryo | Beklenen | Tekrar | Oyuncu (son) | Karar | Tur tur karar/kanıt | Söz |");
            report.AppendLine("|---|---|---|---|---|---|---|");
            var summary = new StringBuilder();
            var mismatches = new StringBuilder();
            int failures = 0;

            using (var client = new OllamaClient(settings))
            {
                foreach (Scenario scenario in LoadScenarios())
                {
                    int hits = 0;
                    for (int run = 0; run < Runs; run++)
                    {
                        var conversation = new GuardConversation(client, new NpcResponseParser(settings.maxReplyChars),
                            new ConversationHistory(prompt, reminder, settings.historyTurns), settings.maxInputChars,
                            settings.patienceTurns, patience, doubt, proofs);
                        string action = "-", reply = "-", said = "-";
                        var trace = new StringBuilder(); // tur tur karar/kanıt sayısı

                        foreach (Step step in scenario.Steps)
                        {
                            string text = step.Kind == '@' ? AttackLine(step.Options[0]) : step.Options[run % step.Options.Length];
                            Task<TurnOutcome> task;
                            if (step.Kind == '!')
                            {
                                Assert.IsTrue(items.TryGet(text, out ItemData item), "eşya yok: " + text);
                                task = conversation.ShowItemAsync(item, CancellationToken.None);
                                said = "(Gösterdi: " + item.label + ")";
                            }
                            else
                            {
                                task = conversation.SendAsync(text, CancellationToken.None);
                                said = step.Kind == '@' ? "(saldırı dosyasından: " + step.Options[0] + ")" : text;
                            }

                            var watch = Stopwatch.StartNew();
                            while (!task.IsCompleted && watch.Elapsed.TotalSeconds < 120)
                                yield return null;
                            Assert.IsTrue(task.IsCompleted, scenario.Name + ": cevap gelmedi");

                            TurnOutcome outcome = task.Result;
                            if (outcome.Status == TurnStatus.Rejected)
                            {
                                action = "filtre"; reply = "(modele gitmedi)";
                                break;
                            }
                            if (outcome.Status != TurnStatus.Answered || string.IsNullOrWhiteSpace(outcome.Decision.Reply))
                            {
                                failures++;
                                action = "HATA:" + outcome.Error; reply = "-";
                                break;
                            }
                            action = outcome.Decision.Action.ToString().ToLowerInvariant();
                            reply = outcome.Decision.Reply;
                            trace.Append($"{action}/{conversation.Proofs} ");
                            // kapı kapalıyken "kapıyı açıyorum" demesi oyuncuyu yanıltır
                            if (outcome.Decision.Action == NpcAction.Deny && (reply.Contains("açıyorum") || reply.Contains("açacağım")))
                                mismatches.AppendLine($"- {scenario.Name} #{run + 1}: \"{Escape(text)}\" → {Escape(reply)}");
                            if (outcome.Decision.Action != NpcAction.Deny)
                                break; // oyun bitti
                        }

                        bool hit = action == scenario.Expected || (scenario.Expected == "deny" && action == "filtre");
                        if (hit) hits++;
                        report.AppendLine($"| {scenario.Name} | {scenario.Expected} | {run + 1} | {Escape(said)} | {action}{(hit ? "" : " ✗")} | {trace.ToString().Trim()} | {Escape(reply)} |");
                    }
                    summary.AppendLine($"{scenario.Name}: {hits}/{Runs} beklenen ({scenario.Expected})");
                }
            }

            string logDir = Path.Combine(Application.dataPath, "..", "Logs");
            Directory.CreateDirectory(logDir);
            File.WriteAllText(Path.Combine(logDir, "npc_scenarios.md"),
                "# NPC senaryo testi\n\n" + summary.ToString().Replace("\n", "  \n") + "\n" + report
                + "\n## Kapı kapalıyken \"kapıyı açıyorum\"\n\n" + (mismatches.Length == 0 ? "yok\n" : mismatches.ToString()));
            UnityEngine.Debug.Log("NPC senaryo özeti:\n" + summary);

            Assert.AreEqual(0, failures, "model cevabı alınamayan tur var (Logs/npc_scenarios.md)");
        }

        private static string Escape(string s) => s.Replace("|", "/").Replace("\n", " ");
    }
}
