HStore.NET
==========

HStore.NET is provide [PostgreSQL HStore](http://www.postgresql.org/docs/9.0/static/hstore.html) class.

Basic Usage
-----------
```cs
var a = HStore.Parse("'foo=>bar,baz=>whatever'");
Trace.Assert(a["foo"] == "bar");
Trace.Assert(a.Get("baz") == "whatever");
Trace.Assert(a.ToString() == "'foo=>bar,baz=>whatever'");

var b = new HStore(new Dictionary<string, HStore.Value>() {
    ["foo"] = "bar",
    ["xxx"] = 123,
});

var c = new HStore(new {X = 10, Y = 30, Z = 500});
var xyz = c.Get("X", "Y", "Z");
Trace.Assert(xyz[0] == "10");
Trace.Assert(xyz[1] == "30");
Trace.Assert(xyz[2] == "500");

var d = a.Concat(b);
Trace.Assert(d["foo"] == "bar");
Trace.Assert(d["xxx"] == "123");
```