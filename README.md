# ConfigMusic

This mod does nothing by itself, however it adds the ability for modders to add music to planets or even biomes using simple configs!

## How to make a ConfigMusic config:

1. Make a config anywhere in GameData. You can use an existing one too if you really want to.
2. Then, put in this code: <code>
BACKGROUND_MUSIC
{
    type = WAV
    path = YourMod/PathToFile/YourAudioFile.wav
    planet = PlanetName
    biome = BiomeName
}</code>
3. <code>type</code> can only be WAV and this will load an extrenal file, however support for reusing stock music is coming soon.
4. <code>path</code> is the path to your audiofile. This path is from GameData, so accessing a file in the folder YourMod in GameData will be something like "YourMod/...". This file must be a .wav file.
5. <code>planet</code> is the planet that the music is assigned to. Unless <code>biome</code> is specified as well, when the player gets to this planet, the music will start playing. 
6. <code>biome</code> is optional, however if it is included the music will be limited to playing only in the specified biome.

There is currently no error checking unless the mod itself messes up (invalid name of the created GameObject), so make sure to check what values you are using unless you want runtime errors and unpredictable behavior. Error checking will be added in a future update.
While looking at the code, you may think that BUILTIN/VAB can already be used instead of WAV, however it is extremely glitchy and is a work in progress.