using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;

namespace CodeWalker.Utils
{
    public class CameraPreset
    {
        public string Name { get; }
        public string Position { get; set; }
        public string Rotation { get; set; }
        public string Distance { get; set; }

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

        public string ToJson()
        {
            return JsonConvert.SerializeObject(Values);
        }

        public static CameraPresetCollection FromJson(string jsonString)
        {
            var collection = new CameraPresetCollection();
            collection.Values = JsonConvert.DeserializeObject<List<CameraPreset>>(jsonString);
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
    }
}
