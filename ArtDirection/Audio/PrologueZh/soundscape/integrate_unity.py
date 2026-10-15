import json,wave,array,math
from pathlib import Path
root=Path(__file__).resolve().parents[4];out=root/'Assets/Audio/StorybookIntro/Prologue/Chinese';out.mkdir(parents=True,exist_ok=True)
# Prefer the approved replacement ending; legacy assembly below is kept for provenance.
approved=root/'ArtDirection/Audio/PrologueZh/full-mix-v3'
if (approved/'prologue-zh-full-v3.wav').exists():
 import shutil
 shutil.copyfile(approved/'prologue-zh-full-v3.wav',out/'PrologueZhFull.wav')
 shutil.copyfile(approved/'VoiceTimeline.zh-CN.v3.json',root/'Assets/DarkFairytale/UI/StorybookIntro/Prologue/VoiceTimeline.zh-CN.json')
 print('Installed approved full-mix-v3 and matching captions')
 raise SystemExit(0)
base=root/'output/prologue-zh-soundscape-v2'
def read(p):
 with wave.open(str(p)) as f:
  a=array.array('h',f.readframes(f.getnframes()));return a,f.getframerate(),f.getnchannels()
a,sr,ch=read(base/'prologue-zh-soundscape.wav');bed,_,_=read(base/'ambience-and-villagers.wav')
# The original female quote starts audibly at 90.26s, earlier than its
# transcript timestamp. Remove its clipped onset while retaining the bed.
for frame in range(round(92.22*sr),round(92.42*sr)):
 gain=max(0.0,1-(frame/sr-92.22)/.02)
 for channel in range(ch):
  i=frame*ch+channel
  a[i]=round(bed[i]+(a[i]-bed[i])*gain)

remap=lambda x: x+2+(3.46 if x>=93.16 else 0)
pause=3.0
cuts=[24.9,43.0797505669,56.444829932,74.4749886621,98.41,115.064920635] # measured low-energy narration gaps
shift=lambda t:t+sum(pause for c in cuts if t>=c)
result=array.array('h');last=0
for cut in cuts:
 k=round(cut*sr)*ch;result.extend(a[last:k]);n=round(pause*sr)*ch
 # The first cut precedes the original forest fade-in. Do not preview future
 # ambience here and then jump back to its silent start when narration resumes.
 b=array.array('h',[0])*n if cut==cuts[0] else bed[k:k+n]
 # Continue the inserted environment into the resumed section, crossfading
 # only the ambience back to its original timeline. Never fade to a constant
 # sample (DC), which sounds like a brief dropout before each new sentence.
 if cut!=cuts[0]:
  fade_frames=round(.45*sr)
  for frame in range(fade_frames):
   t=frame/(fade_frames-1);f=t*t*(3-2*t)
   for channel in range(ch):
    i=frame*ch+channel
    environment=bed[k+n+i]*(1-f)+bed[k+i]*f
    a[k+i]=round(a[k+i]-bed[k+i]+environment)
 result.extend(b);last=k
result.extend(a[last:])
with wave.open(str(out/'PrologueZhFull.wav'),'wb') as f:f.setnchannels(ch);f.setsampwidth(2);f.setframerate(sr);f.writeframes(result.tobytes())
d=json.loads((root/'Assets/DarkFairytale/UI/StorybookIntro/Prologue/Captions.zh-CN.json').read_text())
times=[[(.16,5.52),(6.32,10.56),(11.2,14.96),(15.12,18.88),(18.96,22.72)],[(23.52,28.16),(28.8,31.28),(31.573,34.16),(34.32,37.92),(38.347,41.12)],[(41.28,44.4),(44.56,49.6),(50.107,51.52),(51.68,54.48)],[(54.72,57.28),(57.44,61.52),(61.76,67.2),(67.44,72.48)],[(72.933,77.12),(77.28,80.88),(81.04,84.48),(84.64,88),(88.08,90.32),(90.533,93.12)],[(93.2,95.84),(96,99.36),(99.52,101.76),(102,104.96),(105.04,109.6)],[(110.027,111.92),(112.12,115.84),(115.92,120.8),(121.04,123.36),(123.6,126.24),(126.32,128.88)]]
flat=[]
for pg,tt in zip(d['pages'],times):
 for cap,(start,end) in zip(pg['captions'],tt):
  s,e=remap(start),remap(end)
  if start==90.533:s,e=92.9,98.4
  caption_end=min([shift(e)+.08]+[shift(c)-pause for c in cuts if s<c<=e+.08])
  flat.append(dict(start=round(shift(s),3),end=round(caption_end,3),text=cap['text'].replace('\n','') if pg==d['pages'][0] else cap['text']))
config=dict(captions=flat,pageRevealTimes=[round(c+i*pause,3) for i,c in enumerate(cuts)],openStart=11,openEnd=14.7,cameraStart=7.3,cameraEnd=10.7,transitionDuration=pause,pageTurnDuration=1.6,pageInkDuration=.6,duration=len(result)/ch/sr)
(root/'Assets/DarkFairytale/UI/StorybookIntro/Prologue/VoiceTimeline.zh-CN.json').write_text(json.dumps(config,ensure_ascii=False,indent=2))
print(config['duration'],config['pageRevealTimes'])
