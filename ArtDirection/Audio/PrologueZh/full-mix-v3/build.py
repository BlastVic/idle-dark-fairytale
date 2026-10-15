from pathlib import Path
import wave,json,sys,subprocess,hashlib
import numpy as np
ROOT=Path(__file__).resolve().parent;PROJECT=ROOT.parents[3];OUT=PROJECT/'output/prologue-zh-full-v3';SR=44100
sys.path.insert(0,str(PROJECT/'output/prologue-film/tools'));import imageio_ffmpeg
P6=ROOT.parent/'page06-v3'
def read(p):
 with wave.open(str(p)) as f:
  rate=f.getframerate();n=f.getnchannels();assert f.getsampwidth()==2
  a=np.frombuffer(f.readframes(f.getnframes()),'<i2').astype(float).reshape(-1,n)/32768
 if n==1:a=np.repeat(a,2,axis=1)
 if rate!=SR:
  q=np.arange(round(len(a)*SR/rate))*rate/SR;a=np.column_stack([np.interp(q,np.arange(len(a)),a[:,c]) for c in range(2)])
 return a[:,:2]
def rms(a):return np.sqrt(np.mean(a*a))
def write(p,a):
 assert abs(a).max()<1
 with wave.open(str(p),'wb') as f:
  f.setnchannels(2);f.setsampwidth(2);f.setframerate(SR);f.writeframes(np.round(a*32768).astype('<i2').tobytes())
def filt(a,lo,hi):
 fq=np.fft.rfftfreq(len(a),1/SR);g=(1-np.exp(-(fq/lo)**4))/(1+(fq/hi)**8)
 return np.fft.irfft(np.fft.rfft(a,axis=0)*g[:,None],n=len(a),axis=0)
old=read(OUT/'baseline-full.wav');bed=read(PROJECT/'output/prologue-zh-soundscape-v2/ambience-and-villagers.wav')
raw=read(PROJECT/'output/prologue-zh-soundscape/narration.wav');new=read(P6/'page06-narration.wav')
# Keep all six existing page reveal timestamps and the completed transition.
cut=115.064920635;join=round((cut+18)*SR);start=join/SR
# Fit the actual saved mix gain against a phrase in page five, away from joins.
r=raw[round(96*SR):round(99*SR)];m=old[round(116.46*SR):round(119.46*SR)]
gain=float(np.sum(r*m)/np.sum(r*r));ref=raw[round(93.2*SR):round(109.6*SR)]*gain
active=lambda a:a[np.max(abs(a),axis=1)>.012]
vg=rms(active(ref))/rms(active(new));new*=vg
N=len(new)+3*SR;t=np.arange(N)/SR
# The preceding inserted 3s transition ended at bed[cut+3]; continue from it.
k=round((cut+3)*SR);carried=np.zeros((N,2));available=min(N,len(bed)-k);carried[:available]=bed[k:k+available]
carried*=np.clip((9-t)/4,0,1)[:,None]
wind=filt(read(ROOT.parent/'soundscape/source/forest.wav'),220,3200)
wind*=10**(-46/20)/max(rms(wind),1e-9)
# Fill the longer replacement scene with quiet forest, crossfade below carried bed.
wind=np.tile(wind,(N//len(wind)+1,1))[:N]
forest=wind*np.clip(t/5,0,1)[:,None]
background=(carried+forest)*np.clip((N/SR-t)/3,0,1)[:,None]
wolf=filt(read(ROOT/'distant-wolf.wav'),250,2200);wolf*=10**(-37/20)/max(rms(wolf),1e-9)
f=min(round(.35*SR),len(wolf)//2);wolf[:f]*=np.linspace(0,1,f)[:,None];wolf[-f:]*=np.linspace(1,0,f)[:,None]
wolf*=np.array([.75,1]);howl=np.zeros((N,2));at=16.2;h=round(at*SR);howl[h:h+len(wolf)]+=wolf
for delay,g in [(.18,.16),(.37,.08),(.61,.035)]:
 d=round(delay*SR);howl[h+d:h+d+len(wolf)]+=wolf*g
voiced=np.zeros((N,2));voiced[:len(new)]=new
# Tiny edge taper on the new recording only, not the ambience.
f=round(.008*SR);voiced[:f]*=np.linspace(0,1,f)[:,None]
tail=voiced+background+howl
assert abs(tail).max()<.99,'Unexpected tail clipping; adjust only new segment'
mix=np.concatenate([old[:join],tail]);assert np.array_equal(mix[:join],old[:join])
write(ROOT/'prologue-zh-full-v3.wav',mix);write(OUT/'page05-to-page06-preview.wav',mix[round(122.4*SR):])
write(OUT/'page06-mixed.wav',tail)
ff=imageio_ffmpeg.get_ffmpeg_exe()
for source,dest in [(ROOT/'prologue-zh-full-v3.wav',ROOT/'prologue-zh-full-v3.mp3'),(OUT/'page05-to-page06-preview.wav',OUT/'page05-to-page06-preview.mp3')]:
 subprocess.run([ff,'-hide_banner','-loglevel','error','-y','-i',str(source),'-c:a','libmp3lame','-b:a','192k',str(dest)],check=True)
config=json.loads((OUT/'baseline-timeline.json').read_text());config['captions']=config['captions'][:-6]
for c in json.loads((P6/'sentence-timings.json').read_text())['sentences']:
 config['captions'].append(dict(start=round(start+c['start'],3),end=round(start+c['end'],3),text=c['text']))
config['duration']=len(mix)/SR
(ROOT/'VoiceTimeline.zh-CN.v3.json').write_text(json.dumps(config,ensure_ascii=False,indent=2))
report=dict(duration=len(mix)/SR,prefix_unchanged_until_seconds=start,prefix_bit_identical=True,new_narration_gain_db=float(20*np.log10(vg)),mix_peak_dbfs=float(20*np.log10(abs(mix).max())),join_step=float(abs(mix[join]-mix[join-1]).max()),wolf_start_seconds=start+at,wolf_rms_dbfs=float(20*np.log10(rms(wolf))),page_reveal_times_unchanged=True,unity_assets_modified=False)
(ROOT/'validation.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
