from pathlib import Path
import sys,subprocess,json,wave,numpy as np
P=Path(__file__).resolve().parent;ROOT=P.parents[5];SR=44100
sys.path.insert(0,str(ROOT/'output/prologue-film/tools'));import imageio_ffmpeg
FF=imageio_ffmpeg.get_ffmpeg_exe()
def read(p):return np.frombuffer(subprocess.run([FF,'-v','error','-i',str(p),'-ar',str(SR),'-ac','2','-f','f32le','-'],capture_output=True,check=True).stdout,'<f4').reshape(-1,2).copy()
def fit(a,d):
 e=np.max(abs(a),axis=1);active=np.where(e>max(e.max()*.02,.0001))[0];a=a[max(0,active[0]-220):min(len(a),active[-1]+2205)]
 ratio=len(a)/(SR*d);filters=[]
 while ratio>2:filters.append('atempo=2');ratio/=2
 while ratio<.5:filters.append('atempo=0.5');ratio*=2
 filters.append('atempo='+str(ratio))
 raw=subprocess.run([FF,'-v','error','-f','f32le','-ar',str(SR),'-ac','2','-i','-','-af',','.join(filters),'-f','f32le','-'],input=a.astype('<f4').tobytes(),capture_output=True,check=True).stdout
 b=np.frombuffer(raw,'<f4').reshape(-1,2).copy();n=round(d*SR);return np.pad(b,((0,max(0,n-len(b))),(0,0)))[:n]
def shape(a,pk,release):
 a=a.copy();n=round(.003*SR);m=round(release*SR);a[:n]*=np.linspace(0,1,n)[:,None];a[-m:]*=np.linspace(1,0,m)[:,None]**1.4;a*=pk/max(abs(a).max(),1e-9);return a
def write(path,a):
 with wave.open(str(path),'wb') as w:w.setnchannels(2);w.setsampwidth(2);w.setframerate(SR);w.writeframes(np.round(a*32767).astype('<i2').tobytes())
slash=shape(fit(read(P/'slash-1.mp3'),.10),.42,.018)
spin=shape(fit(read(P/'spin-1.mp3'),1.2667),.32,.20)
out=ROOT/'output/scythe-sweep-audio/corrected';out.mkdir(exist_ok=True)
write(out/'slash-stem.wav',slash);write(out/'spin-stem.wav',spin)
for speed in [1,2]:
 start=.46/speed;hit=.56/speed;end=hit+1.2667
 a=np.zeros((round(2.05*SR),2));s=slash if speed==1 else shape(fit(slash,.1/speed),.42,.01)
 j=round(start*SR);a[j:j+len(s)]+=s;j=round(hit*SR);a[j:j+len(spin)]+=spin
 assert abs(a).max()<1
 write(out/f'timeline-{speed}x.wav',a)
 subprocess.run([FF,'-v','error','-y','-i',str(out/f'timeline-{speed}x.wav'),'-c:a','libmp3lame','-b:a','192k',str(out/f'timeline-{speed}x.mp3')],check=True)
 print('speed',speed,'slash',start,'spawn',hit,'disappear',end)
write(out/'scythe-sweep.wav',np.concatenate([slash,spin]))
(P/'timing.json').write_text(json.dumps({'slash_animation_time':.46,'hit_animation_time':.56,'animation_complete':1.18,'effect_start_time':.2333,'effect_last_attachment':1.5,'effect_visible_duration':1.2667,'effect_destroy_after':1.3667,'runtime_changed':False,'runtime_audition':False},indent=2))
