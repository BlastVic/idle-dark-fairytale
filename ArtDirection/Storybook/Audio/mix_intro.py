"""Build nominal 9.8s intro from locally downloaded ElevenLabs stems (numpy only)."""
from pathlib import Path
import wave
import numpy as np
RATE=44100
src=Path('output/storybook-audio/source'); dst=Path('Assets/Audio/StorybookIntro/Clips')
def read(name):
 with wave.open(str(src/(name+'.wav'))) as f:
  a=np.frombuffer(f.readframes(f.getnframes()),'<i2').reshape(-1,f.getnchannels()).astype(float)/32768
  assert f.getframerate()==RATE
 return a

def fade(a, start=.02, end=.06):
 a=a.copy(); n=min(int(start*RATE),len(a)); m=min(int(end*RATE),len(a))
 if n: a[:n]*=np.linspace(0,1,n)[:,None]
 if m: a[-m:]*=np.linspace(1,0,m)[:,None]
 return a

def norm(a,peak): return a*peak/max(np.max(np.abs(a)),.0001)
def write(p,a):
 assert np.max(np.abs(a))<1
 with wave.open(str(p),'wb') as f:
  f.setnchannels(2);f.setsampwidth(2);f.setframerate(RATE);f.writeframes((a*32767).astype('<i2').tobytes())
track=np.zeros((round(9.8*RATE),2))
def put(a,t,gain=1):
 i=round(t*RATE);n=min(len(a),len(track)-i);track[i:i+n]+=a[:n]*gain
cover=norm(fade(read('cover')),.42);put(cover,1)
page=read('page'); page=page[int(.32*RATE):int(1.14*RATE)]
# Overlap eight 0.82s leaves at the actual Spine start times, with tiny timbral variation.
for i in range(8):
 q=np.arange(len(page))*(1+(i%3-1)*.025)
 leaf=np.column_stack([np.interp(q,np.arange(len(page)),page[:,c]) for c in range(2)])
 put(norm(fade(leaf,.025,.14),.23),2.12+i*.22,[.85,1,.9,.95,.88,1,.9,1.05][i])
# Paper-grain reveal and reverse-paper air swell are derived from the same material.
texture=norm(read('ambience'),.12)
put(fade(texture[:int(1.8*RATE)],.3,.5),4.6,.7)
swell=np.tile(page[::-1],(3,1))[:int(2.2*RATE)]
put(norm(fade(swell,1.65,.25),.12),6.7)
track=fade(track,.01,.3)
write(dst/'BookIntroFoley.wav',track)
# Seamless crossfade loop, attenuated at runtime so paper remains the foreground.
a=norm(read('ambience'),.5); n=int(.25*RATE);w=np.linspace(0,1,n)[:,None]
a=np.concatenate([a[n:-n],a[-n:]*(1-w)+a[:n]*w]);write(dst/'BookIntroAtmosphere.wav',a)
bed=np.tile(a,(int(len(track)/len(a))+1,1))[:len(track)]*.14
preview=track+fade(bed,.5,1.2);write(Path('output/storybook-audio/intro-preview.wav'),preview)
print('9.8s mix peak',float(np.max(np.abs(preview))),'eight page onsets',[round(2.12+i*.22,2) for i in range(8)])
