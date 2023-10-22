using Newtonsoft.Json;
using SimpleFFmpegGUI.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleFFmpegGUI.Manager
{
    public static class PresetManager
    {
        public static int AddOrUpdatePreset(string name, TaskType type, OutputArguments arguments)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Tên trống");
            }
            CodePreset preset = new CodePreset()
            {
                Name = name,
                Type = type,
                Arguments = arguments
            };
            using var db = FFmpegDbContext.GetNew();
            if (db.Presets.Where(p => p.IsDeleted == false).Any(p => p.Name == name && p.Type == type))
            {
                db.Presets.RemoveRange(db.Presets.Where(p => p.IsDeleted == false).Where(p => p.Name == name && p.Type == type).ToArray());
            }
            db.Presets.Add(preset);
            db.SaveChanges();
            return preset.Id;
        }

        public static void UpdatePreset(CodePreset preset)
        {
            using var db = FFmpegDbContext.GetNew();
            db.Presets.Update(preset);
            db.SaveChanges();
        }

        public static CodePreset AddPreset(string name, TaskType type, OutputArguments arguments)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Tên trống");
            }
            if (ContainsPreset(name, type))
            {
                throw new Exception($"Một giá trị đặt trước có tên {name} đã tồn tại");
            }
            CodePreset preset = new CodePreset()
            {
                Name = name,
                Type = type,
                Arguments = arguments
            };
            using var db = FFmpegDbContext.GetNew();

            db.Presets.Add(preset);
            db.SaveChanges();
            return preset;
        }

        public static bool ContainsPreset(string name, TaskType type)
        {
            using var db = FFmpegDbContext.GetNew();
            if (db.Presets.Where(p => p.IsDeleted == false).Any(p => p.Name == name && p.Type == type))
            {
                return true;
            }
            return false;
        }

        public static void DeletePresets()
        {
            using var db = FFmpegDbContext.GetNew();
            foreach (var preset in db.Presets)
            {
                preset.IsDeleted = true;
                db.Entry(preset).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            }
            db.SaveChanges();
        }

        public static void DeletePreset(int id)
        {
            using var db = FFmpegDbContext.GetNew();
            CodePreset preset = db.Presets.Find(id);
            if (preset == null)
            {
                throw new ArgumentException($"Không thể tìm thấy giá trị đặt trước với ID {id}");
            }
            db.Presets.Remove(preset);
            db.SaveChanges();
        }

        public static List<CodePreset> GetPresets()
        {
            using var db = FFmpegDbContext.GetNew();
            return db.Presets.Where(p => !p.IsDeleted)
                             .OrderBy(p => p.Type)
                             .ThenBy(p => p.Name)
                             .ToList();
        }

        public static List<CodePreset> GetPresets(TaskType type)
        {
            using var db = FFmpegDbContext.GetNew();
            return db.Presets.Where(p => !p.IsDeleted)
                             .Where(p => p.Type == type)
                             .OrderBy(p => p.Name)
                             .ToList();
        }

        public static void SetDefaultPreset(int id)
        {
            using var db = FFmpegDbContext.GetNew();
            CodePreset preset = db.Presets.Find(id);
            if (preset == null)
            {
                throw new ArgumentException($"Không thể tìm thấy giá trị đặt trước với ID {id}");
            }
            var type = preset.Type;
            if (db.Presets.Any(p => p.Type == type && p.Default && !p.IsDeleted))
            {
                foreach (var p in db.Presets.Where(p => p.Type == type && p.Default).ToList())
                {
                    p.Default = false;
                    db.Entry(p).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                }
            }
            preset.Default = true;
            db.Entry(preset).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            db.SaveChanges();
        }

        public static CodePreset GetDefaultPreset(TaskType type)
        {
            using var db = FFmpegDbContext.GetNew();
            return db.Presets.FirstOrDefault(p => p.Type == type && p.Default && !p.IsDeleted);
        }

        public static string Export()
        {
            var presets = GetPresets();
            return JsonConvert.SerializeObject(presets, Formatting.Indented);
        }

        public static void Import(string json)
        {
            var presets = JsonConvert.DeserializeObject<List<CodePreset>>(json);

            using var db = FFmpegDbContext.GetNew();
            foreach (var preset in presets)
            {
                string name = preset.Name;
                var type = preset.Type;
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new ArgumentException("Tên trống");
                }
                preset.Id = 0;
                preset.Id = 0;
                if (db.Presets.Where(p => p.IsDeleted == false).Any(p => p.Name == name && p.Type == type))
                {
                    db.Presets.RemoveRange(db.Presets.Where(p => p.IsDeleted == false).Where(p => p.Name == name && p.Type == type).ToArray());
                }
                db.Presets.Add(preset);
            }
            db.SaveChanges();
        }
    }
}
