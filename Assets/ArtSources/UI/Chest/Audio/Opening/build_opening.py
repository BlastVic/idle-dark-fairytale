from pathlib import Path
import sys,subprocess,json,numpy as np
P=Path(__file__).resolve().parent;ROOT=P.parents[5];SR=44100
sys.path.insert(0,str(ROOT/'output/prologue-film/tools'));import imageio_ffmpeg
FF=imageio_ffmpeg.get_ffmpeg_exe()
def read(name):
 return np.frombuffer(subprocess.run([FF,'-v','error','-i',str(P/name),'-ar',str(SR),'-ac','2','-f','f32le','-'],capture_output=True,check=True).stdout,'<f4').reshape(-1,2).astype(float)
def fade(a,attack=.005,release=.04):
 a=a.copy();n=min(round(attack*SR),len(a)//3);m=min(round(release*SR),len(a)//3)
 a[:n]*=np.linspace(0,1,n)[:,None];a[-m:]*=np.linspace(1,0,m)[:,None];return a
def peak(a,v):return a*v/max(abs(a).max(),1e-9)
mix=np.zeros((round(2.18*SR),2));cues=[]
def put(a,t,name):
 j=round(t*SR);n=min(len(a),len(mix)-j);mix[j:j+n]+=a[:n];cues.append({'layer':name,'start':t,'duration':n/SR})
wood=read('shake-1.mp3')
# Short foley accents follow the authored rotation extremes, increasing in intensity.
for i,t in enumerate([.4,.56,.70,.81,.91,1.00,1.08]):
 start=round((.005+(i%3)*.045)*SR);a=wood[start:start+round(.085*SR)]
 put(peak(fade(a,.003,.025),.10+.018*i),t,'wood-iron shake')
charge=read('charge-3.mp3')[:round(1.65*SR)]
t=np.arange(len(charge))/SR;envelope=np.interp(t,[0,.9,1.25,1.65],[0,.85,.3,0])**1.4
put(peak(charge,.16)*envelope[:,None],.30,'magic charge and release')
a=read('open-2.mp3')[round(.075*SR):]
put(peak(fade(a,.003,.16),.52),1.25,'latch and lid opening')
assert abs(mix).max()<.95
out=ROOT/'Assets/Audio/Sounds/ChestOpening.mp3'
subprocess.run([FF,'-v','error','-y','-f','f32le','-ar',str(SR),'-ac','2','-i','-','-c:a','libmp3lame','-b:a','192k',str(out)],input=mix.astype('<f4').tobytes(),check=True)
(P/'timing.json').write_text(json.dumps({'duration':2.18,'cues':cues,'peak_dbfs':float(20*np.log10(abs(mix).max())),'reference':'Chest.json open animation; opening attachment switches at 1.25s','runtime_audition':False},indent=2))
print('Built 2.18s opening, peak',20*np.log10(abs(mix).max()))
