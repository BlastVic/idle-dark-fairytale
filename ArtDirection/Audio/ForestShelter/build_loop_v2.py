"""90-second stereo forest shelter loop; numpy and existing ElevenLabs stems."""
from pathlib import Path
import numpy as np
import wave,json,sys,subprocess
ROOT=Path(__file__).resolve().parent;PROJECT=ROOT.parents[2];OUT=PROJECT/'output/forest-shelter'
SR=44100;DURATION=90;N=SR*DURATION;rng=np.random.default_rng(1937)
def read(name):
 with wave.open(str(ROOT/'source'/name)) as f:
  rate=f.getframerate();ch=f.getnchannels();assert f.getsampwidth()==2
  a=np.frombuffer(f.readframes(f.getnframes()),'<i2').reshape(-1,ch).astype(float)/32768
 if ch==1:a=np.repeat(a,2,axis=1)
 if rate!=SR:a=resample(a,SR/rate)
 return a[:,:2]
def resample(a,scale):
 q=np.arange(round(len(a)*scale))/scale
 return np.column_stack([np.interp(q,np.arange(len(a)),a[:,c]) for c in range(2)])
def filt(a,lo,hi):
 f=np.fft.rfftfreq(len(a),1/SR);h=(1-np.exp(-(f/lo)**4))/(1+(f/hi)**8)
 return np.fft.irfft(np.fft.rfft(a,axis=0)*h[:,None],n=len(a),axis=0)
def rms(a):return np.sqrt(np.mean(a*a))
def level(a,db):return a*10**(db/20)/max(rms(a),1e-9)
# Circular overlap-add wind: gently continuous, no heavy low-frequency rumble.
a=level(filt(read('forest.wav'),220,2400),-55)
a=resample(a,30*SR/len(a));w=np.sin(np.linspace(0,np.pi,len(a)))**2
bed=np.zeros((N,2));weight=np.zeros(N)
for i in range(4):
 ix=(np.arange(len(a))+round(i*22.5*SR))%N
 bed[ix]+=np.roll(a,i*1379,axis=0)*w[:,None];weight[ix]+=w*w
bed/=np.sqrt(np.maximum(weight,1e-9))[:,None]
t=np.arange(N)/SR;bed*= (.85+.15*np.cos(2*np.pi*t/90))[:,None]
tracks={k:np.zeros((N,2)) for k in ['birds','wolves']};events=[]
def put(kind,name,at,db,pan,lo,hi,scale=1,echo=False):
 a=level(filt(resample(read(name),scale),lo,hi),db)
 k=min(round(.12*SR),len(a)//3);a[:k]*=np.linspace(0,1,k)[:,None];a[-k:]*=np.linspace(1,0,k)[:,None]
 a*=np.sqrt(np.array([1-pan,1+pan]))
 ix=(np.arange(len(a))+round(at*SR))%N;tracks[kind][ix]+=a
 if echo:
  for dt,g in [(.17,.12),(.39,.065),(.71,.025)]:tracks[kind][(ix+round(dt*SR))%N]+=a[:,::-1]*g
 events.append(dict(layer=kind,source=name,time=at,duration=len(a)/SR,level_dbfs=db,pan=pan))
# Only bird and wolf events; insects and leaf rustles removed.
for j,at in enumerate([8.1,19.2,43.6,58.5,82.6]):
 put('birds',f'v2-bird-{j%2+1}.wav',at,-35.5-(j%3)*1.7,[-.65,.5,-.35,.7,-.5][j],350,3300,1+(j%3-1)*.06,True)
put('wolves','distant-wolf.wav',26.4,-34,.55,230,2400,1.05,True)
put('wolves','distant-wolf.wav',67.7,-38,-.65,280,1900,1.12,True)
mix=bed+sum(tracks.values())
# Preserve v1 bird/wolf volume; do not normalize away the reduced background.
gain=min(10**(7.96137350917265/20),10**(-6/20)/abs(mix).max());mix*=gain
# The wrap already continues circular events; eliminate only a residual sample step over 2ms.
k=round(.002*SR);delta=mix[0]-mix[-1];mix[-k:]+=np.linspace(0,1,k)[:,None]*delta
assert np.max(abs(mix))<1

def write(path,a):
 with wave.open(str(path),'wb') as f:
  f.setnchannels(2);f.setsampwidth(2);f.setframerate(SR);f.writeframes(np.round(a*32767).astype('<i2').tobytes())
write(ROOT/'ForestShelterLoop-v2.wav',mix)
write(OUT/'ForestShelter-v2-two-loops.wav',np.tile(mix,(2,1)))
write(OUT/'ForestShelter-v2-seam-preview.wav',np.concatenate([mix[-10*SR:],mix[:10*SR]]))
sys.path.insert(0,str(PROJECT/'output/prologue-film/tools'));import imageio_ffmpeg
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-i',str(ROOT/'ForestShelterLoop-v2.wav'),'-c:a','libmp3lame','-b:a','192k',str(OUT/'ForestShelter-v2-preview.mp3')],check=True)
report=dict(duration=DURATION,sample_rate=SR,peak_dbfs=float(20*np.log10(abs(mix).max())),rms_dbfs=float(20*np.log10(rms(mix))),seam_step=float(abs(mix[0]-mix[-1]).max()),events=events,unity_modified=False)
(ROOT/'mix-plan-and-validation-v2.json').write_text(json.dumps(report,ensure_ascii=False,indent=2))
print(json.dumps({k:v for k,v in report.items() if k!='events'},indent=2))
