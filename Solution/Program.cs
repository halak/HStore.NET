using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Halak
{
    public sealed class Program
    {
        static void Main(string[] args)
        {
            var a = Hstore.Parse("foo=>bar,baz=>whatever");
            Trace.Assert(a["foo"] == "bar");
            Trace.Assert(a["baz"] == "whatever");

            var b = Hstore.Parse("'\"1-a\" => \"anything at all\"'::hstore");
            Trace.Assert(b["1-a"] == "anything at all");
        }
    }
}
