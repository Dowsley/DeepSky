# Terrain rock material

The rock layer of `Assets/Materials/Rippled sand.mat` uses ambientCG
[Rock 030](https://ambientcg.com/view?id=Rock030), distributed under
[CC0 1.0 Universal](https://docs.ambientcg.com/license/). Commercial use,
modification and redistribution of the source files are permitted; attribution
is optional. Credit: ambientCG.com.

`Assets/Textures/Terrain/CragRockColor.png` and `CragRockNormal.png` are unmodified
copies of `Rock030_1K-PNG_Color.png` and `Rock030_1K-PNG_NormalGL.png` from the
[1K PNG download](https://ambientcg.com/get?file=Rock030_1K-PNG.zip).

SHA-256, color then normal:

```text
b223d2e16e368a44a45dbd728b0420f82937cda1405d370ebbe5484755f9441e
99aa9436f1672e5615bc79a121ebfb86f61d40d8d14b27780543dfa7937ddf8d
```

Unity imports both maps at 256 x 256, uncompressed, with Point filtering, Repeat
wrapping and mipmaps. The color map is sRGB; the OpenGL normal map uses Default
texture type with sRGB disabled. The surface shader reads raw RGB normal channels,
so Unity's packed Normal Map import is unsuitable.

The rock repeats every 3.2 metres on each world-axis projection
(`_RockWorldScale = 0.3125`), giving 80 texels per projected metre at full detail.
`_RockColor = (0.54, 0.558, 0.576)` keeps the layered surface dark slate under the
underwater lighting. Mipmaps preserve distant stability; coarseness comes from
the imported texture, independently of the camera's retro presentation settings.
