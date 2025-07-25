using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.IO.Hashing;

namespace CYarp.Server.Clients
{
    /// <summary>
    /// 安全的隧道Id
    /// </summary>
    readonly struct TunnelId : IParsable<TunnelId>, IEquatable<TunnelId>
    {
        private readonly Guid guid;
        private readonly bool isValid;
        private static readonly byte[] secureSalt = Guid.NewGuid().ToByteArray();
        public static readonly TunnelId Empty = default;

        /// <summary>
        /// 获取是否为有效的TunnelId值
        /// </summary>
        public bool IsValid => this.isValid;

        private TunnelId(Guid guid)
        {
            this.guid = guid;
            this.isValid = Validate(guid);
        }

        private TunnelId(Guid guid, bool isValid)
        {
            this.guid = guid;
            this.isValid = isValid;
        }

        /// <summary>
        /// 生成安全的tunnelId
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static TunnelId NewTunnelId(string clientId, int value)
        {
            Span<byte> span = stackalloc byte[16];

            // [0-3]  clientId
            BinaryPrimitives.WriteInt32BigEndian(span, clientId.GetHashCode());

            // [4-5]  highValue
            BinaryPrimitives.WriteInt16LittleEndian(span[4..], (short)(value >> 16));

            // [6-7] lowValue
            BinaryPrimitives.WriteInt16LittleEndian(span[6..], (short)value);

            // [8-11] ticks
            BinaryPrimitives.WriteInt32BigEndian(span[8..], Environment.TickCount);

            // [12-15] 校验值
            var hash32 = new XxHash32();
            hash32.Append(span[..12]);
            hash32.Append(secureSalt);
            hash32.GetCurrentHash(span[12..]);

            var guid = new Guid(span);
            return new TunnelId(guid, isValid: true);
        }

        private static bool Validate(Guid guid)
        {
            Span<byte> span = stackalloc byte[20];
            guid.TryWriteBytes(span);

            // 计算校验值，放到[16-19]
            var hash32 = new XxHash32();
            hash32.Append(span[..12]);
            hash32.Append(secureSalt);
            hash32.GetCurrentHash(span[16..]);

            var hash1 = span.Slice(12, 4);
            var hash2 = span.Slice(16, 4);
            return hash1.SequenceEqual(hash2);
        }

        public static TunnelId Parse(string s, IFormatProvider? provider)
        {
            var guid = Guid.Parse(s, provider);
            return new TunnelId(guid);
        }

        public static bool TryParse(ReadOnlySpan<char> s, [MaybeNullWhen(false)] out TunnelId result)
        {
            if (Guid.TryParse(s, out var value))
            {
                result = new TunnelId(value);
                return true;
            }

            result = default;
            return false;
        }

        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out TunnelId result)
        {
            if (Guid.TryParse(s, provider, out var guid))
            {
                result = new TunnelId(guid);
                return true;
            }

            result = default;
            return false;
        }

        public bool Equals(TunnelId other)
        {
            return this.guid == other.guid;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is TunnelId tunnelId && this.Equals(tunnelId);
        }

        public override int GetHashCode()
        {
            return this.guid.GetHashCode();
        }

        public override string ToString()
        {
            return this.guid.ToString();
        }
    }
}
