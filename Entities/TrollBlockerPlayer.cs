using SQLite;

namespace TrollBlocker.Entities
{
    public class TrollBlockerPlayer : ModKit.ORM.ModEntity<TrollBlockerPlayer>
    {
        [AutoIncrement][PrimaryKey] public int Id { get; set; }
        public int PlayerId { get; set; }
        public int CreatedAt { get; set; }
        public bool IsActive { get; set; }
            

        public TrollBlockerPlayer()
        {
        }
    }
}
