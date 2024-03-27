using Newtonsoft.Json;
using UnityEngine;

namespace TrollBlocker
{
    public class JailConfig
    {
        public int areaId { get; set; }
        public string ExitPosition { get; set; }
        [JsonIgnore]
        public Vector3 VPosition
        {
            get => Vector3Converter.ReadJson(ExitPosition);
            set => ExitPosition = Vector3Converter.WriteJson(value);
        }
    }
}
