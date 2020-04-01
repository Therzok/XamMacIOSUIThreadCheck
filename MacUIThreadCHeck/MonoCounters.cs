using System;
using System.Runtime.InteropServices;
using ObjCRuntime;

namespace UIThreadCheck
{
    [Flags]
    public enum MonoCounterFlags : uint
    {
        /* Counter type, bits 0-7. */
        MONO_COUNTER_INT,    /* 32 bit int */
        MONO_COUNTER_UINT,    /* 32 bit uint */
        MONO_COUNTER_WORD,   /* pointer-sized int */
        MONO_COUNTER_LONG,   /* 64 bit int */
        MONO_COUNTER_ULONG,   /* 64 bit uint */
        MONO_COUNTER_DOUBLE,
        MONO_COUNTER_STRING, /* char* */
        MONO_COUNTER_TIME_INTERVAL, /* 64 bits signed int holding usecs. */
        MONO_COUNTER_TYPE_MASK = 0xf,
        MONO_COUNTER_CALLBACK = 128, /* ORed with the other values */
        MONO_COUNTER_SECTION_MASK = 0x00ffff00,
        /* Sections, bits 8-23 (16 bits) */
        MONO_COUNTER_JIT = 1 << 8,
        MONO_COUNTER_GC = 1 << 9,
        MONO_COUNTER_METADATA = 1 << 10,
        MONO_COUNTER_GENERICS = 1 << 11,
        MONO_COUNTER_SECURITY = 1 << 12,
        MONO_COUNTER_RUNTIME = 1 << 13,
        MONO_COUNTER_SYSTEM = 1 << 14,
        MONO_COUNTER_PERFCOUNTERS = 1 << 15,
        MONO_COUNTER_PROFILER = 1 << 16,
        MONO_COUNTER_INTERP = 1 << 17,
        MONO_COUNTER_TIERED = 1 << 18,
        MONO_COUNTER_LAST_SECTION,

        /* Unit, bits 24-27 (4 bits) */
        MONO_COUNTER_UNIT_SHIFT = 24,
        MONO_COUNTER_UNIT_MASK = 0xFu << 24, // MONO_COUNTER_UNIT_SHIFT,
        MONO_COUNTER_RAW = 0 << 24,  /* Raw value */
        MONO_COUNTER_BYTES = 1 << 24, /* Quantity of bytes. RSS, active heap, etc */
        MONO_COUNTER_TIME = 2 << 24,  /* Time interval in 100ns units. Minor pause, JIT compilation*/
        MONO_COUNTER_COUNT = 3 << 24, /*  Number of things (threads, queued jobs) or Number of events triggered (Major collections, Compiled methods).*/
        MONO_COUNTER_PERCENTAGE = 4 << 24, /* [0-1] Fraction Percentage of something. Load average. */

        /* Monotonicity, bits 28-31 (4 bits) */
        MONO_COUNTER_VARIANCE_SHIFT = 28,
        MONO_COUNTER_VARIANCE_MASK = 0xFu << 28, // MONO_COUNTER_VARIANCE_SHIFT,
        MONO_COUNTER_MONOTONIC = 1 << 28, /* This counter value always increase/decreases over time. Reported by --stat. */
        MONO_COUNTER_CONSTANT = 1 << 29, /* Fixed value. Used by configuration data. */
        MONO_COUNTER_VARIABLE = 1 << 30, /* This counter value can be anything on each sampling. Only interesting when sampling. */
    };

    public enum MonoResourceType
    {
        MONO_RESOURCE_JIT_CODE, /* bytes */
        MONO_RESOURCE_METADATA, /* bytes */
        MONO_RESOURCE_GC_HEAP,  /* bytes */
        MONO_RESOURCE_COUNT /* non-ABI value */
    }

    public static class MonoCounters
    {
        static readonly Native.CountersEnumCallback cb = new Native.CountersEnumCallback(CountersForeachCallback);

        static readonly byte[] arr = new byte[4096];

        public static void Dump()
        {
            Native.mono_counters_foreach(cb, IntPtr.Zero);

            // doesn't work
            //mono_counters_dump(MONO_COUNTER_MONOTONIC | MONO_COUNTER_SECTION_MASK, IntPtr.Zero);
        }

