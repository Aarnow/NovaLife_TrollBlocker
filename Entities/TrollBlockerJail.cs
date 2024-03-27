using SQLite;
using UnityEngine;

namespace TrollBlocker.Entities
{
    public class TrollBlockerJail : ModKit.ORM.ModEntity<TrollBlockerJail>
    {
        [AutoIncrement][PrimaryKey] public int Id { get; set; }
        public string Name { get; set; }
        public string Position { get; set; }
        [Ignore]
        public Vector3 VPosition
        {
            get => Vector3Converter.ReadJson(Position);
            set => Position = Vector3Converter.WriteJson(value);
        }

        public TrollBlockerJail()
        {
        }
    }
}
