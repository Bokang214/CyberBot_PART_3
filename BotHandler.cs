using System;
using System.Collections.Generic;
using System.Linq;

namespace CyberBot_PART_3
{
    public enum Sentiment { Neutral, Worried, Frustrated, Curious }

    public class ConversationEntry
    {
        public string Role { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Time { get; set; }
    }

    public static class BotHandler
    {
        private static string userName = string.Empty;
        private static string lastTopic = string.Empty;
        private static Random rng = new Random();

        public static List<ConversationEntry> History { get; } = new();

        private static HashSet<string> topicsVisited = new(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, int> topicAskCount = new(StringComparer.OrdinalIgnoreCase);

        // Response pools
        private static readonly List<string> PhishingResponses = new()
        {
            "🎣  PHISHING\n────────────────────────────────────────────────\n" +
            "Phishing tricks you into revealing credentials via fake messages.\n\n" +
            "Red flags:\n  ✗  Urgency — \"Act NOW or your account closes!\"\n" +
            "  ✗  Mismatched sender (support@paypa1.com)\n" +
            "  ✗  Generic greeting — \"Dear Customer\"\n\n" +
            "What to do: Don't click. Report it. Delete it.",
        };

        private static readonly List<string> PasswordResponses = new()
        {
            "🔑  PASSWORDS\n────────────────────────────────────────────────\n" +
            "Creating strong passwords:\n" +
            "  • Minimum 12 characters — longer is always better.\n" +
            "  • Mix uppercase, lowercase, numbers, and symbols.\n" +
            "  • Use passphrases: \"CorrectHorseBatteryStaple!\"\n\n" +
            "Critical rules:\n  ✗  NEVER reuse passwords across sites.\n" +
            "  ✓  Use a password manager (Bitwarden, 1Password).",
        };

        private static readonly List<string> BrowsingResponses = new()
        {
            "🌐  SAFE BROWSING\n────────────────────────────────────────────────\n" +
            "  ✓  Check for HTTPS — HTTP exposes your data.\n" +
            "  ✓  Hover over links to see the real URL.\n" +
            "  ✓  Spot typos in domains (g00gle.com).\n" +
            "  ✓  Keep your browser and extensions updated.",
        };

        private static readonly List<string> PrivacyResponses = new()
        {
            "🔏  DATA PRIVACY\n────────────────────────────────────────────────\n" +
            "  ✓  Share only what is strictly necessary.\n" +
            "  ✓  Use a separate email for newsletters.\n" +
            "  ✓  Audit app permissions (camera, mic, location).\n\n" +
            "POPIA (SA) gives you the right to access and delete your data.",
        };

        public static Sentiment DetectSentiment(string input)
        {
            string l = input.ToLowerInvariant();
            string[] worried = { "scared", "afraid", "worried", "hacked", "breached", "stolen", "virus" };
            string[] frustrated = { "annoying", "useless", "stupid", "confused", "confusing", "don't understand", "hate" };
            string[] curious = { "curious", "interesting", "explain", "how does", "what is", "tell me" };

            if (worried.Any(k => l.Contains(k))) return Sentiment.Worried;
            if (frustrated.Any(k => l.Contains(k))) return Sentiment.Frustrated;
            if (curious.Any(k => l.Contains(k))) return Sentiment.Curious;
            return Sentiment.Neutral;
        }

        private static void Record(string role, string message)
        {
            History.Add(new ConversationEntry { Role = role, Message = message, Time = DateTime.Now });
            if (History.Count > 200) History.RemoveAt(0);
            ActivityLogger.Log($"{role}: {message.Replace("\n", " ").Substring(0, Math.Min(message.Length, 80))}");
        }

        private static string Return(string response)
        {
            Record("Bot", response);
            return response;
        }

        private static void TrackTopic(string topic)
        {
            topicsVisited.Add(topic);
            topicAskCount[topic] = topicAskCount.TryGetValue(topic, out int c) ? c + 1 : 1;
            lastTopic = topic;
        }

        public static string ProcessInput(string input, CyberBot bot)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            string lower = input.ToLowerInvariant().Trim();
            Record("User", input);

            // Quiz mode
            if (QuizEngine.IsActive)
            {
                return Return(QuizEngine.SubmitAnswer(input));
            }

            // Name collection
            if (string.IsNullOrEmpty(userName))
            {
                userName = input.Trim();
                bot.UserName = userName;
                ActivityLogger.Log($"User identified: {userName}");
                return Return(
                    $"Nice to meet you, {userName}! 😊\n\n" +
                    "I can help you with:\n" +
                    "  🎣  Phishing          —  type 'phishing'\n" +
                    "  🔑  Passwords & 2FA   —  type 'password'\n" +
                    "  🌐  Safe Browsing     —  type 'browsing'\n" +
                    "  🔏  Privacy           —  type 'privacy'\n" +
                    "  🎮  Quiz              —  type 'quiz'\n" +
                    "  📜  Activity log      —  type 'log'\n" +
                    "  ❓  Help              —  type 'help'");
            }

            // Special commands
            if (lower is "quiz" or "start quiz" or "play quiz")
            {
                ActivityLogger.Log("Quiz started");
                return Return(QuizEngine.StartQuiz());
            }

            if (lower is "log" or "activity log" or "history")
                return Return(ActivityLogger.ViewLog());

            if (lower is "show more" or "more log")
                return Return(ActivityLogger.ShowMore());

            if (lower is "chat history" or "conversation")
                return Return(GetChatHistory());

            if (lower is "interests" or "stats" or "my stats")
                return Return(GetInterestsSummary());

            if (lower is "help" or "menu" or "?" or "options")
                return Return(GetHelpMenu());

            // Sentiment
            Sentiment sentiment = DetectSentiment(lower);
            string prefix = sentiment switch
            {
                Sentiment.Worried => $"😟  I hear your concern, {userName}. Let me help.\n\n",
                Sentiment.Frustrated => $"I hear you, {userName} — let me simplify.\n\n",
                Sentiment.Curious => $"Great question, {userName}!\n\n",
                _ => string.Empty,
            };

            string repeatHint = string.Empty;
            if (!string.IsNullOrEmpty(lastTopic) && topicAskCount.TryGetValue(lastTopic, out int asked) && asked >= 2)
                repeatHint = $"\n\n💡  You've asked about {lastTopic} {asked} times — try the quiz! (type 'quiz')";

            if (lower.Contains("more") || lower.Contains("tell me more"))
            {
                if (!string.IsNullOrEmpty(lastTopic))
                    return Return(prefix + GetMoreOnTopic(lastTopic) + repeatHint);
            }

            // General questions
            if (lower.Contains("how are you"))
                return Return($"🤖  I'm doing great, {userName}! How can I help you stay safe online?");

            if (lower.Contains("what can i ask"))
                return Return(GetHelpMenu());

            if (lower.Contains("purpose") || lower.Contains("what are you"))
                return Return($"🛡️  My purpose is to educate you about cybersecurity and help you stay safe online.");

            // Topic detection
            if (lower.Contains("phish") || lower.Contains("scam"))
            {
                TrackTopic("phishing");
                return Return(prefix + PhishingResponses[rng.Next(PhishingResponses.Count)] + repeatHint);
            }

            if (lower.Contains("password") || lower.Contains("2fa"))
            {
                TrackTopic("passwords");
                return Return(prefix + PasswordResponses[rng.Next(PasswordResponses.Count)] + repeatHint);
            }

            if (lower.Contains("brows") || lower.Contains("https") || lower.Contains("vpn"))
            {
                TrackTopic("safe browsing");
                return Return(prefix + BrowsingResponses[rng.Next(BrowsingResponses.Count)] + repeatHint);
            }

            if (lower.Contains("privacy") || lower.Contains("data"))
            {
                TrackTopic("privacy");
                return Return(prefix + PrivacyResponses[rng.Next(PrivacyResponses.Count)] + repeatHint);
            }

            // Fallback
            string[] fallbacks = {
                $"🤔  I didn't quite catch that, {userName}. Try 'phishing', 'quiz', or 'help'.",
                $"❓  Not sure what you mean. Type 'help' to see what I can do.",
            };
            return Return(fallbacks[rng.Next(fallbacks.Length)]);
        }

