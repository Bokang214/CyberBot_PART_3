using System;
using System.Collections.Generic;
using System.Linq;

namespace CyberBot_PART_3
{
    public enum QuestionType { MultipleChoice, TrueFalse }

    public class QuizQuestion
    {
        public QuestionType Type { get; set; }
        public string Question { get; set; } = string.Empty;
        public string[] Options { get; set; } = Array.Empty<string>();
        public int AnswerIndex { get; set; }
        public string Explanation { get; set; } = string.Empty;
    }

    public static class QuizEngine
    {
        private static readonly List<QuizQuestion> Bank = new()
        {
            new QuizQuestion
            {
                Type = QuestionType.MultipleChoice,
                Question = "What should you do if you receive an email asking for your password?",
                Options = new[] { "A) Reply with your password", "B) Delete the email", "C) Report it as phishing", "D) Ignore it" },
                AnswerIndex = 2,
                Explanation = "Correct! Reporting phishing emails helps prevent scams."
            },
            new QuizQuestion
            {
                Type = QuestionType.MultipleChoice,
                Question = "What does HTTPS in a website URL mean?",
                Options = new[] { "A) The site is fast", "B) The site is encrypted and secure", "C) The site is government-owned", "D) The site has no ads" },
                AnswerIndex = 1,
                Explanation = "HTTPS means the connection is encrypted using TLS."
            },
            new QuizQuestion
            {
                Type = QuestionType.MultipleChoice,
                Question = "Which of these is the strongest password?",
                Options = new[] { "A) password123", "B) John1990", "C) P@ssw0rd!", "D) Tr0ub4dor&3Horses!" },
                AnswerIndex = 3,
                Explanation = "Long passphrases with mixed characters are the hardest to crack."
            },
            new QuizQuestion
            {
                Type = QuestionType.MultipleChoice,
                Question = "What is two-factor authentication (2FA)?",
                Options = new[] { "A) Using two different passwords", "B) A second verification step beyond your password", "C) Logging in from two devices", "D) A type of encryption" },
                AnswerIndex = 1,
                Explanation = "2FA adds a second layer of security even if your password is stolen."
            },
            new QuizQuestion
            {
                Type = QuestionType.MultipleChoice,
                Question = "What is phishing?",
                Options = new[] { "A) A type of malware that encrypts files", "B) A trick to get you to reveal personal info via fake messages", "C) A method of securing your network", "D) A tool for scanning viruses" },
                AnswerIndex = 1,
                Explanation = "Phishing uses deceptive emails or SMS to steal credentials."
            },
            new QuizQuestion
            {
                Type = QuestionType.TrueFalse,
                Question = "True or False: Using the same password on multiple sites is safe as long as it is strong.",
                Options = new[] { "A) True", "B) False" },
                AnswerIndex = 1,
                Explanation = "FALSE — if one site is breached, attackers try the same password everywhere."
            },
            new QuizQuestion
            {
                Type = QuestionType.TrueFalse,
                Question = "True or False: Public Wi-Fi is always safe to use for online banking.",
                Options = new[] { "A) True", "B) False" },
                AnswerIndex = 1,
                Explanation = "FALSE — public Wi-Fi can be monitored. Use a VPN instead."
            },
            new QuizQuestion
            {
                Type = QuestionType.TrueFalse,
                Question = "True or False: Software updates should be installed as soon as they are available.",
                Options = new[] { "A) True", "B) False" },
                AnswerIndex = 0,
                Explanation = "TRUE — updates patch security vulnerabilities."
            },
        };

        public static bool IsActive { get; private set; }
        public static int Score { get; private set; }
        public static int TotalScore { get; private set; }
        public static int TotalPlayed { get; private set; }

        private static List<QuizQuestion> _pool = new();
        private static int _index = 0;

        public static string StartQuiz()
        {
            _pool = Bank.OrderBy(_ => Guid.NewGuid()).Take(5).ToList();
            _index = 0;
            Score = 0;
            IsActive = true;
            return BuildQuestion();
        }

        public static string SubmitAnswer(string raw)
        {
            if (!IsActive || _index >= _pool.Count)
                return "No quiz active. Type 'quiz' to start!";

            QuizQuestion q = _pool[_index];
            int chosen = ParseAnswer(raw.Trim().ToUpperInvariant(), q.Options.Length);

            if (chosen < 0)
                return $"⚠️  Please answer with A, B, C, or D (or 1–{q.Options.Length}).";

            bool correct = chosen == q.AnswerIndex;
            if (correct) Score++;
            _index++;

            string verdict = correct
                ? $"✅  Correct! {q.Explanation}"
                : $"❌  Not quite. The correct answer was: {q.Options[q.AnswerIndex]}\n💡  {q.Explanation}";

            if (_index >= _pool.Count)
            {
                IsActive = false;
                TotalScore += Score;
                TotalPlayed += _pool.Count;
                double pct = (double)Score / _pool.Count * 100;
                return verdict + $"\n\n══ QUIZ COMPLETE ══\nYour score: {Score} / {_pool.Count}  ({pct:F0}%)\n\nType 'quiz' to play again!";
            }

            return verdict + "\n\n" + BuildQuestion();
        }

        private static string BuildQuestion()
        {
            QuizQuestion q = _pool[_index];
            string typeLabel = q.Type == QuestionType.TrueFalse ? "  [True / False]" : "  [Multiple Choice]";
            return $"🎮  Question {_index + 1} of {_pool.Count}{typeLabel}\n────────────────────────────────────────────────\n{q.Question}\n\n{string.Join("\n", q.Options.Select(o => $"  {o}"))}\n\nType A, B, C, or D to answer.";
        }

        private static int ParseAnswer(string input, int max)
        {
            if (input == "A") return 0;
            if (input == "B") return 1;
            if (input == "C") return 2;
            if (input == "D") return 3;
            if (int.TryParse(input, out int n) && n >= 1 && n <= max) return n - 1;
            return -1;
        }
    }
}