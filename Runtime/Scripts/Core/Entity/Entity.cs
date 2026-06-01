using System;

namespace OSK
{
    /// <summary>
    /// Pure ECS Entity. Trọng lượng siêu nhẹ (chỉ là 1 số nguyên).
    /// </summary>
    public struct Entity : IEquatable<Entity>
    {
        public readonly int ID;
        public readonly int Version; // Dùng để chống lỗi truy cập vào Entity cũ đã bị huỷ

        public Entity(int id, int version)
        {
            ID = id;
            Version = version;
        }

        public static readonly Entity Null = new Entity(-1, 0);

        public bool Equals(Entity other) => ID == other.ID && Version == other.Version;
        public override bool Equals(object obj) => obj is Entity other && Equals(other);
        public override int GetHashCode() => ID.GetHashCode();

        public static bool operator ==(Entity left, Entity right) => left.Equals(right);
        public static bool operator !=(Entity left, Entity right) => !left.Equals(right);
    }

    /// <summary>
    /// Interface cho các Component thuần dữ liệu (Data-Oriented).
    /// Bắt buộc phải là struct.
    /// </summary>
    public interface IComponentData
    {
    }
}