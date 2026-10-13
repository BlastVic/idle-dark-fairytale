"""Reproducible 44s seamless cabin ambience; numpy + PCM stems decoded with afconvert."""
from pathlib import Path
import numpy as np
import wave, json
RATE=44100
SRC=Path('output/hunter-cabin/source')
DST=Path('Assets/Audio/HunterCabin'); DST.mkdir(parents=True,exist_ok=True)
def read(name):
 with wave.open(str(SRC/(name+'.wav'))) as f:
  assert f.getframerate()==RATE and f.getsampwidth()==2
  a=np.frombuffer(f.readframes(f.getnframes()),'<i2').reshape(-1,f.getnchannels()).astype(float)/32768
 if a.shape[1]==1:a=np.repeat(a,2,axis=1)
 return a[:,:2]
def filt(a,lo,hi):
 f=np.fft.rfftfreq(len(a),1/RATE)
 gain=(1/(1+(f/hi)**8))*(1-np.exp(-(f/lo)**4));gain[0]=0
 return np.fft.irfft(np.fft.rfft(a,axis=0)*gain[:,None],n=len(a),axis=0)
def level(a,rms):return a*rms/max(np.sqrt(np.mean(a*a)),1e-8)
def ring(names,rms,lo,hi):
 n=44*RATE; out=np.zeros((n,2));fade=2*RATE
 for i,name in enumerate(names):
  a=read(name)[:24*RATE];a=level(filt(a,lo,hi),rms)
  w=np.ones(len(a));r=np.linspace(0,np.pi/2,fade)
  w[:fade]=np.sin(r);w[-fade:]=np.cos(r)
  idx=(np.arange(len(a))+i*22*RATE)%n
  out[idx]+=a*w[:,None]
 return out
# Revision 3: quiet room, no continuous fire layer. Only three short ember ticks.
wind=ring(['wind-a','wind-b'],.0012,200,550)
wind=np.roll(wind,9*RATE,axis=0)*np.array([.85,1])
fire=np.zeros_like(wind)
events=[]
for name,t,peak,region in [('fire-a',7.4,.072,0),('fire-b',23.8,.056,1),('fire-a',38.2,.064,2)]:
 a=filt(read(name),900,4500)
 # Select isolated high-frequency transients from separate source regions.
 lo=int((2+region*6)*RATE);hi=min(lo+5*RATE,len(a)-RATE)
 energy=np.mean(a*a,axis=1)
 smooth=np.convolve(energy,np.ones(220)/220,mode='same')
 center=lo+int(np.argmax(smooth[lo:hi]))
 a=a[max(0,center-int(.012*RATE)):center+int(.148*RATE)].copy()
 a-=a.mean(axis=0)
 attack=int(.006*RATE);release=int(.11*RATE)
 a[:attack]*=np.linspace(0,1,attack)[:,None]
 a[-release:]*=np.linspace(1,0,release)[:,None]**2
 a*=peak/max(abs(a).max(),1e-8)
 a*=np.array([1,.88])
 j=round(t*RATE);fire[j:j+len(a)]+=a
 events.append({'time':t,'duration':len(a)/RATE,'peak':peak})
mix=wind+fire
def write(path,a):
 with wave.open(str(path),'wb') as f:
  f.setnchannels(2);f.setsampwidth(2);f.setframerate(RATE);f.writeframes(np.round(a*32767).astype('<i2').tobytes())
write(DST/'HunterCabinLoop.wav',mix)
# Two cycles expose the actual seam at 44s for audition.
write(Path('output/hunter-cabin/two-loops-preview.wav'),np.tile(mix,(2,1)))
write(Path('output/hunter-cabin/quiet-cabin-v3.wav'),np.tile(mix,(2,1)))
report={'duration_seconds':44,'sample_rate':RATE,'peak_dbfs':float(20*np.log10(abs(mix).max())), 'rms_dbfs':float(20*np.log10(np.sqrt(np.mean(mix*mix)))),'boundary_step':float(abs(mix[0]-mix[-1]).max()),'interior_step_p99':float(np.quantile(abs(np.diff(mix,axis=0)),.99)),'fire_events':events,'continuous_fire':False,'room_bed_rms_dbfs':float(20*np.log10(np.sqrt(np.mean(wind*wind))))}
assert report['peak_dbfs']<0 and report['boundary_step']<report['interior_step_p99']
Path('ArtDirection/Audio/HunterCabin/audio-validation.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))
