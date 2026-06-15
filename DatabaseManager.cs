using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CyberBot_PART_3
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsComplete { get; set; }
        public DateTime? ReminderDate { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string ReminderDisplay => ReminderDate.HasValue ? $"Remind: {ReminderDate.Value:dd MMM yyyy}" : "No reminder";
        public string StatusDisplay => IsComplete ? "✅ Done" : "⏳ Pending";
    }

    public static class DatabaseManager
    {
        private static List<TaskItem> _tasks = new();
        private static int _nextId = 1;
        private static readonly string DataFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tasks.json");

        public static bool Initialise(out string error)
        {
            error = string.Empty;
            try
            {
                if (File.Exists(DataFile))
                {
                    string json = File.ReadAllText(DataFile);
                    var data = JsonSerializer.Deserialize<TaskData>(json);
                    if (data != null)
                    {
                        _tasks = data.Tasks ?? new List<TaskItem>();
                        _nextId = data.NextId;
                        return true;
                    }
                }
            }
            catch (Exception ex) { error = ex.Message; }

            _tasks = new List<TaskItem>();
            _nextId = 1;
            SaveToFile();
            return true;
        }

        private static void SaveToFile()
        {
            try
            {
                var data = new TaskData { Tasks = _tasks, NextId = _nextId };
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(DataFile, json);
            }
            catch { }
        }

        public static bool AddTask(TaskItem task, out string error)
        {
            error = string.Empty;
            task.Id = _nextId++;
            _tasks.Add(task);
            SaveToFile();
            return true;
        }

        public static List<TaskItem> GetAllTasks(out string error)
        {
            error = string.Empty;
            return _tasks;
        }

        public static bool MarkComplete(int id, out string error)
        {
            error = string.Empty;
            var task = _tasks.Find(t => t.Id == id);
            if (task != null)
            {
                task.IsComplete = true;
                SaveToFile();
            }
            return true;
        }

        public static bool DeleteTask(int id, out string error)
        {
            error = string.Empty;
            var task = _tasks.Find(t => t.Id == id);
            if (task != null)
            {
                _tasks.Remove(task);
                SaveToFile();
            }
            return true;
        }

        private class TaskData
        {
            public List<TaskItem> Tasks { get; set; } = new();
            public int NextId { get; set; } = 1;
        }
    }
}