[![State-of-the-art Shitcode](https://img.shields.io/static/v1?label=State-of-the-art&message=Shitcode&color=7B5804)](https://github.com/trekhleb/state-of-the-art-shitcode)
# Trainwrecked is a 1-4 player cooperative game about operating a train as it travels along an endless stretch of rail procedurally-generated world. Maintain, refuel, and even construct new parts of the train to keep it running throughout the world.

## Some planned features:
- Several biomes, each with uniquely-generating terrain and points of interest.
- Derailing and rerailing of trains.
- Scavenging of parts and resources from wrecked trains and world points of interest.
- Different types of trains including Diesel, steam, kinetic, electric, and nuclear.
- Vehicles other than trains including railway speeders and draisines
- (?) Enemies- source of resources and fuel.

## Notable challenges/problems:
- Requirement of a seeded random noisefunction for heightmap terrain gen
- Specifics of train rerailing (too boring in real life)
- Violation of the conservation of energy on electric trains (windmill problem)
- Multiplayer save state synchronization
    - should player save be independent from world save?
        - would simplify loading saves and greatly trim save file size
        - would allow players to go into one world and get resources and then keep them in another world, breaking balancing