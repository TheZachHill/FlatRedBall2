# FlatRedBall2.Animation.Content

The `.achx` animation file-format data model (`AnimationChainListSave`, `AnimationChainSave`,
`AnimationFrameSave`, `ColorOperation`, `ShapesSave`) used by
[FlatRedBall2](https://github.com/vchelaru/FlatRedBall2) and the FlatRedBall Animation Editor.

Pure C#, no MonoGame/KNI dependency — this is the shared serialization format only.

Most users should install `FlatRedBall2.MonoGame` or `FlatRedBall2.Kni` instead; both reference
this package automatically and provide the runtime bridge that turns these types into playable
animations backed by real textures.

## License

MIT — see [LICENSE](https://github.com/vchelaru/FlatRedBall2/blob/main/LICENSE).