        private static string GetMoreOnTopic(string topic) => topic switch
        {
            "phishing" => PhishingResponses[rng.Next(PhishingResponses.Count)],
            "passwords" => PasswordResponses[rng.Next(PasswordResponses.Count)],
            "safe browsing" => BrowsingResponses[rng.Next(BrowsingResponses.Count)],
            "privacy" => PrivacyResponses[rng.Next(PrivacyResponses.Count)],
            _ => "What specific topic would you like me to expand on?"
        };

        private static string GetChatHistory()
        {
            if (History.Count == 0) return "📜  No history yet — start chatting first!";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("📜  CHAT HISTORY (last 15 messages)");
            sb.AppendLine("────────────────────────────────────────────────");
            foreach (var e in History.TakeLast(15))
            {
                string preview = e.Message.Replace("\n", " ");
                if (preview.Length > 80) preview = preview.Substring(0, 80) + "…";
                sb.AppendLine($"  [{e.Time:HH:mm}]  {e.Role}: {preview}");
            }
            return sb.ToString();
        }

        private static string GetInterestsSummary()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"⭐  {userName.ToUpperInvariant()}'S INTERESTS");
            sb.AppendLine("────────────────────────────────────────────────");
            if (topicsVisited.Count == 0)
                sb.AppendLine("  No topics explored yet.");
            else
            {
                foreach (string t in topicsVisited)
                {
                    int c = topicAskCount.TryGetValue(t, out int n) ? n : 0;
                    sb.AppendLine($"    ★  {t}  ({c}× asked)");
                }
            }
            return sb.ToString();
        }

        private static string GetHelpMenu() =>
            $"📋  WHAT I CAN DO, {userName.ToUpperInvariant()}\n\n" +
            "  🎣  Phishing          —  type 'phishing'\n" +
            "  🔑  Passwords & 2FA   —  type 'password'\n" +
            "  🌐  Safe Browsing     —  type 'browsing'\n" +
            "  🔏  Privacy           —  type 'privacy'\n" +
            "  🎮  Quiz              —  type 'quiz'\n" +
            "  📜  Activity log      —  type 'log'\n" +
            "  📊  My interests      —  type 'interests'\n" +
            "  📜  Chat history      —  type 'chat history'\n\n" +
            "Or just ask me anything naturally!";
    }
}