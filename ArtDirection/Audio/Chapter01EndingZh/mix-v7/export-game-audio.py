"""Split approved mix into continuous room bed and page-synchronous voice/foley."""
from pathlib import Path
scope={'__file__':str(Path(__file__).with_name('build_mix.py').resolve())}
source=Path(__file__).with_name('build_mix.py').read_text()
exec(compile(source.split('sfx=bed+')[0],str(Path(__file__).with_name('build_mix.py')),'exec'),scope)
P=scope['PROJECT']/'Assets/DarkFairytale/UI/Chapter01Ending/Audio'
write=scope['write']; signal=scope['voice']+scope['foley']*scope['duck'][:,None]+scope['fire_focus']
start=0
for i,n in enumerate(scope['sizes'],1):
 write(P/f'Page{i:02}.wav',signal[start:start+n]);start+=n
write(P/'StoryAmbience.wav',scope['loop']*2.0)
print('Exported continuous bed and five voice/action tracks; timing unchanged.')
