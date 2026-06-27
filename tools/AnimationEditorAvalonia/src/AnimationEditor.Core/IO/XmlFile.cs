using System;
using System.IO;
using System.Xml.Serialization;

namespace AnimationEditor.Core.IO;

// Thin wrapper around System.Xml.Serialization.XmlSerializer that replaces FRB1's
// FileManager.XmlSerialize/XmlDeserialize. Used by AE companion .aeproperties
// files and clipboard payloads — AOT is not a concern in those code paths.
//
// extraTypes registers polymorphic member types that the serializer can't discover
// statically — needed because ShapesSave.Shapes is List<object> holding AARectSave/
// CircleSave/PolygonSave. Without them, serializing a frame/chain that contains a
// shape throws "type ... was not expected".
internal static class XmlFile
{
    public static void Serialize<T>(T obj, string path)
    {
        var serializer = MakeSerializer<T>();
        using var stream = File.Create(path);
        serializer.Serialize(stream, obj);
    }

    public static void SerializeToString<T>(T obj, out string xml, params Type[] extraTypes)
    {
        var serializer = MakeSerializer<T>(extraTypes);
        using var writer = new StringWriter();
        serializer.Serialize(writer, obj);
        xml = writer.ToString();
    }

    public static T Deserialize<T>(string path)
    {
        var serializer = MakeSerializer<T>();
        using var stream = File.OpenRead(path);
        return (T)serializer.Deserialize(stream)!;
    }

    public static T DeserializeFromString<T>(string xml, params Type[] extraTypes)
    {
        var serializer = MakeSerializer<T>(extraTypes);
        using var reader = new StringReader(xml);
        return (T)serializer.Deserialize(reader)!;
    }

    // The single-arg XmlSerializer is process-cached by the framework; the extraTypes
    // overload is not, so only take it when extraTypes are actually supplied.
    private static XmlSerializer MakeSerializer<T>(Type[]? extraTypes = null) =>
        extraTypes is { Length: > 0 }
            ? new XmlSerializer(typeof(T), extraTypes)
            : new XmlSerializer(typeof(T));
}
