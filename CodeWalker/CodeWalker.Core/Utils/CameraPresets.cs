using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeWalker.Utils
{
    public class CameraPreset
    {
        public string Name { get; set; }
        public string Position { get; set; }
        public string Rotation { get; set; }
        public string Distance { get; set; }

        [JsonIgnore]
        public bool IsActive { get; set; }

        public CameraPreset()
        {
        }

        public CameraPreset(string name, string position, string rotation, string distance)
        {
            Name = name;
            Position = position;
            Rotation = rotation;
            Distance = distance;
        }
    }

    public class CameraPresetCollection
    {
        public List<CameraPreset> Values { get; set; } = new List<CameraPreset>();

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public string Serialize()
        {
            return JsonSerializer.Serialize(Values, JsonOptions);
        }

        public static CameraPresetCollection Deserialize(string jsonString)
        {
            var collection = new CameraPresetCollection();
            if (string.IsNullOrWhiteSpace(jsonString) || jsonString == "[]")
            {
                collection.Values = new List<CameraPreset>();
            }
            else
            {
                try
                {
                    collection.Values = JsonSerializer.Deserialize<List<CameraPreset>>(jsonString, JsonOptions) ?? new List<CameraPreset>();
                }
                catch
                {
                    collection.Values = new List<CameraPreset>();
                }
            }
            return collection;
        }

        public void Add(CameraPreset preset)
        {
            Values.Add(preset);
        }

        public bool RemoveByName(string name)
        {
            var mapValueToRemove = Values.FirstOrDefault(v => v.Name == name);
            if (mapValueToRemove != null)
            {
                Values.Remove(mapValueToRemove);
                return true;
            }
            return false;
        }

        public CameraPreset GetByIndex(int index)
        {
            if (index < 0 || index >= Values.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
            }
            return Values[index];
        }

        public CameraPreset GetByName(string name)
        {
            return Values.FirstOrDefault(v => v.Name == name);
        }
    }
}