        [MonoPInvokeCallback(typeof(Native.CountersEnumCallback))]
        static int CountersForeachCallback(IntPtr counter, IntPtr _)
        {
            if (IsInteresting(counter))
            {
                unsafe
                {
                    fixed (byte* ptr = arr)
                    {
                        var size = Native.mono_counters_sample(counter, ptr, arr.Length);
                        Console.WriteLine("{0}: {1}", Native.mono_counter_get_name(counter), CounterToValue(counter, arr, size));
                    }
                }

            }

            return 1;

            static bool IsInteresting(IntPtr counter)
            {
                MonoCounterFlags section = Native.mono_counter_get_section(counter);
                if ((section & MonoCounterFlags.MONO_COUNTER_GC) == 0)
                    return false;

                MonoCounterFlags variance = Native.mono_counter_get_variance(counter);
                if ((variance & MonoCounterFlags.MONO_COUNTER_MONOTONIC | MonoCounterFlags.MONO_COUNTER_VARIABLE) == 0)
                    return false;

                return true;
            }

            static object CounterToValue(IntPtr counter, byte[] arr, int size)
            {
                if (size <= 0)
                    return null;

                switch (Native.mono_counter_get_type(counter))
                {
                    case MonoCounterFlags.MONO_COUNTER_INT:
                        return MemoryMarshal.Read<int>(arr);
                    case MonoCounterFlags.MONO_COUNTER_UINT:
                        return MemoryMarshal.Read<uint>(arr);
                    case MonoCounterFlags.MONO_COUNTER_WORD:
                        return MemoryMarshal.Read<IntPtr>(arr);
                    case MonoCounterFlags.MONO_COUNTER_LONG:
                        return MemoryMarshal.Read<long>(arr);
                    case MonoCounterFlags.MONO_COUNTER_ULONG:
                        return MemoryMarshal.Read<ulong>(arr);
                    case MonoCounterFlags.MONO_COUNTER_DOUBLE:
                        return MemoryMarshal.Read<double>(arr);
                    case MonoCounterFlags.MONO_COUNTER_STRING:
                        return System.Text.Encoding.UTF8.GetString(arr);
                    case MonoCounterFlags.MONO_COUNTER_TIME_INTERVAL:
                        var ts = MemoryMarshal.Read<long>(arr);
                        return TimeSpan.FromMilliseconds(ts / 1000);
                    default:
                        return null;
                }
            }
        }

        static class Native
        {
            const string lib = "__Internal";

            [DllImport(lib)]
            public static extern void mono_counters_register(string descr, MonoCounterFlags type, IntPtr addr);

            [DllImport(lib)]
            public static extern void mono_counters_register_with_size(string name, MonoCounterFlags type, IntPtr addr, int size);

            public delegate void MonoCounterRegisterCallback(IntPtr ptr);

            [DllImport(lib)]
            public static extern void mono_counters_on_register(MonoCounterRegisterCallback callback);

            public delegate int CountersEnumCallback(IntPtr counter, IntPtr user_data);

            [DllImport(lib)]
            public static extern void mono_counters_foreach(CountersEnumCallback cb, IntPtr user_data);

            [DllImport(lib)]
            public unsafe static extern int mono_counters_sample(IntPtr counter, byte* buffer, int buffer_size);

            [DllImport(lib)]
            public static extern string mono_counter_get_name(IntPtr counter);

            [DllImport(lib)]
            public static extern MonoCounterFlags mono_counter_get_type(IntPtr counter);

            [DllImport(lib)]
            public static extern MonoCounterFlags mono_counter_get_section(IntPtr counter);

            [DllImport(lib)]
            public static extern MonoCounterFlags mono_counter_get_unit(IntPtr counter);

            [DllImport(lib)]
            public static extern MonoCounterFlags mono_counter_get_variance(IntPtr counter);

            public delegate void MonoResourceCallback(MonoResourceType resourceType, UIntPtr value, int is_soft);

            [DllImport(lib)]
            public static extern int mono_runtime_resource_limit(MonoResourceType resource_type, UIntPtr soft_limit, UIntPtr hard_limit);

            [DllImport(lib)]
            public static extern void mono_runtime_resource_set_callback(MonoResourceCallback callback);

            [DllImport(lib)]
            public static extern void mono_runtime_resource_check_limit(MonoResourceType resource_type, UIntPtr value);
        }
    }
}
