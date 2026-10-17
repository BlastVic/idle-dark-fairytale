from pathlib import Path
import sys,subprocess,json,numpy as np
ROOT=Path(__file__).resolve().parents[5]
sys.path.insert(0,str(ROOT/'output/prologue-film/tools'))
import imageio_ffmpeg
ff=imageio_ffmpeg.get_ffmpeg_exe();sr=44100
src=Path(__file__).parent/'crystal-reward-take2.mp3'
def decode(path):
 return np.frombuffer(subprocess.run([ff,'-v','error','-i',str(path),'-ar',str(sr),'-ac','2','-f','f32le','-'],capture_output=True,check=True).stdout,'<f4').reshape(-1,2).copy()
a=decode(src)[:round(.9*sr)]
a[:round(.003*sr)]*=np.linspace(0,1,round(.003*sr))[:,None]
a[-round(.18*sr):]*=np.linspace(1,0,round(.18*sr))[:,None]**2
a*=10**(-4/20)/abs(a).max()
dst=ROOT/'Assets/Audio/Sounds/treasure chest drops.mp3'
subprocess.run([ff,'-v','error','-y','-f','f32le','-ar',str(sr),'-ac','2','-i','-','-c:a','libmp3lame','-b:a','192k',str(dst)],input=a.astype('<f4').tobytes(),check=True)
b=decode(dst)
assert len(b)==round(.9*sr) and abs(b).max()<1
report={'duration':len(b)/sr,'peak_dbfs':float(20*np.log10(abs(b).max())),'rms_dbfs':float(20*np.log10(np.sqrt(np.mean(b*b)))),'opening_start':1.25,'opening_end':2.15,'next_reveal':2.2,'runtime_audition':False}
(ROOT/'output/chest-audio/validation.json').write_text(json.dumps(report,indent=2));print(report)
