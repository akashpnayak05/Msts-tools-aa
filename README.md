# ShapeToObj Own
New independent C# MSTS/Open Rails `.S` to OBJ converter.

Build with GitHub Actions:
Actions -> Build ShapeToObj Own -> Run workflow -> download `ShapeToObj-Own-Windows`.

No Python, Open Rails runtime, or Aspose dependency.

Current version supports uncompressed text `.S`, points, matrices/hierarchy,
vertex states/sets and indexed triangle lists, and writes `.OBJ + .MTL`.

It is intentionally a new implementation and is not yet a complete replacement
for every MSTS `.S` variant. Compressed shapes, full material/texture mapping,
animations and some advanced submodel/geometry variants still need support.
