"""Chinese prologue soundscape: original narration except the villagers' line.
Uses local ElevenLabs stems. No Unity scene/timeline modifications.
"""
from pathlib import Path
import numpy as np
import wave,json,sys,subprocess
ROOT=Path(__file__).resolve().parent
PROJECT=ROOT.parents[3]
OUT=PROJECT/'output/prologue-zh-soundscape'; OUT.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(PROJECT/'output/prologue-film/tools'))
import imageio_ffmpeg
SR=44100
LEAD=2.; CUT_A=90.42; CUT_B=93.16; INSERT=5.4
SHIFT=INSERT-(CUT_B-CUT_A)
def read(path):
 with wave.open(str(path)) as w:
  sr=w.getframerate();ch=w.getnchannels();assert w.getsampwidth()==2
  a=np.frombuffer(w.readframes(w.getnframes()),'<i2').astype(float).reshape(-1,ch)/32768
 if ch==1:a=np.repeat(a,2,axis=1)
 if sr!=SR:
  q=np.arange(round(len(a)*SR/sr))*sr/SR
  a=np.column_stack([np.interp(q,np.arange(len(a)),a[:,c]) for c in range(2)])
 return a[:,:2]
def write(path,a):
 assert np.max(abs(a))<1
 with wave.open(str(path),'wb') as w:
  w.setnchannels(2);w.setsampwidth(2);w.setframerate(SR);w.writeframes(np.round(a*32767).astype('<i2').tobytes())
def fade(a,sec=.02):
 a=a.copy();n=min(int(sec*SR),len(a)//2)
 a[:n]*=np.linspace(0,1,n)[:,None];a[-n:]*=np.linspace(1,0,n)[:,None];return a
def filt(a,low,high):
 f=np.fft.rfftfreq(len(a),1/SR);h=(1-np.exp(-(f/low)**4))/(1+(f/high)**8)
 return np.fft.irfft(np.fft.rfft(a,axis=0)*h[:,None],n=len(a),axis=0)
def rms(a):return np.sqrt(np.mean(a*a))
def norm(a,db):return a*10**(db/20)/max(rms(a),1e-8)
voice=read(OUT/'narration.wav')
pre=voice[:round(CUT_A*SR)].copy();post=voice[round(CUT_B*SR):].copy()
n=round(.012*SR);pre[-n:]*=np.linspace(1,0,n)[:,None];post[:n]*=np.linspace(0,1,n)[:,None]
dry=np.concatenate([np.zeros((round(LEAD*SR),2)),pre,np.zeros((round(INSERT*SR),2)),post,np.zeros((3*SR,2))])
# Consistent narrative level. Keep breathing and all other original performance intact.
active=np.max(abs(dry),axis=1)>.015
dry*=10**(-20/20)/max(rms(dry[active]),1e-8)
N=len(dry);t=np.arange(N)/SR;T=N/SR
remap=lambda x:LEAD+x+(SHIFT if x>=CUT_B else 0)
def env(times,values):return np.interp(t,[remap(x) for x in times],values)
def loop(name,db,lo,hi,offset=0):
 a=norm(filt(read(ROOT/'source'/name),lo,hi),db)
 a=np.roll(a,int(offset*SR),axis=0)
 y=np.zeros((N,2));overlap=SR;step=len(a)-overlap
 for start in range(0,N,step):
  b=a[:min(len(a),N-start)].copy();k=min(overlap,len(b))
  if start>0:b[:k]*=np.linspace(0,1,k)[:,None]
  if start+len(a)<N:b[-k:]*=np.linspace(1,0,k)[:,None]
  y[start:start+len(b)]+=b
 return y
forest=loop('forest.wav',-35,140,4000)*env([0,23,41,55,90,110,121,129],[.35,1,.85,.65,.45,1,.6,.25])[:,None]
murmur=loop('village-murmur.wav',-31,180,1800)*env([0,36,41,54,61,72,89,94,105,110,116,121,129],[0,0,.12,.3,.65,.85,1,.9,.7,.4,.05,0,0])[:,None]
# Wider, brighter crowd only as the girl reaches the square; delayed duplicate adds space.
bustle=loop('village-murmur.wav',-36,250,4000,11.7)[:,::-1]*env([0,58,67,73,89,94,105,115,121,129],[0,0,.3,.6,1,.9,.6,.05,0,0])[:,None]
fire=loop('ritual-fire.wav',-37,450,4800)*env([0,43,50,60,73,90,99,110,116,129],[0,0,.2,.4,.75,1,.8,.25,0,0])[:,None]
# Gentle ducking beneath speech, not pumping with every syllable.
act=np.convolve((np.max(abs(dry),axis=1)>.012).astype(float),np.ones(6000)/6000,mode='same')
bed=(forest+murmur+bustle+fire)*(1-.38*np.clip(act*2,0,1))[:,None]
chant=np.zeros_like(dry);onset=LEAD+CUT_A+.48
for i,(delay,gain,pan) in enumerate([(0,1,-.12),(.13,.48,-.65),(.27,.42,.65),(.4,.32,.25)],1):
 a=read(ROOT/'source'/f'villager-{i}.wav')
 a=fade(norm(filt(a,170,4400),-22),.025)
 a*=gain*np.array([np.sqrt((1-pan)/2),np.sqrt((1+pan)/2)])*1.414
 k=round((onset+delay)*SR);chant[k:k+len(a)]+=a
# Short outdoor reflections; avoid a cathedral-like long reverb.
for delay,gain in [(.083,.12),(.167,.075),(.29,.04)]:
 d=round(delay*SR);chant[d:]+=chant[:-d].copy()*gain
# Background stays present, slightly dips for the intelligible group line.
bed*= (1-.15*np.exp(-((t-(onset+1.5))/2)**4))[:,None]
edge=np.clip(t/1.4,0,1)*np.clip((T-t)/3,0,1);bed*=edge[:,None]
mix=dry+chant+bed;gain=min(1,.89/abs(mix).max());mix*=gain
write(OUT/'prologue-zh-soundscape.wav',mix)
write(OUT/'ritual-comparison-preview.wav',mix[round(remap(84)*SR):round(remap(102)*SR)])
write(OUT/'ambience-and-villagers.wav',(chant+bed)*gain)
ff=imageio_ffmpeg.get_ffmpeg_exe()
for name in ['prologue-zh-soundscape','ritual-comparison-preview']:
 subprocess.run([ff,'-hide_banner','-loglevel','error','-y','-i',str(OUT/(name+'.wav')),'-c:a','libmp3lame','-b:a','192k',str(OUT/(name+'.mp3'))],check=True)
# Verify complete exclusion of the female narrator during the substituted line.
a=round((LEAD+CUT_A)*SR);b=a+round(INSERT*SR)
assert np.max(abs(dry[a:b]))==0
assert np.max(abs(chant[b:]))<1e-6,'Chant overlaps following narration'
report={'duration':T,'peak_dbfs':float(20*np.log10(abs(mix).max())),'rms_dbfs':float(20*np.log10(rms(mix))), 'female_quote_removed_original_seconds':[CUT_A,CUT_B],'villagers_start_seconds':onset,'following_narration_seconds':LEAD+CUT_A+INSERT,'narrator_silent_during_replacement':True,'chant_ends_before_next_narration':True,'environment':'Existing English forest/crowd/fire stems; crowd adds distant murmurs and nearby bustle','not_integrated_in_unity':True}
(ROOT/'mix-validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2));print(json.dumps(report,ensure_ascii=False,indent=2))
