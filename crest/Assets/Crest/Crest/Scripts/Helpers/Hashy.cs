// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest
{
    public static class Hashy
    {
        public static int CreateHash() => 0x19384567;

        public static void Add(float value, ref int hash)
        {
            hash ^= value.GetHashCode();
        }

        public static void Add(int value, ref int hash)
        {
            hash ^= value;
        }

        public static void Add(bool value, ref int hash)
        {
            hash ^= (value ? 0x74659374 : 0x62649035);
        }

        public static void Add(object value, ref int hash)
        {
            // Will be the index of this object instance
            hash ^= value.GetHashCode();
        }
    }
}
